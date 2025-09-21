namespace TrafficSimulation.Sim.Components;

public readonly struct LaneInfo(int laneId, float length, int leftLaneIndex, int rightLaneIndex, float speedLimit) {
    /// <summary>
    /// Represents the unique managed identifier for a lane within a traffic simulation.
    /// </summary>
    public readonly int LaneId = laneId;

    /// <summary>
    /// Represents the length of the lane measured in meters.
    /// </summary>
    public readonly float Length = length;

    /// <summary>
    /// The index of the lane to the left of the current lane.
    /// </summary>
    /// <remarks>
    /// A value of -1 indicates that there is no lane to the left.
    /// </remarks>
    public readonly int LeftLaneIndex = leftLaneIndex;

    /// <summary>
    /// The index of the lane to the right of the current lane.
    /// </summary>
    /// <remarks>
    /// A value of -1 indicates that there is no lane to the right.
    /// </remarks>
    public readonly int RightLaneIndex = rightLaneIndex;

    /// <summary>
    /// Represents the maximum allowable speed for vehicles traveling on the lane, measured in meters per second.
    /// </summary>
    public readonly float SpeedLimit = speedLimit;
}
