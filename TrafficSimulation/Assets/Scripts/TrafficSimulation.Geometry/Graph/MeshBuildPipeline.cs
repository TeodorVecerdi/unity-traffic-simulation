using System.Diagnostics.CodeAnalysis;
using TrafficSimulation.Geometry.Build;
using TrafficSimulation.Geometry.Data;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace TrafficSimulation.Geometry.Graph;

public static class MeshBuildPipeline {
    [SuppressMessage("ReSharper", "RedundantArgumentDefaultValue")]
    internal static readonly VertexAttributeDescriptor[] Layout = [
        new(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0),
        new(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3, 0),
        new(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2, 0),
    ];

    /// <summary>
    /// Executes the mesh build process for the provided mesh graph and context, and returns the resulting mesh and materials.
    /// </summary>
    /// <param name="graph">The <see cref="MeshGraph"/> that defines the structure and configuration of how the mesh will be created, including its layers.</param>
    /// <param name="context">The <see cref="MeshGenerationContext"/> holding transformation matrices and other required details for mesh generation.</param>
    /// <param name="meshName">The name to assign to the generated mesh. Defaults to "Mesh" if not explicitly specified.</param>
    /// <returns>A <see cref="MeshBuildResult"/> which contains the generated <see cref="Mesh"/> and associated <see cref="Material"/> array.</returns>
    public static MeshBuildResult Build(MeshGraph graph, in MeshGenerationContext context, string meshName = "Mesh") {
        return ScheduleBuild(graph, context, meshName).GetResult();
    }

    /// <summary>
    /// Schedules a mesh build operation asynchronously and returns a handle for finalization and retrieval of results.
    /// </summary>
    /// <param name="graph">The <see cref="MeshGraph"/> that defines the mesh build structure, including its layers and configurations.</param>
    /// <param name="context">The read-only <see cref="MeshGenerationContext"/> that provides necessary information and configurations for the mesh generation process.</param>
    /// <param name="meshName">The name to assign to the resulting mesh. Defaults to "Mesh" if not specified.</param>
    /// <returns>A <see cref="MeshBuildHandle"/> containing the job handle, writable mesh data, resulting mesh instance, and the associated materials.</returns>
    public static MeshBuildHandle ScheduleBuild(MeshGraph graph, in MeshGenerationContext context, string meshName = "Mesh") {
        var layers = graph.Layers
            .Where(l => l is { Enabled: true } && l.Generator != null! && l.Material != null!)
            .ToList();
        if (layers.Count == 0) {
            return MeshBuildHandle.Empty(meshName);
        }

        // Prepare per-layer scratch chunks and schedule jobs
        var perLayer = new List<LayerWorkItem>(layers.Count);
        var handles = new NativeArray<JobHandle>(layers.Count, Allocator.Temp);
        for (var i = 0; i < layers.Count; i++) {
            var layer = layers[i];

            // Estimate capacity
            layer.Generator.EstimateCounts(context, out var vertexCount, out var indexCount);
            if (vertexCount is 0) vertexCount = GeometryChunk.DefaultVertexCapacity;
            if (indexCount is 0) indexCount = GeometryChunk.DefaultIndexCapacity;

            // Setup work item
            var chunk = new GeometryChunk(Allocator.TempJob, vertexCount, indexCount);
            var writer = new GeometryWriter(chunk.Vertices, chunk.Indices);
            var handle = layer.Generator.ScheduleGenerate(context, writer, default);
            var workItem = new LayerWorkItem(layer, chunk, handle);

            perLayer.Add(workItem);
            handles[i] = handle;
        }

        // Combine all layer jobs
        var combined = JobHandle.CombineDependencies(handles);
        handles.Dispose();

        return new MeshBuildHandle(
            combined,
            perLayer,
            meshName
        );
    }
}
