using UnityEngine;

namespace TrafficSimulation.UI;

public interface IVirtualScrollItem {
    RectTransform RectTransform { get; }
    void SetVisible(bool isVisible);
}
