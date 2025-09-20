using System.Diagnostics.CodeAnalysis;
using TrafficSimulation.Core;
using TrafficSimulation.UI.Colors;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace TrafficSimulation.UI;

[ExecuteAlways, DisallowMultipleComponent]
[RequireComponent(typeof(TextMeshProUGUI))]
public sealed partial class UILabel : BaseUIBehaviour, IColorProperties, IColorAnimatable {
    public IColorProperties Color => AnimatedProperties ?? m_ColorProperties;

    [field: AllowNull, MaybeNull]
    private TextMeshProUGUI Graphic => OrNull(ref field) ??= GetComponent<TextMeshProUGUI>();

    [Title("Color")]
    [SerializeField, OnValueChanged(nameof(SetDirty), true)]
    private ColorComponentProperties m_ColorProperties = new();

    private readonly AnimationController m_AnimationController = new();

    private IColorProperties? AnimatedProperties => m_AnimationController.IsAnimating ? m_AnimationController : null;

#if UNITY_EDITOR
    private void OnValidate() {
        Graphic.ForceMeshUpdate();
    }
#endif

    private void OnEnable() {
        Graphic.OnPreRenderText -= ModifyLabelMesh;
        Graphic.OnPreRenderText += ModifyLabelMesh;
        Graphic.SetAllDirty();
    }

    private void OnDisable() {
        Graphic.OnPreRenderText -= ModifyLabelMesh;
        Graphic.SetAllDirty();
    }

    public ColorPreset? GetPreset() => m_ColorProperties.GetPreset();
    public ColorPresetMode GetMode() => Color.GetMode();
    public Color GetColor1() => Color.GetColor1();
    public Color GetColor2() => Color.GetColor2();
    public float GetAngle() => Color.GetAngle();
    public Vector2 GetStops() => Color.GetStops();
    public Vector2 GetCenter() => Color.GetCenter();
    public Vector2 GetRadius() => Color.GetRadius();

    public void SetColor(IColorProperties colorProperties) {
        m_ColorProperties = new ColorComponentProperties(colorProperties);
        SetDirty();
    }

    public void SetDirty() {
        Graphic.ForceMeshUpdate();
    }

    public async UniTask AnimateColorAsync(IColorProperties targetColor, float duration, CancellationToken cancellationToken) {
        if (!Application.isPlaying) throw new InvalidOperationException("Cannot animate color in edit mode.");

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken, cancellationToken);
        var wasCanceled = await m_AnimationController.Animate(this, targetColor, duration, SetDirty, linkedCts.Token).SuppressCancellationThrow();
        if (wasCanceled) return;

        m_ColorProperties = new ColorComponentProperties(targetColor);
        SetDirty();
    }
}
