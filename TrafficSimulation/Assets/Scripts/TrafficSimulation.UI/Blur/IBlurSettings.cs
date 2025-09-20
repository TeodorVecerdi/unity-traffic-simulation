using UnityEngine;

namespace TrafficSimulation.UI.Blur;

public interface IBlurSettings {
    public int DownSamplePasses { get; set; }
    public int Passes { get; set; }
    public float BlurSize { get; set; }
    public bool EnableTint { get; set; }
    public float TintAmount { get; set; }
    public Color Tint { get; set; }
    public bool EnableVibrancy { get; set; }
    public float Vibrancy { get; set; }
    public bool EnableNoise { get; set; }
    public Texture2D? NoiseTexture { get; set; }
}
