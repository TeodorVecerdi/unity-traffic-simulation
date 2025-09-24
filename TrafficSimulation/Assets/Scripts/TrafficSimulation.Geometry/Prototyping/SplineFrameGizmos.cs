using Sirenix.OdinInspector;
using TrafficSimulation.Geometry.Data;
using TrafficSimulation.Geometry.Helpers;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

namespace TrafficSimulation.Geometry.Prototyping;

[ExecuteAlways]
public sealed class SplineFrameGizmos : MonoBehaviour {
    [SerializeField, Required] private SplineContainer m_SplineContainer = null!;
    [SerializeField, OnValueChanged(nameof(RepaintScene)), MinValue(0.005f)] private float m_MaxError = 0.05f;
    [SerializeField, OnValueChanged(nameof(RepaintScene)), MinValue(0.01f)] private float m_MaxStep = 2.0f;
    [Space]
    [SerializeField] private bool m_FixedUp = true; // if true, no banking
    [SerializeField] private Vector3 m_InitialUp = Vector3.up; // treated as world-up; converted to local before frame build
    [Space]
    [SerializeField, MinValue(0.01f)] private float m_AxisLength = 0.5f;
    [SerializeField, Range(0.05f, 0.5f)] private float m_HeadSize = 0.2f; // fraction of axis length
    [SerializeField, MinValue(1)] private int m_DrawEvery = 1; // draw every Nth sample to reduce clutter
    [Space]
    [SerializeField] private Color m_TangentColor = Color.cyan; // forward
    [SerializeField] private Color m_NormalColor = Color.green; // up
    [SerializeField] private Color m_BinormalColor = Color.red; // right
    [SerializeField] private bool m_DrawGizmos = true;

    // Cached data
    private NativeList<float4> m_CachedPositions;
    private NativeList<float4> m_CachedTangents;
    private NativeArray<Frame> m_CachedFrames;
    private bool m_HasValidCache;

    // Parameter tracking for cache invalidation
    private float m_LastMaxError;
    private float m_LastMaxStep;
    private bool m_LastFixedUp;
    private Vector3 m_LastInitialUp;
    private Spline? m_LastSpline;
    private Transform? m_LastTransform;

    private void OnDrawGizmos() {
        if (!m_DrawGizmos || m_SplineContainer == null)
            return;

        var spline = m_SplineContainer.Spline;
        if (spline == null)
            return;

        var tr = m_SplineContainer.transform;
        var maxError = math.max(0.005f, m_MaxError);
        var maxStep = math.max(0.01f, m_MaxStep);
        var drawEvery = math.max(1, m_DrawEvery);

        // Check if cache needs to be invalidated
        if (!IsCacheValid(spline, tr, maxError, maxStep)) {
            InvalidateCache();
            UpdateCache(spline, tr, maxError, maxStep);
        }

        // Use cached data for drawing
        if (m_HasValidCache && m_CachedFrames.IsCreated) {
            var frames = m_CachedFrames;
            for (var i = 0; i < frames.Length; i += drawEvery) {
                var f = frames[i];
                var localPos = new Vector3(f.Position.x, f.Position.y, f.Position.z);
                var worldPos = tr.TransformPoint(localPos);

                var fwdWorld = tr.TransformDirection(new Vector3(f.Tangent.x, f.Tangent.y, f.Tangent.z));
                var upWorld = tr.TransformDirection(new Vector3(f.Normal.x, f.Normal.y, f.Normal.z));
                var rightWorld = tr.TransformDirection(new Vector3(f.Binormal.x, f.Binormal.y, f.Binormal.z));

                DrawArrow(worldPos, fwdWorld, m_AxisLength, m_HeadSize, m_TangentColor);
                DrawArrow(worldPos, upWorld, m_AxisLength, m_HeadSize, m_NormalColor);
                DrawArrow(worldPos, rightWorld, m_AxisLength, m_HeadSize, m_BinormalColor);
            }
        }
    }

