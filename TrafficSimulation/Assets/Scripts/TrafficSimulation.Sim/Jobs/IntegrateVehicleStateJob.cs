using TrafficSimulation.Sim.Components;
using TrafficSimulation.Sim.Math;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace TrafficSimulation.Sim.Jobs;

[BurstCompile]
public struct IntegrateVehicleStateJob : IJobParallelFor {
    public float DeltaTime;
    public NativeArray<VehicleState> Vehicles;
    public NativeArray<LaneChangeState> LaneChangeStates;
    [ReadOnly] public NativeArray<MobilParameters> MobilParameters;
    [ReadOnly] public NativeArray<float> Accelerations;
    [ReadOnly] public NativeArray<LaneInfo> Lanes;

    public void Execute(int index) {
        var self = Vehicles[index];
        var lane = Lanes[self.LaneIndex];
        self.Acceleration = Accelerations[index];

        // Integrate longitudinal state
        var v0 = self.Speed;
        IntegrationMath.Integrate(DeltaTime, ref self, in lane);
        var deltaS = (v0 + self.Speed) * 0.5f * DeltaTime;

        // Update lane change state
        var laneChangeState = LaneChangeStates[index];
        if (laneChangeState.Active) {
            // Advance along lane-change curve by the same longitudinal Δs
            laneChangeState.ProgressS += deltaS;
            var totalLen = math.max(0.1f, laneChangeState.LongitudinalLength);
            if (laneChangeState.ProgressS >= totalLen) {
                // Complete lane change
                self.LaneIndex = laneChangeState.TargetLaneIndex;
                var mobil = MobilParameters[index];
                var currentCooldown = laneChangeState.Cooldown;
                laneChangeState = default;
                laneChangeState.Cooldown = math.max(currentCooldown, mobil.MinTimeBetweenLaneChanges);

                // Re-wrap position in case the target lane length differs
                var newLength = Lanes[self.LaneIndex].Length;
                if (self.Position >= newLength) {
                    self.Position -= newLength * math.floor(self.Position / newLength);
                }
            }
        } else {
            // Tick cooldown
            if (laneChangeState.Cooldown > 0.0f) {
                laneChangeState.Cooldown = math.max(0.0f, laneChangeState.Cooldown - DeltaTime);
            }
        }

        Vehicles[index] = self;
        LaneChangeStates[index] = laneChangeState;
    }
}
