using TrafficSimulation.Core.Editor;
using TrafficSimulation.Core.Events;
using TrafficSimulation.Core.Extensions;
using TrafficSimulation.Core.Notifiable;
using TrafficSimulation.UI.Colors;
using UnityEngine.UIElements;

namespace TrafficSimulation.UI.Editor.UIToolkit;

public sealed class ColorSetSlider : VisualElement, INotifiable<ColorPreset> {
    private static readonly EditorResource<StyleSheet> s_StyleSheet = new("StyleSheets/ColorSetSlider.uss");

    private readonly PriorityEvent<ColorPreset> m_ValueChanged = new();
    private readonly PriorityEvent m_Confirmed = new();
    private readonly List<VisualElement> m_ColorSwatches = [];
    private readonly List<Label> m_SwatchLabels = [];
    private readonly SliderInt m_Slider;
    private VisualElement? m_DragContainer;
    private VisualElement? m_Track;
    private VisualElement? m_Dragger;
    private VisualElement? m_SwatchesContainer;
    private VisualElement? m_LabelsContainer;

    public bool ShowLabels {
        get;
        set {
            if (field == value)
                return;
            field = value;
            ToggleLabels(value);
        }
    }

    public ColorSet? ColorSet { get; private set; }

    public ISubscribable<ColorPreset> ValueChanged {
        get => m_ValueChanged;
        set { }
    }

    public ISubscribable Confirmed {
        get => m_Confirmed;
        set { }
    }

    public ColorPreset Value {
        get => ColorSet?.ColorPresets.GetValueOrDefault(m_Slider.value) ?? throw new InvalidOperationException("ColorSet is null");
        set => m_Slider.value = ColorSet?.ColorPresets.IndexOf(value) ?? throw new InvalidOperationException("ColorSet is null");
    }

    public ColorPreset? ValueOrDefault => ColorSet?.ColorPresets.GetValueOrDefault(m_Slider.value);

    public void SetValueWithoutNotify(ColorPreset value) {
        m_Slider.SetValueWithoutNotify(ColorSet?.ColorPresets.IndexOf(value) ?? throw new InvalidOperationException("ColorSet is null"));
        UpdateDragger();
    }

    public ColorSetSlider() {
        var styleSheet = s_StyleSheet.Value ?? throw new InvalidOperationException("ColorSetSlider StyleSheet is null");
        styleSheets.Add(styleSheet);

        AddToClassList("color-slider");

        // Create slider
        m_Slider = new SliderInt();
        m_Slider.AddToClassList("color-slider-input");
        m_Slider.RegisterValueChangedCallback(OnSliderValueChanged);
        m_Slider.RegisterCallback<ClickEvent>(evt => {
            if (evt.clickCount == 2) {
                m_Confirmed.InvokeEvent();
            }
        });
        Add(m_Slider);

        // We need to wait for the slider to be constructed before we can access its internal elements
        schedule.Execute(FindSliderElements);
    }

    private void FindSliderElements() {
        // Find the internal slider elements by their Unity IDs
        m_Track = m_Slider.Q<VisualElement>("unity-tracker");
        m_Dragger = m_Slider.Q<VisualElement>("unity-dragger");
        m_DragContainer = m_Slider.Q<VisualElement>("unity-drag-container");

        if (m_Track is null || m_Dragger is null || m_DragContainer is null) {
            // Try again next frame if not found yet
            schedule.Execute(FindSliderElements);
            return;
        }

        // Apply custom styling to make the track thicker and the dragger larger
        m_Track.AddToClassList("color-slider-track");
        m_Dragger.AddToClassList("color-slider-dragger");

        // Create the swatches and labels containers
        CreateOverlayContainers();

        // If we already have color presets, create the swatches now
        if (ColorSet is { ColorPresets.Count: > 0 }) {
            RebuildSwatchesAndLabels();
            UpdateDragger();
        }
    }

