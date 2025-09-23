using TrafficSimulation.Sim.Authoring;
using TrafficSimulation.Sim.Visualization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrafficSimulation.Sim.Editor;

public sealed class VisualizationManagerWindow : EditorWindow {
    private Vector2 m_ScrollPosition;
    private bool m_ShowAuthoringComponents = true;
    private bool m_ShowVisualizationComponents = true;
    private bool m_ShowSettings = true;

    // Cached references to avoid FindObjectsByType on every repaint
    private List<SimulationVisualizer> m_CachedVisualizers = [];
    private VehicleAuthoring[] m_CachedVehicles = [];
    private LaneAuthoring[] m_CachedLanes = [];
    private TrafficLightGroupAuthoring[] m_CachedTrafficLights = [];

    [MenuItem("Traffic Simulation/Visualization Manager")]
    public static void ShowWindow() {
        var window = GetWindow<VisualizationManagerWindow>("Visualization Manager");
        window.minSize = new Vector2(400, 500);
        window.Show();
    }

    private void OnEnable() {
        // Populate caches initially
        RefreshCaches();

        // Subscribe to editor events to keep caches up-to-date without querying every repaint
        EditorApplication.hierarchyChanged += OnHierarchyOrSceneChanged;
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorSceneManager.sceneClosed += OnSceneClosed;
        EditorSceneManager.sceneDirtied += OnSceneDirtied;
        Undo.undoRedoPerformed += OnUndoRedoPerformed;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private void OnDisable() {
        // Unsubscribe to avoid leaks
        EditorApplication.hierarchyChanged -= OnHierarchyOrSceneChanged;
        EditorSceneManager.sceneOpened -= OnSceneOpened;
        EditorSceneManager.sceneClosed -= OnSceneClosed;
        EditorSceneManager.sceneDirtied -= OnSceneDirtied;
        Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
    }

    private void OnHierarchyOrSceneChanged() {
        RefreshCaches();
        Repaint();
    }

    private void OnSceneOpened(Scene scene, OpenSceneMode mode) => OnHierarchyOrSceneChanged();
    private void OnSceneClosed(Scene scene) => OnHierarchyOrSceneChanged();
    private void OnSceneDirtied(Scene scene) => Repaint();
    private void OnUndoRedoPerformed() => OnHierarchyOrSceneChanged();
    private void OnPlayModeChanged(PlayModeStateChange state) => OnHierarchyOrSceneChanged();

    private void RefreshCaches() {
        // Visualizers: prefer singleton instance if exists, else search once
        m_CachedVisualizers.Clear();
        if (SimulationVisualizer.InstanceExists) {
            m_CachedVisualizers.Add(SimulationVisualizer.Instance);
        } else {
            m_CachedVisualizers.AddRange(FindObjectsByType<SimulationVisualizer>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        }

        // Authoring components
        m_CachedVehicles = FindObjectsByType<VehicleAuthoring>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        m_CachedLanes = FindObjectsByType<LaneAuthoring>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        m_CachedTrafficLights = FindObjectsByType<TrafficLightGroupAuthoring>(FindObjectsInactive.Include, FindObjectsSortMode.None);
    }

    private void OnGUI() {
        GUILayout.Label("Traffic Simulation - Visualization Manager", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);

        DrawVisualizationComponents();
        EditorGUILayout.Space();

        DrawVisualizationSettings();
        EditorGUILayout.Space();

        DrawAuthoringComponents();
        EditorGUILayout.Space();

        DrawUtilities();

        EditorGUILayout.EndScrollView();
    }

    private void DrawVisualizationComponents() {
        m_ShowVisualizationComponents = EditorGUILayout.Foldout(m_ShowVisualizationComponents, "Visualization Components", true);
        if (!m_ShowVisualizationComponents) return;

        EditorGUI.indentLevel++;

        var visualizers = GetVisualizers();
        if (visualizers.Count == 0) {
            EditorGUILayout.HelpBox("No visualization components found in the scene.", MessageType.Info);

            if (GUILayout.Button("Add Visualizer to Scene")) {
                AddVisualizerToScene();
            }
        } else {
            if (visualizers.Count > 0) {
                GUILayout.Label($"Visualizers ({visualizers.Count})", EditorStyles.boldLabel);

                foreach (var visualizer in visualizers) {
                    if (visualizer == null) continue;
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                    var icon = visualizer.enabled ? EditorGUIUtility.IconContent("TestPassed") : EditorGUIUtility.IconContent("TestFailed");
                    GUILayout.Label(icon, GUILayout.Width(20));

                    EditorGUILayout.ObjectField(visualizer, typeof(SimulationVisualizer), true);

                    if (GUILayout.Button(visualizer.enabled ? "Disable" : "Enable", GUILayout.Width(60))) {
                        Undo.RecordObject(visualizer, "Toggle Visualizer");
                        visualizer.enabled = !visualizer.enabled;
                    }

                    if (GUILayout.Button("Select", GUILayout.Width(60))) {
                        Selection.activeObject = visualizer;
                        EditorGUIUtility.PingObject(visualizer);
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }

            if (visualizers.Count > 1) {
                EditorGUILayout.HelpBox("Multiple visualizers detected. Consider using only one to avoid conflicts.", MessageType.Warning);
            }
        }

        EditorGUI.indentLevel--;
    }

    private void DrawVisualizationSettings() {
        m_ShowSettings = EditorGUILayout.Foldout(m_ShowSettings, "Visualization Settings", true);
        if (!m_ShowSettings) return;

        EditorGUI.indentLevel++;

        // Find all VisualizationSettings assets
        var settingsGuids = AssetDatabase.FindAssets("t:VisualizationSettings");
        var settingsAssets = settingsGuids.Select(guid => AssetDatabase.LoadAssetAtPath<VisualizationSettings>(AssetDatabase.GUIDToAssetPath(guid))).ToArray();

        if (settingsAssets.Length == 0) {
            EditorGUILayout.HelpBox("No VisualizationSettings assets found.", MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create Default Settings")) {
                CreateSettingsAsset("DefaultVisualizationSettings", DefaultVisualizationSettings.CreateDefault());
            }

            if (GUILayout.Button("Create Minimal Settings")) {
                CreateSettingsAsset("MinimalVisualizationSettings", DefaultVisualizationSettings.CreateMinimal());
            }

            if (GUILayout.Button("Create Detailed Settings")) {
                CreateSettingsAsset("DetailedVisualizationSettings", DefaultVisualizationSettings.CreateDetailed());
            }

            EditorGUILayout.EndHorizontal();
        } else {
            var visualizers = GetVisualizers();
            foreach (var settings in settingsAssets) {
                if (settings == null) continue;

                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                var icon = visualizers.Any(visualizer => visualizer != null && visualizer.Settings == settings) ? EditorGUIUtility.IconContent("TestPassed") : EditorGUIUtility.IconContent("TestFailed");
                GUILayout.Label(icon, GUILayout.Width(20));

                EditorGUILayout.ObjectField(settings, typeof(VisualizationSettings), false);

                if (GUILayout.Button("Apply", GUILayout.Width(60))) {
                    ApplySettingsToScene(settings);
                }

                if (GUILayout.Button("Select", GUILayout.Width(60))) {
                    Selection.activeObject = settings;
                    EditorGUIUtility.PingObject(settings);
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUI.indentLevel--;
    }

    private void DrawAuthoringComponents() {
        m_ShowAuthoringComponents = EditorGUILayout.Foldout(m_ShowAuthoringComponents, "Authoring Components", true);
        if (!m_ShowAuthoringComponents) return;

        EditorGUI.indentLevel++;

        // Use cached arrays to avoid per-repaint queries
        var vehicles = m_CachedVehicles;
        var lanes = m_CachedLanes;
        var trafficLights = m_CachedTrafficLights;

        GUILayout.Label("Scene Components:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"• Vehicles: {vehicles.Length}");
        EditorGUILayout.LabelField($"• Lanes: {lanes.Length}");
        EditorGUILayout.LabelField($"• Traffic Light Groups: {trafficLights.Length}");

        EditorGUILayout.Space();

        // Authoring visualization controls
        GUILayout.Label("Authoring Visualization Controls:", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Enable All Authoring Gizmos")) {
            SetAuthoringGizmosState(true);
        }

        if (GUILayout.Button("Disable All Authoring Gizmos")) {
            SetAuthoringGizmosState(false);
        }

        EditorGUILayout.EndHorizontal();

        EditorGUI.indentLevel--;
    }

    private void DrawUtilities() {
        GUILayout.Label("Utilities", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        if (GUILayout.Button("Refresh Scene Analysis")) {
            RefreshCaches();
            Repaint();
        }

        EditorGUILayout.EndVertical();
    }

    private void AddVisualizerToScene() {
        var controller = FindFirstObjectByType<TrafficSimulationController>();
        if (controller == null) {
            EditorUtility.DisplayDialog("Error", "No TrafficSimulationController found in the scene. Add one first.", "OK");
            return;
        }

        var visualizer = controller.gameObject.GetComponent<SimulationVisualizer>();
        if (visualizer == null) {
            visualizer = Undo.AddComponent<SimulationVisualizer>(controller.gameObject);

            // Try to find and assign default settings
            var defaultSettings = AssetDatabase.FindAssets("t:VisualizationSettings")
                .Select(guid => AssetDatabase.LoadAssetAtPath<VisualizationSettings>(AssetDatabase.GUIDToAssetPath(guid)))
                .FirstOrDefault(s => s.name.Contains("Default"));

            if (defaultSettings == null) {
                // Create default settings if none exist
                var settings = DefaultVisualizationSettings.CreateDefault();
                var path = "Assets/DefaultVisualizationSettings.asset";
                AssetDatabase.CreateAsset(settings, path);
                AssetDatabase.SaveAssets();
                defaultSettings = settings;
            }

            visualizer.Settings = defaultSettings;
            EditorUtility.SetDirty(visualizer);
        }

        // Update cache and UI
        RefreshCaches();
        Selection.activeObject = visualizer;
        EditorGUIUtility.PingObject(visualizer);
    }

    private void CreateSettingsAsset(string assetName, VisualizationSettings settings) {
        var path = $"Assets/{assetName}.asset";
        path = AssetDatabase.GenerateUniqueAssetPath(path);

        AssetDatabase.CreateAsset(settings, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = settings;
        EditorGUIUtility.PingObject(settings);
    }

    private void ApplySettingsToScene(VisualizationSettings settings) {
        var visualizers = GetVisualizers();
        if (visualizers.Count == 0) {
            EditorUtility.DisplayDialog("Info", "No Simulation Visualizers found in the scene.", "OK");
            return;
        }

        var undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply Visualization Settings");
        foreach (var visualizer in visualizers) {
            if (visualizer == null) continue;
            Undo.RecordObject(visualizer, "Apply Visualization Settings");
            visualizer.Settings = settings;
            EditorUtility.SetDirty(visualizer);
        }

        Undo.CollapseUndoOperations(undoGroup);
        if (!Application.isPlaying)
            EditorSceneManager.MarkAllScenesDirty();

        RefreshCaches();
        Repaint();
    }

    private void SetAuthoringGizmosState(bool enabled) {
        var undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(enabled ? "Enable Authoring Gizmos" : "Disable Authoring Gizmos");

        // Vehicles
        var vehicles = FindObjectsByType<VehicleAuthoring>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var vehicle in vehicles) {
            Undo.RecordObject(vehicle, "Toggle Authoring Gizmos");
            vehicle.AlwaysDrawGizmos = enabled;
            EditorUtility.SetDirty(vehicle);
        }

        // Lanes
        var lanes = FindObjectsByType<LaneAuthoring>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var lane in lanes) {
            Undo.RecordObject(lane, "Toggle Authoring Gizmos");
            lane.AlwaysDrawGizmos = enabled;
            EditorUtility.SetDirty(lane);
        }

        // Traffic Lights
        var trafficLights = FindObjectsByType<TrafficLightGroupAuthoring>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var trafficLight in trafficLights) {
            Undo.RecordObject(trafficLight, "Toggle Authoring Gizmos");
            trafficLight.DrawGizmos = enabled;
            EditorUtility.SetDirty(trafficLight);
        }

        Undo.CollapseUndoOperations(undoGroup);
        if (!Application.isPlaying)
            EditorSceneManager.MarkAllScenesDirty();

        RefreshCaches();
        Repaint();
    }

    private List<SimulationVisualizer> GetVisualizers() {
        // Ensure cache is initialized
        if (m_CachedVisualizers == null!)
            m_CachedVisualizers = [];
        return m_CachedVisualizers;
    }
}
