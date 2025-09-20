using TrafficSimulation.UI.Animations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace TrafficSimulation.UI.Selectables;

[Serializable, InlineProperty, HideLabel, HideReferenceObjectPicker]
public sealed class SwitchAnimations {
    [Title("Animations")]
    [FormerlySerializedAs("m_DeselectedAnimation")]
    [SerializeReference] private IUIAnimation? m_Deselected;
    [FormerlySerializedAs("m_SelectedAnimation")]
    [SerializeReference] private IUIAnimation? m_Selected;
    [FormerlySerializedAs("m_DisabledAnimation")]
    [SerializeReference] private IUIAnimation? m_Disabled;
    [Space(4.0f)]
    [FormerlySerializedAs("m_HoverAnimation")]
    [LabelText("@" + nameof(m_UseSameHoverAnimation) + " ? \"Hovered\" : \"Hovered While Deselected\"")]
    [SerializeReference] private IUIAnimation? m_HoveredWhileDeselected;
    [HideIf(nameof(m_UseSameHoverAnimation))]
    [SerializeReference] private IUIAnimation? m_HoveredWhileSelected;
    [Space(6.0f)]
    [SerializeField] private bool m_UseSameHoverAnimation = true;
    [SerializeField, Unit(Units.Second)] private float m_AnimationDuration = 0.15f;

    private readonly AnimationController m_AnimationController = new(true);
    private SwitchState m_PreviousState = (SwitchState)(-1);

    public void SetState(SwitchState state, bool setImplicitHover, CancellationToken cancellationToken, AnimationMode animationMode = AnimationMode.Animated) {
        SetStateImpl(state, animationMode, setImplicitHover, m_AnimationDuration, cancellationToken);
    }

    public void ClearReverseAnimation() {
        m_AnimationController.ClearReverseAnimation();
    }

    public void PlayReverseAnimationForState(SwitchState state, CancellationToken cancellationToken, AnimationMode animationMode = AnimationMode.Animated) {
        var animation = GetAnimationForState(state);

        if (animationMode is AnimationMode.Animated) {
            animation?.PlayReverse(m_AnimationDuration, cancellationToken).Forget();
        } else {
            animation?.PlayReverse();
        }
    }

    public void PlayReverseAnimation(CancellationToken cancellationToken, AnimationMode animationMode = AnimationMode.Animated) {
        if (animationMode is AnimationMode.Animated) {
            m_AnimationController.Play(null, m_AnimationDuration, cancellationToken).Forget();
        } else {
            m_AnimationController.Play(null);
        }
    }

    private IUIAnimation? GetAnimationForState(SwitchState state) {
        return state switch {
            SwitchState.Deselected => m_Deselected,
            SwitchState.Selected => m_Selected,
            SwitchState.Disabled => m_Disabled,
            SwitchState.HoveredWhileDeselected => m_HoveredWhileDeselected,
            SwitchState.HoveredWhileSelected => m_UseSameHoverAnimation ? m_HoveredWhileDeselected : m_HoveredWhileSelected,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null),
        };
    }

    private void SetStateImpl(SwitchState state, AnimationMode animationMode, bool setImplicitHover, float animationDuration, CancellationToken cancellationToken) {
        switch (state) {
            case SwitchState.Deselected:
                PlayAnimationForState(SwitchState.Deselected, animationMode, animationDuration, cancellationToken, m_PreviousState is SwitchState.HoveredWhileSelected && setImplicitHover);

                // If we're also hovered when deselecting (the most common case), play the hovered animation (hoping that it doesn't interfere)
                // HoveredWhileSelected (+ implicit Selected) -> Deselected + Hovered -> HoveredWhileDeselected
                if (setImplicitHover) {
                    PlayAnimationForStateDirect(SwitchState.HoveredWhileDeselected, animationMode, animationDuration, cancellationToken);
                    state = SwitchState.HoveredWhileDeselected;
                }

                break;
            case SwitchState.Selected:
                PlayAnimationForState(SwitchState.Selected, animationMode, animationDuration, cancellationToken, m_PreviousState is SwitchState.HoveredWhileDeselected && setImplicitHover);

                // If we're also hovered when selecting (the most common case), play the hovered animation (hoping that it doesn't interfere)
                // HoveredWhileDeselected -> Selected + Hovered -> HoveredWhileSelected
                if (setImplicitHover) {
                    PlayAnimationForStateDirect(SwitchState.HoveredWhileSelected, animationMode, animationDuration, cancellationToken);
                    state = SwitchState.HoveredWhileSelected;
                }

                break;
            case SwitchState.Disabled:
                PlayAnimationForState(SwitchState.Disabled, animationMode, animationDuration, cancellationToken);
                break;
            case SwitchState.HoveredWhileDeselected:
                PlayAnimationForState(SwitchState.HoveredWhileDeselected, animationMode, animationDuration, cancellationToken);
                break;
            case SwitchState.HoveredWhileSelected:
                PlayAnimationForState(SwitchState.HoveredWhileSelected, animationMode, animationDuration, cancellationToken);
                break;
            default: throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }

        HandleTransition(state);
        m_PreviousState = state;
    }

    private void HandleTransition(SwitchState state) {
        switch (m_PreviousState, state) {
            // Clear the reverse animation when transitioning from:
            // hovered while deselected -> deselected
            case (SwitchState.HoveredWhileDeselected, SwitchState.Deselected):
            // hovered while selected -> selected
            case (SwitchState.HoveredWhileSelected, SwitchState.Selected):
                m_AnimationController.ClearReverseAnimation();
                break;
        }
    }

    private void PlayAnimationForState(SwitchState state, AnimationMode animationMode, float animationDuration, CancellationToken cancellationToken, bool skipReverse = false) {
        var animation = GetAnimationForState(state);
        if (animationMode is AnimationMode.Animated) {
            m_AnimationController.Play(animation, animationDuration, cancellationToken, skipReverse).Forget();
        } else {
            m_AnimationController.Play(animation, skipReverse);
        }
    }

    private void PlayAnimationForStateDirect(SwitchState state, AnimationMode animationMode, float animationDuration, CancellationToken cancellationToken, bool skipReverse = false) {
        var animation = GetAnimationForState(state);
        if (animationMode is AnimationMode.Animated) {
            animation?.Play(animationDuration, cancellationToken).Forget();
        } else {
            animation?.Play();
        }

        if (!skipReverse) {
            m_AnimationController.SetReverseAnimation(animation, animationDuration);
        }
    }
}
