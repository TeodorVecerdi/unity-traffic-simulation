namespace TrafficSimulation.Core.Errors;

public static partial class ErrorOrExtensions {
    public static async UniTask<T> Unwrap<T>(this UniTask<ErrorOr<T>> task) {
        var result = await task;
        if (result.TryGetError(out var error)) {
            throw new InvalidOperationException($"Called Unwrap on a task that contains an error: {error}", error.Exception);
        }

        return result.Value!;
    }

    public static async UniTask Unwrap(this UniTask<ErrorOrVoid> task) {
        var result = await task;
        if (result.TryGetError(out var error)) {
            throw new InvalidOperationException($"Called Unwrap on a task that contains an error: {error}", error.Exception);
        }
    }

    public static async UniTask<ErrorOrVoid> Discard<T>(this UniTask<ErrorOr<T>> task) {
        var result = await task;
        return result.TryGetError(out var error) ? error : ErrorOrVoid.Success;
    }

    public static async UniTask<ErrorOrVoid> WrapExceptions(this UniTask task) {
        try {
            await task;
            return ErrorOrVoid.Success;
        } catch (Exception e) {
            return Error.Unexpected(e.Message, exception: e);
        }
    }

    public static async UniTask<ErrorOrVoid> WrapExceptions(this UniTask<ErrorOrVoid> task) {
        try {
            return await task;
        } catch (Exception e) {
            return Error.Unexpected(e.Message, exception: e);
        }
    }

    public static async UniTask<ErrorOr<T>> WrapExceptions<T>(this UniTask<T> task) {
        try {
            return await task;
        } catch (Exception e) {
            return Error.Unexpected<T>(e.Message, exception: e);
        }
    }
}
