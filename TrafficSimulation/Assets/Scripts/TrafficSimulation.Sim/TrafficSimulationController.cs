using Microsoft.Extensions.Logging;
using Sirenix.OdinInspector;
using TrafficSimulation.Core.Random;
using TrafficSimulation.Sim.Authoring;
using TrafficSimulation.Sim.Components;
using TrafficSimulation.Sim.Jobs;
using TrafficSimulation.Sim.Math;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Vecerdi.Extensions.DependencyInjection;

namespace TrafficSimulation.Sim;

public sealed class TrafficSimulationController : BaseMonoBehaviour {
    [Inject] public ILogger<TrafficSimulationController> Logger { get; set; } = null!;

    [Title("Simulation Settings")]
    [SerializeField, Min(0.001f), Unit(Units.Second)] private float m_TimeStep = 0.02f;
    [SerializeField, PropertyRange(0.0f, 4.0f)] private float m_TimeScale = 1.0f;
    [SerializeField] private bool m_Paused;
    [SerializeField, Min(1)] private int m_MaxStepsPerFrame = 8;

    private float m_AccumulatedTime;
    private int m_ManualStepsToSimulate;

    private readonly Dictionary<int, VehicleAuthoring> m_VehicleIdMap = new();
    private readonly Dictionary<int, LaneAuthoring> m_LaneIdMap = new();
    private TrafficLightGroupAuthoring[]? m_TrafficLightGroups;
    private WorldState? m_WorldState;

    private void Start() {
        SetupWorldState();
    }

    private void OnDestroy() {
        m_WorldState?.Dispose();
        m_WorldState = null;
    }

    private void Update() {
        var stepsThisFrame = 0;
        if (!m_Paused && m_TimeScale > 0.0f) {
            // Accumulate real time so simulation step frequency stays constant; scale the per-step delta instead.
            m_AccumulatedTime += Time.deltaTime;
        }

        while (!m_Paused && m_TimeScale > 0.0f && m_AccumulatedTime >= m_TimeStep && stepsThisFrame < m_MaxStepsPerFrame) {
            var effectiveStep = m_TimeStep * m_TimeScale;
            ScheduleSimulation(effectiveStep).Complete();
            m_AccumulatedTime -= m_TimeStep;
            stepsThisFrame++;
        }

        if (m_ManualStepsToSimulate > 0) {
            var stepsToSimulate = m_ManualStepsToSimulate;
            m_ManualStepsToSimulate = 0;
            while (stepsToSimulate > 0) {
                ScheduleSimulation(m_TimeStep).Complete();
                stepsToSimulate--;
            }
        }

        SyncRenderers();
    }

    [Button, DisableInEditorMode]
    private void StepSimulation() {
        m_ManualStepsToSimulate++;
    }

    [Button, DisableInEditorMode]
    private void StepSimulation(int steps) {
        if (steps < 1)
            throw new ArgumentOutOfRangeException(nameof(steps), "Steps must be at least 1.");
        m_ManualStepsToSimulate += steps;
    }

