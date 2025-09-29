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
    private float3 m_DragStartSnapped;
    private int m_HandleSign = 1; // +1 or -1, toggled by scroll while dragging

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

            // Drag preview
            if (m_IsDragging) {
                var start = m_DragStartSnapped;
                var end = snapped;
                var delta = end - start;
                var len = math.length(delta);
                if (len > 1e-5f) {
                    if (!m_ShiftDown) {
                        // Straight segment preview
                        Handles.color = new Color(0.2f, 0.9f, 1f, 0.95f);
                        Handles.DrawLine(start, end);
                    } else {
                        // Bezier preview with a single shared handle direction (L-shaped bend)
                        var n = (float3)m_Grid.Normal;
                        var t = math.normalizesafe(delta, new float3(1, 0, 0));
                        var lateral = math.normalizesafe(math.cross(n, t), new float3(0, 0, 1)) * m_HandleSign;

                        var handleLen = len * 0.45f;
                        var endBaseControlPoint = end + lateral * handleLen;
                        var startBaseControlPoint = start + lateral * (handleLen * 0.25f);
                        var startControlPoint = math.lerp(startBaseControlPoint, endBaseControlPoint, 0.35f);
                        var endControlPoint = math.lerp(startControlPoint, endBaseControlPoint, 0.35f);

                        Handles.color = new Color(0.2f, 0.9f, 1f, 0.95f);
                        Handles.DrawBezier(start, end, startControlPoint, endControlPoint, Handles.color, null, 2.0f);

                        // Dotted guide lines to handles and small discs
                        Handles.color = new Color(0.2f, 0.9f, 1f, 0.6f);
                        Handles.DrawDottedLine(start, startControlPoint, 4.0f);
                        Handles.DrawDottedLine(end, endControlPoint, 4.0f);
                        Handles.SphereHandleCap(0, startControlPoint, Quaternion.identity, HandleUtility.GetHandleSize(startControlPoint) * 0.04f, EventType.Repaint);
                        Handles.SphereHandleCap(0, endControlPoint, Quaternion.identity, HandleUtility.GetHandleSize(endControlPoint) * 0.04f, EventType.Repaint);
                    }
                }
            }

            // Input callbacks (visual-only now; no logging)
            // Allow alt+LMB to orbit SceneView: do not consume those events
            var isAltOrbit = evt.alt && evt.button == 0;
            if (isAltOrbit) return;

            switch (evt.type) {
                case EventType.MouseDown when evt.button == 0:
                    if (evt.shift) m_ShiftDown = true;
                    m_IsDragging = true;
                    m_DragStartSnapped = snapped;
                    m_HandleSign = 1; // default direction; can be flipped with scroll
                    evt.Use();
                    break;
                case EventType.MouseDrag when evt.button == 0:
                    evt.Use();
                    break;
                case EventType.MouseUp when evt.button == 0:
                    if (m_ShiftDown && !evt.shift) m_ShiftDown = false;
                    m_IsDragging = false;
                    evt.Use();
                    break;
                case EventType.ScrollWheel:
                    if (m_IsDragging && m_ShiftDown) {
                        if (evt.delta.sqrMagnitude > 0.001f) {
                            m_HandleSign = -m_HandleSign;
                            evt.Use();
                        }
                    }

                    break;
                case EventType.KeyDown when evt.keyCode is KeyCode.LeftShift or KeyCode.RightShift:
                    if (!m_IsDragging) return;
                    m_ShiftDown = true;
                    evt.Use();
                    break;
                case EventType.KeyUp when evt.keyCode is KeyCode.LeftShift or KeyCode.RightShift:
                    m_ShiftDown = false;
                    if (!m_IsDragging) return;
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
