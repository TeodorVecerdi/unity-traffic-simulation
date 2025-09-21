using TrafficSimulation.Sim.Components;
using Unity.Collections;

namespace TrafficSimulation.Sim;

public sealed class WorldState(
    NativeArray<VehicleState> vehicles,
    NativeArray<IdmParameters> idmParameters,
    NativeArray<MobilParameters> mobilParameters,
    NativeArray<LaneChangeState> laneChangeStates,
    NativeArray<float> accelerations,
    NativeArray<LaneInfo> lanes,
    NativeArray<LaneVehicleRange> laneRanges,
    // Traffic lights
    NativeArray<TrafficLightGroupParameters> trafficLightGroupParameters,
    NativeArray<TrafficLightGroupState> trafficLightGroupStates,
    NativeArray<TrafficLightLaneBinding> trafficLightLaneBindings
) : IDisposable {
    // Per-vehicle data
    public NativeArray<VehicleState> Vehicles = vehicles;
    public NativeArray<IdmParameters> IdmParameters = idmParameters;
    public NativeArray<MobilParameters> MobilParameters = mobilParameters;
    public NativeArray<LaneChangeState> LaneChangeStates = laneChangeStates;
    public NativeArray<float> Accelerations = accelerations;

    // Per-lane data
    public NativeArray<LaneInfo> Lanes = lanes;
    public NativeArray<LaneVehicleRange> LaneRanges = laneRanges;

    // Traffic lights
    public NativeArray<TrafficLightGroupParameters> TrafficLightGroupParameters = trafficLightGroupParameters;
    public NativeArray<TrafficLightGroupState> TrafficLightGroupStates = trafficLightGroupStates;
    public NativeArray<TrafficLightLaneBinding> TrafficLightLaneBindings = trafficLightLaneBindings;

    public void Dispose() {
        if (Vehicles.IsCreated)
            Vehicles.Dispose();
        if (IdmParameters.IsCreated)
            IdmParameters.Dispose();
        if (MobilParameters.IsCreated)
            MobilParameters.Dispose();
        if (LaneChangeStates.IsCreated)
            LaneChangeStates.Dispose();
        if (Accelerations.IsCreated)
            Accelerations.Dispose();
        if (Lanes.IsCreated)
            Lanes.Dispose();
        if (LaneRanges.IsCreated)
            LaneRanges.Dispose();
        if (TrafficLightGroupParameters.IsCreated)
            TrafficLightGroupParameters.Dispose();
        if (TrafficLightGroupStates.IsCreated)
            TrafficLightGroupStates.Dispose();
        if (TrafficLightLaneBindings.IsCreated)
            TrafficLightLaneBindings.Dispose();
    }
}
