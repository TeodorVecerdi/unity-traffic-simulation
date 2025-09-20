namespace TrafficSimulation.UI.Animations;

public interface IUIAnimation {
    UniTask Play(float duration, CancellationToken cancellationToken);
    UniTask PlayReverse(float duration, CancellationToken cancellationToken);
    void Play();
    void PlayReverse();

    string GetDebugString() {
        return GetType().Name;
    }
}
