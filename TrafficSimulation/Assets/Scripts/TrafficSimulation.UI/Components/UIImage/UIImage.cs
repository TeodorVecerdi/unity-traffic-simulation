using System.Diagnostics.CodeAnalysis;
#if LETAI_TRUESHADOW
using LeTai.TrueShadow;
using LeTai.TrueShadow.PluginInterfaces;
#endif
using TrafficSimulation.Core;
using TrafficSimulation.UI.Colors;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace TrafficSimulation.UI;

[RequireComponent(typeof(Image))]
[ExecuteAlways]
public sealed partial class UIImage : BaseUIBehaviour, IColorProperties, IColorAnimatable, IMeshModifier
#if LETAI_TRUESHADOW
, ITrueShadowCustomHashProvider
#endif
{
    [field: AllowNull, MaybeNull]
    public Image Image => OrNull(ref field) ??= gameObject.GetOrAddComponent<Image>();

    public IColorProperties Color => AnimatedProperties ?? m_ColorProperties;

    private bool UseSimplifiedRendering => (GetMode() is ColorPresetMode.SolidColor || m_ColorProperties.SettingType is ColorSettingType.Reference && m_ColorProperties.GetPreset() is null)
                                        && m_AnimationController is { IsAnimating: false } or { FromMode: ColorPresetMode.SolidColor };

    private IColorProperties? AnimatedProperties => m_AnimationController.IsAnimating ? m_AnimationController : null;

    [NonSerialized] private Material? m_InlineMaterial;

    [Title("Color")]
    [SerializeField] private ColorComponentProperties m_ColorProperties = new();
    [Title("Mesh Modifier Properties")]
    [SerializeField] private MeshModifierProperties m_MeshModifierProperties = new();

    private readonly ColorAnimationController m_AnimationController = new();
#if LETAI_TRUESHADOW
    private TrueShadow? m_Shadow;
#endif

    private void Start() {
        HandlePropertiesChanged();
    }

    private void OnEnable() {
        Image.SetVerticesDirty();
#if LETAI_TRUESHADOW
        m_Shadow = GetComponent<TrueShadow>();
#endif
    }

    private void OnDisable() {
        Image.SetVerticesDirty();
    }

#if UNITY_EDITOR
    private void OnValidate() {
        SetDirty();
    }
#endif

    private void OnDestroy() {
        m_AnimationController.Dispose();
    }

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
        Image.SetMaterialDirty();
    }

    public async UniTask AnimateColorAsync(IColorProperties targetColor, float duration, CancellationToken cancellationToken) {
        if (!Application.isPlaying) throw new InvalidOperationException("Cannot animate color in edit mode.");

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken, cancellationToken);
        var wasCanceled = await m_AnimationController.Animate(this, targetColor, duration, SetDirty, linkedCts.Token).SuppressCancellationThrow();
        if (wasCanceled) return;

        m_ColorProperties = new ColorComponentProperties(targetColor);
        SetDirty();
    }

    private void HandlePropertiesChanged() {
        if (UseSimplifiedRendering) {
            DestroyInlineMaterial();
            Image.material = null;
        } else if (m_AnimationController.IsAnimating) {
            DestroyInlineMaterial();
            Image.material = m_AnimationController.Material;

            // Update properties on the material used for rendering too, as it can be different
            // from the one assigned (e.g., could be replaced by a mask).
            var materialForRendering = Image.materialForRendering;
            if (materialForRendering != Image.material && materialForRendering.shader == Image.material!.shader) {
                ColorMaterialHelper.SetMaterialProperties(materialForRendering, this);
            }
        } else if (m_ColorProperties.SettingType is ColorSettingType.Inline) {
            OrNull(ref m_InlineMaterial) ??= new Material(Shader.Find("TeodorVecerdi/Color")) {
                name = $"Color_{GetInstanceID()}",
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
        } else if (m_ColorProperties.SettingType is ColorSettingType.Reference) {
            DestroyInlineMaterial();
            Image.material = m_ColorProperties.GetPreset()?.GetMaterial();
        }
    }

    private void UpdateHashCode() {
#if LETAI_TRUESHADOW
        if (m_Shadow == null) return;
        m_Shadow.CustomHash = GetMode() switch {
            ColorPresetMode.SolidColor => HashCode.Combine(GetColor1()),
            ColorPresetMode.LinearGradient => HashCode.Combine(GetColor1(), GetColor2(), GetAngle(), GetStops()),
            ColorPresetMode.RadialGradient => HashCode.Combine(GetColor1(), GetColor2(), GetStops(), GetCenter(), GetRadius()),
            ColorPresetMode.ConicGradient => HashCode.Combine(GetColor1(), GetColor2(), GetAngle(), GetStops(), GetCenter()),
            _ => throw new ArgumentOutOfRangeException(),
        };
#endif
    }

    private void DestroyInlineMaterial() {
        m_InlineMaterial.DestroyObject();
        m_InlineMaterial = null;
    }
}
