namespace TrafficSimulation.Core.Errors;

public static partial class ErrorOrExtensions {
    public static ErrorOr<TBase> Upcast<T, TBase>(this ErrorOr<T> result) where T : TBase {
        if (result.TryGetError(out var error)) {
            return error;
        }

        return result.Value;
    }

    public static T Unwrap<T>(this ErrorOr<T> result) {
        if (result.TryGetError(out var error)) {
            throw new InvalidOperationException("Called Unwrap on a task that contains an error", error.Exception);
        }

        return result.Value!;
    }

    public static void Unwrap(this ErrorOrVoid result) {
        if (result.TryGetError(out var error)) {
            throw new InvalidOperationException("Called Unwrap on a task that contains an error", error.Exception);
        }
    }

    public static ErrorOrVoid Discard<T>(this ErrorOr<T> result) {
        return result.TryGetError(out var error) ? error : ErrorOrVoid.Success;
    }
}
