using System.Globalization;
using UnityEngine;

namespace TrafficSimulation.UI.Colors;

public static class ColorUtilities {
    /// <summary>
    /// Parses a hex color representation from a <see cref="ReadOnlySpan{T}"/> of characters.
    /// </summary>
    /// <param name="hexColor">The hex color representation.</param>
    /// <returns>The parsed color.</returns>
    public static Color ParseHexColor(ReadOnlySpan<char> hexColor) {
        TryParseHexColor(hexColor, out var color);
        return color;
    }

    /// <summary>
    /// Tries to parse a hex color representation from a <see cref="ReadOnlySpan{T}"/> of characters.
    /// </summary>
    /// <param name="hexColor">The hex color representation.</param>
    /// <param name="color">The parsed color, if successful.</param>
    /// <returns><see langword="true"/> if the parsing was successful; otherwise, <see langword="false"/>.</returns>
    public static bool TryParseHexColor(ReadOnlySpan<char> hexColor, out Color color) {
        color = Color.clear;

        if (hexColor.Length is 0) return false;
        if (hexColor[0] == '#') hexColor = hexColor[1..];
        if (hexColor.Length is not (3 or 4 or 6 or 8)) return false;

        var isShortHex = hexColor.Length is 3 or 4;

        var redSpan = isShortHex ? hexColor[..1] : hexColor[..2];
        if (!byte.TryParse(redSpan, NumberStyles.HexNumber, null, out var red)) {
            return false;
        }

        var greenSpan = isShortHex ? hexColor[1..2] : hexColor[2..4];
        if (!byte.TryParse(greenSpan, NumberStyles.HexNumber, null, out var green)) {
            return false;
        }

        var blueSpan = isShortHex ? hexColor[2..3] : hexColor[4..6];
        if (!byte.TryParse(blueSpan, NumberStyles.HexNumber, null, out var blue)) {
            return false;
        }

        var hasAlpha = hexColor.Length is 4 or 8;
        var alpha = !hasAlpha ? byte.MaxValue : (byte)0;
        if (hasAlpha) {
            var alphaSpan = isShortHex ? hexColor[3..4] : hexColor[6..8];
            if (!byte.TryParse(alphaSpan, NumberStyles.HexNumber, null, out alpha)) {
                return false;
            }
        }

        if (isShortHex) {
            red *= 17;
            green *= 17;
            blue *= 17;
            if (hasAlpha) alpha *= 17;
        }

        color = new Color32(red, green, blue, alpha);
        return true;
    }
}
