using Unity.Collections;
using Unity.Mathematics;

namespace TrafficSimulation.Geometry.Helpers;

public static class Triangulation {
    // Ear-clipping for simple, non-self-intersecting polygons (no holes).
    // polygon: vertices in order (CW or CCW). Triangles are output in that winding.
    public static void TriangulateSimplePolygon(in NativeArray<float2> polygon, NativeList<int> triangles, Allocator tempAllocator = Allocator.Temp) {
        triangles.Clear();
        var n = polygon.Length;
        if (n < 3)
            return;

        var verts = new NativeList<int>(n, tempAllocator);
        var ccw = GeometryUtils.IsCCW(in polygon);
        if (ccw) {
            for (var i = 0; i < n; i++) {
                verts.Add(i);
            }
        } else {
            for (var i = n - 1; i >= 0; i--) {
                verts.Add(i);
            }
        }

        var guard = 0;
        while (verts.Length > 3 && guard < 10000) {
            guard++;
            var earFound = false;

            for (var i = 0; i < verts.Length; i++) {
                var i0 = verts[(i - 1 + verts.Length) % verts.Length];
                var i1 = verts[i];
                var i2 = verts[(i + 1) % verts.Length];

                var a = polygon[i0];
                var b = polygon[i1];
                var c = polygon[i2];

                // Convex check in CCW polygon space
                var ab = b - a;
                var bc = c - b;
                var z = ab.x * bc.y - ab.y * bc.x;
                if (z <= 0.0f)
                    continue;

                var contains = false;
                for (var k = 0; k < verts.Length; k++) {
                    var idx = verts[k];
                    if (idx == i0 || idx == i1 || idx == i2)
                        continue;
                    if (GeometryUtils.PointInTriangle(polygon[idx], a, b, c)) {
                        contains = true;
                        break;
                    }
                }

                if (contains)
                    continue;

                // NOTE: @coderabbitai suggests that this is always CCW; if there's issues with the results, check this. CW order would be i2, i1, i0 (if needed).
                triangles.Add(i0);
                triangles.Add(i1);
                triangles.Add(i2);
                verts.RemoveAt(i);
                earFound = true;
                break;
            }

            if (!earFound) {
                // likely non-simple polygon
                break;
            }
        }

        if (verts.Length == 3) {
            // NOTE: @coderabbitai suggests that this is always CCW; if there's issues with the results, check this. CW order would be 2, 1, 0 (if needed).
            triangles.Add(verts[0]);
            triangles.Add(verts[1]);
            triangles.Add(verts[2]);
        }

        verts.Dispose();
    }
}
