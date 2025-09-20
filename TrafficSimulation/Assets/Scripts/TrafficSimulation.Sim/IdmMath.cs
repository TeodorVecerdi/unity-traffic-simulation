using TrafficSimulation.Sim.Components;
using Unity.Burst;
using Unity.Mathematics;

namespace TrafficSimulation.Sim;

[BurstCompile]
public static class IdmMath {
    public const float Epsilon = 1e-4f;

    [BurstCompile]
    public static float AccelerationFreeRoad(float speed, float speedLimit, in IdmParameters idmParameters) {
        var desiredSpeed = math.min(idmParameters.DesiredSpeed, speedLimit);

        // Used to determine how close the vehicle is to its target speed
        var ratio = speed / math.max(Epsilon, desiredSpeed);

        // Acceleration calculation based on the Intelligent Driver Model (IDM)
        // - The formula incorporates the maximum acceleration and a term that reduces acceleration
        //   as the vehicle's speed approaches the desired speed
        var acceleration = idmParameters.MaxAcceleration * (1.0f - math.pow(ratio, idmParameters.AccelerationExponent));
        return acceleration;
    }

    [BurstCompile]
    public static float AccelerationWithLeader(float speed, float deltaSpeed, float gap, float speedLimit, in IdmParameters idmParameters) {
        var desiredSpeed = math.min(idmParameters.DesiredSpeed, speedLimit);
        // Used to determine how close the vehicle is to its target speed
        var ratio = speed / math.max(Epsilon, desiredSpeed);
        // Calculate the desired gap to the leading vehicle
        var sStar = DesiredGap(speed, deltaSpeed, idmParameters);
        // Free-road component: represents how much acceleration is reduced due to current speed vs desired speed
        var freeRoadComponent = math.pow(ratio, idmParameters.AccelerationExponent);
        // Interaction component: represents deceleration needed due to proximity to leading vehicle
        var interactionComponent = math.pow(sStar / math.max(Epsilon, gap), 2.0f);
        var acceleration = idmParameters.MaxAcceleration * (1.0f - freeRoadComponent - interactionComponent);
        return acceleration;
    }

    [BurstCompile]
    private static float DesiredGap(float speed, float deltaSpeed, in IdmParameters idmParameters) {
        // Desired minimum gap to the leading vehicle
        // - Combines a base minimum gap, a speed-dependent term, and a term accounting for relative speed
        var term = speed * deltaSpeed / (2.0f * math.sqrt(idmParameters.MaxAcceleration * idmParameters.ComfortableBraking));
        var sStar = idmParameters.MinGap + speed * idmParameters.HeadwayTime + math.max(0.0f, term);
        return math.max(idmParameters.MinGap, sStar);
    }
}
