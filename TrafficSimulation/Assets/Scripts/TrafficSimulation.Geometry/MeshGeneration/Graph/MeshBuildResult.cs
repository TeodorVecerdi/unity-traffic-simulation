using UnityEngine;

namespace TrafficSimulation.Geometry.MeshGeneration.Graph;

public readonly struct MeshBuildResult(Mesh mesh, Material[] materials) {
    public readonly Mesh Mesh = mesh;
    public readonly Material[] Materials = materials;
}
