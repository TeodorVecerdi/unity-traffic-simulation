using TrafficSimulation.Geometry.Data;
using Unity.Collections;
using UnityEngine;

namespace TrafficSimulation.Geometry.Build;

internal struct SurfaceAggregator(Material material, Allocator allocator) : IDisposable {
    public readonly Material Material = material;
    public NativeList<MeshVertex> Vertices = new(GeometryChunk.DefaultVertexCapacity, allocator);
    public NativeList<int> Indices = new(GeometryChunk.DefaultIndexCapacity, allocator);

    public void Dispose() {
        if (Vertices.IsCreated) Vertices.Dispose();
        if (Indices.IsCreated) Indices.Dispose();
    }

    public void AppendChunk(ref NativeList<MeshVertex> vertices, ref NativeList<int> indices) {
        var baseV = Vertices.Length;
        Vertices.AddRange(vertices.AsArray());

        Indices.Capacity = Indices.Length + indices.Length;
        for (var i = 0; i < indices.Length; i++) {
            Indices.Add(indices[i] + baseV);
        }
    }
}
