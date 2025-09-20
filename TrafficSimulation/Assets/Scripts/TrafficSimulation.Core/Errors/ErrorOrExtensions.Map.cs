namespace TrafficSimulation.Core.Errors;

public static partial class ErrorOrExtensions {
    public static ErrorOr<TResult> Map<T, TResult>(this ErrorOr<T> result, Func<T, TResult> mapFunc) {
        if (result.TryGetError(out var error)) {
            return error;
        }

        return mapFunc(result.Value);
    }

    public static async UniTask<ErrorOr<TResult>> Map<T, TResult>(this UniTask<ErrorOr<T>> resultTask, Func<T, TResult> mapFunc) {
        var result = await resultTask;
        if (result.TryGetError(out var error)) {
            return error;
        }

        return mapFunc(result.Value);
    }

    public static async UniTask<ErrorOr<TResult>> Map<T, TResult>(this UniTask<ErrorOr<T>> resultTask, Func<T, UniTask<TResult>> mapFunc) {
        var result = await resultTask;
        if (result.TryGetError(out var error)) {
            return error;
        }

        return await mapFunc(result.Value);
    }
}
