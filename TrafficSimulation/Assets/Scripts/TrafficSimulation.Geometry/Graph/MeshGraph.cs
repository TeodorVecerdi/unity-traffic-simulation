using Sirenix.OdinInspector;
using TrafficSimulation.Geometry.Build;
using UnityEngine;

namespace TrafficSimulation.Geometry.Graph;

[Serializable]
public sealed class MeshGraph {
    [Required] public List<Layer> Layers = [];

    [Serializable]
    public sealed class Layer {
        [Required] public string Name = "Layer";
        [Required, SerializeReference] public MeshGenerator Generator = null!;
        [Required] public List<Material> Materials = null!;
        public bool Enabled;
    }
}
