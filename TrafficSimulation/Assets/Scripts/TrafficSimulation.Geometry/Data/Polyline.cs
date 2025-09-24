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

    public List<float3> Points => ComputePoints();

    private List<float3> ComputePoints() {
        var pts = new List<float3>(m_Points.Count);
        foreach (var p in m_Points) {
            pts.Add(p.Position);
            if (p.HardEdge) {
                pts.Add(p.Position);
            }
        }
        return pts;
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

    [Serializable]
    private struct PolylinePoint {
        [HideLabel]
        public float3 Position;
        [LabelText("Hard?")]
        public bool HardEdge;
    }
}
