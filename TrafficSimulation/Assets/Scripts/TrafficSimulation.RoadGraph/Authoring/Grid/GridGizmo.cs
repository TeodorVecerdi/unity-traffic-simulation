using TrafficSimulation.Core.Maths;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TrafficSimulation.RoadGraph.Authoring.Grid;

public sealed class GridGizmo : MonoBehaviour {
    [SerializeField] private GridManager m_Grid = null!;
    [SerializeField] private float m_HalfExtent = 250.0f;

#if UNITY_EDITOR
    private void OnDrawGizmos() {
        if (m_Grid == null || !m_Grid.IsValid) return;

        var settings = m_Grid.Settings;
        var cell = settings.CellSize;
        var origin = (float3)m_Grid.Origin;
        var n = math.normalize(m_Grid.Normal);

        GeometryUtils.BuildOrthonormalBasis(n, math.up(), out var right, out var up);

        // Center around SceneView pivot if available
#if UNITY_EDITOR
        if (SceneView.lastActiveSceneView != null) {
            var pivot = (float3)SceneView.lastActiveSceneView.pivot;
            // Project pivot to grid plane to keep drawing aligned with plane
            var toPivot = pivot - origin;
            var distPivot = math.dot(toPivot, n);
            var pivotOnPlane = pivot - n * distPivot;
            origin = pivotOnPlane;
        }
#endif

        var extent = math.max(cell, m_HalfExtent);
        var count = (int)math.ceil(extent / cell);

        Handles.zTest = CompareFunction.LessEqual;

        // Minor lines
        Handles.color = settings.MinorLineColor;
        for (var i = -count; i <= count; i++) {
            var offset = i * cell;
            var a1 = origin + right * -extent + up * offset;
            var b1 = origin + right * extent + up * offset;
            Handles.DrawLine(a1, b1);

            var a2 = origin + up * -extent + right * offset;
            var b2 = origin + up * extent + right * offset;
            Handles.DrawLine(a2, b2);
        }

        // Major lines
        if (settings.MajorLineEvery > 1) {
            Handles.color = settings.MajorLineColor;
            for (var i = -count; i <= count; i++) {
                if (i % settings.MajorLineEvery != 0) continue;
                var offset = i * cell;
                var a1 = origin + right * -extent + up * offset;
                var b1 = origin + right * extent + up * offset;
                Handles.DrawLine(a1, b1);

                var a2 = origin + up * -extent + right * offset;
                var b2 = origin + up * extent + right * offset;
                Handles.DrawLine(a2, b2);
            }
        }

        // Axes
        Handles.color = settings.AxisXColor;
        Handles.DrawLine(origin + up * -extent, origin + up * extent);
        Handles.color = settings.AxisZColor;
        Handles.DrawLine(origin + right * -extent, origin + right * extent);
    }
#endif
}
