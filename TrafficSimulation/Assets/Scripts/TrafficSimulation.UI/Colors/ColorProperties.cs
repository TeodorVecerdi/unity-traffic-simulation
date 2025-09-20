using UnityEngine;

namespace TrafficSimulation.UI.Colors;

public sealed class ColorProperties : IColorProperties {
    private ColorPresetMode Mode { get; init; }
    private Color Color1 { get; init; }
    private Color Color2 { get; init; }
    private float Angle { get; init; }
    private Vector2 Stops { get; init; } = new(0.0f, 1.0f);
    private Vector2 Center { get; init; } = new(0.5f, 0.5f);
    private Vector2 Radius { get; init; } = new(0.78f, 0.78f);

    private ColorProperties() { }

    public ColorPresetMode GetMode() => Mode;
    public Color GetColor1() => Color1;
    public Color GetColor2() => Color2;
    public float GetAngle() => Angle;
    public Vector2 GetStops() => Stops;
    public Vector2 GetCenter() => Center;
    public Vector2 GetRadius() => Radius;

    public static ColorProperties Create(IColorProperties other) => new() {
        Mode = other.GetMode(),
        Color1 = other.GetColor1(),
        Color2 = other.GetColor2(),
        Angle = other.GetAngle(),
        Stops = other.GetStops(),
        Center = other.GetCenter(),
        Radius = other.GetRadius(),
    };

    public static ColorProperties CreateSolidColor(Color color) => new() {
        Mode = ColorPresetMode.SolidColor,
        Color1 = color,
        Color2 = color,
    };

    public static ColorProperties CreateLinearGradient(Color color1, Color color2, float angle, Vector2 stops) => new() {
        Mode = ColorPresetMode.LinearGradient,
        Color1 = color1,
        Color2 = color2,
        Angle = angle,
        Stops = stops,
    };

    public static ColorProperties CreateRadialGradient(Color color1, Color color2, Vector2 stops, Vector2 center, Vector2 radius) => new() {
        Mode = ColorPresetMode.RadialGradient,
        Color1 = color1,
        Color2 = color2,
        Stops = stops,
        Center = center,
        Radius = radius,
    };

    public static ColorProperties CreateConicGradient(Color color1, Color color2, float angle, Vector2 stops, Vector2 center) => new() {
        Mode = ColorPresetMode.ConicGradient,
        Color1 = color1,
        Color2 = color2,
        Angle = angle,
        Stops = stops,
        Center = center,
    };
}
