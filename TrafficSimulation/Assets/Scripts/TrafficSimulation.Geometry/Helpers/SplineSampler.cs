using TrafficSimulation.Geometry.Data;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Splines;

namespace TrafficSimulation.Geometry.Helpers;

public static class SplineSampler {
    /// <summary>
    /// Adaptively sample a spline into frames using both curvature and chord error criteria using fixed up = math.up().
    /// </summary>
    /// <param name="spline">The source spline (0..1 parameterization).</param>
    /// <param name="maxError">Maximum allowed midpoint chord error in world units.</param>
    /// <param name="frames">Output frames list (cleared and filled).</param>
    public static void Sample(Spline spline, float maxError, ref NativeList<Frame> frames) {
        frames.Clear();

        var maxErrorSq = maxError * maxError;
        var upHint = math.up();

        // Recursively append frames at segment start parameters; we'll add t=1 at the end.
        SubdivideIntoFrames(spline, 0.0f, 1.0f, maxErrorSq, in upHint, ref frames);

        // Ensure endpoint sample at t=1
        AppendFrameAt(spline, 1.0f, in upHint, ref frames);
    }

    private static void AppendFrameAt(Spline spline, float t, in float3 upHint, ref NativeList<Frame> frames) {
        var pos = spline.EvaluatePosition(t);
        var tangent = spline.EvaluateTangent(t);
        tangent = math.normalizesafe(tangent, new float3(0.0f, 0.0f, 1.0f));

        GeometryUtils.BuildOrthonormalBasis(in tangent, in upHint, out var right, out var up);

        // Deduplicate if same position as last (can happen at recursion termination boundaries)
        if (frames.Length > 0) {
            var last = frames[^1];
            if (math.distancesq(last.Position.xyz, pos) <= math.EPSILON)
                return;
        }

        frames.Add(new Frame {
            Position = new float4(pos, 1.0f),
            Tangent = new float4(tangent, 0.0f),
            Normal = new float4(up, 0.0f),
            Binormal = new float4(right, 0.0f),
        });
    }

    private static void SubdivideIntoFrames(Spline spline, float t0, float t1, float maxErrorSq, in float3 upHint, ref NativeList<Frame> frames) {
        // Terminate if parameter interval is extremely small to avoid infinite recursion
        if (t1 - t0 <= math.EPSILON) {
            AppendFrameAt(spline, t0, in upHint, ref frames);
            return;
        }

        if (ShouldSubdivide(spline, t0, t1, maxErrorSq)) {
            var tm = (t0 + t1) * 0.5f;
            SubdivideIntoFrames(spline, t0, tm, maxErrorSq, in upHint, ref frames);
            SubdivideIntoFrames(spline, tm, t1, maxErrorSq, in upHint, ref frames);
        } else {
            AppendFrameAt(spline, t0, in upHint, ref frames);
        }
    }

    private static bool ShouldSubdivide(Spline spline, float t0, float t1, float maxErrorSq) {
        var p0 = spline.EvaluatePosition(t0);
        var p1 = spline.EvaluatePosition(t1);
        var pm = spline.EvaluatePosition((t0 + t1) * 0.5f);
        var chordMid = (p0 + p1) * 0.5f;
        return math.distancesq(pm, chordMid) > maxErrorSq;
    }
}
