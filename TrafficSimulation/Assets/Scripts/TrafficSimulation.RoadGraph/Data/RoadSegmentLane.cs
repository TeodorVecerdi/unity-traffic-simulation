using Sirenix.OdinInspector;
using UnityEngine;

namespace TrafficSimulation.RoadGraph.Data;

[Serializable]
public struct RoadSegmentLane() {
    [SerializeField, Unit(Units.Meter)] public float Width = 3.0f;
    [SerializeField] public LaneDirection Direction = LaneDirection.Forward;
    [SerializeField] public RoadMarkingType LeftMarking = RoadMarkingType.None;
    [SerializeField] public SidewalkType LeftSidewalk = SidewalkType.None;
    [SerializeField] public RoadMarkingType RightMarking = RoadMarkingType.None;
    [SerializeField] public SidewalkType RightSidewalk = SidewalkType.None;
}
