using TrafficSimulation.Core;
using TrafficSimulation.Core.Events;
using TrafficSimulation.Core.Notifiable;
using TrafficSimulation.UI.Animations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TrafficSimulation.UI.Selectables;

public sealed class Switch : BaseUIBehaviour, IReadOnlyNotifiable<bool>, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler {
    [PropertySpace]
    [ShowInInspector]
    public bool IsDisabled {
        get => m_IsDisabled;
        set => SetDisabled(value, Application.isPlaying && isActiveAndEnabled ? AnimationMode.Animated : AnimationMode.Instant);
    }

    [ShowInInspector]
    public bool IsSelected {
        get => m_IsSelected;
        set => SetSelected(value, Application.isPlaying && isActiveAndEnabled ? AnimationMode.Animated : AnimationMode.Instant);
    }

    public ISubscribable<bool> ValueChanged {
        get => m_ValueChanged;
        set { }
    }

    public ISubscribable<bool> HoverStateChanged {
        get => m_HoverStateChanged;
        set { }
    }

    public bool Value => IsSelected;

    public SwitchAnimations Animations => m_Animations;

    [SerializeField, Required] private SwitchAnimations m_Animations = new();
    [SerializeField] private bool m_AllowDeselect = true;

    [Title("Behavior")]
    [SerializeField] private bool m_ClearReverseAnimationWhenDisabling = true;

    private readonly PriorityEvent<bool> m_ValueChanged = new();
    private readonly PriorityEvent<bool> m_HoverStateChanged = new();

    private bool m_IsSelected;
    private bool m_IsHovered;
    private bool m_IsDisabled;
    private bool m_IsHoverAnimationActive;

    public void Initialize(bool value) {
        // Set the value to the opposite to bypass the equality check in case the value is the same
        m_IsSelected = !value;
        SetValueWithoutNotify(value, AnimationMode.Instant);
        m_Animations.ClearReverseAnimation();
    }

    public void SetValueWithoutNotify(bool value, AnimationMode animationMode = AnimationMode.Animated) {
        if (m_IsSelected == value) return;
        m_IsSelected = value;

        var state = value ? SwitchState.Selected : SwitchState.Deselected;
        m_Animations.SetState(state, m_IsHovered && (!value || m_AllowDeselect), destroyCancellationToken, animationMode);

        if (!value && m_IsHovered) {
            m_IsHoverAnimationActive = true;
        }
    }

    public void OnPointerEnter(PointerEventData eventData) {
        m_IsHovered = true;
        m_HoverStateChanged.InvokeEvent(true);

        if (m_IsDisabled) {
            return;
        }

        if (m_IsSelected && !m_AllowDeselect) {
            return;
        }

        var state = m_IsSelected ? SwitchState.HoveredWhileSelected : SwitchState.HoveredWhileDeselected;
        m_Animations.SetState(state, false, destroyCancellationToken);
        m_IsHoverAnimationActive = true;
    }

    public void OnPointerExit(PointerEventData eventData) {
        m_IsHovered = false;
        m_HoverStateChanged.InvokeEvent(false);

        if (m_IsDisabled || !m_IsHoverAnimationActive) return;

        m_IsHoverAnimationActive = false;
        m_Animations.SetState(m_IsSelected ? SwitchState.Selected : SwitchState.Deselected, false, destroyCancellationToken);
    }

    public void OnPointerClick(PointerEventData eventData) {
        if (m_IsDisabled) return;
        if (m_IsSelected && !m_AllowDeselect) return;
        SetSelected(!m_IsSelected);
    }

    private void SetSelected(bool value, AnimationMode animationMode = AnimationMode.Animated) {
        if (m_IsSelected == value) return;
        SetValueWithoutNotify(value, animationMode);
        m_ValueChanged.InvokeEvent(value);
    }

    public void SetDisabled(bool value, AnimationMode animationMode = AnimationMode.Animated) {
        if (m_IsDisabled == value) return;
        m_IsDisabled = value;

        SwitchState state;
        if (m_IsDisabled) {
            state = SwitchState.Disabled;
        } else if (m_IsSelected) {
            state = SwitchState.Selected;
        } else {
            state = SwitchState.Deselected;
        }

        var setHover = !m_IsDisabled && m_IsHovered && (!m_IsSelected || m_AllowDeselect);
        m_Animations.SetState(state, setHover, destroyCancellationToken, animationMode);

        if (m_ClearReverseAnimationWhenDisabling) {
            m_Animations.ClearReverseAnimation();
        }

        if (setHover) {
            m_IsHoverAnimationActive = true;
        }
    }
}
