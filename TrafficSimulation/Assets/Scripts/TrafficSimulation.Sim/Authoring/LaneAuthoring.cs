using System;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace TrafficSimulation.Sim.Authoring;

public sealed class LaneAuthoring : MonoBehaviour {
    [Title("Core Properties")]
    [SerializeField] private int m_LaneId;
    [SerializeField, Unit(Units.Meter)] private float m_Length;
    [SerializeField] private bool m_Loop;

    [Title("Connections")]
    [SerializeField] private LaneAuthoring? m_LeftLane;
    [SerializeField] private LaneAuthoring? m_RightLane;

    [Title("Driving Properties")]
    [SerializeField, Unit(Units.MetersPerSecond)] private float m_SpeedLimit = 30.0f;

    [Title("Gizmos")]
    [SerializeField] private Color m_LaneGizmoColor = Color.yellow;
    [SerializeField] private float m_GizmoSize = 0.2f;
    [SerializeField] private bool m_DrawConnections = true;
    [SerializeField, ShowIf(nameof(m_DrawConnections))] private Color m_ConnectionGizmoColor = Color.cyan;
    [SerializeField, ShowIf(nameof(m_DrawConnections))] private float m_ConnectionGizmoSize = 0.1f;
    [SerializeField] private bool m_AlwaysDrawGizmos;

    public int LaneId => m_LaneId;
    public float Length => m_Length;
    public bool Loop => m_Loop;
    public LaneAuthoring? LeftLane => m_LeftLane;
    public LaneAuthoring? RightLane => m_RightLane;
    public float SpeedLimit => m_SpeedLimit;

    private void OnDrawGizmos() {
        if (!m_AlwaysDrawGizmos) return;
#if UNITY_EDITOR
        DrawLaneGizmos(Selection.Contains(gameObject));
#endif
    }

    private void OnDrawGizmosSelected() {
        if (m_AlwaysDrawGizmos) return;
        DrawLaneGizmos(true);
    }

    private void DrawLaneGizmos(bool selected) {
        Gizmos.color = m_LaneGizmoColor;
        var startPosition = transform.position;
        var endPosition = startPosition + transform.forward * m_Length;
        Gizmos.DrawLine(startPosition, endPosition);
        Gizmos.DrawSphere(startPosition, m_GizmoSize);
        Gizmos.DrawSphere(endPosition, m_GizmoSize);

        if (m_DrawConnections && selected) {
            DrawConnections();
        }
    }

    private void DrawConnections() {
#if UNITY_EDITOR
        if (m_LeftLane == null && m_RightLane == null)
            return;

        Gizmos.color = m_ConnectionGizmoColor;
        var sceneView = SceneView.lastActiveSceneView;
        var lanePoint = transform.position + transform.forward * (m_Length * 0.5f);

        if (sceneView != null && sceneView.camera != null) {
            lanePoint = ClosestPointOnLaneToSceneCenterRay(transform.position, transform.forward, m_Length, sceneView.camera);
            Gizmos.DrawSphere(lanePoint, m_ConnectionGizmoSize);
        }

        if (m_LeftLane != null) {
            var axis = m_LeftLane.transform;
            var leftLanePoint = ProjectPointOntoAxisClamped(axis, m_LeftLane.m_Length, lanePoint);
            Gizmos.DrawLine(lanePoint, leftLanePoint);
            DrawArrowHead(lanePoint, (leftLanePoint - lanePoint) * 0.5f + lanePoint);
            Gizmos.DrawSphere(leftLanePoint, m_ConnectionGizmoSize);
        }

        if (m_RightLane != null) {
            var axis = m_RightLane.transform;
            var rightLanePoint = ProjectPointOntoAxisClamped(axis, m_RightLane.Length, lanePoint);
            Gizmos.DrawLine(lanePoint, rightLanePoint);
            DrawArrowHead(lanePoint, (rightLanePoint - lanePoint) * 0.5f + lanePoint);
            Gizmos.DrawSphere(rightLanePoint, m_ConnectionGizmoSize);
        }
#endif
    }

    private void DrawArrowHead(Vector3 from, Vector3 to, float headLength = 0.25f, float headAngle = 20.0f) {
        var direction = (to - from).normalized;
        var right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + headAngle, 0) * Vector3.forward;
        var left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - headAngle, 0) * Vector3.forward;
        Gizmos.DrawLine(to, to + right * headLength);
        Gizmos.DrawLine(to, to + left * headLength);
    }

#if UNITY_EDITOR
    private static Vector3 ClosestPointOnLaneToSceneCenterRay(Vector3 laneOrigin, Vector3 laneDir, float laneLength, Camera cam) {
        // Center of the SceneView
        var ray = cam.ScreenPointToRay(new Vector3(cam.pixelWidth * 0.5f, cam.pixelHeight * 0.5f, 0f));

        laneDir = laneDir.normalized;
        var r = laneOrigin - ray.origin;
        var a = Vector3.Dot(laneDir, laneDir); // ~= 1
        var b = Vector3.Dot(laneDir, ray.direction);
        var c = Vector3.Dot(ray.direction, ray.direction); // ~= 1
        var d = Vector3.Dot(laneDir, r);
        var e = Vector3.Dot(ray.direction, r);

        var denom = a * c - b * b;

        float t;
        if (denom > 1e-6f) {
            // Closest points between two non-parallel lines
            t = (b * e - c * d) / denom;
        } else {
            // Nearly parallel: just project camera position onto the lane
            t = -d / (a > 1e-6f ? a : 1f);
        }

        t = Mathf.Clamp(t, 0f, laneLength);
        return laneOrigin + laneDir * t;
    }

    private static Vector3 ProjectPointOntoAxisClamped(Transform axis, float length, Vector3 worldPoint) {
        var dir = axis.forward.normalized;
        var s = Mathf.Clamp(Vector3.Dot(worldPoint - axis.position, dir), 0f, length);
        return axis.position + dir * s;
    }
#endif
}
