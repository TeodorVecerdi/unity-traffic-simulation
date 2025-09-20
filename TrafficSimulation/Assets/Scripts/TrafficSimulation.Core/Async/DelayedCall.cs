using Microsoft.Extensions.Logging;
using Vecerdi.Extensions.Logging;

namespace TrafficSimulation.Core.Async;

public readonly struct DelayedCall() {
    private readonly AsyncHandler m_AsyncHandler = new();
    private readonly ILogger<DelayedCall> m_Logger = UnityLoggerFactory.CreateLogger<DelayedCall>();

    /// <summary>
    /// Gets a value indicating whether the delayed call is currently active (scheduled but not yet executed).
    /// </summary>
    public bool IsActive => m_AsyncHandler.IsActive;

    /// <summary>
    /// Cancels the currently scheduled delayed call if it is active.
    /// </summary>
    public void Cancel() => m_AsyncHandler.Cancel();

    /// <summary>
    /// Reschedules the delayed call to execute the specified action after the given delay.
    /// </summary>
    /// <param name="delay">The time to wait before executing the action.</param>
    /// <param name="action">The action to execute after the delay.</param>
    /// <exception cref="ArgumentNullException">Thrown when action is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when delay is negative.</exception>
    public void Reschedule(TimeSpan delay, Action action) {
        if (action == null) throw new ArgumentNullException(nameof(action));
        if (delay < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(delay), "Delay cannot be negative");
        ScheduleAsync(delay, action).Forget();
    }

    /// <summary>
    /// Reschedules the delayed call to execute the specified async action after the given delay.
    /// </summary>
    /// <param name="delay">The time to wait before executing the action.</param>
    /// <param name="action">The async action to execute after the delay.</param>
    /// <exception cref="ArgumentNullException">Thrown when action is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when delay is negative.</exception>
    public void Reschedule(TimeSpan delay, Func<UniTask> action) {
        if (action == null) throw new ArgumentNullException(nameof(action));
        if (delay < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(delay), "Delay cannot be negative");
        ScheduleAsync(delay, action).Forget();
    }

    /// <summary>
    /// Reschedules the delayed call to execute the specified async action after the given delay with cancellation support.
    /// </summary>
    /// <param name="delay">The time to wait before executing the action.</param>
    /// <param name="cancellationToken">The cancellation token to monitor for cancellation requests.</param>
    /// <param name="action">The async action to execute after the delay that accepts a cancellation token.</param>
    /// <exception cref="ArgumentNullException">Thrown when action is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when delay is negative.</exception>
    public void Reschedule(TimeSpan delay, CancellationToken cancellationToken, Func<CancellationToken, UniTask> action) {
        if (action == null) throw new ArgumentNullException(nameof(action));
        if (delay < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(delay), "Delay cannot be negative");
        ScheduleAsync(delay, action, cancellationToken).Forget();
    }

    /// <summary>
    /// Creates a new delayed call that will execute the specified action after the given delay.
    /// </summary>
    /// <param name="delay">The time to wait before executing the action.</param>
    /// <param name="action">The action to execute after the delay.</param>
    /// <returns>A new DelayedCall instance that can be used to monitor or cancel the scheduled action.</returns>
    /// <exception cref="ArgumentNullException">Thrown when action is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when delay is negative.</exception>
    public static DelayedCall Schedule(TimeSpan delay, Action action) {
        DelayedCall delayedCall = new();
        delayedCall.Reschedule(delay, action);
        return delayedCall;
    }

    /// <summary>
    /// Creates a new delayed call that will execute the specified async action after the given delay.
    /// </summary>
    /// <param name="delay">The time to wait before executing the action.</param>
    /// <param name="action">The async action to execute after the delay.</param>
    /// <returns>A new DelayedCall instance that can be used to monitor or cancel the scheduled action.</returns>
    /// <exception cref="ArgumentNullException">Thrown when action is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when delay is negative.</exception>
    public static DelayedCall Schedule(TimeSpan delay, Func<UniTask> action) {
        DelayedCall delayedCall = new();
        delayedCall.Reschedule(delay, action);
        return delayedCall;
    }

    /// <summary>
    /// Creates a new delayed call that will execute the specified async action after the given delay with cancellation support.
    /// </summary>
    /// <param name="delay">The time to wait before executing the action.</param>
    /// <param name="cancellationToken">The cancellation token to monitor for cancellation requests.</param>
    /// <param name="action">The async action to execute after the delay that accepts a cancellation token.</param>
    /// <returns>A new DelayedCall instance that can be used to monitor or cancel the scheduled action.</returns>
    /// <exception cref="ArgumentNullException">Thrown when action is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when delay is negative.</exception>
    public static DelayedCall Schedule(TimeSpan delay, CancellationToken cancellationToken, Func<CancellationToken, UniTask> action) {
        DelayedCall delayedCall = new();
        delayedCall.Reschedule(delay, cancellationToken, action);
        return delayedCall;
    }

    private async UniTaskVoid ScheduleAsync(TimeSpan delay, Action action) {
        using var scope = m_AsyncHandler.Create();
        try {
            await UniTask.Delay(delay, cancellationToken: scope.Token).SuppressCancellationThrow();
            if (scope.Token.IsCancellationRequested)
                return;
            action();
        } catch (Exception e) {
            m_Logger.LogError(e, "Exception in delayed call");
        }
    }

    private async UniTaskVoid ScheduleAsync(TimeSpan delay, Func<UniTask> action) {
        using var scope = m_AsyncHandler.Create();
        try {
            await UniTask.Delay(delay, cancellationToken: scope.Token).SuppressCancellationThrow();
            if (scope.Token.IsCancellationRequested)
                return;
            await action();
        } catch (Exception e) {
            m_Logger.LogError(e, "Exception in delayed call");
        }
    }

    private async UniTaskVoid ScheduleAsync(TimeSpan delay, Func<CancellationToken, UniTask> action, CancellationToken cancellationToken) {
        using var scope = m_AsyncHandler.Create(cancellationToken);
        try {
            await UniTask.Delay(delay, cancellationToken: scope.Token).SuppressCancellationThrow();
            if (scope.Token.IsCancellationRequested)
                return;
            await action(scope.Token);
        } catch (Exception e) {
            m_Logger.LogError(e, "Exception in delayed call");
        }
    }
}
