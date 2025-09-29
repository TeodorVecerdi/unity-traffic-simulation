using TrafficSimulation.RoadGraph.Authoring.Grid;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace TrafficSimulation.RoadGraph.Editor.Authoring;

[EditorTool("Road Authoring Tool", typeof(GridManager))]
public sealed class RoadAuthoringSceneTool : EditorTool {
    private GridManager? m_Grid;

    private GUIContent? m_ToolbarIcon;
    public override GUIContent? toolbarIcon => m_ToolbarIcon;
    private bool m_IsDragging;
    private bool m_ShiftDown;

    private void OnEnable() {
        m_ToolbarIcon = EditorGUIUtility.IconContent("grid icon", "Road Authoring Tool|Road Authoring Tool");
    }

    private void OnDisable() {
        m_ToolbarIcon = null;
    }

    public override void OnActivated() {
        SceneView.lastActiveSceneView.ShowNotification(new GUIContent("Entering Road Authoring Tool"), 0.1f);
    }

    public override void OnWillBeDeactivated() {
        SceneView.lastActiveSceneView.ShowNotification(new GUIContent("Exiting Road Authoring Tool"), 0.1f);
    }

    public override void OnToolGUI(EditorWindow window) {
        if (window is not SceneView sceneView)
            return;

        if (m_Grid == null || !m_Grid.isActiveAndEnabled)
            m_Grid = FindFirstObjectByType<GridManager>();

        if (m_Grid == null || !m_Grid.IsValid)
            return;

        var evt = Event.current;

        // Disable default SceneView selection while tool is active
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        sceneView.wantsMouseMove = true;
        var mouseRay = HandleUtility.GUIPointToWorldRay(evt.mousePosition);

        if (TryRaycastToGrid(m_Grid, mouseRay, out var hit)) {
            var snapped = m_Grid.Snap(hit);

            // Draw hover and snapped points
            Handles.color = new Color(1f, 1f, 1f, 0.35f);
            Handles.SphereHandleCap(0, hit, Quaternion.identity, HandleUtility.GetHandleSize(hit) * 0.05f, EventType.Repaint);
            Handles.color = new Color(0.2f, 0.9f, 1f, 0.9f);
            Handles.SphereHandleCap(0, snapped, Quaternion.identity, HandleUtility.GetHandleSize(snapped) * 0.07f, EventType.Repaint);

            // Dashed line from hit to snapped
            Handles.color = new Color(0.2f, 0.9f, 1f, 0.6f);
            Handles.DrawDottedLine(hit, snapped, 4.0f);

            // Input callbacks (logging only for now)
            // Allow alt+LMB to orbit SceneView: do not consume those events
            var isAltOrbit = evt.alt && evt.button == 0;
            if (isAltOrbit) return;

            switch (evt.type) {
                case EventType.MouseDown when evt.button == 0:
                    if (evt.shift) {
                        if (!m_ShiftDown)
                            Debug.Log("[RoadAuthoring] Shift Down");
                        m_ShiftDown = true;
                    }

                    m_IsDragging = true;
                    Debug.Log($"[RoadAuthoring] MouseDown @ world={hit} snapped={snapped} shift={m_ShiftDown}");
                    evt.Use();
                    break;
                case EventType.MouseDrag when evt.button == 0:
                    Debug.Log($"[RoadAuthoring] MouseDrag @ world={hit} snapped={snapped} shift={m_ShiftDown}");
                    evt.Use();
                    break;
                case EventType.MouseUp when evt.button == 0:
                    if (m_ShiftDown && !evt.shift) {
                        Debug.Log("[RoadAuthoring] Shift Up");
                        m_ShiftDown = false;
                    }

                    m_IsDragging = false;
                    Debug.Log($"[RoadAuthoring] MouseUp   @ world={hit} snapped={snapped} shift={m_ShiftDown}");
                    evt.Use();
                    break;
                case EventType.KeyDown when evt.keyCode is KeyCode.LeftShift or KeyCode.RightShift:
                    if (!m_IsDragging && m_ShiftDown)
                        return;
                    m_ShiftDown = true;
                    Debug.Log("[RoadAuthoring] Shift Down");
                    evt.Use();
                    break;
                case EventType.KeyUp when evt.keyCode is KeyCode.LeftShift or KeyCode.RightShift:
                    m_ShiftDown = false;
                    if (!m_IsDragging)
                        return;

                    Debug.Log("[RoadAuthoring] Shift Up");
                    evt.Use();
                    break;
            }

            HandleUtility.Repaint();
        }
    }

    private static bool TryRaycastToGrid(GridManager grid, Ray ray, out float3 hitPoint) {
        var n = (float3)grid.Normal;
        var p0 = (float3)grid.Origin;

        var denom = math.dot(n, ray.direction);
        if (math.abs(denom) < 1e-6f) {
            hitPoint = default;
            return false;
        }

        var t = math.dot(n, p0 - (float3)ray.origin) / denom;
        if (t < 0.0f) {
            hitPoint = default;
            return false;
        }

        hitPoint = (float3)ray.origin + (float3)ray.direction * t;
        return true;
    }
}
