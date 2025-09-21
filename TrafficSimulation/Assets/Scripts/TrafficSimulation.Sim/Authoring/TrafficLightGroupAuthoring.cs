using Sirenix.OdinInspector;
using TrafficSimulation.Sim.Components;
using TrafficSimulation.Sim.Math;

namespace TrafficSimulation.Sim.Authoring;

public sealed class TrafficLightGroupAuthoring : MonoBehaviour {
    [Title("Signal Timings (seconds)")]
    [SerializeField, MinValue(0.0f)] private float m_GreenDurationSeconds = 10.0f;
    [SerializeField, MinValue(0.0f)] private float m_AmberDurationSeconds = 2.5f;
    [SerializeField, MinValue(0.0f)] private float m_RedDurationSeconds = 10.0f;
    [SerializeField, MinValue(0.0f)] private float m_StartTimeOffsetSeconds;

    [Title("Behavior")]
    [SerializeField, MinValue(0.0f), Unit(Units.Meter)] private float m_AmberStopBufferMeters = 2.0f;

    [Title("Lane Bindings")]
    [SerializeField] private List<LaneBindingEntry> m_LaneBindings = [];

    [Title("Gizmos")]
    [SerializeField] private bool m_DrawGizmos = true;
    [SerializeField] private float m_GizmoLineHalfWidth = 1.5f;
    [SerializeField] private float m_GizmoSphereSize = 0.2f;

    // Runtime state fed from controller for visualization
    [NonSerialized] private float m_RuntimeTimeInCycleSeconds;

    public TrafficLightGroupParameters Parameters => new(
        m_GreenDurationSeconds,
        m_AmberDurationSeconds,
        m_RedDurationSeconds,
        m_StartTimeOffsetSeconds,
        m_AmberStopBufferMeters
    );

    public IReadOnlyList<LaneBindingEntry> LaneBindings => m_LaneBindings;

    public void SetRuntimeTime(float timeInCycleSeconds) {
        m_RuntimeTimeInCycleSeconds = timeInCycleSeconds;
    }

    private void OnDrawGizmos() {
        if (!m_DrawGizmos)
            return;

        var parameters = Parameters;
        var color = Application.isPlaying
            ? TrafficLightMath.EvaluateColor(m_RuntimeTimeInCycleSeconds, in parameters)
            : TrafficLightColor.Green;

        var gizmoColor = color switch {
            TrafficLightColor.Green => Color.green,
            TrafficLightColor.Amber => new Color(1.0f, 0.65f, 0.0f, 1.0f),
            _ => Color.red,
        };

        Gizmos.color = gizmoColor;

        foreach (var entry in m_LaneBindings) {
            if (entry == null || entry.Lane == null)
                continue;
            var tr = entry.Lane.transform;
            var center = tr.position + tr.forward * entry.StopLinePositionMeters;
            var left = -tr.right * m_GizmoLineHalfWidth;
            var leftStop = -tr.right * (m_GizmoLineHalfWidth - 0.2f);
            var stopBuffer = center - tr.forward * parameters.AmberStopBufferMeters;

            Gizmos.DrawLine(center - left, center + left);
            Gizmos.DrawLine(center, stopBuffer);
            Gizmos.DrawLine(stopBuffer - leftStop, stopBuffer + leftStop);
            Gizmos.DrawSphere(stopBuffer, m_GizmoSphereSize);
        }
    }

    [Serializable]
    public sealed class LaneBindingEntry {
        [SerializeField, Required] public LaneAuthoring Lane = null!;
        [SerializeField, MinValue(0.0f), Unit(Units.Meter)] public float StopLinePositionMeters;
    }
}
