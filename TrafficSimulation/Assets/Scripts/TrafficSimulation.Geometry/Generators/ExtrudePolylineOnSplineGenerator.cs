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
public sealed class ExtrudePolylineOnSplineGenerator : MeshGenerator {
    [SerializeField, Required] private Polyline m_Polyline = null!;
    [SerializeField, Required] private SplineContainer m_SplineContainer = null!;
    [Space]
    [SerializeField] private float m_MaxError = 0.2f;
    [SerializeField] private WindingOrder m_WindingOrder;

    public override bool Validate() {
        return m_Polyline != null
            && m_Polyline.Points.Count >= 2
            && m_SplineContainer != null
            && m_SplineContainer.Spline.Count >= 2
            && m_MaxError > 0.0f;
    }

    public override void EstimateCounts(in MeshGenerationContext context, out int vertexCount, out int indexCount) {
        var rings = (int)(m_SplineContainer.Spline.GetLength() / 10.0f);
        var ringSize = math.max(2, m_Polyline.Points.Count);
        vertexCount = rings * ringSize;
        indexCount = (rings - 1) * (ringSize - 1) * 6;
    }

    public override JobHandle ScheduleGenerate(in MeshGenerationContext context, List<GeometryWriter> writers, JobHandle dependency) {
        var points = Polyline.GetGeometry(m_Polyline.Points);

        var frames = SplineSamplingJobHelper.SampleSpline(m_SplineContainer.Spline, m_MaxError, Allocator.TempJob);
        var polylinePoints = new NativeArray<float3>(points.Positions.ToArray(), Allocator.TempJob);
        var emitEdges = new NativeArray<bool>(points.EmitEdges.ToArray(), Allocator.TempJob);
        var segmentDirections = new NativeArray<float2>(polylinePoints.Length, Allocator.TempJob);

        var job = new ExtrudePolylineOnSplineJob {
            Frames = frames,
            PolylinePoints = polylinePoints,
            PolylineEmitEdges = emitEdges,
            PolylineSegmentDirections = segmentDirections,
            LocalToWorld = m_SplineContainer.transform.localToWorldMatrix,
            Writer = writers[0],
            WindingOrder = m_WindingOrder,
        }.Schedule(dependency);

        var cleanupJob = new DisposeNativeArrayJob<Frame, float3, bool, float2> {
            Array1 = frames,
            Array2 = polylinePoints,
            Array3 = emitEdges,
            Array4 = segmentDirections,
        }.Schedule(job);

        return cleanupJob;
    }
}
