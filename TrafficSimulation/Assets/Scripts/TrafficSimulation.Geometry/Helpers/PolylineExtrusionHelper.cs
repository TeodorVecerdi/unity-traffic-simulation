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
}
