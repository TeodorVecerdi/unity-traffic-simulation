using Sirenix.OdinInspector;
using TrafficSimulation.Geometry.Data;
using TrafficSimulation.Geometry.Graph;
using UnityEngine;

namespace TrafficSimulation.Geometry.Authoring;

[ExecuteAlways, DisallowMultipleComponent]
public sealed class MeshGraphAuthoring : MonoBehaviour {
    [Title("Graph Input")]
    [SerializeField, Required, InlineProperty, HideLabel] private MeshGraph m_Graph = null!;

    [Title("References")]
    [SerializeField, Required] private MeshFilter m_MeshFilter = null!;
    [SerializeField, Required] private MeshRenderer m_MeshRenderer = null!;

    [Title("Settings")]
    [SerializeField] private bool m_AutoRebuildInEditor = true;
    [SerializeField] private bool m_BuildSync = true;

    // Internal scheduled build state
    private MeshBuildHandle m_Handle;
    private bool m_HasScheduled;

    // This gets shown in inspector for quick debugging
    [Title("Debug Info")]
    [ShowInInspector, ReadOnly]
    private Mesh? CurrentMesh => m_MeshFilter != null ? m_MeshFilter.sharedMesh : null;

    [ShowInInspector, ReadOnly]
    private Material[] CurrentMaterials => m_MeshRenderer != null ? m_MeshRenderer.sharedMaterials : [];

    [Button(ButtonSizes.Large)]
    public void Rebuild(string meshName = "Mesh") {
        if (m_Graph == null!)
            return;

        EnsureComponents();
        if (m_MeshFilter == null || m_MeshRenderer == null)
            return;

        var context = new MeshGenerationContext(transform.localToWorldMatrix, transform.worldToLocalMatrix);
        if (m_BuildSync) {
            var result = MeshBuildPipeline.Build(m_Graph, context, meshName);
            ApplyResult(result.Mesh, result.Materials);
        } else {
            if (m_HasScheduled) {
                // If there's an existing job, complete it first to dispose resources properly
                var mesh = m_Handle.GetResult().Mesh;
                mesh.DestroyObject();
                m_Handle = default;
            }

            m_Handle = MeshBuildPipeline.ScheduleBuild(m_Graph, context, meshName);
            m_HasScheduled = true;
        }
    }

    [Button(ButtonSizes.Medium)]
    public void Clear() {
        if (m_MeshFilter != null) {
            m_MeshFilter.sharedMesh = null;
        }

        if (m_MeshRenderer != null) {
            m_MeshRenderer.sharedMaterials = [];
        }
    }

    private void OnEnable() {
        EnsureComponents();
        if (m_AutoRebuildInEditor && !Application.isPlaying) {
            Rebuild();
        }
    }

    private void Update() {
        if (!m_HasScheduled) return;
        if (m_Handle.IsCompleted) {
            var handle = m_Handle;
            m_Handle = default;
            m_HasScheduled = false;

            var result = handle.GetResult();
            ApplyResult(result.Mesh, result.Materials);
        }
    }

    private void EnsureComponents() {
        if (m_MeshFilter == null) {
            m_MeshFilter = GetComponent<MeshFilter>();
        }

        if (m_MeshRenderer == null) {
            m_MeshRenderer = GetComponent<MeshRenderer>();
        }
    }

    private void ApplyResult(Mesh mesh, Material[] materials) {
        if (m_MeshFilter != null) {
            var oldMesh = m_MeshFilter.sharedMesh;
            if (oldMesh != null && oldMesh != mesh) {
                oldMesh.DestroyObject();
            }

            m_MeshFilter.sharedMesh = mesh;
        }

        if (m_MeshRenderer != null) {
            m_MeshRenderer.sharedMaterials = materials;
        }
    }
}
