using TrafficSimulation.Sim.Components;
using TrafficSimulation.Sim.Math;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace TrafficSimulation.Sim.Jobs;

[BurstCompile]
public struct IntegrateVehicleStateJob : IJobParallelFor {
    public float DeltaTime;
    public NativeArray<VehicleState> Vehicles;
    [ReadOnly] public NativeArray<float> Accelerations;
    [ReadOnly] public NativeArray<LaneInfo> Lanes;

    public void Execute(int index) {
        var self = Vehicles[index];
        var lane = Lanes[self.LaneIndex];
        self.Acceleration = Accelerations[index];
        IntegrationMath.Integrate(DeltaTime, ref self, in lane);

        Vehicles[index] = self;
    }
}
