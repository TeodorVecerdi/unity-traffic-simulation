using System.Diagnostics.CodeAnalysis;
using TrafficSimulation.UI.Colors;
using Microsoft.Extensions.Logging;
using Sirenix.OdinInspector;
using UnityEngine;
using Vecerdi.Extensions.Logging;

namespace TrafficSimulation.UI.Animations;

[Serializable]
public sealed class ColorAnimation : IUIAnimation {
    [field: MaybeNull]
    private ILogger<ColorAnimation> Logger => field ??= UnityLoggerFactory.CreateLogger<ColorAnimation>();

    [ShowInInspector, Required, PropertyOrder(-1)]
    private IColorAnimatable Animatable {
        get => (IColorAnimatable?)m_AnimatableObject.OrNull() ?? m_Animatable;
        set {
            if (value is Object unityObject) {
                m_AnimatableObject = unityObject;
                m_Animatable = null!;
            } else {
                m_AnimatableObject = null!;
                m_Animatable = value;
            }
        }
    }

    [SerializeField] private bool m_EnableOriginalProperties;

    [Title("Target Color")]
    [SerializeField, Required] private ColorComponentProperties m_TargetProperties = null!;
    [ShowIf(nameof(m_EnableOriginalProperties))]
    [Title("Original Color")]
    [SerializeField, Required] private ColorComponentProperties m_OriginalProperties = null!;

    [HideInInspector, SerializeReference] private IColorAnimatable m_Animatable = null!;
    [HideInInspector, SerializeField] private Object m_AnimatableObject = null!;

    private IColorProperties? m_StoredOriginalProperties;

    public UniTask Play(float duration, CancellationToken cancellationToken) {
        var animatable = Animatable;

        if (!m_EnableOriginalProperties) {
            m_StoredOriginalProperties = ColorProperties.Create(animatable.Color);
        }

        return animatable.AnimateColorAsync(m_TargetProperties, duration, cancellationToken);
    }

    public UniTask PlayReverse(float duration, CancellationToken cancellationToken) {
        var animatable = Animatable;

        var originalProperties = m_EnableOriginalProperties ? m_OriginalProperties : m_StoredOriginalProperties;
        if (originalProperties is not null) {
            return animatable.AnimateColorAsync(originalProperties, duration, cancellationToken);
        }

        Logger.LogWarning("Original properties are null, cannot play reverse animation.");
        return UniTask.CompletedTask;
    }

    public void Play() {
        var animatable = Animatable;

        if (!m_EnableOriginalProperties) {
            m_StoredOriginalProperties = ColorProperties.Create(animatable.Color);
        }

        animatable.SetColor(m_TargetProperties);
    }

    public void PlayReverse() {
        var animatable = Animatable;

        var originalProperties = m_EnableOriginalProperties ? m_OriginalProperties : m_StoredOriginalProperties;
        if (originalProperties is not null) {
            animatable.SetColor(originalProperties);
        } else {
            Logger.LogWarning("Original properties are null, cannot play reverse animation.");
        }
    }
}
