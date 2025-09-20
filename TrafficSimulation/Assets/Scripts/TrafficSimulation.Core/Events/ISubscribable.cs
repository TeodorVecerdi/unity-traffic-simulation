namespace TrafficSimulation.Core.Events;

public interface ISubscribable : ISubscribableBase<ISubscribable, Action> { }
public interface ISubscribable<T> : ISubscribableBase<ISubscribable<T>, Action<T>> { }
public interface ISubscribable<T1, T2> : ISubscribableBase<ISubscribable<T1, T2>, Action<T1, T2>> { }

public interface ISubscribableBase<TSelf, in TDelegate> where TSelf : ISubscribableBase<TSelf, TDelegate> where TDelegate : Delegate {
    public void Subscribe(TDelegate handler, int priority = 0);
    public void Unsubscribe(TDelegate handler);

    public static TSelf operator +(ISubscribableBase<TSelf, TDelegate> @event, TDelegate? handler) {
        if (handler is not null)
            @event.Subscribe(handler);
        return (TSelf)@event;
    }

    public static TSelf operator -(ISubscribableBase<TSelf, TDelegate> @event, TDelegate? handler) {
        if (handler is not null)
            @event.Unsubscribe(handler);
        return (TSelf)@event;
    }
}
