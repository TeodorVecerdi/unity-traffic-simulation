using Microsoft.Extensions.Logging;

namespace TrafficSimulation.Core.Events;

public abstract class PriorityEventBase<TDelegate>(ILogger logger) where TDelegate : Delegate {
    protected readonly List<Handler> Handlers = [];
    protected bool IsInvoking;
    protected event Action? InvokeFinished;

    private bool m_IsOrderDirty;

    public void Subscribe(TDelegate handler, int priority = 0) {
        if (IsInvoking) {
            logger.LogDebug("Subscribing to an event while it is being invoked");
            InvokeFinished += SubscribeOnFinished;

            void SubscribeOnFinished() {
                SubscribeInternal(handler, priority);
                InvokeFinished -= SubscribeOnFinished;
            }

            return;
        }

        SubscribeInternal(handler, priority);
    }

    private void SubscribeInternal(TDelegate handler, int priority) {
        m_IsOrderDirty = m_IsOrderDirty || Handlers.Count > 0 && Handlers[^1].Priority > priority;
        Handlers.Add(new Handler(handler, priority));
    }

    public void Unsubscribe(TDelegate handler) {
        if (IsInvoking) {
            logger.LogDebug("Unsubscribing from an event while it is being invoked");
            InvokeFinished += UnsubscribeOnFinish;

            void UnsubscribeOnFinish() {
                UnsubscribeInternal(handler);
                InvokeFinished -= UnsubscribeOnFinish;
            }

            return;
        }

        UnsubscribeInternal(handler);
    }

    private void UnsubscribeInternal(TDelegate handler) {
        Handlers.RemoveAll(h => h.Delegate.Equals(handler));
    }

    protected void EnsureHandlersAreSorted() {
        if (!m_IsOrderDirty) return;
        Handlers.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        m_IsOrderDirty = false;
    }

    protected readonly struct Handler(TDelegate @delegate, int priority) {
        public readonly TDelegate Delegate = @delegate;
        public readonly int Priority = priority;
    }

    protected void OnInvokeFinished() {
        InvokeFinished?.Invoke();
    }
}
