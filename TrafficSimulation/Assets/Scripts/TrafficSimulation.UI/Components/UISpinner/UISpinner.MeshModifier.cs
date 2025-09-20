using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace TrafficSimulation.UI;

public sealed partial class UISpinner {
    void IMeshModifier.ModifyMesh(Mesh mesh) {
        using VertexHelper vh = new(mesh);
        ((IMeshModifier)this).ModifyMesh(vh);
        vh.FillMesh(mesh);
    }

    void IMeshModifier.ModifyMesh(VertexHelper vh) {
        if (!isActiveAndEnabled || vh.currentVertCount <= 0) {
            return;
        }

        var (minPosition, maxPosition) = CalculateBounds(vh);
        var angle = GetAngle();

        UIVertex vertex = new();
        for (var i = 0; i < vh.currentVertCount; i++) {
            vh.PopulateUIVertex(ref vertex, i);

            var uvx = (vertex.position.x - minPosition.x) / (maxPosition.x - minPosition.x);
            var uvy = (vertex.position.y - minPosition.y) / (maxPosition.y - minPosition.y);
            vertex.uv1 = new Vector2(uvx, uvy);
            vertex.uv2 = new Vector4(angle, Radius, Thickness, Smoothness);
            vertex.uv3 = new Vector4(ArcLength, Rotation, OffsetRotationByArcLength);

            vh.SetUIVertex(vertex, i);
        }
    }

    [ContextMenu("Mark Dirty")]
    private void MarkDirty() {
        Image.SetVerticesDirty();
        m_MeshModifierProperties.LinkedGraphics.ForEach(g => g.SetVerticesDirty());
    }

    private (Vector3 Min, Vector3 Max) CalculateBounds(VertexHelper vertexHelper) {
        if (m_MeshModifierProperties.BoundsMode is BoundsMode.Self || m_MeshModifierProperties.BoundsTransforms.Count < 1) {
            var min = Vector3.positiveInfinity;
            var max = Vector3.negativeInfinity;

            UIVertex vertex = new();
            for (var i = 0; i < vertexHelper.currentVertCount; i++) {
                vertexHelper.PopulateUIVertex(ref vertex, i);
                min = Vector3.Min(min, vertex.position);
                max = Vector3.Max(max, vertex.position);
            }

            return (min, max);
        }

        if (m_MeshModifierProperties.BoundsMode is BoundsMode.FromTransforms) {
            var min = Vector3.positiveInfinity;
            var max = Vector3.negativeInfinity;

            var corners = new Vector3[4];
            foreach (var rt in m_MeshModifierProperties.BoundsTransforms) {
                rt.GetWorldCorners(corners);
                for (var i = 0; i < corners.Length; i++) {
                    corners[i] = Image.rectTransform.InverseTransformPoint(corners[i]);
                }

                min = Min(min, corners);
                max = Max(max, corners);
            }

            return (min, max);
        }

        throw new ArgumentOutOfRangeException(nameof(m_MeshModifierProperties.BoundsMode), m_MeshModifierProperties.BoundsMode, null);
    }

    private static Vector3 Min(Vector3 current, IReadOnlyCollection<Vector3> vectors) => vectors.Aggregate(current, Vector3.Min);
    private static Vector3 Max(Vector3 current, IReadOnlyCollection<Vector3> vectors) => vectors.Aggregate(current, Vector3.Max);

    private enum BoundsMode {
        /// <summary>
        /// Use the mesh's bounds.
        /// </summary>
        Self,

        /// <summary>
        /// Calculate bounds encompassing the referenced transforms.
        /// </summary>
        FromTransforms,
    }

    [Serializable, HideReferenceObjectPicker, InlineProperty, HideLabel]
    private class MeshModifierProperties {
        [SerializeField, EnumToggleButtons]
        private BoundsMode m_BoundsMode = BoundsMode.Self;
        [SerializeField, Required, ShowIf(nameof(IsFromTransformsMode))]
        private List<RectTransform> m_BoundsTransforms = new();
        [SerializeField, Required, ShowIf(nameof(IsFromTransformsMode))]
        private List<Graphic> m_LinkedGraphics = new();

        public BoundsMode BoundsMode => m_BoundsMode;
        public List<RectTransform> BoundsTransforms => m_BoundsTransforms;
        public List<Graphic> LinkedGraphics => m_LinkedGraphics;

        private bool IsFromTransformsMode => m_BoundsMode is BoundsMode.FromTransforms;
    }
}
