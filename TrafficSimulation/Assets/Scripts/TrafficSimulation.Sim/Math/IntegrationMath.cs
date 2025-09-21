using System.Runtime.CompilerServices;
using TrafficSimulation.Sim.Components;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Mathematics;

namespace TrafficSimulation.Sim.Math;

[BurstCompile]
public static class IntegrationMath {
    /// <summary>
    /// Updates the state of a vehicle by integrating its position and speed based on its
    /// acceleration over a given timestep, ensuring the position wraps within the length
    /// of the current lane.
    /// </summary>
    /// <param name="dt">The timestep for the simulation, in seconds.</param>
    /// <param name="vehicleState">The state of the vehicle to be updated, including its position, speed, and acceleration.</param>
    /// <param name="laneInfo">Information about the lane the vehicle is on, including its length.</param>
    [BurstCompile]
    public static void Integrate(float dt, ref VehicleState vehicleState, in LaneInfo laneInfo) {
        var length = laneInfo.Length;

        var s = vehicleState.Position;
        var v = vehicleState.Speed;
        Integrate(dt, ref s, ref v, vehicleState.Acceleration);

        // wrap around [0, length)
        Hint.Assume(s >= 0.0f);
        if (s >= length)
            s -= length * math.floor(s / length);

        vehicleState.Speed = v;
        vehicleState.Position = s;
    }

    [BurstCompile]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Integrate(float dt, ref float position, ref float speed, float acceleration) {
        // Semi-implicit Euler integration with trapezoidal rule for position
        // v1 = v0 + a * dt
        // s1 = s0 + (v0 + v1) / 2 * dt
        var newSpeed = math.max(0.0f, speed + acceleration * dt);
        var newPosition = position + (speed + newSpeed) * 0.5f * dt;

        speed = newSpeed;
        position = newPosition;
    }
}
