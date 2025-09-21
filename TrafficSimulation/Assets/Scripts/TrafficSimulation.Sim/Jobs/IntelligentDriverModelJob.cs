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
    [ReadOnly] public NativeArray<TrafficLightGroupParameters> TrafficLightGroupParameters;
    [ReadOnly] public NativeArray<TrafficLightGroupState> TrafficLightGroupStates;
    [ReadOnly] public NativeArray<TrafficLightLaneBinding> TrafficLightLaneBindings;
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

        // FIXME: Cross-lane checks work on the assumption that the current vehicle's lane has the same origin and
        //        length as the target lane. We should instead change this to map the current position to the target
        //        lane's coordinate system, but this requires more extensive refactoring.

        // Consider only vehicles that are entering this lane (target == self lane) and only from immediate neighbor lanes.
        var incomingHazardVehicleIndex = -1;
        var nearestAheadCenterDistance = float.MaxValue;
        if (lane.LeftLaneIndex >= 0) {
            ScanLaneForIncomingHazard(lane.LeftLaneIndex, self.LaneIndex, self.Position, ref incomingHazardVehicleIndex, ref nearestAheadCenterDistance);
        }

        if (lane.RightLaneIndex >= 0) {
            ScanLaneForIncomingHazard(lane.RightLaneIndex, self.LaneIndex, self.Position, ref incomingHazardVehicleIndex, ref nearestAheadCenterDistance);
        }

        if (incomingHazardVehicleIndex >= 0) {
            var hazard = Vehicles[incomingHazardVehicleIndex];
            var hazardGap = MathUtilities.BumperGap(self.Position, self.Length, hazard.Position, hazard.Length, lane.Length);
            var relativeSpeedToHazard = self.Speed - hazard.Speed;
            var hazardAcceleration = IdmMath.AccelerationWithLeader(self.Speed, relativeSpeedToHazard, math.max(IdmMath.Epsilon, hazardGap), lane.SpeedLimit, in idm);
            baseAcceleration = math.min(baseAcceleration, hazardAcceleration);
        }

        // Apply traffic light constraints
        var lightConstrainedAcceleration = ComputeTrafficLightConstrainedAcceleration(self, in idm, in lane);
        if (!float.IsNaN(lightConstrainedAcceleration)) {
            baseAcceleration = math.min(baseAcceleration, lightConstrainedAcceleration);
        }

        Accelerations[index] = baseAcceleration;
    }

    private void ScanLaneForIncomingHazard(int neighborLaneIndex, int targetLaneIndex, float selfPosition, ref int bestHazardIndex, ref float bestAheadCenterDistance) {
        if (neighborLaneIndex < 0) return;

        var range = LaneRanges[neighborLaneIndex];
        var laneLength = Lanes[neighborLaneIndex].Length;
        var end = range.Start + range.Count;

        for (var i = range.Start; i < end; i++) {
            var laneChangeState = LaneChangeStates[i];
            if (!laneChangeState.Active)
                continue;
            if (laneChangeState.TargetLaneIndex != targetLaneIndex)
                // only vehicles merging into this lane
                continue;

            // FIXME: Assumes aligned s-frames
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

    private float ComputeTrafficLightConstrainedAcceleration(in VehicleState self, in IdmParameters idm, in LaneInfo lane) {
        var nearestDistanceAhead = float.MaxValue;
        var selectedBindingIndex = -1;
        // Find nearest stop line ahead on this lane
        for (var i = 0; i < TrafficLightLaneBindings.Length; i++) {
            var binding = TrafficLightLaneBindings[i];
            if (binding.LaneIndex != self.LaneIndex)
                continue;
            var distanceAhead = MathUtilities.ComputeDistanceAlongLane(self.Position, binding.StopLinePositionMeters, lane.Length);
            if (distanceAhead < nearestDistanceAhead) {
                nearestDistanceAhead = distanceAhead;
                selectedBindingIndex = i;
            }
        }

        if (selectedBindingIndex < 0)
            return float.NaN;

        var selectedBinding = TrafficLightLaneBindings[selectedBindingIndex];
        var groupParameters = TrafficLightGroupParameters[selectedBinding.GroupIndex];
        var groupState = TrafficLightGroupStates[selectedBinding.GroupIndex];
        var color = TrafficLightMath.EvaluateColor(groupState.TimeInCycleSeconds, in groupParameters);

        var shouldTreatAsStop = false;
        const float timeSafetyMarginSeconds = 0.2f;
        var passThroughToleranceMeters = math.clamp(groupParameters.AmberStopBufferMeters, 0.25f, 1.0f);
        var timeToReachLineSeconds = nearestDistanceAhead / math.max(IdmMath.Epsilon, self.Speed);
        var timeToBoundarySeconds = TrafficLightMath.TimeToPhaseBoundary(groupState.TimeInCycleSeconds, in groupParameters);

        if (color == TrafficLightColor.Red) {
            // If we are effectively at the line, allow pass-through to avoid unrealistic instant stop
            shouldTreatAsStop = nearestDistanceAhead > passThroughToleranceMeters + 0.5f * self.Length;
        } else if (color == TrafficLightColor.Amber) {
            // Dilemma-zone policy: stop if we can stop comfortably AND we'd arrive after boundary (red)
            var stoppingDistanceMeters = (self.Speed * self.Speed) / math.max(IdmMath.Epsilon, 2.0f * idm.ComfortableBraking);
            var effectiveAvailableMeters = math.max(0.0f, nearestDistanceAhead - 0.5f * self.Length - groupParameters.AmberStopBufferMeters);
            var canStopComfortably = effectiveAvailableMeters >= stoppingDistanceMeters;
            var wouldArriveAfterBoundary = timeToReachLineSeconds >= (timeToBoundarySeconds - timeSafetyMarginSeconds);
            shouldTreatAsStop = canStopComfortably && wouldArriveAfterBoundary;
        }

        if (!shouldTreatAsStop)
            return float.NaN;

        // Virtual stopped leader at stop line; apply buffer by reducing gap
        var rawGap = MathUtilities.BumperGap(self.Position, self.Length, selectedBinding.StopLinePositionMeters, 0.0f, lane.Length);
        var gap = math.max(IdmMath.Epsilon, rawGap - groupParameters.AmberStopBufferMeters);
        var relativeSpeedToLeader = self.Speed - 0.0f;
        var acceleration = IdmMath.AccelerationWithLeader(self.Speed, relativeSpeedToLeader, gap, lane.SpeedLimit, in idm);
        return acceleration;
    }
}
