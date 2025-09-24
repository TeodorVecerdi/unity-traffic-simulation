using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace TrafficSimulation.Geometry.Graph;

public struct MeshBuildHandle(JobHandle jobHandle, Mesh.MeshDataArray meshData, Mesh mesh, Material[] materials, SubMeshDescriptor[] subMeshes, List<IDisposable> disposables) {
    public JobHandle JobHandle { get; } = jobHandle;
    public readonly Mesh.MeshDataArray MeshData = meshData;
    public readonly Mesh Mesh = mesh;
    public readonly Material[] Materials = materials;
    public readonly SubMeshDescriptor[] SubMeshes = subMeshes;
    public readonly List<IDisposable> Disposables = disposables;

    public bool IsApplied { get; private set; }
    public bool IsCompleted => JobHandle.IsCompleted;

    public void CompleteAndApply() {
        if (JobHandle == default || IsApplied) return;
        IsApplied = true;

        JobHandle.Complete();
        if (Mesh == null) {
            Debug.LogError("MeshBuildHandle: Mesh is null, cannot apply mesh data.");
            return;
        }

        var meshData = MeshData[0];
        for (var i = 0; i < SubMeshes.Length; i++) {
            meshData.SetSubMesh(
                i,
                SubMeshes[i],
                MeshUpdateFlags.DontRecalculateBounds |
                MeshUpdateFlags.DontValidateIndices |
                MeshUpdateFlags.DontNotifyMeshUsers
            );
        }

        Mesh.ApplyAndDisposeWritableMeshData(MeshData, Mesh, MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers);
        Mesh.RecalculateBounds();

        foreach (var disposable in Disposables) {
            disposable.Dispose();
        }
    }

    public MeshBuildResult GetResult() {
        CompleteAndApply();
        return new MeshBuildResult(Mesh, Materials);
    }

    public static MeshBuildHandle Empty(string name) {
        var mesh = new Mesh { name = name };
        return new MeshBuildHandle(default, default, mesh, [], [], []);
    }
}
