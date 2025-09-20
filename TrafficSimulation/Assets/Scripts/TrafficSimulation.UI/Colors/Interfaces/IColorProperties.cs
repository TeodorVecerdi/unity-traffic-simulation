using UnityEngine;

namespace TrafficSimulation.UI.Colors;

public interface IColorProperties {
    ColorPresetMode GetMode();
    Color GetColor1();
    Color GetColor2();
    float GetAngle();
    Vector2 GetStops();
    Vector2 GetCenter();
    Vector2 GetRadius();
}
