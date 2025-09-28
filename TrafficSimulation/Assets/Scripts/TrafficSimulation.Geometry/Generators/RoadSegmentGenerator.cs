using Sirenix.OdinInspector;
using TrafficSimulation.Geometry.Build;
using TrafficSimulation.Geometry.Data;
using TrafficSimulation.Geometry.Helpers;
using TrafficSimulation.Geometry.Jobs;
using TrafficSimulation.RoadGraph.Authoring;
using TrafficSimulation.RoadGraph.Data;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace TrafficSimulation.Geometry.Generators;

[Serializable]
public sealed class RoadSegmentGenerator : MeshGenerator {
    [SerializeField, Required] private RoadSegmentAuthoring m_RoadSegment = null!;
    [SerializeField, Required] private SplineContainer m_SplineContainer = null!;
    [SerializeField] private bool m_SplitLanes;
    [SerializeField] private float m_MaxError = 0.2f;
    [SerializeField] private bool m_WindingClockwise;

    public override bool Validate() {
        return m_RoadSegment != null
            && m_RoadSegment.RoadSegment.Lanes.Count >= 1
            && m_SplineContainer != null
            && m_SplineContainer.Spline.Count >= 2;
    }

    public override JobHandle ScheduleGenerate(in MeshGenerationContext context, GeometryWriter writer, JobHandle dependency) {
        var points = GenerateRoadProfile();
        if (points.Positions.Count < 2) {
            Debug.LogWarning($"{nameof(RoadSegmentGenerator)}: Generated road profile has less than 2 points.");
            return dependency;
        }

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
            Writer = writer,
            WindingClockwise = m_WindingClockwise,
        }.Schedule(dependency);

        var cleanupJob = new DisposeNativeArrayJob<Frame, float3, bool, float2> {
            Array1 = frames,
            Array2 = polylinePoints,
            Array3 = emitEdges,
            Array4 = segmentDirections,
        }.Schedule(job);

        return cleanupJob;
    }

    private (List<float3> Positions, List<bool> EmitEdges) GenerateRoadProfile() {
        var sidewalkWidth = m_RoadSegment.SidewalkConfiguration.Width;

        // TODO: Reject/ignore invalid configurations (e.g. sidewalks in the middle of the road, zero-width lanes, etc)
        var totalWidth = 0.0f;
        foreach (var segment in m_RoadSegment.RoadSegment.Lanes) {
            totalWidth += segment.Width;
            if (segment.LeftSidewalk is SidewalkType.Sidewalk)
                totalWidth += sidewalkWidth;
            if (segment.RightSidewalk is SidewalkType.Sidewalk)
                totalWidth += sidewalkWidth;
        }

        if (m_SplitLanes) {
            return GenerateSplitLanes(totalWidth);
        }

        return GenerateUnifiedRoad(totalWidth);
    }

    private (List<float3> Positions, List<bool> EmitEdges) GenerateSplitLanes(float roadWidth) {
        var halfWidth = roadWidth * 0.5f;
        var sidewalkCurbHeight = m_RoadSegment.SidewalkConfiguration.CurbHeight;
        var sidewalkWidth = m_RoadSegment.SidewalkConfiguration.Width;

        var points = new List<PolylinePoint>();
        var currentX = halfWidth;
        var nextPointHardEdge = false;
        for (var i = 0; i < m_RoadSegment.RoadSegment.Lanes.Count; i++) {
            var segment = m_RoadSegment.RoadSegment.Lanes[i];
            // Handle right sidewalk
            if (i == 0 && segment.RightSidewalk is SidewalkType.Sidewalk) {
                // Sidewalk outer edge, soft
                points.Add(new PolylinePoint { Position = new float3(currentX, sidewalkCurbHeight, 0.0f), HardEdge = false });
                currentX -= sidewalkWidth;

                // Sidewalk inner edge, hard
                points.Add(new PolylinePoint { Position = new float3(currentX, sidewalkCurbHeight, 0.0f), HardEdge = true });

                // Mark next point as hard edge to avoid smoothing between sidewalk and road
                nextPointHardEdge = true;
            }

            // Road edge
            points.Add(new PolylinePoint { Position = new float3(currentX, 0.0f, 0.0f), HardEdge = nextPointHardEdge });
            currentX -= segment.Width;
            nextPointHardEdge = false;

            if (i == m_RoadSegment.RoadSegment.Lanes.Count - 1) {
                // Add last road edge
                var hasSidewalk = segment.LeftSidewalk is SidewalkType.Sidewalk;
                points.Add(new PolylinePoint { Position = new float3(currentX, 0.0f, 0.0f), HardEdge = hasSidewalk });

                // Handle left sidewalk
                if (hasSidewalk) {
                    // Sidewalk inner edge, hard
                    points.Add(new PolylinePoint { Position = new float3(currentX, sidewalkCurbHeight, 0.0f), HardEdge = true });
                    currentX -= sidewalkWidth;

                    // Sidewalk outer edge, soft
                    points.Add(new PolylinePoint { Position = new float3(currentX, sidewalkCurbHeight, 0.0f), HardEdge = false });
                }
            }
        }

        return Polyline.GetGeometry(points);
    }

    private (List<float3> Positions, List<bool> EmitEdges) GenerateUnifiedRoad(float roadWidth) {
        var halfWidth = roadWidth * 0.5f;
        var sidewalkCurbHeight = m_RoadSegment.SidewalkConfiguration.CurbHeight;
        var sidewalkWidth = m_RoadSegment.SidewalkConfiguration.Width;

        var points = new List<PolylinePoint>();
        var currentX = halfWidth;
        var remainingWidth = roadWidth;
        var segments = m_RoadSegment.RoadSegment.Lanes;
        var nextPointHardEdge = false;

        // Right sidewalk
        if (segments[0].RightSidewalk is SidewalkType.Sidewalk) {
            // Right sidewalk outer edge, soft
            points.Add(new PolylinePoint { Position = new float3(currentX, sidewalkCurbHeight, 0.0f), HardEdge = false });
            // Right sidewalk inner edge, hard
            points.Add(new PolylinePoint { Position = new float3(currentX - sidewalkWidth, sidewalkCurbHeight, 0.0f), HardEdge = true });
            currentX -= sidewalkWidth;
            remainingWidth -= sidewalkWidth;
            nextPointHardEdge = true;
        }

        // Right road edge
        points.Add(new PolylinePoint { Position = new float3(currentX, 0.0f, 0.0f), HardEdge = nextPointHardEdge });
        nextPointHardEdge = segments[^1].LeftSidewalk is SidewalkType.Sidewalk;

        // Left road edge
        currentX -= remainingWidth;
        if (nextPointHardEdge) {
            currentX += sidewalkWidth;
        }

        points.Add(new PolylinePoint { Position = new float3(currentX, 0.0f, 0.0f), HardEdge = nextPointHardEdge });

        // Left sidewalk
        if (nextPointHardEdge) {
            points.Add(new PolylinePoint { Position = new float3(currentX, sidewalkCurbHeight, 0.0f), HardEdge = true });
            points.Add(new PolylinePoint { Position = new float3(currentX - sidewalkWidth, sidewalkCurbHeight, 0.0f), HardEdge = false });
        }

        return Polyline.GetGeometry(points);
    }
}
