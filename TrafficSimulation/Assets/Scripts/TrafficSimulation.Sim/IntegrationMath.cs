using TrafficSimulation.Sim.Components;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Mathematics;

namespace TrafficSimulation.Sim;

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

        var v = math.max(0.0f, vehicleState.Speed + vehicleState.Acceleration * dt);
        var s = vehicleState.Position + (vehicleState.Speed + v) * 0.5f * dt;

        // wrap around [0, length)
        Hint.Assume(s >= 0.0f);
        if (s >= length)
            s -= length;

        vehicleState.Speed = v;
        vehicleState.Position = s;
    }
}
