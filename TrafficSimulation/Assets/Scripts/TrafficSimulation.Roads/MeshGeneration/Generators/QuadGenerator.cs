using System.Runtime.InteropServices;
using Sirenix.OdinInspector;
using TrafficSimulation.Roads.MeshGeneration.Data;
using TrafficSimulation.Roads.MeshGeneration.Graph;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace TrafficSimulation.Roads.MeshGeneration.Generators;

[Serializable]
public sealed class QuadGenerator : MeshGenerator {
    [SerializeField, MinValue(0.001f)] private float m_Width = 1.0f;
    [SerializeField, MinValue(0.001f)] private float m_Length = 1.0f;
    [SerializeField] private bool m_Centered = true;
    [SerializeField] private Vector3 m_Normal = Vector3.up;

    public override void GetCounts(in MeshGenerationContext context, out int vertexCount, out int indexCount) {
        vertexCount = 4;
        indexCount = 6;
    }

    public override JobHandle ScheduleFill(in MeshGenerationContext context, in MeshBufferSlice bufferSlice, JobHandle dependency) {
        return new QuadFillJob {
            Width = m_Width,
            Length = m_Length,
            Centered = m_Centered,
            Normal = m_Normal.normalized,
            LocalToWorld = context.LocalToWorld,
            BufferSlice = bufferSlice,
        }.Schedule(dependency);
    }

    [BurstCompile]
    private struct QuadFillJob : IJob {
        public float Width;
        public float Length;
        [MarshalAs(UnmanagedType.U1)]
        public bool Centered;
        public float3 Normal;
        public float4x4 LocalToWorld;
        public MeshBufferSlice BufferSlice;

        public void Execute() {
            var tangent = math.normalize(math.cross(Normal, math.abs(Normal.y) < 0.99f ? new float3(0, 1, 0) : new float3(1, 0, 0)));
            var bitangent = math.cross(Normal, tangent);

            var halfSize = new float3(Width * 0.5f, 0, Length * 0.5f);
            var offset = Centered ? float3.zero : new float3(0, 0, Length * 0.5f);

            var v0 = -tangent * halfSize.x - bitangent * halfSize.z + offset;
            var v1 = tangent * halfSize.x - bitangent * halfSize.z + offset;
            var v2 = tangent * halfSize.x + bitangent * halfSize.z + offset;
            var v3 = -tangent * halfSize.x + bitangent * halfSize.z + offset;

            var vertices = BufferSlice.GetVertices();
            vertices[0] = new MeshVertex {
                Position = math.transform(LocalToWorld, v0),
                Normal = math.mul((float3x3)LocalToWorld, Normal),
                UV = new float2(0, 0),
            };
            vertices[1] = new MeshVertex {
                Position = math.transform(LocalToWorld, v1),
                Normal = math.mul((float3x3)LocalToWorld, Normal),
                UV = new float2(1, 0),
            };
            vertices[2] = new MeshVertex {
                Position = math.transform(LocalToWorld, v2),
                Normal = math.mul((float3x3)LocalToWorld, Normal),
                UV = new float2(1, 1),
            };
            vertices[3] = new MeshVertex {
                Position = math.transform(LocalToWorld, v3),
                Normal = math.mul((float3x3)LocalToWorld, Normal),
                UV = new float2(0, 1),
            };

            var indices = BufferSlice.GetIndices();
            indices[0] = 0;
            indices[1] = 1;
            indices[2] = 2;
            indices[3] = 2;
            indices[4] = 3;
            indices[5] = 0;
        }
    }
}
