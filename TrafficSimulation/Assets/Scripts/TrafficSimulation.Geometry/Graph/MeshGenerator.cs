using TrafficSimulation.Geometry.Data;
using Unity.Jobs;

namespace TrafficSimulation.Geometry.Graph;

public abstract class MeshGenerator {
    public abstract bool Validate();
    public abstract void GetCounts(in MeshGenerationContext context, out int vertexCount, out int indexCount);
    public abstract JobHandle ScheduleFill(in MeshGenerationContext context, in MeshBufferSlice bufferSlice, JobHandle dependency);
}
