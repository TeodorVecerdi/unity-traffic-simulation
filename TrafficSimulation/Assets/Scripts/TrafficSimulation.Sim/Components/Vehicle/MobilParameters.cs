namespace TrafficSimulation.Sim.Components;

public readonly struct MobilParameters(float politeness, float advantageThreshold, float safeBrakingDeceleration, float minTimeBetweenLaneChanges) {
    /// <summary>
    /// Represents the degree of consideration a vehicle exhibits toward others during lane changes.
    /// This value ranges between 0.0 and 1.0, where a lower value indicates more selfish behavior
    /// while a higher value reflects greater politeness or willingness to yield to other vehicles.
    /// </summary>
    public readonly float Politeness = politeness;

    /// <summary>
    /// Specifies the minimum acceleration advantage, expressed in meters per second squared (m/s^2),
    /// required to justify a lane change. This threshold helps determine whether the potential benefits
    /// of changing lanes outweigh the potential risks or disadvantages.
    /// </summary>
    public readonly float AdvantageThreshold = advantageThreshold;

    /// <summary>
    /// Represents the maximum safe deceleration rate for the vehicle, expressed in m/s^2. This value
    /// specifies the deceleration limit that ensures the vehicle can safely reduce speed without
    /// compromising safety under typical driving conditions.
    /// </summary>
    public readonly float SafeBrakingDeceleration = safeBrakingDeceleration;

    /// <summary>
    /// Specifies the minimum time interval, in seconds, that must elapse between successive lane changes
    /// by the vehicle. This parameter ensures a safe and realistic frequency of lane changes to prevent
    /// erratic or aggressive driving behavior.
    /// </summary>
    public readonly float MinTimeBetweenLaneChanges = minTimeBetweenLaneChanges;
}
