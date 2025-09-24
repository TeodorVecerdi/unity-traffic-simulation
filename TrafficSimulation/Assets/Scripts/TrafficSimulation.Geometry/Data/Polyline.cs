using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TrafficSimulation.Geometry.Data;

public sealed class Polyline : MonoBehaviour {
    [SerializeField, Required] private List<float3> m_Points = [];
    [SerializeField] private Color m_GizmoColor = Color.white;
    [SerializeField, Min(0f)] private float m_SphereRadius = 0.075f;

    public List<float3> Points => m_Points;

    private void OnDrawGizmos() {
        if (m_Points == null! || m_Points.Count == 0)
            return;

        Gizmos.color = m_GizmoColor;

        // Draw spheres at each point
        foreach (var p in m_Points) {
            var worldPos = transform.TransformPoint(new Vector3(p.x, p.y, p.z));
            Gizmos.DrawSphere(worldPos, m_SphereRadius);
        }

        // Draw lines between consecutive points
        for (var i = 0; i < m_Points.Count - 1; i++) {
            var p1 = m_Points[i];
            var p2 = m_Points[i + 1];
            var worldPos1 = transform.TransformPoint(new Vector3(p1.x, p1.y, p1.z));
            var worldPos2 = transform.TransformPoint(new Vector3(p2.x, p2.y, p2.z));
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
            var worldPos = transform.TransformPoint(new Vector3(p.x, p.y, p.z));
            Handles.Label(worldPos + Vector3.up * (m_SphereRadius * 1.5f), i.ToString(), style);
        }
#endif
    }
}
