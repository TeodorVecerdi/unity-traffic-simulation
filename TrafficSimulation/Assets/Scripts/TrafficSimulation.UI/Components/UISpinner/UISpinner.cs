using System.Diagnostics.CodeAnalysis;
using LitMotion;
using TrafficSimulation.Core;
using TrafficSimulation.Core.Tweening;
using TrafficSimulation.UI.Colors;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace TrafficSimulation.UI;

[ExecuteAlways]
[RequireComponent(typeof(Image))]
public sealed partial class UISpinner : BaseUIBehaviour, IColorProperties, IMeshModifier {
    [field: AllowNull, MaybeNull]
    public Image Image => OrNull(ref field) ??= this.GetOrAddComponent<Image>();

    public IColorProperties Color => m_ColorProperties;

    public float Radius { get; set; }
    public float Thickness { get; set; }
    public float Smoothness { get; set; }
    public float ArcLength { get; set; }
    public float Rotation { get; set; }
    public float OffsetRotationByArcLength { get; set; }

    [Title("Color")]
    [SerializeField] private ColorComponentProperties m_ColorProperties = new();
    [Title("Mesh Modifier Properties")]
    [SerializeField] private MeshModifierProperties m_MeshModifierProperties = new();
    [SerializeField] private bool m_StartSpinnerOnEnable = true;

    [Title("Spinner Settings")]
    [SerializeField, OnValueChanged(nameof(SetSpinnerPropertiesDirty))]
    private float m_Radius = 0.6f;
    [SerializeField, OnValueChanged(nameof(SetSpinnerPropertiesDirty))]
    private float m_Thickness = 0.14f;
    [SerializeField, OnValueChanged(nameof(SetSpinnerPropertiesDirty))]
    private float m_Smoothness = 0.06f;
    [SerializeField, Unit(Units.Radian, Units.Degree), OnValueChanged(nameof(SetSpinnerPropertiesDirty))]
    private float m_ArcLength = 1.24f;
    [SerializeField, Unit(Units.Radian, Units.Degree), OnValueChanged(nameof(SetSpinnerPropertiesDirty))]
    private float m_Rotation;
    [SerializeField, OnValueChanged(nameof(SetSpinnerPropertiesDirty))]
    private float m_OffsetRotationByArcLength;

    [Title("Sequence Settings")]
    [SerializeField, Unit(Units.Second)] private float m_InitialSpinDuration = 1.5f;
    [SerializeField, Unit(Units.Second)] private float m_FinalSpinDuration = 1.5f;
    [SerializeField, Unit(Units.Second)] private float m_SpinDuration = 0.75f;
    [SerializeField, Unit(Units.Second)] private float m_ScaleDuration = 0.75f;
    [SerializeField, Unit(Units.Radian, Units.Degree)] private float m_ScaleTarget = 1.0f;
    [SerializeField] private int m_Spins = 1;

    [NonSerialized] private Material? m_InlineMaterial;
    private MotionHandle m_SpinnerSequenceHandle;

    private void Start() {
        HandlePropertiesChanged();
        CopySerializedProperties();
    }

    private void OnEnable() {
        Image.SetVerticesDirty();
        if (Application.isPlaying && m_StartSpinnerOnEnable) {
            StartSpinner();
        }
    }

    private void OnDisable() {
        Image.SetVerticesDirty();
        StopSpinner();
    }

    private void OnDestroy() {
        StopSpinner();
    }

#if UNITY_EDITOR
    private void OnValidate() {
        SetDirty();
        SetSpinnerPropertiesDirty();
    }
#endif

    public ColorPreset? GetPreset() => m_ColorProperties.GetPreset();
    public ColorPresetMode GetMode() => Color.GetMode();
    public float GetAngle() => Color.GetAngle();
    public Color GetColor1() => Color.GetColor1();
    public Color GetColor2() => Color.GetColor2();
    public Vector2 GetStops() => Color.GetStops();
    public Vector2 GetCenter() => Color.GetCenter();
    public Vector2 GetRadius() => Color.GetRadius();

    public void SetColor(IColorProperties colorProperties) {
        m_ColorProperties = new ColorComponentProperties(colorProperties);
        SetDirty();
    }

