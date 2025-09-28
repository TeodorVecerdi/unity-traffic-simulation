using Sirenix.OdinInspector;
using TrafficSimulation.RoadGraph.Configuration;
using TrafficSimulation.RoadGraph.Data;
using UnityEngine;

namespace TrafficSimulation.RoadGraph.Authoring;

public sealed class RoadSegmentAuthoring : MonoBehaviour {
    [Title("Segment Configuration")]
    [SerializeField, InlineProperty, HideLabel] private RoadSegment m_SegmentConfiguration;
    [SerializeField, Required] private RoadMarkingConfiguration m_RoadMarkingConfiguration = null!;
    [SerializeField, Required] private SidewalkConfiguration m_SidewalkConfiguration = null!;

    public RoadSegment RoadSegment => m_SegmentConfiguration;
    public RoadMarkingConfiguration RoadMarkingConfiguration => m_RoadMarkingConfiguration;
    public SidewalkConfiguration SidewalkConfiguration => m_SidewalkConfiguration;
}
