using Sirenix.OdinInspector;
using TrafficSimulation.Geometry.Data;
using TrafficSimulation.Geometry.Graph;
using TrafficSimulation.Geometry.Helpers;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace TrafficSimulation.Geometry.Generators;

[Serializable]
public sealed class QuadGenerator : MeshGenerator {
    [SerializeField, MinValue(0.001f)] private float m_Width = 1.0f;
    [SerializeField, MinValue(0.001f)] private float m_Length = 1.0f;
    [SerializeField] private Vector3 m_Normal = Vector3.up;

    public override bool Validate() {
        return m_Width > 0.0f && m_Length > 0.0f && m_Normal != Vector3.zero;
    }

    public override void GetCounts(in MeshGenerationContext context, out int vertexCount, out int indexCount) {
        vertexCount = 4;
        indexCount = 6;
    }

    public override JobHandle ScheduleFill(in MeshGenerationContext context, in MeshBufferSlice bufferSlice, JobHandle dependency) {
        return new QuadFillJob {
            Width = m_Width,
            Length = m_Length,
            Normal = m_Normal.normalized,
            BufferSlice = bufferSlice,
        }.Schedule(dependency);
    }

    [BurstCompile]
    private struct QuadFillJob : IJob {
        public float Width;
        public float Length;
        public float3 Normal;
        public MeshBufferSlice BufferSlice;

        public void Execute() {
            var tangent = math.normalize(math.cross(Normal, math.abs(Normal.y) < 0.99f ? new float3(0.0f, 1.0f, 0.0f) : new float3(1.0f, 0.0f, 0.0f)));
            var bitangent = math.cross(Normal, tangent);

            var halfSize = new float3(Width * 0.5f, 0.0f, Length * 0.5f);

            var v0 = -tangent * halfSize.x - bitangent * halfSize.z;
            var v1 = tangent * halfSize.x - bitangent * halfSize.z;
            var v2 = tangent * halfSize.x + bitangent * halfSize.z;
            var v3 = -tangent * halfSize.x + bitangent * halfSize.z;

            var vertex0 = new MeshVertex { Position = v0, Normal = Normal, UV = new float2(0.0f, 0.0f) };
            var vertex1 = new MeshVertex { Position = v1, Normal = Normal, UV = new float2(1.0f, 0.0f) };
            var vertex2 = new MeshVertex { Position = v2, Normal = Normal, UV = new float2(1.0f, 1.0f) };
            var vertex3 = new MeshVertex { Position = v3, Normal = Normal, UV = new float2(0.0f, 1.0f) };

            var vertices = BufferSlice.GetVertices();
            var indices = BufferSlice.GetIndices();
            var vertexOffset = 0;
            var indexOffset = 0;
            MeshWrite.WriteQuad(vertex0, vertex1, vertex2, vertex3, ref vertices, ref indices, ref vertexOffset, ref indexOffset);
        }
    }
}
