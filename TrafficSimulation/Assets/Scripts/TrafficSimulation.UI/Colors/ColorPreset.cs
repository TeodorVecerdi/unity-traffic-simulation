using System.Diagnostics.CodeAnalysis;
using TrafficSimulation.Core.Attributes;
using Microsoft.Extensions.Logging;
using Sirenix.OdinInspector;
using UnityEngine;
using Vecerdi.Extensions.Logging;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace TrafficSimulation.UI.Colors;

[HideMonoScript]
[CreateAssetMenu(fileName = "New Color Preset", menuName = "Traffic Simulation/Color Preset", order = -1000)]
public sealed class ColorPreset : ScriptableObject, IColorProperties {
    private static Shader? s_Shader;

    [SerializeField, EnumToggleButtons]
    private ColorPresetMode m_Mode = ColorPresetMode.SolidColor;

    [TitleGroup("Properties")]
    [SerializeField, LabelText("@" + nameof(Color1Name))]
    private ColorPropertyReference m_Color1 = new(Color.white);

    [TitleGroup("Properties")]
    [ShowIf(nameof(ShowColor2))]
    [SerializeField, LabelText("@" + nameof(Color2Name))]
    private ColorPropertyReference m_Color2 = new(Color.white);

    [TitleGroup("Properties")]
    [ShowIf(nameof(ShowAngle))]
    [SerializeField, Angle]
    private float m_Angle;

    [TitleGroup("Properties")]
    [ShowIf(nameof(ShowStops))]
    [SerializeField, MinMaxSlider(0.0f, 1.0f, true)]
    private Vector2 m_Stops = new(0.0f, 1.0f);

    [TitleGroup("Properties")]
    [ShowIf(nameof(ShowCenter))]
    [SerializeField]
    private Vector2 m_Center = new(0.5f, 0.5f);

    [TitleGroup("Properties")]
    [ShowIf(nameof(ShowRadius))]
    [SerializeField]
    private Vector2 m_Radius = new(0.78f, 0.78f);

    [TitleGroup("Properties")]
    [ShowIf(nameof(ShowMaterial))]
    [SerializeReference]
    private Material? m_Material;

    public ColorPresetMode Mode => m_Mode;

    private bool ShowColor2 => m_Mode is ColorPresetMode.LinearGradient or ColorPresetMode.RadialGradient or ColorPresetMode.ConicGradient;
    private bool ShowAngle => m_Mode is ColorPresetMode.LinearGradient or ColorPresetMode.ConicGradient;
    private bool ShowStops => m_Mode is ColorPresetMode.LinearGradient or ColorPresetMode.RadialGradient or ColorPresetMode.ConicGradient;
    private bool ShowCenter => m_Mode is ColorPresetMode.RadialGradient or ColorPresetMode.ConicGradient;
    private bool ShowRadius => m_Mode is ColorPresetMode.RadialGradient;
    private bool ShowMaterial => m_Mode is ColorPresetMode.LinearGradient or ColorPresetMode.RadialGradient or ColorPresetMode.ConicGradient;

    private string Color1Name => m_Mode is ColorPresetMode.SolidColor ? "Color" : "First Color";
    private string Color2Name => "Second Color";

    [field: MaybeNull]
    private ILogger<ColorPreset> Logger => field ??= UnityLoggerFactory.CreateLogger<ColorPreset>();

    public ColorPresetMode GetMode() => m_Mode;

    public Color GetColor1() => m_Color1.GetColor();
    public Color GetColor2() => m_Color2.GetColor();
    public float GetAngle() => m_Angle;
    public Vector2 GetStops() => m_Stops;
    public Vector2 GetCenter() => m_Center;
    public Vector2 GetRadius() => m_Radius;
    public Material? GetMaterial() => m_Material;

    public void SetProperties(IColorProperties colorProperties, bool refreshUI = false) {
        m_Mode = colorProperties.GetMode();
        m_Color1.SetInlineColor(colorProperties.GetColor1());
        m_Color2.SetInlineColor(colorProperties.GetColor2());
        m_Angle = colorProperties.GetAngle();
        m_Stops = colorProperties.GetStops();
        m_Center = colorProperties.GetCenter();
        m_Radius = colorProperties.GetRadius();

        if (m_Mode is ColorPresetMode.SolidColor) {
            ClearMaterial();
        } else {
            FindOrCreateMaterial();
            UpdateMaterialProperties();
            UpdateMaterialName();
        }

        if (refreshUI) {
            RefreshUIElements();
        }
    }

