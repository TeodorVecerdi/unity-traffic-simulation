using Unity.Mathematics;

namespace TrafficSimulation.RoadGraph.Authoring.Grid;

public static class SelectedRoadTypeExtensions {
    extension(SelectedRoadType type) {
        public int2 GetRoadSpan() => type switch {
            SelectedRoadType.None => new int2(0, 0),
            SelectedRoadType.OneLane => new int2(1, 1),
            SelectedRoadType.TwoLane => new int2(2, 1),
            SelectedRoadType.ThreeLane => new int2(3, 1),
            SelectedRoadType.FourLane => new int2(4, 1),
            _ => new int2(0, 0),
        };

        public float2 GetGridOffset() {
            var span = type.GetRoadSpan();
            return 0.5f * new float2(span);
        }
    }
}
