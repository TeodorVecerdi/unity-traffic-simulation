using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

namespace TrafficSimulation.Geometry.Helpers;

public static class GeometryUtils {
    public const float Epsilon = 1e-6f;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float3 AnyPerpendicular(float3 v) {
        // Choose an axis not parallel to v, then cross.
        var a = math.abs(v.y) < 0.99f ? new float3(0.0f, 1.0f, 0.0f) : new float3(1.0f, 0.0f, 0.0f);
        var p = math.cross(v, a);
        var len = math.length(p);
        return len > Epsilon ? p / len : new float3(0.0f, 0.0f, 0.0f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void BuildOrthonormalBasis(float3 tangent, float3 upHint, out float3 right, out float3 up) {
        var t = math.normalizesafe(tangent, new float3(0.0f, 0.0f, 1.0f));
        var upProj = upHint - t * math.dot(upHint, t);
        up = math.normalizesafe(upProj, AnyPerpendicular(t));
        right = math.normalize(math.cross(up, t));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float DistancePointLine(float3 p, float3 a, float3 b) {
        var ab = b - a;
        var denominator = math.lengthsq(ab);
        if (denominator <= Epsilon)
            return math.length(p - a);
        return math.length(math.cross(ab, p - a)) / math.sqrt(denominator);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float3 ClosestPointOnSegment(float3 p, float3 a, float3 b) {
        var ab = b - a;
        var lengthSquared = math.lengthsq(ab);
        if (lengthSquared <= Epsilon)
            return a;
        var t = math.clamp(math.dot(p - a, ab) / lengthSquared, 0.0f, 1.0f);
        return a + ab * t;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float DistancePointSegment(float3 p, float3 a, float3 b) {
        return math.length(p - ClosestPointOnSegment(p, a, b));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float PolylineLength(in NativeArray<float3> pts) {
        var len = 0f;
        for (var i = 1; i < pts.Length; i++) {
            len += math.distance(pts[i - 1], pts[i]);
        }

        return len;
    }

    public static float SignedArea2D(in NativeArray<float2> poly) {
        var area = 0f;
        var n = poly.Length;
        var j = n - 1;
        for (var i = 0; i < n; j = i, i++) {
            var pi = poly[i];
            var pj = poly[j];
            area += pj.x * pi.y - pi.x * pj.y;
        }

        return 0.5f * area;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsCCW(in NativeArray<float2> poly) {
        return SignedArea2D(in poly) > 0.0f;
    }

    public static bool PointInTriangle(float2 p, float2 a, float2 b, float2 c) {
        // Barycentric (inclusive)
        var v0 = c - a;
        var v1 = b - a;
        var v2 = p - a;

        var dot00 = math.dot(v0, v0);
        var dot01 = math.dot(v0, v1);
        var dot02 = math.dot(v0, v2);
        var dot11 = math.dot(v1, v1);
        var dot12 = math.dot(v1, v2);

        var denominator = dot00 * dot11 - dot01 * dot01;
        if (math.abs(denominator) < Epsilon)
            return false;

        var u = (dot11 * dot02 - dot01 * dot12) / denominator;
        var v = (dot00 * dot12 - dot01 * dot02) / denominator;

        return u >= -Epsilon && v >= -Epsilon && u + v <= 1.0f + Epsilon;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float3 ComputeTriangleNormal(float3 a, float3 b, float3 c) {
        var n = math.cross(b - a, c - a);
        var lengthSquared = math.lengthsq(n);
        return lengthSquared > Epsilon
            ? n / math.sqrt(lengthSquared)
            : new float3(0.0f, 1.0f, 0.0f);
    }
}
