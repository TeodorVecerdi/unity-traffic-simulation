using Sirenix.OdinInspector;
using TrafficSimulation.Geometry.Build;
using TrafficSimulation.Geometry.Data;
using TrafficSimulation.Geometry.Helpers;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using ReadOnlyAttribute = Unity.Collections.ReadOnlyAttribute;

namespace TrafficSimulation.Geometry.Generators;

[Serializable]
public sealed class ExtrudePolylineOnSplineGenerator : MeshGenerator {
    [SerializeField, Required] private Polyline m_Polyline = null!;
    [SerializeField, Required] private SplineContainer m_SplineContainer = null!;
    [Space]
    [SerializeField] private float m_MaxError = 0.2f;
    [SerializeField] private bool m_WindingClockwise;

    public override bool Validate() {
        return m_Polyline != null
            && m_Polyline.Points.Count >= 2
            && m_SplineContainer != null
            && m_SplineContainer.Spline.Count >= 2
            && m_MaxError > 0.0f;
    }

    public override void EstimateCounts(in MeshGenerationContext context, out int vertexCount, out int indexCount) {
        var rings = (int)(m_SplineContainer.Spline.GetLength() / 10.0f);
        var ringSize = math.max(2, m_Polyline.Points.Count);
        vertexCount = rings * ringSize;
        indexCount = (rings - 1) * (ringSize - 1) * 6;
    }

    public override JobHandle ScheduleGenerate(in MeshGenerationContext context, GeometryWriter writer, JobHandle dependency) {
        var frameList = new NativeList<Frame>(Allocator.Temp);
        SplineSampler.Sample(m_SplineContainer.Spline, m_MaxError, ref frameList);

        var frames = new NativeArray<Frame>(frameList.Length, Allocator.TempJob);
        frames.CopyFrom(frameList.AsArray());
        frameList.Dispose();

        var points = m_Polyline.GetGeometry();
        var polylinePoints = new NativeArray<float3>(points.Positions.ToArray(), Allocator.TempJob);
        var emitEdges = new NativeArray<bool>(points.EmitEdges.ToArray(), Allocator.TempJob);
        var segmentDirections = new NativeArray<float2>(polylinePoints.Length, Allocator.TempJob);

        var job = new ExtrudeJob {
            Frames = frames,
            PolylinePoints = polylinePoints,
            PolylineEmitEdges = emitEdges,
            PolylineSegmentDirections = segmentDirections,
            LocalToWorld = m_SplineContainer.transform.localToWorldMatrix,
            Writer = writer,
            WindingClockwise = m_WindingClockwise,
        };

        return job.Schedule(dependency);
    }

    [BurstCompile]
    private struct ExtrudeJob : IJob {
        [DeallocateOnJobCompletion, ReadOnly] public NativeArray<Frame> Frames;
        [DeallocateOnJobCompletion, ReadOnly] public NativeArray<float3> PolylinePoints;
        [DeallocateOnJobCompletion, ReadOnly] public NativeArray<bool> PolylineEmitEdges;
        [DeallocateOnJobCompletion] public NativeArray<float2> PolylineSegmentDirections;
        public float4x4 LocalToWorld;
        public GeometryWriter Writer;
        public bool WindingClockwise;

        public void Execute() {
            var ringSize = PolylinePoints.Length;
            var ringCount = Frames.Length;

            // 2D segment directions (for normals)
            for (var i = 0; i < ringSize - 1; i++) {
                // WindingClockwise swaps direction to maintain consistent normal orientation
                var direction = WindingClockwise
                    ? PolylinePoints[i].xy - PolylinePoints[i + 1].xy
                    : PolylinePoints[i + 1].xy - PolylinePoints[i].xy;
                PolylineSegmentDirections[i] = math.normalizesafe(direction, new float2(1, 0));
            }

            var normalMatrix = math.transpose(math.inverse((float3x3)LocalToWorld));

            for (var i = 0; i < ringCount; i++) {
                var frame = Frames[i];
                var rowBase = Writer.Vertices.Length; // base for this ring

                // Write ring vertices
                for (var j = 0; j < ringSize; j++) {
                    var point = PolylinePoints[j].xy;
                    var position = frame.Position + point.x * frame.Binormal + point.y * frame.Normal;
                    position.w = 1.0f;
                    var worldPosition = math.mul(LocalToWorld, position).xyz;

                    float2 dir2D;
                    if (j == 0) {
                        dir2D = PolylineSegmentDirections[0];
                    } else if (j == ringSize - 1) {
                        dir2D = PolylineSegmentDirections[ringSize - 2];
                    } else {
                        var sum = PolylineSegmentDirections[j - 1] + PolylineSegmentDirections[j];
                        dir2D = math.normalizesafe(sum, PolylineSegmentDirections[j]);
                    }

                    var along = frame.Tangent.xyz;
                    var across = frame.Binormal.xyz * dir2D.x + frame.Normal.xyz * dir2D.y;
                    var localNormal = math.normalize(math.cross(along, across));
                    var normal = math.normalize(math.mul(normalMatrix, localNormal));

                    var uv = new float2(
                        (float)j / math.max(1, ringSize - 1),
                        (float)i / math.max(1, ringCount - 1)
                    );

                    Writer.WriteVertex(new MeshVertex {
                        Position = worldPosition,
                        Normal = normal,
                        UV = uv,
                    });
                }

                // Link rings with quads
                if (i == 0)
                    continue;

                var prevBase = rowBase - ringSize;
                if (WindingClockwise) {
                    Writer.WriteRingStitch(prevBase, rowBase, ringSize, closed: false, PolylineEmitEdges);
                } else {
                    Writer.WriteRingStitchCCW(prevBase, rowBase, ringSize, closed: false, PolylineEmitEdges);
                }
            }
        }
    }
}
