using TrafficSimulation.Core;
using TrafficSimulation.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TrafficSimulation.UI.Colors;

[Serializable, HideReferenceObjectPicker, InlineProperty, HideLabel]
public sealed class ColorComponentProperties : IColorProperties {
    private static readonly List<ColorPresetMode> s_ValidColorModes = [
        ColorPresetMode.SolidColor,
        ColorPresetMode.LinearGradient,
        ColorPresetMode.RadialGradient,
        ColorPresetMode.ConicGradient,
    ];

    [SerializeField] private ColorSettingType m_SettingType = ColorSettingType.Reference;

    [ShowIf(nameof(IsPreset), false)]
    [SerializeField]
    private ColorPreset? m_Preset;

    [ShowIf(nameof(ShowOverrideAngle), false)]
    [SerializeField]
    private bool m_OverrideAngle;

    [ShowIf(nameof(ShowOverrideAlpha), false)]
    [OnValueChanged(nameof(OnOverrideAlphaChanged), true)]
    [SerializeField]
    private OptionalValue<float> m_OverrideAlpha = new(1.0f);

    [ShowIf(nameof(IsInline), false)]
    [ValueDropdown(nameof(s_ValidColorModes))]
    [SerializeField]
    private ColorPresetMode m_Mode = ColorPresetMode.SolidColor;

    [LabelText("@" + nameof(Color1FieldLabel))]
    [ShowIf(nameof(IsInline), false)]
    [SerializeField]
    private ColorPropertyReference m_Color1 = new(Color.white);

    [ShowIf(nameof(ShowColor2), false)]
    [SerializeField]
    private ColorPropertyReference m_Color2 = new(Color.white);

    [ShowIf(nameof(ShowAngleField), false)]
    [SerializeField, Angle]
    private float m_Angle;

    [ShowIf(nameof(ShowStops), false)]
    [SerializeField, MinMaxSlider(-1.0f, 2.0f, true)]
    private Vector2 m_Stops = new(0.0f, 1.0f);

    [ShowIf(nameof(ShowCenter), false)]
    [SerializeField]
    private Vector2 m_Center = new(0.5f, 0.5f);

    [ShowIf(nameof(ShowRadius), false)]
    [SerializeField]
    private Vector2 m_Radius = new(0.78f, 0.78f);

    public ColorSettingType SettingType => m_SettingType;

    public ColorComponentProperties() { }

    public ColorComponentProperties(IColorProperties properties) {
        if (properties is ColorPreset preset) {
            m_SettingType = ColorSettingType.Reference;
            m_Preset = preset;
            return;
        }

        if (properties is ColorComponentProperties { m_SettingType: ColorSettingType.Reference } ccp) {
            m_SettingType = ColorSettingType.Reference;
            m_Preset = ccp.m_Preset;
            m_OverrideAngle = ccp.m_OverrideAngle;
            m_Angle = ccp.m_Angle;
            m_OverrideAlpha = ccp.m_OverrideAlpha;
            return;
        }

        m_SettingType = ColorSettingType.Inline;
        m_Mode = properties.GetMode();
        m_Color1.SetInlineColor(properties.GetColor1());
        m_Color2.SetInlineColor(properties.GetColor2());
        m_Angle = properties.GetAngle();
        m_Stops = properties.GetStops();
        m_Center = properties.GetCenter();
        m_Radius = properties.GetRadius();
    }

    public ColorPreset? GetPreset() {
        return m_SettingType is ColorSettingType.Reference ? m_Preset : null;
    }

    public ColorPresetMode GetMode() {
        return m_SettingType is ColorSettingType.Reference ? m_Preset.OrNull()?.GetMode() ?? ColorPresetMode.SolidColor : m_Mode;
    }

    public Color GetColor1() {
        if (m_SettingType is ColorSettingType.Reference) {
            var color = m_Preset.OrNull()?.GetColor1() ?? Color.white;
            if (m_OverrideAlpha.Enabled && m_Preset.OrNull()?.GetMode() is ColorPresetMode.SolidColor) {
                color.a = Mathf.Clamp01(m_OverrideAlpha.Value);
            }

            return color;
        }

        return m_Color1.GetColor();
    }

    public Color GetColor2() {
        return m_SettingType is ColorSettingType.Reference ? m_Preset.OrNull()?.GetColor2() ?? Color.white : m_Color2.GetColor();
    }

    public float GetAngle() {
        if (m_SettingType is ColorSettingType.Inline || m_OverrideAngle) return m_Angle;
        return m_Preset.OrNull()?.GetAngle() ?? 0.0f;
    }

    public Vector2 GetStops() {
        return m_SettingType is ColorSettingType.Reference ? m_Preset.OrNull()?.GetStops() ?? new Vector2(0.0f, 1.0f) : m_Stops;
    }

    public Vector2 GetCenter() {
        return m_SettingType is ColorSettingType.Reference ? m_Preset.OrNull()?.GetCenter() ?? new Vector2(0.5f, 0.5f) : m_Center;
    }

    public Vector2 GetRadius() {
        return m_SettingType is ColorSettingType.Reference ? m_Preset.OrNull()?.GetRadius() ?? new Vector2(0.78f, 0.78f) : m_Radius;
    }

    private void OnOverrideAlphaChanged() {
        if (m_OverrideAlpha is { Enabled: true, Value: < 0.0f or > 1.0f }) {
            m_OverrideAlpha.Value = Mathf.Clamp01(m_OverrideAlpha.Value);
        }
    }

    private string Color1FieldLabel => m_Mode is ColorPresetMode.SolidColor ? "Color" : "Color 1";

    private bool IsPreset => m_SettingType is ColorSettingType.Reference;
    private bool IsInline => m_SettingType is ColorSettingType.Inline;
    private bool IsGradientMode => m_Mode is ColorPresetMode.LinearGradient or ColorPresetMode.RadialGradient or ColorPresetMode.ConicGradient;
    private bool ShowOverrideAngle => IsPreset && GetMode() is ColorPresetMode.LinearGradient or ColorPresetMode.ConicGradient;
    private bool ShowOverrideAlpha => IsPreset && m_Preset.OrNull()?.GetMode() is ColorPresetMode.SolidColor;
    private bool ShowColor2 => IsInline && IsGradientMode;
    private bool ShowAngleField => (IsInline || IsPreset && m_OverrideAngle) && GetMode() is ColorPresetMode.LinearGradient or ColorPresetMode.ConicGradient;
    private bool ShowStops => IsInline && IsGradientMode;
    private bool ShowCenter => IsInline && m_Mode is ColorPresetMode.RadialGradient or ColorPresetMode.ConicGradient;
    private bool ShowRadius => IsInline && m_Mode is ColorPresetMode.RadialGradient;
}
