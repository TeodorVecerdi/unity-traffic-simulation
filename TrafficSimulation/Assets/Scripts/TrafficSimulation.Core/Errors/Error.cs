using System.Globalization;
using System.Net;
using System.Runtime.CompilerServices;

namespace TrafficSimulation.Core.Errors;

public record Error(
    string Description,
    ErrorLevel Level,
    int Code,
    DateTimeOffset Timestamp,
    Exception? Exception = null
) {
    protected virtual bool PrintMembers(StringBuilder builder) {
        RuntimeHelpers.EnsureSufficientExecutionStack();
        builder.Append("Description = ");
        builder.Append(Description);
        builder.Append(", Level = ");
        builder.Append(Level.ToString());
        builder.Append(", Code = ");
        builder.Append(Code.ToString());
        builder.Append(", Timestamp = ");
        builder.Append(Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));
        return true;
    }

    public ErrorOrVoid OrVoid() => new(this);
    public ErrorOr<T> Or<T>() => new(this);

    public static Error Cancelled(string description, ErrorLevel level = ErrorLevel.Error, Exception? exception = null)
        => new(description, level, CommonErrorCodes.Cancelled, DateTimeOffset.UtcNow, exception);

    public static ErrorOr<T> Cancelled<T>(string description, ErrorLevel level = ErrorLevel.Error, Exception? exception = null)
        => Cancelled(description, level, exception);

    public static Error HttpError(HttpStatusCode statusCode, string? description, ErrorLevel level = ErrorLevel.Error, Exception? exception = null) {
        description ??= statusCode.ToString();
        return new Error(description, level, (int)statusCode, DateTimeOffset.UtcNow, exception);
    }

    public static ErrorOr<T> HttpError<T>(HttpStatusCode statusCode, string? description, ErrorLevel level = ErrorLevel.Error, Exception? exception = null)
        => HttpError(statusCode, description, level, exception);

    public static Error Unexpected(string description, ErrorLevel level = ErrorLevel.Unexpected, Exception? exception = null)
        => new(description, level, CommonErrorCodes.Unexpected, DateTimeOffset.UtcNow, exception);

    public static ErrorOr<T> Unexpected<T>(string description, ErrorLevel level = ErrorLevel.Unexpected, Exception? exception = null)
        => Unexpected(description, level, exception);

    public static Error Timeout(string description, ErrorLevel level = ErrorLevel.Error, Exception? exception = null)
        => new(description, level, CommonErrorCodes.Timeout, DateTimeOffset.UtcNow, exception);

    public static ErrorOr<T> Timeout<T>(string description, ErrorLevel level = ErrorLevel.Error, Exception? exception = null)
        => Timeout(description, level, exception);

    public static Error InvalidArgument(string description, ErrorLevel level = ErrorLevel.Error, Exception? exception = null)
        => new(description, level, CommonErrorCodes.InvalidArgument, DateTimeOffset.UtcNow, exception);

    public static ErrorOr<T> InvalidArgument<T>(string description, ErrorLevel level = ErrorLevel.Error, Exception? exception = null)
        => InvalidArgument(description, level, exception);

    public static Error InvalidOperation(string description, ErrorLevel level = ErrorLevel.Error, Exception? exception = null)
        => new(description, level, CommonErrorCodes.InvalidOperation, DateTimeOffset.UtcNow, exception);

    public static ErrorOr<T> InvalidOperation<T>(string description, ErrorLevel level = ErrorLevel.Error, Exception? exception = null)
        => InvalidOperation(description, level, exception);

    public static Error Failure(string description, ErrorLevel level = ErrorLevel.Error, Exception? exception = null)
        => new(description, level, CommonErrorCodes.Failure, DateTimeOffset.UtcNow, exception);

    public static ErrorOr<T> Failure<T>(string description, ErrorLevel level = ErrorLevel.Error, Exception? exception = null)
        => Failure(description, level, exception);

    public static Error NotFound(string description, ErrorLevel level = ErrorLevel.Error, Exception? exception = null)
        => new(description, level, CommonErrorCodes.NotFound, DateTimeOffset.UtcNow, exception);

    public static ErrorOr<T> NotFound<T>(string description, ErrorLevel level = ErrorLevel.Error, Exception? exception = null)
        => NotFound(description, level, exception);
}
