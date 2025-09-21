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

        // Base acceleration from in-lane leader (or free road if none)
        float baseAcceleration;
        if (!TryGetLeader(index, in range, out var leaderIndex)) {
            baseAcceleration = IdmMath.AccelerationFreeRoad(self.Speed, lane.SpeedLimit, in idm);
        } else {
            var leader = Vehicles[leaderIndex];
            var leaderGap = MathUtilities.BumperGap(self.Position, self.Length, leader.Position, leader.Length, lane.Length);
            var relativeSpeedToLeader = self.Speed - leader.Speed;
            baseAcceleration = IdmMath.AccelerationWithLeader(self.Speed, relativeSpeedToLeader, math.max(IdmMath.Epsilon, leaderGap), lane.SpeedLimit, in idm);
        }

        Accelerations[index] = baseAcceleration;
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
}
