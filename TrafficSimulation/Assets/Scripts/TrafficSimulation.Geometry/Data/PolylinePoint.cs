using Sirenix.OdinInspector;
using Unity.Mathematics;

namespace TrafficSimulation.Geometry.Data;

[Serializable]
public struct PolylinePoint {
    [HideLabel]
    public float3 Position;
    [LabelText("Hard?")]
    public bool HardEdge;
}