    private void SetupWorldState() {
        if (m_WorldState != null)
            throw new InvalidOperationException("WorldState is already initialized.");

        var allVehicles = FindObjectsByType<VehicleAuthoring>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        DestroyInvalidVehicles(allVehicles);

        var vehicles = allVehicles.Where(v => v != null && v.Lane != null).ToList();
        var vehicleStates = new NativeArray<VehicleState>(vehicles.Count, Allocator.Persistent);
        var idmParameters = new NativeArray<IdmParameters>(vehicles.Count, Allocator.Persistent);
        var mobilParameters = new NativeArray<MobilParameters>(vehicles.Count, Allocator.Persistent);
        var laneChangeStates = new NativeArray<LaneChangeState>(vehicles.Count, Allocator.Persistent);
        var accelerations = new NativeArray<float>(vehicles.Count, Allocator.Persistent);

        var lanes = FindObjectsByType<LaneAuthoring>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        var laneData = new NativeArray<LaneInfo>(lanes.Length, Allocator.Persistent);
        var laneRanges = new NativeArray<LaneVehicleRange>(lanes.Length, Allocator.Persistent);
        var laneIdToIndex = lanes.Index().ToDictionary(pair => pair.Item.LaneId, pair => pair.Index);

        // Traffic lights
        m_TrafficLightGroups = FindObjectsByType<TrafficLightGroupAuthoring>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        var trafficLightGroupParameters = new NativeArray<TrafficLightGroupParameters>(m_TrafficLightGroups.Length, Allocator.Persistent);
        var trafficLightGroupStates = new NativeArray<TrafficLightGroupState>(m_TrafficLightGroups.Length, Allocator.Persistent);
        var totalBindings = m_TrafficLightGroups.Sum(trafficLightGroup => trafficLightGroup.LaneBindings.Count);
        var trafficLightLaneBindings = new NativeArray<TrafficLightLaneBinding>(totalBindings, Allocator.Persistent);

        m_LaneIdMap.Clear();
        foreach (var lane in lanes) {
            if (lane == null) continue;
            if (!m_LaneIdMap.TryAdd(lane.LaneId, lane)) {
                throw new ArgumentException($"Duplicate LaneId detected: {lane.LaneId}. LaneIds must be unique.");
            }
        }

        // Setup vehicles
        for (var i = 0; i < vehicles.Count; i++) {
            var vehicle = vehicles[i];
            ValidateVehicleParameters(vehicle);

            // Clamp initial position within lane length
            var position = math.clamp(vehicle.InitialPosition, 0.0f, vehicle.Lane.Length);
            var speed = math.max(0.0f, vehicle.InitialSpeed);

            vehicleStates[i] = new VehicleState(vehicle.VehicleId, laneIdToIndex[vehicle.Lane.LaneId], position, speed, 0.0f, vehicle.Length);
            idmParameters[i] = new IdmParameters(vehicle.DesiredSpeed, vehicle.MaxAcceleration, vehicle.ComfortableBraking, vehicle.HeadwayTime, vehicle.MinGap, vehicle.AccelerationExponent);
            mobilParameters[i] = new MobilParameters(vehicle.Politeness, vehicle.AdvantageThreshold, vehicle.SafeBrakingDeceleration, vehicle.MinTimeBetweenLaneChanges);
            laneChangeStates[i] = new LaneChangeState() { Cooldown = Rand.Range(0.0f, mobilParameters[i].MinTimeBetweenLaneChanges) };
            accelerations[i] = 0.0f;
        }

        for (var i = 0; i < lanes.Length; i++) {
            var lane = lanes[i];
            ValidateLaneParameters(lane);

            var leftLaneIndex = laneIdToIndex.GetValueOrDefault(lane.LeftLane?.LaneId ?? -1, -1);
            var rightLaneIndex = laneIdToIndex.GetValueOrDefault(lane.RightLane?.LaneId ?? -1, -1);
            laneData[i] = new LaneInfo(lane.LaneId, lane.Length, leftLaneIndex, rightLaneIndex, lane.SpeedLimit);
            laneRanges[i] = new LaneVehicleRange(0, 0);
        }

        // Setup traffic lights
        var bindingWriteCursor = 0;
        for (var groupIndex = 0; groupIndex < m_TrafficLightGroups.Length; groupIndex++) {
            var group = m_TrafficLightGroups[groupIndex];
            var parameters = group.Parameters;
            trafficLightGroupParameters[groupIndex] = parameters;
            trafficLightGroupStates[groupIndex] = new TrafficLightGroupState(parameters.StartTimeOffsetSeconds);

            var bindings = group.LaneBindings;
            foreach (var binding in bindings) {
                if (binding == null || binding.Lane == null)
                    continue;
                if (!laneIdToIndex.TryGetValue(binding.Lane.LaneId, out var laneIndex))
                    continue;

                trafficLightLaneBindings[bindingWriteCursor++] = new TrafficLightLaneBinding(laneIndex, groupIndex, binding.StopLinePositionMeters);
            }
        }

        if (bindingWriteCursor < trafficLightLaneBindings.Length) {
            // Trim trailing unused entries if any lanes were invalid
            var trimmed = new NativeArray<TrafficLightLaneBinding>(bindingWriteCursor, Allocator.Persistent);
            for (var k = 0; k < bindingWriteCursor; k++)
                trimmed[k] = trafficLightLaneBindings[k];
            trafficLightLaneBindings.Dispose();
            trafficLightLaneBindings = trimmed;
        }

        m_WorldState = new WorldState(
            vehicleStates,
            idmParameters,
            mobilParameters,
            laneChangeStates,
            accelerations,
            laneData,
            laneRanges,
            trafficLightGroupParameters,
            trafficLightGroupStates,
            trafficLightLaneBindings
        );
    }

