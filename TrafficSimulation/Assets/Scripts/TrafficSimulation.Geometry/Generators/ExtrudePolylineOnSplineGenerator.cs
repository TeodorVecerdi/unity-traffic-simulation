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
    private NativeArray<float2> m_PolylineSegmentDirections;
    private NativeArray<bool> m_PolylineEmitEdges;

    public override bool Validate() {
        return m_Polyline != null
            && m_Polyline.PointCount >= 2
            && m_SplineContainer != null
            && m_SplineContainer.Spline.Count >= 2
            && m_MaxError > 0.0f
            && m_MaxStep > 0.0f
            && math.any(m_InitialUp != float3.zero);
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

        var (points, emitEdges) = m_Polyline.GetGeometry();
        (m_CachedVertexCount, m_CachedIndexCount, _) = PolylineExtrusionHelper.CalculateExtrusionCounts(points.Count, frames.Length, false, emitEdges);
        vertexCount = m_CachedVertexCount;
        indexCount = m_CachedIndexCount;

        m_PolylinePoints = new NativeArray<float3>(points.ToArray(), Allocator.TempJob);
        m_PolylineEmitEdges = new NativeArray<bool>(emitEdges.ToArray(), Allocator.TempJob);
        m_PolylineSegmentDirections = new NativeArray<float2>(points.Count, Allocator.TempJob);
    }

    public override JobHandle ScheduleFill(in MeshGenerationContext context, in MeshBufferSlice bufferSlice, JobHandle dependency) {
        var job = new ExtrudeJob {
            Frames = m_CachedFrames,
            PolylinePoints = m_PolylinePoints,
            PolylineEmitEdges = m_PolylineEmitEdges,
            PolylineSegmentDirections = m_PolylineSegmentDirections,
            BufferSlice = bufferSlice,
            LocalToWorld = m_SplineContainer.transform.localToWorldMatrix,
        };
        return job.Schedule(dependency);
    }

    public void Dispose() {
        m_CachedFrames.Dispose();
        m_PolylinePoints.Dispose();
        m_PolylineEmitEdges.Dispose();
        m_PolylineSegmentDirections.Dispose();
    }

    [BurstCompile]
    private struct ExtrudeJob : IJob {
        [ReadOnly] public NativeArray<Frame> Frames;
        [ReadOnly] public NativeArray<float3> PolylinePoints;
        [ReadOnly] public NativeArray<bool> PolylineEmitEdges;
        public NativeArray<float2> PolylineSegmentDirections;
        public MeshBufferSlice BufferSlice;
        public float4x4 LocalToWorld;

        public void Execute() {
            var vertices = BufferSlice.GetVertices();
            var indices = BufferSlice.GetIndices();
            var indexOffset = 0;

            var ringSize = PolylinePoints.Length;
            var ringCount = Frames.Length;

            // Build 2D segment directions
            for (var i = 0; i < ringSize - 1; i++) {
                var d = PolylinePoints[i + 1].xy - PolylinePoints[i].xy;
                var lsq = math.lengthsq(d);
                PolylineSegmentDirections[i] = lsq > 1e-8f ? d / math.sqrt(lsq) : new float2(1, 0);
            }

            // Normal matrix (handles non-uniform scale)
            var normalMatrix = math.transpose(math.inverse((float3x3)LocalToWorld));

            for (var i = 0; i < ringCount; i++) {
                var frame = Frames[i];
                var vertexOffset = i * ringSize;

                // Write vertices
                for (var j = 0; j < ringSize; j++) {
                    // Position
                    var point = PolylinePoints[j].xy;
                    var position = frame.Position + point.x * frame.Binormal + point.y * frame.Normal;
                    position.w = 1f;
                    var worldPosition = math.mul(LocalToWorld, position).xyz;

                    // Normal
                    float2 dir2D;
                    if (j == 0) {
                        dir2D = PolylineSegmentDirections[0];
                    } else if (j == ringSize - 1) {
                        dir2D = PolylineSegmentDirections[ringSize - 2];
                    } else {
                        var sum = PolylineSegmentDirections[j - 1] + PolylineSegmentDirections[j];
                        dir2D = math.lengthsq(sum) > 1e-8f
                            ? math.normalize(sum)
                            : PolylineSegmentDirections[j];
                    }

                    var along  = frame.Tangent.xyz;
                    var across = frame.Binormal.xyz * dir2D.x + frame.Normal.xyz * dir2D.y;

                    // Build normal in local space, then transform with normal matrix
                    var localNormal = math.normalize(math.cross(along, across));
                    var normal = math.normalize(math.mul(normalMatrix, localNormal));

                    vertices[vertexOffset + j] = new MeshVertex {
                        Position = worldPosition,
                        Normal = normal,
                        UV = new float2((float)j / (ringSize - 1), (float)i / (ringCount - 1)),
                    };
                }

                // Link rings with quads
                if (i == 0)
                    continue;

                var previousVertexOffset = vertexOffset - ringSize;
                for (var j = 0; j < ringSize - 1; j++) {
                    if (!PolylineEmitEdges[j])
                        continue;
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