    private bool IsCacheValid(Spline spline, Transform tr, float maxError, float maxStep) {
        return m_HasValidCache &&
               m_LastMaxError == maxError &&
               m_LastMaxStep == maxStep &&
               m_LastFixedUp == m_FixedUp &&
               m_LastInitialUp == m_InitialUp &&
               m_LastSpline == spline &&
               m_LastTransform == tr;
    }

    private void UpdateCache(Spline spline, Transform tr, float maxError, float maxStep) {
        var positions = new NativeList<float4>(Allocator.Temp);
        var tangents = new NativeList<float4>(Allocator.Temp);
        SplineSamplingUtility.AdaptiveSample(spline, ref positions, ref tangents, maxError, maxStep);

        if (positions.Length == 0) {
            positions.Dispose();
            tangents.Dispose();
            return;
        }

        var frames = new NativeArray<Frame>(positions.Length, Allocator.Temp);
        // Convert world up to spline local before building frames
        var initialUpLocal = (float3)tr.InverseTransformDirection(m_InitialUp);
        FrameBuilder.BuildFramesFromPolyline(positions.AsArray(), tangents.AsArray(), initialUpLocal, m_FixedUp, ref frames);

        // Allocate persistent cache
        m_CachedPositions = new NativeList<float4>(positions.Length, Allocator.Persistent);
        m_CachedTangents = new NativeList<float4>(tangents.Length, Allocator.Persistent);
        m_CachedFrames = new NativeArray<Frame>(frames.Length, Allocator.Persistent);

        // Copy data to cache
        m_CachedPositions.CopyFrom(positions);
        m_CachedTangents.CopyFrom(tangents);
        m_CachedFrames.CopyFrom(frames);

        m_HasValidCache = true;

        // Update tracking parameters
        m_LastMaxError = maxError;
        m_LastMaxStep = maxStep;
        m_LastFixedUp = m_FixedUp;
        m_LastInitialUp = m_InitialUp;
        m_LastSpline = spline;
        m_LastTransform = tr;

        // Dispose temporary data
        positions.Dispose();
        tangents.Dispose();
        frames.Dispose();
    }

    private void InvalidateCache() {
        if (m_CachedPositions.IsCreated)
            m_CachedPositions.Dispose();
        if (m_CachedTangents.IsCreated)
            m_CachedTangents.Dispose();
        if (m_CachedFrames.IsCreated)
            m_CachedFrames.Dispose();

        m_HasValidCache = false;
        m_LastSpline = null;
        m_LastTransform = null;
    }

    private void OnDisable() {
        InvalidateCache();
    }

    private void OnDestroy() {
        InvalidateCache();
    }

    // Called when inspector values change to invalidate cache
    private void OnValidate() {
        InvalidateCache();
    }

    private static void DrawArrow(in Vector3 origin, in Vector3 direction, float length, float headFraction, in Color color) {
        var dir = direction;
        var mag = dir.magnitude;
        if (mag < 1e-6f)
            return;

        var dirN = dir / mag;
        var shaftLen = length;
        var tip = origin + dirN * shaftLen;

        Gizmos.color = color;
        Gizmos.DrawLine(origin, tip);

        // Build a small arrow head using two lines
        var side = Vector3.Cross(dirN, Vector3.up);
        if (side.sqrMagnitude < 1e-6f)
            side = Vector3.Cross(dirN, Vector3.right);
        side.Normalize();

        var headLen = math.max(0.01f, shaftLen * headFraction);
        var headDir1 = (-dirN + 0.5f * side).normalized;
        var headDir2 = (-dirN - 0.5f * side).normalized;

        Gizmos.DrawLine(tip, tip + headDir1 * headLen);
        Gizmos.DrawLine(tip, tip + headDir2 * headLen);
    }

    private void RepaintScene() {
        InvalidateCache();
#if UNITY_EDITOR
        SceneView.RepaintAll();
#endif
    }
}
