using TrafficSimulation.Sim.Components;
using TrafficSimulation.Sim.Math;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace TrafficSimulation.Sim.Jobs;

/// <summary>
/// Evaluates MOBIL lane-change incentives and, if beneficial and safe, initializes a lane change.
/// </summary>
[BurstCompile]
public struct MobilLaneChangeDecisionJob : IJobParallelFor {
    [ReadOnly] public NativeArray<VehicleState> Vehicles;
    [ReadOnly] public NativeArray<IdmParameters> IdmParameters;
    [ReadOnly] public NativeArray<MobilParameters> MobilParameters;
    [ReadOnly] public NativeArray<LaneInfo> Lanes;
    [ReadOnly] public NativeArray<LaneVehicleRange> LaneRanges;
    [ReadOnly] public NativeArray<float> Accelerations;
    [ReadOnly] public NativeArray<TrafficLightGroupParameters> TrafficLightGroupParameters;
    [ReadOnly] public NativeArray<TrafficLightGroupState> TrafficLightGroupStates;
    [ReadOnly] public NativeArray<TrafficLightLaneBinding> TrafficLightLaneBindings;

    public NativeArray<LaneChangeState> LaneChangeStates;

    // If a vehicle ahead is currently merging into the target lane within this distance, avoid switching into it.
    private const float IncomingAheadReservationDistance = 12.0f; // meters

    public void Execute(int index) {
        var self = Vehicles[index];
        var laneChangeState = LaneChangeStates[index];
        if (laneChangeState.Active) {
            // Already changing lanes; do nothing here.
            return;
        }

        if (laneChangeState.Cooldown > 0.0f) {
            // Wait for cooldown
            return;
        }

        var selfIdm = IdmParameters[index];
        var mobil = MobilParameters[index];
        var lane = Lanes[self.LaneIndex];

        // Current acceleration as baseline
        var selfAcceleration = Accelerations[index];
        var bestIncentive = -1e9f;
        var bestTargetLane = -1;

        // Evaluate both directions if available
        if (lane.LeftLaneIndex >= 0) {
            TryEvaluateTarget(index, self, selfIdm, mobil, selfAcceleration, lane.LeftLaneIndex, ref bestIncentive, ref bestTargetLane);
        }

        if (lane.RightLaneIndex >= 0) {
            TryEvaluateTarget(index, self, selfIdm, mobil, selfAcceleration, lane.RightLaneIndex, ref bestIncentive, ref bestTargetLane);
        }

        if (bestTargetLane >= 0 && bestIncentive > mobil.AdvantageThreshold) {
            // Initialize lane change state (geometry set later in rendering)
            laneChangeState.Active = true;
            laneChangeState.SourceLaneIndex = self.LaneIndex;
            laneChangeState.TargetLaneIndex = bestTargetLane;
            laneChangeState.ProgressS = 0.0f;
            laneChangeState.LongitudinalLength = math.max(5.0f, math.min(40.0f, 2.5f * math.max(5.0f, self.Speed))); // heuristic
            // P0/Forward/Left/LateralOffset left for SyncRenderers to set on first frame
            LaneChangeStates[index] = laneChangeState;
        }
    }

