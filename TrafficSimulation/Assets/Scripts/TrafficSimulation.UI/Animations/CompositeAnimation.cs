using UnityEngine;

namespace TrafficSimulation.UI.Animations;

[Serializable]
public sealed class CompositeAnimation : IUIAnimation {
    [SerializeReference] private List<IUIAnimation> m_Animations = new();

    public UniTask Play(float duration, CancellationToken cancellationToken) {
        return UniTask.WhenAll(m_Animations.Select(animation => animation.Play(duration, cancellationToken)));
    }

    public UniTask PlayReverse(float duration, CancellationToken cancellationToken) {
        return UniTask.WhenAll(m_Animations.Select(animation => animation.PlayReverse(duration, cancellationToken)));
    }

    public void Play() {
        foreach (var animation in m_Animations) {
            animation.Play();
        }
    }

    public void PlayReverse() {
        foreach (var animation in m_Animations) {
            animation.PlayReverse();
        }
    }

    public string GetDebugString() {
        return $"CompositeAnimation[{string.Join(", ", m_Animations.Select(animation => animation.GetDebugString()))}]";
    }
}
