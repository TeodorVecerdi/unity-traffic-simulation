using TrafficSimulation.Geometry.Data;
using Unity.Jobs;

namespace TrafficSimulation.Geometry.Build;

public abstract class MeshGenerator {
    public virtual void EstimateCounts(in MeshGenerationContext context, out int vertexCount, out int indexCount) {
        vertexCount = 0;
        indexCount = 0;
    }

    public virtual int GetSubMeshCount() => 1;
    public abstract bool Validate();
    public abstract JobHandle ScheduleGenerate(in MeshGenerationContext context, List<GeometryWriter> writers, JobHandle dependency);
}
