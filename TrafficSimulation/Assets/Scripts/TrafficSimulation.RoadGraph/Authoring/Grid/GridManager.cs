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

    public float3 Snap(float3 worldPosition) {
        if (!IsValid || !m_Settings.SnapEnabled)
            return worldPosition;

        var n = math.normalize(m_Normal);
        var toPoint = worldPosition - (float3)m_Origin;
        var dist = math.dot(toPoint, n);
        var onPlane = worldPosition - n * dist;

        GeometryUtils.BuildOrthonormalBasis(n, math.up(), out var right, out var up);
        var local = new float2(math.dot(onPlane - (float3)m_Origin, right), math.dot(onPlane - (float3)m_Origin, up));

        var cell = m_Settings.CellSize;
        local = new float2(math.round(local.x / cell) * cell, math.round(local.y / cell) * cell);

        var snapped = (float3)m_Origin + right * local.x + up * local.y;
        return snapped;
    }
}
