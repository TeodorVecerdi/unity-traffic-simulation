using System.Diagnostics.CodeAnalysis;

namespace TrafficSimulation.Core.Errors;

public readonly struct ErrorOr<T> {
    private readonly T? m_Value;
    private readonly Error? m_Error;

    /// <summary>
    /// Gets the value, if present, or throws an exception.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the ErrorOr has an error value.</exception>
    public T Value => m_Error is null ? m_Value! : throw new InvalidOperationException("ErrorOr has an error value.", m_Error.Exception);

    /// <summary>
    /// Gets the error, if present.
    /// </summary>
    public Error? Error => m_Error;

    /// <summary>
    /// Returns <see langword="true"/> if the ErrorOr has an error value.
    /// </summary>
    public bool IsError => m_Error is not null;

    /// <summary>
    /// Returns <see langword="true"/> if the ErrorOr has a value.
    /// </summary>
    public bool HasValue => !IsError;

    /// <summary>
    /// Constructs a new instance with the given value.
    /// </summary>
    /// <param name="value">The value to construct the ErrorOr with.</param>
    public ErrorOr(T value) {
        m_Value = value;
        m_Error = null;
    }

    /// <summary>
    /// Constructs a new instance with the given error.
    /// </summary>
    /// <param name="error">The error to construct the ErrorOr with.</param>
    public ErrorOr(Error error) {
        m_Value = default;
        m_Error = error;
    }

    /// <summary>
    /// Attempts to retrieve the value contained within the instance.
    /// </summary>
    /// <param name="value">When this method returns <c>true</c>, contains the contained value; otherwise, contains the default value of <c>T</c>.</param>
    /// <returns><c>true</c> if the instance contains a value; otherwise, <c>false</c>.</returns>
    public bool TryGetValue([NotNullWhen(true)] out T? value) {
        value = m_Value;
        return HasValue;
    }

    /// <summary>
    /// Attempts to retrieve the error contained within the instance.
    /// </summary>
    /// <param name="error">When this method returns <c>true</c>, contains the error; otherwise, contains <c>null</c>.</param>
    /// <returns><c>true</c> if the instance contains an error; otherwise, <c>false</c>.</returns>
    public bool TryGetError([NotNullWhen(true)] out Error? error) {
        error = m_Error;
        return IsError;
    }

    /// <summary>
    /// Converts the current instance of <see cref="ErrorOr{T}"/> to an instance of <see cref="ErrorOr{T}"/> where T is nullable.
    /// </summary>
    /// <returns>
    /// A new instance of <see cref="ErrorOr{T}"/> that contains the same value or error as the current instance.
    /// </returns>
    public ErrorOr<T?> ToNullable() => IsError ? new ErrorOr<T?>(Error!) : new ErrorOr<T?>(Value);

    public static implicit operator ErrorOr<T>(T value) => new(value);
    public static implicit operator ErrorOr<T>(Error error) => new(error);
}
