using LitMotion;
using LitMotion.Extensions;
using TrafficSimulation.Core.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TrafficSimulation.UI.Animations;

[Serializable]
public sealed class AnchoredPositionAnimation : IUIAnimation {
    [SerializeField, Required] private RectTransform m_RectTransform = null!;
    [SerializeField] private Vector2 m_TargetPosition = Vector2.zero;
    [SerializeField] private Vector2 m_OriginalPosition = Vector2.zero;

    public async UniTask Play(float duration, CancellationToken cancellationToken) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, m_RectTransform.GetCancellationTokenOnDestroy());
        await LMotion.Create(m_RectTransform.anchoredPosition, m_TargetPosition, duration)
            .WithDefaults().BindToAnchoredPosition(m_RectTransform).CancelOnDestroy(m_RectTransform).ToUniTask(cts.Token);
    }

    public async UniTask PlayReverse(float duration, CancellationToken cancellationToken) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, m_RectTransform.GetCancellationTokenOnDestroy());
        await LMotion.Create(m_RectTransform.anchoredPosition, m_OriginalPosition, duration)
            .WithDefaults().BindToAnchoredPosition(m_RectTransform).CancelOnDestroy(m_RectTransform).ToUniTask(cts.Token);
    }

    public void Play() {
        if (m_RectTransform == null) return;
        m_RectTransform.anchoredPosition = m_TargetPosition;
    }

    public void PlayReverse() {
        if (m_RectTransform == null) return;
        m_RectTransform.anchoredPosition = m_OriginalPosition;
    }
}
