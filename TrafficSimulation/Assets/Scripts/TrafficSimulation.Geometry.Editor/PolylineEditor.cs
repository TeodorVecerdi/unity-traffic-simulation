using TrafficSimulation.Geometry.Data;
using UnityEditor;
using UnityEngine;

namespace TrafficSimulation.Geometry.Editor;

[CustomEditor(typeof(Polyline))]
public sealed class PolylineEditor : UnityEditor.Editor {
    private void OnSceneGUI() {
        var polyline = (Polyline)target;
        var tr = polyline.transform;

        if (polyline.Points.Count == 0) return;

        for (var i = 0; i < polyline.Points.Count; i++) {
            EditorGUI.BeginChangeCheck();
            var p = polyline.Points[i];
            var worldPos = tr.TransformPoint(p.Position);
            var newWorldPos = Handles.PositionHandle(worldPos, Quaternion.identity);
            if (EditorGUI.EndChangeCheck()) {
                Undo.RecordObject(polyline, "Move Polyline Point");
                p.Position = tr.InverseTransformPoint(newWorldPos);
                polyline.Points[i] = p;
                EditorUtility.SetDirty(polyline);
            }
        }
    }
}