    private void CreateOverlayContainers() {
        if (m_DragContainer is null) return;

        m_SwatchesContainer = new VisualElement();
        m_SwatchesContainer.AddToClassList("color-swatches-container");
        m_SwatchesContainer.pickingMode = PickingMode.Ignore;

        m_LabelsContainer = new VisualElement();
        m_LabelsContainer.AddToClassList("color-labels-container");
        m_LabelsContainer.pickingMode = PickingMode.Ignore;

        m_DragContainer.Insert(1, m_SwatchesContainer);
        m_DragContainer.Add(m_LabelsContainer);
    }

    public void SetColorSet(ColorSet? colorSet) {
        if (colorSet == ColorSet)
            return;

        ColorSet = colorSet;
        if (ColorSet is null or { ColorPresets.Count: 0 }) {
            m_Slider.style.display = DisplayStyle.None;
            ClearSwatchesAndLabels();
            return;
        }

        // Configure slider
        m_Slider.lowValue = 0;
        m_Slider.highValue = ColorSet.ColorPresets.Count - 1;
        m_Slider.value = 0;
        m_Slider.style.display = DisplayStyle.Flex;

        // Create color swatches if the swatches container is available
        if (m_SwatchesContainer != null) {
            RebuildSwatchesAndLabels();
            UpdateDragger();
        }
    }

    private void RebuildSwatchesAndLabels() {
        if (m_SwatchesContainer is null || m_LabelsContainer is null || ColorSet is null or { ColorPresets.Count: 0 })
            return;

        ClearSwatchesAndLabels();

        for (var i = 0; i < ColorSet.ColorPresets.Count; i++) {
            var preset = ColorSet.ColorPresets[i];

            // Create swatch
            var swatch = CreateColorSwatch(preset, i);
            m_ColorSwatches.Add(swatch);
            m_SwatchesContainer.Add(swatch);

            // Create label
            var label = CreateSwatchLabel(preset, i);
            m_SwatchLabels.Add(label);
            m_LabelsContainer.Add(label);
        }
    }

    private VisualElement CreateColorSwatch(ColorPreset preset, int index) {
        var swatch = new VisualElement();
        swatch.AddToClassList("color-swatch-inline");
        if (index == 0)
            swatch.AddToClassList("color-swatch-first");
        if (index == ColorSet!.ColorPresets.Count - 1)
            swatch.AddToClassList("color-swatch-last");

        if (preset != null) {
            swatch.style.backgroundColor = preset.GetColor1();
            swatch.tooltip = preset.name;
        }

        return swatch;
    }

    private Label CreateSwatchLabel(ColorPreset preset, int index) {
        var label = new Label(GetSuffixFromName(preset.name));
        label.AddToClassList("swatch-label");
        if (index == 0)
            label.AddToClassList("swatch-label-first");
        if (index == ColorSet!.ColorPresets.Count - 1)
            label.AddToClassList("swatch-label-last");

        return label;
    }

    private string GetSuffixFromName(string text) {
        if (string.IsNullOrEmpty(text))
            return "";

        // Use color set prefix if specified
        if (!string.IsNullOrEmpty(ColorSet?.Prefix)) {
            return text.StartsWith(ColorSet!.Prefix) ? text[ColorSet.Prefix.Length..] : text;
        }

        // Find the index of the first hyphen.
        var index = text.IndexOf('-');

        // If no hyphen is found (IndexOf returns -1), return the original name.
        // Otherwise, return the substring starting from the character after the hyphen.
        return index == -1 ? text : text[(index + 1)..];
    }

    private void ToggleLabels(bool show) {
        EnableInClassList("with-labels", show);
        if (m_LabelsContainer != null) {
            m_LabelsContainer.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    private void UpdateDragger() {
        var colorPreset = Value;
        if (m_Dragger == null || colorPreset == null)
            return;

        m_Dragger.style.backgroundColor = colorPreset.GetColor1();
        m_Dragger.tooltip = colorPreset.name;
    }

    private void ClearSwatchesAndLabels() {
        m_ColorSwatches.ForEach(swatch => swatch.RemoveFromHierarchy());
        m_ColorSwatches.Clear();

        m_SwatchLabels.ForEach(label => label.RemoveFromHierarchy());
        m_SwatchLabels.Clear();
    }

    private void OnSliderValueChanged(ChangeEvent<int> evt) {
        UpdateDragger();
        m_ValueChanged.InvokeEvent(Value);
    }
}
