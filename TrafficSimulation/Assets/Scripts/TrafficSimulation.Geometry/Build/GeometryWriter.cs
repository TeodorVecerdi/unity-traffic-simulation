using TrafficSimulation.Geometry.Data;
using Unity.Collections;

namespace TrafficSimulation.Geometry.Build;

/// <summary>
/// Low-level helper for building indexed mesh topology into <see cref="NativeList{T}"/> buffers.
/// </summary>
/// <remarks>
/// - Methods without a suffix emit triangles with clockwise (CW) winding.<br/>
/// - Methods with the <c>CCW</c> suffix emit the counter-clockwise winding of the same topology.<br/>
/// - This writer appends to the provided <see cref="Vertices"/> and <see cref="Indices"/> lists; it does not allocate.
/// </remarks>
public struct GeometryWriter(NativeList<MeshVertex> vertices, NativeList<int> indices) {
    /// <summary>The target vertex buffer. New vertices are appended.</summary>
    public NativeList<MeshVertex> Vertices = vertices;
    /// <summary>The target index buffer (triangle list). Indices reference entries in <see cref="Vertices"/>.</summary>
    public NativeList<int> Indices = indices;

    /// <summary>
    /// Append a single vertex to <see cref="Vertices"/>.
    /// </summary>
    /// <param name="v">Vertex data to append.</param>
    public void WriteVertex(in MeshVertex v) {
        Vertices.Add(v);
    }

    /// <summary>
    /// Append a single triangle to <see cref="Indices"/> using the provided vertex indices.
    /// </summary>
    /// <param name="i0">Index of the first vertex of the triangle.</param>
    /// <param name="i1">Index of the second vertex of the triangle.</param>
    /// <param name="i2">Index of the third vertex of the triangle.</param>
    /// <remarks>
    /// The order (<paramref name="i0"/>, <paramref name="i1"/>, <paramref name="i2"/>) determines the winding.
    /// </remarks>
    public void WriteTriangleIndices(int i0, int i1, int i2) {
        Indices.Add(i0);
        Indices.Add(i1);
        Indices.Add(i2);
    }

    /// <summary>
    /// Append three vertices (a,b,c) and a CW triangle referencing them.
    /// </summary>
    /// <param name="a">First vertex.</param>
    /// <param name="b">Second vertex.</param>
    /// <param name="c">Third vertex.</param>
    /// <remarks>
    /// Uses CW winding (0,1,2). Use <see cref="WriteTriangleCCW(in MeshVertex, in MeshVertex, in MeshVertex)"/> for the CCW variant.
    /// </remarks>
    public void WriteTriangle(in MeshVertex a, in MeshVertex b, in MeshVertex c) {
        var baseV = Vertices.Length;
        WriteVertex(a);
        WriteVertex(b);
        WriteVertex(c);
        WriteTriangleIndices(baseV + 0, baseV + 1, baseV + 2);
    }

    /// <summary>
    /// Append three vertices (a,b,c) and a CCW triangle referencing them.
    /// </summary>
    /// <param name="a">First vertex.</param>
    /// <param name="b">Second vertex.</param>
    /// <param name="c">Third vertex.</param>
    /// <remarks>
    /// CCW winding: (0,2,1). The CW counterpart is <see cref="WriteTriangle(in MeshVertex, in MeshVertex, in MeshVertex)"/>.
    /// </remarks>
    public void WriteTriangleCCW(in MeshVertex a, in MeshVertex b, in MeshVertex c) {
        var baseV = Vertices.Length;
        WriteVertex(a);
        WriteVertex(b);
        WriteVertex(c);
        // Reverse winding: (0,2,1)
        WriteTriangleIndices(baseV + 0, baseV + 2, baseV + 1);
    }

    /// <summary>
    /// Append four vertices and two CW triangles forming a quad.
    /// </summary>
    /// <param name="v0">Quad vertex 0.</param>
    /// <param name="v1">Quad vertex 1.</param>
    /// <param name="v2">Quad vertex 2.</param>
    /// <param name="v3">Quad vertex 3.</param>
    /// <remarks>
    /// CW triangles: (0,1,2) and (2,3,0). Use <see cref="WriteQuadCCW(in MeshVertex, in MeshVertex, in MeshVertex, in MeshVertex)"/> for CCW.
    /// </remarks>
    public void WriteQuad(in MeshVertex v0, in MeshVertex v1, in MeshVertex v2, in MeshVertex v3) {
        var baseV = Vertices.Length;
        WriteVertex(v0);
        WriteVertex(v1);
        WriteVertex(v2);
        WriteVertex(v3);

        // CW front face: (0,1,2) (2,3,0)
        WriteTriangleIndices(baseV + 0, baseV + 1, baseV + 2);
        WriteTriangleIndices(baseV + 2, baseV + 3, baseV + 0);
    }

