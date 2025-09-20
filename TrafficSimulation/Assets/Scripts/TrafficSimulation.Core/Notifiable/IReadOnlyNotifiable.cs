using TrafficSimulation.Core.Events;

namespace TrafficSimulation.Core.Notifiable;

public interface IReadOnlyNotifiable<T> {
    public ISubscribable<T> ValueChanged { get; set; }
    public T Value { get; }
}
