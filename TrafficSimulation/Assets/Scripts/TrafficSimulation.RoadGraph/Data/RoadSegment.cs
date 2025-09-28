using Sirenix.OdinInspector;
using UnityEngine;

namespace TrafficSimulation.RoadGraph.Data;

[Serializable]
public struct RoadSegment() {
    [LabelText("Lanes (Right to Left)")]
    [SerializeField, Required] public List<RoadLaneSegment> Lanes = [];
}
