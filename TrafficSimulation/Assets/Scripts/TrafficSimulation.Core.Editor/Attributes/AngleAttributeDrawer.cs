using System.Reflection;
using TrafficSimulation.Core.Attributes;
using UnityEditor;
using UnityEngine;

namespace TrafficSimulation.Core.Editor.Attributes;

[CustomPropertyDrawer(typeof(AngleAttribute))]
public sealed class AngleAttributeDrawer : PropertyDrawer {
    private static readonly EditorResource<Texture2D> s_AngleBackgroundTexture = new("EditorIcons/AngleAttribute/AngleBackground");
    private static readonly EditorResource<Texture2D> s_AngleForegroundTexture = new("EditorIcons/AngleAttribute/AngleForeground");

    private static readonly MethodInfo s_DoFloatFieldMethod;
    private static readonly FieldInfo s_RecycledEditorPropertyField;
    private static readonly FieldInfo s_FloatFieldFormatStringField;

    private static readonly Color s_AngleBgColor;
    private static readonly Color s_AngleFgColor;
    private static readonly Color s_AngleFgColorActive;

    private static float ControlHeight => EditorGUIUtility.singleLineHeight * 2.0f;
    private static float KnobSize => EditorGUIUtility.singleLineHeight * 2.5f;
    private static float KnobYOffset => (ControlHeight - KnobSize) / 2;

    static AngleAttributeDrawer() {
        Type[] argumentTypes = [Assembly.GetAssembly(typeof(EditorGUI)).GetType("UnityEditor.EditorGUI+RecycledTextEditor"), typeof(Rect), typeof(Rect), typeof(int), typeof(float), typeof(string), typeof(GUIStyle), typeof(bool)];
        s_DoFloatFieldMethod = typeof(EditorGUI).GetMethod("DoFloatField", BindingFlags.Static | BindingFlags.NonPublic, null, argumentTypes, null)!;
        s_RecycledEditorPropertyField = typeof(EditorGUI).GetField("s_RecycledEditorInternal", BindingFlags.Static | BindingFlags.NonPublic)!;
        s_FloatFieldFormatStringField = typeof(EditorGUI).GetField("kFloatFieldFormatString", BindingFlags.Static | BindingFlags.NonPublic)!;
        s_AngleBgColor = new Color(0.164f, 0.164f, 0.164f);
        s_AngleFgColor = new Color(0.701f, 0.701f, 0.701f);
        s_AngleFgColorActive = new Color(0.49f, 0.67f, 0.94f);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) => DrawAngleProperty(position, label, property, Vector2.down);
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => ControlHeight;

    private static float DoFloatFieldInternal(Rect position, Rect dragHotZone, int id, float value, string? formatString = null, GUIStyle? style = null, bool draggable = true) {
        style ??= EditorStyles.numberField;
        formatString ??= (string)s_FloatFieldFormatStringField.GetValue(null);

        var editor = s_RecycledEditorPropertyField.GetValue(null);
        return (float)s_DoFloatFieldMethod.Invoke(null, [editor, position, dragHotZone, id, value, formatString, style, draggable]);
    }

    private static void DrawAngleProperty(Rect rect, GUIContent label, SerializedProperty prop, Vector2 zeroVector) {
        using var propScope = new EditorGUI.PropertyScope(rect, label, prop);
        using var changeScope = new EditorGUI.ChangeCheckScope();

        var angle = prop.floatValue;
        var prevAngle = angle;

        var labelRect = new Rect(rect) {
            y = rect.y + (ControlHeight - EditorGUIUtility.singleLineHeight) / 2,
            height = EditorGUIUtility.singleLineHeight,
        };

        var fieldId = GUIUtility.GetControlID(FocusType.Keyboard, labelRect);
        var fieldRect = EditorGUI.PrefixLabel(labelRect, fieldId, propScope.content);
        labelRect.xMax = fieldRect.x;
        fieldRect.x += ControlHeight;
        fieldRect.width -= ControlHeight;

        var knobRect = new Rect(rect.x + EditorGUIUtility.labelWidth + KnobYOffset, rect.y + KnobYOffset, KnobSize, KnobSize);

        var knobId = GUIUtility.GetControlID(FocusType.Passive, knobRect);
        if (Event.current != null) {
            if (Event.current.type == EventType.MouseDown && knobRect.Contains(Event.current.mousePosition)) {
                GUIUtility.hotControl = knobId;
                angle = GetAngleWrapped(zeroVector, (Event.current.mousePosition - knobRect.center).normalized);
            } else if (Event.current.type == EventType.MouseUp && GUIUtility.hotControl == knobId) {
                GUIUtility.hotControl = 0;
            } else if (Event.current.type == EventType.MouseDrag && GUIUtility.hotControl == knobId) {
                angle = GetAngleWrapped(zeroVector, (Event.current.mousePosition - knobRect.center).normalized);
            } else if (Event.current.type == EventType.Repaint) {
                var notRotated = GUI.matrix;
                var oldColor = GUI.color;
                var highlighted = GUIUtility.hotControl == knobId || GUIUtility.hotControl == fieldId || GUIUtility.keyboardControl == fieldId;

                GUIUtility.RotateAroundPivot(angle - 90.0f, knobRect.center);
                GUI.color = s_AngleBgColor;
                GUI.DrawTexture(knobRect, s_AngleBackgroundTexture.Value!, ScaleMode.ScaleToFit, true, 1);
                GUI.color = highlighted ? s_AngleFgColorActive : s_AngleFgColor;
                GUI.DrawTexture(knobRect, s_AngleForegroundTexture.Value!, ScaleMode.ScaleToFit, true, 1);

                GUI.matrix = notRotated;
                GUI.color = oldColor;
            }

            if (Mathf.Abs(angle - prevAngle) > 0.001f) {
                GUI.changed = true;
            }
        }

        angle = DoFloatFieldInternal(fieldRect, labelRect, fieldId, angle);
        if (changeScope.changed) {
            prop.floatValue = angle;
        }
    }

    private static float GetAngleWrapped(Vector2 from, Vector2 to) {
        var angle = Vector2.SignedAngle(from, to);
        return angle < 0.0f ? angle + 360.0f : angle;
    }
}
