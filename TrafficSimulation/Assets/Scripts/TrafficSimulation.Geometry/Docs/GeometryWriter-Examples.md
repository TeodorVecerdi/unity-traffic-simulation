# GeometryWriter: Usage Examples (Non-trivial APIs)

This guide shows end-to-end examples using `GeometryWriter` to build meshes using NativeList buffers. It focuses on the non-trivial writer methods:

- WriteQuadIndicesFromBase / WriteQuadIndicesFromBaseCCW
- WriteStripStep / WriteStripStepCCW
- WriteRingStitch / WriteRingStitchCCW
- WriteTriangleFan / WriteTriangleFanCCW

Each example builds a complete mesh with positions, normals, and UVs, then converts the buffers into a Unity `Mesh`.

Prerequisites:
- `using Unity.Collections;`
- `using Unity.Mathematics;`
- `using UnityEngine;`
- `using TrafficSimulation.Geometry.Build;`
- `using TrafficSimulation.Geometry.Data;`

Helper: convert NativeList buffers to a Unity Mesh.

```csharp
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using TrafficSimulation.Geometry.Data;

static class MeshHelpers
{
    public static Mesh BuildMesh(NativeList<MeshVertex> vertices, NativeList<int> indices, bool computeNormalsIfMissing = false)
    {
        var mesh = new Mesh();
        mesh.indexFormat = (vertices.Length > 65535) ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;

        var vCount = vertices.Length;
        var positions = new Vector3[vCount];
        var normals = new Vector3[vCount];
        var uvs = new Vector2[vCount];

        for (int i = 0; i < vCount; i++)
        {
            var v = vertices[i];
            positions[i] = new Vector3(v.Position.x, v.Position.y, v.Position.z);
            normals[i] = new Vector3(v.Normal.x, v.Normal.y, v.Normal.z);
            uvs[i] = new Vector2(v.UV.x, v.UV.y);
        }

        mesh.SetVertices(positions);
        if (computeNormalsIfMissing)
        {
            // If normals are zeroed or unreliable, auto-calculate.
            mesh.RecalculateNormals();
        }
        else
        {
            mesh.SetNormals(normals);
        }
        mesh.SetUVs(0, uvs);

        var tris = indices.AsArray().ToArray();
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        return mesh;
    }
}
```

