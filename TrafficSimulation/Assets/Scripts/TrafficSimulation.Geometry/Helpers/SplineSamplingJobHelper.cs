using TrafficSimulation.Geometry.Data;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Splines;

namespace TrafficSimulation.Geometry.Helpers;

public static class SplineSamplingJobHelper {
    public static NativeArray<Frame> SampleSpline(Spline spline, float maxError, Allocator allocator) {
        var frameList = new NativeList<Frame>(Allocator.Temp);
        try {
            SplineSampler.Sample(spline, maxError, ref frameList);
            var frames = new NativeArray<Frame>(frameList.Length, allocator);
            frames.CopyFrom(frameList.AsArray());
            return frames;
        } finally {
            frameList.Dispose();
        }
    }

    public static NativeArray<Frame> SampleFramesForRibbon(Spline spline, Allocator allocator, float maxError, float onLength = 0.0f, float offLength = 0.0f, float phase = 0.0f) {
        var frameList = new NativeList<Frame>(Allocator.Temp);
        if (onLength <= 0.0f || offLength <= 0.0f) {
            SplineSampler.Sample(spline, maxError, ref frameList);
        } else {
            SamplePreciseFrames(spline, onLength, offLength, phase, ref frameList);
        }

        var frames = new NativeArray<Frame>(frameList.Length, allocator);
        frames.CopyFrom(frameList.AsArray());
        frameList.Dispose();
        return frames;
    }

    private static void SamplePreciseFrames(Spline spline, float onLength, float offLength, float phase, ref NativeList<Frame> frameList) {
        var totalLength = spline.GetLength();
        var cycleLength = onLength + offLength;

        // Collect transition points in arc-length space
        var ts = new NativeList<float>(Allocator.Temp);

        // Find first cycle start after (Phase mod cycleLength), cover [-cycleLength, totalLength + cycleLength]
        var effectivePhase = phase % cycleLength;
        if (effectivePhase < 0.0f)
            effectivePhase += cycleLength;
        var s = effectivePhase - cycleLength; // start one cycle before to cover negatives

        while (s < totalLength + cycleLength) {
            var onEnd = s + onLength;
            var offEnd = onEnd + offLength;

            // Clamp to [0, totalLength] and add if within range
            if (s >= 0.0f && s <= totalLength) {
                ts.Add(s / totalLength);
            }

            if (onEnd >= 0.0f && onEnd <= totalLength) {
                ts.Add(onEnd / totalLength);
            }

            if (offEnd >= 0.0f && offEnd <= totalLength) {
                ts.Add(offEnd / totalLength);
            }

            s += cycleLength;
        }

        // Ensure the domain endpoints are sampled
        ts.Add(0.0f);
        ts.Add(1.0f - math.EPSILON);

        // Sort unique ts and evaluate to build precise frames
        ts.Sort();
        RemoveDuplicates(ref ts);

        frameList.Resize(ts.Length, NativeArrayOptions.ClearMemory);

        // Sample frames
        for (var i = 0; i < ts.Length; i++) {
            var t = ts[i];
            var pos = spline.EvaluatePosition(t);
            var tangent = spline.EvaluateTangent(t);
            tangent = math.normalizesafe(tangent, new float3(0.0f, 0.0f, 1.0f));

            GeometryUtils.BuildOrthonormalBasis(in tangent, math.up(), out var right, out var up);

            frameList[i] = new Frame {
                Position = new float4(pos, 1.0f),
                Tangent = new float4(tangent, 0.0f),
                Normal = new float4(up, 0.0f),
                Binormal = new float4(right, 0.0f),
            };
        }

        ts.Dispose();
    }

    private static void RemoveDuplicates(ref NativeList<float> list, float epsilon = 1e-5f) {
        if (list.Length <= 1) return;

        var write = 1;
        for (var read = 1; read < list.Length; read++) {
            if (math.abs(list[read] - list[write - 1]) > epsilon) {
                list[write] = list[read];
                write++;
            }
        }

        list.Length = write;
    }
}
