using System.Diagnostics;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;

namespace TrafficSimulation.UI.Blur;

[ExecuteAlways]
[RequireComponent(typeof(BlurRunner), typeof(RawImage), typeof(RectTransform))]
public class BlurredImage : SerializedMonoBehaviour, IBlurSettings {
    [Title("References")]
    [SerializeField, Required] private Texture2D m_SourceTexture = null!;

    [Title("Settings")]
    [SerializeField, HideIf("@UnityEngine.Application.isPlaying")]
    private bool m_EnableInEditorMode;

    [SerializeField] private GraphicsFormat m_ColorFormat = GraphicsFormat.R8G8B8A8_UNorm;

    [Title("Blur Settings")]
    [SerializeField, Range(0, 8)]
    private int m_DownSamplePasses = 3;
    [SerializeField, Range(0, 64)]
    private int m_Passes = 8;
    [SerializeField]
    private float m_BlurSize = 1.0f;

    [SerializeField]
    private bool m_EnableTint;
    [SerializeField, Indent, ShowIf(nameof(m_EnableTint))]
    private float m_TintAmount;
    [SerializeField, Indent, ShowIf(nameof(m_EnableTint))]
    private Color m_Tint = Color.black;

    [SerializeField]
    private bool m_EnableVibrancy;
    [SerializeField, Indent, ShowIf(nameof(m_EnableVibrancy))]
    private float m_Vibrancy;

    [SerializeField]
    private bool m_EnableNoise;
    [SerializeField, Indent, ShowIf(nameof(m_EnableNoise))]
    private Texture2D? m_NoiseTexture;

    [ShowInInspector, DisplayAsString, Unit(Units.Millisecond)]
    private double m_ElapsedTime;

    [ShowInInspector, NonSerialized]
    private RenderTexture? m_TargetTexture;

    private BlurRunner BlurRunner => OrNull(ref m_BlurRunner) ??= GetComponent<BlurRunner>();
    private RawImage Image => OrNull(ref m_Image) ??= GetComponent<RawImage>();

    private BlurRunner? m_BlurRunner;
    private RawImage? m_Image;
    private List<RenderTexture> m_TexturesToRelease = [];

    private void OnEnable() {
        Image.enabled = true;
    }

    private void OnDisable() {
        Image.enabled = false;
    }

    private void OnValidate() {
        Blur();
    }

    private void Update() {
        Blur();
        if (m_TexturesToRelease.Count == 0) return;
        m_TexturesToRelease.ForEach(renderTexture => renderTexture.DestroyObject());
        m_TexturesToRelease.Clear();
    }

    public void SetTexture(Texture2D texture) {
        m_SourceTexture = texture;
        Blur();
    }

    private void Blur() {
        var timestamp = Stopwatch.GetTimestamp();
        if (!Application.isPlaying && !m_EnableInEditorMode || !enabled) {
            return;
        }

        if (m_TargetTexture is null) {
            RegenerateTargetTexture();
        } else if (m_SourceTexture.width != m_TargetTexture.width || m_SourceTexture.height != m_TargetTexture.height) {
            RegenerateTargetTexture();
        }

        Image.texture = m_TargetTexture;
        Graphics.Blit(m_SourceTexture, m_TargetTexture);
        BlurRunner.Blur(this, m_TargetTexture!);
        m_ElapsedTime = TimeSpan.FromTicks(Stopwatch.GetTimestamp() - timestamp).TotalMilliseconds;
    }

    private void RegenerateTargetTexture() {
        if (m_TargetTexture != null) {
            m_TexturesToRelease.Add(m_TargetTexture);
        }

        var resolution = new Vector2Int(m_SourceTexture.width, m_SourceTexture.height);
        m_TargetTexture = new RenderTexture(resolution.x, resolution.y, 0, m_ColorFormat, 0) {
            name = $"BlurTargetTexture_{resolution.x}x{resolution.y}@{name}",
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.DontSave,
        };
    }

    public int DownSamplePasses {
        get => m_DownSamplePasses;
        set => m_DownSamplePasses = value;
    }

    public int Passes {
        get => m_Passes;
        set => m_Passes = value;
    }

    public float BlurSize {
        get => m_BlurSize;
        set => m_BlurSize = value;
    }

    public bool EnableTint {
        get => m_EnableTint;
        set => m_EnableTint = value;
    }

    public float TintAmount {
        get => m_TintAmount;
        set => m_TintAmount = value;
    }

    public Color Tint {
        get => m_Tint;
        set => m_Tint = value;
    }

    public bool EnableVibrancy {
        get => m_EnableVibrancy;
        set => m_EnableVibrancy = value;
    }

    public float Vibrancy {
        get => m_Vibrancy;
        set => m_Vibrancy = value;
    }

    public bool EnableNoise {
        get => m_EnableNoise;
        set => m_EnableNoise = value;
    }

    public Texture2D? NoiseTexture {
        get => m_NoiseTexture;
        set => m_NoiseTexture = value;
    }
}
