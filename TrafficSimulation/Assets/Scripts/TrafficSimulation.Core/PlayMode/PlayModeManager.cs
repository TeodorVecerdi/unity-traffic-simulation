#if !UNITY_EDITOR
namespace TrafficSimulation.Core.PlayMode;

public static class PlayModeManager {
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void RegisterExitPlayModeAction(string owner, System.Action action) { }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void RegisterEnterPlayModeAction(string owner, System.Action action) { }
}
#else
using System.Collections.Concurrent;
using UnityEditor;
using UnityEngine;

namespace TrafficSimulation.Core.PlayMode;

public static class PlayModeManager {
    private static readonly ConcurrentDictionary<CleanupAction, byte> s_ExitPlayModeActions = new();
    private static readonly ConcurrentDictionary<CleanupAction, byte> s_EnterPlayModeActions = new();

    /// <summary>
    /// Registers a delegate to be ran when exiting play mode.
    /// </summary>
    /// <param name="owner">The name of the class registering the delegate.</param>
    /// <param name="action">The delegate to be ran.</param>
    public static void RegisterExitPlayModeAction(string owner, Action action) {
        if (!s_ExitPlayModeActions.TryAdd(new CleanupAction(owner, action), 0)) {
            Debug.LogWarning($"{owner} attempted to register the same exit play mode action twice.");
        }
    }

    /// <summary>
    /// Registers a delegate to be ran when exiting play mode.
    /// </summary>
    /// <param name="owner">The name of the class registering the delegate.</param>
    /// <param name="action">The delegate to be ran.</param>
    public static void RegisterEnterPlayModeAction(string owner, Action action) {
        if (!s_EnterPlayModeActions.TryAdd(new CleanupAction(owner, action), 0)) {
            Debug.LogWarning($"{owner} attempted to register the same enter play mode action twice.");
        }
    }

    [InitializeOnLoadMethod]
    private static void Initialize() {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state) {
        if (state is PlayModeStateChange.EnteredEditMode && s_ExitPlayModeActions.Count > 0) {
            RunActions(s_ExitPlayModeActions);
        }

        if (state is PlayModeStateChange.ExitingEditMode && s_EnterPlayModeActions.Count > 0) {
            RunActions(s_EnterPlayModeActions);
        }
    }

    private static void RunActions(ConcurrentDictionary<CleanupAction, byte> actions) {
        List<CleanupException> exceptions = [];
        foreach (var cleanupAction in actions.Keys) {
            RunCleanupAction(cleanupAction, exceptions);
        }

        if (exceptions.Count is not 0) {
            ReportExceptions(exceptions);
        }
    }

#pragma warning disable CS0162 // unreachable code
    private static void RunCleanupAction(CleanupAction cleanupAction, ICollection<CleanupException> exceptions) {
        try {
            cleanupAction.Action();
        } catch (Exception e) {
            exceptions.Add(new CleanupException(cleanupAction.Owner, e));
        }
    }
#pragma warning restore CS0162

    private static void ReportExceptions(List<CleanupException> exceptions) {
        StringBuilder sb = new();
        sb.AppendLine("Cleanup actions failed:");
        foreach (var exception in exceptions) {
            sb.AppendLine($"- [{exception.Owner}] {exception.Exception.Message}");
        }

        Debug.LogError(sb.ToString());
    }

    private readonly struct CleanupAction : IEquatable<CleanupAction> {
        public readonly string Owner;
        public readonly Action Action;

        public CleanupAction(string owner, Action action) {
            Owner = owner;
            Action = action;
        }

        public bool Equals(CleanupAction other) => Owner == other.Owner && Action.Equals(other.Action);
        public override bool Equals(object? obj) => obj is CleanupAction other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Owner, Action);
        public static bool operator ==(CleanupAction left, CleanupAction right) => left.Equals(right);
        public static bool operator !=(CleanupAction left, CleanupAction right) => !left.Equals(right);
    }

    private readonly struct CleanupException {
        public readonly string Owner;
        public readonly Exception Exception;

        public CleanupException(string owner, Exception exception) {
            Owner = owner;
            Exception = exception;
        }
    }
}
#endif
