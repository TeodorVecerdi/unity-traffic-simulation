using TrafficSimulation.UI.Animations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace TrafficSimulation.UI.Selectables;

[Serializable, InlineProperty, HideLabel, HideReferenceObjectPicker]
public sealed class ButtonAnimations {
    [SerializeReference] private IUIAnimation? m_DefaultAnimation;
    [FormerlySerializedAs("m_HoverAnimation")]
    [SerializeReference] private IUIAnimation? m_HoveredAnimation;
    [FormerlySerializedAs("m_SelectedAnimation")]
    [SerializeReference] private IUIAnimation? m_PressedAnimation;
    [SerializeReference] private IUIAnimation? m_DisabledAnimation;
    [Space(4.0f)]
    [SerializeField, Unit(Units.Second)] private float m_AnimationDuration = 0.15f;

    private readonly AnimationController m_AnimationController = new(true);

    public void SetState(ButtonState state, CancellationToken cancellationToken, AnimationMode animationMode = AnimationMode.Animated) {
        PlayAnimationForState(state, animationMode, m_AnimationDuration, cancellationToken);
    }

    public void ClearReverseAnimation() {
        m_AnimationController.ClearReverseAnimation();
    }

    public void PlayReverseAnimation(CancellationToken cancellationToken, AnimationMode animationMode = AnimationMode.Animated) {
        if (animationMode == AnimationMode.Animated) {
            m_AnimationController.Play(null, m_AnimationDuration, cancellationToken).Forget();
        } else {
            m_AnimationController.Play(null);
        }
    }

    private IUIAnimation? GetAnimationForState(ButtonState state) {
        return state switch {
            ButtonState.Default => m_DefaultAnimation,
            ButtonState.Hovered => m_HoveredAnimation,
            ButtonState.Pressed => m_PressedAnimation,
            ButtonState.Disabled => m_DisabledAnimation,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null),
        };
    }

    private void PlayAnimationForState(ButtonState state, AnimationMode animationMode, float animationDuration, CancellationToken cancellationToken) {
        var animation = GetAnimationForState(state);
        if (animationMode is AnimationMode.Animated) {
            m_AnimationController.Play(animation, animationDuration, cancellationToken).Forget();
        } else {
            m_AnimationController.Play(animation);
        }
    }
}
