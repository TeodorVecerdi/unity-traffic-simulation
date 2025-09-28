using Sirenix.OdinInspector;
using UnityEngine;

namespace TrafficSimulation.RoadGraph.Configuration;

[CreateAssetMenu(fileName = "SidewalkConfiguration", menuName = "Traffic Simulation/Sidewalk Configuration")]
public sealed class SidewalkConfiguration : ScriptableObject {
    [SerializeField, Unit(Units.Meter), MinValue(0.0f)]
    private float m_Width = 2.5f;
    [SerializeField, Unit(Units.Meter), MinValue(0.0f)]
    private float m_CurbHeight = 0.15f;

    public float Width => m_Width;
    public float CurbHeight => m_CurbHeight;
}
