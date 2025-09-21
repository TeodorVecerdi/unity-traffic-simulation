using TrafficSimulation.Sim.Components;
using Unity.Collections;

namespace TrafficSimulation.Sim;

public sealed class WorldState(
    NativeArray<VehicleState> vehicles,
    NativeArray<IdmParameters> idmParameters,
    NativeArray<float> accelerations,
    NativeArray<LaneInfo> lanes,
    NativeArray<LaneVehicleRange> laneRanges
) : IDisposable {
    // Per-vehicle data
    public NativeArray<VehicleState> Vehicles = vehicles;
    public NativeArray<IdmParameters> IdmParameters = idmParameters;
    public NativeArray<float> Accelerations = accelerations;

    // Per-lane data
    public NativeArray<LaneInfo> Lanes = lanes;
    public NativeArray<LaneVehicleRange> LaneRanges = laneRanges;

    public void Dispose() {
        if (Vehicles.IsCreated)
            Vehicles.Dispose();
        if (IdmParameters.IsCreated)
            IdmParameters.Dispose();
        if (Accelerations.IsCreated)
            Accelerations.Dispose();
        if (Lanes.IsCreated)
            Lanes.Dispose();
        if (LaneRanges.IsCreated)
            LaneRanges.Dispose();
    }
}
