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
    [ReadOnly] public NativeArray<LaneChangeState> LaneChangeStates;
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


        // Consider only vehicles that are entering this lane (target == self lane) and only from immediate neighbor lanes.
        var incomingHazardVehicleIndex = -1;
        var nearestAheadCenterDistance = float.MaxValue;
        if (lane.LeftLaneIndex >= 0) {
            ScanLaneForIncomingHazard(lane.LeftLaneIndex, index, self.LaneIndex, self.Position, lane.Length, ref incomingHazardVehicleIndex, ref nearestAheadCenterDistance);
        }

        if (lane.RightLaneIndex >= 0) {
            ScanLaneForIncomingHazard(lane.RightLaneIndex, index, self.LaneIndex, self.Position, lane.Length, ref incomingHazardVehicleIndex, ref nearestAheadCenterDistance);
        }

        if (incomingHazardVehicleIndex >= 0) {
            var hazard = Vehicles[incomingHazardVehicleIndex];
            var hazardGap = MathUtilities.BumperGap(self.Position, self.Length, hazard.Position, hazard.Length, lane.Length);
            var relativeSpeedToHazard = self.Speed - hazard.Speed;
            var hazardAcceleration = IdmMath.AccelerationWithLeader(self.Speed, relativeSpeedToHazard, math.max(IdmMath.Epsilon, hazardGap), lane.SpeedLimit, in idm);
            baseAcceleration = math.min(baseAcceleration, hazardAcceleration);
        }

        Accelerations[index] = baseAcceleration;
    }

    private void ScanLaneForIncomingHazard(int laneIndex, int selfIndex, int selfLaneIndex, float selfPosition, float laneLength, ref int bestHazardIndex, ref float bestAheadCenterDistance) {
        if (laneIndex < 0) return;

        var range = LaneRanges[laneIndex];
        var end = range.Start + range.Count;

        for (var i = range.Start; i < end; i++) {
            if (i == selfIndex) continue;

            var laneChangeState = LaneChangeStates[i];
            if (!laneChangeState.Active)
                continue;
            if (laneChangeState.TargetLaneIndex != selfLaneIndex)
                // only vehicles merging into this lane
                continue;

            var distanceAhead = MathUtilities.ComputeDistanceAlongLane(selfPosition, Vehicles[i].Position, laneLength);
            if (distanceAhead < bestAheadCenterDistance) {
                bestAheadCenterDistance = distanceAhead;
                bestHazardIndex = i;
            }
        }
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
