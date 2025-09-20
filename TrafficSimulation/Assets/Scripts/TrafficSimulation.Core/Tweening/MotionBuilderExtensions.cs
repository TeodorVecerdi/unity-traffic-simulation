using LitMotion;

namespace TrafficSimulation.Core.Tweening;

public static class MotionBuilderExtensions {
    public static MotionBuilder<TValue, TOptions, TAdapter> WithDefaults<TValue, TOptions, TAdapter>(this MotionBuilder<TValue, TOptions, TAdapter> builder) where TValue : unmanaged where TOptions : unmanaged, IMotionOptions where TAdapter : unmanaged, IMotionAdapter<TValue, TOptions> {
        builder.WithEase(Ease.OutQuad);
        builder.WithCancelOnError();
        return builder;
    }
}
