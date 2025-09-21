namespace TrafficSimulation.Sim.Components;

public readonly struct TrafficLightGroupParameters(float greenDurationSeconds, float amberDurationSeconds, float redDurationSeconds, float startTimeOffsetSeconds, float amberStopBufferMeters) {
    public readonly float GreenDurationSeconds = greenDurationSeconds;
    public readonly float AmberDurationSeconds = amberDurationSeconds;
    public readonly float RedDurationSeconds = redDurationSeconds;
    public readonly float StartTimeOffsetSeconds = startTimeOffsetSeconds;

    // Extra distance buffer to stop before the line on amber
    public readonly float AmberStopBufferMeters = amberStopBufferMeters;

    public float TotalCycleSeconds => GreenDurationSeconds + AmberDurationSeconds + RedDurationSeconds;
}
