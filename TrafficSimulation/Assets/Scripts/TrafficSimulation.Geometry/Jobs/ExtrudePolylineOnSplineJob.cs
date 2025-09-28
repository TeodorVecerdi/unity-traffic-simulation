using TrafficSimulation.Geometry.Build;
using TrafficSimulation.Geometry.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace TrafficSimulation.Geometry.Jobs;

[BurstCompile]
public struct ExtrudePolylineOnSplineJob : IJob {
    [ReadOnly] public NativeArray<Frame> Frames;
    [ReadOnly] public NativeArray<float3> PolylinePoints;
    [ReadOnly] public NativeArray<bool> PolylineEmitEdges;
    public NativeArray<float2> PolylineSegmentDirections;
    public float4x4 LocalToWorld;
    public GeometryWriter Writer;
    public WindingOrder WindingOrder;

    public void Execute() {
        var ringSize = PolylinePoints.Length;
        var ringCount = Frames.Length;

        // 2D segment directions (for normals)
        for (var i = 0; i < ringSize - 1; i++) {
            // WindingOrder.Clockwise swaps direction to maintain consistent normal orientation
            var direction = WindingOrder is WindingOrder.Clockwise
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
            if (WindingOrder is WindingOrder.Clockwise) {
                Writer.WriteRingStitch(prevBase, rowBase, ringSize, closed: false, PolylineEmitEdges);
            } else {
                Writer.WriteRingStitchCCW(prevBase, rowBase, ringSize, closed: false, PolylineEmitEdges);
            }
        }
    }
}
