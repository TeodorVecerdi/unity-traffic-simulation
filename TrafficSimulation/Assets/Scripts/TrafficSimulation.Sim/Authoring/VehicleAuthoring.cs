namespace TrafficSimulation.Sim.Authoring;

public sealed class VehicleAuthoring : MonoBehaviour {
    [Title("Core Properties")]
    [SerializeField] private int m_VehicleId;
    [SerializeField, Required] private LaneAuthoring m_Lane = null!;
    [SerializeField] private float m_Length = 4.5f;

    [Title("Initial Conditions")]
    [SerializeField, MinValue(0.0f), MaxValue("@m_Lane != null ? m_Lane.Length : 100.0f")]
    private float m_InitialPosition;
    [SerializeField, Unit(Units.MetersPerSecond)]
    private float m_InitialSpeed;

    [Title("IDM Parameters")]
    [SerializeField, MinValue(1.0f), Unit(Units.MetersPerSecond)]
    private float m_DesiredSpeed = 30.0f;
    [SerializeField, MinValue(0.1f), Unit(Units.MetersPerSecondSquared)]
    private float m_MaxAcceleration = 1.4f;

    [SerializeField, MinValue(0.1f), Unit(Units.MetersPerSecondSquared)]
    private float m_ComfortableBraking = 2.0f;
    [SerializeField, MinValue(0.1f), Unit(Units.Second)]
    private float m_HeadwayTime = 1.6f;
    [SerializeField, MinValue(0.0f), Unit(Units.Meter)]
    private float m_MinGap = 2.0f;
    [SerializeField, MinValue(1.0f)]
    private float m_AccelerationExponent = 4.0f;

    [Title("MOBIL Lane Change Parameters")]
    [SerializeField, PropertyRange(0.0f, 1.0f)]
    private float m_Politeness = 0.1f;
    [SerializeField, MinValue(0.0f), Unit(Units.MetersPerSecondSquared)]
    private float m_AdvantageThreshold = 0.2f;
    [SerializeField, MinValue(0.1f), Unit(Units.MetersPerSecondSquared)]
    private float m_SafeBrakingDeceleration = 4.0f;
    [SerializeField, MinValue(0.0f), Unit(Units.Second)]
    private float m_MinTimeBetweenLaneChanges = 2.0f;

    [Title("Gizmos")]
    [SerializeField] private Color m_VehicleColor = Color.cyan;
    [SerializeField] private float m_VehicleWidth = 2.0f;
    [SerializeField] private bool m_AlwaysDrawGizmos;

    /// <summary>
    /// Represents the unique identifier assigned to a vehicle within the traffic simulation.
    /// This identifier is used to distinguish vehicles from one another within the system.
    /// </summary>
    public int VehicleId {
        get => m_VehicleId;
        set => m_VehicleId = value;
    }

