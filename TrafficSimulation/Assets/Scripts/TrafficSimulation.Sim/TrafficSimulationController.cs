using Microsoft.Extensions.Logging;
using Sirenix.OdinInspector;
using TrafficSimulation.Sim.Authoring;
using TrafficSimulation.Sim.Components;
using TrafficSimulation.Sim.Jobs;
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
            laneChangeStates[i] = default;
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

        m_WorldState = new WorldState(vehicleStates, idmParameters, mobilParameters, laneChangeStates, accelerations, laneData, laneRanges);
    }

    private void SyncRenderers() {
        if (m_WorldState == null)
            throw new InvalidOperationException("WorldState is not initialized.");

        var vehicles = m_WorldState.Vehicles;
        var lanes = m_WorldState.Lanes;
        foreach (var vehicleState in vehicles) {
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

            var laneTransform = laneAuthoring.transform;
            var position = laneTransform.position + laneTransform.forward * vehicleState.Position;
            var rotation = Quaternion.LookRotation(laneTransform.forward, Vector3.up);
            vehicleAuthoring.transform.SetPositionAndRotation(position, rotation);
            vehicleAuthoring.VehicleState = vehicleState;
        }
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
            Accelerations = m_WorldState.Accelerations,
        }.Schedule(m_WorldState.Vehicles.Length, 64, sortVehiclesJob);

        // Integrate vehicle state
        var integrateJob = new IntegrateVehicleStateJob {
            DeltaTime = timeStep,
            Vehicles = m_WorldState.Vehicles,
            Lanes = m_WorldState.Lanes,
            Accelerations = m_WorldState.Accelerations,
        }.Schedule(m_WorldState.Vehicles.Length, 64, idmJob);

        return integrateJob;
    }
}
