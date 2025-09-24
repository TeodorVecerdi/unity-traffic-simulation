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
        if (n < 2) return;

        // Initialize first frame
        var p0 = positions[0];
        var t0 = math.normalizesafe(tangents[0].xyz, new float3(0.0f, 0.0f, 1.0f));
        GeometryUtils.BuildOrthonormalBasis(in t0, in initialUp, out var right0, out var up0);

        frames[0] = new Frame {
            Position = new float4(p0.x, p0.y, p0.z, 1.0f),
            Tangent = new float4(t0, 0.0f),
            Normal = new float4(up0, 0.0f),
            Binormal = new float4(right0, 0.0f),
        };

        var prevT = t0;
        var prevUp = up0;

        for (var i = 1; i < n; i++) {
            var pv = positions[i];
            var tv = tangents[i];
            var p = new float3(pv.x, pv.y, pv.z);
            var t = math.normalizesafe(tv.xyz, prevT);

            float3 right;
            float3 up;

            if (fixedUp) {
                GeometryUtils.BuildOrthonormalBasis(in t, in initialUp, out right, out up);
            } else {
                var axis = math.cross(prevT, t);
                var sinTheta = math.length(axis);
                var cosTheta = math.clamp(math.dot(prevT, t), -1.0f, 1.0f);

                if (sinTheta <= GeometryUtils.Epsilon) {
                    up = cosTheta < 0.0f ? -prevUp : prevUp;
                } else {
                    var axisN = axis / sinTheta;
                    var oneMinusC = 1.0f - cosTheta;

                    up = prevUp * cosTheta
                       + math.cross(axisN, prevUp) * sinTheta
                       + axisN * math.dot(axisN, prevUp) * oneMinusC;

                    up = math.normalizesafe(up - t * math.dot(up, t), up);
                }

                right = math.normalize(math.cross(up, t));
            }

            frames[i] = new Frame {
                Position = new float4(p, 1.0f),
                Tangent = new float4(t, 0.0f),
                Normal = new float4(up, 0.0f),
                Binormal = new float4(right, 0.0f),
            };

            prevT = t;
            prevUp = up;
        }
    }
}
