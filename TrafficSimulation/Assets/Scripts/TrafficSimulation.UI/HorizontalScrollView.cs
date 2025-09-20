using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Vecerdi.Extensions.DependencyInjection;

namespace TrafficSimulation.UI;

public sealed class HorizontalScrollView : BaseMonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler {
    [SerializeField] private bool m_RequireShift;
    [SerializeField, Required] private ScrollRect m_ScrollRect = null!;

    private InputAction m_ShiftAction = null!;

    protected override void Awake() {
        base.Awake();
        m_ShiftAction = InputSystem.actions.FindAction("UI/Shift", true);
    }

    private void OnShiftActionStarted(InputAction.CallbackContext ctx) {
        if (!m_RequireShift) return;
        m_ScrollRect.enabled = true;
    }

    private void OnShiftActionCanceled(InputAction.CallbackContext ctx) {
        if (!m_RequireShift) return;
        m_ScrollRect.enabled = false;
    }

    public void OnPointerEnter(PointerEventData eventData) {
        if (!m_RequireShift) return;
        m_ShiftAction.started += OnShiftActionStarted;
        m_ShiftAction.canceled += OnShiftActionCanceled;

        if (!m_ShiftAction.IsPressed()) {
            m_ScrollRect.enabled = false;
        }
    }

    public void OnPointerExit(PointerEventData eventData) {
        m_ShiftAction.started -= OnShiftActionStarted;
        m_ShiftAction.canceled -= OnShiftActionCanceled;
        m_ScrollRect.enabled = true;
    }

    public void OnPointerDown(PointerEventData eventData) {
        if (!m_RequireShift) return;
        m_ScrollRect.enabled = true;
    }
    public void OnPointerUp(PointerEventData eventData) {
        if (!m_RequireShift) return;
        m_ScrollRect.enabled = m_ShiftAction.IsPressed();
    }
}
