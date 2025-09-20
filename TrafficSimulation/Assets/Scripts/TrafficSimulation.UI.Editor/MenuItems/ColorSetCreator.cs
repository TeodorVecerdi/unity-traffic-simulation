using System.IO;
using TrafficSimulation.UI.Colors;
using UnityEditor;
using UnityEngine;

namespace TrafficSimulation.UI.Editor.MenuItems;

public static class ColorSetCreator {
    [MenuItem("Assets/Create/Traffic Simulation/Color Set")]
    public static void CreateColorSet() {
        var selectedColorPresets = GetSelectedColorPresets();
        if (selectedColorPresets.Count > 0) {
            CreateColorSetWithPresets(selectedColorPresets);
        } else {
            CreateEmptyColorSet();
        }
    }

    private static List<ColorPreset> GetSelectedColorPresets() {
        return Selection.objects
            .OfType<ColorPreset>()
            .Where(preset => preset != null)
            .ToList();
    }

    private static void CreateColorSetWithPresets(List<ColorPreset> colorPresets) {
        var colorSet = ScriptableObject.CreateInstance<ColorSet>();

        // Sort the presets for consistent ordering
        var sortedPresets = colorPresets
            .OrderBy(preset => ExtractBaseName(preset.name))
            .ThenBy(preset => ExtractNumericSuffix(preset.name))
            .ToList();

        colorSet.SetColorPresets(sortedPresets);

        // Generate a meaningful name based on the selected presets
        var setName = GenerateColorSetName(sortedPresets);
        colorSet.DisplayName = setName;

        var path = GetAssetCreationPath();
        var assetPath = AssetDatabase.GenerateUniqueAssetPath($"{path}/{setName}.asset");

        ProjectWindowUtil.CreateAsset(colorSet, assetPath);
    }

    private static void CreateEmptyColorSet() {
        var colorSet = ScriptableObject.CreateInstance<ColorSet>();
        var path = GetAssetCreationPath();
        var assetPath = AssetDatabase.GenerateUniqueAssetPath($"{path}/New Color Set.asset");
        ProjectWindowUtil.CreateAsset(colorSet, assetPath);
    }

    private static string GetAssetCreationPath() {
        // Try to use the path of the first selected asset, or fall back to Assets folder
        var selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);

        if (!string.IsNullOrEmpty(selectedPath)) {
            if (AssetDatabase.IsValidFolder(selectedPath)) {
                return selectedPath;
            }

            return Path.GetDirectoryName(selectedPath) ?? "Assets";
        }

        return "Assets";
    }

    private static string GenerateColorSetName(List<ColorPreset> presets) {
        if (presets.Count == 0)
            return "Empty Color Set";

        if (presets.Count == 1)
            return $"{presets[0].name}";

        // Try to find a common base name (e.g., "amber" from "amber-50", "amber-100", etc.)
        var baseNames = presets
            .Select(preset => ExtractBaseName(preset.name))
            .Distinct()
            .ToList();

        if (baseNames.Count == 1) {
            // All presets share the same base name
            var baseName = baseNames[0];
            return $"{CapitalizeFirstLetter(baseName)} Colors";
        }

        // Mixed base names
        return "Mixed Color Set";
    }

    private static string ExtractBaseName(string presetName) {
        // Extract base name from patterns like "amber-50" -> "amber"
        var parts = presetName.Split('-');
        return parts.Length > 0 ? parts[0] : presetName;
    }

    private static int ExtractNumericSuffix(string presetName) {
        // Extract numeric suffix from patterns like "amber-50" -> 50
        var parts = presetName.Split('-');
        if (parts.Length > 1 && int.TryParse(parts[1], out var number)) {
            return number;
        }

        return 0;
    }

    private static string CapitalizeFirstLetter(string input) {
        if (string.IsNullOrEmpty(input))
            return input;

        return char.ToUpper(input[0]) + input[1..].ToLower();
    }
}
