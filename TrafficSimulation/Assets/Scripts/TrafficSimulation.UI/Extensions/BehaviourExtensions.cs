using TrafficSimulation.Core;
using TrafficSimulation.Core.UI;
using TrafficSimulation.Core.UI.Interfaces;
using UnityEngine;

namespace TrafficSimulation.UI.Extensions;

public static class BehaviourExtensions {
    public static IFadeable AsFadeable(this Component component, Action<IFadeable>? configure = null) {
        if (component is BaseUIBehaviour uiBehaviour)
            return uiBehaviour.AsFadeable(configure);

        if (component is IFadeable fadeable) {
            configure?.Invoke(fadeable);
            return fadeable;
        }

        var canvasGroup = component as CanvasGroup ?? component.GetOrAddComponent<CanvasGroup>();
        FadeableBehaviour fadeableBehaviour = new(canvasGroup);
        configure?.Invoke(fadeableBehaviour);
        return fadeableBehaviour;
    }

    public static IFadeable AsFadeable(this Component component, bool manageRaycasting) {
        return AsFadeable(component, fadeable => fadeable.ManageRaycasting = manageRaycasting);
    }

    public static IScaleable AsScaleable(this MonoBehaviour monoBehaviour) {
        if (monoBehaviour is BaseUIBehaviour uiBehaviour)
            return uiBehaviour.AsScaleable();
        if (monoBehaviour is IScaleable scaleable)
            return scaleable;
        return new ScaleableBehaviour(monoBehaviour.transform);
    }
}
