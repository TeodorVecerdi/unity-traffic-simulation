using Sirenix.OdinInspector;
using TrafficSimulation.Roads.Splines;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace TrafficSimulation.Roads.Prototyping;

public sealed class SplineSamplingTests : MonoBehaviour {
    [SerializeField, Required] private SplineContainer m_SplineContainer = null!;
    [SerializeField, OnValueChanged(nameof(UpdateSamples)), MinValue(0.005f)] private float m_MaxError = 0.05f;
    [SerializeField, OnValueChanged(nameof(UpdateSamples)), MinValue(0.01f)] private float m_MaxStep = 2.0f;
    [Space]
    [SerializeField] private float m_GizmoSize = 0.1f;
    [SerializeField] private Color m_GizmoColor = Color.yellow;

    private List<float>? m_Samples;

    private void OnDrawGizmos() {
        if (m_SplineContainer == null) return;

        // Update samples
        m_Samples ??= [];
        if (m_Samples.Count == 0) {
            SplineSamplingUtility.AdaptiveSample(m_SplineContainer.Spline, m_Samples, math.max(0.005f, m_MaxError), math.max(0.01f, m_MaxStep));
        }

        // Draw spline
        var spline = m_SplineContainer.Spline;
        var positionOffset = (float3)m_SplineContainer.transform.position;
        Gizmos.color = m_GizmoColor;
        foreach (var t in m_Samples) {
            var pos = spline.EvaluatePosition(t) + positionOffset;
            Gizmos.DrawSphere(pos, m_GizmoSize);
        }
    }

    private void UpdateSamples() {
        if (m_SplineContainer == null)
            return;
        m_Samples ??= [];
        SplineSamplingUtility.AdaptiveSample(m_SplineContainer.Spline, m_Samples, math.max(0.005f, m_MaxError), math.max(0.01f, m_MaxStep));
    }
}
