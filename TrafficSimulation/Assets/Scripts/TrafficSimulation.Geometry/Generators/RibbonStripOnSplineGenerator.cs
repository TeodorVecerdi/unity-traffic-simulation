using Sirenix.OdinInspector;
using TrafficSimulation.Geometry.Build;
using TrafficSimulation.Geometry.Data;
using TrafficSimulation.Geometry.Helpers;
using TrafficSimulation.Geometry.Jobs;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace TrafficSimulation.Geometry.Generators;

[Serializable]
public sealed class RibbonStripOnSplineGenerator : MeshGenerator {
    [Title("Input")]
    [SerializeField, Required] private SplineContainer m_SplineContainer = null!;

    [Title("Frames")]
    [SerializeField] private float m_MaxError = 0.2f;

    [Title("Local Offset")]
    [SerializeField, Tooltip("Offset in frame space (Right, Up) meters. Useful to place multiple markings from the same spline.")]
    private float3 m_LocalOffset;

    [Title("Ribbon")]
    [SerializeField, MinValue(0.001f), Unit(Units.Meter, Units.Millimeter)] private float m_Width = 0.2f;
    [SerializeField] private bool m_WindingClockwise;

    [Title("Dash Pattern (Gaps)")]
    [SerializeField, Unit(Units.Meter)]
    private float m_OnLength = 1.0f;
    [SerializeField, Unit(Units.Meter)]
    private float m_OffLength = 1.0f;
    [SerializeField, Unit(Units.Meter)]
    private float m_Phase;

    public override bool Validate() {
        if (m_SplineContainer == null) return false;
        if (m_SplineContainer.Spline == null || m_SplineContainer.Spline.Count < 2) return false;
        if (m_Width <= 0.0f) return false;
        if (m_MaxError <= 0.0f) return false;
        return true;
    }

    public override void EstimateCounts(in MeshGenerationContext context, out int vertexCount, out int indexCount) {
        // Approximate number of frames (segments + 1)
        var lengthMeters = math.max(1.0f, m_SplineContainer.Spline.GetLength());
        var framesApprox = math.max(2, (int)math.ceil(lengthMeters / 10.0f) + 1);

        // Duty factor for gaps
        var duty = 1.0f;
        if (m_OnLength > 0.0f && m_OffLength > 0.0f)
            duty = m_OnLength / (m_OnLength + m_OffLength);

        // Approx counts (NativeList can grow anyway; this is just a hint)
        vertexCount = (int)math.ceil(2.0f * framesApprox * duty + 4.0f);
        indexCount = (int)math.ceil(6.0f * (framesApprox - 1) * duty);
    }

    public override JobHandle ScheduleGenerate(in MeshGenerationContext context, List<GeometryWriter> writers, JobHandle dependency) {
        var spline = m_SplineContainer.Spline;

        var frameList = new NativeList<Frame>(Allocator.Temp);

        if (m_OnLength <= 0.0f || m_OffLength <= 0.0f) {
            SplineSampler.Sample(spline, m_MaxError, ref frameList);
        } else {
            SamplePreciseFrames(spline, ref frameList);
        }

        var frames = new NativeArray<Frame>(frameList.Length, Allocator.TempJob);
        frames.CopyFrom(frameList.AsArray());
        frameList.Dispose();

        var job = new RibbonStripJob {
            Frames = frames,
            Width = m_Width,
            WindingClockwise = m_WindingClockwise,
            OnLength = m_OnLength,
            OffLength = m_OffLength,
            Phase = m_Phase,
            LocalOffset = m_LocalOffset,
            LocalToWorld = m_SplineContainer.transform.localToWorldMatrix,
            Writer = writers[0],
        }.Schedule(dependency);

        var cleanupJob = new DisposeNativeArrayJob<Frame> { Array = frames }
            .Schedule(job);

        return cleanupJob;
    }

    private void SamplePreciseFrames(Spline spline, ref NativeList<Frame> frameList) {
        var totalLength = spline.GetLength();
        var cycleLength = m_OnLength + m_OffLength;

        // Collect transition points in arc-length space
        var ts = new NativeList<float>(Allocator.Temp);

        // Find first cycle start after (Phase mod cycleLength), cover [-cycleLength, totalLength + cycleLength]
        var effectivePhase = m_Phase % cycleLength;
        if (effectivePhase < 0.0f)
            effectivePhase += cycleLength;
        var s = effectivePhase - cycleLength; // start one cycle before to cover negatives

        while (s < totalLength + cycleLength) {
            var onEnd = s + m_OnLength;
            var offEnd = onEnd + m_OffLength;

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
