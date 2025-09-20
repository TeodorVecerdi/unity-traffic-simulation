using UnityEngine;

namespace TrafficSimulation.Core.PlayMode;

public class PlayModeTransient<T> {
    public T Value { get; set; } = default!;

    private readonly Action<T>? m_ResetAction;
    private readonly Func<T>? m_Initializer;
    private readonly string? m_InstanceDescription;

    public PlayModeTransient(Func<T> initializer, string? instanceDescription = null) {
        m_Initializer = initializer;
        m_InstanceDescription = instanceDescription ?? $"PlayModeTransient<{typeof(T).Name}>";

        try {
            Value = m_Initializer();
        } catch (Exception e) {
            Debug.LogError($"Exception thrown while initializing PlayModeTransient<{typeof(T).Name}>: {e}");
        }

        PlayModeManager.RegisterExitPlayModeAction(m_InstanceDescription, Reset);
    }

    public PlayModeTransient(T initialValue, Action<T> resetAction, string? instanceDescription = null) {
        Value = initialValue;
        m_ResetAction = resetAction;
        m_InstanceDescription = instanceDescription ?? $"PlayModeTransient<{typeof(T).Name}>";

        PlayModeManager.RegisterExitPlayModeAction(m_InstanceDescription, Reset);
    }

    private void Reset() {
        if (Value is IDisposable disposable) {
            try {
                disposable.Dispose();
            } catch (Exception e) {
                Debug.LogError($"Exception thrown while disposing {m_InstanceDescription}: {e}");
            }
        }

        if (m_ResetAction is not null) {
            try {
                m_ResetAction(Value);
            } catch (Exception e) {
                Debug.LogError($"Exception thrown while resetting {m_InstanceDescription}: {e}");
            }
        } else if (m_Initializer is not null) {
            try {
                Value = m_Initializer();
            } catch (Exception e) {
                Debug.LogError($"Exception thrown while resetting {m_InstanceDescription}>: {e}");
            }
        } else {
            Debug.LogWarning($"No reset action or initializer provided for {m_InstanceDescription}.");
        }
    }

    public static implicit operator T(PlayModeTransient<T> transient) => transient.Value;
}
