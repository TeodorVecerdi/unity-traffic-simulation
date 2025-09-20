using UnityEngine;

namespace TrafficSimulation.UI.Colors;

public static class ColorMaterialHelper {
    private static readonly int s_Color1ShaderId = Shader.PropertyToID("_Color1");
    private static readonly int s_Color2ShaderId = Shader.PropertyToID("_Color2");
    private static readonly int s_StopsShaderId = Shader.PropertyToID("_Stops");
    private static readonly int s_CenterShaderId = Shader.PropertyToID("_Center");
    private static readonly int s_RadiusShaderId = Shader.PropertyToID("_Radius");
    private static readonly int s_AngleId = Shader.PropertyToID("_Angle");
    private static readonly int s_MorphProgressId = Shader.PropertyToID("_MorphProgress");

    private static readonly string[] s_ColorKeywords = ["COLOR_SOLID", "COLOR_LINEAR_GRADIENT", "COLOR_RADIAL_GRADIENT", "COLOR_CONIC_GRADIENT"];
    private static readonly string[] s_MorphKeywords = ["_GRADIENTFROM_LINEAR", "_GRADIENTFROM_RADIAL", "_GRADIENTFROM_CONIC", "_GRADIENTTO_LINEAR", "_GRADIENTTO_RADIAL", "_GRADIENTTO_CONIC"];

    public static void SetMaterialProperties(Material material, IColorProperties properties) {
        SetMaterialProperties(material, properties.GetMode(), properties.GetColor1(), properties.GetColor2(), properties.GetStops(), properties.GetCenter(), properties.GetRadius());
    }

    public static void SetMaterialProperties(Material material, ColorPresetMode mode, Color color1, Color color2, Vector2 stops, Vector2 center, Vector2 radius) {
        if (material == null) return;
        material.SetColor(s_Color1ShaderId, color1);
        material.SetColor(s_Color2ShaderId, color2);
        material.SetVector(s_StopsShaderId, stops);
        material.SetVector(s_CenterShaderId, center);
        material.SetVector(s_RadiusShaderId, radius);

        foreach (var keyword in s_ColorKeywords) {
            material.DisableKeyword(keyword);
        }

        switch (mode) {
            case ColorPresetMode.SolidColor:
                material.EnableKeyword("COLOR_SOLID");
                break;
            case ColorPresetMode.LinearGradient:
                material.EnableKeyword("COLOR_LINEAR_GRADIENT");
                break;
            case ColorPresetMode.RadialGradient:
                material.EnableKeyword("COLOR_RADIAL_GRADIENT");
                break;
            case ColorPresetMode.ConicGradient:
                material.EnableKeyword("COLOR_CONIC_GRADIENT");
                break;
        }
    }

    public static void SetMorphingMaterialProperties(Material material, ColorPresetMode fromMode, IColorProperties toProperties, float morphProgress) {
        SetMorphingMaterialProperties(material, fromMode, toProperties.GetMode(), toProperties.GetColor1(), toProperties.GetColor2(), toProperties.GetAngle(), toProperties.GetStops(), toProperties.GetCenter(), toProperties.GetRadius(), morphProgress);
    }

    public static void SetMorphingMaterialProperties(Material material, ColorPresetMode fromMode, ColorPresetMode toMode, Color color1, Color color2, float angle, Vector2 stops, Vector2 center, Vector2 radius, float morphProgress) {
        if (material == null) return;
        material.SetColor(s_Color1ShaderId, color1);
        material.SetColor(s_Color2ShaderId, color2);
        material.SetFloat(s_AngleId, angle);
        material.SetVector(s_StopsShaderId, stops);
        material.SetVector(s_CenterShaderId, center);
        material.SetVector(s_RadiusShaderId, radius);
        material.SetFloat(s_MorphProgressId, morphProgress);

        foreach (var keyword in s_MorphKeywords) {
            material.DisableKeyword(keyword);
        }

        material.EnableKeyword($"_GRADIENTFROM_{PresetToKeyword(fromMode)}");
        material.EnableKeyword($"_GRADIENTTO_{PresetToKeyword(toMode)}");
    }

    private static string PresetToKeyword(ColorPresetMode mode) {
        return mode switch {
            ColorPresetMode.LinearGradient => "LINEAR",
            ColorPresetMode.RadialGradient => "RADIAL",
            ColorPresetMode.ConicGradient => "CONIC",
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
        };
    }
}
