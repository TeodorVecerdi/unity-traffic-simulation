using TrafficSimulation.Geometry.Build;
using Unity.Jobs;

namespace TrafficSimulation.Geometry.Graph;

internal struct LayerWorkItem(MeshGraph.Layer layer, List<GeometryChunk> chunks, JobHandle handle) : IDisposable {
    public readonly MeshGraph.Layer Layer = layer;
    public readonly List<GeometryChunk> Chunks = chunks;
    public JobHandle JobHandle = handle;

    public void Dispose() {
        JobHandle.Complete();
        Chunks.ForEach(chunk => chunk.Dispose());
        if (Layer.Generator is IDisposable disposable)
            disposable.Dispose();
    }
}
