using TrafficSimulation.Geometry.Build;
using Unity.Jobs;

namespace TrafficSimulation.Geometry.Graph;

internal struct LayerWorkItem(MeshGraph.Layer layer, GeometryChunk chunk, JobHandle handle) : IDisposable {
    public readonly MeshGraph.Layer Layer = layer;
    public GeometryChunk Chunk = chunk;
    public JobHandle JobHandle = handle;

    public void Dispose() {
        JobHandle.Complete();
        Chunk.Dispose();
        if (Layer.Generator is IDisposable disposable)
            disposable.Dispose();
    }
}
