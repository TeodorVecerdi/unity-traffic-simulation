#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using UnityEditor;

namespace TrafficSimulation.Sim.Visualization.Editor;

[CustomEditor(typeof(VisualizationSettings))]
public sealed class VisualizationSettingsEditor : OdinEditor {
    public override void OnInspectorGUI() {
        var settings = (VisualizationSettings)target;

        GUILayout.Space(10);

        // Preset buttons
        GUILayout.Label("Quick Presets", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Minimal", GUILayout.Height(30))) {
            ApplyPreset(settings, DefaultVisualizationSettings.CreateMinimal());
        }

        if (GUILayout.Button("Default", GUILayout.Height(30))) {
            ApplyPreset(settings, DefaultVisualizationSettings.CreateDefault());
        }

        if (GUILayout.Button("Detailed", GUILayout.Height(30))) {
            ApplyPreset(settings, DefaultVisualizationSettings.CreateDetailed());
        }

        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        // Performance warning
        if (settings is { ShowVehicles: true, ShowVehicleLabels: true, OnlyShowSelectedVehicleDetails: false }) {
            EditorGUILayout.HelpBox(
                "Showing labels for all vehicles can impact performance with many vehicles. " +
                "Consider enabling 'Only Show Selected Vehicle Details' for better performance.",
                MessageType.Warning
            );
        }

        if (settings is { EnableLevelOfDetail: true, MaxDetailDistance: <= 0 }) {
            EditorGUILayout.HelpBox(
                "Level of Detail is enabled but Max Detail Distance is 0. Set a distance value to enable culling.",
                MessageType.Info
            );
        }

        GUILayout.Space(5);

        // Draw default inspector
        base.OnInspectorGUI();

        GUILayout.Space(10);

        // Export buttons
        GUILayout.Label("Asset Management", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Create Default Asset")) {
            CreateSettingsAsset("DefaultVisualization", DefaultVisualizationSettings.CreateDefault());
        }

        if (GUILayout.Button("Create Minimal Asset")) {
            CreateSettingsAsset("MinimalVisualization", DefaultVisualizationSettings.CreateMinimal());
        }

        if (GUILayout.Button("Create Detailed Asset")) {
            CreateSettingsAsset("DetailedVisualization", DefaultVisualizationSettings.CreateDetailed());
        }

        EditorGUILayout.EndHorizontal();

        // Statistics
        GUILayout.Space(10);
        ShowStatistics(settings);
    }

    private void ApplyPreset(VisualizationSettings settings, VisualizationSettings preset) {
        Undo.RecordObject(settings, "Apply Visualization Preset");

        EditorUtility.CopySerialized(preset, settings);

        DestroyImmediate(preset);
        EditorUtility.SetDirty(settings);
    }

    private void CreateSettingsAsset(string baseName, VisualizationSettings settings) {
        var path = EditorUtility.SaveFilePanelInProject(
            "Create Visualization Settings",
            baseName,
            "asset",
            "Choose location for the new visualization settings asset"
        );

        if (!string.IsNullOrEmpty(path)) {
            AssetDatabase.CreateAsset(settings, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = settings;
        } else {
            DestroyImmediate(settings);
        }
    }

    private void ShowStatistics(VisualizationSettings settings) {
        GUILayout.Label("Performance Impact Estimate", EditorStyles.boldLabel);

        var score = CalculatePerformanceScore(settings);
        var color = score > 80 ? Color.red : score > 60 ? Color.yellow : Color.green;
        var label = score > 80 ? "High" : score > 60 ? "Medium" : "Low";

        var originalColor = GUI.color;
        GUI.color = color;
        EditorGUILayout.HelpBox($"Estimated Performance Impact: {label} ({score}/100)",
            score > 80 ? MessageType.Warning : MessageType.Info);
        GUI.color = originalColor;

        // Feature breakdown
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Active Features:", EditorStyles.miniLabel);

        var features = GetActiveFeatures(settings);
        foreach (var feature in features) {
            EditorGUILayout.LabelField("• " + feature, EditorStyles.miniLabel);
        }

        EditorGUILayout.EndVertical();
    }

    private int CalculatePerformanceScore(VisualizationSettings settings) {
        var score = 0;

        // Vehicle features
        if (settings.ShowVehicles) score += 10;
        if (settings is { ShowVehicleLabels: true, OnlyShowSelectedVehicleDetails: false }) score += 20;
        if (settings.ShowVelocityVectors) score += 5;
        if (settings.ShowAccelerationVectors) score += 5;
        if (settings.ShowVehicleGaps) score += 10;
        if (settings.ShowLaneChangeTrajectories) score += 8;
        if (settings.ShowSpeedComparison) score += 5;
        if (settings.ShowBehaviorStates) score += 3;
        if (settings.ShowVehicleSensors) score += 15;

        // Lane features
        if (settings.ShowLanes) score += 5;
        if (settings.ShowLaneLabels) score += 8;
        if (settings.ShowLaneConnections) score += 10;
        if (settings.ShowTrafficDensity) score += 12;

        // Advanced features
        if (settings.ShowFlowMetrics) score += 15;
        if (settings.ShowBottlenecks) score += 20;

        // Performance optimizations (reduce score)
        if (settings.OnlyShowSelectedVehicleDetails) score -= 15;
        if (settings is { EnableLevelOfDetail: true, MaxDetailDistance: > 0 }) score -= 10;

        return Mathf.Clamp(score, 0, 100);
    }

    private string[] GetActiveFeatures(VisualizationSettings settings) {
        var features = new List<string>();

        if (settings.ShowVehicles) {
            features.Add("Vehicle Bodies");
            if (settings.ShowVehicleLabels) features.Add("Vehicle Labels");
            if (settings.ShowVelocityVectors) features.Add("Velocity Vectors");
            if (settings.ShowAccelerationVectors) features.Add("Acceleration Vectors");
            if (settings.ShowVehicleGaps) features.Add("Gap Indicators");
            if (settings.ShowLaneChangeTrajectories) features.Add("Lane Change Paths");
            if (settings.ShowSpeedComparison) features.Add("Speed Comparison");
            if (settings.ShowBehaviorStates) features.Add("Behavior States");
            if (settings.ShowVehicleSensors) features.Add("Vehicle Sensors");
        }

        if (settings.ShowLanes) {
            features.Add("Lane Geometry");
            if (settings.ShowLaneLabels) features.Add("Lane Labels");
            if (settings.ShowLaneConnections) features.Add("Lane Connections");
            if (settings.ShowTrafficDensity) features.Add("Traffic Density");
        }

        if (settings.ShowTrafficLights) {
            features.Add("Traffic Lights");
            if (settings.ShowStopLines) features.Add("Stop Lines");
            if (settings.ShowLightTimings) features.Add("Light Timings");
        }

        if (settings.ShowFlowMetrics) features.Add("Flow Metrics");
        if (settings.ShowBottlenecks) features.Add("Bottleneck Detection");

        if (settings.OnlyShowSelectedVehicleDetails) features.Add("Selection-based LOD");
        if (settings.EnableLevelOfDetail) features.Add("Distance-based LOD");

        return features.ToArray();
    }
}
#endif
