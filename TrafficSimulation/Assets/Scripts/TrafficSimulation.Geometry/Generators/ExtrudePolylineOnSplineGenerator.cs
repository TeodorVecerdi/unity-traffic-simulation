using Sirenix.OdinInspector;
using TrafficSimulation.Geometry.Data;
using TrafficSimulation.Geometry.Graph;
using TrafficSimulation.Geometry.Helpers;
using TrafficSimulation.Geometry.Splines;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using ReadOnlyAttribute = Unity.Collections.ReadOnlyAttribute;

namespace TrafficSimulation.Geometry.Generators;

[Serializable]
public sealed class ExtrudePolylineOnSplineGenerator : MeshGenerator, IDisposable {
    [SerializeField, Required] private Polyline m_Polyline = null!;
    [SerializeField, Required] private SplineContainer m_SplineContainer = null!;
    [Space]
    [SerializeField] private float m_MaxError = 0.2f;
    [SerializeField] private float m_MaxStep = 50.0f;
    [SerializeField] private bool m_FixedUp = true;
    [SerializeField] private float3 m_InitialUp = math.up();

    private int m_CachedVertexCount = -1;
    private int m_CachedIndexCount = -1;
    private NativeArray<Frame> m_CachedFrames;
    private NativeArray<float3> m_PolylinePoints;

    public override bool Validate() {
        return m_Polyline != null
            && m_Polyline.Points.Count >= 2
            && m_SplineContainer != null
            && m_SplineContainer.Spline.Count >= 2
            && m_MaxError > 0.0f
            && m_MaxStep > 0.0f
            && math.all(m_InitialUp != float3.zero);
    }

    public override void GetCounts(in MeshGenerationContext context, out int vertexCount, out int indexCount) {
        var positions = new NativeList<float4>(Allocator.Temp);
        var tangents = new NativeList<float4>(Allocator.Temp);
        SplineSamplingUtility.AdaptiveSample(m_SplineContainer.Spline, ref positions, ref tangents, m_MaxError, m_MaxStep);

        var frames = new NativeArray<Frame>(positions.Length, Allocator.TempJob);
        var positionsArray = positions.AsArray();
        var tangentsArray = tangents.AsArray();
        FrameBuilder.BuildFramesFromPolyline(in positionsArray, in tangentsArray, m_InitialUp, m_FixedUp, ref frames);
        m_CachedFrames = frames;

        positions.Dispose();
        tangents.Dispose();

        (m_CachedVertexCount, m_CachedIndexCount, _) = PolylineExtrusionHelper.CalculateExtrusionCounts(m_Polyline.Points.Count, frames.Length, false);
        vertexCount = m_CachedVertexCount;
        indexCount = m_CachedIndexCount;

        m_PolylinePoints = new NativeArray<float3>(m_Polyline.Points.ToArray(), Allocator.TempJob);
    }

    public override JobHandle ScheduleFill(in MeshGenerationContext context, in MeshBufferSlice bufferSlice, JobHandle dependency) {
        var job = new ExtrudeJob {
            Frames = m_CachedFrames,
            PolylinePoints = m_PolylinePoints,
            BufferSlice = bufferSlice,
            LocalToWorld = m_SplineContainer.transform.localToWorldMatrix,
        };
        return job.Schedule(dependency);
    }

    public void Dispose() {
        m_CachedFrames.Dispose();
        m_PolylinePoints.Dispose();
    }

    [BurstCompile]
    private struct ExtrudeJob : IJob {
        [ReadOnly] public NativeArray<Frame> Frames;
        [ReadOnly] public NativeArray<float3> PolylinePoints;
        public MeshBufferSlice BufferSlice;
        public float4x4 LocalToWorld;

        public void Execute() {
            var vertices = BufferSlice.GetVertices();
            var indices = BufferSlice.GetIndices();
            var indexOffset = 0;

            var ringSize = PolylinePoints.Length;
            var ringCount = Frames.Length;

            for (var i = 0; i < ringCount; i++) {
                var frame = Frames[i];
                var vertexOffset = i * ringSize;

                // Write vertices
                for (var j = 0; j < ringSize; j++) {
                    TransformPoint(PolylinePoints[j].xy, frame, out var transformedPoint);
                    transformedPoint.w = 1.0f;
                    transformedPoint = math.mul(LocalToWorld, transformedPoint);
                    var normalMatrix = math.transpose(math.inverse((float3x3)LocalToWorld));
                    var normal = math.normalize(math.mul(normalMatrix, frame.Normal.xyz));
                    vertices[vertexOffset + j] = new MeshVertex {
                        Position = transformedPoint.xyz,
                        Normal = normal,
                        UV = new float2((float)j / (ringSize - 1), (float)i / (ringCount - 1)),
                    };
                }

                // Link rings with quads
                if (i == 0)
                    continue;

                var previousVertexOffset = vertexOffset - ringSize;
                for (var j = 0; j < ringSize - 1; j++) {
                    var v0 = previousVertexOffset + j;
                    var v1 = previousVertexOffset + j + 1;
                    var v2 = vertexOffset + j;
                    var v3 = vertexOffset + j + 1;

                    indices[indexOffset + 0] = v0;
                    indices[indexOffset + 1] = v2;
                    indices[indexOffset + 2] = v1;
                    indices[indexOffset + 3] = v1;
                    indices[indexOffset + 4] = v2;
                    indices[indexOffset + 5] = v3;
                    indexOffset += 6;
                }
            }
        }

        private static void TransformPoint(in float2 point, in Frame frame, out float4 transformedPoint) {
            transformedPoint = frame.Position + point.x * frame.Binormal + point.y * frame.Normal;
        }
    }
}
