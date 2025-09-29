using TrafficSimulation.Core.Maths;
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
            SamplePreciseFrames(spline, onLength, offLength, phase, maxError, ref frameList);
        }

        var frames = new NativeArray<Frame>(frameList.Length, allocator);
        frames.CopyFrom(frameList.AsArray());
        frameList.Dispose();
        return frames;
    }

    private static void SamplePreciseFrames(Spline spline, float onLength, float offLength, float phase, float maxError, ref NativeList<Frame> frameList) {
        var totalLength = spline.GetLength();
        if (totalLength <= math.EPSILON) {
            // Degenerate spline; fall back to default sampler
            SplineSampler.Sample(spline, maxError, ref frameList);
            return;
        }

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

        // If maxError <= 0, skip refinement
        NativeList<float> refinedTs;
        if (maxError <= 0.0f) {
            refinedTs = ts;
            ts = default;
        } else {
            refinedTs = new NativeList<float>(Allocator.Temp);
            if (ts.Length > 0)
                refinedTs.Add(ts[0]);

            // Subdivide each interval adaptively by chord error
            for (var i = 0; i < ts.Length - 1; i++) {
                SubdivideIntervalByError(spline, ts[i], ts[i + 1], maxError, ref refinedTs);
            }
        }

        // Build frames from refined T samples
        frameList.Resize(refinedTs.Length, NativeArrayOptions.ClearMemory);
        for (var i = 0; i < refinedTs.Length; i++) {
            var t = refinedTs[i];
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

        if (refinedTs.IsCreated) refinedTs.Dispose();
        if (ts.IsCreated) ts.Dispose();
    }

    // Iteratively subdivide [t0, t1] until the max chord error is below threshold. Appends the final t1.
    private static void SubdivideIntervalByError(Spline spline, float t0, float t1, float maxError, ref NativeList<float> outTs) {
        // If no refinement requested, just append t1
        if (maxError <= 0.0f) {
            outTs.Add(t1);
            return;
        }

        // Limit recursion/iteration to avoid pathological cases
        const int kMaxDepth = 16;

        // Use a small stack to maintain order (LIFO). Push right first so left is processed first.
        var stack = new NativeList<float4>(Allocator.Temp); // store (t0, t1, depth, unused)
        stack.Add(new float4(t0, t1, 0, 0));

        while (stack.Length > 0) {
            var item = stack[^1];
            stack.Length -= 1;
            var a = item.x;
            var b = item.y;
            var depth = (int)item.z;

            var p0 = spline.EvaluatePosition(a);
            var p1 = spline.EvaluatePosition(b);

            // Midpoint in parameter space
            var m = 0.5f * (a + b);
            var pm = spline.EvaluatePosition(m);

            // Compute distance of pm to line segment p0-p1
            var ab = p1 - p0;
            var ap = pm - p0;
            var abLen2 = math.max(math.lengthsq(ab), 1e-12f);
            var t = math.saturate(math.dot(ap, ab) / abLen2);
            var closest = p0 + t * ab;
            var err = math.distance(pm, closest);

            if (err > maxError && depth < kMaxDepth) {
                var left = new float4(a, m, depth + 1, 0);
                var right = new float4(m, b, depth + 1, 0);
                // Push right then left to process left first and keep increasing order when appending
                stack.Add(right);
                stack.Add(left);
            } else {
                // Accept this segment; append its endpoint b
                outTs.Add(b);
            }
        }

        stack.Dispose();
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
