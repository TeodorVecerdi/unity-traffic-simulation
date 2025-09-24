using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace TrafficSimulation.Geometry.Data;

[StructLayout(LayoutKind.Sequential)]
public struct Frame {
    public float4 Position;
    public float4 Tangent; // Forward
    public float4 Normal; // Up
    public float4 Binormal; // Right
}
