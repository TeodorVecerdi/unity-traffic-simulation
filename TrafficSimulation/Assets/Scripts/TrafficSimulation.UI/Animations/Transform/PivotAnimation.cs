using LitMotion;
using LitMotion.Extensions;
using TrafficSimulation.Core.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TrafficSimulation.UI.Animations;

[Serializable]
public sealed class PivotAnimation : IUIAnimation {
    [SerializeField, Required] private RectTransform m_RectTransform = null!;
    [SerializeField] private Vector2 m_TargetPivot = Vector2.zero;
    [SerializeField] private Vector2 m_OriginalPivot = Vector2.zero;

    public async UniTask Play(float duration, CancellationToken cancellationToken) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, m_RectTransform.GetCancellationTokenOnDestroy());
        await LMotion.Create(m_RectTransform.pivot, m_TargetPivot, duration)
            .WithDefaults().BindToPivot(m_RectTransform).CancelOnDestroy(m_RectTransform).ToUniTask(cts.Token);
    }

    public async UniTask PlayReverse(float duration, CancellationToken cancellationToken) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, m_RectTransform.GetCancellationTokenOnDestroy());
        await LMotion.Create(m_RectTransform.pivot, m_OriginalPivot, duration)
            .WithDefaults().BindToPivot(m_RectTransform).CancelOnDestroy(m_RectTransform).ToUniTask(cts.Token);
    }

    public void Play() {
        if (m_RectTransform == null) return;
        m_RectTransform.pivot = m_TargetPivot;
    }

    public void PlayReverse() {
        if (m_RectTransform == null) return;
        m_RectTransform.pivot = m_OriginalPivot;
    }
}
