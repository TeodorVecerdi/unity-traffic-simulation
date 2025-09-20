namespace TrafficSimulation.Sim.Components;

/// <summary>
/// Represents the array segment of vehicles within a specific lane in the global vehicle array.
/// It allows efficient access to vehicles associated with a particular lane.
/// </summary>
/// <param name="start">The starting index of the vehicle range within the lane.</param>
/// <param name="count">The total number of vehicles within the specified range in the lane.</param>
public readonly struct LaneVehicleRange(int start, int count) {
    /// <summary>
    /// Represents the starting index of the range of vehicles within a lane.
    /// </summary>
    public readonly int Start = start;

    /// <summary>
    /// Represents the total number of vehicles within the specified range in a lane.
    /// </summary>
    public readonly int Count = count;
}
