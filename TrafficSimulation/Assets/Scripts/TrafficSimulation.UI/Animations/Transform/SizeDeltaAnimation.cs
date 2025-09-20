using LitMotion;
using LitMotion.Extensions;
using TrafficSimulation.Core;
using TrafficSimulation.Core.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TrafficSimulation.UI.Animations;

[Serializable]
public sealed class SizeDeltaAnimation : IUIAnimation {
    [SerializeField, Required] private RectTransform m_RectTransform = null!;
    [SerializeField] private bool m_EnableOriginalProperties = true;
    [SerializeField] private OptionalValue<float> m_TargetSizeX;
    [SerializeField] private OptionalValue<float> m_TargetSizeY;
    [Space]
    [ShowIf(nameof(m_EnableOriginalProperties))]
    [SerializeField] private OptionalValue<float> m_OriginalSizeX;
    [ShowIf(nameof(m_EnableOriginalProperties))]
    [SerializeField] private OptionalValue<float> m_OriginalSizeY;

    public async UniTask Play(float duration, CancellationToken cancellationToken) {
        if (!m_TargetSizeX.Enabled && !m_TargetSizeY.Enabled)
            return;

        var fromSizeDelta = GetOriginalSizeDelta();
        var toSizeDelta = GetTargetSizeDelta();
        if ((toSizeDelta - fromSizeDelta).sqrMagnitude < 0.01f)
            return;

        await LMotion.Create(fromSizeDelta, toSizeDelta, duration)
            .WithDefaults()
            .BindToSizeDelta(m_RectTransform)
            .CancelOnDestroy(m_RectTransform)
            .ToUniTask(cancellationToken);
    }

    public async UniTask PlayReverse(float duration, CancellationToken cancellationToken) {
        if (!m_TargetSizeX.Enabled && !m_TargetSizeY.Enabled)
            return;

        var fromSizeDelta = GetTargetSizeDelta();
        var toSizeDelta = GetOriginalSizeDelta();
        if ((toSizeDelta - fromSizeDelta).sqrMagnitude < 0.01f)
            return;

        await LMotion.Create(fromSizeDelta, toSizeDelta, duration)
            .WithDefaults()
            .BindToSizeDelta(m_RectTransform)
            .CancelOnDestroy(m_RectTransform)
            .ToUniTask(cancellationToken);
    }

    private Vector2 GetTargetSizeDelta() {
        var targetSizeDelta = m_RectTransform.sizeDelta;
        if (m_TargetSizeX.Enabled)
            targetSizeDelta.x = m_TargetSizeX.Value;
        if (m_TargetSizeY.Enabled)
            targetSizeDelta.y = m_TargetSizeY.Value;
        return targetSizeDelta;
    }

    private Vector2 GetOriginalSizeDelta() {
        var originalSizeDelta = m_RectTransform.sizeDelta;
        if (!m_EnableOriginalProperties)
            return originalSizeDelta;

        if (m_OriginalSizeX.Enabled)
            originalSizeDelta.x = m_OriginalSizeX.Value;
        if (m_OriginalSizeY.Enabled)
            originalSizeDelta.y = m_OriginalSizeY.Value;
        return originalSizeDelta;
    }

    public void Play() {
        if (m_RectTransform == null) return;
        m_RectTransform.sizeDelta = GetTargetSizeDelta();
    }

    public void PlayReverse() {
        if (m_RectTransform == null) return;
        m_RectTransform.sizeDelta = GetOriginalSizeDelta();
    }
}
