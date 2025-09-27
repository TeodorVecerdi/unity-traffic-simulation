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

    [Title("Local Offset")]
    [SerializeField, Tooltip("Offset in frame space (Right, Up) meters. Useful to place multiple markings from the same spline.")]
    private float3 m_LocalOffset;

    [Title("Ribbon")]
    [SerializeField, MinValue(0.001f), Unit(Units.Meter, Units.Millimeter)] private float m_Width = 0.2f;
    [SerializeField] private bool m_WindingClockwise;

    [Title("Dash Pattern (Gaps)")]
    [SerializeField, Unit(Units.Meter)]
    private float m_OnLength = 1.0f;
    [SerializeField, Unit(Units.Meter)]
    private float m_OffLength = 1.0f;
    [SerializeField, Unit(Units.Meter)]
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
        var spline = m_SplineContainer.Spline;

        var frameList = new NativeList<Frame>(Allocator.Temp);

        if (m_OnLength <= 0.0f || m_OffLength <= 0.0f) {
            SplineSampler.Sample(spline, m_MaxError, ref frameList);
        } else {
            SamplePreciseFrames(spline, ref frameList);
        }

        var frames = new NativeArray<Frame>(frameList.Length, Allocator.TempJob);
        frames.CopyFrom(frameList.AsArray());
        frameList.Dispose();

        var job = new RibbonStripWithGapsJob {
            Frames = frames,
            Width = m_Width,
            WindingClockwise = m_WindingClockwise,
            OnLength = m_OnLength,
            OffLength = m_OffLength,
            Phase = m_Phase,
            LocalOffset = m_LocalOffset,
            LocalToWorld = m_SplineContainer.transform.localToWorldMatrix,
            Writer = writer,
        };

        return job.Schedule(dependency);
    }

    private void SamplePreciseFrames(Spline spline, ref NativeList<Frame> frameList) {
        var totalLength = spline.GetLength();
        var cycleLength = m_OnLength + m_OffLength;

        // Collect transition points in arc-length space
        var ts = new NativeList<float>(Allocator.Temp);

        // Find first cycle start after (Phase mod cycleLength), cover [-cycleLength, totalLength + cycleLength]
        var effectivePhase = m_Phase % cycleLength;
        if (effectivePhase < 0.0f)
            effectivePhase += cycleLength;
        var s = effectivePhase - cycleLength; // start one cycle before to cover negatives

        while (s < totalLength + cycleLength) {
            var onEnd = s + m_OnLength;
            var offEnd = onEnd + m_OffLength;

            // Clamp to [0, totalLength] and add if within range
            if (s >= 0.0f && s <= totalLength) {
                ts.Add(s / totalLength);
            }

            if (onEnd >= 0.0f && onEnd <= totalLength) {
                ts.Add(onEnd / totalLength);
            }

            if (offEnd >= 0.0f && offEnd <= totalLength) {
                ts.Add(offEnd / totalLength);
            }

            s += cycleLength;
        }

        // Sort unique ts and evaluate to build precise frames
        ts.Sort();
        RemoveDuplicates(ref ts);

        frameList.Resize(ts.Length, NativeArrayOptions.ClearMemory);

        // Sample frames
        for (var i = 0; i < ts.Length; i++) {
            var t = ts[i];
            var pos = spline.EvaluatePosition(t);
            var tangent = spline.EvaluateTangent(t);
            tangent = math.normalizesafe(tangent, new float3(0.0f, 0.0f, 1.0f));

            GeometryUtils.BuildOrthonormalBasis(in tangent, math.up(), out var right, out var up);

            frameList[i] = new Frame {
                Position = new float4(pos, 1.0f),
                Tangent = new float4(tangent, 0.0f),
                Normal = new float4(up, 0.0f),
                Binormal = new float4(right, 0.0f),
            };
        }

        ts.Dispose();
    }

    private static void RemoveDuplicates(ref NativeList<float> list, float epsilon = math.EPSILON) {
        if (list.Length <= 1) return;

        var write = 1;
        for (var read = 1; read < list.Length; read++) {
            if (math.abs(list[read] - list[write - 1]) > epsilon) {
                list[write] = list[read];
                write++;
            }
        }

        list.Length = write;
    }

    [BurstCompile]
    private struct RibbonStripWithGapsJob : IJob {
        [DeallocateOnJobCompletion, ReadOnly] public NativeArray<Frame> Frames;

        // Geometry
        public float Width; // meters
        [MarshalAs(UnmanagedType.U1)]
        public bool WindingClockwise;
        public float3 LocalOffset;

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

            var hasGaps = OnLength > 0.0f && OffLength > 0.0f;
            var cycleLength = OnLength + OffLength;

            var sPrev = 0.0f; // accumulated distance up to frame i-1
            var inRun = false; // currently inside an ON run

            var previousLeftIndex = -1; // previous strip pair indices
            var previousRightIndex = -1; // previous strip pair indices

            // Normal matrix for transforming local->world normals
            var nrmM = math.transpose(math.inverse((float3x3)LocalToWorld));

            for (var i = 1; i < frameCount; i++) {
                var f0 = Frames[i - 1];
                var f1 = Frames[i];

                var p0 = f0.Position.xyz;
                var p1 = f1.Position.xyz;
                var segmentLength = math.distance(p0, p1);

                // Decide if this segment is ON or OFF (sample pattern at segment midpoint)
                var on = true;
                if (hasGaps) {
                    var midS = sPrev + 0.5f * segmentLength + Phase;
                    var k = math.floor(midS / cycleLength);
                    var frac = midS - k * cycleLength; // positive modulo
                    on = frac < OnLength;
                }

                if (!on) {
                    // End/skip run
                    inRun = false;
                    sPrev += segmentLength;
                    continue;
                }

                if (!inRun) {
                    // Start a new ON run: seed first pair at frame i-1
                    inRun = true;

                    var n0 = f0.Normal.xyz;
                    var r0 = f0.Binormal.xyz;
                    var up0 = f0.Normal.xyz;

                    // Local offset applied to the centerline before expanding width
                    var offset0 = r0 * LocalOffset.x + up0 * LocalOffset.y + f0.Tangent.xyz * LocalOffset.z;

                    var left0 = (p0 + offset0) - r0 * halfWidth;
                    var right0 = (p0 + offset0) + r0 * halfWidth;

                    var vL0 = new MeshVertex {
                        Position = math.mul(LocalToWorld, new float4(left0, 1.0f)).xyz,
                        Normal = math.normalize(math.mul(nrmM, n0)),
                        UV = new float2(0.0f, 0.0f),
                    };
                    var vR0 = new MeshVertex {
                        Position = math.mul(LocalToWorld, new float4(right0, 1.0f)).xyz,
                        Normal = math.normalize(math.mul(nrmM, n0)),
                        UV = new float2(1.0f, 0.0f),
                    };

                    // Append seed pair
                    previousLeftIndex = Writer.Vertices.Length;
                    Writer.WriteVertex(vL0);
                    previousRightIndex = Writer.Vertices.Length;
                    Writer.WriteVertex(vR0);
                }

                var n1 = f1.Normal.xyz;
                var r1 = f1.Binormal.xyz;
                var up1 = f1.Normal.xyz;

                var offset1 = r1 * LocalOffset.x + up1 * LocalOffset.y + f1.Tangent.xyz * LocalOffset.z;
                var left1 = (p1 + offset1) - r1 * halfWidth;
                var right1 = (p1 + offset1) + r1 * halfWidth;

                var leftVertex = new MeshVertex {
                    Position = math.mul(LocalToWorld, new float4(left1, 1.0f)).xyz,
                    Normal = math.normalize(math.mul(nrmM, n1)),
                    UV = new float2(0.0f, 1.0f),
                };
                var rightVertex = new MeshVertex {
                    Position = math.mul(LocalToWorld, new float4(right1, 1.0f)).xyz,
                    Normal = math.normalize(math.mul(nrmM, n1)),
                    UV = new float2(1.0f, 1.0f),
                };

                if (WindingClockwise) {
                    Writer.WriteStripStep(leftVertex, rightVertex, previousLeftIndex, previousRightIndex, out previousLeftIndex, out previousRightIndex);
                } else {
                    Writer.WriteStripStepCCW(leftVertex, rightVertex, previousLeftIndex, previousRightIndex, out previousLeftIndex, out previousRightIndex);
                }

                sPrev += segmentLength;
            }
        }
    }
}