    private static void RefreshUIElements() {
        foreach (var image in FindObjectsByType<UIImage>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) {
            image.SetDirty();
        }

        foreach (var spinner in FindObjectsByType<UISpinner>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) {
            spinner.SetDirty();
        }

        foreach (var label in FindObjectsByType<UILabel>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) {
            label.SetDirty();
        }

#if UNITY_EDITOR
        var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        if (prefabStage != null) {
            foreach (var image in prefabStage.FindComponentsOfType<UIImage>()) {
                image.SetDirty();
            }

            foreach (var spinner in prefabStage.FindComponentsOfType<UISpinner>()) {
                spinner.SetDirty();
            }

            foreach (var label in prefabStage.FindComponentsOfType<UILabel>()) {
                label.SetDirty();
            }
        }
#endif
    }

    private void ClearMaterial() {
        if (m_Material == null) return;

        m_Material.DestroyObject(true);
        m_Material = null;

#if UNITY_EDITOR
        var assetPath = AssetDatabase.GetAssetPath(this);
        if (string.IsNullOrEmpty(assetPath)) return;
        EditorApplication.delayCall += () => {
            // Remove sub-assets
            var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (var asset in assets) {
                if (asset is Material material) {
                    AssetDatabase.RemoveObjectFromAsset(material);
                }
            }

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        };
#endif
    }

    private void FindOrCreateMaterial() {
        LoadShaderIfNeeded();
        var initialMaterial = m_Material;
#if UNITY_EDITOR
        if (m_Material == null) {
            FindMaterial();
        }
#endif

        if (m_Material == null || m_Material.shader != s_Shader) {
            CreateMaterial();
        }

#if UNITY_EDITOR
        if (initialMaterial != m_Material) {
            EditorUtility.SetDirty(this);
            EditorApplication.delayCall += () => {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            };
        }
#endif
    }

    private void CreateMaterial() {
        LoadShaderIfNeeded();
        m_Material = new Material(s_Shader);
        m_Material.name = $"{name}Material";
        m_Material.hideFlags = HideFlags.NotEditable;

#if UNITY_EDITOR
        var assetPath = AssetDatabase.GetAssetPath(this);
        if (string.IsNullOrEmpty(assetPath)) return;

        EditorApplication.delayCall += () => {
            // Add material to asset
            AssetDatabase.AddObjectToAsset(m_Material, assetPath);
            EditorUtility.SetDirty(this);

            // Cleanup sub-assets, not really necessary under normal use
            var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (var asset in assets) {
                if (asset is Material material && material != m_Material) {
                    AssetDatabase.RemoveObjectFromAsset(material);
                }
            }
        };
#endif
    }

    private void UpdateMaterialProperties() {
        if (m_Material == null) return;
        ColorMaterialHelper.SetMaterialProperties(m_Material, this);
    }

    private void UpdateMaterialName() {
        if (m_Material == null) return;
        var targetName = $"{name}Material";
        if (m_Material.name == targetName) {
            return;
        }

        m_Material.name = targetName;
#if UNITY_EDITOR
        EditorUtility.SetDirty(m_Material);
        EditorApplication.delayCall += () => {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        };
#endif
    }

#if UNITY_EDITOR
    private void OnValidate() {
        // Prevent self-references
        if (m_Color1.Type == ColorSettingType.Reference && m_Color1.Preset == this) {
            m_Color1.SetInlineColor(Color.white);
        }

        if (m_Color2.Type == ColorSettingType.Reference && m_Color2.Preset == this) {
            m_Color2.SetInlineColor(Color.white);
        }

        if (m_Mode is ColorPresetMode.SolidColor) {
            ClearMaterial();
        } else {
            FindOrCreateMaterial();
            UpdateMaterialProperties();
            UpdateMaterialName();
        }

        RefreshUIElements();
    }

    private void Reset() {
        var assetPath = AssetDatabase.GetAssetPath(this);
        if (string.IsNullOrEmpty(assetPath)) return;

        EditorApplication.delayCall += () => {
            FindOrCreateMaterial();
            UpdateMaterialProperties();
            UpdateMaterialName();
        };
    }

    private void FindMaterial() {
        LoadShaderIfNeeded();

        var assetPath = AssetDatabase.GetAssetPath(this);
        if (string.IsNullOrEmpty(assetPath)) return;

        var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        foreach (var asset in assets) {
            if (asset is Material material && material.shader.name == s_Shader!.name) {
                m_Material = material;
                return;
            }
        }
    }
#endif

    private static void LoadShaderIfNeeded() {
        if (s_Shader == null || s_Shader.name != "TeodorVecerdi/Color") {
            s_Shader = Shader.Find("TeodorVecerdi/Color");
        }
    }
}
