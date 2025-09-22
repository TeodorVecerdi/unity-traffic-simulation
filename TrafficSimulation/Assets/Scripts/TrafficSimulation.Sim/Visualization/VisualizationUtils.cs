namespace TrafficSimulation.Sim.Visualization;

public static class VisualizationUtils {
    public static bool ShouldSkipAuthoringVisualization() {
        if (!Application.isPlaying)
            return false;
        if (SimulationVisualizer.InstanceExists && SimulationVisualizer.Instance.enabled)
            return true;
        return false;
    }
}
