using LitMotion;
using LitMotion.Adapters;

namespace TrafficSimulation.UI;

public sealed partial class UIImage {
    public float Alpha {
        get => AsFadeable().Alpha;
        set => AsFadeable().Alpha = value;
    }

    public MotionHandle AnimateAlpha(float alpha, float duration, Action<MotionBuilder<float, NoOptions, FloatMotionAdapter>>? configure = null) {
        return AsFadeable().AnimateAlpha(alpha, duration, configure);
    }

    public MotionHandle AnimateAlpha(float fromAlpha, float toAlpha, float duration, Action<MotionBuilder<float, NoOptions, FloatMotionAdapter>>? configure = null) {
        return AsFadeable().AnimateAlpha(fromAlpha, toAlpha, duration, configure);
    }

    public void CancelAlphaAnimation() {
        AsFadeable().CancelAlphaAnimation();
    }
}
