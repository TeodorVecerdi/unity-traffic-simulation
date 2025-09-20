using TrafficSimulation.Core;
using TrafficSimulation.Core.Events;
using TrafficSimulation.UI.Animations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace TrafficSimulation.UI.Selectables;

public sealed class UIButton : BaseUIBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, ISelectHandler, IDeselectHandler {
    [PropertySpace]
    [ShowInInspector]
    public bool IsDisabled {
        get => m_IsDisabled;
        set => SetDisabled(value, Application.isPlaying && isActiveAndEnabled ? AnimationMode.Animated : AnimationMode.Instant);
    }

    public ISubscribable Clicked {
        get => m_Clicked;
        set { }
    }

    public ISubscribable<bool> HoverStateChanged {
        get => m_HoverStateChanged;
        set { }
    }

    public ISubscribable<bool> PressStateChanged {
        get => m_PressStateChanged;
        set { }
    }

    public ButtonAnimations Animations => m_Animations;
    public bool IsSelectable => m_IsSelectable;

    [SerializeField] private bool m_EnableAnimations = true;
    [Indent, ShowIf(nameof(m_EnableAnimations))]
    [SerializeField, Required] private ButtonAnimations m_Animations = new();

    [SerializeField] private bool m_IsSelectable;
    [Indent, ShowIf(nameof(m_IsSelectable))]
    [SerializeField, Required] private GameObject? m_FocusRing;
    [Indent, ShowIf(nameof(m_IsSelectable))]
    [SerializeField] private bool m_SelectOnClick;
    [Indent, ShowIf("@m_IsSelectable && m_SelectOnClick")]
    [SerializeField] private bool m_ShowFocusRingOnClick = true;

    [Title("Behavior")]
    [SerializeField] private bool m_SetDefaultStateWhenDisabling = true;
    [SerializeField] private bool m_PlayReverseAnimationIfDisabled;
    [SerializeField] private bool m_ClearReverseAnimationWhenDisabling;
    [SerializeField] private bool m_ResetWhenEnabling = true;

    private readonly PriorityEvent m_Clicked = new();
    private readonly PriorityEvent<bool> m_HoverStateChanged = new();
    private readonly PriorityEvent<bool> m_PressStateChanged = new();
    private InputAction m_SubmitAction = null!;

    private bool m_IsHovered;
    private bool m_IsDisabled;
    private bool m_IsSelected;
    private bool m_SuppressFocusRingOnClick;

    protected override void Awake() {
        base.Awake();
        m_FocusRing.OrNull()?.SetActive(false);
        m_SubmitAction = InputSystem.actions.FindAction("UI/Submit", true);
    }

    private void OnEnable() {
        if (m_ResetWhenEnabling) {
            m_IsHovered = false;
            m_Animations.SetState(m_IsDisabled ? ButtonState.Disabled : ButtonState.Default, destroyCancellationToken, AnimationMode.Instant);
            m_Animations.ClearReverseAnimation();
        }
    }

    public void SetDisabled(bool value, AnimationMode animationMode = AnimationMode.Animated) {
        if (m_IsDisabled == value) return;
        m_IsDisabled = value;
        var state = m_IsDisabled ? ButtonState.Disabled : m_IsHovered ? ButtonState.Hovered : ButtonState.Default;

        if (m_EnableAnimations) {
            if (m_IsDisabled && m_SetDefaultStateWhenDisabling)
                m_Animations.SetState(ButtonState.Default, destroyCancellationToken, AnimationMode.Instant);
            if (m_IsDisabled && m_ClearReverseAnimationWhenDisabling)
                m_Animations.ClearReverseAnimation();
            m_Animations.SetState(state, destroyCancellationToken, animationMode);
        }
    }

    public void OnPointerEnter(PointerEventData eventData) {
        m_IsHovered = true;
        m_HoverStateChanged.InvokeEvent(true);

        if (m_IsDisabled) {
            return;
        }

        if (m_EnableAnimations)
            m_Animations.SetState(ButtonState.Hovered, destroyCancellationToken);
    }

    public void OnPointerExit(PointerEventData eventData) {
        m_IsHovered = false;
        m_HoverStateChanged.InvokeEvent(false);

        if (m_IsDisabled) {
            if (m_PlayReverseAnimationIfDisabled && m_EnableAnimations) {
                m_Animations.PlayReverseAnimation(destroyCancellationToken);
            }

            return;
        }

        if (m_EnableAnimations) {
            m_Animations.SetState(m_IsSelected ? ButtonState.Hovered : ButtonState.Default, destroyCancellationToken);
        }
    }

    public void OnPointerDown(PointerEventData eventData) {
        m_PressStateChanged.InvokeEvent(true);

        if (m_IsDisabled) {
            if (m_PlayReverseAnimationIfDisabled && m_EnableAnimations) {
                m_Animations.PlayReverseAnimation(destroyCancellationToken);
            }

            return;
        }

        if (m_EnableAnimations) {
            m_Animations.SetState(ButtonState.Pressed, destroyCancellationToken);
        }
    }

    public void OnPointerUp(PointerEventData eventData) {
        m_PressStateChanged.InvokeEvent(false);

        if (m_IsDisabled) {
            if (m_PlayReverseAnimationIfDisabled && m_EnableAnimations) {
                m_Animations.PlayReverseAnimation(destroyCancellationToken);
            }

            return;
        }

        if (m_EnableAnimations) {
            m_Animations.SetState(m_IsHovered || m_IsSelected ? ButtonState.Hovered : ButtonState.Default, destroyCancellationToken);
        }
    }

    public void OnPointerClick(PointerEventData eventData) {
        if (m_IsDisabled) return;
        m_Clicked.InvokeEvent();

        if (m_IsSelectable && m_SelectOnClick && EventSystem.current.currentSelectedGameObject != gameObject) {
            m_SuppressFocusRingOnClick = !m_ShowFocusRingOnClick;
            EventSystem.current.SetSelectedGameObject(gameObject);
        }
    }

    public void OnSelect(BaseEventData eventData) {
        if (m_IsDisabled || !m_IsSelectable) return;
        m_IsSelected = true;

        if (m_SuppressFocusRingOnClick) {
            m_FocusRing.OrNull()?.SetActive(false);
            m_SuppressFocusRingOnClick = false;
        } else {
            m_FocusRing.OrNull()?.SetActive(true);
        }

        m_SubmitAction.performed += OnSubmit;
        if (m_EnableAnimations) {
            m_Animations.SetState(ButtonState.Hovered, destroyCancellationToken);
        }
    }

    public void OnDeselect(BaseEventData eventData) {
        if (m_IsDisabled || !m_IsSelectable) return;
        m_IsSelected = false;
        m_FocusRing.OrNull()?.SetActive(false);
        m_SubmitAction.performed -= OnSubmit;
        if (m_EnableAnimations) {
            m_Animations.SetState(m_IsHovered ? ButtonState.Hovered : ButtonState.Default, destroyCancellationToken);
        }
    }

    private void OnSubmit(InputAction.CallbackContext context) {
        if (m_IsDisabled) return;
        m_Clicked.InvokeEvent();
    }
}
