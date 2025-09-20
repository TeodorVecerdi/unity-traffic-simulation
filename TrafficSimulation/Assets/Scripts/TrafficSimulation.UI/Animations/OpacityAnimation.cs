using LitMotion;
using LitMotion.Extensions;
using TrafficSimulation.Core.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TrafficSimulation.UI.Animations;

[Serializable]
public sealed class OpacityAnimation : IUIAnimation {
    [SerializeField, Required] private CanvasGroup m_CanvasGroup = null!;
    [SerializeField, Range(0.0f, 1.0f)] private float m_TargetOpacity = 1.0f;
    [SerializeField, Range(0.0f, 1.0f)] private float m_OriginalOpacity = 1.0f;

    public async UniTask Play(float duration, CancellationToken cancellationToken) {
        if (m_CanvasGroup == null) return;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, m_CanvasGroup.GetCancellationTokenOnDestroy());
        await LMotion.Create(m_CanvasGroup.alpha, m_TargetOpacity, duration)
            .WithDefaults().BindToAlpha(m_CanvasGroup).CancelOnDestroy(m_CanvasGroup).ToUniTask(cts.Token);
    }

    public async UniTask PlayReverse(float duration, CancellationToken cancellationToken) {
        if (m_CanvasGroup == null) return;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, m_CanvasGroup.GetCancellationTokenOnDestroy());
        await LMotion.Create(m_CanvasGroup.alpha, m_OriginalOpacity, duration)
            .WithDefaults().BindToAlpha(m_CanvasGroup).CancelOnDestroy(m_CanvasGroup).ToUniTask(cts.Token);
    }

    public void Play() {
        if (m_CanvasGroup == null) return;
        m_CanvasGroup.alpha = m_TargetOpacity;
    }

    public void PlayReverse() {
        if (m_CanvasGroup == null) return;
        m_CanvasGroup.alpha = m_OriginalOpacity;
    }
}
