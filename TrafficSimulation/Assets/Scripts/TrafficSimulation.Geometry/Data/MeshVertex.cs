using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace TrafficSimulation.Geometry.Data;

[StructLayout(LayoutKind.Sequential)]
public struct MeshVertex {
    public float3 Position;
    public float3 Normal;
    public float2 UV;
}
