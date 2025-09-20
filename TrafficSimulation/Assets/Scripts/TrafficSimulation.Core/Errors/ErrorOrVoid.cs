using System.Diagnostics.CodeAnalysis;

namespace TrafficSimulation.Core.Errors;

public readonly struct ErrorOrVoid {
    private readonly Error? m_Error;

    /// <summary>
    /// Gets the error, if present.
    /// </summary>
    public Error? Error => m_Error;

    /// <summary>
    /// Returns <see langword="true"/> if the ErrorOrVoid has an error value.
    /// </summary>
    public bool IsError => m_Error is not null;

    /// <summary>
    /// Constructs a new instance with the given error.
    /// </summary>
    /// <param name="error">The error to construct the ErrorOrVoid with.</param>
    public ErrorOrVoid(Error? error) {
        m_Error = error;
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
    /// Represents a successful operation.
    /// </summary>
    public static ErrorOrVoid Success { get; } = new();

    public static implicit operator ErrorOrVoid(Error error) => new(error);
}
