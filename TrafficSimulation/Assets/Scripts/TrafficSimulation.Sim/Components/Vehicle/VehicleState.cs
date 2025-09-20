namespace TrafficSimulation.Sim.Components;

public struct VehicleState(int vehicleId, int laneIndex, float position, float speed, float acceleration, float length) {
    /// <summary>
    /// Represents the unique managed identifier for a vehicle.
    /// </summary>
    public int VehicleId = vehicleId;

    /// <summary>
    /// Represents the index of the lane in which the vehicle is currently positioned.
    /// </summary>
    public int LaneIndex = laneIndex;

    /// <summary>
    /// Represents the longitudinal position of the vehicle along its lane measured in meters.
    /// This value indicates the vehicle's distance from the start of the lane.
    /// </summary>
    public float Position = position;

    /// <summary>
    /// Represents the current speed of a vehicle in m/s
    /// </summary>
    public float Speed = speed;

    /// <summary>
    /// Represents the acceleration for a vehicle in m/s².
    /// </summary>
    public float Acceleration = acceleration;

    /// <summary>
    /// Represents the length of the vehicle in meters.
    /// </summary>
    public float Length = length;
}
