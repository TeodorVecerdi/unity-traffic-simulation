using UnityEngine;

namespace TrafficSimulation.UI.Colors;

public sealed class ColorSet : ScriptableObject {
    [SerializeField] private string m_DisplayName = "";
    [SerializeField] private string m_Prefix = "";
    [SerializeField] private int m_DisplayPriority;
    [SerializeField] private List<ColorPreset> m_ColorPresets = [];

    public string DisplayName {
        get => string.IsNullOrEmpty(m_DisplayName) ? name : m_DisplayName;
        set => m_DisplayName = value;
    }

    public string Prefix => m_Prefix;
    public int DisplayPriority => m_DisplayPriority;
    public IReadOnlyList<ColorPreset> ColorPresets => m_ColorPresets;

    public void SetColorPresets(IEnumerable<ColorPreset> presets) {
        m_ColorPresets.Clear();
        m_ColorPresets.AddRange(presets);
    }

    public void AddColorPreset(ColorPreset preset) {
        if (preset != null && !m_ColorPresets.Contains(preset)) {
            m_ColorPresets.Add(preset);
        }
    }

    public void RemoveColorPreset(ColorPreset preset) {
        m_ColorPresets.Remove(preset);
    }

    public void ClearColorPresets() {
        m_ColorPresets.Clear();
    }

    private void OnValidate() {
        // Remove null entries that might occur from deleted assets
        m_ColorPresets.RemoveAll(preset => preset == null);
    }
}
