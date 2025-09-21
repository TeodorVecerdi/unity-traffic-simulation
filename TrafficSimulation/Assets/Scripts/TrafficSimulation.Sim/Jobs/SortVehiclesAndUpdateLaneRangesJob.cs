using TrafficSimulation.Sim.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace TrafficSimulation.Sim.Jobs;

[BurstCompile]
public struct SortVehiclesAndUpdateLaneRangesJob : IJob {
    [ReadOnly] public NativeArray<LaneInfo> Lanes;
    public NativeArray<LaneVehicleRange> LaneRanges;
    public NativeArray<VehicleState> Vehicles;

    public void Execute() {
        // 1. Sort vehicles by (LaneIndex, Position)
        Vehicles.Sort(new VehicleStateComparer());

        // 2. Reconstruct LaneRanges
        var laneCount = Lanes.Length;
        for (var i = 0; i < laneCount; i++) {
            LaneRanges[i] = default;
        }

        var vehicleCount = Vehicles.Length;
        var start = 0;
        while (start < vehicleCount) {
            var laneIndex = Vehicles[start].LaneIndex;
            var end = start + 1;
            while (end < vehicleCount && Vehicles[end].LaneIndex == laneIndex) {
                end++;
            }

            var count = end - start;
            LaneRanges[laneIndex] = new LaneVehicleRange(start, count);
            start = end;
        }
    }

    private struct VehicleStateComparer : IComparer<VehicleState> {
        public int Compare(VehicleState x, VehicleState y) {
            if (x.LaneIndex != y.LaneIndex) return x.LaneIndex.CompareTo(y.LaneIndex);
            return x.Position.CompareTo(y.Position);
        }
    }
}
