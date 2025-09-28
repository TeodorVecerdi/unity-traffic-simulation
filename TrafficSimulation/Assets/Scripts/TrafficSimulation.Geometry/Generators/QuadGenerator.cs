using Sirenix.OdinInspector;
using TrafficSimulation.Geometry.Build;
using TrafficSimulation.Geometry.Data;
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
    [SerializeField] private bool m_WindingClockwise = true;

    public override bool Validate() {
        return m_Width > 0.0f && m_Length > 0.0f && m_Normal != Vector3.zero;
    }

    public override void EstimateCounts(in MeshGenerationContext context, out int vertexCount, out int indexCount) {
        vertexCount = 4;
        indexCount = 6;
    }

    public override JobHandle ScheduleGenerate(in MeshGenerationContext context, List<GeometryWriter> writers, JobHandle dependency) {
        return new QuadFillJob {
            Width = m_Width,
            Length = m_Length,
            Normal = m_Normal.normalized,
            WindingClockwise = m_WindingClockwise,
            Writer = writers[0],
        }.Schedule(dependency);
    }

    [BurstCompile]
    private struct QuadFillJob : IJob {
        public float Width;
        public float Length;
        public float3 Normal;
        public bool WindingClockwise;
        public GeometryWriter Writer;

        public void Execute() {
            var pivot = math.abs(Normal.y) < 0.99f ? new float3(0.0f, 1.0f, 0.0f) : new float3(1.0f, 0.0f, 0.0f);
            var tangent = math.normalize(math.cross(Normal, pivot));
            var bitangent = math.cross(Normal, tangent);

            var halfSize = new float3(Width * 0.5f, 0.0f, Length * 0.5f);

            var vertex0 = new MeshVertex { Position = -tangent * halfSize.x - bitangent * halfSize.z, Normal = Normal, UV = new float2(0.0f, 0.0f) };
            var vertex1 = new MeshVertex { Position = +tangent * halfSize.x - bitangent * halfSize.z, Normal = Normal, UV = new float2(1.0f, 0.0f) };
            var vertex2 = new MeshVertex { Position = +tangent * halfSize.x + bitangent * halfSize.z, Normal = Normal, UV = new float2(1.0f, 1.0f) };
            var vertex3 = new MeshVertex { Position = -tangent * halfSize.x + bitangent * halfSize.z, Normal = Normal, UV = new float2(0.0f, 1.0f) };

            if (WindingClockwise) {
                Writer.WriteQuad(vertex0, vertex1, vertex2, vertex3);
            } else {
                Writer.WriteQuadCCW(vertex0, vertex1, vertex2, vertex3);
            }
        }
    }
}