    private void SyncRenderers() {
        if (m_WorldState == null)
            throw new InvalidOperationException("WorldState is not initialized.");

        var vehicles = m_WorldState.Vehicles;
        var lanes = m_WorldState.Lanes;
        var laneChangeStates = m_WorldState.LaneChangeStates;
        for (var i = 0; i < vehicles.Length; i++) {
            var vehicleState = vehicles[i];
            if (!m_VehicleIdMap.TryGetValue(vehicleState.VehicleId, out var vehicleAuthoring)) {
                Logger.LogWarning("No VehicleAuthoring found for VehicleId: {VehicleId}", vehicleState.VehicleId);
                continue;
            }

            if (vehicleAuthoring == null || vehicleAuthoring.Lane == null)
                continue;

            var lane = lanes[vehicleState.LaneIndex];
            if (!m_LaneIdMap.TryGetValue(lane.LaneId, out var laneAuthoring) || laneAuthoring == null) {
                Logger.LogWarning("No LaneAuthoring found for LaneId: {LaneId}", lane.LaneId);
                continue;
            }

            var laneChangeState = laneChangeStates[i];
            float3 position;
            float3 forward;
            if (laneChangeState.Active) {
                // Initialize geometry on first frame of lane change
                if (math.lengthsq(laneChangeState.Forward) < 1e-6f) {
                    InitializeLaneChangeGeometry(ref laneChangeState, in vehicleState);
                    laneChangeStates[i] = laneChangeState;
                }

                MathUtilities.EvaluateLaneChangeCurve(in laneChangeState, laneChangeState.ProgressS, out position, out forward);
            } else {
                var laneTransform = laneAuthoring.transform;
                position = laneTransform.position + laneTransform.forward * vehicleState.Position;
                forward = laneTransform.forward;
            }

            var rotation = Quaternion.LookRotation(forward, Vector3.up);
            vehicleAuthoring.transform.SetPositionAndRotation(position, rotation);
            vehicleAuthoring.VehicleState = vehicleState;
            vehicleAuthoring.LaneChangeState = laneChangeState;
        }
    }

    private void InitializeLaneChangeGeometry(ref LaneChangeState laneChangeState, in VehicleState vehicleState) {
        // Get source and target lanes
        if (!m_LaneIdMap.TryGetValue(m_WorldState!.Lanes[vehicleState.LaneIndex].LaneId, out var sourceLane))
            return;
        if (laneChangeState.TargetLaneIndex < 0 || laneChangeState.TargetLaneIndex >= m_WorldState!.Lanes.Length)
            return;
        var targetLaneInfo = m_WorldState.Lanes[laneChangeState.TargetLaneIndex];
        if (!m_LaneIdMap.TryGetValue(targetLaneInfo.LaneId, out var targetLane))
            return;

        var sourceTransform = sourceLane.transform;
        var targetTransform = targetLane.transform;

        var sourcePosition = (float3)sourceTransform.position;
        var targetPosition = (float3)targetTransform.position;
        var sourceForward = (float3)sourceTransform.forward;
        var sourceRight = (float3)sourceTransform.right;

        // Base curve frame at start s=0 (current longitudinal position on source lane)
        var p0 = sourcePosition + sourceForward * vehicleState.Position;

        // Signed lateral offset from source to target lane center projected on left axis
        var delta = targetPosition - sourcePosition;
        var lateralOffset = math.dot(delta, -sourceRight);

        laneChangeState.P0 = p0;
        laneChangeState.Forward = sourceForward;
        laneChangeState.Left = -sourceRight;
        laneChangeState.LateralOffset = lateralOffset;
    }

    private void DestroyInvalidVehicles(VehicleAuthoring[] allVehicles) {
        var invalidVehicles = allVehicles.Where(v => v.Lane == null).ToList();
        if (invalidVehicles.Count > 0) {
            var invalidIds = string.Join(", ", invalidVehicles.Select(v => v.VehicleId));
            Logger.LogError("The following vehicles have no assigned lane and will be ignored: {VehicleIds}", invalidIds);
            invalidVehicles.ForEach(v => Destroy(v.gameObject));
            invalidVehicles.Clear();
        }

        m_VehicleIdMap.Clear();
        foreach (var vehicle in allVehicles) {
            if (vehicle == null) continue;
            if (!m_VehicleIdMap.TryAdd(vehicle.VehicleId, vehicle)) {
                Logger.LogError("Duplicate VehicleId detected: {VehicleId}. VehicleIds must be unique. Ignoring duplicate.", vehicle.VehicleId);
                Destroy(vehicle.gameObject);
            }
        }
    }

