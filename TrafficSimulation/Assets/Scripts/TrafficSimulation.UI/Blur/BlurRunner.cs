using Sirenix.OdinInspector;
using UnityEngine;

namespace TrafficSimulation.UI.Blur;

public class BlurRunner : MonoBehaviour {
    private static readonly int s_BlurSizeShaderPropertyId = Shader.PropertyToID("_BlurSize");
    private static readonly int s_EnableTintShaderPropertyId = Shader.PropertyToID("_EnableTint");
    private static readonly int s_TintAmountShaderPropertyId = Shader.PropertyToID("_TintAmount");
    private static readonly int s_TintShaderPropertyId = Shader.PropertyToID("_Tint");
    private static readonly int s_EnableVibrancyShaderPropertyId = Shader.PropertyToID("_EnableVibrancy");
    private static readonly int s_VibrancyShaderPropertyId = Shader.PropertyToID("_Vibrancy");
    private static readonly int s_EnableNoiseShaderPropertyId = Shader.PropertyToID("_EnableNoise");
    private static readonly int s_NoiseTexShaderPropertyId = Shader.PropertyToID("_NoiseTex");
    private static readonly int s_ScreenResolutionShaderPropertyId = Shader.PropertyToID("_ScreenResolution");
    private static readonly int s_NoiseTextureResolutionShaderPropertyId = Shader.PropertyToID("_NoiseTextureResolution");

    private Shader? m_BlurShader;
    private Material? m_BlurMaterial;

    private void OnDisable() {
        if (m_BlurMaterial != null) {
            Destroy(m_BlurMaterial);
        }
    }

    public void Blur(IBlurSettings blurSettings, RenderTexture targetTexture) {
        PrepareMaterial(blurSettings, targetTexture);
        BlurImpl(blurSettings, targetTexture);
    }

    [Button, DisableInEditorMode]
    private void ResetMaterial() {
        if (m_BlurMaterial != null) {
            Destroy(m_BlurMaterial);
        }

        m_BlurShader = null;
        m_BlurMaterial = null;
    }

    private void PrepareMaterial(IBlurSettings blurSettings, RenderTexture targetTexture) {
        if (m_BlurMaterial == null) {
            if (m_BlurShader == null) {
                m_BlurShader = Shader.Find("TeodorVecerdi/Blur");
            }

            m_BlurMaterial = new Material(m_BlurShader);
        }

        m_BlurMaterial.SetFloat(s_BlurSizeShaderPropertyId, blurSettings.BlurSize);
        m_BlurMaterial.SetFloat(s_EnableTintShaderPropertyId, blurSettings.EnableTint ? 1.0f : 0.0f);
        m_BlurMaterial.SetFloat(s_TintAmountShaderPropertyId, blurSettings.TintAmount);
        m_BlurMaterial.SetColor(s_TintShaderPropertyId, blurSettings.Tint);
        m_BlurMaterial.SetFloat(s_EnableVibrancyShaderPropertyId, blurSettings.EnableVibrancy ? 1.0f : 0.0f);
        m_BlurMaterial.SetFloat(s_VibrancyShaderPropertyId, blurSettings.Vibrancy);
        m_BlurMaterial.SetFloat(s_EnableNoiseShaderPropertyId, blurSettings.EnableNoise ? 1.0f : 0.0f);
        m_BlurMaterial.SetTexture(s_NoiseTexShaderPropertyId, blurSettings.NoiseTexture);
        m_BlurMaterial.SetVector(s_ScreenResolutionShaderPropertyId, new Vector2(targetTexture.width, targetTexture.height));
        if (blurSettings.NoiseTexture != null) {
            m_BlurMaterial.SetVector(s_NoiseTextureResolutionShaderPropertyId, new Vector2(blurSettings.NoiseTexture.width, blurSettings.NoiseTexture.height));
        }
    }

    private void BlurImpl(IBlurSettings blurSettings, RenderTexture targetTexture) {
        var currentWidth = targetTexture.width;
        var currentHeight = targetTexture.height;
        var nonDownSamplePasses = Math.Max(0, blurSettings.Passes - blurSettings.DownSamplePasses);

        var src = RenderTexture.GetTemporary(currentWidth, currentHeight);
        src.filterMode = FilterMode.Bilinear;

        var activeRT = RenderTexture.active;
        RenderTexture.active = src;

        Graphics.Blit(targetTexture, src);

        // Downsample passes
        for (var i = 0; i < blurSettings.DownSamplePasses; i++) {
            currentWidth >>= 1;
            currentHeight >>= 1;

            var tempA = RenderTexture.GetTemporary(currentWidth, currentHeight);
            var tempB = RenderTexture.GetTemporary(currentWidth, currentHeight);
            tempA.filterMode = FilterMode.Bilinear;
            tempB.filterMode = FilterMode.Bilinear;

            // Src -> A, Horizontal blur pass
            Graphics.Blit(src, tempA, m_BlurMaterial, Passes.HorizontalBlur);

            // A -> B, Vertical blur pass
            Graphics.Blit(tempA, tempB, m_BlurMaterial, Passes.VerticalBlur);

            RenderTexture.ReleaseTemporary(tempA);
            RenderTexture.ReleaseTemporary(src);

            // Src = B
            src = tempB;
        }

        // Non-downsample passes
        for (var i = 0; i < nonDownSamplePasses; i++) {
            var tempA = RenderTexture.GetTemporary(currentWidth, currentHeight);
            var tempB = RenderTexture.GetTemporary(currentWidth, currentHeight);
            tempA.filterMode = FilterMode.Bilinear;
            tempB.filterMode = FilterMode.Bilinear;

            // Src -> A, Horizontal blur pass
            Graphics.Blit(src, tempA, m_BlurMaterial, Passes.HorizontalBlur);

            // A -> B, Vertical blur pass
            Graphics.Blit(tempA, tempB, m_BlurMaterial, Passes.VerticalBlur);

            RenderTexture.ReleaseTemporary(tempA);
            RenderTexture.ReleaseTemporary(src);

            // Src = B
            src = tempB;
        }

        // Src -> Target, Tint, vibrancy pass, Upsample
        Graphics.Blit(src, targetTexture, m_BlurMaterial, Passes.ColorEffects);

        RenderTexture.ReleaseTemporary(src);
        RenderTexture.active = activeRT;
    }

    private static class Passes {
        public const int HorizontalBlur = 0;
        public const int VerticalBlur = 1;
        public const int ColorEffects = 2;
    }
}
