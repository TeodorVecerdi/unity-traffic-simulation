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
        var gridOrigin = (float3)m_Grid.Origin; // keep grid alignment relative to grid origin
        var drawOrigin = gridOrigin;
        var n = math.normalize(m_Grid.Normal);

        GeometryUtils.BuildOrthonormalBasis(n, math.up(), out var right, out var up);

        // Center drawing bounds around SceneView pivot if available, but keep lines aligned to grid origin
#if UNITY_EDITOR
        if (SceneView.lastActiveSceneView != null) {
            var pivot = (float3)SceneView.lastActiveSceneView.pivot;
            var toPivot = pivot - gridOrigin;
            var distPivot = math.dot(toPivot, n);
            var pivotOnPlane = pivot - n * distPivot;
            drawOrigin = pivotOnPlane; // only affects rendering, not grid alignment
        }
#endif

        var extent = math.max(cell, m_HalfExtent);
        var count = (int)math.ceil(extent / cell);

        // Compute grid indices of drawOrigin relative to grid origin on both axes
        var drawOriginRightCoord = math.dot(drawOrigin - gridOrigin, right);
        var drawOriginUpCoord = math.dot(drawOrigin - gridOrigin, up);
        var centerRightIndex = (int)math.round(drawOriginRightCoord / cell);
        var centerUpIndex = (int)math.round(drawOriginUpCoord / cell);

        Handles.zTest = CompareFunction.LessEqual;

        // Minor lines
        Handles.color = settings.MinorLineColor;
        for (var i = -count; i <= count; i++) {
            // Horizontal lines (varying along up)
            var upIndex = centerUpIndex + i;
            var upCoord = upIndex * cell;
            var startR = centerRightIndex * cell - extent;
            var a1 = gridOrigin + up * upCoord + right * startR;
            var b1 = a1 + right * (2 * extent);
            Handles.DrawLine(a1, b1);

            // Vertical lines (varying along right)
            var rightIndex = centerRightIndex + i;
            var rightCoord = rightIndex * cell;
            var startU = centerUpIndex * cell - extent;
            var a2 = gridOrigin + right * rightCoord + up * startU;
            var b2 = a2 + up * (2 * extent);
            Handles.DrawLine(a2, b2);
        }

        // Major lines
        if (settings.MajorLineEvery > 1) {
            Handles.color = settings.MajorLineColor;
            for (var i = -count; i <= count; i++) {
                // Horizontal (varying up) — major if absolute grid index is multiple of MajorLineEvery
                var upIndex = centerUpIndex + i;
                if (upIndex % settings.MajorLineEvery == 0) {
                    var upCoord = upIndex * cell;
                    var startR = centerRightIndex * cell - extent;
                    var a1 = gridOrigin + up * upCoord + right * startR;
                    var b1 = a1 + right * (2 * extent);
                    Handles.DrawLine(a1, b1);
                }

                // Vertical (varying right) — major if absolute grid index is multiple of MajorLineEvery
                var rightIndex = centerRightIndex + i;
                if (rightIndex % settings.MajorLineEvery == 0) {
                    var rightCoord = rightIndex * cell;
                    var startU = centerUpIndex * cell - extent;
                    var a2 = gridOrigin + right * rightCoord + up * startU;
                    var b2 = a2 + up * (2 * extent);
                    Handles.DrawLine(a2, b2);
                }
            }
        }

        // Axes
        Handles.color = settings.AxisXColor;
        Handles.DrawLine(drawOrigin + up * -extent, drawOrigin + up * extent);
        Handles.color = settings.AxisZColor;
        Handles.DrawLine(drawOrigin + right * -extent, drawOrigin + right * extent);
    }
#endif
}
