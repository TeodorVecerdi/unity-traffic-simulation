using Unity.Mathematics;

namespace TrafficSimulation.Geometry.Data;

public readonly struct MeshGenerationContext(float4x4 localToWorld, float4x4 worldToLocal) {
    public readonly float4x4 LocalToWorld = localToWorld;
    public readonly float4x4 WorldToLocal = worldToLocal;
}
