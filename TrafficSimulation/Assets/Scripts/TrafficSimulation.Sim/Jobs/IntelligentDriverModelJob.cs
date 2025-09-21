using TrafficSimulation.Sim.Components;
using TrafficSimulation.Sim.Math;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace TrafficSimulation.Sim.Jobs;

[BurstCompile]
public struct IntelligentDriverModelJob : IJobParallelFor {
    [ReadOnly] public NativeArray<VehicleState> Vehicles;
    [ReadOnly] public NativeArray<IdmParameters> IdmParameters;
    [ReadOnly] public NativeArray<LaneInfo> Lanes;
    [ReadOnly] public NativeArray<LaneVehicleRange> LaneRanges;
    public NativeArray<float> Accelerations;

    public void Execute(int index) {
        var self = Vehicles[index];
        var idm = IdmParameters[index];
        var lane = Lanes[self.LaneIndex];
        var range = LaneRanges[self.LaneIndex];

        if (!TryGetLeader(index, in range, out var leaderIndex)) {
            // No leader, free road ahead
            Accelerations[index] = IdmMath.AccelerationFreeRoad(self.Speed, lane.SpeedLimit, in idm);
            return;
        }

        var leader = Vehicles[leaderIndex];
        var gap = BumperGap(self.Position, self.Length, leader.Position, leader.Length, lane.Length);
        var relativeSpeed = self.Speed - leader.Speed;
        Accelerations[index] = IdmMath.AccelerationWithLeader(self.Speed, relativeSpeed, math.max(IdmMath.Epsilon, gap), lane.SpeedLimit, in idm);
    }

    private static bool TryGetLeader(int selfIndex, in LaneVehicleRange range, out int leaderIndex) {
        if (range.Count <= 1) {
            leaderIndex = -1;
            return false;
        }

        var localIndex = selfIndex - range.Start;
        leaderIndex = range.Start + (localIndex + 1) % range.Count;
        return leaderIndex != selfIndex;
    }

    private static float BumperGap(float rearPosition, float rearLength, float frontPosition, float frontLength, float laneLength) {
        // Center-to-center arc-length distance between vehicles
        var d = frontPosition - rearPosition;
        if (d < 0.0f)
            d += laneLength;

        // Bumper-to-bumper distance between vehicles
        d -= 0.5f * (rearLength + frontLength);
        return math.max(0.0f, d);
    }
}
