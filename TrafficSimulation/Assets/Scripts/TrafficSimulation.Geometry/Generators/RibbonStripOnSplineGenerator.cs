using System.Runtime.InteropServices;
using Sirenix.OdinInspector;
using TrafficSimulation.Geometry.Build;
using TrafficSimulation.Geometry.Data;
using TrafficSimulation.Geometry.Helpers;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using ReadOnlyAttribute = Unity.Collections.ReadOnlyAttribute;

namespace TrafficSimulation.Geometry.Generators;

[Serializable]
public sealed class RibbonStripOnSplineGenerator : MeshGenerator {
    [Title("Input")]
    [SerializeField, Required] private SplineContainer m_SplineContainer = null!;

    [Title("Frames")]
    [SerializeField] private float m_MaxError = 0.2f;
    [SerializeField, Tooltip("Use frame.Normal as vertex normal. If false, uses normalize(cross(Tangent, Binormal)).")]
    private bool m_UseFrameNormals = true;

    [Title("Local Offset")]
    [SerializeField, Tooltip("Offset in frame space (Right, Up) meters. Useful to place multiple markings from the same spline.")]
    private float3 m_LocalOffset;

    [Title("Ribbon")]
    [SerializeField, MinValue(0.001f)] private float m_Width = 0.2f;
    [SerializeField] private bool m_WindingClockwise;

    [Title("UVs")]
    [SerializeField, Tooltip("V = distance * UVScale + VStart")]
    private float m_UVScale = 1.0f;
    [SerializeField] private bool m_ResetVOnEachRun;
    [SerializeField] private float m_VStart;

    [Title("Dash Pattern (Gaps)")]
    [SerializeField, Tooltip("Meters ON per cycle. If <= 0 or OffLength <= 0 → always ON.")]
    private float m_OnLength = 1.0f;
    [SerializeField, Tooltip("Meters OFF per cycle. If <= 0 or OnLength <= 0 → always ON.")]
    private float m_OffLength = 1.0f;
    [SerializeField, Tooltip("Meters phase offset along the path (can be negative).")]
    private float m_Phase;

    public override bool Validate() {
        if (m_SplineContainer == null) return false;
        if (m_SplineContainer.Spline == null || m_SplineContainer.Spline.Count < 2) return false;
        if (m_Width <= 0.0f) return false;
        if (m_MaxError <= 0.0f) return false;
        return true;
    }

    public override void EstimateCounts(in MeshGenerationContext context, out int vertexCount, out int indexCount) {
        // Approximate number of frames (segments + 1)
        var lengthMeters = math.max(1.0f, m_SplineContainer.Spline.GetLength());
        var framesApprox = math.max(2, (int)math.ceil(lengthMeters / 10.0f) + 1);

        // Duty factor for gaps
        var duty = 1.0f;
        if (m_OnLength > 0.0f && m_OffLength > 0.0f)
            duty = m_OnLength / (m_OnLength + m_OffLength);

        // Approx counts (NativeList can grow anyway; this is just a hint)
        vertexCount = (int)math.ceil(2.0f * framesApprox * duty + 4.0f);
        indexCount = (int)math.ceil(6.0f * (framesApprox - 1) * duty);
    }

    public override JobHandle ScheduleGenerate(in MeshGenerationContext context, GeometryWriter writer, JobHandle dependency) {
        var frameList = new NativeList<Frame>(Allocator.Temp);
        SplineSampler.Sample(m_SplineContainer.Spline, m_MaxError, ref frameList);

        var frames = new NativeArray<Frame>(frameList.Length, Allocator.TempJob);
        frames.CopyFrom(frameList.AsArray());
        frameList.Dispose();

        var job = new RibbonStripWithGapsJob {
            Frames = frames,
            Width = m_Width,
            WindingClockwise = m_WindingClockwise,
            UseFrameNormals = m_UseFrameNormals,
            UVScale = m_UVScale,
            ResetVOnEachRun = m_ResetVOnEachRun,
            VStart = m_VStart,
            OnLength = m_OnLength,
            OffLength = m_OffLength,
            Phase = m_Phase,
            LocalOffset = m_LocalOffset,
            LocalToWorld = m_SplineContainer.transform.localToWorldMatrix,
            Writer = writer,
        };

        return job.Schedule(dependency);
    }

    [BurstCompile]
    private struct RibbonStripWithGapsJob : IJob {
        [DeallocateOnJobCompletion, ReadOnly] public NativeArray<Frame> Frames;

        // Geometry
        public float Width; // meters
        [MarshalAs(UnmanagedType.U1)]
        public bool WindingClockwise;
        [MarshalAs(UnmanagedType.U1)]
        public bool UseFrameNormals; // if false -> normal = normalize(cross(Tangent, Binormal))
        public float3 LocalOffset;

        // UVs
        public float UVScale; // V = distance * UVScale
        [MarshalAs(UnmanagedType.U1)]
        public bool ResetVOnEachRun; // reset V to 0 for each ON run
        public float VStart; // added after reset logic

        // Dash pattern
        public float OnLength; // meters ON per cycle
        public float OffLength; // meters OFF per cycle
        public float Phase; // meters phase offset

        // Transform
        public float4x4 LocalToWorld;

        // Output
        public GeometryWriter Writer;

