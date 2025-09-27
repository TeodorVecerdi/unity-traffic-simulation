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

    public void AppendChunk(ref NativeList<MeshVertex> v, ref NativeList<int> i) {
        var baseV = Vertices.Length;
        Vertices.AddRange(v.AsArray());
        for (var idx = 0; idx < i.Length; idx++) {
            Indices.Add(i[idx] + baseV);
        }
    }
}
