namespace TrafficSimulation.Core.Async;

public sealed class AsyncHandler {
    public bool IsActive => m_Cts is not null;

    private CancellationTokenSource? m_Cts;

    public Scope Create() => new(this);
    public Scope Create(CancellationToken token) => new(this, token);
    public Scope Create(CancellationToken token1, CancellationToken token2) => new(this, token1, token2);

    public void Cancel() {
        m_Cts?.Cancel();
    }

    public class Scope : IDisposable {
        public CancellationToken Token => m_LinkedCts.Token;

        private readonly AsyncHandler m_Handler;
        private readonly CancellationTokenSource m_AnimationCts;
        private readonly CancellationTokenSource m_LinkedCts;

        public Scope(AsyncHandler handler) {
            m_Handler = handler;
            m_LinkedCts = m_AnimationCts = new CancellationTokenSource();
            m_Handler.m_Cts?.Cancel();
            m_Handler.m_Cts = m_AnimationCts;
        }

        public Scope(AsyncHandler handler, CancellationToken token) {
            m_Handler = handler;

            m_AnimationCts = new CancellationTokenSource();
            m_LinkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, m_AnimationCts.Token);
            m_Handler.m_Cts?.Cancel();
            m_Handler.m_Cts = m_AnimationCts;
        }

        public Scope(AsyncHandler handler, CancellationToken token1, CancellationToken token2) {
            m_Handler = handler;

            m_AnimationCts = new CancellationTokenSource();
            m_LinkedCts = CancellationTokenSource.CreateLinkedTokenSource(token1, token2, m_AnimationCts.Token);
            m_Handler.m_Cts?.Cancel();
            m_Handler.m_Cts = m_AnimationCts;
        }

        public void Dispose() {
            if (m_Handler.m_Cts == m_AnimationCts) {
                m_Handler.m_Cts = null;
            }

            if (m_LinkedCts != m_AnimationCts) {
                m_LinkedCts.Dispose();
            }

            m_AnimationCts.Dispose();
        }
    }
}
