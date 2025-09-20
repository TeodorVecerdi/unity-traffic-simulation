using TrafficSimulation.Core.Events;

namespace TrafficSimulation.Core.Notifiable;

public class Notifiable<T>(T value) : INotifiable<T> {
    private readonly PriorityEvent<T> m_ValueChanged = new();
    private T m_Value = value;

    public ISubscribable<T> ValueChanged {
        get => m_ValueChanged;
        set { }
    }

    public T Value {
        get => m_Value;
        set {
            if (m_Value?.Equals(value) ?? value is null) {
                return;
            }

            SetValueWithoutNotify(value);
            m_ValueChanged.InvokeEvent(value);
        }
    }

    public void SetValueWithoutNotify(T value) => m_Value = value;

    public void SetValueAndForceNotify(T value) {
        SetValueWithoutNotify(value);
        m_ValueChanged.InvokeEvent(value);
    }
}
