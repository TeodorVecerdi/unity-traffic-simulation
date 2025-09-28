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

        var frames = SplineSamplingJobHelper.SampleFramesForRibbon(spline, Allocator.TempJob, m_MaxError, m_OnLength, m_OffLength, m_Phase);

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
}
