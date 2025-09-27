using UnityEngine;

namespace TrafficSimulation.Geometry.Build;

public readonly struct SurfaceKey(Material material) : IEquatable<SurfaceKey> {
    public readonly Material Material = material;

    public bool Equals(SurfaceKey other) => Material.Equals(other.Material);
    public override bool Equals(object? obj) => obj is SurfaceKey other && Equals(other);
    public override int GetHashCode() => Material.GetHashCode();
    public static bool operator ==(SurfaceKey left, SurfaceKey right) => left.Equals(right);
    public static bool operator !=(SurfaceKey left, SurfaceKey right) => !left.Equals(right);
}
