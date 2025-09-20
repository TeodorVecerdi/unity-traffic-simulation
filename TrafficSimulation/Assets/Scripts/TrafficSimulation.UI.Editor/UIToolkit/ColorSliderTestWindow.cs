using TrafficSimulation.Core.Editor;
using TrafficSimulation.UI.Colors;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TrafficSimulation.UI.Editor.UIToolkit;

public sealed class ColorSliderTestWindow : EditorWindow {
    private static readonly EditorResource<StyleSheet> s_StyleSheet = new("StyleSheets/ColorSliderTestWindow.uss");

    private ColorSetSlider m_ColorSetSlider = null!;
    private Label m_SelectedColorLabel = null!;
    private VisualElement m_PreviewBox = null!;
    private ObjectField m_ColorSetField = null!;

    [MenuItem("Traffic Simulation/Color Slider Test")]
    public static void ShowWindow() {
        var window = GetWindow<ColorSliderTestWindow>();
        window.titleContent = new GUIContent("Color Slider Test");
        window.minSize = new Vector2(400, 200);
    }

    public void CreateGUI() {
        var root = rootVisualElement;

        // Load USS
        var styleSheet = s_StyleSheet.Value;
        if (styleSheet != null) {
            root.styleSheets.Add(styleSheet);
        }

        CreateUI(root);
        LoadDefaultFolder();
    }

    private void CreateUI(VisualElement root) {
        root.AddToClassList("color-slider-window");

        // Title
        var titleLabel = new Label("Color Slider Test");
        titleLabel.AddToClassList("window-title");
        root.Add(titleLabel);

        // Folder input section
        m_ColorSetField = new ObjectField("Color Set");
        m_ColorSetField.objectType = typeof(ColorSet);
        m_ColorSetField.allowSceneObjects = false;
        m_ColorSetField.AddToClassList("color-set-field");
        m_ColorSetField.RegisterValueChangedCallback(OnColorSetChanged);
        root.Add(m_ColorSetField);

        // Color slider
        m_ColorSetSlider = new ColorSetSlider();
        m_ColorSetSlider.ValueChanged += OnColorChanged;
        root.Add(m_ColorSetSlider);

        // Selected color info
        var infoContainer = new VisualElement();
        infoContainer.AddToClassList("info-container");

        m_SelectedColorLabel = new Label("No color selected");
        m_PreviewBox = new VisualElement();
        m_PreviewBox.AddToClassList("color-preview");

        infoContainer.Add(m_SelectedColorLabel);
        infoContainer.Add(m_PreviewBox);
        root.Add(infoContainer);
    }

    private void LoadDefaultFolder() {
        var defaultPath = "Assets/Graphics/Colors/color-sets/amber";
        var assets = AssetDatabase.FindAssets("t:ColorSet", [defaultPath]);
        if (assets.Length > 0) {
            var assetPath = AssetDatabase.GUIDToAssetPath(assets[0]);
            var colorSet = AssetDatabase.LoadAssetAtPath<ColorSet>(assetPath);
            if (colorSet != null) {
                m_ColorSetField.value = colorSet;
            }
        }
    }

    private void OnColorSetChanged(ChangeEvent<Object> evt) {
        m_ColorSetSlider.SetColorSet(evt.newValue as ColorSet);
    }

    private void OnColorChanged(ColorPreset selectedPreset) {
        if (selectedPreset != null) {
            var color = selectedPreset.GetColor1();
            m_SelectedColorLabel.text = $"Selected: {selectedPreset.name}";
            m_PreviewBox.style.backgroundColor = color;
        } else {
            m_SelectedColorLabel.text = "No color selected";
            m_PreviewBox.style.backgroundColor = Color.clear;
        }
    }
}
