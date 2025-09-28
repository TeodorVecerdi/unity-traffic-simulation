using TrafficSimulation.Geometry.Build;
using TrafficSimulation.Geometry.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace TrafficSimulation.Geometry.Jobs;

[BurstCompile]
public struct RibbonStripJob : IJob {
    [ReadOnly] public NativeArray<Frame> Frames;

    // Geometry
    public float Width; // meters
    public bool WindingClockwise;
    public float3 LocalOffset;

    // Dash pattern
    public float OnLength; // meters ON per cycle
    public float OffLength; // meters OFF per cycle
    public float Phase; // meters phase offset

    // Transform
    public float4x4 LocalToWorld;

    // Output
    public GeometryWriter Writer;

    private float3 ComputeLocalOffset(in Frame frame) {
        return frame.Binormal.xyz * LocalOffset.x + frame.Normal.xyz * LocalOffset.y + frame.Tangent.xyz * LocalOffset.z;
    }

    public void Execute() {
        var frameCount = Frames.Length;
        if (frameCount < 2) return;

        var halfWidth = 0.5f * Width;

        var hasGaps = OnLength > 0.0f && OffLength > 0.0f;
        var cycleLength = OnLength + OffLength;

        var sPrev = 0.0f; // accumulated distance up to frame i-1
        var inRun = false; // currently inside an ON run

        var previousLeftIndex = -1; // previous strip pair indices
        var previousRightIndex = -1; // previous strip pair indices

        // Normal matrix for transforming local->world normals
        var nrmM = math.transpose(math.inverse((float3x3)LocalToWorld));

        for (var i = 1; i < frameCount; i++) {
            var f0 = Frames[i - 1];
            var f1 = Frames[i];

            var p0 = f0.Position.xyz;
            var p1 = f1.Position.xyz;
            var segmentLength = math.distance(p0, p1);

            // Decide if this segment is ON or OFF (sample pattern at segment midpoint)
            var on = true;
            if (hasGaps) {
                var midS = sPrev + 0.5f * segmentLength + Phase;
                var k = math.floor(midS / cycleLength);
                var frac = midS - k * cycleLength; // positive modulo
                on = frac < OnLength;
            }

            if (!on) {
                // End/skip run
                inRun = false;
                sPrev += segmentLength;
                continue;
            }

            if (!inRun) {
                // Start a new ON run: seed first pair at frame i-1
                inRun = true;

                var n0 = f0.Normal.xyz;
                var r0 = f0.Binormal.xyz;

                var offset0 = ComputeLocalOffset(in f0);
                var left0 = p0 + offset0 - r0 * halfWidth;
                var right0 = p0 + offset0 + r0 * halfWidth;

                var vL0 = new MeshVertex {
                    Position = math.mul(LocalToWorld, new float4(left0, 1.0f)).xyz,
                    Normal = math.normalize(math.mul(nrmM, n0)),
                    UV = new float2(0.0f, 0.0f),
                };
                var vR0 = new MeshVertex {
                    Position = math.mul(LocalToWorld, new float4(right0, 1.0f)).xyz,
                    Normal = math.normalize(math.mul(nrmM, n0)),
                    UV = new float2(1.0f, 0.0f),
                };

                // Append seed pair
                previousLeftIndex = Writer.Vertices.Length;
                Writer.WriteVertex(vL0);
                previousRightIndex = Writer.Vertices.Length;
                Writer.WriteVertex(vR0);
            }

            var n1 = f1.Normal.xyz;
            var r1 = f1.Binormal.xyz;

            var offset1 = ComputeLocalOffset(in f1);
            var left1 = p1 + offset1 - r1 * halfWidth;
            var right1 = p1 + offset1 + r1 * halfWidth;

            var leftVertex = new MeshVertex {
                Position = math.mul(LocalToWorld, new float4(left1, 1.0f)).xyz,
                Normal = math.normalize(math.mul(nrmM, n1)),
                UV = new float2(0.0f, 1.0f),
            };
            var rightVertex = new MeshVertex {
                Position = math.mul(LocalToWorld, new float4(right1, 1.0f)).xyz,
                Normal = math.normalize(math.mul(nrmM, n1)),
                UV = new float2(1.0f, 1.0f),
            };

            if (WindingClockwise) {
                Writer.WriteStripStep(leftVertex, rightVertex, previousLeftIndex, previousRightIndex, out previousLeftIndex, out previousRightIndex);
            } else {
                Writer.WriteStripStepCCW(leftVertex, rightVertex, previousLeftIndex, previousRightIndex, out previousLeftIndex, out previousRightIndex);
            }

            sPrev += segmentLength;
        }
    }
}
