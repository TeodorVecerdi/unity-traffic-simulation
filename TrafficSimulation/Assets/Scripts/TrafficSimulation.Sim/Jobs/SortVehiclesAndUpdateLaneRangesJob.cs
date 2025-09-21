using TrafficSimulation.Sim.Components;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Jobs;

namespace TrafficSimulation.Sim.Jobs;

[BurstCompile]
public struct SortVehiclesAndUpdateLaneRangesJob : IJob {
    [ReadOnly] public NativeArray<LaneInfo> Lanes;
    public NativeArray<LaneVehicleRange> LaneRanges;
    public NativeArray<VehicleState> Vehicles;
    public NativeArray<IdmParameters> IdmParameters;
    public NativeArray<MobilParameters> MobilParameters;
    public NativeArray<LaneChangeState> LaneChangeStates;

    public void Execute() {
        // All per-vehicle arrays must be aligned by index.
        Hint.Assume(Vehicles.Length == IdmParameters.Length);
        Hint.Assume(Vehicles.Length == MobilParameters.Length);
        Hint.Assume(Vehicles.Length == LaneChangeStates.Length);

        // LaneRanges must align with Lanes by index.
        Hint.Assume(Lanes.Length == LaneRanges.Length);

        // 1. Sort vehicles by (LaneIndex, Position) and keep IdmParameters aligned by index
        if (Vehicles.Length > 1) {
            DualSortByLaneAndPosition(0, Vehicles.Length - 1);
        }

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

            // Skip invalid lane indices defensively (upstream should guarantee validity).
            if (Hint.Unlikely((uint)laneIndex >= (uint)laneCount)) {
                start = end;
                continue;
            }

            var count = end - start;
            LaneRanges[laneIndex] = new LaneVehicleRange(start, count);
            start = end;
        }
    }

    // In-place quicksort that swaps both Vehicles and IdmParameters to keep indices aligned
    private void DualSortByLaneAndPosition(int left, int right) {
        if (left >= right) return;
        var i = left;
        var j = right;
        var pivot = Vehicles[(left + right) >> 1];
        while (i <= j) {
            while (Compare(Vehicles[i], pivot) < 0) i++;
            while (Compare(Vehicles[j], pivot) > 0) j--;
            if (i <= j) {
                if (i != j) Swap(i, j);
                i++;
                j--;
            }
        }

        if (left < j) {
            DualSortByLaneAndPosition(left, j);
        }

        if (i < right) {
            DualSortByLaneAndPosition(i, right);
        }
    }

    private int Compare(in VehicleState x, in VehicleState y) {
        if (x.LaneIndex != y.LaneIndex)
            return x.LaneIndex.CompareTo(y.LaneIndex);
        return x.Position.CompareTo(y.Position);
    }

    private void Swap(int a, int b) {
        (Vehicles[a], Vehicles[b]) = (Vehicles[b], Vehicles[a]);
        (IdmParameters[a], IdmParameters[b]) = (IdmParameters[b], IdmParameters[a]);
        (MobilParameters[a], MobilParameters[b]) = (MobilParameters[b], MobilParameters[a]);
        (LaneChangeStates[a], LaneChangeStates[b]) = (LaneChangeStates[b], LaneChangeStates[a]);
    }
}