        public void Execute() {
            var frameCount = Frames.Length;
            if (frameCount < 2) return;

            var halfWidth = 0.5f * Width;

            var gaps = (OnLength > 0f) && (OffLength > 0f);
            var cycle = OnLength + OffLength;

            var sPrev = 0f; // accumulated distance up to frame i-1
            var inRun = false; // currently inside an ON run
            var vOrigin = 0f; // V reset origin for current run

            var previousLeftIndex = -1; // previous strip pair indices
            var previousRightIndex = -1; // previous strip pair indices

            // Normal matrix for transforming local->world normals
            var nrmM = math.transpose(math.inverse((float3x3)LocalToWorld));

            for (var i = 1; i < frameCount; i++) {
                var f0 = Frames[i - 1];
                var f1 = Frames[i];

                var p0 = f0.Position.xyz;
                var p1 = f1.Position.xyz;
                var segLen = math.distance(p0, p1);

                // Decide if this segment is ON or OFF (sample pattern at segment midpoint)
                var on = true;
                if (gaps) {
                    var midS = sPrev + 0.5f * segLen + Phase;
                    var k = math.floor(midS / cycle);
                    var frac = midS - k * cycle; // positive modulo
                    on = frac < OnLength;
                }

                if (on) {
                    if (!inRun) {
                        // Start a new ON run: seed first pair at frame i-1
                        inRun = true;
                        vOrigin = ResetVOnEachRun ? sPrev : 0f;

                        var n0 = UseFrameNormals
                            ? math.normalize(f0.Normal.xyz)
                            : math.normalize(math.cross(f0.Tangent.xyz, f0.Binormal.xyz));
                        var r0 = math.normalize(f0.Binormal.xyz);
                        var up0 = math.normalize(f0.Normal.xyz);

                        // Local offset applied to the centerline before expanding width
                        var offset0 = r0 * LocalOffset.x + up0 * LocalOffset.y + f0.Tangent.xyz * LocalOffset.z;

                        var left0 = (p0 + offset0) - r0 * halfWidth;
                        var right0 = (p0 + offset0) + r0 * halfWidth;

                        var v0 = (sPrev - vOrigin) * UVScale + VStart;

                        var vL0 = new MeshVertex {
                            Position = math.mul(LocalToWorld, new float4(left0, 1f)).xyz,
                            Normal = math.normalize(math.mul(nrmM, n0)),
                            UV = new float2(0f, v0),
                        };
                        var vR0 = new MeshVertex {
                            Position = math.mul(LocalToWorld, new float4(right0, 1f)).xyz,
                            Normal = math.normalize(math.mul(nrmM, n0)),
                            UV = new float2(1f, v0),
                        };

                        // Append seed pair
                        previousLeftIndex = Writer.Vertices.Length;
                        Writer.WriteVertex(vL0);
                        previousRightIndex = Writer.Vertices.Length;
                        Writer.WriteVertex(vR0);

                        // Append the next pair at frame i and stitch
                        var n1 = UseFrameNormals
                            ? math.normalize(f1.Normal.xyz)
                            : math.normalize(math.cross(f1.Tangent.xyz, f1.Binormal.xyz));
                        var r1 = math.normalize(f1.Binormal.xyz);
                        var up1 = math.normalize(f1.Normal.xyz);

                        var offset1 = r1 * LocalOffset.x + up1 * LocalOffset.y;

                        var left1 = (p1 + offset1) - r1 * halfWidth;
                        var right1 = (p1 + offset1) + r1 * halfWidth;

                        var v1 = (sPrev + segLen - vOrigin) * UVScale + VStart;

                        var leftVertex = new MeshVertex {
                            Position = math.mul(LocalToWorld, new float4(left1, 1f)).xyz,
                            Normal = math.normalize(math.mul(nrmM, n1)),
                            UV = new float2(0f, v1),
                        };
                        var rightVertex = new MeshVertex {
                            Position = math.mul(LocalToWorld, new float4(right1, 1f)).xyz,
                            Normal = math.normalize(math.mul(nrmM, n1)),
                            UV = new float2(1f, v1),
                        };

                        if (WindingClockwise) {
                            Writer.WriteStripStep(leftVertex, rightVertex, previousLeftIndex, previousRightIndex, out previousLeftIndex, out previousRightIndex);
                        } else {
                            Writer.WriteStripStepCCW(leftVertex, rightVertex, previousLeftIndex, previousRightIndex, out previousLeftIndex, out previousRightIndex);
                        }
                    } else {
                        // Continue current ON run: append pair at frame i and stitch
                        var n1 = UseFrameNormals
                            ? math.normalize(f1.Normal.xyz)
                            : math.normalize(math.cross(f1.Tangent.xyz, f1.Binormal.xyz));
                        var r1 = math.normalize(f1.Binormal.xyz);
                        var up1 = math.normalize(f1.Normal.xyz);

                        var offset1 = r1 * LocalOffset.x + up1 * LocalOffset.y;

                        var left1 = (p1 + offset1) - r1 * halfWidth;
                        var right1 = (p1 + offset1) + r1 * halfWidth;

                        var v1 = (sPrev + segLen - vOrigin) * UVScale + VStart;

                        var leftVertex = new MeshVertex {
                            Position = math.mul(LocalToWorld, new float4(left1, 1f)).xyz,
                            Normal = math.normalize(math.mul(nrmM, n1)),
                            UV = new float2(0f, v1),
                        };
                        var rightVertex = new MeshVertex {
                            Position = math.mul(LocalToWorld, new float4(right1, 1f)).xyz,
                            Normal = math.normalize(math.mul(nrmM, n1)),
                            UV = new float2(1f, v1),
                        };

                        if (WindingClockwise) {
                            Writer.WriteStripStep(leftVertex, rightVertex, previousLeftIndex, previousRightIndex, out previousLeftIndex, out previousRightIndex);
                        } else {
                            Writer.WriteStripStepCCW(leftVertex, rightVertex, previousLeftIndex, previousRightIndex, out previousLeftIndex, out previousRightIndex);
                        }
                    }
                } else {
                    // End/skip run
                    inRun = false;
                }

                sPrev += segLen;
            }
        }
    }
}
