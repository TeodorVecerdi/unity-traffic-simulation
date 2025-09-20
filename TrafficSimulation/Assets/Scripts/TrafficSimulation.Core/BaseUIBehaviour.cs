using TrafficSimulation.Core.UI;
using TrafficSimulation.Core.UI.Interfaces;
using UnityEngine;
using Vecerdi.Extensions.DependencyInjection;

namespace TrafficSimulation.Core;

public class BaseUIBehaviour : BaseMonoBehaviour {
    private IFadeable? m_AsFadeable;
    private IScaleable? m_AsScaleable;

    public IFadeable AsFadeable(Action<IFadeable>? configure = null) {
        // Return self if it implements IFadeable
        if (this is IFadeable fadeable) {
            configure?.Invoke(fadeable);
            return fadeable;
        }

        // Return cached instance if it exists and is valid
        if (m_AsFadeable is { IsValid: true }) {
            configure?.Invoke(m_AsFadeable);
            return m_AsFadeable;
        }

        // Create new instance
        var canvasGroup = this.GetOrAddComponent<CanvasGroup>();
        m_AsFadeable = new FadeableBehaviour(canvasGroup);
        configure?.Invoke(m_AsFadeable);
        return m_AsFadeable;
    }

    public IFadeable AsFadeable(bool manageRaycasting) {
        return AsFadeable(fadeable => fadeable.ManageRaycasting = manageRaycasting);
    }

    public IScaleable AsScaleable() {
        // Return self if it implements IScaleable
        if (this is IScaleable scaleable)
            return scaleable;

        // Return cached instance if it exists and is valid
        if (m_AsScaleable is { IsValid: true })
            return m_AsScaleable;

        // Create new instance
        return m_AsScaleable = new ScaleableBehaviour(transform);
    }
}
