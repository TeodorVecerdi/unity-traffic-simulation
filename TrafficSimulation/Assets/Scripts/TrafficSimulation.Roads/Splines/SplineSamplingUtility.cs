using Unity.Mathematics;
using UnityEngine.Splines;

namespace TrafficSimulation.Roads.Splines;

public static class SplineSamplingUtility {
    public static void AdaptiveSample(Spline spline, List<float> results, float maxError = 0.05f, float maxStep = 2.0f) {
        results.Clear();
        Subdivide(spline, 0.0f, 1.0f, maxError * maxError, maxStep, results);
        results.Add(1.0f);
    }

    private static void Subdivide<T>(Spline spline, float t0, float t1, float maxErrorSq, float maxStep, T results) where T : ICollection<float> {
        var p0 = spline.EvaluatePosition(t0);
        var p1 = spline.EvaluatePosition(t1);
        var pm = spline.EvaluatePosition((t0 + t1) * 0.5f);

        // chord midpoint
        var chordMid = (p0 + p1) * 0.5f;
        var deviation = math.distancesq(pm, chordMid);

        // arc-length estimate via chord length vs midpoint split
        var splitLen = math.distance(p0, pm) + math.distance(pm, p1);

        var tooCurvy = deviation > maxErrorSq;
        var tooLong = splitLen > maxStep;

        if (tooCurvy || tooLong) {
            var tm = (t0 + t1) * 0.5f;
            Subdivide(spline, t0, tm, maxErrorSq, maxStep, results);
            Subdivide(spline, tm, t1, maxErrorSq, maxStep, results);
        } else {
            results.Add(t0);
        }
    }
}
