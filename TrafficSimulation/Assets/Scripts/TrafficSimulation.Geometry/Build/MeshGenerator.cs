using TrafficSimulation.Geometry.Data;
using Unity.Jobs;

namespace TrafficSimulation.Geometry.Build;

public abstract class MeshGenerator {
    public virtual void EstimateCounts(in MeshGenerationContext context, out int vertexCount, out int indexCount) {
        vertexCount = 0;
        indexCount = 0;
    }

    public abstract bool Validate();
    public abstract JobHandle ScheduleGenerate(in MeshGenerationContext context, GeometryWriter writer, JobHandle dependency);
}
