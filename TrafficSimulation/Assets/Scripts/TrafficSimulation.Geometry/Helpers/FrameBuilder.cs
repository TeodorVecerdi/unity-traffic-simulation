using TrafficSimulation.Geometry.Data;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

namespace TrafficSimulation.Geometry.Helpers;

[BurstCompile]
public static class FrameBuilder {
    [BurstCompile]
    public static void BuildFramesFromPolyline(in NativeArray<float4> positions, in NativeArray<float4> tangents, in float3 initialUp, bool fixedUp, ref NativeArray<Frame> frames) {
        Hint.Assume(positions.Length == tangents.Length);
        Hint.Assume(positions.Length == frames.Length);

        var n = positions.Length;
        if (n < 2)
            return;

        // Initialize first frame
        var firstTangent = math.normalizesafe(tangents[0].xyz, new float3(0.0f, 0.0f, 1.0f));
        GeometryUtils.BuildOrthonormalBasis(in firstTangent, in initialUp, out var firstRight, out var firstUp);

        frames[0] = new Frame {
            Position = new float4(positions[0].xyz, 1.0f),
            Tangent = new float4(firstTangent, 0.0f),
            Normal = new float4(firstUp, 0.0f),
            Binormal = new float4(firstRight, 0.0f),
        };

        var previousTangent = firstTangent;
        var previousUp = firstUp;

        for (var i = 1; i < n; i++) {
            var tangent = math.normalizesafe(tangents[i].xyz, previousTangent);
            float3 right;
            float3 up;

            if (fixedUp) {
                GeometryUtils.BuildOrthonormalBasis(in tangent, in initialUp, out right, out up);
            } else {
                var axis = math.cross(previousTangent, tangent);
                var sinTheta = math.length(axis);
                var cosTheta = math.clamp(math.dot(previousTangent, tangent), -1.0f, 1.0f);

                if (sinTheta <= math.EPSILON) {
                    up = cosTheta < 0.0f ? -previousUp : previousUp;
                } else {
                    var axisN = axis / sinTheta;
                    var oneMinusC = 1.0f - cosTheta;

                    up = previousUp * cosTheta
                       + math.cross(axisN, previousUp) * sinTheta
                       + axisN * math.dot(axisN, previousUp) * oneMinusC;

                    up = math.normalizesafe(up - tangent * math.dot(up, tangent), up);
                }

                right = math.normalize(math.cross(up, tangent));
            }

            frames[i] = new Frame {
                Position = new float4(positions[i].xyz, 1.0f),
                Tangent = new float4(tangent, 0.0f),
                Normal = new float4(up, 0.0f),
                Binormal = new float4(right, 0.0f),
            };

            previousTangent = tangent;
            previousUp = up;
        }
    }
}