    /// <summary>
    /// Represents the lane associated with a vehicle in the traffic simulation. This property
    /// defines the lane where the vehicle is positioned and is responsible for providing context
    /// such as lane geometry and driving constraints.
    /// </summary>
    public LaneAuthoring Lane {
        get => m_Lane;
        set => m_Lane = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Specifies the physical length of the vehicle, measured in meters. This value is used in
    /// simulations to determine the spatial occupancy of the vehicle on a lane and to model
    /// interactions with other vehicles.
    /// </summary>
    public float Length {
        get => m_Length;
        set => m_Length = value;
    }

    /// <summary>
    /// Specifies the initial position of the vehicle along its assigned lane, measured in meters.
    /// This value determines the starting location of the vehicle relative to the beginning of the
    /// lane.
    /// </summary>
    public float InitialPosition {
        get => m_InitialPosition;
        set => m_InitialPosition = value;
    }

    /// <summary>
    /// Specifies the initial speed of the vehicle in meters per second (m/s) when the simulation
    /// starts. This value determines the velocity at which the vehicle begins its movement in the
    /// simulation environment.
    /// </summary>
    public float InitialSpeed {
        get => m_InitialSpeed;
        set => m_InitialSpeed = value;
    }

    /// <summary>
    /// Specifies the target speed for the vehicle, expressed in meters per second (m/s).
    /// This value determines the intended cruising speed under optimal driving conditions,
    /// considering road constraints and other traffic influences.
    /// </summary>
    public float DesiredSpeed {
        get => m_DesiredSpeed;
        set => m_DesiredSpeed = value;
    }

    /// <summary>
    /// Specifies the maximum acceleration capability of the vehicle, measured in m/s^2.
    /// This value represents the highest rate of increase in speed that the vehicle can achieve
    /// under ideal conditions, such as optimal traction and no external constraints.
    /// </summary>
    public float MaxAcceleration {
        get => m_MaxAcceleration;
        set => m_MaxAcceleration = value;
    }

    /// <summary>
    /// Represents the deceleration rate, in m/s^2, that provides a comfortable braking experience
    /// for passengers. This value is used to model typical braking behavior that minimizes discomfort
    /// during deceleration.
    /// </summary>
    public float ComfortableBraking {
        get => m_ComfortableBraking;
        set => m_ComfortableBraking = value;
    }

    /// <summary>
    /// Represents the desired headway time, expressed in seconds, which a driver aims to maintain
    /// from the vehicle ahead. This value influences the safe following distance and is a critical
    /// parameter for traffic and vehicle behavior modeling.
    /// </summary>
    public float HeadwayTime {
        get => m_HeadwayTime;
        set => m_HeadwayTime = value;
    }

    /// <summary>
    /// Represents the minimum desired distance, measured in meters, that a vehicle should maintain
    /// from the vehicle ahead during normal operation to ensure safety.
    /// </summary>
    public float MinGap {
        get => m_MinGap;
        set => m_MinGap = value;
    }

    /// <summary>
    /// Represents the acceleration exponent used in the Intelligent Driver Model (IDM).
    /// This parameter influences the sensitivity of a vehicle's acceleration and deceleration
    /// behavior, with higher values leading to more aggressive responses to speed adjustments.
    /// </summary>
    public float AccelerationExponent {
        get => m_AccelerationExponent;
        set => m_AccelerationExponent = value;
    }

    /// <summary>
    /// Represents the degree of consideration a vehicle exhibits toward others during lane changes.
    /// This value ranges between 0.0 and 1.0, where a lower value indicates more selfish behavior
    /// while a higher value reflects greater politeness or willingness to yield to other vehicles.
    /// </summary>
    public float Politeness {
        get => m_Politeness;
        set => m_Politeness = value;
    }

    /// <summary>
    /// Specifies the minimum acceleration advantage, expressed in meters per second squared (m/s^2),
    /// required to justify a lane change. This threshold helps determine whether the potential benefits
    /// of changing lanes outweigh the potential risks or disadvantages.
    /// </summary>
    public float AdvantageThreshold {
        get => m_AdvantageThreshold;
        set => m_AdvantageThreshold = value;
    }

    /// <summary>
    /// Represents the maximum safe deceleration rate for the vehicle, expressed in m/s^2. This value
    /// specifies the deceleration limit that ensures the vehicle can safely reduce speed without
    /// compromising safety under typical driving conditions.
    /// </summary>
    public float SafeBrakingDeceleration {
        get => m_SafeBrakingDeceleration;
        set => m_SafeBrakingDeceleration = value;
    }

    /// <summary>
    /// Specifies the minimum time interval, in seconds, that must elapse between successive lane changes
    /// by the vehicle. This parameter ensures a safe and realistic frequency of lane changes to prevent
    /// erratic or aggressive driving behavior.
    /// </summary>
    public float MinTimeBetweenLaneChanges {
        get => m_MinTimeBetweenLaneChanges;
        set => m_MinTimeBetweenLaneChanges = value;
    }

    private void OnDrawGizmos() {
        if (!m_AlwaysDrawGizmos) return;
        DrawCarGizmos();
    }

    private void OnDrawGizmosSelected() {
        if (m_AlwaysDrawGizmos) return;
        DrawCarGizmos();
    }

    private void DrawCarGizmos() {
        if (m_Lane == null)
            return;
        if (m_InitialPosition < 0.0f || m_InitialPosition > m_Lane.Length)
            return;
        var laneTransform = m_Lane.transform;
        var position = laneTransform.position + laneTransform.forward * m_InitialPosition;
        DrawCarBox(position, laneTransform.forward, laneTransform.right, m_Length, m_VehicleWidth, m_VehicleColor);
    }

    private static void DrawCarBox(Vector3 center, Vector3 forward, Vector3 right, float length, float width, Color color) {
        Gizmos.color = color;
        var f = forward * (length * 0.5f);
        var r = right * (width * 0.5f);
        var c = center;

        var p0 = c - f - r;
        var p1 = c - f + r;
        var p2 = c + f + r;
        var p3 = c + f - r;

        Gizmos.DrawLine(p0, p1);
        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p2, p3);
        Gizmos.DrawLine(p3, p0);

        Gizmos.DrawLine(p2, p2 + forward * 0.5f);
        Gizmos.DrawLine(p3, p3 + forward * 0.5f);
    }
}
