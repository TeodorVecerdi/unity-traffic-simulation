using Sirenix.OdinInspector;
using TrafficSimulation.Sim.Authoring;
using TrafficSimulation.Sim.Components;
using TrafficSimulation.Sim.Math;
using Unity.Mathematics;
using Vecerdi.Extensions.DependencyInjection;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TrafficSimulation.Sim.Visualization;

[RequireComponent(typeof(TrafficSimulationController))]
public sealed class SimulationVisualizer : MonoSingleton<SimulationVisualizer> {
    [Title("Visualization Settings")]
    [SerializeField, Required] private VisualizationSettings m_Settings = null!;

    [Title("Debug Features")]
    [SerializeField] private bool m_ShowDebugInfo;
    [SerializeField, ShowIf(nameof(m_ShowDebugInfo))] private bool m_ShowPerformanceMetrics;
    [SerializeField, ShowIf(nameof(m_ShowDebugInfo))] private bool m_ShowMemoryUsage;

    private TrafficSimulationController m_SimulationController = null!;
    private readonly Dictionary<int, LaneAuthoring> m_LaneIdMap = [];
    private readonly Dictionary<int, VehicleAuthoring> m_VehicleIdMap = [];
    private Camera m_MainCamera = null!;

    // Performance tracking
    private int m_LastFrameVehicleCount;
    private float m_LastRenderTime;

    public VisualizationSettings Settings {
        get => m_Settings;
        set => m_Settings = value ?? throw new ArgumentNullException(nameof(value));
    }

    protected override void Awake() {
        base.Awake();
        m_SimulationController = GetComponent<TrafficSimulationController>();
        m_MainCamera = Camera.main ?? FindFirstObjectByType<Camera>();
    }

    private void Start() {
        CacheAuthoringComponents();
    }

    private void OnDrawGizmos() {
        if (!isActiveAndEnabled || !Application.isPlaying || m_SimulationController == null || m_Settings == null) {
            return;
        }

        var startTime = Time.realtimeSinceStartup;
        var worldState = m_SimulationController.WorldState;
        if (worldState == null) {
            return;
        }

        var settings = m_Settings;
        if (settings.ShowLanes) {
            DrawLaneVisualization(worldState, settings);
        }

        if (settings.ShowVehicles) {
            DrawVehicleVisualization(worldState, settings);
        }

        if (settings.ShowTrafficLights) {
            DrawTrafficLightVisualization(worldState, settings);
        }

        if (m_ShowDebugInfo) {
            DrawDebugInformation(worldState);
        }

        m_LastRenderTime = Time.realtimeSinceStartup - startTime;
        m_LastFrameVehicleCount = worldState.Vehicles.Length;
    }

    private void CacheAuthoringComponents() {
        m_LaneIdMap.Clear();
        var lanes = FindObjectsByType<LaneAuthoring>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var lane in lanes) {
            if (lane != null) {
                m_LaneIdMap[lane.LaneId] = lane;
            }
        }