    private void TryEvaluateTarget(int index, in VehicleState self, in IdmParameters selfIdm, in MobilParameters mobil, float oldSelfAcceleration, int targetLaneIndex, ref float bestIncentive, ref int bestTargetLane) {
        Hint.Assume(targetLaneIndex >= 0 && targetLaneIndex < Lanes.Length);

        // FIXME: FindNeighborsAtPosition and FindNearestIncomingHazardsForTarget work on the assumption that the current
        //        vehicle's lane has the same origin and length as the target lane. We should instead change this to map
        //        the current position to the target lane's coordinate system, but this requires more extensive refactoring.

        var (laneLeaderIndex, laneFollowerIndex) = FindNeighborsAtPosition(targetLaneIndex, self.Position);

        // Consider vehicles currently merging into the target lane as provisional occupants.
        FindNearestIncomingHazardsForTarget(targetLaneIndex, self.Position, out var incomingAheadIndex, out var incomingAheadDistance, out var incomingBehindIndex, out var incomingBehindDistance);

        // Choose the closest ahead entity as the effective leader in the target lane.
        var effectiveLeaderIndex = laneLeaderIndex;
        var effectiveLeaderDistance = float.MaxValue;
        if (laneLeaderIndex >= 0) {
            var leaderPosition = Vehicles[laneLeaderIndex].Position;
            var laneLength = Lanes[targetLaneIndex].Length;
            effectiveLeaderDistance = MathUtilities.ComputeDistanceAlongLane(self.Position, leaderPosition, laneLength);
        }

        var effectiveLeaderIsIncoming = false;
        if (incomingAheadIndex >= 0 && incomingAheadDistance < effectiveLeaderDistance) {
            effectiveLeaderIndex = incomingAheadIndex;
            effectiveLeaderDistance = incomingAheadDistance;
            effectiveLeaderIsIncoming = true;
        }

        // If the nearest ahead is an incoming merger within a short buffer, avoid switching into that lane.
        if (effectiveLeaderIsIncoming && effectiveLeaderDistance < IncomingAheadReservationDistance) {
            return;
        }

        // Choose the closest behind entity as the effective follower in the target lane.
        var effectiveFollowerIndex = laneFollowerIndex;
        var effectiveBehindDistance = float.MaxValue;
        if (laneFollowerIndex >= 0) {
            var followerPosition = Vehicles[laneFollowerIndex].Position;
            var laneLength = Lanes[targetLaneIndex].Length;
            effectiveBehindDistance = MathUtilities.ComputeDistanceAlongLane(followerPosition, self.Position, laneLength);
        }

        if (incomingBehindIndex >= 0 && incomingBehindDistance < effectiveBehindDistance) {
            effectiveFollowerIndex = incomingBehindIndex;
        }

        // Safety for the effective new follower (may be in the target lane already or incoming from neighbor).
        var safeForNewFollower = true;
        var currentNewFollowerAcceleration = 0.0f; // old = current world (reuse IDM)
        var newNewFollowerAcceleration = 0.0f; // new = with us as leader (recompute)
        if (effectiveFollowerIndex >= 0) {
            var newFollower = Vehicles[effectiveFollowerIndex];
            var newFollowerIdm = IdmParameters[effectiveFollowerIndex];

            currentNewFollowerAcceleration = Accelerations[effectiveFollowerIndex]; // reuse IDM old accel
            newNewFollowerAcceleration = AccelerationBetween(newFollower, newFollowerIdm, self, Lanes[targetLaneIndex].SpeedLimit);

            if (newNewFollowerAcceleration < -mobil.SafeBrakingDeceleration)
                safeForNewFollower = false;
        }

        if (!safeForNewFollower)
            return;

        // Effect on old follower in the current lane
        var (oldLeaderIndex, oldFollowerIndex) = FindNeighborsAtIndex(self.LaneIndex, index);
        var currentOldFollowerAcceleration = 0.0f; // old = current world (reuse IDM)
        var newOldFollowerAcceleration = 0.0f; // new = with our old leader or free road (recompute)
        if (oldFollowerIndex >= 0) {
            var oldFollower = Vehicles[oldFollowerIndex];
            var oldFollowerIdm = IdmParameters[oldFollowerIndex];

            currentOldFollowerAcceleration = Accelerations[oldFollowerIndex]; // reuse IDM old accel
            if (oldLeaderIndex >= 0)
                newOldFollowerAcceleration = AccelerationBetween(oldFollower, oldFollowerIdm, Vehicles[oldLeaderIndex], Lanes[self.LaneIndex].SpeedLimit);
            else
                newOldFollowerAcceleration = IdmMath.AccelerationFreeRoad(oldFollower.Speed, Lanes[self.LaneIndex].SpeedLimit, in oldFollowerIdm);
        }

        // MOBIL incentive
        var othersDelta = 0.0f;
        if (oldFollowerIndex >= 0)
            othersDelta += (newOldFollowerAcceleration - currentOldFollowerAcceleration);
        if (effectiveFollowerIndex >= 0)
            othersDelta += (newNewFollowerAcceleration - currentNewFollowerAcceleration);

        // Self in target lane (new)
        var newSelfAcceleration = ComputeAccelerationAsFollower(self, selfIdm, targetLaneIndex, effectiveLeaderIndex);

        // If the target lane has a red light close ahead, reduce incentive to avoid switching into a stopped queue.
        if (HasRedOrStoppingAmberAhead(targetLaneIndex, self.Position, out var distanceToStopLine, out var brakingBufferMeters)) {
            var reduced = IdmMath.AccelerationWithLeader(self.Speed, self.Speed - 0.0f, math.max(IdmMath.Epsilon, distanceToStopLine - 0.5f * self.Length - brakingBufferMeters), Lanes[targetLaneIndex].SpeedLimit, in selfIdm);
            newSelfAcceleration = math.min(newSelfAcceleration, reduced);
        }

        var incentive = (newSelfAcceleration - oldSelfAcceleration) + mobil.Politeness * othersDelta;
        if (incentive > bestIncentive) {
            bestIncentive = incentive;
            bestTargetLane = targetLaneIndex;
        }
    }

    private (int LeaderIndex, int FollowerIndex) FindNeighborsAtIndex(int laneIndex, int selfIndex) {
        var range = LaneRanges[laneIndex];
        if (range.Count <= 0)
            return (-1, -1);

        var local = selfIndex - range.Start;
        var leader = range.Start + (local + 1) % range.Count;
        var follower = range.Start + (local - 1 + range.Count) % range.Count;
        return (leader, follower);
    }

