using Microsoft.Extensions.Logging;
using Vecerdi.Extensions.Logging;

namespace TrafficSimulation.Core.Events;

public class PriorityEvent(ILogger<PriorityEvent> logger) : PriorityEventBase<Action>(logger), IInvokable, ISubscribable {
    public PriorityEvent() : this(UnityLoggerFactory.CreateLogger<PriorityEvent>()) { }

    public void InvokeEvent() {
        lock (Handlers) {
            try {
                IsInvoking = true;
                EnsureHandlersAreSorted();

                for (var i = 0; i < Handlers.Count; i++) {
                    Handlers[i].Delegate();
                }
            } finally {
                IsInvoking = false;
                OnInvokeFinished();
            }
        }
    }
}

public class PriorityEvent<T>(ILogger<PriorityEvent<T>> logger) : PriorityEventBase<Action<T>>(logger), IInvokable<T>, ISubscribable<T> {
    public PriorityEvent() : this(UnityLoggerFactory.CreateLogger<PriorityEvent<T>>()) { }

    public void InvokeEvent(T argument) {
        lock (Handlers) {
            try {
                IsInvoking = true;
                EnsureHandlersAreSorted();

                for (var i = 0; i < Handlers.Count; i++) {
                    Handlers[i].Delegate(argument);
                }
            } finally {
                IsInvoking = false;
                OnInvokeFinished();
            }
        }
    }
}

public class PriorityEvent<T1, T2>(ILogger<PriorityEvent<T1, T2>> logger) : PriorityEventBase<Action<T1, T2>>(logger), IInvokable<T1, T2>, ISubscribable<T1, T2> {
    public PriorityEvent() : this(UnityLoggerFactory.CreateLogger<PriorityEvent<T1, T2>>()) { }

    public void InvokeEvent(T1 arg1, T2 arg2) {
        lock (Handlers) {
            try {
                IsInvoking = true;
                EnsureHandlersAreSorted();
                for (var i = 0; i < Handlers.Count; i++) {
                    Handlers[i].Delegate(arg1, arg2);
                }
            } finally {
                IsInvoking = false;
                OnInvokeFinished();
            }
        }
    }
}
