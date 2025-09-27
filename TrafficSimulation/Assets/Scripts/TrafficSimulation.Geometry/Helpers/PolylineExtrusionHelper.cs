namespace TrafficSimulation.Geometry.Helpers;

public static class PolylineExtrusionHelper {
    public static (int VertexCount, int IndexCount, int QuadCount) CalculateExtrusionCounts(int polylinePointCount, int crossSectionVertexCount, bool isClosed) {
        if (polylinePointCount < 2 || crossSectionVertexCount < 2)
            return (0, 0, 0);

        var segmentCount = isClosed ? polylinePointCount : polylinePointCount - 1;
        var quadCount = segmentCount * (crossSectionVertexCount - 1);
        var vertexCount = polylinePointCount * crossSectionVertexCount;
        var indexCount = quadCount * 6; // 2 triangles per quad, 3 indices per triangle
        return (vertexCount, indexCount, quadCount);
    }

    public static (int VertexCount, int IndexCount, int QuadCount) CalculateExtrusionCounts(int polylinePointCount, int crossSectionVertexCount, bool isClosed, IReadOnlyList<bool> emitEdges) {
        if (polylinePointCount < 2 || crossSectionVertexCount < 2)
            return (0, 0, 0);

        if (emitEdges == null)
            throw new ArgumentNullException(nameof(emitEdges));

        var expectedEdgeCount = isClosed ? polylinePointCount : polylinePointCount - 1;
        if (emitEdges.Count != expectedEdgeCount)
            throw new ArgumentException($"emitEdges.Count ({emitEdges.Count}) must equal {(isClosed ? "polylinePointCount (closed)" : "polylinePointCount - 1 (open)")}: {expectedEdgeCount}", nameof(emitEdges));

        var emittedEdgeCount = emitEdges.Count(t => t);
        var vertexCount = polylinePointCount * crossSectionVertexCount;
        var quadCount = emittedEdgeCount * (crossSectionVertexCount - 1);
        var indexCount = quadCount * 6; // 2 triangles per quad, 3 indices per triangle
        return (vertexCount, indexCount, quadCount);
    }
}
