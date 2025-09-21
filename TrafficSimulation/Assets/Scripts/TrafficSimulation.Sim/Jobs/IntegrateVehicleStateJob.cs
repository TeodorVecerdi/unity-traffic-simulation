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
        IntegrationMath.Integrate(DeltaTime, ref self, in lane);

        // Update lane change state
        var laneChangeState = LaneChangeStates[index];
        if (laneChangeState.Active) {
            var currentSpeed = self.Speed;
            IntegrationMath.Integrate(DeltaTime, ref laneChangeState.ProgressS, ref currentSpeed, self.Acceleration);
            var totalLen = math.max(0.1f, laneChangeState.LongitudinalLength);
            if (laneChangeState.ProgressS >= totalLen) {
                // Complete lane change
                self.LaneIndex = laneChangeState.TargetLaneIndex;
                var mobil = MobilParameters[index];
                var currentCooldown = laneChangeState.Cooldown;
                laneChangeState = default;
                laneChangeState.Cooldown = math.max(currentCooldown, mobil.MinTimeBetweenLaneChanges);
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
