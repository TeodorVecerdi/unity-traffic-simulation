namespace TrafficSimulation.UI.Colors;

public interface IColorAnimatable {
    IColorProperties Color { get; }
    UniTask AnimateColorAsync(IColorProperties targetColor, float duration, CancellationToken cancellationToken);
    void SetColor(IColorProperties color);
}
