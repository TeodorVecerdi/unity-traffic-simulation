using Unity.Burst;
using Unity.Mathematics;

namespace TrafficSimulation.Sim.Math;

[BurstCompile]
public static class MathUtilities {
    /// <summary>
    /// Calculates the bumper-to-bumper distance between two vehicles on a circular lane.
    /// </summary>
    /// <param name="rearPosition">The position of the rear vehicle on the lane.</param>
    /// <param name="rearLength">The length of the rear vehicle.</param>
    /// <param name="frontPosition">The position of the front vehicle on the lane.</param>
    /// <param name="frontLength">The length of the front vehicle.</param>
    /// <param name="laneLength">The total length of the lane.</param>
    /// <returns>Returns the non-negative bumper-to-bumper distance between the two vehicles.</returns>
    [BurstCompile]
    public static float BumperGap(float rearPosition, float rearLength, float frontPosition, float frontLength, float laneLength) {
        // Center-to-center arc-length distance between vehicles
        var d = frontPosition - rearPosition;
        if (d < 0.0f)
            d += laneLength;

        // Bumper-to-bumper distance between vehicles
        d -= 0.5f * (rearLength + frontLength);
        return math.max(0.0f, d);
    }
}
