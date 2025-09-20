using TrafficSimulation.Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TrafficSimulation.UI.Colors;

[Serializable, InlineProperty]
public sealed class ColorPropertyReference {
    [HideInInspector]
    [SerializeField] private ColorSettingType m_Type = ColorSettingType.Inline;

    [LabelWidth(60)]
    [ShowIf(nameof(IsInline), false), InlineButton("@m_Type = ColorSettingType.Reference", SdfIconType.Link, "")]
    [SerializeField] private Color m_Color = Color.white;

    [LabelWidth(60)]
    [ShowIf(nameof(IsReference), false), InlineButton("@m_Type = ColorSettingType.Inline", SdfIconType.PaintBucket, "")]
    [SerializeField] private ColorPreset? m_Preset;

    [LabelWidth(60)]
    [ShowIf(nameof(ShowProperty), false)]
    [SerializeField] private ColorProperty m_Property = ColorProperty.Color1;

    [LabelWidth(60)]
    [ShowIf(nameof(IsReference), false)]
    [OnValueChanged(nameof(OnOverrideAlphaChanged), true)]
    [SerializeField]
    private OptionalValue<float> m_Alpha = new(1.0f);

    public ColorSettingType Type => m_Type;
    public ColorPreset? Preset => m_Preset;
    public ColorProperty Property => m_Property;

    private bool IsInline => m_Type == ColorSettingType.Inline;
    private bool IsReference => m_Type == ColorSettingType.Reference;
    private bool ShowProperty => IsReference && m_Preset != null && m_Preset.Mode is not ColorPresetMode.SolidColor;

    public ColorPropertyReference() { }

    public ColorPropertyReference(Color color) {
        m_Type = ColorSettingType.Inline;
        m_Color = color;
    }

    public ColorPropertyReference(ColorPreset preset, ColorProperty property) {
        m_Type = ColorSettingType.Reference;
        m_Preset = preset;
        m_Property = property;
    }

    public Color GetColor() {
        return m_Type switch {
            ColorSettingType.Inline => m_Color,
            ColorSettingType.Reference => GetReferencedColor(),
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    private Color GetReferencedColor() {
        if (m_Preset == null) return Color.white;
        try {
            Color color;
            if (m_Preset.Mode is ColorPresetMode.SolidColor) {
                color = m_Preset.GetColor1();
            } else {
                color = m_Property switch {
                    ColorProperty.Color1 => m_Preset.GetColor1(),
                    ColorProperty.Color2 => m_Preset.GetColor2(),
                    _ => throw new ArgumentOutOfRangeException(),
                };
            }

            if (m_Alpha.Enabled) {
                color.a = Mathf.Clamp01(m_Alpha.Value);
            }

            return color;
        } catch (StackOverflowException) {
            Debug.LogWarning("Detected circular reference in color preset");
            m_Preset = null;
            return Color.white;
        }
    }

    public void SetInlineColor(Color color) {
        m_Type = ColorSettingType.Inline;
        m_Color = color;
    }

    public void SetReference(ColorPreset preset, ColorProperty property) {
        m_Type = ColorSettingType.Reference;
        m_Preset = preset;
        m_Property = property;
    }

    private void OnOverrideAlphaChanged() {
        if (m_Alpha is { Enabled: true, Value: < 0.0f or > 1.0f }) {
            m_Alpha.Value = Mathf.Clamp01(m_Alpha.Value);
        }
    }
}
