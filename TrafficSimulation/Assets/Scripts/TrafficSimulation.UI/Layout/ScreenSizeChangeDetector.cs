using UnityEngine;
using Vecerdi.Extensions.DependencyInjection;

namespace TrafficSimulation.UI.Layout;

[ExecuteAlways]
public sealed class ScreenSizeChangeDetector : MonoSingleton<ScreenSizeChangeDetector> {
    static ScreenSizeChangeDetector() => MissingInstanceBehavior = MissingSingletonInstanceBehavior.Throw;
    public event Action? ScreenSizeChanged;

    private void OnRectTransformDimensionsChange() {
        ScreenSizeChanged?.Invoke();
    }
}
