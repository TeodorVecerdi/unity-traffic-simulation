using TrafficSimulation.UI.Colors;
using TrafficSimulation.UI.Colors.Graphics;
using TMPro;
using UnityEngine;

namespace TrafficSimulation.UI;

public sealed partial class UILabel {
    private bool UseSimplifiedRendering => (m_ColorProperties.GetMode() is ColorPresetMode.SolidColor || m_ColorProperties.SettingType is ColorSettingType.Reference && m_ColorProperties.GetPreset() is null)
                                        && !m_AnimationController.IsAnimating;

    private void ModifyLabelMesh(TMP_TextInfo textInfo) {
        if (UseSimplifiedRendering) {
            ModifyLabelMeshSimple(textInfo);
            return;
        }

        var mode = GetMode();
        var color1 = GetColor1();
        var color2 = GetColor2();
        var angle = GetAngle() * Mathf.Deg2Rad;
        var stops = GetStops();
        var center = GetCenter();
        var radius = GetRadius();

        var bounds = CalculateBounds();
        var colorModifier = m_AnimationController.IsAnimating ? m_AnimationController.GetAnimatedColor : GetColorFunction(mode);

        for (var i = 0; i < textInfo.characterCount; i++) {
            var charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible || charInfo.elementType is not TMP_TextElementType.Character) {
                continue;
            }

            var materialIndex = charInfo.materialReferenceIndex;
            var vertexIndex = charInfo.vertexIndex;

            var vertexColors = textInfo.meshInfo[materialIndex].colors32;
            for (var j = 0; j < 4; j++) {
                var position = textInfo.meshInfo[materialIndex].vertices[vertexIndex + j];
                Vector2 uv = new((position.x - bounds.min.x) / bounds.size.x, (position.y - bounds.min.y) / bounds.size.y);
                vertexColors[vertexIndex + j] *= colorModifier(uv, color1, color2, angle, stops, center, radius);
            }
        }
    }

    private void ModifyLabelMeshSimple(TMP_TextInfo textInfo) {
        var color = GetColor1();
        for (var i = 0; i < textInfo.characterCount; i++) {
            var charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible || charInfo.elementType is not TMP_TextElementType.Character) {
                continue;
            }

            var materialIndex = charInfo.materialReferenceIndex;
            var vertexIndex = charInfo.vertexIndex;

            var vertexColors = textInfo.meshInfo[materialIndex].colors32;
            for (var j = 0; j < 4; j++) {
                vertexColors[vertexIndex + j] *= color;
            }
        }
    }

    private Bounds CalculateBounds() {
        Vector3 min = new(float.MaxValue, float.MaxValue, 0.0f);
        Vector3 max = new(float.MinValue, float.MinValue, 0.0f);

        for (var i = 0; i < Graphic.textInfo.characterCount; i++) {
            var charInfo = Graphic.textInfo.characterInfo[i];
            if (!charInfo.isVisible || charInfo.elementType is not TMP_TextElementType.Character) {
                continue;
            }

            var materialIndex = charInfo.materialReferenceIndex;
            var vertexIndex = charInfo.vertexIndex;

            var vertices = Graphic.textInfo.meshInfo[materialIndex].vertices;
            for (var vI = 0; vI < 4; vI++) {
                var position = vertices[vertexIndex + vI];
                min = Vector3.Min(min, position);
                max = Vector3.Max(max, position);
            }
        }

        return new Bounds((min + max) / 2.0f, max - min);
    }

    private static ColorFunction GetColorFunction(ColorPresetMode mode) {
        return mode switch {
            ColorPresetMode.SolidColor => SolidColor,
            ColorPresetMode.LinearGradient => LinearGradient,
            ColorPresetMode.RadialGradient => RadialGradient,
            ColorPresetMode.ConicGradient => ConicGradient,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
        };

        static Color SolidColor(Vector2 uv, Color color1, Color color2, float angle, Vector2 stops, Vector2 center, Vector2 radius) {
            return color1;
        }

        static Color LinearGradient(Vector2 uv, Color color1, Color color2, float angle, Vector2 stops, Vector2 center, Vector2 radius) {
            return Gradients.CalculateLinearGradient(uv, color1, color2, angle, stops);
        }

        static Color RadialGradient(Vector2 uv, Color color1, Color color2, float angle, Vector2 stops, Vector2 center, Vector2 radius) {
            return Gradients.CalculateRadialGradient(uv, color1, color2, center, radius, stops);
        }

        static Color ConicGradient(Vector2 uv, Color color1, Color color2, float angle, Vector2 stops, Vector2 center, Vector2 radius) {
            return Gradients.CalculateConicGradient(uv, color1, color2, center, angle, stops);
        }
    }

    private delegate Color ColorFunction(Vector2 uv, Color color1, Color color2, float angle, Vector2 stops, Vector2 center, Vector2 radius);
}
