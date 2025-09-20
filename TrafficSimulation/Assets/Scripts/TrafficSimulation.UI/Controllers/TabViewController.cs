using TrafficSimulation.UI.Selectables;
using Sirenix.OdinInspector;
using UnityEngine;
using Vecerdi.Extensions.DependencyInjection;

namespace TrafficSimulation.UI.Controllers;

public class TabViewController : BaseMonoBehaviour {
    [Title("References")]
    [SerializeField, Required] private SwitchGroup m_SwitchGroup = null!;
    [SerializeField, Required] private List<NavigationEntry> m_Entries = [];

    [Title("Settings")]
    [SerializeField] private bool m_NavigateOnEnable = true;

    public int CurrentTabIndex { get; protected set; } = -1;
    public IReadOnlyList<NavigationEntry> Entries => m_Entries;

    public void SwitchToTab(int index) {
        if (index < 0 || index >= m_Entries.Count) throw new ArgumentOutOfRangeException(nameof(index));
        if (CurrentTabIndex == index) return;
        m_SwitchGroup.Value = index;
    }

    public void SwitchToTabWithoutNotify(int index) {
        if (index < 0 || index >= m_Entries.Count) throw new ArgumentOutOfRangeException(nameof(index));
        if (CurrentTabIndex == index) return;
        m_SwitchGroup.SetValueWithoutNotify(index);
        CurrentTabIndex = index;
    }

    protected override void Awake() {
        base.Awake();
        m_SwitchGroup.SetButtons(m_Entries.Select(entry => entry.Switch));
    }

    protected virtual void OnEnable() {
        CurrentTabIndex = -1;
        m_Entries.ForEach(entry => entry.View.SetActive(false));

        m_SwitchGroup.ValueChanged += OnSwitchValueChanged;
        if (m_NavigateOnEnable) {
            m_SwitchGroup.Value = 0;
        }
    }

    protected virtual void OnDisable() {
        m_SwitchGroup.ValueChanged -= OnSwitchValueChanged;
    }

    protected virtual void OnSwitchValueChanged(int? value) {
        if (value is not { } index || CurrentTabIndex == index) return;
        var from = CurrentTabIndex;
        CurrentTabIndex = index;
        OnTabChanged(from, index);
    }

    protected virtual void OnTabChanged(int fromIndex, int toIndex) {
        // Base implementation does nothing - derived classes can override
        m_Entries.GetValueOrDefault(fromIndex)?.View.SetActive(false);
        m_Entries.GetValueOrDefault(toIndex)?.View.SetActive(true);
    }

    [Serializable]
    public sealed class NavigationEntry {
        [SerializeField, Required] private Switch m_Switch = null!;
        [SerializeField, Required] private GameObject m_View = null!;
        public Switch Switch => m_Switch;
        public GameObject View => m_View;
    }
}
