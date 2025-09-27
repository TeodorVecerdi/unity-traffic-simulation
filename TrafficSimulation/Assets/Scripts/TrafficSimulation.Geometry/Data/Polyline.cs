using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TrafficSimulation.Geometry.Data;

public sealed class Polyline : MonoBehaviour {
    [SerializeField, Required] private List<PolylinePoint> m_Points = [];
    [SerializeField] private Color m_GizmoColor = Color.white;
    [SerializeField] private Color m_HardGizmoColor = Color.cyan;
    [SerializeField, Min(0f)] private float m_SphereRadius = 0.075f;

    public List<PolylinePoint> Points => m_Points;

    public (List<float3> Positions, List<bool> EmitEdges) GetGeometry() {
        if (m_Points == null! || m_Points.Count == 0) {
            return ([], []);
        }

        var positions = new List<float3>(m_Points.Count);
        var emitEdges = new List<bool>(m_Points.Count);

        for (var i = 0; i < m_Points.Count; i++) {
            var p = m_Points[i].Position;

            // Normal addition
            AddPoint(p);

            // Duplicate hard points; edge between duplicates must be skipped
            if (m_Points[i].HardEdge) {
                AddPoint(p, forceSkipEdge: true);
            }
        }

        if (positions.Count != emitEdges.Count + 1) {
            throw new Exception($"Internal error in {nameof(Polyline)}: points count ({positions.Count}) != emitEdges count + 1 ({emitEdges.Count + 1})");
        }

        return (positions, emitEdges);

        void AddPoint(float3 p, bool forceSkipEdge = false) {
            if (positions.Count > 0) {
                var isDegenerate = math.lengthsq(p - positions[^1]) <= 1e-12f;
                emitEdges.Add(!(forceSkipEdge || isDegenerate));
            }

            positions.Add(p);
        }
    }

    private void OnDrawGizmos() {
        if (m_Points == null! || m_Points.Count == 0)
            return;

        // Draw spheres at each point
        foreach (var p in m_Points) {
            var worldPos = transform.TransformPoint(p.Position);
            Gizmos.color = p.HardEdge ? m_HardGizmoColor : m_GizmoColor;
            Gizmos.DrawSphere(worldPos, m_SphereRadius);
        }

        // Draw lines between consecutive points
        Gizmos.color = m_GizmoColor;
        for (var i = 0; i < m_Points.Count - 1; i++) {
            var p1 = m_Points[i];
            var p2 = m_Points[i + 1];
            var worldPos1 = transform.TransformPoint(p1.Position);
            var worldPos2 = transform.TransformPoint(p2.Position);
            Gizmos.DrawLine(worldPos1, worldPos2);
        }
    }

    private void OnDrawGizmosSelected() {
        if (m_Points == null! || m_Points.Count == 0) return;

#if UNITY_EDITOR
        // Draw labels (indices) for each point when selected
        var style = new GUIStyle(EditorStyles.boldLabel) {
            normal = { textColor = m_GizmoColor },
        };

        for (var i = 0; i < m_Points.Count; i++) {
            var p = m_Points[i];
            var worldPos = transform.TransformPoint(p.Position);
            Handles.Label(worldPos + Vector3.up * (m_SphereRadius * 1.5f), i.ToString(), style);
        }
#endif
    }
}
