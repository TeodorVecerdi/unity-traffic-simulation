using TrafficSimulation.Core.Events;
using TrafficSimulation.Core.Notifiable;
using TrafficSimulation.UI.Animations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TrafficSimulation.UI.Selectables;

public sealed class SwitchGroup : MonoBehaviour, INotifiable<int?> {
    public ISubscribable<int?> ValueChanged {
        get => m_SelectedOption.ValueChanged;
        set { }
    }

    public int? Value {
        get => m_SelectedOption.Value;
        set {
            if (m_SelectedOption.Value is { } previousValue && previousValue == value) {
                return;
            }

            SetValueWithoutNotify(value, isActiveAndEnabled ? AnimationMode.Animated : AnimationMode.Instant);
            m_SelectedOption.SetValueAndForceNotify(value);
        }
    }

    public Switch? SelectedSwitch => m_SelectedOption.Value.HasValue ? m_Buttons[m_SelectedOption.Value.Value] : null;

    [SerializeField, Required] private List<Switch> m_Buttons = null!;
    private readonly Notifiable<int?> m_SelectedOption = new(null);

    private void OnEnable() {
        foreach (var button in m_Buttons) {
            if (button != null) {
                button.ValueChanged += OnSelectableValueChanged;
            }
        }
    }

    private void OnDisable() {
        foreach (var button in m_Buttons) {
            if (button != null) {
                button.ValueChanged -= OnSelectableValueChanged;
            }
        }
    }

    public void SetButtons(IEnumerable<Switch> buttons, bool notifyValueChange = true) {
        foreach (var button in m_Buttons) {
            if (button != null) {
                button.ValueChanged -= OnSelectableValueChanged;
            }
        }

        m_Buttons.Clear();
        m_Buttons.AddRange(buttons);
        foreach (var button in m_Buttons) {
            button.ValueChanged += OnSelectableValueChanged;
        }

        if (notifyValueChange) {
            m_SelectedOption.Value = null;
        } else {
            m_SelectedOption.SetValueWithoutNotify(null);
        }
    }

    public void SetValueWithoutNotify(int? value, AnimationMode animationMode = AnimationMode.Animated) {
        if (m_SelectedOption.Value is { } previousValue && previousValue != value) {
            m_Buttons[previousValue].SetValueWithoutNotify(false, animationMode);
            m_Buttons[previousValue].Animations.ClearReverseAnimation();
        }

        if (value.HasValue) {
            m_Buttons[value.Value].SetValueWithoutNotify(true, animationMode);
            m_Buttons[value.Value].Animations.ClearReverseAnimation();
        }

        m_SelectedOption.SetValueWithoutNotify(value);
    }

    private void OnSelectableValueChanged(bool newValue) {
        if (!newValue) {
            // Reset the selected index if there are no selected buttons.
            if (!m_Buttons.Any(button => button.IsSelected)) {
                m_SelectedOption.Value = null;
            }

            return;
        }

        // Find the newly selected button (it's index is not m_SelectedIndex).
        var selectedIndex = -1;
        var currentSelectedIndex = m_SelectedOption.Value;
        for (var i = 0; i < m_Buttons.Count; i++) {
            if (currentSelectedIndex != i && m_Buttons[i].IsSelected) {
                selectedIndex = i;
                break;
            }
        }

        OnSelectionChanged(selectedIndex);
    }

    private void OnSelectionChanged(int selectedIndex) {
        if (selectedIndex == -1 || m_SelectedOption.Value == selectedIndex) {
            return;
        }

        SetValueWithoutNotify(selectedIndex, isActiveAndEnabled ? AnimationMode.Animated : AnimationMode.Instant);
        m_SelectedOption.SetValueAndForceNotify(selectedIndex);
    }

    void INotifiable<int?>.SetValueWithoutNotify(int? value) => SetValueWithoutNotify(value);
}
