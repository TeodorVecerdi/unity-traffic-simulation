namespace TrafficSimulation.Core.Extensions;

public static class AsyncExtensions {
    public static UniTask<T> AsNonNull<T>(this UniTask<T?> task) where T : notnull => task!;
}
