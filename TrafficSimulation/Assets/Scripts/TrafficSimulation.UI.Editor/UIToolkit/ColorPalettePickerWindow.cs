using TrafficSimulation.Core.Editor;
using TrafficSimulation.UI.Colors;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TrafficSimulation.UI.Editor.UIToolkit;

public sealed class ColorPalettePickerWindow : EditorWindow {
    private static readonly EditorResource<StyleSheet> s_StyleSheet = new("StyleSheets/ColorPaletteWindow.uss");

    private readonly List<ColorSet> m_AllColorSets = [];
    private readonly List<ColorSetItem> m_FilteredColorSets = [];
    private readonly Dictionary<ColorSet, ColorSetItem> m_ColorSetItems = new();

    private readonly List<ColorPreset> m_AllIndividualPresets = [];
    private readonly List<IndividualPresetItem> m_FilteredIndividualPresets = [];
    private readonly Dictionary<ColorPreset, IndividualPresetItem> m_IndividualPresetItems = new();

    private TextField m_SearchField = null!;
    private ScrollView m_ColorSetsContainer = null!;
    private ScrollView m_IndividualPresetsContainer = null!;

    private ColorSetItem? m_ActiveColorSetItem;
    private IndividualPresetItem? m_ActiveIndividualPresetItem;

    private List<ColorPreset?> m_OriginalValues = null!;
    private Action<ColorPreset?> m_OnSelectionConfirmed = null!;
    private Action<List<ColorPreset?>> m_OnSelectionCancelled = null!;

    public static ColorPalettePickerWindow ShowPicker(List<ColorPreset?> currentValues, Action<ColorPreset?> onSelectionConfirmed, Action<List<ColorPreset?>> onSelectionCancelled) {
        var window = CreateInstance<ColorPalettePickerWindow>();
        window.titleContent = new GUIContent("Color Palette Picker");
        window.m_OriginalValues = currentValues;
        window.m_OnSelectionConfirmed = onSelectionConfirmed;
        window.m_OnSelectionCancelled = onSelectionCancelled;

        // Position window near mouse cursor
        var mousePosition = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
        window.position = new Rect(mousePosition.x, mousePosition.y, 400, 800);
        window.minSize = new Vector2(300, 500);
        window.maxSize = new Vector2(800, 1000);

        window.ShowUtility();
        window.Focus();

        return window;
    }

    public void CreateGUI() {
        rootVisualElement.AddToClassList("color-palette-window");
        if (s_StyleSheet.Value is not null) {
            rootVisualElement.styleSheets.Add(s_StyleSheet.Value);
        }

        CreateUI();
        LoadColorSets();
        LoadIndividualPresets();
        PreselectCurrentValue();
        RefreshColorSets();
        RefreshIndividualPresets();

        // Set focus to search field
        rootVisualElement.schedule.Execute(() => m_SearchField.Focus()).ExecuteLater(16);
    }

    private void OnEnable() {
        EditorApplication.projectChanged += OnProjectChanged;
    }

    private void OnDisable() {
        EditorApplication.projectChanged -= OnProjectChanged;
    }

    private void OnLostFocus() {
        ConfirmSelection();
    }

    private void OnProjectChanged() {
        LoadColorSets();
        LoadIndividualPresets();
        RefreshColorSets();
        RefreshIndividualPresets();
    }

    private void CreateUI() {
        // Search section
        var searchContainer = new VisualElement();
        searchContainer.AddToClassList("search-container");

        var searchLabel = new Label("Search Colors:");
        searchLabel.AddToClassList("search-label");

        m_SearchField = new TextField();
        m_SearchField.AddToClassList("search-field");
        m_SearchField.RegisterValueChangedCallback(OnSearchChanged);

        searchContainer.Add(searchLabel);
        searchContainer.Add(m_SearchField);
        rootVisualElement.Add(searchContainer);

        m_ColorSetsContainer = new ScrollView();
        m_ColorSetsContainer.AddToClassList("color-sets-container");
        rootVisualElement.Add(m_ColorSetsContainer);

        m_IndividualPresetsContainer = new ScrollView();
        m_IndividualPresetsContainer.AddToClassList("individual-presets-container");
        rootVisualElement.Add(m_IndividualPresetsContainer);

        // Handle global key events
        rootVisualElement.RegisterCallback<KeyDownEvent>(OnKeyDown);
        rootVisualElement.focusable = true;
    }

