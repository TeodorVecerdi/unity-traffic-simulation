using System.Diagnostics.CodeAnalysis;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TrafficSimulation.UI;

[RequireComponent(typeof(ScrollRect))]
public sealed class VerticalScrollView : MonoBehaviour, IScrollHandler, IBeginDragHandler, IEndDragHandler {
    [SerializeField] private float m_ScrollAmount = 2000.0f;
    [SerializeField] private float m_ScrollSpeed = 16.0f;
    [Space]
    [SerializeField, Required] private RectTransform m_ContentRect = null!;
    [SerializeField, Required] private RectTransform m_ViewportRect = null!;
    [Space]
    [Tooltip("Amount to add to the calculated position when calling ScrollTo(Transform) when the target is above the viewport.")]
    [SerializeField, LabelText("ScrollTo Extra Offset (Above)")] private float m_ScrollToAboveOffset = 16.0f;
    [Tooltip("Amount to add to the calculated position when calling ScrollTo(Transform) when the target is below the viewport.")]
    [SerializeField, LabelText("ScrollTo Extra Offset (Below)")] private float m_ScrollToBelowOffset = 16.0f;

    private bool m_ScrollingEnabled = true;
    private bool m_IsBeingDragged;
    private float m_LastDragScrollNormalizedPosition;

    public event Action? ScrollPositionChanged;

    [field: AllowNull] public ScrollRect ScrollRect => OrNull(ref field) ??= GetComponent<ScrollRect>();
    public float? TargetScrollPosition { get; set; }

    private void Awake() {
        ScrollRect.verticalNormalizedPosition = 1.0f;
        ScrollRect.onValueChanged.AddListener(HandleScrollRectValueChanged);
        TargetScrollPosition = 0.0f;
    }

    private void OnDestroy() {
        ScrollRect.onValueChanged.RemoveListener(HandleScrollRectValueChanged);
    }

    private void Update() {
        if (m_IsBeingDragged) {
            TargetScrollPosition = null;

            if (Mathf.Abs(m_LastDragScrollNormalizedPosition - ScrollRect.verticalNormalizedPosition) > 0.0001f) {
                m_LastDragScrollNormalizedPosition = ScrollRect.verticalNormalizedPosition;
                ScrollPositionChanged?.Invoke();
            }
        }

        if (!TargetScrollPosition.HasValue) {
            return;
        }

        var scrollableHeight = GetScrollableHeight();
        var currentScrollPosition = GetCurrentScrollPosition();
        var targetScrollPosition = Mathf.Clamp(TargetScrollPosition.Value, 0.0f, scrollableHeight);
        if (Mathf.Abs(currentScrollPosition - targetScrollPosition) < 0.1f) {
            return;
        }

        currentScrollPosition = Mathf.Lerp(currentScrollPosition, targetScrollPosition, Time.deltaTime * m_ScrollSpeed);
        ScrollRect.verticalNormalizedPosition = 1.0f - Mathf.Clamp01(currentScrollPosition / scrollableHeight);
        ScrollPositionChanged?.Invoke();
    }

    public float GetCurrentScrollPosition() {
        return (1.0f - ScrollRect.verticalNormalizedPosition) * GetScrollableHeight();
    }

    public void SetEnabled(bool isEnabled) {
        m_ScrollingEnabled = isEnabled;
    }

    public void OnScroll(PointerEventData eventData) {
        if (!m_ScrollingEnabled) {
            return;
        }

        TargetScrollPosition = Mathf.Clamp((TargetScrollPosition ?? GetCurrentScrollPosition()) - Time.deltaTime * m_ScrollAmount * eventData.scrollDelta.y, 0.0f, GetScrollableHeight());
        ScrollPositionChanged?.Invoke();
    }

    [Button]
    public void ScrollToTop() {
        TargetScrollPosition = 0.0f;
    }

    [Button]
    public void ScrollToTopInstant() {
        TargetScrollPosition = 0.0f;
        ScrollRect.verticalNormalizedPosition = 1.0f;
        ScrollPositionChanged?.Invoke();
    }

    [Button]
    public void ScrollToBottom() {
        TargetScrollPosition = GetScrollableHeight();
    }

    [Button]
    public void ScrollToBottomInstant() {
        TargetScrollPosition = GetScrollableHeight();
        ScrollRect.verticalNormalizedPosition = 0.0f;
        ScrollPositionChanged?.Invoke();
    }

    public void ScrollTo(RectTransform? target) {
        if (target == null || !IsChildOf(target, m_ContentRect)) {
            return;
        }

        // Not visible from the top (above the viewport)
        if (IsAboveViewport(target, m_ScrollToAboveOffset, out var deltaAbove)) {
            TargetScrollPosition = GetCurrentScrollPosition() - deltaAbove - m_ScrollToAboveOffset;
            return;
        }

        // Not visible from the bottom (below the viewport)
        if (IsBelowViewport(target, m_ScrollToBelowOffset, out var deltaBelow)) {
            TargetScrollPosition = GetCurrentScrollPosition() - deltaBelow -
                m_ViewportRect.rect.height + m_ScrollToBelowOffset;
        }
    }

    public void ScrollToInstant(RectTransform? target) {
        ScrollTo(target);

        ScrollRect.verticalNormalizedPosition = 1.0f - Mathf.Clamp01((TargetScrollPosition ?? GetCurrentScrollPosition()) / GetScrollableHeight());
    }

    public float GetTopOfObject(RectTransform? target) {
        if (target == null) {
            return 0.0f;
        }

        return m_ViewportRect.InverseTransformPoint(target.parent.TransformPoint(target.localPosition)).y;
    }

    public float GetBottomOfObject(RectTransform? target) {
        if (target == null) {
            return 0.0f;
        }

        return m_ViewportRect.InverseTransformPoint(target.parent.TransformPoint(target.localPosition - (Vector3)target.rect.size)).y;
    }

    public bool IsAboveViewport(RectTransform? target, float extraOffset, out float delta) {
        var topOfObject = GetTopOfObject(target);
        delta = topOfObject;
        return topOfObject + extraOffset > 0.0f;
    }

    public bool IsBelowViewport(RectTransform? target, float extraOffset, out float delta) {
        var bottomOfObject = GetBottomOfObject(target);
        var viewportHeight = m_ViewportRect.rect.height;
        delta = bottomOfObject;
        return bottomOfObject < -viewportHeight + extraOffset;
    }

    public float GetScrollableHeight() {
        return Mathf.Max(0.0001f, m_ContentRect.rect.height - m_ViewportRect.rect.height);
    }

    private static bool IsChildOf(Transform? target, Transform? parent) {
        if (target == null || (parent == null && target.parent != null)) {
            return false;
        }

        if (target.parent == parent) {
            return true;
        }

        while (target != null) {
            if (target.parent == parent) {
                return true;
            }

            target = target.parent;
        }

        return false;
    }

    public void OnBeginDrag(PointerEventData eventData) {
        m_IsBeingDragged = true;
        m_LastDragScrollNormalizedPosition = ScrollRect.verticalNormalizedPosition;
        ScrollPositionChanged?.Invoke();
    }

    public void OnEndDrag(PointerEventData eventData) {
        m_IsBeingDragged = false;
    }

    private void HandleScrollRectValueChanged(Vector2 _) {
        ScrollPositionChanged?.Invoke();
    }
}
