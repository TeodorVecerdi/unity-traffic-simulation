using System.Runtime.CompilerServices;
using TrafficSimulation.Geometry.Data;
using Unity.Collections;

namespace TrafficSimulation.Geometry.Helpers;

// Minimal append-style writer for NativeArray-backed vertex/index buffers.
// Assumes the caller pre-allocated exact counts. No capacity checks.
public static class MeshWrite {
    // Write a single vertex and advance offset.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteVertex(in MeshVertex v, ref NativeArray<MeshVertex> vertices, ref int vertexOffset) {
        vertices[vertexOffset] = v;
        vertexOffset++;
    }

    // Write a triangle's indices (base-relative) and advance offset.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteTriangleIndices(int i0, int i1, int i2, ref NativeArray<int> indices, ref int indexOffset) {
        indices[indexOffset + 0] = i0;
        indices[indexOffset + 1] = i1;
        indices[indexOffset + 2] = i2;
        indexOffset += 3;
    }

    // Write 3 new vertices (a,b,c) and add one triangle referencing them.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteTriangle(in MeshVertex a, in MeshVertex b, in MeshVertex c, ref NativeArray<MeshVertex> vertices, ref NativeArray<int> indices, ref int vertexOffset, ref int indexOffset) {
        var baseV = vertexOffset;
        WriteVertex(a, ref vertices, ref vertexOffset);
        WriteVertex(b, ref vertices, ref vertexOffset);
        WriteVertex(c, ref vertices, ref vertexOffset);
        WriteTriangleIndices(baseV + 0, baseV + 1, baseV + 2, ref indices, ref indexOffset);
    }

    // Write 4 vertices and 6 indices (CW winding like: 0-1-2, 2-3-0).
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteQuad(in MeshVertex v0, in MeshVertex v1, in MeshVertex v2, in MeshVertex v3, ref NativeArray<MeshVertex> vertices, ref NativeArray<int> indices, ref int vertexOffset, ref int indexOffset) {
        var baseV = vertexOffset;
        WriteVertex(v0, ref vertices, ref vertexOffset);
        WriteVertex(v1, ref vertices, ref vertexOffset);
        WriteVertex(v2, ref vertices, ref vertexOffset);
        WriteVertex(v3, ref vertices, ref vertexOffset);

        // CW front face: (0,1,2) (2,3,0)
        WriteTriangleIndices(baseV + 0, baseV + 1, baseV + 2, ref indices, ref indexOffset);
        WriteTriangleIndices(baseV + 2, baseV + 3, baseV + 0, ref indices, ref indexOffset);
    }

    // Only write indices for a quad when the 4 vertices already exist at baseV..baseV+3.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteQuadIndicesFromBase(int baseV, ref NativeArray<int> indices, ref int indexOffset) {
        WriteTriangleIndices(baseV + 0, baseV + 1, baseV + 2, ref indices, ref indexOffset);
        WriteTriangleIndices(baseV + 2, baseV + 3, baseV + 0, ref indices, ref indexOffset);
    }

    // Append a strip segment: given previous two vertex indices (prevL, prevR),
    // write two new vertices (newL, newR) and stitch.
    // Returns the two new indices via out parameters.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteStripStep(in MeshVertex newLeft, in MeshVertex newRight, int prevLeftIndex, int prevRightIndex, ref NativeArray<MeshVertex> vertices, ref NativeArray<int> indices, ref int vertexOffset, ref int indexOffset, out int newLeftIndex, out int newRightIndex) {
        newLeftIndex = vertexOffset;
        WriteVertex(newLeft, ref vertices, ref vertexOffset);
        newRightIndex = vertexOffset;
        WriteVertex(newRight, ref vertices, ref vertexOffset);

        // Two tris: (prevL, prevR, newR) and (newR, newL, prevL)
        WriteTriangleIndices(prevLeftIndex, prevRightIndex, newRightIndex, ref indices, ref indexOffset);
        WriteTriangleIndices(newRightIndex, newLeftIndex, prevLeftIndex, ref indices, ref indexOffset);
    }

    // Stitch two rings with the same segment count. If closed=true, stitches last->first.
    // prevStart: base index of previous ring, curStart: base index of current ring.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void StitchRings(int prevStart, int curStart, int segments, bool closed, ref NativeArray<int> indices, ref int indexOffset) {
        var last = closed ? segments : segments - 1;
        for (var i = 0; i < last; i++) {
            var i0 = i;
            var i1 = (i + 1) % segments;

            var a = prevStart + i0;
            var b = prevStart + i1;
            var c = curStart + i0;
            var d = curStart + i1;

            // Quads (a,c,d) and (d,b,a) -> CW
            WriteTriangleIndices(a, c, d, ref indices, ref indexOffset);
            WriteTriangleIndices(d, b, a, ref indices, ref indexOffset);
        }
    }

    // Triangle fan around a center vertex already in-buffer at centerIndex.
    // Writes indices for a fan around a ring [start..start+count-1].
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FanFromCenter(int centerIndex, int ringStartIndex, int ringCount, bool closed, ref NativeArray<int> indices, ref int indexOffset) {
        var last = closed ? ringCount : ringCount - 1;
        for (var i = 0; i < last; i++) {
            var i0 = ringStartIndex + i;
            var i1 = ringStartIndex + ((i + 1) % ringCount);
            // CW: (center, i0, i1)
            WriteTriangleIndices(centerIndex, i0, i1, ref indices, ref indexOffset);
        }
    }
}
