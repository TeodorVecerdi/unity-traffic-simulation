using Sirenix.OdinInspector;
using UnityEngine;

namespace TrafficSimulation.Roads.MeshGeneration.Graph;

[Serializable]
public sealed class MeshGraph {
    [Required] public List<Layer> Layers = [];

    [Serializable]
    public sealed class Layer {
        [Required] public string Name = "Layer";
        [Required, SerializeReference] public MeshGenerator Generator = null!;
        [Required] public Material Material = null!;
        public bool Enabled;
    }
}
