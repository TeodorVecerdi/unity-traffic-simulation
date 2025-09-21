using TrafficSimulation.Sim.Components;
using Unity.Burst;
using Unity.Mathematics;

namespace TrafficSimulation.Sim.Math;

[BurstCompile]
public static class TrafficLightMath {
    [BurstCompile]
    public static TrafficLightColor EvaluateColor(float timeInCycleSeconds, in TrafficLightGroupParameters parameters) {
        var t = timeInCycleSeconds;
        var g = parameters.GreenDurationSeconds;
        var a = parameters.AmberDurationSeconds;
        var r = parameters.RedDurationSeconds;
        var cycle = g + a + r;
        if (cycle <= 0.0f)
            return TrafficLightColor.Red;
        var mod = t - cycle * math.floor(t / cycle);
        if (mod < g)
            return TrafficLightColor.Green;
        if (mod < g + a)
            return TrafficLightColor.Amber;
        return TrafficLightColor.Red;
    }
}
