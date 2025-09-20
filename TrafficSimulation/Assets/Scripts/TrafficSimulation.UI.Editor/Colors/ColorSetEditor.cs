using TrafficSimulation.UI.Colors;
using UnityEditor;
using UnityEngine;

namespace TrafficSimulation.UI.Editor.Colors;

[CustomEditor(typeof(ColorSet))]
public sealed class ColorSetEditor : UnityEditor.Editor {
    private SerializedProperty m_DisplayNameProperty = null!;
    private SerializedProperty m_PrefixProperty = null!;
    private SerializedProperty m_DisplayPriorityProperty = null!;
    private SerializedProperty m_ColorPresetsProperty = null!;

    private void OnEnable() {
        m_DisplayNameProperty = serializedObject.FindProperty("m_DisplayName");
        m_PrefixProperty = serializedObject.FindProperty("m_Prefix");
        m_DisplayPriorityProperty = serializedObject.FindProperty("m_DisplayPriority");
        m_ColorPresetsProperty = serializedObject.FindProperty("m_ColorPresets");
    }

    public override void OnInspectorGUI() {
        serializedObject.Update();

        EditorGUILayout.PropertyField(m_DisplayNameProperty, new GUIContent("Display Name"));
        EditorGUILayout.PropertyField(m_PrefixProperty, new GUIContent("Prefix"));
        EditorGUILayout.PropertyField(m_DisplayPriorityProperty, new GUIContent("Display Priority"));
        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(m_ColorPresetsProperty, new GUIContent("Color Presets"));
        EditorGUILayout.Space();

        // Show some info about the color set
        var colorSet = target as ColorSet;
        if (colorSet != null && colorSet.ColorPresets.Count > 0) {
            EditorGUILayout.LabelField("Info", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Color Count: {colorSet.ColorPresets.Count}");

            if (colorSet.ColorPresets.Count > 0) {
                EditorGUILayout.LabelField("Preview:");
                DrawColorPreview(colorSet);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private static void DrawColorPreview(ColorSet colorSet) {
        const float swatchSize = 20f;
        const float spacing = 2f;

        var rect = GUILayoutUtility.GetRect(0, swatchSize);
        var currentX = rect.x;
        var availableWidth = rect.width;
        var maxSwatches = Mathf.FloorToInt(availableWidth / (swatchSize + spacing));
        var swatchesToShow = Mathf.Min(colorSet.ColorPresets.Count, maxSwatches);

        for (var i = 0; i < swatchesToShow; i++) {
            var preset = colorSet.ColorPresets[i];
            if (preset != null) {
                var swatchRect = new Rect(currentX, rect.y, swatchSize, swatchSize);
                var color = preset.GetColor1();

                EditorGUI.DrawRect(swatchRect, Color.black);
                EditorGUI.DrawRect(swatchRect, color);

                currentX += swatchSize + spacing;
            }
        }

        if (colorSet.ColorPresets.Count > swatchesToShow) {
            var labelRect = new Rect(currentX, rect.y, 50, swatchSize);
            EditorGUI.LabelField(labelRect, $"+{colorSet.ColorPresets.Count - swatchesToShow}");
        }
    }
}
