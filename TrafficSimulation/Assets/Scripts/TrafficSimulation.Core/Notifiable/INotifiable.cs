namespace TrafficSimulation.Core.Notifiable;

public interface INotifiable<T> : IReadOnlyNotifiable<T> {
    public new T Value { get; set; }
    public void SetValueWithoutNotify(T value);
}
