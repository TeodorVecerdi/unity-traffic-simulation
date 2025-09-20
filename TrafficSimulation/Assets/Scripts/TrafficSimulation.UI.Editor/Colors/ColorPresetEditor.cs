using TrafficSimulation.Core.Editor;
using TrafficSimulation.UI.Colors;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace TrafficSimulation.UI.Editor.Colors;

[CustomEditor(typeof(ColorPreset))]
public class ColorPresetEditor : OdinEditor {
    private static readonly EditorResource<Shader> s_Shader = new("Shaders/ColorPresetPreview.shader");
    private static readonly ThreadLocal<Material> s_PreviewMaterial = new(() => new Material(s_Shader.Value));

    // Shader property IDs
    private static readonly int s_Color1ShaderProperty = Shader.PropertyToID("_Color1");
    private static readonly int s_Color2ShaderProperty = Shader.PropertyToID("_Color2");
    private static readonly int s_AngleShaderProperty = Shader.PropertyToID("_Angle");
    private static readonly int s_StopsShaderProperty = Shader.PropertyToID("_Stops");
    private static readonly int s_CenterShaderProperty = Shader.PropertyToID("_Center");
    private static readonly int s_RadiusShaderProperty = Shader.PropertyToID("_Radius");
    private static readonly int s_ModeShaderProperty = Shader.PropertyToID("_Mode");

    public override Texture2D? RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height) {
        var colorPreset = target as ColorPreset;
        return colorPreset != null
            ? CreatePreviewTexture(width, height, colorPreset)
            : base.RenderStaticPreview(assetPath, subAssets, width, height);
    }

    private static Texture2D CreatePreviewTexture(int width, int height, ColorPreset colorPreset) {
        Texture2D texture = new(width, height, TextureFormat.ARGB32, false);
        var renderTexture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
        RenderTexture.active = renderTexture;

        var material = s_PreviewMaterial.Value;
        if (material == null) {
            material = new Material(s_Shader.Value);
            s_PreviewMaterial.Value = material;
        }

        // Set common properties
        material.SetColor(s_Color1ShaderProperty, colorPreset.GetColor1());
        material.SetColor(s_Color2ShaderProperty, colorPreset.GetColor2());

        // Set mode-specific properties
        var mode = colorPreset.GetMode();
        material.SetFloat(s_ModeShaderProperty, (float)mode);

        switch (mode) {
            case ColorPresetMode.LinearGradient:
            case ColorPresetMode.ConicGradient:
                material.SetFloat(s_AngleShaderProperty, colorPreset.GetAngle());
                material.SetVector(s_StopsShaderProperty, colorPreset.GetStops());
                break;

            case ColorPresetMode.RadialGradient:
                material.SetVector(s_StopsShaderProperty, colorPreset.GetStops());
                material.SetVector(s_CenterShaderProperty, colorPreset.GetCenter());
                material.SetVector(s_RadiusShaderProperty, colorPreset.GetRadius());
                break;
        }

        Graphics.Blit(Texture2D.whiteTexture, renderTexture, material);

        texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        texture.Apply(false, true);

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(renderTexture);
        return texture;
    }
}
