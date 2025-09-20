namespace TrafficSimulation.Sim.Components;

/// <summary>
/// Represents the configuration parameters for the Intelligent Driver Model (IDM).
/// The IDM is a traffic simulation model used to describe the longitudinal driving behavior
/// of vehicles, balancing desired speed, acceleration, and safe following distances.
/// </summary>
public readonly struct IdmParameters(float desiredSpeed, float maxAcceleration, float comfortableBraking, float headwayTime, float minGap, float accelerationExponent) {
    /// <summary>
    /// Specifies the target speed for the vehicle, expressed in meters per second (m/s).
    /// This value determines the intended cruising speed under optimal driving conditions,
    /// considering road constraints and other traffic influences.
    /// </summary>
    public readonly float DesiredSpeed = desiredSpeed;

    /// <summary>
    /// Specifies the maximum acceleration capability of the vehicle, measured in m/s^2.
    /// This value represents the highest rate of increase in speed that the vehicle can achieve
    /// under ideal conditions, such as optimal traction and no external constraints.
    /// </summary>
    public readonly float MaxAcceleration = maxAcceleration;

    /// <summary>
    /// Represents the deceleration rate, in m/s^2, that provides a comfortable braking experience
    /// for passengers. This value is used to model typical braking behavior that minimizes discomfort
    /// during deceleration.
    /// </summary>
    public readonly float ComfortableBraking = comfortableBraking;

    /// <summary>
    /// Represents the desired headway time, expressed in seconds, which a driver aims to maintain
    /// from the vehicle ahead. This value influences the safe following distance and is a critical
    /// parameter for traffic and vehicle behavior modeling.
    /// </summary>
    public readonly float HeadwayTime = headwayTime;

    /// <summary>
    /// Represents the minimum desired distance, measured in meters, that a vehicle should maintain
    /// from the vehicle ahead during normal operation to ensure safety.
    /// </summary>
    public readonly float MinGap = minGap;

    /// <summary>
    /// Represents the acceleration exponent used in the Intelligent Driver Model (IDM).
    /// This parameter influences the sensitivity of a vehicle's acceleration and deceleration
    /// behavior, with higher values leading to more aggressive responses to speed adjustments.
    /// </summary>
    public readonly float AccelerationExponent = accelerationExponent;
}
