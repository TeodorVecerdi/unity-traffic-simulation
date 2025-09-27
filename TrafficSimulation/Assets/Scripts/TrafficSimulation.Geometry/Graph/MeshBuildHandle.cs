using TrafficSimulation.Geometry.Build;
using TrafficSimulation.Geometry.Data;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace TrafficSimulation.Geometry.Graph;

public sealed class MeshBuildHandle {
    public JobHandle JobHandle { get; }
    private readonly List<LayerWorkItem> m_WorkItems;
    private readonly string m_MeshName;
    private MeshBuildResult m_BuildResult;
    private bool m_IsApplied;

    public bool IsCompleted => JobHandle.IsCompleted;

    internal MeshBuildHandle(JobHandle jobHandle, List<LayerWorkItem> workItems, string meshName) {
        JobHandle = jobHandle;
        m_WorkItems = workItems;
        m_MeshName = meshName;
        m_IsApplied = false;
    }

    public void CompleteAndApply() {
        if (m_IsApplied) return;
        m_IsApplied = true;

        if (JobHandle != default) {
            JobHandle.Complete();
        }

        if (m_WorkItems.Count == 0) {
            return;
        }

        // 2) Merge chunks by material (surface)
        var surfaceMap = new Dictionary<SurfaceKey, int>(16);
        var surfaces = new List<SurfaceAggregator>(8);

        try {
            foreach (var workItem in m_WorkItems) {
                var key = new SurfaceKey(workItem.Layer.Material);

                if (!surfaceMap.TryGetValue(key, out var surfaceIndex)) {
                    surfaceIndex = surfaces.Count;
                    surfaceMap.Add(key, surfaceIndex);
                    surfaces.Add(new SurfaceAggregator(workItem.Layer.Material, Allocator.Temp));
                }

                var chunk = workItem.Chunk;
                surfaces[surfaceIndex].AppendChunk(ref chunk.Vertices, ref chunk.Indices);
            }

            // 3) Allocate MeshData once with exact counts
            var totalVertices = 0;
            var totalIndices = 0;
            foreach (var surface in surfaces) {
                totalVertices += surface.Vertices.Length;
                totalIndices += surface.Indices.Length;
            }

            var writable = Mesh.AllocateWritableMeshData(1);
            var meshData = writable[0];
            meshData.SetVertexBufferParams(totalVertices, MeshBuildPipeline.Layout);
            meshData.SetIndexBufferParams(totalIndices, IndexFormat.UInt32);
            meshData.subMeshCount = surfaces.Count;

            // 4) Copy per-surface into final buffers and create submesh descriptors
            var vertices = meshData.GetVertexData<MeshVertex>();
            var indices = meshData.GetIndexData<int>();
            var subMeshes = new SubMeshDescriptor[surfaces.Count];

            var vertexCursor = 0;
            var indexCursor = 0;
            for (var surfaceIndex = 0; surfaceIndex < surfaces.Count; surfaceIndex++) {
                var surface = surfaces[surfaceIndex];

                // Vertices
                var sourceVertices = surface.Vertices.AsArray();
                var vertexSlice = vertices.GetSubArray(vertexCursor, sourceVertices.Length);
                vertexSlice.CopyFrom(sourceVertices);

                // Indices (0-based within this surface)
                var sourceIndices = surface.Indices.AsArray();
                var indexSlice = indices.GetSubArray(indexCursor, sourceIndices.Length);
                indexSlice.CopyFrom(sourceIndices);

                subMeshes[surfaceIndex] = new SubMeshDescriptor(indexCursor, sourceIndices.Length) {
                    topology = MeshTopology.Triangles,
                    baseVertex = vertexCursor,
                    firstVertex = vertexCursor,
                    vertexCount = sourceVertices.Length,
                };

                vertexCursor += sourceVertices.Length;
                indexCursor += sourceIndices.Length;
            }

            // 5) Create Mesh + set submeshes + materials
            var mesh = new Mesh {
                name = m_MeshName,
                indexFormat = IndexFormat.UInt32,
            };

            for (var surfaceIndex = 0; surfaceIndex < surfaces.Count; surfaceIndex++) {
                meshData.SetSubMesh(surfaceIndex, subMeshes[surfaceIndex], MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers);
            }

            Mesh.ApplyAndDisposeWritableMeshData(writable, mesh, MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers);
            mesh.RecalculateBounds();

            var materials = surfaces.Select(s => s.Material).ToArray();
            m_BuildResult = new MeshBuildResult(mesh, materials);
        } catch (Exception e) {
            Debug.LogException(e);
            m_BuildResult = new MeshBuildResult(new Mesh { name = m_MeshName }, []);
        } finally {
            // Dispose all temporary allocations
            for (var i = 0; i < m_WorkItems.Count; i++) {
                m_WorkItems[i].Dispose();
            }

            foreach (var surface in surfaces) {
                surface.Dispose();
            }

            m_WorkItems.Clear();
        }
    }

    public MeshBuildResult GetResult() {
        CompleteAndApply();
        return m_BuildResult;
    }

    public static MeshBuildHandle Empty(string name) {
        return new MeshBuildHandle(default, [], name);
    }
}