    /// <summary>
    /// Append four vertices and two CCW triangles forming a quad.
    /// </summary>
    /// <param name="v0">Quad vertex 0.</param>
    /// <param name="v1">Quad vertex 1.</param>
    /// <param name="v2">Quad vertex 2.</param>
    /// <param name="v3">Quad vertex 3.</param>
    /// <remarks>
    /// CCW triangles: (0,2,1) and (2,0,3). The CW counterpart is <see cref="WriteQuad(in MeshVertex, in MeshVertex, in MeshVertex, in MeshVertex)"/>.
    /// </remarks>
    public void WriteQuadCCW(in MeshVertex v0, in MeshVertex v1, in MeshVertex v2, in MeshVertex v3) {
        var baseV = Vertices.Length;
        WriteVertex(v0);
        WriteVertex(v1);
        WriteVertex(v2);
        WriteVertex(v3);

        // CCW front face: (0,2,1) (2,0,3)
        WriteTriangleIndices(baseV + 0, baseV + 2, baseV + 1);
        WriteTriangleIndices(baseV + 2, baseV + 0, baseV + 3);
    }

    /// <summary>
    /// Append quad indices for four existing vertices at <paramref name="baseV"/>..(<paramref name="baseV"/>+3) with CW winding.
    /// </summary>
    /// <param name="baseV">Base index of the first of the 4 vertices (v0..v3).</param>
    /// <remarks>
    /// CW triangles: (0,1,2) and (2,3,0) offset by <paramref name="baseV"/>.
    /// Use <see cref="WriteQuadIndicesFromBaseCCW(int)"/> for CCW.
    /// </remarks>
    public void WriteQuadIndicesFromBase(int baseV) {
        WriteTriangleIndices(baseV + 0, baseV + 1, baseV + 2);
        WriteTriangleIndices(baseV + 2, baseV + 3, baseV + 0);
    }

    /// <summary>
    /// Append quad indices for four existing vertices at <paramref name="baseV"/>..(<paramref name="baseV"/>+3) with CCW winding.
    /// </summary>
    /// <param name="baseV">Base index of the first of the 4 vertices (v0..v3).</param>
    /// <remarks>
    /// CCW triangles: (0,2,1) and (2,0,3) offset by <paramref name="baseV"/>.
    /// The CW counterpart is <see cref="WriteQuadIndicesFromBase(int)"/>.
    /// </remarks>
    public void WriteQuadIndicesFromBaseCCW(int baseV) {
        WriteTriangleIndices(baseV + 0, baseV + 2, baseV + 1);
        WriteTriangleIndices(baseV + 2, baseV + 0, baseV + 3);
    }

    /// <summary>
    /// Append two CW triangles to stitch a new segment onto an existing strip.
    /// </summary>
    /// <param name="newLeft">New left vertex to append.</param>
    /// <param name="newRight">New right vertex to append.</param>
    /// <param name="prevLeftIndex">Index of the previous left vertex already in the buffer.</param>
    /// <param name="prevRightIndex">Index of the previous right vertex already in the buffer.</param>
    /// <param name="newLeftIndex">Returns the index of the appended left vertex.</param>
    /// <param name="newRightIndex">Returns the index of the appended right vertex.</param>
    /// <remarks>
    /// Emits CW tris: (prevL, prevR, newR) and (newR, newL, prevL). Use <see cref="WriteStripStepCCW(in MeshVertex, in MeshVertex, int, int, out int, out int)"/> for CCW.
    /// </remarks>
    public void WriteStripStep(in MeshVertex newLeft, in MeshVertex newRight, int prevLeftIndex, int prevRightIndex, out int newLeftIndex, out int newRightIndex) {
        newLeftIndex = Vertices.Length;
        WriteVertex(newLeft);
        newRightIndex = Vertices.Length;
        WriteVertex(newRight);

        // Two tris: (prevL, prevR, newR) and (newR, newL, prevL)
        WriteTriangleIndices(prevLeftIndex, prevRightIndex, newRightIndex);
        WriteTriangleIndices(newRightIndex, newLeftIndex, prevLeftIndex);
    }

    /// <summary>
    /// Append two CCW triangles to stitch a new segment onto an existing strip.
    /// </summary>
    /// <param name="newLeft">New left vertex to append.</param>
    /// <param name="newRight">New right vertex to append.</param>
    /// <param name="prevLeftIndex">Index of the previous left vertex already in the buffer.</param>
    /// <param name="prevRightIndex">Index of the previous right vertex already in the buffer.</param>
    /// <param name="newLeftIndex">Returns the index of the appended left vertex.</param>
    /// <param name="newRightIndex">Returns the index of the appended right vertex.</param>
    /// <remarks>
    /// CCW tris: (prevL, newR, prevR) and (newR, prevL, newL). The CW counterpart is <see cref="WriteStripStep(in MeshVertex, in MeshVertex, int, int, out int, out int)"/>.
    /// </remarks>
    public void WriteStripStepCCW(in MeshVertex newLeft, in MeshVertex newRight, int prevLeftIndex, int prevRightIndex, out int newLeftIndex, out int newRightIndex) {
        newLeftIndex = Vertices.Length;
        WriteVertex(newLeft);
        newRightIndex = Vertices.Length;
        WriteVertex(newRight);

        // CCW tris: (prevL, newR, prevR) and (newR, prevL, newL)
        WriteTriangleIndices(prevLeftIndex, newRightIndex, prevRightIndex);
        WriteTriangleIndices(newRightIndex, prevLeftIndex, newLeftIndex);
    }

