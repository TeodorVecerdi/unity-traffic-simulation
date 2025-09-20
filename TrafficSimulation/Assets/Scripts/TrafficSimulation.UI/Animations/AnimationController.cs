using TrafficSimulation.Core.Async;
using TrafficSimulation.Core.Tweening;

namespace TrafficSimulation.UI.Animations;

public sealed class AnimationController(bool playReverseAnimations) {
    private readonly AsyncHandler m_AnimationHandler = new();
    private IUIAnimation? m_LastAnimation;
    private float m_LastAnimationDuration;

    public async UniTask Play(IUIAnimation? animation, float duration, CancellationToken cancellationToken, bool skipReverse = false) {
        using var scope = m_AnimationHandler.Create(cancellationToken);
        if (playReverseAnimations && !skipReverse && m_LastAnimation is not null && m_LastAnimation != animation) {
            var lastAnimation = m_LastAnimation;
            var lastAnimationDuration = m_LastAnimationDuration;
            if (MathF.Abs(lastAnimationDuration) <= 0.001f)
                lastAnimationDuration = duration;
            m_LastAnimation = animation;
            m_LastAnimationDuration = duration;

            if (animation is null) {
                await lastAnimation.PlayReverse(lastAnimationDuration, cancellationToken);
            } else {
                await PlayWithReverse(lastAnimation, lastAnimationDuration, animation, duration, scope.Token);
            }

            return;
        }

        if (animation is null) return;
        m_LastAnimation = animation;
        m_LastAnimationDuration = duration;
        await animation.Play(duration, scope.Token);
    }

    public void Play(IUIAnimation? animation, bool skipReverse = false) {
        if (playReverseAnimations && !skipReverse && m_LastAnimation is not null && m_LastAnimation != animation) {
            m_LastAnimation.PlayReverse();
        }

        animation?.Play();

        m_LastAnimation = animation;
        m_LastAnimationDuration = 0.0f;
    }

    public void SetReverseAnimation(IUIAnimation? animation, float duration) {
        m_LastAnimation = animation;
        m_LastAnimationDuration = animation is not null ? duration : 0.0f;
    }

    public void ClearReverseAnimation() {
        m_LastAnimation = null;
        m_LastAnimationDuration = 0.0f;
    }

    public void Cancel() {
        m_AnimationHandler.Cancel();
    }

    private static UniTask PlayWithReverse(IUIAnimation lastAnimation, float lastAnimationDuration, IUIAnimation animation, float duration, CancellationToken cancellationToken) {
        return UniTask.WhenAll(
            lastAnimation.PlayReverse(lastAnimationDuration, cancellationToken),
            animation.Play(duration, cancellationToken)
        );
    }
}
