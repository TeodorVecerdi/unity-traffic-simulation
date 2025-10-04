using Sirenix.OdinInspector;
using TrafficSimulation.Core.Maths;
using Unity.Mathematics;
using UnityEngine;

namespace TrafficSimulation.RoadGraph.Authoring.Grid;

public sealed class GridManager : MonoBehaviour {
    [SerializeField, Required, InlineEditor]
    private GridSettings m_Settings = null!;
    [SerializeField] private Vector3 m_Origin = Vector3.zero;
    [SerializeField] private Vector3 m_Normal = Vector3.up;

    [Title("Road Authoring")]
    [SerializeField, EnumToggleButtons]
    private SelectedRoadType m_SelectedRoadType = SelectedRoadType.None;

    public GridSettings Settings => m_Settings;
    public Vector3 Origin => m_Origin;
    public Vector3 Normal => m_Normal;
    public SelectedRoadType SelectedRoadType => m_SelectedRoadType;

    public bool IsValid => m_Settings != null && m_Settings.CellSize > 0.0f && math.lengthsq(m_Normal) > math.EPSILON;

    public float3 Snap(float3 worldPosition, float2 offset = default, float3 direction = default) {
        if (!IsValid || !m_Settings.SnapEnabled)
            return worldPosition;

        // Project position onto grid plane and convert to local coordinates
        ProjectToGridPlane(worldPosition, m_Normal, out var right, out var up, out var local);

        var cell = m_Settings.CellSize;

        // Calculate final offset (rotate if direction is provided)
        var finalOffset = offset;
        if (math.lengthsq(direction) > 1e-6f) {
            var snappedDirLocal = SnapDirectionToGridAxis(direction, m_Normal, right, up);
            finalOffset = CalculateDirectionAwareOffset(offset, snappedDirLocal, m_Normal, right, up);
        }

        // Apply offset, snap to grid, then add offset back
        var offsetLocal = local - finalOffset * cell;
        var roundedLocal = new float2(math.round(offsetLocal.x / cell) * cell, math.round(offsetLocal.y / cell) * cell);
        local = roundedLocal + finalOffset * cell;

        // Convert back to world space
        var snapped = (float3)m_Origin + right * local.x + up * local.y;
        return snapped;
    }

    private void ProjectToGridPlane(float3 worldPosition, float3 normal, out float3 right, out float3 up, out float2 localPosition) {
        var n = math.normalize(normal);
        var toPoint = worldPosition - (float3)m_Origin;
        var dist = math.dot(toPoint, n);
        var onPlane = worldPosition - n * dist;

        GeometryUtils.BuildOrthonormalBasis(n, math.up(), out right, out up);
        localPosition = new float2(math.dot(onPlane - (float3)m_Origin, right), math.dot(onPlane - (float3)m_Origin, up));
    }

    private static float2 SnapDirectionToGridAxis(float3 direction, float3 normal, float3 right, float3 up) {
        // Project direction onto grid plane and normalize
        var n = math.normalize(normal);
        var dirOnPlane = direction - n * math.dot(direction, n);
        var dirNormalized = math.normalizesafe(dirOnPlane, right);

        // Convert direction to grid-local 2D coordinates
        var dirLocal = new float2(math.dot(dirNormalized, right), math.dot(dirNormalized, up));

        // Snap direction to nearest grid axis (only valid directions are (±1,0) or (0,±1))
        var absX = math.abs(dirLocal.x);
        var absY = math.abs(dirLocal.y);

        if (absX > absY) {
            // Snap to right/left axis
            return new float2(math.sign(dirLocal.x), 0.0f);
        }

        // Snap to up/down axis
        return new float2(0.0f, math.sign(dirLocal.y));
    }

    private static float2 CalculateDirectionAwareOffset(float2 offset, float2 snappedDirLocal, float3 normal, float3 right, float3 up) {
        // Convert snapped direction back to world space
        var snappedDirWorld = right * snappedDirLocal.x + up * snappedDirLocal.y;

        // Calculate perpendicular direction in the grid plane (to the right of the snapped direction)
        var n = math.normalize(normal);
        var perpendicular = math.normalizesafe(math.cross(n, snappedDirWorld), up);

        // Convert perpendicular to grid-local 2D coordinates
        var perpLocal = new float2(math.dot(perpendicular, right), math.dot(perpendicular, up));

        // Rotate offset: offset.x (across) uses perpendicular, offset.y (along) uses direction
        return offset.x * perpLocal + offset.y * snappedDirLocal;
    }
}
