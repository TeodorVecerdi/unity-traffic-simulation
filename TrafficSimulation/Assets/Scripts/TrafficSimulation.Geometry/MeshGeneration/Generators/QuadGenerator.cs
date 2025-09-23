using Sirenix.OdinInspector;
using TrafficSimulation.Geometry.MeshGeneration.Data;
using TrafficSimulation.Geometry.MeshGeneration.Graph;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace TrafficSimulation.Geometry.MeshGeneration.Generators;

[Serializable]
public sealed class QuadGenerator : MeshGenerator {
    [SerializeField, MinValue(0.001f)] private float m_Width = 1.0f;
    [SerializeField, MinValue(0.001f)] private float m_Length = 1.0f;
    [SerializeField] private Vector3 m_Normal = Vector3.up;

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
            var tangent = math.normalize(math.cross(Normal, math.abs(Normal.y) < 0.99f ? new float3(0, 1, 0) : new float3(1, 0, 0)));
            var bitangent = math.cross(Normal, tangent);

            var halfSize = new float3(Width * 0.5f, 0, Length * 0.5f);

            var v0 = -tangent * halfSize.x - bitangent * halfSize.z;
            var v1 = tangent * halfSize.x - bitangent * halfSize.z;
            var v2 = tangent * halfSize.x + bitangent * halfSize.z;
            var v3 = -tangent * halfSize.x + bitangent * halfSize.z;

            var vertices = BufferSlice.GetVertices();
            vertices[0] = new MeshVertex {
                Position = v0,
                Normal = Normal,
                UV = new float2(0, 0),
            };
            vertices[1] = new MeshVertex {
                Position = v1,
                Normal = Normal,
                UV = new float2(1, 0),
            };
            vertices[2] = new MeshVertex {
                Position = v2,
                Normal = Normal,
                UV = new float2(1, 1),
            };
            vertices[3] = new MeshVertex {
                Position = v3,
                Normal = Normal,
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
