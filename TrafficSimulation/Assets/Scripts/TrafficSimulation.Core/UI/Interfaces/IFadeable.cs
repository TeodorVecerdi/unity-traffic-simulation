using LitMotion;
using LitMotion.Adapters;

namespace TrafficSimulation.Core.UI.Interfaces;

public interface IFadeable {
    public bool IsValid { get; }
    public float Alpha { get; set; }
    public bool SupportsRaycasting { get; }
    public bool ManageRaycasting { get; set; }
    public bool RaycastTarget { get; set; }

    public MotionHandle AnimateAlpha(float alpha, float duration, Action<MotionBuilder<float, NoOptions, FloatMotionAdapter>>? configure = null);
    public MotionHandle AnimateAlpha(float fromAlpha, float toAlpha, float duration, Action<MotionBuilder<float, NoOptions, FloatMotionAdapter>>? configure = null);
    public void CancelAlphaAnimation();
}
