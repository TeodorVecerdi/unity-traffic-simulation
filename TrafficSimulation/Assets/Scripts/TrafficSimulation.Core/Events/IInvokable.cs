namespace TrafficSimulation.Core.Events;

public interface IInvokable {
    void InvokeEvent();
}

public interface IInvokable<in T> {
    void InvokeEvent(T arg);
}

public interface IInvokable<in T1, in T2> {
    void InvokeEvent(T1 arg1, T2 arg2);
}
