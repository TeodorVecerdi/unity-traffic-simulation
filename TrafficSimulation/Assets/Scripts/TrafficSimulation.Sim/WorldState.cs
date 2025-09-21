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
    public readonly NativeArray<VehicleState> Vehicles = vehicles;
    public readonly NativeArray<IdmParameters> IdmParameters = idmParameters;
    public readonly NativeArray<float> Accelerations = accelerations;

    // Per-lane data
    public readonly NativeArray<LaneInfo> Lanes = lanes;
    public readonly NativeArray<LaneVehicleRange> LaneRanges = laneRanges;

    public void Dispose() {
        var vehicles = Vehicles;
        if (vehicles.IsCreated)
            vehicles.Dispose();

        var idmParameters = IdmParameters;
        if (idmParameters.IsCreated)
            idmParameters.Dispose();

        var accelerations = Accelerations;
        if (accelerations.IsCreated)
            accelerations.Dispose();

        var lanes = Lanes;
        if (lanes.IsCreated)
            lanes.Dispose();

        var laneRanges = LaneRanges;
        if (laneRanges.IsCreated)
            laneRanges.Dispose();
    }
}
