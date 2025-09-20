using LitMotion;
using LitMotion.Extensions;
using TrafficSimulation.Core;
using TrafficSimulation.Core.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TrafficSimulation.UI.Animations;

[Serializable]
public sealed class AnchorAnimation : IUIAnimation {
    [SerializeField, Required] private RectTransform m_RectTransform = null!;
    [SerializeField] private OptionalValue<Vector2> m_TargetMinAnchor = Vector2.zero;
    [SerializeField] private OptionalValue<Vector2> m_TargetMaxAnchor = Vector2.one;
    [SerializeField] private OptionalValue<Vector2> m_OriginalMinAnchor = Vector2.zero;
    [SerializeField] private OptionalValue<Vector2> m_OriginalMaxAnchor = Vector2.one;

    public async UniTask Play(float duration, CancellationToken cancellationToken) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, m_RectTransform.GetCancellationTokenOnDestroy());
        var sequenceBuilder = LSequence.Create();
        if (m_TargetMinAnchor.Enabled)
            sequenceBuilder.Join(LMotion.Create(m_RectTransform.anchorMin, m_TargetMinAnchor.Value, duration).WithDefaults().BindToAnchorMin(m_RectTransform));
        if (m_TargetMaxAnchor.Enabled)
            sequenceBuilder.Join(LMotion.Create(m_RectTransform.anchorMax, m_TargetMaxAnchor.Value, duration).WithDefaults().BindToAnchorMax(m_RectTransform));
        await sequenceBuilder.Run().CancelOnDestroy(m_RectTransform).ToUniTask(cts.Token);
    }

    public async UniTask PlayReverse(float duration, CancellationToken cancellationToken) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, m_RectTransform.GetCancellationTokenOnDestroy());
        var sequenceBuilder = LSequence.Create();
        if (m_OriginalMinAnchor.Enabled)
            sequenceBuilder.Join(LMotion.Create(m_RectTransform.anchorMin, m_OriginalMinAnchor.Value, duration).WithDefaults().BindToAnchorMin(m_RectTransform));
        if (m_OriginalMaxAnchor.Enabled)
            sequenceBuilder.Join(LMotion.Create(m_RectTransform.anchorMax, m_OriginalMaxAnchor.Value, duration).WithDefaults().BindToAnchorMax(m_RectTransform));
        await sequenceBuilder.Run().CancelOnDestroy(m_RectTransform).ToUniTask(cts.Token);
    }

    public void Play() {
        if (m_RectTransform == null) return;

        if (m_TargetMinAnchor.Enabled)
            m_RectTransform.anchorMin = m_TargetMinAnchor.Value;
        if (m_TargetMaxAnchor.Enabled)
            m_RectTransform.anchorMax = m_TargetMaxAnchor.Value;
    }

    public void PlayReverse() {
        if (m_RectTransform == null) return;

        if (m_OriginalMinAnchor.Enabled)
            m_RectTransform.anchorMin = m_OriginalMinAnchor.Value;
        if (m_OriginalMaxAnchor.Enabled)
            m_RectTransform.anchorMax = m_OriginalMaxAnchor.Value;
    }
}