        m_VehicleIdMap.Clear();
        var vehicles = FindObjectsByType<VehicleAuthoring>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var vehicle in vehicles) {
            if (vehicle != null) {
                m_VehicleIdMap[vehicle.VehicleId] = vehicle;
            }
        }
    }

    private void DrawLaneVisualization(WorldState worldState, VisualizationSettings settings) {
        var lanes = worldState.Lanes;
        var laneRanges = worldState.LaneRanges;

        for (var i = 0; i < lanes.Length; i++) {
            var lane = lanes[i];
            if (!m_LaneIdMap.TryGetValue(lane.LaneId, out var laneAuthoring) || laneAuthoring == null) {
                continue;
            }

            if (ShouldSkipBasedOnDistance(laneAuthoring.transform.position, settings)) {
                continue;
            }

            var laneTransform = laneAuthoring.transform;
            DrawLaneGeometry(lane, laneTransform, settings);

            if (settings.ShowTrafficDensity) {
                DrawTrafficDensity(lane, laneRanges[i], laneTransform, settings);
            }

            if (settings.ShowLaneConnections) {
                DrawLaneConnections(lane, laneTransform, worldState, settings);
            }

            if (settings.ShowLaneLabels) {
                DrawLaneInfo(lane, laneRanges[i], laneTransform);
            }
        }
    }

    private static void DrawLaneGeometry(LaneInfo lane, Transform laneTransform, VisualizationSettings settings) {
        var start = laneTransform.position;
        var end = start + laneTransform.forward * lane.Length;

        // Draw lane centerline
        Gizmos.color = settings.LaneColor;
        Gizmos.DrawLine(start, end);

        // Draw lane boundaries
        var rightOffset = laneTransform.right * (settings.VehicleWidth * 0.6f);
        var boundaryColor = new Color(settings.LaneColor.r, settings.LaneColor.g, settings.LaneColor.b, settings.LaneColor.a * 0.5f);
        Gizmos.color = boundaryColor;
        Gizmos.DrawLine(start + rightOffset, end + rightOffset);
        Gizmos.DrawLine(start - rightOffset, end - rightOffset);
    }

    private static void DrawTrafficDensity(LaneInfo lane, LaneVehicleRange range, Transform laneTransform, VisualizationSettings settings) {
        var density = range.Count / (lane.Length / 100.0f); // Vehicles per 100m
        var densityColor = Color.Lerp(Color.green, Color.red, math.clamp(density / 10.0f, 0.0f, 1.0f));

        Gizmos.color = new Color(densityColor.r, densityColor.g, densityColor.b, 0.3f);
        var start = laneTransform.position + Vector3.up * 0.1f;
        var end = start + laneTransform.forward * lane.Length;

        // Draw thick line to represent density
        for (var i = 0; i < 5; i++) {
            var offset = laneTransform.right * (i - 2) * 0.1f;
            Gizmos.DrawLine(start + offset, end + offset);
        }
    }

    private void DrawLaneConnections(LaneInfo lane, Transform laneTransform, WorldState worldState, VisualizationSettings settings) {
        Gizmos.color = settings.LaneConnectionColor;
        var midPoint = laneTransform.position + laneTransform.forward * (lane.Length * 0.5f);

        // Draw connections with animated dashed lines
        var time = Time.time;
        var dashLength = 0.5f;
        var dashSpeed = 2.0f;

        DrawConnectionLine(lane.LeftLaneIndex, midPoint, worldState, settings, time, dashLength, dashSpeed);
        DrawConnectionLine(lane.RightLaneIndex, midPoint, worldState, settings, time, dashLength, dashSpeed);
    }

    private void DrawConnectionLine(int targetLaneIndex, Vector3 fromPoint, WorldState worldState, VisualizationSettings settings, float time, float dashLength, float dashSpeed) {
        if (targetLaneIndex < 0 || targetLaneIndex >= worldState.Lanes.Length) return;

        var targetLane = worldState.Lanes[targetLaneIndex];
        if (!m_LaneIdMap.TryGetValue(targetLane.LaneId, out var targetLaneAuthoring) || targetLaneAuthoring == null) return;

        var toPoint = targetLaneAuthoring.transform.position + targetLaneAuthoring.transform.forward * (targetLane.Length * 0.5f);

        // Draw animated dashed line
        var direction = (toPoint - fromPoint).normalized;
        var distance = Vector3.Distance(fromPoint, toPoint);
        var dashCount = (int)(distance / dashLength);
        var animOffset = time * dashSpeed % (dashLength * 2.0f);

        for (var i = 0; i < dashCount; i++) {
            var dashStart = fromPoint + direction * (i * dashLength * 2.0f + animOffset);
            var dashEnd = dashStart + direction * dashLength;

            if (Vector3.Distance(dashStart, fromPoint) < distance) {
                Gizmos.DrawLine(dashStart, dashEnd);
            }
        }

        DrawArrowHead(fromPoint, toPoint, 0.3f);
    }

    private void DrawVehicleVisualization(WorldState worldState, VisualizationSettings settings) {
        var vehicles = worldState.Vehicles;
        var lanes = worldState.Lanes;
        var laneChangeStates = worldState.LaneChangeStates;
        var idmParameters = worldState.IdmParameters;

        for (var i = 0; i < vehicles.Length; i++) {
            var vehicle = vehicles[i];
            var lane = lanes[vehicle.LaneIndex];
            var laneChangeState = laneChangeStates[i];
            var idmParams = idmParameters[i];

            if (!m_LaneIdMap.TryGetValue(lane.LaneId, out var laneAuthoring) || laneAuthoring == null) {
                continue;
            }

            if (!m_VehicleIdMap.TryGetValue(vehicle.VehicleId, out var vehicleAuthoring) || vehicleAuthoring == null) {
                continue;
            }

            if (ShouldSkipBasedOnDistance(vehicleAuthoring.transform.position, settings)) {
                continue;
            }

#if UNITY_EDITOR
            var isSelected = Selection.Contains(vehicleAuthoring.gameObject);
            var showDetails = !settings.OnlyShowSelectedVehicleDetails || isSelected;
#else
            var showDetails = false;
#endif

            // Get vehicle position and orientation
            GetVehicleTransform(vehicle, laneChangeState, laneAuthoring.transform, out var position, out var forward);

            var right = math.cross(math.up(), forward);

            // Draw vehicle body with behavior-based coloring
            var vehicleColor = GetVehicleBehaviorColor(vehicle, idmParams, settings);
            DrawVehicleBody(position, forward, right, vehicle.Length, settings.VehicleWidth, vehicleColor, settings);

            if (!showDetails) continue;

            // Draw various vehicle features
            DrawVehicleFeatures(vehicle, laneChangeState, idmParams, position, forward, settings, worldState, lane, laneAuthoring.transform);
        }
    }

    private static void GetVehicleTransform(VehicleState vehicle, LaneChangeState laneChangeState, Transform laneTransform, out float3 position, out float3 forward) {
        if (laneChangeState.Active) {
            MathUtilities.EvaluateLaneChangeCurve(in laneChangeState, laneChangeState.ProgressS, out position, out forward);
        } else {
            position = laneTransform.position + laneTransform.forward * vehicle.Position;
            forward = laneTransform.forward;
        }
    }

    private static Color GetVehicleBehaviorColor(VehicleState vehicle, IdmParameters idmParams, VisualizationSettings settings) {
        if (!settings.ShowBehaviorStates) {
            return settings.VehicleBodyColor;
        }

        // Color based on speed relative to desired speed
        var speedRatio = vehicle.Speed / idmParams.DesiredSpeed;
        return speedRatio switch {
            < 0.5f => Color.red, // Slow/stopped
            < 0.8f => Color.yellow, // Below desired
            > 1.1f => Color.cyan, // Above desired (should never happen with IDM)
            _ => Color.green, // At desired speed
        };
    }

    private static void DrawVehicleBody(float3 center, float3 forward, float3 right, float length, float width, Color bodyColor, VisualizationSettings settings) {
        var halfLength = length * 0.5f;
        var halfWidth = width * 0.5f;

        var frontLeft = center + forward * halfLength - right * halfWidth;
        var frontRight = center + forward * halfLength + right * halfWidth;
        var rearLeft = center - forward * halfLength - right * halfWidth;
        var rearRight = center - forward * halfLength + right * halfWidth;

        // Draw filled vehicle body
        Gizmos.color = bodyColor;
        DrawFilledQuad(frontLeft, frontRight, rearRight, rearLeft);

        // Draw vehicle border
        Gizmos.color = settings.VehicleBorderColor;
        Gizmos.DrawLine(frontLeft, frontRight);
        Gizmos.DrawLine(frontRight, rearRight);
        Gizmos.DrawLine(rearRight, rearLeft);
        Gizmos.DrawLine(rearLeft, frontLeft);

        // Draw direction indicator
        var frontCenter = center + forward * halfLength;
        Gizmos.DrawLine(frontCenter, frontCenter + forward * 0.5f);

        // Draw headlight indicators
        Gizmos.color = Color.white;
        var headlightOffset = right * (halfWidth * 0.7f);
        Gizmos.DrawSphere(frontCenter + headlightOffset, 0.1f);
        Gizmos.DrawSphere(frontCenter - headlightOffset, 0.1f);
    }

    private static void DrawFilledQuad(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4) {
        // Simple filled quad using multiple lines (Gizmos doesn't have a fill method)
        var steps = 16;
        for (var i = 0; i <= steps; i++) {
            var t = i / (float)steps;
            var left = Vector3.Lerp(p4, p1, t);
            var right = Vector3.Lerp(p3, p2, t);
            Gizmos.DrawLine(left, right);
        }
    }

    private static void DrawVehicleFeatures(VehicleState vehicle, LaneChangeState laneChangeState, IdmParameters idmParams, float3 position, float3 forward, VisualizationSettings settings, WorldState worldState, LaneInfo lane, Transform laneTransform) {
        // Velocity vector
        if (settings.ShowVelocityVectors && vehicle.Speed > 0.1f) {
            DrawVelocityVector(position, forward, vehicle.Speed, settings);
        }

        // Acceleration vector
        if (settings.ShowAccelerationVectors && math.abs(vehicle.Acceleration) > 0.1f) {
            DrawAccelerationVector(position, forward, vehicle.Acceleration, settings);
        }

        // Speed comparison
        if (settings.ShowSpeedComparison) {
            DrawSpeedComparison(position, vehicle.Speed, idmParams.DesiredSpeed, settings);
        }

        // Vehicle gaps
        if (settings.ShowVehicleGaps) {
            DrawVehicleGaps(vehicle, lane, worldState, laneTransform, settings);
        }

        // Lane change trajectory
        if (settings.ShowLaneChangeTrajectories && laneChangeState.Active) {
            DrawLaneChangeTrajectory(laneChangeState, settings);
        }

        // Vehicle sensors (if enabled)
        if (settings.ShowVehicleSensors) {
            DrawVehicleSensors(vehicle, idmParams, position, forward, settings);
        }

        // Vehicle labels
        if (settings.ShowVehicleLabels) {
            DrawVehicleInfo(vehicle, laneChangeState, idmParams, position);
        }
    }

    private static void DrawVelocityVector(float3 position, float3 forward, float speed, VisualizationSettings settings) {
        Gizmos.color = settings.VelocityColor;
        var velocityEnd = position + forward * speed * settings.VelocityVectorScale + math.up() * 0.5f;
        var velocityStart = position + math.up() * 0.5f;

        Gizmos.DrawLine(velocityStart, velocityEnd);
        DrawArrowHead(velocityStart, velocityEnd, 0.3f);

        // Draw speed magnitude as text
#if UNITY_EDITOR
        var speedText = $"{speed:F1} m/s";
        Handles.color = settings.VelocityColor;
        Handles.Label((Vector3)velocityEnd + Vector3.up * 0.2f, speedText);
#endif
    }

    private static void DrawAccelerationVector(float3 position, float3 forward, float acceleration, VisualizationSettings settings) {
        var color = acceleration > 0 ? settings.AccelerationPositiveColor : settings.AccelerationNegativeColor;
        Gizmos.color = color;

        var accelEnd = position + forward * acceleration * settings.AccelerationVectorScale + math.up() * 1.0f;
        var accelStart = position + math.up() * 1.0f;

        Gizmos.DrawLine(accelStart, accelEnd);
        DrawArrowHead(accelStart, accelEnd, 0.2f);

        // Draw acceleration magnitude
#if UNITY_EDITOR
        var accelText = $"{acceleration:F2} m/s²";
        Handles.color = color;
        Handles.Label((Vector3)accelEnd + Vector3.up * 0.2f, accelText);
#endif
    }

    private static void DrawSpeedComparison(float3 position, float actualSpeed, float desiredSpeed, VisualizationSettings settings) {
        var barPosition = position + math.up() * 1.5f;
        var barLength = 2.0f;
        var barHeight = 0.1f;

        // Background bar
        Gizmos.color = Color.gray;
        var barStart = barPosition - math.right() * (barLength * 0.5f);
        var barEnd = barPosition + math.right() * (barLength * 0.5f);
        Gizmos.DrawLine(barStart, barEnd);

        // Actual speed bar
        var actualRatio = math.clamp(actualSpeed / math.max(desiredSpeed, 0.1f), 0.0f, 2.0f);
        var actualColor = actualRatio < 0.8f ? Color.red : actualRatio > 1.2f ? Color.cyan : Color.green;
        Gizmos.color = actualColor;
        var actualEnd = barStart + math.right() * (barLength * actualRatio * 0.5f);
        Gizmos.DrawLine(barStart, actualEnd);

        // Desired speed marker
        Gizmos.color = Color.white;
        var desiredPos = barPosition;
        Gizmos.DrawLine(desiredPos + math.up() * barHeight, desiredPos - math.up() * barHeight);
    }

    private static void DrawVehicleGaps(VehicleState vehicle, LaneInfo lane, WorldState worldState, Transform laneTransform, VisualizationSettings settings) {
        var vehicles = worldState.Vehicles;
        var laneRanges = worldState.LaneRanges;
        var range = laneRanges[vehicle.LaneIndex];

        // Find leader and follower
        VehicleState? leader = null;
        VehicleState? follower = null;
        var minLeaderDistance = float.MaxValue;
        var minFollowerDistance = float.MaxValue;

        for (var j = range.Start; j < range.Start + range.Count; j++) {
            var otherVehicle = vehicles[j];
            if (otherVehicle.VehicleId == vehicle.VehicleId) continue;

            var distance = MathUtilities.ComputeDistanceAlongLane(vehicle.Position, otherVehicle.Position, lane.Length);
            var reverseDistance = MathUtilities.ComputeDistanceAlongLane(otherVehicle.Position, vehicle.Position, lane.Length);

            // Leader (ahead)
            if (distance > 0 && distance < minLeaderDistance) {
                minLeaderDistance = distance;
                leader = otherVehicle;
            }

            // Follower (behind)
            if (reverseDistance > 0 && reverseDistance < minFollowerDistance) {
                minFollowerDistance = reverseDistance;
                follower = otherVehicle;
            }
        }

        // Draw gap to leader
        if (leader.HasValue) {
            DrawGapLine(vehicle, leader.Value, lane, laneTransform, settings, true);
        }

        // Draw gap to follower (optional, less cluttered)
        if (follower.HasValue && settings.ShowVehicleGaps) {
            var followerColor = new Color(settings.GapLineColor.r, settings.GapLineColor.g, settings.GapLineColor.b, settings.GapLineColor.a * 0.3f);
            DrawGapLine(follower.Value, vehicle, lane, laneTransform, settings, false, followerColor);
        }
    }

    private static void DrawGapLine(VehicleState rearVehicle, VehicleState frontVehicle, LaneInfo lane, Transform laneTransform,
        VisualizationSettings settings, bool showLabel, Color? customColor = null) {
        var gap = MathUtilities.BumperGap(rearVehicle.Position, rearVehicle.Length, frontVehicle.Position, frontVehicle.Length, lane.Length);

        if (gap < 50.0f) {
            // Only show reasonable gaps
            Gizmos.color = customColor ?? settings.GapLineColor;

            var rearPos = laneTransform.position + laneTransform.forward * rearVehicle.Position;
            var frontPos = laneTransform.position + laneTransform.forward * frontVehicle.Position;

            // Draw gap line
            var rearFront = rearPos + laneTransform.forward * (rearVehicle.Length * 0.5f);
            var frontRear = frontPos - laneTransform.forward * (frontVehicle.Length * 0.5f);

            Gizmos.DrawLine(rearFront + Vector3.up * 0.2f, frontRear + Vector3.up * 0.2f);

            // Draw gap markers
            Gizmos.DrawLine(rearFront, rearFront + Vector3.up * 0.4f);
            Gizmos.DrawLine(frontRear, frontRear + Vector3.up * 0.4f);

            if (showLabel) {
                // Draw gap text with color coding
#if UNITY_EDITOR
                var gapCenter = (rearFront + frontRear) * 0.5f + Vector3.up * 0.6f;
                var gapColor = gap < 2.0f ? Color.red : gap < 5.0f ? Color.yellow : Color.green;
                Handles.color = gapColor;
                Handles.Label(gapCenter, $"{gap:F1}m");
#endif
            }
        }
    }

    private static void DrawLaneChangeTrajectory(LaneChangeState laneChangeState, VisualizationSettings settings) {
        if (!laneChangeState.Active) return;

        Gizmos.color = settings.LaneChangeColor;
        const int segments = 20;
        float3 prevPos = default;

        for (var i = 0; i <= segments; i++) {
            var s = laneChangeState.LongitudinalLength * (i / (float)segments);
            MathUtilities.EvaluateLaneChangeCurve(in laneChangeState, s, out var pos, out _);

            if (i > 0) {
                Gizmos.DrawLine(prevPos, pos);
            }

            prevPos = pos;

            // Draw progress indicator
            if (i == (int)(segments * (laneChangeState.ProgressS / laneChangeState.LongitudinalLength))) {
                Gizmos.color = Color.white;
                Gizmos.DrawSphere(pos, 0.2f);
                Gizmos.color = settings.LaneChangeColor;
            }
        }
    }

    private static void DrawVehicleSensors(VehicleState vehicle, IdmParameters idmParams, float3 position, float3 forward, VisualizationSettings settings) {
        // Draw detection range based on IDM parameters
        var detectionRange = vehicle.Speed * idmParams.HeadwayTime + idmParams.MinGap;

        Gizmos.color = new Color(0.0f, 1.0f, 0.0f, 0.2f);
        var sensorEnd = position + forward * detectionRange;

        // Draw sensor cone
        var sensorWidth = 1.0f;
        var right = math.cross(math.up(), forward);

        Gizmos.DrawLine(position, sensorEnd - right * sensorWidth);
        Gizmos.DrawLine(position, sensorEnd + right * sensorWidth);
        Gizmos.DrawLine(sensorEnd - right * sensorWidth, sensorEnd + right * sensorWidth);
    }

    private static void DrawVehicleInfo(VehicleState vehicle, LaneChangeState laneChangeState, IdmParameters idmParams, float3 position) {
#if UNITY_EDITOR
        Handles.color = Color.white;
        var infoPos = (Vector3)position + Vector3.up * 2.5f;

        var info = $"ID: {vehicle.VehicleId}\n" +
                   $"Lane: {vehicle.LaneIndex}\n" +
                   $"Pos: {vehicle.Position:F1}m\n" +
                   $"Speed: {vehicle.Speed:F1}/{idmParams.DesiredSpeed:F1}m/s\n" +
                   $"Accel: {vehicle.Acceleration:F2}m/s²";

        if (laneChangeState.Active) {
            var progress = laneChangeState.ProgressS / laneChangeState.LongitudinalLength;
            info += $"\nLC → {laneChangeState.TargetLaneIndex}\n" +
                    $"Progress: {progress * 100:F0}%";
        }

        Handles.Label(infoPos, info);
#endif
    }

    private static void DrawTrafficLightVisualization(WorldState worldState, VisualizationSettings settings) {
        var groupStates = worldState.TrafficLightGroupStates;
        var groupParameters = worldState.TrafficLightGroupParameters;

        var trafficLightGroups = FindObjectsByType<TrafficLightGroupAuthoring>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        for (var groupIndex = 0; groupIndex < math.min(groupStates.Length, trafficLightGroups.Length); groupIndex++) {
            var groupState = groupStates[groupIndex];
            var parameters = groupParameters[groupIndex];
            var group = trafficLightGroups[groupIndex];

            var color = TrafficLightMath.EvaluateColor(groupState.TimeInCycleSeconds, parameters);
            var gizmoColor = GetTrafficLightColor(color);

            if (settings.ShowStopLines) {
                DrawStopLines(group, gizmoColor, parameters, settings);
            }

            if (settings.ShowLightTimings) {
                DrawLightTimings(group, groupState, parameters, gizmoColor);
            }
        }
    }

    private static Color GetTrafficLightColor(TrafficLightColor color) {
        return color switch {
            TrafficLightColor.Green => Color.green,
            TrafficLightColor.Amber => new Color(1.0f, 0.65f, 0.0f, 1.0f),
            _ => Color.red,
        };
    }

    private static void DrawStopLines(TrafficLightGroupAuthoring group, Color color, TrafficLightGroupParameters parameters, VisualizationSettings settings) {
        Gizmos.color = color;

        foreach (var binding in group.LaneBindings) {
            if (binding?.Lane == null) continue;

            var laneTransform = binding.Lane.transform;
            var stopLineCenter = laneTransform.position + laneTransform.forward * binding.StopLinePositionMeters;
            var halfWidth = laneTransform.right * (settings.StopLineWidth * 0.5f);

            // Draw animated stop line
            var time = Time.time;
            var pulse = (math.sin(time * 3.0f) + 1.0f) * 0.5f;
            var animatedColor = Color.Lerp(color, Color.white, pulse * 0.3f);
            Gizmos.color = animatedColor;

            // Draw main stop line
            Gizmos.DrawLine(stopLineCenter - halfWidth, stopLineCenter + halfWidth);

            // Draw stop buffer zone
            var bufferStart = stopLineCenter - laneTransform.forward * parameters.AmberStopBufferMeters;
            var bufferHalfWidth = laneTransform.right * (settings.StopLineWidth * 0.3f);
            Gizmos.color = new Color(color.r, color.g, color.b, 0.3f);
            Gizmos.DrawLine(bufferStart - bufferHalfWidth, bufferStart + bufferHalfWidth);
            Gizmos.DrawLine(stopLineCenter, bufferStart);

            // Draw countdown indicators for amber
            if (color.r > 0.9f && color.g > 0.6f && color.b < 0.1f) {
                // Amber
                DrawCountdownIndicators(stopLineCenter, laneTransform, parameters);
            }
        }
    }

    private static void DrawCountdownIndicators(Vector3 stopLineCenter, Transform laneTransform, TrafficLightGroupParameters parameters) {
        // Draw animated countdown dots
        var dotCount = 5;
        var animSpeed = 2.0f;
        var time = Time.time;

        for (var i = 0; i < dotCount; i++) {
            var dotPos = stopLineCenter - laneTransform.forward * (i + 1) * 2.0f + Vector3.up * 0.5f;
            var dotPhase = (time * animSpeed + i * 0.5f) % (math.PI * 2.0f);
            var dotAlpha = (math.sin(dotPhase) + 1.0f) * 0.5f;

            Gizmos.color = new Color(1.0f, 0.65f, 0.0f, dotAlpha);
            Gizmos.DrawSphere(dotPos, 0.1f);
        }
    }

    private static void DrawLightTimings(TrafficLightGroupAuthoring group, TrafficLightGroupState state, TrafficLightGroupParameters parameters, Color currentColor) {
#if UNITY_EDITOR
        if (group.LaneBindings.Count == 0) return;

        var firstBinding = group.LaneBindings[0];
        if (firstBinding?.Lane == null) return;

        var labelPos = firstBinding.Lane.transform.position + Vector3.up * 3.0f;
        var totalCycle = parameters.GreenDurationSeconds + parameters.AmberDurationSeconds + parameters.RedDurationSeconds;
        var timeRemaining = totalCycle - (state.TimeInCycleSeconds % totalCycle);

        var timingInfo = $"Cycle: {totalCycle:F1}s\n" +
                         $"Remaining: {timeRemaining:F1}s\n" +
                         $"State: {GetLightStateName(currentColor)}";

        Handles.color = currentColor;
        Handles.Label(labelPos, timingInfo);
#endif
    }

    private static string GetLightStateName(Color color) {
        if (color == Color.green) return "GREEN";
        if (color.r > 0.9f && color.g > 0.6f) return "AMBER";
        return "RED";
    }

    private void DrawDebugInformation(WorldState worldState) {
#if UNITY_EDITOR
        if (!m_ShowDebugInfo) return;

        var debugPos = m_MainCamera != null ? m_MainCamera.ScreenToWorldPoint(new Vector3(10, Screen.height - 10, 10)) : Vector3.zero;

        var debugInfo = $"Simulation Debug Info:\n" +
                        $"Vehicles: {m_LastFrameVehicleCount}\n" +
                        $"Lanes: {worldState.Lanes.Length}\n" +
                        $"Traffic Lights: {worldState.TrafficLightGroupStates.Length}";

        if (m_ShowPerformanceMetrics) {
            debugInfo += $"\nRender Time: {m_LastRenderTime * 1000:F2}ms\n" +
                         $"FPS: {1.0f / Time.deltaTime:F0}";
        }

        if (m_ShowMemoryUsage) {
            var memoryMb = GC.GetTotalMemory(false) / (1024 * 1024);
            debugInfo += $"\nMemory: {memoryMb}MB";
        }

        Handles.color = Color.yellow;
        Handles.Label(debugPos, debugInfo);
#endif
    }

    private static void DrawLaneInfo(LaneInfo lane, LaneVehicleRange range, Transform laneTransform) {
#if UNITY_EDITOR
        var midPoint = laneTransform.position + laneTransform.forward * (lane.Length * 0.5f) + Vector3.up * 1.5f;
        var info = $"Lane {lane.LaneId}\n" +
                   $"Length: {lane.Length:F1}m\n" +
                   $"Speed: {lane.SpeedLimit:F1}m/s\n" +
                   $"Vehicles: {range.Count}\n" +
                   $"Density: {range.Count / (lane.Length / 100.0f):F1}/100m";

        Handles.color = Color.white;
        Handles.Label(midPoint, info);
#endif
    }

    private bool ShouldSkipBasedOnDistance(Vector3 position, VisualizationSettings settings) {
        Camera? sceneViewCamera = null;
#if UNITY_EDITOR
        sceneViewCamera = SceneView.lastActiveSceneView?.camera;
#endif
        if (!settings.EnableLevelOfDetail || (m_MainCamera == null && sceneViewCamera == null)) return false;
        if (settings.MaxDetailDistance <= 0.0f) return false;

        var cameraToUse = sceneViewCamera.OrNull() ?? m_MainCamera!;
        var distance = Vector3.Distance(cameraToUse.transform.position, position);
        return distance > settings.MaxDetailDistance;
    }

    private static void DrawArrowHead(Vector3 from, Vector3 to, float headLength = 0.25f, float headAngle = 20.0f) {
        var direction = (to - from).normalized;
        var right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + headAngle, 0) * Vector3.forward;
        var left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - headAngle, 0) * Vector3.forward;
        Gizmos.DrawLine(to, to + right * headLength);
        Gizmos.DrawLine(to, to + left * headLength);
    }
}
