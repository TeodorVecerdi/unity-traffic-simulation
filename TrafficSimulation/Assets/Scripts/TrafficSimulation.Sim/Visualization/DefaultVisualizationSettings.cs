namespace TrafficSimulation.Sim.Visualization;

public static class DefaultVisualizationSettings {
    public static VisualizationSettings CreateDefault() {
        var settings = ScriptableObject.CreateInstance<VisualizationSettings>();

        // Vehicle Visualization
        settings.ShowVehicles = true;
        settings.ShowVehicleLabels = true;
        settings.ShowVelocityVectors = true;
        settings.ShowAccelerationVectors = true;
        settings.ShowVehicleGaps = true;
        settings.ShowLaneChangeTrajectories = true;
        settings.ShowSpeedComparison = false;
        settings.ShowBehaviorStates = true;

        // Vehicle Appearance
        settings.VehicleWidth = 2.0f;
        settings.VehicleBodyColor = new Color(0.2f, 0.6f, 1.0f, 0.8f);
        settings.VehicleBorderColor = Color.white;
        settings.VelocityVectorScale = 2.0f;
        settings.AccelerationVectorScale = 1.5f;

        // Lane Visualization
        settings.ShowLanes = true;
        settings.ShowLaneLabels = false;
        settings.ShowLaneConnections = false;
        settings.ShowTrafficDensity = false;
        settings.LaneColor = new Color(1.0f, 1.0f, 0.0f, 0.3f);
        settings.LaneConnectionColor = new Color(0.0f, 1.0f, 1.0f, 0.5f);

        // Traffic Light Visualization
        settings.ShowTrafficLights = true;
        settings.ShowStopLines = true;
        settings.ShowLightTimings = false;
        settings.StopLineWidth = 3.0f;

        // Performance
        settings.OnlyShowSelectedVehicleDetails = false;
        settings.MaxDetailDistance = 0.0f;
        settings.EnableLevelOfDetail = false;

        // Color Coding
        settings.VelocityColor = Color.green;
        settings.AccelerationPositiveColor = Color.green;
        settings.AccelerationNegativeColor = Color.red;
        settings.GapLineColor = new Color(1.0f, 1.0f, 0.0f, 0.6f);
        settings.LaneChangeColor = new Color(1.0f, 0.0f, 1.0f, 0.8f);

        // Advanced Features
        settings.ShowVehicleSensors = false;
        settings.ShowFlowMetrics = false;
        settings.ShowBottlenecks = false;

        return settings;
    }

    public static VisualizationSettings CreateMinimal() {
        var settings = ScriptableObject.CreateInstance<VisualizationSettings>();

        // Vehicle Visualization - minimal
        settings.ShowVehicles = true;
        settings.ShowVehicleLabels = false;
        settings.ShowVelocityVectors = false;
        settings.ShowAccelerationVectors = false;
        settings.ShowVehicleGaps = false;
        settings.ShowLaneChangeTrajectories = true;
        settings.ShowSpeedComparison = false;
        settings.ShowBehaviorStates = false;

        // Vehicle Appearance
        settings.VehicleWidth = 2.0f;
        settings.VehicleBodyColor = new Color(0.3f, 0.7f, 1.0f, 0.8f);
        settings.VehicleBorderColor = Color.white;

        // Lane Visualization - minimal
        settings.ShowLanes = true;
        settings.ShowLaneLabels = false;
        settings.ShowLaneConnections = false;
        settings.ShowTrafficDensity = false;
        settings.LaneColor = new Color(0.8f, 0.8f, 0.8f, 0.3f);

        // Traffic Light Visualization
        settings.ShowTrafficLights = true;
        settings.ShowStopLines = true;
        settings.ShowLightTimings = false;
        settings.StopLineWidth = 2.0f;

        // Performance optimized
        settings.OnlyShowSelectedVehicleDetails = true;
        settings.MaxDetailDistance = 100.0f;
        settings.EnableLevelOfDetail = true;

        return settings;
    }

    public static VisualizationSettings CreateDetailed() {
        var settings = ScriptableObject.CreateInstance<VisualizationSettings>();

        // Vehicle Visualization - all features enabled
        settings.ShowVehicles = true;
        settings.ShowVehicleLabels = true;
        settings.ShowVelocityVectors = true;
        settings.ShowAccelerationVectors = true;
        settings.ShowVehicleGaps = true;
        settings.ShowLaneChangeTrajectories = true;
        settings.ShowSpeedComparison = true;
        settings.ShowBehaviorStates = true;

        // Vehicle Appearance
        settings.VehicleWidth = 2.0f;
        settings.VehicleBodyColor = new Color(0.2f, 0.6f, 1.0f, 0.8f);
        settings.VehicleBorderColor = Color.white;
        settings.VelocityVectorScale = 2.5f;
        settings.AccelerationVectorScale = 2.0f;

        // Lane Visualization - detailed
        settings.ShowLanes = true;
        settings.ShowLaneLabels = true;
        settings.ShowLaneConnections = true;
        settings.ShowTrafficDensity = true;
        settings.LaneColor = new Color(1.0f, 1.0f, 0.0f, 0.4f);
        settings.LaneConnectionColor = new Color(0.0f, 1.0f, 1.0f, 0.7f);

        // Traffic Light Visualization
        settings.ShowTrafficLights = true;
        settings.ShowStopLines = true;
        settings.ShowLightTimings = true;
        settings.StopLineWidth = 4.0f;

        // Performance
        settings.OnlyShowSelectedVehicleDetails = false;
        settings.MaxDetailDistance = 0.0f;
        settings.EnableLevelOfDetail = false;

        // Color Coding - vibrant
        settings.VelocityColor = new Color(0.0f, 1.0f, 0.2f);
        settings.AccelerationPositiveColor = new Color(0.0f, 1.0f, 0.0f);
        settings.AccelerationNegativeColor = new Color(1.0f, 0.2f, 0.0f);
        settings.GapLineColor = new Color(1.0f, 0.8f, 0.0f, 0.8f);
        settings.LaneChangeColor = new Color(1.0f, 0.0f, 1.0f, 1.0f);

        // Advanced Features
        settings.ShowVehicleSensors = true;
        settings.ShowFlowMetrics = true;
        settings.ShowBottlenecks = true;

        return settings;
    }
}
