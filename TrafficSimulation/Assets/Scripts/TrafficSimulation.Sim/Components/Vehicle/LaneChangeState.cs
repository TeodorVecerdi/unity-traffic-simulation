using Unity.Mathematics;

namespace TrafficSimulation.Sim.Components;

/// <summary>
/// Holds the runtime state of a vehicle's lane change and the geometric parameters for rendering a smooth curve.
/// </summary>
public struct LaneChangeState {
    /// <summary>
    /// Whether a lane change is in progress.
    /// </summary>
    public bool Active;

    /// <summary>
    /// Source lane index (where the change started).
    /// </summary>
    public int SourceLaneIndex;

    /// <summary>
    /// Target lane index.
    /// </summary>
    public int TargetLaneIndex;

    /// <summary>
    /// Longitudinal progress along the lane-change curve (meters).
    /// </summary>
    public float ProgressS;

    /// <summary>
    /// Total longitudinal length over which the lateral transition occurs (meters).
    /// </summary>
    public float LongitudinalLength;

    /// <summary>
    /// Remaining cooldown before another lane change is allowed (seconds).
    /// </summary>
    public float Cooldown;

    // Geometry for rendering the smooth curve in world space
    /// <summary>
    /// The starting world position of the lane change curve when the lane change begins.
    /// </summary>
    public float3 P0;

    /// <summary>
    /// The world-space forward vector representing the initial orientation
    /// of the lane during a lane change operation.
    /// </summary>
    public float3 Forward;

    /// <summary>
    /// Represents the world left vector at the start of a lane change, pointing towards the target lane.
    /// </summary>
    public float3 Left;

    /// <summary>
    /// The signed lateral offset (in meters) representing the distance from the current lane center
    /// to the target lane center during a lane change.
    /// </summary>
    public float LateralOffset;
}
