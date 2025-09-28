using TrafficSimulation.Geometry.Data;
using Unity.Collections;
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
}
