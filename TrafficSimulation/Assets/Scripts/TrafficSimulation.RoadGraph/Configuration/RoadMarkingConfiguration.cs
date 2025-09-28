using Sirenix.OdinInspector;
using UnityEngine;

namespace TrafficSimulation.RoadGraph.Configuration;

[CreateAssetMenu(fileName = "RoadMarkingConfiguration", menuName = "Traffic Simulation/Road Marking Configuration")]
public sealed class RoadMarkingConfiguration : ScriptableObject {
    [SerializeField, Unit(Units.Meter, Units.Millimeter), MinValue(0.0f)]
    private float m_Width = 0.15f;
    [SerializeField, Unit(Units.Meter), MinValue(0.0f)]
    private float m_DashedLength = 3f;
    [SerializeField, Unit(Units.Meter), MinValue(0.0f)]
    private float m_DashedGapLength = 3f;

    public float Width => m_Width;
    public float DashedLength => m_DashedLength;
    public float DashedGapLength => m_DashedGapLength;
}