    private (int LeaderIndex, int FollowerIndex) FindNeighborsAtPosition(int laneIndex, float position) {
        var range = LaneRanges[laneIndex];
        if (range.Count <= 0)
            return (-1, -1);

        var first = range.Start;
        var last = range.Start + range.Count - 1;
        for (var i = first; i <= last; i++) {
            var v = Vehicles[i];
            if (v.Position > position) {
                var follower = (i == first) ? last : (i - 1);
                return (i, follower);
            }
        }

        return (first, last);
    }

    private void FindNearestIncomingHazardsForTarget(int targetLaneIndex, float selfPosition, out int aheadIndex, out float aheadDistance, out int behindIndex, out float behindDistance) {
        aheadIndex = -1;
        aheadDistance = float.MaxValue;
        behindIndex = -1;
        behindDistance = float.MaxValue;

        var leftLaneIndex = Lanes[targetLaneIndex].LeftLaneIndex;
        if (leftLaneIndex >= 0) {
            ScanLaneForIncomingHazards(leftLaneIndex, targetLaneIndex, selfPosition, ref aheadIndex, ref aheadDistance, ref behindIndex, ref behindDistance);
        }

        var rightLaneIndex = Lanes[targetLaneIndex].RightLaneIndex;
        if (rightLaneIndex >= 0) {
            ScanLaneForIncomingHazards(rightLaneIndex, targetLaneIndex, selfPosition, ref aheadIndex, ref aheadDistance, ref behindIndex, ref behindDistance);
        }
    }

    private void ScanLaneForIncomingHazards(int scanLaneIndex, int targetLaneIndex, float selfPosition, ref int aheadIndex, ref float aheadDistance, ref int behindIndex, ref float behindDistance) {
        var laneLength = Lanes[targetLaneIndex].Length;
        var range = LaneRanges[scanLaneIndex];
        var end = range.Start + range.Count;
        for (var i = range.Start; i < end; i++) {
            var lcs = LaneChangeStates[i];
            if (!lcs.Active || lcs.TargetLaneIndex != targetLaneIndex)
                continue;
            var other = Vehicles[i];
            var delta = other.Position - selfPosition;
            var currentAheadDistance = MathUtilities.ComputeDistanceAlongLane(delta, laneLength);
            if (currentAheadDistance < aheadDistance) {
                aheadDistance = currentAheadDistance;
                aheadIndex = i;
            }

            var currentBehindDistance = MathUtilities.ComputeDistanceAlongLane(-delta, laneLength); // self - other
            if (currentBehindDistance < behindDistance) {
                behindDistance = currentBehindDistance;
                behindIndex = i;
            }
        }
    }

    private float ComputeAccelerationAsFollower(in VehicleState follower, in IdmParameters followerIdm, int laneIndex, int leaderIndex) {
        var lane = Lanes[laneIndex];
        if (leaderIndex < 0)
            return IdmMath.AccelerationFreeRoad(follower.Speed, lane.SpeedLimit, in followerIdm);
        var leader = Vehicles[leaderIndex];
        return AccelerationBetween(follower, followerIdm, leader, lane.SpeedLimit);
    }

    private float AccelerationBetween(in VehicleState rear, in IdmParameters rearIdm, in VehicleState front, float speedLimit) {
        var gap = MathUtilities.BumperGap(rear.Position, rear.Length, front.Position, front.Length, Lanes[rear.LaneIndex].Length);
        var relativeSpeed = rear.Speed - front.Speed;
        return IdmMath.AccelerationWithLeader(rear.Speed, relativeSpeed, math.max(IdmMath.Epsilon, gap), speedLimit, in rearIdm);
    }

    private bool HasRedOrStoppingAmberAhead(int laneIndex, float selfPosition, out float distanceToStopLine, out float bufferMeters) {
        distanceToStopLine = float.MaxValue;
        bufferMeters = 0.0f;
        var laneLength = Lanes[laneIndex].Length;
        var found = false;

        for (var i = 0; i < TrafficLightLaneBindings.Length; i++) {
            var binding = TrafficLightLaneBindings[i];
            if (binding.LaneIndex != laneIndex)
                continue;
            var distanceAhead = MathUtilities.ComputeDistanceAlongLane(selfPosition, binding.StopLinePositionMeters, laneLength);
            if (distanceAhead >= distanceToStopLine)
                continue;

            var groupParams = TrafficLightGroupParameters[binding.GroupIndex];
            var groupState = TrafficLightGroupStates[binding.GroupIndex];
            var color = TrafficLightMath.EvaluateColor(groupState.TimeInCycleSeconds, in groupParams);
            var treatAsStop = color == TrafficLightColor.Red;
            if (!treatAsStop && color == TrafficLightColor.Amber) {
                // Without vehicle context here, be conservative and treat amber as stop if reasonably near.
                treatAsStop = distanceAhead <= (10.0f + groupParams.AmberStopBufferMeters);
            }

            if (treatAsStop) {
                distanceToStopLine = distanceAhead;
                bufferMeters = groupParams.AmberStopBufferMeters;
                found = true;
            }
        }

        return found;
    }
}
