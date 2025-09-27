using TrafficSimulation.Geometry.Data;
using Unity.Collections;

namespace TrafficSimulation.Geometry.Build;

public struct GeometryChunk(Allocator allocator, int vertexCapacity = GeometryChunk.DefaultVertexCapacity, int indexCapacity = GeometryChunk.DefaultIndexCapacity) : IDisposable {
    public const int DefaultVertexCapacity = 4096;
    public const int DefaultIndexCapacity = DefaultVertexCapacity * 6 / 4; // estimate based on 6:4 ratio of indices to vertices for quads

    public NativeList<MeshVertex> Vertices = new(vertexCapacity, allocator);
    public NativeList<int> Indices = new(indexCapacity, allocator);

    public void Dispose() {
        if (Vertices.IsCreated) Vertices.Dispose();
        if (Indices.IsCreated) Indices.Dispose();
    }
}