    private void LoadColorSets() {
        m_AllColorSets.Clear();
        var newColorSets = AssetDatabase.FindAssets("t:ColorSet")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<ColorSet>)
            .Where(colorSet => colorSet != null)
            .ToList();

        m_AllColorSets.AddRange(newColorSets);

        // Sort by display priority, then alphabetically by name
        m_AllColorSets.Sort((a, b) => {
            var priorityComparison = b.DisplayPriority.CompareTo(a.DisplayPriority);
            return priorityComparison != 0
                ? priorityComparison
                : string.CompareOrdinal(a.DisplayName, b.DisplayName);
        });

        // Create or update ColorSetItems, preserving existing state
        var existingItems = new Dictionary<ColorSet, ColorSetItem>(m_ColorSetItems);
        m_ColorSetItems.Clear();

        foreach (var colorSet in m_AllColorSets) {
            if (existingItems.TryGetValue(colorSet, out var existingItem)) {
                // Reuse existing item to preserve state
                m_ColorSetItems[colorSet] = existingItem;
            } else {
                // Create new item for new color set
                m_ColorSetItems[colorSet] = new ColorSetItem(colorSet);
            }
        }
    }

    private void LoadIndividualPresets() {
        m_AllIndividualPresets.Clear();

        // Get all ColorPresets
        var allPresets = AssetDatabase.FindAssets("t:ColorPreset")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<ColorPreset>)
            .Where(preset => preset != null)
            .ToList();

        // Get all presets that are part of color sets
        var presetsInSets = new HashSet<ColorPreset>();
        foreach (var colorSet in m_AllColorSets) {
            foreach (var preset in colorSet.ColorPresets) {
                if (preset != null) {
                    presetsInSets.Add(preset);
                }
            }
        }

        // Filter out presets that are part of sets
        var individualPresets = allPresets.Where(preset => !presetsInSets.Contains(preset)).ToList();

        // Sort alphabetically
        individualPresets.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

        m_AllIndividualPresets.AddRange(individualPresets);

        // Create or update IndividualPresetItems
        var existingItems = new Dictionary<ColorPreset, IndividualPresetItem>(m_IndividualPresetItems);
        m_IndividualPresetItems.Clear();

        foreach (var preset in m_AllIndividualPresets) {
            if (existingItems.TryGetValue(preset, out var existingItem)) {
                m_IndividualPresetItems[preset] = existingItem;
            } else {
                m_IndividualPresetItems[preset] = new IndividualPresetItem(preset);
            }
        }
    }

    private void RefreshColorSets() {
        var searchTerm = m_SearchField.value?.ToLowerInvariant() ?? "";

        m_FilteredColorSets.Clear();
        m_FilteredColorSets.AddRange(m_AllColorSets
            .Where(colorSet => MatchesSearch(colorSet, searchTerm))
            .Select(colorSet => m_ColorSetItems[colorSet]));

        RebuildColorSetsUI();
    }

    private void RefreshIndividualPresets() {
        var searchTerm = m_SearchField.value?.ToLowerInvariant() ?? "";

        m_FilteredIndividualPresets.Clear();
        m_FilteredIndividualPresets.AddRange(m_AllIndividualPresets
            .Where(preset => MatchesPresetSearch(preset, searchTerm))
            .Select(preset => m_IndividualPresetItems[preset]));

        RebuildIndividualPresetsUI();
    }

    private void RebuildColorSetsUI() {
        m_ColorSetsContainer.Clear();
        m_ColorSetsContainer.EnableInClassList("display-none", m_FilteredColorSets.Count == 0);

        foreach (var item in m_FilteredColorSets) {
            var colorSetElement = GetOrCreateColorSetElement(item);
            m_ColorSetsContainer.Add(colorSetElement);
        }
    }

    private void RebuildIndividualPresetsUI() {
        m_IndividualPresetsContainer.Clear();
        m_IndividualPresetsContainer.EnableInClassList("display-none", m_FilteredIndividualPresets.Count == 0);

        var gridContainer = new VisualElement();
        gridContainer.AddToClassList("individual-presets-grid");

        foreach (var item in m_FilteredIndividualPresets) {
            var presetElement = GetOrCreateIndividualPresetElement(item);
            gridContainer.Add(presetElement);
        }

        m_IndividualPresetsContainer.Add(gridContainer);
    }

    private VisualElement GetOrCreateColorSetElement(ColorSetItem item) {
        // If we already have a container, reuse it
        if (item.Container != null) {
            return item.Container;
        }

        // Create new container and slider
        var container = new VisualElement();
        container.AddToClassList("color-set-item");

        var label = new Label(item.ColorSet.DisplayName);
        label.AddToClassList("color-set-label");

        var slider = new ColorSetSlider();
        slider.AddToClassList("color-set-slider");
        slider.ShowLabels = true;
        slider.SetColorSet(item.ColorSet);
        slider.Confirmed += ConfirmSelection;

        // Restore preserved slider value
        if (item.SelectedPreset is not null) {
            slider.SetValueWithoutNotify(item.SelectedPreset);
        }

        slider.ValueChanged += _ => OnColorSliderValueChanged(slider, item);

        // Store references
        item.Slider = slider;
        item.Container = container;

        // Restore active state
        if (m_ActiveColorSetItem == item) {
            container.AddToClassList("active");
        }

        // Make the entire container clickable to set active slider
        container.RegisterCallback<ClickEvent>(_ => SetActiveColorSet(item, true)); // Updated method name
        container.Add(label);
        container.Add(slider);

        return container;
    }

    private VisualElement GetOrCreateIndividualPresetElement(IndividualPresetItem item) {
        if (item.Container != null) {
            return item.Container;
        }

        var container = new VisualElement();
        container.AddToClassList("individual-preset-item");

        var preview = new SmartAssetPreview();
        preview.AddToClassList("individual-preset-preview");
        preview.Asset = item.ColorPreset;

        var label = new Label(item.ColorPreset.name);
        label.AddToClassList("individual-preset-label");

        // Make clickable
        container.RegisterCallback<ClickEvent>(evt => {
            SetActiveIndividualPreset(item, true);
            if (evt.clickCount == 2) {
                ConfirmSelection();
            }
        });

        // Store reference
        item.Container = container;

        // Set active state if this is the selected item
        if (m_ActiveIndividualPresetItem == item) {
            container.AddToClassList("active");
        }

        container.Add(preview);
        container.Add(label);

        return container;
    }

    private void OnSearchChanged(ChangeEvent<string> evt) {
        RefreshColorSets();
        RefreshIndividualPresets();
    }

    private void OnKeyDown(KeyDownEvent evt) {
        switch (evt.keyCode) {
            case KeyCode.Return:
            case KeyCode.KeypadEnter:
                ConfirmSelection();
                evt.StopPropagation();
                break;

            case KeyCode.Escape:
                CancelSelection();
                evt.StopPropagation();
                break;
        }
    }

    private void OnColorSliderValueChanged(ColorSetSlider slider, ColorSetItem item) {
        item.SelectedPreset = slider.Value;
        SetActiveColorSet(item, true);
        m_OnSelectionConfirmed.Invoke(item.SelectedPreset);
    }

    private void PreselectCurrentValue() {
        if (m_OriginalValues.Count != 1 || m_OriginalValues[0] == null) return;

        // First check if it's in a color set
        var originalValue = m_OriginalValues[0]!;
        var containingSet = m_AllColorSets.FirstOrDefault(set => set.ColorPresets.Contains(originalValue));
        if (containingSet is not null && m_ColorSetItems.TryGetValue(containingSet, out var colorSetItem)) {
            SetActiveColorSet(colorSetItem, false);
            colorSetItem.SelectedPreset = originalValue;
            return;
        }

        // Check if it's an individual preset
        if (m_IndividualPresetItems.TryGetValue(originalValue, out var individualItem)) {
            SetActiveIndividualPreset(individualItem, false);
        }
    }

    private void SetActiveColorSet(ColorSetItem colorSetItem, bool notify) {
        if (m_ActiveColorSetItem == colorSetItem) return;

        // Clear any active individual preset
        ClearActiveIndividualPreset();

        // Remove active class from previous color set item
        m_ActiveColorSetItem?.Container?.RemoveFromClassList("active");

        // Set new active item
        m_ActiveColorSetItem = colorSetItem;

        if (m_ActiveColorSetItem != null) {
            m_ActiveColorSetItem.Container?.AddToClassList("active");

            // Update selected color to current slider value
            var selectedPreset = m_ActiveColorSetItem.Slider?.ValueOrDefault;
            if (selectedPreset != null) {
                m_ActiveColorSetItem.SelectedPreset = selectedPreset;
                if (notify) m_OnSelectionConfirmed.Invoke(selectedPreset);
            }
        }
    }

    private void SetActiveIndividualPreset(IndividualPresetItem presetItem, bool notify) {
        if (m_ActiveIndividualPresetItem == presetItem) return;

        // Clear any active color set
        ClearActiveColorSet();

        // Remove active class from previous individual preset
        m_ActiveIndividualPresetItem?.Container?.RemoveFromClassList("active");

        // Set new active item
        m_ActiveIndividualPresetItem = presetItem;

        if (m_ActiveIndividualPresetItem != null) {
            m_ActiveIndividualPresetItem.Container?.AddToClassList("active");
            if (notify) m_OnSelectionConfirmed.Invoke(m_ActiveIndividualPresetItem.ColorPreset);
        }
    }

    private void ClearActiveColorSet() {
        m_ActiveColorSetItem?.Container?.RemoveFromClassList("active");
        m_ActiveColorSetItem = null;
    }

    private void ClearActiveIndividualPreset() {
        m_ActiveIndividualPresetItem?.Container?.RemoveFromClassList("active");
        m_ActiveIndividualPresetItem = null;
    }

    private ColorPreset? GetCurrentSelection() {
        return m_ActiveColorSetItem?.SelectedPreset ?? m_ActiveIndividualPresetItem?.ColorPreset;
    }

    private void ConfirmSelection() {
        var currentSelection = GetCurrentSelection();
        if (currentSelection is null) {
            m_OnSelectionCancelled.Invoke(m_OriginalValues);
        } else {
            m_OnSelectionConfirmed.Invoke(currentSelection);
        }

        Close();
    }

    private void CancelSelection() {
        m_OnSelectionCancelled.Invoke(m_OriginalValues);
        Close();
    }

    private static bool MatchesSearch(ColorSet colorSet, string searchTerm) {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return true;

        // Check ColorSet name and display name
        if (colorSet.DisplayName.ToLowerInvariant().Contains(searchTerm) || colorSet.name.ToLowerInvariant().Contains(searchTerm))
            return true;

        // Check names of ColorPresets within the ColorSet
        return colorSet.ColorPresets.Any(preset => preset != null && preset.name.ToLowerInvariant().Contains(searchTerm));
    }

    private static bool MatchesPresetSearch(ColorPreset preset, string searchTerm) {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return true;

        return preset.name.ToLowerInvariant().Contains(searchTerm);
    }

    private class ColorSetItem(ColorSet colorSet) {
        public ColorSet ColorSet { get; } = colorSet;
        public ColorSetSlider? Slider { get; set; }
        public VisualElement? Container { get; set; }
        public ColorPreset? SelectedPreset { get; set; }
    }

    private class IndividualPresetItem(ColorPreset colorPreset) {
        public ColorPreset ColorPreset { get; } = colorPreset;
        public VisualElement? Container { get; set; }
    }
}
