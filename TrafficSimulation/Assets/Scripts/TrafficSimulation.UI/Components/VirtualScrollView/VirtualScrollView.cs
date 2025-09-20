using TrafficSimulation.UI;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Vecerdi.Extensions.DependencyInjection;

namespace TrafficSimulation.UI;

public abstract class VirtualScrollView<TData, TView> : BaseMonoBehaviour where TView : MonoBehaviour, IVirtualScrollItem {
    [Title("References")]
    [SerializeField, Required] private ScrollRect m_ScrollRect = null!;
    [SerializeField, Required] private RectTransform m_ContentContainer = null!;
    [Space]
    [SerializeField] private float m_ExtraOffset = 64.0f;

    public IList<TData>? Items {
        get => m_Items;
        set {
            m_Items = value;
            if (m_Items != null) {
                UpdateItems();
            } else {
                ClearItems();
            }
        }
    }

    private readonly List<TView> m_Views = [];
    private readonly List<ItemContentBounds> m_ItemContentBounds = [];
    private IList<TData>? m_Items;

    private int m_LastVisibleStartIndex = -1;
    private int m_LastVisibleEndIndex = -1;
    private bool m_ContentBoundsCacheValid;

    private void OnEnable() {
        m_ScrollRect.onValueChanged.AddListener(OnScrollPositionChanged);
    }

    private void OnDisable() {
        m_ScrollRect.onValueChanged.RemoveListener(OnScrollPositionChanged);
    }

    protected abstract TView CreateInstance();
    protected abstract void InitializeInstance(TView instance, TData data, int index);

    [Button("Refresh Content Bounds Cache"), DisableInEditorMode]
    public void RefreshContentBoundsCache() {
        InvalidateContentBoundsCache();
        CullItems();
    }

    public void UpdateItems() {
        if (m_Items == null) {
            ClearItems();
            return;
        }

        for (var i = 0; i < m_Items.Count; i++) {
            var viewInstance = CreateOrReuseView(i);
            InitializeInstance(viewInstance, m_Items[i], i);
        }

        // Remove excess items
        for (var i = m_Items.Count; i < m_Views.Count; i++) {
            Destroy(m_Views[i].gameObject);
        }

        m_Views.RemoveRange(m_Items.Count, m_Views.Count - m_Items.Count);

        LayoutRebuilder.ForceRebuildLayoutImmediate(m_ContentContainer);
        RefreshContentBoundsCache();
    }

    private void ClearItems() {
        m_Views.ForEach(view => Destroy(view.gameObject));
        m_Views.Clear();
    }

    private TView CreateOrReuseView(int index) {
        if (index < m_Views.Count) {
            return m_Views[index];
        }

        m_Views.Add(CreateInstance());
        return m_Views[index];
    }

    private void OnScrollPositionChanged(Vector2 position) {
        CullItems();
    }

    private void InvalidateContentBoundsCache() {
        m_ContentBoundsCacheValid = false;
    }

    private void UpdateContentBoundsCache() {
        if (m_ContentBoundsCacheValid) return;

        m_ItemContentBounds.Clear();
        m_ItemContentBounds.Capacity = m_Views.Count;

        foreach (var item in m_Views) {
            var rectTransform = item.RectTransform;
            var localPos = rectTransform.localPosition;
            var rect = rectTransform.rect;

            // Calculate content-relative positions (these don't change when scrolling)
            var contentTop = -localPos.y - rect.height * (1.0f - rectTransform.pivot.y);
            var contentBottom = -localPos.y + rect.height * rectTransform.pivot.y;

            m_ItemContentBounds.Add(new ItemContentBounds {
                ContentTop = contentTop,
                ContentBottom = contentBottom,
            });
        }

        m_ContentBoundsCacheValid = true;
    }

    private void CullItems() {
        if (m_Views.Count == 0)
            return;

        UpdateContentBoundsCache();

        var viewportHeight = m_ScrollRect.viewport.rect.height;
        var contentHeight = m_ContentContainer.rect.height;
        var scrollPosition = (1.0f - m_ScrollRect.verticalNormalizedPosition) * (contentHeight - viewportHeight);

        var visibleRange = FindVisibleItemRange(scrollPosition, viewportHeight);

        // Only update items if the visible range has changed significantly
        if (HasVisibleRangeChanged(visibleRange.Start, visibleRange.End)) {
            ApplyVisibilityToRange(visibleRange.Start, visibleRange.End);
            m_LastVisibleStartIndex = visibleRange.Start;
            m_LastVisibleEndIndex = visibleRange.End;
        }
    }

    private bool HasVisibleRangeChanged(int newStart, int newEnd) {
        return newStart != m_LastVisibleStartIndex || newEnd != m_LastVisibleEndIndex;
    }

    private (int Start, int End) FindVisibleItemRange(float scrollPosition, float viewportHeight) {
        var start = FindFirstVisibleItem(scrollPosition);
        var end = FindLastVisibleItem(scrollPosition + viewportHeight, start);
        return (start, end);
    }

    private int FindFirstVisibleItem(float viewportTop) {
        int left = 0, right = m_ItemContentBounds.Count - 1;
        var result = m_ItemContentBounds.Count;

        while (left <= right) {
            var mid = (left + right) / 2;
            var bounds = m_ItemContentBounds[mid];

            if (bounds.ContentBottom >= viewportTop - m_ExtraOffset) {
                result = mid;
                right = mid - 1;
            } else {
                left = mid + 1;
            }
        }

        return result;
    }

    private int FindLastVisibleItem(float viewportBottom, int startIndex) {
        var result = startIndex + 1;
        while (result < m_ItemContentBounds.Count - 1) {
            var nextBounds = m_ItemContentBounds[result + 1];
            if (nextBounds.ContentTop <= viewportBottom + m_ExtraOffset) {
                result++;
            } else {
                break;
            }
        }

        return result;
    }

    private void ApplyVisibilityToRange(int visibleStart, int visibleEnd) {
        for (var i = 0; i < m_Views.Count; i++) {
            m_Views[i].SetVisible(i >= visibleStart && i <= visibleEnd);
        }
    }

    private struct ItemContentBounds {
        public float ContentTop;
        public float ContentBottom;
    }
}
