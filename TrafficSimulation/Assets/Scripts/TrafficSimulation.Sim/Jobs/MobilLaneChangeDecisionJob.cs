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
/// Geometry (P0/Forward/Left/LateralOffset) is initialized on the rendering side.
/// </summary>
[BurstCompile]
public struct MobilLaneChangeDecisionJob : IJobParallelFor {
    [ReadOnly] public NativeArray<VehicleState> Vehicles;
    [ReadOnly] public NativeArray<IdmParameters> IdmParameters;
    [ReadOnly] public NativeArray<MobilParameters> MobilParameters;
    [ReadOnly] public NativeArray<LaneInfo> Lanes;
    [ReadOnly] public NativeArray<LaneVehicleRange> LaneRanges;
    [ReadOnly] public NativeArray<float> Accelerations;

    public NativeArray<LaneChangeState> LaneChangeStates;

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
        var (newLeaderIndex, newFollowerIndex) = FindNeighborsAtPosition(targetLaneIndex, self.Position);

        // Safety for new follower in the target lane
        var safeForNewFollower = true;
        var currentNewFollowerAcceleration = 0.0f; // old = current world (reuse IDM)
        var newNewFollowerAcceleration = 0.0f; // new = with us as leader (recompute)
        if (newFollowerIndex >= 0) {
            var newFollower = Vehicles[newFollowerIndex];
            var newFollowerIdm = IdmParameters[newFollowerIndex];

            currentNewFollowerAcceleration = Accelerations[newFollowerIndex]; // reuse IDM old accel
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
        if (newFollowerIndex >= 0)
            othersDelta += (newNewFollowerAcceleration - currentNewFollowerAcceleration);

        // Self in target lane (new)
        var newSelfAcceleration = ComputeAccelerationAsFollower(self, selfIdm, targetLaneIndex, newLeaderIndex);
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
}
