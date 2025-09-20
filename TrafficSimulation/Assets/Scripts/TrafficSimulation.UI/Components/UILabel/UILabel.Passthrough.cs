using System.Diagnostics.CodeAnalysis;
using TMPro;
using UnityEngine;

namespace TrafficSimulation.UI;

public sealed partial class UILabel {
    /// <summary>
    /// A string containing the text to be displayed.
    /// </summary>
    [AllowNull]
    public string Text {
        get => Graphic.text;
        set => Graphic.text = value ?? "";
    }

    /// <summary>
    /// The style of the text
    /// </summary>
    public FontStyles FontStyle {
        get => Graphic.fontStyle;
        set => Graphic.fontStyle = value;
    }

    /// <summary>
    /// The point size of the font.
    /// </summary>
    public float FontSize {
        get => Graphic.fontSize;
        set => Graphic.fontSize = value;
    }

    /// <summary>
    /// Enable text auto-sizing
    /// </summary>
    public bool EnableAutoSizing {
        get => Graphic.enableAutoSizing;
        set => Graphic.enableAutoSizing = value;
    }

    /// <summary>
    /// Minimum point size of the font when text auto-sizing is enabled.
    /// </summary>
    public float MinFontSize {
        get => Graphic.fontSizeMin;
        set => Graphic.fontSizeMin = value;
    }

    /// <summary>
    /// Maximum point size of the font when text auto-sizing is enabled.
    /// </summary>
    public float MaxFontSize {
        get => Graphic.fontSizeMax;
        set => Graphic.fontSizeMax = value;
    }

    /// <summary>
    /// Text alignment options
    /// </summary>
    public float CharacterSpacing {
        get => Graphic.characterSpacing;
        set => Graphic.characterSpacing = value;
    }

    /// <summary>
    /// The amount of additional spacing between words.
    /// </summary>
    public float WordSpacing {
        get => Graphic.wordSpacing;
        set => Graphic.wordSpacing = value;
    }

    /// <summary>
    /// The amount of additional spacing to add between each line of text.
    /// </summary>
    public float LineSpacing {
        get => Graphic.lineSpacing;
        set => Graphic.lineSpacing = value;
    }

    /// <summary>
    /// The amount of additional spacing to add between each line of text.
    /// </summary>
    public float ParagraphSpacing {
        get => Graphic.paragraphSpacing;
        set => Graphic.paragraphSpacing = value;
    }

    /// <summary>
    /// Text alignment options
    /// </summary>
    public TextAlignmentOptions Alignment {
        get => Graphic.alignment;
        set => Graphic.alignment = value;
    }

    /// <summary>
    /// Horizontal alignment options
    /// </summary>
    public HorizontalAlignmentOptions HorizontalAlignment {
        get => Graphic.horizontalAlignment;
        set => Graphic.horizontalAlignment = value;
    }

    /// <summary>
    /// Vertical alignment options
    /// </summary>
    public VerticalAlignmentOptions VerticalAlignment {
        get => Graphic.verticalAlignment;
        set => Graphic.verticalAlignment = value;
    }

    /// <summary>
    /// Enables or Disables Rich Text Tags
    /// </summary>
    public bool RichText {
        get => Graphic.richText;
        set => Graphic.richText = value;
    }

    /// <summary>
    /// Controls the text wrapping mode.
    /// </summary>
    public TextWrappingModes TextWrappingMode {
        get => Graphic.textWrappingMode;
        set => Graphic.textWrappingMode = value;
    }

    /// <summary>
    /// Controls the Text Overflow Mode
    /// </summary>
    public TextOverflowModes OverflowMode {
        get => Graphic.overflowMode;
        set => Graphic.overflowMode = value;
    }

    /// <summary>
    /// The margins of the text object.
    /// </summary>
    public Vector4 Margin {
        get => Graphic.margin;
        set => Graphic.margin = value;
    }
}
