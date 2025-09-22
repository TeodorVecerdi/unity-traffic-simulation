using System.Diagnostics.CodeAnalysis;
using TrafficSimulation.Roads.MeshGeneration.Data;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace TrafficSimulation.Roads.MeshGeneration.Graph;

public static class MeshBuildPipeline {
    [SuppressMessage("ReSharper", "RedundantArgumentDefaultValue")]
    private static readonly VertexAttributeDescriptor[] s_Layout = [
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
        return ScheduleBuild(graph, context, meshName)
            .GetResult();
    }

    /// <summary>
    /// Schedules a mesh build operation asynchronously and returns a handle for finalization and retrieval of results.
    /// </summary>
    /// <param name="graph">The <see cref="MeshGraph"/> that defines the mesh build structure, including its layers and configurations.</param>
    /// <param name="context">The read-only <see cref="MeshGenerationContext"/> that provides necessary information and configurations for the mesh generation process.</param>
    /// <param name="meshName">The name to assign to the resulting mesh. Defaults to "Mesh" if not specified.</param>
    /// <returns>A <see cref="MeshBuildHandle"/> containing the job handle, writable mesh data, resulting mesh instance, and the associated materials.</returns>
    public static MeshBuildHandle ScheduleBuild(MeshGraph graph, in MeshGenerationContext context, string meshName = "Mesh") {
        var layers = graph.Layers.Where(l => l is { Enabled: true } && l.Generator != null!).ToList();
        var layerCount = layers.Count;
        if (layerCount == 0) {
            return MeshBuildHandle.Empty(meshName);
        }

        // 1) Query counts per layer.
        var vertexCounts = new int[layerCount];
        var indexCounts = new int[layerCount];
        var totalVertices = 0;
        var totalIndices = 0;
        for (var i = 0; i < layerCount; i++) {
            layers[i].Generator.GetCounts(context, out var vertexCount, out var indexCount);
            vertexCounts[i] = math.max(0, vertexCount);
            indexCounts[i] = math.max(0, indexCount);
            totalVertices += vertexCounts[i];
            totalIndices += indexCounts[i];
        }

        // 2) Allocate a single MeshData with one interleaved vertex buffer
        // and one index buffer (UInt32 for simplicity).
        var writable = Mesh.AllocateWritableMeshData(1);
        var meshData = writable[0];
        meshData.SetVertexBufferParams(totalVertices, s_Layout);
        meshData.SetIndexBufferParams(totalIndices, IndexFormat.UInt32);
        meshData.subMeshCount = layerCount;

        // 3) Get global buffers and create per-layer slices.
        var vertexOffset = 0;
        var indexOffset = 0;

        // Keep submesh descriptors here; they’ll be set before apply.
        var subMeshDescriptors = new SubMeshDescriptor[layerCount];

        // Schedule each layer's fill job.
        var handles = new NativeArray<JobHandle>(layerCount, Allocator.Temp);
        for (var i = 0; i < layerCount; i++) {
            var vertexCount = vertexCounts[i];
            var indexCount = indexCounts[i];

            // Set submesh descriptor:
            // Indices are zero-based relative to this submesh slice.
            // We set baseVertex to the global vertex offset.
            subMeshDescriptors[i] = new SubMeshDescriptor(indexOffset, indexCount) {
                topology = MeshTopology.Triangles,
                baseVertex = vertexOffset,
                firstVertex = vertexOffset,
                vertexCount = vertexCount,
            };

            var slice = new MeshBufferSlice(
                meshData,
                vertexOffset,
                vertexCount,
                indexOffset,
                indexCount
            );

            var jobHandle = layers[i].Generator.ScheduleFill(context, slice, default);
            handles[i] = jobHandle;

            vertexOffset += vertexCount;
            indexOffset += indexCount;
        }

        // 4) Combine and return a handle to finalize later.
        var combined = JobHandle.CombineDependencies(handles);
        handles.Dispose();

        // Prepare result Mesh and materials list.
        var materials = new Material[layerCount];
        for (var i = 0; i < layerCount; i++) {
            materials[i] = layers[i].Material;
        }

        var resultMesh = new Mesh {
            name = meshName,
            indexFormat = IndexFormat.UInt32,
        };

        return new MeshBuildHandle(
            combined,
            writable,
            resultMesh,
            materials,
            subMeshDescriptors
        );
    }
}
