using Sirenix.OdinInspector;
using UnityEngine;

namespace TrafficSimulation.RoadGraph.Authoring.Grid;

[CreateAssetMenu(menuName = "TrafficSimulation/Road Authoring/Grid Settings", fileName = "GridSettings")]
public sealed class GridSettings : ScriptableObject {
    [Title("Grid")]
    [SerializeField, Min(0.01f)] private float m_CellSize = 1.0f;
    [SerializeField, Min(1)] private int m_MajorLineEvery = 10;
    [SerializeField] private Color m_MinorLineColor = new(1f, 1f, 1f, 0.08f);
    [SerializeField] private Color m_MajorLineColor = new(1f, 1f, 1f, 0.18f);
    [SerializeField] private Color m_AxisXColor = new(1f, 0.25f, 0.25f, 0.9f);
    [SerializeField] private Color m_AxisZColor = new(0.25f, 0.75f, 1f, 0.9f);
    [SerializeField] private bool m_SnapEnabled = true;

    public float CellSize => m_CellSize;
    public int MajorLineEvery => m_MajorLineEvery;
    public Color MinorLineColor => m_MinorLineColor;
    public Color MajorLineColor => m_MajorLineColor;
    public Color AxisXColor => m_AxisXColor;
    public Color AxisZColor => m_AxisZColor;
    public bool SnapEnabled => m_SnapEnabled;
}
