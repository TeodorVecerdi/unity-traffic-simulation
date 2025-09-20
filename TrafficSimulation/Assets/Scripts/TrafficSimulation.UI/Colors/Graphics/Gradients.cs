using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

namespace TrafficSimulation.UI.Colors.Graphics;

internal static class Gradients {
    public static Color CalculateLinearGradient(float2 uv, Color color1, Color color2, float angle, float2 stops) {
        sincos(angle, out var sin, out var cos);

        var centeredUV = uv * 2.0f - 1.0f;
        var rotatedUV = float2(centeredUV.x * cos - centeredUV.y * sin, centeredUV.x * sin + centeredUV.y * cos);

        var t = GetInterpolationFactor(0.5f * (rotatedUV.y + 1.0f), stops);
        return Interpolate(color1, color2, t);
    }

    public static Color CalculateRadialGradient(float2 uv, Color color1, Color color2, float2 center, float2 radius, float2 stops) {
        // Convert UV to centered coordinates
        var centeredUV = uv - center;

        // Calculate distance from the center, normalized by radius
        // Using separate X and Y radius allows for elliptical gradients
        var normalizedDist = centeredUV / radius;
        var dist = length(normalizedDist);

        // Calculate interpolation factor
        var t = GetInterpolationFactor(dist, stops);
        return Interpolate(color1, color2, t);
    }

    public static Color CalculateConicGradient(float2 uv, Color color1, Color color2, float2 center, float startAngle, float2 stops) {
        // Convert UV to centered coordinates
        var centeredUV = uv - center;

        // Calculate angle from center
        var angle = -atan2(centeredUV.y, centeredUV.x) + PIHALF;
        // Adjust angle to match start angle
        angle = (angle - startAngle) / PI2;
        // Normalize angle to [0, 1] range
        angle = frac(angle);

        // Calculate interpolation factor
        var t = GetInterpolationFactor(angle, stops);
        return Interpolate(color1, color2, t);
    }

    private static float GetInterpolationFactor(float x, float2 stops) {
        return saturate((x - stops.x) / (stops.y - stops.x));
    }

    private static Color Interpolate(Color color1, Color color2, float t) {
        return Color.Lerp(color1, color2, t);
    }
}
