using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace TrafficSimulation.UI;

public sealed class VirtualScrollItem : MonoBehaviour, IVirtualScrollItem {
    [SerializeField, Required] private GameObject m_VisualContent = null!;
    [SerializeField] private LayoutElement? m_LayoutElement;

    public RectTransform RectTransform => (RectTransform)transform;

    private bool m_IsVisible;

    public void SetVisible(bool isVisible) {
        if (m_IsVisible == isVisible) return;
        m_IsVisible = isVisible;

        // We use a layout element to set the size to the current size in case the object's
        // content size is dynamic/driven by a layout group.
        if (m_LayoutElement != null) {
            m_LayoutElement.enabled = !isVisible;
            if (!isVisible) {
                m_LayoutElement.enabled = true;
                m_LayoutElement.minHeight = RectTransform.rect.height;
                m_LayoutElement.minWidth = RectTransform.rect.width;
                m_LayoutElement.preferredHeight = RectTransform.rect.height;
                m_LayoutElement.preferredWidth = RectTransform.rect.width;
                m_LayoutElement.flexibleHeight = 0;
                m_LayoutElement.flexibleWidth = 0;
            }
        }

        m_VisualContent.SetActive(isVisible);
    }
}
