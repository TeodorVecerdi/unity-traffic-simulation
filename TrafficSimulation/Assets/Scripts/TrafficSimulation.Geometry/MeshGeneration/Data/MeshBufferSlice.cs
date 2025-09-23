using System.Runtime.CompilerServices;
using Unity.Collections;
using UnityEngine;

namespace TrafficSimulation.Geometry.MeshGeneration.Data;

public struct MeshBufferSlice(Mesh.MeshData meshData, int vertexStart, int vertexCount, int indexStart, int indexCount) {
    private Mesh.MeshData m_MeshData = meshData;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeArray<MeshVertex> GetVertices() {
        return m_MeshData.GetVertexData<MeshVertex>().GetSubArray(vertexStart, vertexCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeArray<uint> GetIndices() {
        return m_MeshData.GetIndexData<uint>().GetSubArray(indexStart, indexCount);
    }
}