    private static void ValidateVehicleParameters(VehicleAuthoring vehicle) {
        if (vehicle.Length <= 0.0f)
            throw new ArgumentException($"Vehicle length must be positive. VehicleId: {vehicle.VehicleId}");
        if (vehicle.DesiredSpeed <= 0.0f)
            throw new ArgumentException($"Vehicle desired speed must be positive. VehicleId: {vehicle.VehicleId}");
        if (vehicle.MaxAcceleration <= 0.0f)
            throw new ArgumentException($"Vehicle max acceleration must be positive. VehicleId: {vehicle.VehicleId}");
        if (vehicle.ComfortableBraking <= 0.0f)
            throw new ArgumentException($"Vehicle comfortable braking must be positive. VehicleId: {vehicle.VehicleId}");
        if (vehicle.HeadwayTime <= 0.0f)
            throw new ArgumentException($"Vehicle headway time must be positive. VehicleId: {vehicle.VehicleId}");
        if (vehicle.MinGap < 0.0f)
            throw new ArgumentException($"Vehicle min gap must be non-negative. VehicleId: {vehicle.VehicleId}");
        if (vehicle.AccelerationExponent < 1.0f)
            throw new ArgumentException($"Vehicle acceleration exponent must be at least 1. VehicleId: {vehicle.VehicleId}");
    }

    private static void ValidateLaneParameters(LaneAuthoring lane) {
        if (lane.Length <= 0.0f)
            throw new ArgumentException($"Lane length must be positive. LaneId: {lane.LaneId}");
        if (lane.SpeedLimit <= 0.0f)
            throw new ArgumentException($"Lane speed limit must be positive. LaneId: {lane.LaneId}");
    }

    private JobHandle ScheduleSimulation(float timeStep) {
        if (m_WorldState == null)
            throw new InvalidOperationException("WorldState is not initialized.");

        var safetyCheckJob = default(JobHandle);
        // Ensure vehicles' state is valid (e.g., non-negative speed), ensure range data is valid (positive length)
        // var safetyCheckJob = new SafetyCheckJob {
        //     Lanes = default,
        //     Vehicles = default,
        // }.Schedule();

        // Sort vehicles by lane and position, update lane ranges
        var sortVehiclesJob = new SortVehiclesAndUpdateLaneRangesJob {
            Lanes = m_WorldState.Lanes,
            LaneRanges = m_WorldState.LaneRanges,
            Vehicles = m_WorldState.Vehicles,
            IdmParameters = m_WorldState.IdmParameters,
            MobilParameters = m_WorldState.MobilParameters,
            LaneChangeStates = m_WorldState.LaneChangeStates,
        }.Schedule(safetyCheckJob);

        // Compute accelerations using IDM
        var idmJob = new IntelligentDriverModelJob {
            Vehicles = m_WorldState.Vehicles,
            IdmParameters = m_WorldState.IdmParameters,
            Lanes = m_WorldState.Lanes,
            LaneRanges = m_WorldState.LaneRanges,
            LaneChangeStates = m_WorldState.LaneChangeStates,
            Accelerations = m_WorldState.Accelerations,
        }.Schedule(m_WorldState.Vehicles.Length, 64, sortVehiclesJob);

        // Decide lane changes (MOBIL)
        var mobilJob = new MobilLaneChangeDecisionJob {
            Vehicles = m_WorldState.Vehicles,
            IdmParameters = m_WorldState.IdmParameters,
            MobilParameters = m_WorldState.MobilParameters,
            Lanes = m_WorldState.Lanes,
            LaneRanges = m_WorldState.LaneRanges,
            Accelerations = m_WorldState.Accelerations,
            LaneChangeStates = m_WorldState.LaneChangeStates,
        }.Schedule(m_WorldState.Vehicles.Length, 64, idmJob);

        // Integrate vehicle state
        var integrateJob = new IntegrateVehicleStateJob {
            DeltaTime = timeStep,
            Vehicles = m_WorldState.Vehicles,
            LaneChangeStates = m_WorldState.LaneChangeStates,
            MobilParameters = m_WorldState.MobilParameters,
            Accelerations = m_WorldState.Accelerations,
            Lanes = m_WorldState.Lanes,
        }.Schedule(m_WorldState.Vehicles.Length, 64, mobilJob);

        return integrateJob;
    }
}