    public void SetDirty() {
        HandlePropertiesChanged();
        Image.SetVerticesDirty();
    }

    private void SetSpinnerPropertiesDirty() {
        CopySerializedProperties();
        Image.SetVerticesDirty();
    }

    private void CopySerializedProperties() {
        Radius = m_Radius;
        Thickness = m_Thickness;
        Smoothness = m_Smoothness;
        ArcLength = m_ArcLength;
        Rotation = m_Rotation;
        OffsetRotationByArcLength = m_OffsetRotationByArcLength;
    }

    private void HandlePropertiesChanged() {
        OrNull(ref m_InlineMaterial) ??= new Material(Shader.Find("TeodorVecerdi/ColorSpinner")) {
            name = $"ColorSpinner_{GetInstanceID()}",
            hideFlags = HideFlags.HideAndDontSave,
        };

        Image.material = m_InlineMaterial;
        ColorMaterialHelper.SetMaterialProperties(m_InlineMaterial!, this);

        // Update properties on the material used for rendering too, as it can be different
        // from the one assigned (e.g., could be replaced by a mask).
        var materialForRendering = Image.materialForRendering;
        if (materialForRendering != Image.material && materialForRendering.shader == Image.material!.shader) {
            ColorMaterialHelper.SetMaterialProperties(materialForRendering, this);
        }
    }

    [Button, DisableInEditorMode]
    public void StartSpinner() {
        if (m_SpinnerSequenceHandle.IsPlaying()) {
            return;
        }

        var sequenceBuilder = LSequence.Create();
        AppendInitialSequence(ref sequenceBuilder);
        AppendSpinSequence(ref sequenceBuilder);
        AppendFinalSequence(ref sequenceBuilder);
        m_SpinnerSequenceHandle = sequenceBuilder.Run(builder => builder.WithLoops(-1)).CancelOnDestroy(this);
    }

    [Button, DisableInEditorMode]
    public void StopSpinner() {
        m_SpinnerSequenceHandle.TryCancel();
        m_SpinnerSequenceHandle = default;
    }

    private void AppendInitialSequence(ref MotionSequenceBuilder sequenceBuilder) {
        sequenceBuilder
            .Append(Rotate(0.0f, 2.0f * Mathf.PI, m_InitialSpinDuration))
            .Join(Scale(0.0f, m_ScaleTarget, m_ScaleDuration, Ease.InQuad))
            .Join(LCallback.Create(() => {
                OffsetRotationByArcLength = 1.0f;
                Image.SetVerticesDirty();
            }));
    }

    private void AppendSpinSequence(ref MotionSequenceBuilder sequenceBuilder) {
        var step = 2.0f / m_Spins;
        for (var i = 0; i < m_Spins; i++) {
            sequenceBuilder.Append(Rotate(0.0f, 2.0f * Mathf.PI, m_SpinDuration));
            sequenceBuilder.Join(LMotion.Create(1.0f - i * step, 1.0f - (i + 1) * step, m_SpinDuration).WithDefaults().WithEase(Ease.Linear).Bind(value => {
                OffsetRotationByArcLength = value;
                Image.SetVerticesDirty();
            }));
        }
    }

    private void AppendFinalSequence(ref MotionSequenceBuilder sequenceBuilder) {
        sequenceBuilder
            .Append(Rotate(0.0f, 2.0f * Mathf.PI, m_FinalSpinDuration))
            .Join(Scale(m_ScaleTarget, 0.0f, m_ScaleDuration, Ease.OutQuad, m_FinalSpinDuration - m_ScaleDuration));
    }

    private MotionHandle Rotate(float start, float end, float duration) {
        return LMotion.Create(start, end, duration).WithDefaults().WithEase(Ease.Linear).Bind(value => {
            Rotation = value;
            Image.SetVerticesDirty();
        }).CancelOnDestroy(this);
    }

    private MotionHandle Scale(float start, float end, float duration, Ease ease, float delay = 0.0f) {
        return LMotion.Create(start, end, duration).WithDefaults().WithDelay(delay).WithEase(ease).Bind(value => {
            ArcLength = value;
            Image.SetVerticesDirty();
        }).CancelOnDestroy(this);
    }
}