    /// <summary>
    /// Stitch two rings with identical segment counts by appending 2 triangles per segment (CW winding).
    /// </summary>
    /// <param name="prevStart">Base index of the previous ring (segment 0).</param>
    /// <param name="curStart">Base index of the current ring (segment 0).</param>
    /// <param name="segmentCount">Number of segments/vertices in each ring.</param>
    /// <param name="closed">If true, stitch the last segment to the first; otherwise, leave the gap open.</param>
    /// <remarks>
    /// For each segment i, stitches (prev[i], prev[i+1], cur[i+1]) and (cur[i+1], cur[i], prev[i]).
    /// Use <see cref="WriteRingStitchCCW(int, int, int, bool)"/> for CCW.
    /// </remarks>
    public void WriteRingStitch(int prevStart, int curStart, int segmentCount, bool closed) {
        var count = segmentCount - (closed ? 0 : 1);
        for (var i = 0; i < count; i++) {
            var i1 = (i + 1) % segmentCount;

            var p0 = prevStart + i;
            var p1 = prevStart + i1;
            var c0 = curStart + i;
            var c1 = curStart + i1;
            WriteTriangleIndices(p0, p1, c1);
            WriteTriangleIndices(c1, c0, p0);
        }
    }

    /// <summary>
    /// Stitch two rings with identical segment counts by appending 2 triangles per segment (CCW winding).
    /// </summary>
    /// <param name="prevStart">Base index of the previous ring (segment 0).</param>
    /// <param name="curStart">Base index of the current ring (segment 0).</param>
    /// <param name="segmentCount">Number of segments/vertices in each ring.</param>
    /// <param name="closed">If true, stitch the last segment to the first; otherwise, leave the gap open.</param>
    /// <remarks>
    /// For each segment i, stitches (prev[i], cur[i+1], prev[i+1]) and (cur[i+1], prev[i], cur[i]).
    /// The CW counterpart is <see cref="WriteRingStitch(int, int, int, bool)"/>.
    /// </remarks>
    public void WriteRingStitchCCW(int prevStart, int curStart, int segmentCount, bool closed) {
        var count = segmentCount - (closed ? 0 : 1);
        for (var i = 0; i < count; i++) {
            var i1 = (i + 1) % segmentCount;

            var p0 = prevStart + i;
            var p1 = prevStart + i1;
            var c0 = curStart + i;
            var c1 = curStart + i1;
            WriteTriangleIndices(p0, c1, p1);
            WriteTriangleIndices(c1, p0, c0);
        }
    }

    /// <summary>
    /// Append <paramref name="count"/> CW triangles forming a fan around an existing center vertex.
    /// </summary>
    /// <param name="centerIndex">Index of the center vertex already appended to <see cref="Vertices"/>.</param>
    /// <param name="start">Start index of the ring to fan around.</param>
    /// <param name="count">Number of vertices in the ring.</param>
    /// <remarks>
    /// Emits triangles (center, ring[i], ring[i+1]) for i = 0..count-1 (wrapping at end). Use <see cref="WriteTriangleFanCCW(int, int, int)"/> for CCW.
    /// </remarks>
    public void WriteTriangleFan(int centerIndex, int start, int count) {
        for (var i = 0; i < count; i++) {
            var i0 = start + i;
            var i1 = start + ((i + 1) % count);
            WriteTriangleIndices(centerIndex, i0, i1);
        }
    }

    /// <summary>
    /// Append <paramref name="count"/> CCW triangles forming a fan around an existing center vertex.
    /// </summary>
    /// <param name="centerIndex">Index of the center vertex already appended to <see cref="Vertices"/>.</param>
    /// <param name="start">Start index of the ring to fan around.</param>
    /// <param name="count">Number of vertices in the ring.</param>
    /// <remarks>
    /// Emits triangles (center, ring[i+1], ring[i]) for i = 0..count-1 (wrapping at end). The CW counterpart is <see cref="WriteTriangleFan(int, int, int)"/>.
    /// </remarks>
    public void WriteTriangleFanCCW(int centerIndex, int start, int count) {
        for (var i = 0; i < count; i++) {
            var i0 = start + i;
            var i1 = start + ((i + 1) % count);
            WriteTriangleIndices(centerIndex, i1, i0);
        }
    }
}
