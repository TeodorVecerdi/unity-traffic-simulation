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
    [SerializeField] private float m_MaxStep = 50.0f;
    [SerializeField] private bool m_FixedUp = true;
    [SerializeField] private float3 m_InitialUp = math.up();

    public override bool Validate() {
        return m_Polyline != null
            && m_Polyline.Points.Count >= 2
            && m_SplineContainer != null
            && m_SplineContainer.Spline.Count >= 2
            && m_MaxError > 0.0f
            && m_MaxStep > 0.0f
            && math.lengthsq(m_InitialUp) > 1e-6f;
    }

    public override void EstimateCounts(in MeshGenerationContext context, out int vertexCount, out int indexCount) {
        var rings = math.max(16, (int)math.ceil(100f / m_MaxStep));
        var ringSize = math.max(2, m_Polyline.Points.Count);
        vertexCount = rings * ringSize;
        indexCount = (rings - 1) * (ringSize - 1) * 6;
    }

    public override JobHandle ScheduleGenerate(in MeshGenerationContext context, GeometryWriter writer, JobHandle dependency) {
        var positions = new NativeList<float4>(Allocator.TempJob);
        var tangents = new NativeList<float4>(Allocator.TempJob);
        SplineSamplingUtility.AdaptiveSample(m_SplineContainer.Spline, ref positions, ref tangents, m_MaxError, m_MaxStep);

        var frames = new NativeArray<Frame>(positions.Length, Allocator.TempJob);
        FrameBuilder.BuildFramesFromPolyline(positions.AsArray(), tangents.AsArray(), math.normalize(m_InitialUp), m_FixedUp, ref frames);

        positions.Dispose();
        tangents.Dispose();

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

        public void Execute() {
            var ringSize = PolylinePoints.Length;
            var ringCount = Frames.Length;

            // 2D segment directions (for normals)
            for (var i = 0; i < ringSize - 1; i++) {
                var direction = PolylinePoints[i + 1].xy - PolylinePoints[i].xy;
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
                    position.w = 1f;
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
                for (var j = 0; j < ringSize - 1; j++) {
                    if (!PolylineEmitEdges[j])
                        continue;
                    Writer.WriteRingStitchCCW(prevBase, rowBase, ringSize, closed: false);
                }
            }
        }
    }
}
