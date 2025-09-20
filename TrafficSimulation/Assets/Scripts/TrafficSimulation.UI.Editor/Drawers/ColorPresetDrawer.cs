using System.Collections.Concurrent;
using System.Reflection;
using TrafficSimulation.UI.Colors;
using TrafficSimulation.UI.Editor.UIToolkit;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace TrafficSimulation.UI.Editor.Drawers;

public class ColorPresetDrawer : OdinValueDrawer<ColorPreset> {
    private static readonly ConcurrentDictionary<Type, MethodInfo?> s_OnValidateMethods = [];

    protected override void DrawPropertyLayout(GUIContent label) {
        // Use Odin's built-in horizontal group for better layout
        SirenixEditorGUI.BeginHorizontalPropertyLayout(label);

        // Draw the object field using Odin's property system

        // Let Odin handle the object field drawing (this preserves all Odin functionality)
        CallNextDrawer(GUIContent.none);

        // Draw the palette picker button
        var objectFieldRect = EditorGUILayout.GetControlRect(GUILayout.Width(22));
        var buttonRect = new Rect(objectFieldRect.x + 2, objectFieldRect.y, 20, objectFieldRect.height);
        if (SirenixEditorGUI.SDFIconButton(buttonRect, new GUIContent("", "Open Color Palette Picker"), SdfIconType.PaletteFill)) {
            OpenColorPalettePicker();
        }

        SirenixEditorGUI.EndHorizontalPropertyLayout();
    }

    private void OpenColorPalettePicker() {
        var currentValues = ValueEntry.Values.ToList();
        ColorPalettePickerWindow.ShowPicker(currentValues!, SetValue, ResetValues);
    }

    private void SetValue(ColorPreset? newValue) {
        // Record undo state
        Property.Tree.UpdateTree();
        var targets = Property.Tree.WeakTargets.Cast<Object>().ToArray();
        Undo.RecordObjects(targets, "Change Color Preset");

        // Set values
        ValueEntry.SmartValue = newValue!;

        // Apply changes and trigger callbacks
        Property.Tree.ApplyChanges();
        ForceCompleteUpdate(targets);
        GUIHelper.RequestRepaint();
    }

    private void ResetValues(List<ColorPreset?> originalValues) {
        // Record undo state
        Property.Tree.UpdateTree();
        var targets = Property.Tree.WeakTargets.Cast<Object>().ToArray();
        Undo.RecordObjects(targets, "Reset Color Preset");

        // Set values
        for (var i = 0; i < ValueEntry.Values.Count; i++) {
            ValueEntry.Values[i] = originalValues[i]!;
        }

        // Apply changes and trigger callbacks
        Property.Tree.ApplyChanges();
        ForceCompleteUpdate(targets);
        GUIHelper.RequestRepaint();
    }

    private static void ForceCompleteUpdate(Object[] targets) {
        foreach (var target in targets) {
            if (target == null) continue;
            TriggerOnValidate(target);
        }

        EditorApplication.QueuePlayerLoopUpdate();
        SceneView.RepaintAll();
    }

    private static void TriggerOnValidate(Object target) {
        if (target is MonoBehaviour or ScriptableObject) {
            var onValidateMethod = s_OnValidateMethods.GetOrAdd(target.GetType(), t => t.GetMethod("OnValidate", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance));
            onValidateMethod?.Invoke(target, null);
        }
    }
}
