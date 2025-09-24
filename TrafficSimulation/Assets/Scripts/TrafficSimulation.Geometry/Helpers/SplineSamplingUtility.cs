using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Splines;

namespace TrafficSimulation.Geometry.Helpers;

public static class SplineSamplingUtility {
    public static void AdaptiveSample(Spline spline, List<float> results, float maxError = 0.05f, float maxStep = 2.0f) {
        results.Clear();
        Subdivide(spline, 0.0f, 1.0f, maxError * maxError, maxStep, results);
        results.Add(1.0f);
    }

    public static void AdaptiveSample(Spline spline, ref NativeList<float4> positions, ref NativeList<float4> tangents, float maxError = 0.05f, float maxStep = 2.0f) {
        positions.Clear();
        tangents.Clear();
        Subdivide(spline, 0.0f, 1.0f, maxError * maxError, maxStep, ref positions, ref tangents);
        // Ensure endpoint at t=1 is included
        var p1 = spline.EvaluatePosition(1.0f);
        var t1 = spline.EvaluateTangent(1.0f);
        positions.Add(new float4(p1, 1.0f));
        tangents.Add(new float4(t1, 0.0f));
    }

    private static bool ShouldSubdivide(Spline spline, float t0, float t1, float maxErrorSq, float maxStep) {
        var p0 = spline.EvaluatePosition(t0);
        var p1 = spline.EvaluatePosition(t1);
        var pm = spline.EvaluatePosition((t0 + t1) * 0.5f);

        var chordMid = (p0 + p1) * 0.5f;
        var deviation = math.distancesq(pm, chordMid);

        var splitLen = math.distance(p0, pm) + math.distance(pm, p1);

        return deviation > maxErrorSq || splitLen > maxStep;
    }

    private static void Subdivide(Spline spline, float t0, float t1, float maxErrorSq, float maxStep, List<float> results) {
        if (ShouldSubdivide(spline, t0, t1, maxErrorSq, maxStep)) {
            var tm = (t0 + t1) * 0.5f;
            Subdivide(spline, t0, tm, maxErrorSq, maxStep, results);
            Subdivide(spline, tm, t1, maxErrorSq, maxStep, results);
        } else {
            results.Add(t0);
        }
    }

    // Overload writing directly to NativeLists for float4 positions/tangents (w=1 and w=0 respectively).
    private static void Subdivide(Spline spline, float t0, float t1, float maxErrorSq, float maxStep, ref NativeList<float4> positions, ref NativeList<float4> tangents) {
        if (ShouldSubdivide(spline, t0, t1, maxErrorSq, maxStep)) {
            var tm = (t0 + t1) * 0.5f;
            Subdivide(spline, t0, tm, maxErrorSq, maxStep, ref positions, ref tangents);
            Subdivide(spline, tm, t1, maxErrorSq, maxStep, ref positions, ref tangents);
        } else {
            var pos = spline.EvaluatePosition(t0);
            var tan = spline.EvaluateTangent(t0);
            positions.Add(new float4(pos, 1.0f));
            tangents.Add(new float4(tan, 0.0f));
        }
    }
}
