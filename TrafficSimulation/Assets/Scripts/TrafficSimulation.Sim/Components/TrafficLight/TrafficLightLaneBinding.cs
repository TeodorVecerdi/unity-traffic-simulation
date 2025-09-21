namespace TrafficSimulation.Sim.Components;

/// <summary>
/// Associates a lane with a traffic light group and the longitudinal position of the stop line.
/// </summary>
public readonly struct TrafficLightLaneBinding(int laneIndex, int groupIndex, float stopLinePositionMeters) {
    public readonly int LaneIndex = laneIndex;
    public readonly int GroupIndex = groupIndex;
    public readonly float StopLinePositionMeters = stopLinePositionMeters;
}