Notes on winding:
- Methods without a suffix use CW winding.
- Methods with the `CCW` suffix flip the winding. If your rendering looks inverted, try the CCW variant (or toggle the material's culling).

---

## 1) WriteQuadIndicesFromBase: Build a tiled quad (indices-only)

This example shows how to pre-append four vertices (with position/normal/uv), then add the quad’s indices using `WriteQuadIndicesFromBase`. We also demonstrate the CCW variant.

```csharp
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using TrafficSimulation.Geometry.Build;
using TrafficSimulation.Geometry.Data;

public class QuadFromBaseExample : MonoBehaviour
{
    public MeshFilter Target;

    void Start()
    {
        using var vertices = new NativeList<MeshVertex>(Allocator.Temp);
        using var indices = new NativeList<int>(Allocator.Temp);

        var writer = new GeometryWriter(vertices, indices);

        // Define a unit quad in the XZ plane (Y up), CW winding.
        // v0----v1
        // | \   |
        // |  \  |
        // |   \ |
        // v3----v2
        int baseV = vertices.Length;
        vertices.Add(new MeshVertex { Position = new float3(0, 0, 0), Normal = new float3(0, 1, 0), UV = new float2(0, 0) }); // v0
        vertices.Add(new MeshVertex { Position = new float3(1, 0, 0), Normal = new float3(0, 1, 0), UV = new float2(1, 0) }); // v1
        vertices.Add(new MeshVertex { Position = new float3(1, 0, 1), Normal = new float3(0, 1, 0), UV = new float2(1, 1) }); // v2
        vertices.Add(new MeshVertex { Position = new float3(0, 0, 1), Normal = new float3(0, 1, 0), UV = new float2(0, 1) }); // v3

        // Indices (CW): (0,1,2), (2,3,0) relative to baseV.
        writer.WriteQuadIndicesFromBase(baseV);
        // For CCW, use: writer.WriteQuadIndicesFromBaseCCW(baseV);

        var mesh = MeshHelpers.BuildMesh(vertices, indices);
        Target.sharedMesh = mesh;
    }
}
```

---

## 2) WriteStripStep: Build a ribbon (triangle strip stitched segment-by-segment)

This constructs a ribbon along a polyline by appending two vertices per step and stitching them to the previous pair using `WriteStripStep`. You control handedness by choosing CW or CCW.

```csharp
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using TrafficSimulation.Geometry.Build;
using TrafficSimulation.Geometry.Data;

public class StripRibbonExample : MonoBehaviour
{
    public MeshFilter Target;

    [SerializeField] Vector3[] ControlPoints =
    {
        new Vector3(0, 0, 0),
        new Vector3(2, 0, 1),
        new Vector3(4, 0, 0),
        new Vector3(6, 0, -1),
        new Vector3(8, 0, 0),
    };

    [SerializeField] float HalfWidth = 0.2f;

    void Start()
    {
        using var vertices = new NativeList<MeshVertex>(Allocator.Temp);
        using var indices = new NativeList<int>(Allocator.Temp);
        var writer = new GeometryWriter(vertices, indices);

        if (ControlPoints.Length < 2)
        {
            Debug.LogWarning("Need at least two control points for a strip.");
            return;
        }

        // Build a left/right pair for the first segment
        var p0 = (float3)ControlPoints[0];
        var p1 = (float3)ControlPoints[1];
        var tangent = math.normalize(p1 - p0);
        var up = new float3(0, 1, 0);
        var right = math.normalize(math.cross(up, tangent));

        var left0 = p0 - right * HalfWidth;
        var right0 = p0 + right * HalfWidth;

        int prevL = vertices.Length; writer.WriteVertex(new MeshVertex { Position = left0, Normal = up, UV = new float2(0, 0) });
        int prevR = vertices.Length; writer.WriteVertex(new MeshVertex { Position = right0, Normal = up, UV = new float2(1, 0) });

        float v = 0f;
        for (int i = 1; i < ControlPoints.Length; i++)
        {
            var a = (float3)ControlPoints[i - 1];
            var b = (float3)ControlPoints[i];
            tangent = math.normalize(b - a);
            right = math.normalize(math.cross(up, tangent));

            var leftPos = b - right * HalfWidth;
            var rightPos = b + right * HalfWidth;

            // CW stitching from previous pair to new pair
            writer.WriteStripStep(
                new MeshVertex { Position = leftPos, Normal = up, UV = new float2(0, v) },
                new MeshVertex { Position = rightPos, Normal = up, UV = new float2(1, v) },
                prevL, prevR,
                out var newL, out var newR);

            prevL = newL;
            prevR = newR;
            v += math.length(b - a); // simple running length used as a V texcoord
        }

        // If your winding is inverted (backface culled), try the CCW variant instead:
        // writer.WriteStripStepCCW(...)

        var mesh = MeshHelpers.BuildMesh(vertices, indices);
        Target.sharedMesh = mesh;
    }
}
```

---

## 3) WriteRingStitch: Connect two rings (e.g., a tapered cylinder section)

We generate two concentric rings with the same segment count and stitch them together into a side surface. Toggle `closed` to join the last segment to the first.

```csharp
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using TrafficSimulation.Geometry.Build;
using TrafficSimulation.Geometry.Data;

public class RingStitchExample : MonoBehaviour
{
    public MeshFilter Target;

    [SerializeField] int Segments = 24;
    [SerializeField] float Radius0 = 0.5f;
    [SerializeField] float Radius1 = 0.2f;
    [SerializeField] float Height = 1.0f;
    [SerializeField] bool Closed = true;

    void Start()
    {
        using var vertices = new NativeList<MeshVertex>(Allocator.Temp);
        using var indices = new NativeList<int>(Allocator.Temp);
        var writer = new GeometryWriter(vertices, indices);

        var up = new float3(0, 1, 0);

        int basePrev = vertices.Length;
        for (int i = 0; i < Segments; i++)
        {
            float t = (i / (float)Segments) * math.PI * 2f;
            var dir = new float3(math.cos(t), 0, math.sin(t));
            var pos = dir * Radius0; // y=0
            var normal = math.normalize(dir); // simple outward normal
            vertices.Add(new MeshVertex { Position = pos, Normal = new float3(normal.x, 0, normal.z), UV = new float2(i / (float)Segments, 0) });
        }

        int baseCur = vertices.Length;
        for (int i = 0; i < Segments; i++)
        {
            float t = (i / (float)Segments) * math.PI * 2f;
            var dir = new float3(math.cos(t), 0, math.sin(t));
            var pos = dir * Radius1 + up * Height; // elevated second ring
            var normal = math.normalize(dir);
            vertices.Add(new MeshVertex { Position = pos, Normal = new float3(normal.x, 0, normal.z), UV = new float2(i / (float)Segments, 1) });
        }

        // Stitch the side surface between the two rings.
        writer.WriteRingStitch(basePrev, baseCur, Segments, Closed);
        // If backfaces appear, use the CCW variant:
        // writer.WriteRingStitchCCW(basePrev, baseCur, Segments, Closed);

        var mesh = MeshHelpers.BuildMesh(vertices, indices);
        Target.sharedMesh = mesh;
    }
}
```

---

## 4) WriteTriangleFan: Create a disk cap from a center and ring

This creates a disk (or cap) by fanning triangles from a center vertex to a ring. The order of the ring determines winding; the CCW variant reverses it.

```csharp
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using TrafficSimulation.Geometry.Build;
using TrafficSimulation.Geometry.Data;

public class TriangleFanDiskExample : MonoBehaviour
{
    public MeshFilter Target;

    [SerializeField] int Segments = 24;
    [SerializeField] float Radius = 0.5f;

    void Start()
    {
        using var vertices = new NativeList<MeshVertex>(Allocator.Temp);
        using var indices = new NativeList<int>(Allocator.Temp);
        var writer = new GeometryWriter(vertices, indices);

        // Center vertex
        int center = vertices.Length;
        vertices.Add(new MeshVertex { Position = new float3(0, 0, 0), Normal = new float3(0, 1, 0), UV = new float2(0.5f, 0.5f) });

        // Ring vertices (CW order viewed from +Y)
        int ringStart = vertices.Length;
        for (int i = 0; i < Segments; i++)
        {
            float t = (i / (float)Segments) * math.PI * 2f;
            var x = math.cos(t) * Radius;
            var z = math.sin(t) * Radius;
            var uv = new float2(0.5f + 0.5f * (x / Radius), 0.5f + 0.5f * (z / Radius));
            vertices.Add(new MeshVertex { Position = new float3(x, 0, z), Normal = new float3(0, 1, 0), UV = uv });
        }

        // Fan from center across the ring (CW). Close automatically by wrapping.
        writer.WriteTriangleFan(center, ringStart, Segments);
        // Or for flipped winding:
        // writer.WriteTriangleFanCCW(center, ringStart, Segments);

        var mesh = MeshHelpers.BuildMesh(vertices, indices);
        Target.sharedMesh = mesh;
    }
}
```

Tips:
- Unity’s default backface culling hides triangles whose winding is considered back-facing under the current view and material settings. If you don’t see geometry, try the `CCW` variants or set the material to Double Sided / disable culling.
- Ensure normals match the intended facing direction to get correct lighting.

