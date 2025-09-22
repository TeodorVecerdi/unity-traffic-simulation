using Sirenix.OdinInspector;

namespace TrafficSimulation.Sim.Visualization;

[CreateAssetMenu(fileName = "VisualizationSettings", menuName = "Traffic Simulation/Visualization Settings")]
public sealed class VisualizationSettings : ScriptableObject {
    [Title("Vehicle Visualization")]
    [Tooltip("Show vehicle bodies and basic vehicle gizmos")]
    public bool ShowVehicles = true;

    [ShowIf(nameof(ShowVehicles))]
    [Tooltip("Display detailed information labels for vehicles")]
    public bool ShowVehicleLabels = true;

    [ShowIf(nameof(ShowVehicles))]
    [Tooltip("Show velocity vectors indicating speed and direction")]
    public bool ShowVelocityVectors = true;

    [ShowIf(nameof(ShowVehicles))]
    [Tooltip("Show acceleration vectors (green for acceleration, red for braking)")]
    public bool ShowAccelerationVectors = true;

    [ShowIf(nameof(ShowVehicles))]
    [Tooltip("Display gap distances to leading vehicles")]
    public bool ShowVehicleGaps = true;

    [ShowIf(nameof(ShowVehicles))]
    [Tooltip("Show lane change trajectories when vehicles are changing lanes")]
    public bool ShowLaneChangeTrajectories = true;

    [ShowIf(nameof(ShowVehicles))]
    [Tooltip("Show desired speed vs actual speed comparison")]
    public bool ShowSpeedComparison;

    [ShowIf(nameof(ShowVehicles))]
    [Tooltip("Highlight vehicles based on their current behavior state")]
    public bool ShowBehaviorStates;

    [Title("Vehicle Appearance")]
    [ShowIf(nameof(ShowVehicles))]
    [PropertyRange(0.5f, 4.0f)]
    [Tooltip("Width of vehicle visualization")]
    public float VehicleWidth = 2.0f;

    [ShowIf(nameof(ShowVehicles))]
    [Tooltip("Main body color for vehicles")]
    public Color VehicleBodyColor = new(0.2f, 0.6f, 1.0f, 0.8f);

    [ShowIf(nameof(ShowVehicles))]
    [Tooltip("Border color for vehicle outlines")]
    public Color VehicleBorderColor = Color.white;

    [ShowIf(nameof(ShowVehicles))]
    [PropertyRange(0.1f, 5.0f)]
    [Tooltip("Scale factor for velocity vector length")]
    public float VelocityVectorScale = 2.0f;

    [ShowIf(nameof(ShowVehicles))]
    [PropertyRange(0.1f, 3.0f)]
    [Tooltip("Scale factor for acceleration vector length")]
    public float AccelerationVectorScale = 1.5f;

    [Title("Lane Visualization")]
    [Tooltip("Show lane centerlines and boundaries")]
    public bool ShowLanes = true;

    [ShowIf(nameof(ShowLanes))]
    [Tooltip("Display lane information labels")]
    public bool ShowLaneLabels;

    [ShowIf(nameof(ShowLanes))]
    [Tooltip("Show connections between adjacent lanes")]
    public bool ShowLaneConnections;

    [ShowIf(nameof(ShowLanes))]
    [Tooltip("Highlight lanes with heavy traffic")]
    public bool ShowTrafficDensity;

    [ShowIf(nameof(ShowLanes))]
    [Tooltip("Color for lane centerlines")]
    public Color LaneColor = new(1.0f, 1.0f, 0.0f, 0.3f);

    [ShowIf(nameof(ShowLanes))]
    [Tooltip("Color for lane connection indicators")]
    public Color LaneConnectionColor = new(0.0f, 1.0f, 1.0f, 0.5f);

    [Title("Traffic Light Visualization")]
    [Tooltip("Show traffic light states and stop lines")]
    public bool ShowTrafficLights = true;

    [ShowIf(nameof(ShowTrafficLights))]
    [Tooltip("Display stop lines at intersections")]
    public bool ShowStopLines = true;

    [ShowIf(nameof(ShowTrafficLights))]
    [Tooltip("Show timing information for traffic light cycles")]
    public bool ShowLightTimings;

    [ShowIf(nameof(ShowTrafficLights))]
    [PropertyRange(1.0f, 10.0f)]
    [Tooltip("Width of stop line indicators")]
    public float StopLineWidth = 3.0f;

    [Title("Performance")]
    [Tooltip("Only show detailed information for selected vehicles (improves performance)")]
    public bool OnlyShowSelectedVehicleDetails;

    [Tooltip("Maximum distance to show vehicle details (0 = unlimited)")]
    [PropertyRange(0.0f, 200.0f)]
    public float MaxDetailDistance;

    [Tooltip("Enable adaptive level of detail based on camera distance")]
    public bool EnableLevelOfDetail = true;

    [Title("Color Coding")]
    [Tooltip("Color for velocity vectors")]
    public Color VelocityColor = Color.green;

    [Tooltip("Color for positive acceleration")]
    public Color AccelerationPositiveColor = Color.green;

    [Tooltip("Color for negative acceleration (braking)")]
    public Color AccelerationNegativeColor = Color.red;

    [Tooltip("Color for gap distance indicators")]
    public Color GapLineColor = new(1.0f, 1.0f, 0.0f, 0.6f);

    [Tooltip("Color for lane change trajectory curves")]
    public Color LaneChangeColor = new(1.0f, 0.0f, 1.0f, 0.8f);

    [Title("Advanced Features")]
    [Tooltip("Show vehicle sensor ranges and detection zones")]
    public bool ShowVehicleSensors;

    [Tooltip("Display flow rates and throughput metrics")]
    public bool ShowFlowMetrics;

    [Tooltip("Highlight bottlenecks and congestion areas")]
    public bool ShowBottlenecks;
}
