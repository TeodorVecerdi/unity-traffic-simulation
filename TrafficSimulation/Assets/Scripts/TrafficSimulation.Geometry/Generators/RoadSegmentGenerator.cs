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
    [SerializeField] private WindingOrder m_RoadWinding;
    [SerializeField] private WindingOrder m_MarkingsWinding;

    public override bool Validate() {
        return m_RoadSegment != null
            && m_RoadSegment.RoadSegment.Lanes.Count >= 1
            && m_SplineContainer != null
            && m_SplineContainer.Spline.Count >= 2;
    }

    public override int GetSubMeshCount() {
        var hasAnyRoadMarkings = m_RoadSegment.RoadSegment.Lanes.Exists(lane => lane.LeftMarking != RoadMarkingType.None || lane.RightMarking != RoadMarkingType.None);
        if (hasAnyRoadMarkings) {
            return 2; // One sub-mesh for the road surface, one for the road markings
        }

        return 1; // Only one sub-mesh for the road surface
    }

    public override JobHandle ScheduleGenerate(in MeshGenerationContext context, List<GeometryWriter> writers, JobHandle dependency) {
        var points = GenerateRoadProfile(out var roadWidth);
        if (points.Positions.Count < 2) {
            Debug.LogWarning($"{nameof(RoadSegmentGenerator)}: Generated road profile has less than 2 points.");
            return dependency;
        }

        var maxError = math.max(0.005f, m_MaxError);
        var frames = SplineSamplingJobHelper.SampleSpline(m_SplineContainer.Spline, maxError, Allocator.TempJob);
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
            WindingOrder = m_RoadWinding,
        }.Schedule(dependency);

        var lastJob = dependency;
        var hasAnyRoadMarkings = m_RoadSegment.RoadSegment.Lanes.Exists(lane => lane.LeftMarking != RoadMarkingType.None || lane.RightMarking != RoadMarkingType.None);
        if (hasAnyRoadMarkings) {
            ScheduleRoadMarkingsJobs(roadWidth, writers[1], ref lastJob);
        }

        var combinedJob = JobHandle.CombineDependencies(job, lastJob);
        var cleanupJob = new DisposeNativeArrayJob<Frame, float3, bool, float2> {
            Array1 = frames,
            Array2 = polylinePoints,
            Array3 = emitEdges,
            Array4 = segmentDirections,
        }.Schedule(combinedJob);

        return cleanupJob;
    }

    private void ScheduleRoadMarkingsJobs(float roadWidth, GeometryWriter writer, ref JobHandle lastJob) {
        var markingWidth = m_RoadSegment.RoadMarkingConfiguration.Width;
        var markingDashedLength = m_RoadSegment.RoadMarkingConfiguration.DashedLength;
        var markingDashedGapLength = m_RoadSegment.RoadMarkingConfiguration.DashedGapLength;
        var sidewalkWidth = m_RoadSegment.SidewalkConfiguration.Width;

        var maxError = math.max(0.005f, m_MaxError);
        var anyDashed = m_RoadSegment.RoadSegment.Lanes.Exists(lane => lane.LeftMarking == RoadMarkingType.Dashed || lane.RightMarking == RoadMarkingType.Dashed);
        var dashedFrames = anyDashed
            ? SplineSamplingJobHelper.SampleFramesForRibbon(m_SplineContainer.Spline, Allocator.TempJob, maxError, markingDashedLength, markingDashedGapLength)
            : default;

        var anySolid = m_RoadSegment.RoadSegment.Lanes.Exists(lane => lane.LeftMarking == RoadMarkingType.Solid || lane.RightMarking == RoadMarkingType.Solid);
        var solidFrames = anySolid
            ? SplineSamplingJobHelper.SampleFramesForRibbon(m_SplineContainer.Spline, Allocator.TempJob, maxError)
            : default;

        var currentX = roadWidth * 0.5f;
        var lanes = m_RoadSegment.RoadSegment.Lanes;
        for (var i = 0; i < lanes.Count; i++) {
            var lane = lanes[i];

            // Adjust for right sidewalk
            if (i == 0 && lane.RightSidewalk is SidewalkType.Sidewalk) {
                currentX -= sidewalkWidth;
            }

            if (lane.RightMarking is not RoadMarkingType.None) {
                var isDashed = lane.RightMarking is RoadMarkingType.Dashed;

                var offset = 0.0f;
                if (i == 0) {
                    // On the edge of the road
                    offset = -markingWidth * 0.5f;
                } else if (lanes[i - 1].LeftMarking is not RoadMarkingType.None) {
                    // Between two lanes with markings on both sides
                    offset = -markingWidth * 0.725f; // leave half of the marking width gap between the two markings
                }

                var dashLength = isDashed ? markingDashedLength : 0.0f;
                var gapLength = isDashed ? markingDashedGapLength : 0.0f;
                var frames = isDashed ? dashedFrames : solidFrames;
                ScheduleJob(frames, currentX + offset, dashLength, gapLength, ref lastJob);
            }

            currentX -= lane.Width;

            if (lane.LeftMarking is not RoadMarkingType.None) {
                var isDashed = lane.LeftMarking is RoadMarkingType.Dashed;

                var offset = 0.0f;
                if (i == lanes.Count - 1) {
                    // On the edge of the road
                    offset = markingWidth * 0.5f;
                } else if (lanes[i + 1].RightMarking is not RoadMarkingType.None) {
                    // Between two lanes with markings on both sides
                    offset = markingWidth * 0.725f; // leave half of the marking width gap between the two markings
                }

                var dashLength = isDashed ? markingDashedLength : 0.0f;
                var gapLength = isDashed ? markingDashedGapLength : 0.0f;
                var frames = isDashed ? dashedFrames : solidFrames;
                ScheduleJob(frames, currentX + offset, dashLength, gapLength, ref lastJob);
            }
        }

        // Cleanup jobs
        if (anyDashed && anySolid) {
            var cleanupJob = new DisposeNativeArrayJob<Frame, Frame> {
                Array1 = dashedFrames,
                Array2 = solidFrames,
            }.Schedule(lastJob);
            lastJob = cleanupJob;
        } else if (anyDashed) {
            var cleanupJob = new DisposeNativeArrayJob<Frame> {
                Array = dashedFrames,
            }.Schedule(lastJob);
            lastJob = cleanupJob;
        } else if (anySolid) {
            var cleanupJob = new DisposeNativeArrayJob<Frame> {
                Array = solidFrames,
            }.Schedule(lastJob);
            lastJob = cleanupJob;
        }

        return;

        void ScheduleJob(NativeArray<Frame> frames, float positionX, float dashLength, float gapLength, ref JobHandle lastJob) {
            var job = new RibbonStripJob {
                Frames = frames,
                Width = markingWidth,
                OnLength = dashLength,
                OffLength = gapLength,
                Phase = 0.0f,
                LocalOffset = new float3(positionX, 0.01f, 0.0f), // Slightly above the road surface to avoid z-fighting
                LocalToWorld = m_SplineContainer.transform.localToWorldMatrix,
                Writer = writer,
                WindingOrder = m_MarkingsWinding,
            }.Schedule(lastJob);
            lastJob = job;
        }
    }

    private (List<float3> Positions, List<bool> EmitEdges) GenerateRoadProfile(out float roadWidth) {
        var sidewalkWidth = m_RoadSegment.SidewalkConfiguration.Width;

        // TODO: Reject/ignore invalid configurations (e.g. sidewalks in the middle of the road, zero-width lanes, etc)
        roadWidth = 0.0f;
        foreach (var segment in m_RoadSegment.RoadSegment.Lanes) {
            roadWidth += segment.Width;
            if (segment.LeftSidewalk is SidewalkType.Sidewalk)
                roadWidth += sidewalkWidth;
            if (segment.RightSidewalk is SidewalkType.Sidewalk)
                roadWidth += sidewalkWidth;
        }

        if (m_SplitLanes)
            return GenerateSplitLanes(roadWidth);
        return GenerateUnifiedRoad(roadWidth);
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
