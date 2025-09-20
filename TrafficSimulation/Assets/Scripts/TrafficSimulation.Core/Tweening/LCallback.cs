using LitMotion;

namespace TrafficSimulation.Core.Tweening;

public static class LCallback {
    public static MotionHandle Create(Action action) {
        return LMotion.Create(0.0f, 0.0f, 0.0f).WithOnComplete(action).RunWithoutBinding();
    }

    public static MotionHandle Create(float delay, Action action) {
        return LMotion.Create(0.0f, 0.0f, delay).WithOnComplete(action).RunWithoutBinding();
    }
}
