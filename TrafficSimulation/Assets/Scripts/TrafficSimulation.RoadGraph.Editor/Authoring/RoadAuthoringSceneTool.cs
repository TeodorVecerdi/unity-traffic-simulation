using TrafficSimulation.Core.Maths;
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
        var originalColor = Handles.color;

        // Disable default SceneView selection while tool is active
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        sceneView.wantsMouseMove = true;
        var mouseRay = HandleUtility.GUIPointToWorldRay(evt.mousePosition);
        if (!TryRaycastToGrid(m_Grid, mouseRay, out var hit))
            return;

        var offset = m_Grid.SelectedRoadType.GetGridOffset();
        var snapped = m_Grid.Snap(hit, offset);

        // Draw hit and snapped positions
        DrawHitPosition(hit, snapped);

        // Draw ghost rectangle for the selected road type
        if (m_Grid.SelectedRoadType is not SelectedRoadType.None) {
            DrawGhostRectangle(m_Grid, snapped);
        }

        // Drag preview
        if (m_IsDragging) {
            DrawRoadSegmentPreview(snapped);
        }

        Handles.color = originalColor;

        // Input callbacks (visual-only now; no logging)
        // Allow alt+LMB to orbit SceneView: do not consume those events
        var isAltOrbit = evt is { alt: true, button: 0 };
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

    private static void DrawHitPosition(float3 hit, float3 snapped) {
        // Draw hover and snapped points
        Handles.color = new Color(1f, 1f, 1f, 0.35f);
        Handles.SphereHandleCap(0, hit, Quaternion.identity, HandleUtility.GetHandleSize(hit) * 0.05f, EventType.Repaint);
        Handles.color = new Color(0.2f, 0.9f, 1f, 0.9f);
        Handles.SphereHandleCap(0, snapped, Quaternion.identity, HandleUtility.GetHandleSize(snapped) * 0.07f, EventType.Repaint);

        // Dashed line from hit to snapped
        Handles.color = new Color(0.2f, 0.9f, 1f, 0.6f);
        Handles.DrawDottedLine(hit, snapped, 4.0f);
    }

    private void DrawRoadSegmentPreview(float3 snapped) {
        var start = m_DragStartSnapped;
        var end = snapped;
        var delta = end - start;
        var len = math.length(delta);
        if (len < 1e-5f)
            return;

        var roadType = m_Grid!.SelectedRoadType;
        var span = roadType.GetRoadSpan();
        var cellSize = m_Grid.Settings.CellSize;
        var roadWidth = span.x * cellSize;

        // Calculate extension amount to cover full cells (half a cell in each direction along the road)
        var halfCellExtension = cellSize * 0.5f * span.y;

        // Build orthonormal basis for the grid plane
        var n = math.normalize(m_Grid.Normal);
        GeometryUtils.BuildOrthonormalBasis(n, math.up(), out var gridRight, out var gridUp);

        if (!m_ShiftDown) {
            // Straight segment preview - draw oriented box
            var forward = math.normalizesafe(delta, gridUp);
            var right = math.normalizesafe(math.cross(n, forward), gridRight);
            var halfWidth = roadWidth * 0.5f;

            // Extend start and end positions to cover full cells
            var extendedStart = start - forward * halfCellExtension;
            var extendedEnd = end + forward * halfCellExtension;

            // Calculate the four corners of the oriented box
            var corner0 = extendedStart - right * halfWidth;
            var corner1 = extendedStart + right * halfWidth;
            var corner2 = extendedEnd + right * halfWidth;
            var corner3 = extendedEnd - right * halfWidth;

            // Draw the road geometry
            var fillColor = new Color(0.2f, 0.9f, 1f, 0.25f);
            var outlineColor = new Color(0.2f, 0.9f, 1f, 0.8f);
            var corners = new Vector3[] { corner0, corner1, corner2, corner3 };
            Handles.DrawSolidRectangleWithOutline(corners, fillColor, outlineColor);

            // Draw centerline
            Handles.color = new Color(0.2f, 0.9f, 1f, 0.6f);
            Handles.DrawLine(extendedStart, extendedEnd);
        } else {
            // Bezier preview with a single shared handle direction (L-shaped bend)
            var t = math.normalizesafe(delta, new float3(1, 0, 0));
            var lateral = math.normalizesafe(math.cross(n, t), new float3(0, 0, 1)) * m_HandleSign;

            // First, calculate control points based on original endpoints to determine tangent directions
            var handleLen = len * 0.45f;
            var endBaseControlPoint = end + lateral * handleLen;
            var startBaseControlPoint = start + lateral * (handleLen * 0.25f);
            var startControlPoint = math.lerp(startBaseControlPoint, endBaseControlPoint, 0.35f);
            var endControlPoint = math.lerp(startControlPoint, endBaseControlPoint, 0.35f);

            // Calculate tangent directions from control points for proper extension
            var startTangent = math.normalizesafe(startControlPoint - start, delta);
            var endTangent = math.normalizesafe(end - endControlPoint, delta);

            // Extend start and end positions along their tangent directions
            var extendedStart = start - startTangent * halfCellExtension;
            var extendedEnd = end + endTangent * halfCellExtension;

            // Recalculate control points based on extended endpoints
            var extendedDelta = extendedEnd - extendedStart;
            var extendedLen = math.length(extendedDelta);
            var extendedHandleLen = extendedLen * 0.45f;
            var extendedEndBaseControlPoint = extendedEnd + lateral * extendedHandleLen;
            var extendedStartBaseControlPoint = extendedStart + lateral * (extendedHandleLen * 0.25f);
            var extendedStartControlPoint = math.lerp(extendedStartBaseControlPoint, extendedEndBaseControlPoint, 0.35f);
            var extendedEndControlPoint = math.lerp(extendedStartControlPoint, extendedEndBaseControlPoint, 0.35f);

            // Draw road geometry along bezier curve
            DrawBezierRoad(extendedStart, extendedEnd, extendedStartControlPoint, extendedEndControlPoint, roadWidth, n);

            // Dotted guide lines to handles and small discs
            Handles.color = new Color(0.2f, 0.9f, 1f, 0.4f);
            Handles.DrawDottedLine(extendedStart, extendedStartControlPoint, 4.0f);
            Handles.DrawDottedLine(extendedEnd, extendedEndControlPoint, 4.0f);
            Handles.SphereHandleCap(0, extendedStartControlPoint, Quaternion.identity, HandleUtility.GetHandleSize(extendedStartControlPoint) * 0.04f, EventType.Repaint);
            Handles.SphereHandleCap(0, extendedEndControlPoint, Quaternion.identity, HandleUtility.GetHandleSize(extendedEndControlPoint) * 0.04f, EventType.Repaint);

            // Draw centerline bezier
            Handles.color = new Color(0.2f, 0.9f, 1f, 0.6f);
            Handles.DrawBezier(extendedStart, extendedEnd, extendedStartControlPoint, extendedEndControlPoint, Handles.color, null, 1.5f);
        }
    }

    private static void DrawBezierRoad(float3 start, float3 end, float3 startControl, float3 endControl, float roadWidth, float3 normal) {
        const int segments = 32;
        var halfWidth = roadWidth * 0.5f;
        var fillColor = new Color(0.2f, 0.9f, 1f, 0.25f);
        var outlineColor = new Color(0.2f, 0.9f, 1f, 0.8f);

        // Sample bezier curve and calculate perpendicular offsets
        var leftPoints = new Vector3[segments + 1];
        var rightPoints = new Vector3[segments + 1];

        for (var i = 0; i <= segments; i++) {
            var t = i / (float)segments;
            var pos = EvaluateBezier(start, end, startControl, endControl, t);
            var tangent = EvaluateBezierDerivative(start, end, startControl, endControl, t);
            var right = math.normalizesafe(math.cross(normal, tangent), new float3(1, 0, 0));

            leftPoints[i] = pos - right * halfWidth;
            rightPoints[i] = pos + right * halfWidth;
        }

        // Draw filled quads between segments
        for (var i = 0; i < segments; i++) {
            var quad = new[] {
                leftPoints[i],
                rightPoints[i],
                rightPoints[i + 1],
                leftPoints[i + 1],
            };
            Handles.DrawSolidRectangleWithOutline(quad, fillColor, Color.clear);
        }

        // Draw outline edges
        Handles.color = outlineColor;
        for (var i = 0; i < segments; i++) {
            Handles.DrawLine(leftPoints[i], leftPoints[i + 1]);
            Handles.DrawLine(rightPoints[i], rightPoints[i + 1]);
        }

        // Close the ends
        Handles.DrawLine(leftPoints[0], rightPoints[0]);
        Handles.DrawLine(leftPoints[segments], rightPoints[segments]);
    }

    private static float3 EvaluateBezier(float3 p0, float3 p3, float3 p1, float3 p2, float t) {
        var u = 1.0f - t;
        var tt = t * t;
        var uu = u * u;
        var uuu = uu * u;
        var ttt = tt * t;
        return uuu * p0 + 3.0f * uu * t * p1 + 3.0f * u * tt * p2 + ttt * p3;
    }

    private static float3 EvaluateBezierDerivative(float3 p0, float3 p3, float3 p1, float3 p2, float t) {
        var u = 1.0f - t;
        var uu = u * u;
        var tt = t * t;

        return 3.0f * uu * (p1 - p0) + 6.0f * u * t * (p2 - p1) + 3.0f * tt * (p3 - p2);
    }

    private static bool TryRaycastToGrid(GridManager grid, Ray ray, out float3 hitPoint) {
        var n = (float3)grid.Normal;
        var p0 = (float3)grid.Origin;

        var denominator = math.dot(n, ray.direction);
        if (math.abs(denominator) < 1e-6f) {
            hitPoint = default;
            return false;
        }

        var t = math.dot(n, p0 - (float3)ray.origin) / denominator;
        if (t < 0.0f) {
            hitPoint = default;
            return false;
        }

        hitPoint = (float3)ray.origin + (float3)ray.direction * t;
        return true;
    }

    private static void DrawGhostRectangle(GridManager grid, float3 snappedPosition) {
        var roadType = grid.SelectedRoadType;
        var span = roadType.GetRoadSpan();
        var cellSize = grid.Settings.CellSize;

        // Build orthonormal basis for the grid plane
        var n = math.normalize(grid.Normal);
        GeometryUtils.BuildOrthonormalBasis(n, math.up(), out var right, out var up);

        // Calculate the rectangle dimensions in world space
        var widthAcross = span.x * cellSize;
        var widthAlong = span.y * cellSize;

        // Calculate the half extents
        var halfAcross = widthAcross * 0.5f;
        var halfAlong = widthAlong * 0.5f;

        // Calculate the four corners of the rectangle centered at snappedPosition
        var corner0 = snappedPosition + right * -halfAcross + up * -halfAlong;
        var corner1 = snappedPosition + right * +halfAcross + up * -halfAlong;
        var corner2 = snappedPosition + right * +halfAcross + up * +halfAlong;
        var corner3 = snappedPosition + right * -halfAcross + up * +halfAlong;

        // Draw filled rectangle with outline
        var fillColor = new Color(0.2f, 0.9f, 1f, 0.15f);
        var outlineColor = new Color(0.2f, 0.9f, 1f, 0.6f);

        var corners = new Vector3[] { corner0, corner1, corner2, corner3 };
        Handles.DrawSolidRectangleWithOutline(corners, fillColor, outlineColor);
    }
}
