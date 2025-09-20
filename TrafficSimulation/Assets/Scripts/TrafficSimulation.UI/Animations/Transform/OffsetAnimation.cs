using LitMotion;
using TrafficSimulation.Core;
using TrafficSimulation.Core.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TrafficSimulation.UI.Animations;

[Serializable]
public sealed class OffsetAnimation : IUIAnimation {
    [SerializeField, Required] private RectTransform m_RectTransform = null!;
    [SerializeField] private OptionalValue<float> m_TargetMinOffsetX;
    [SerializeField] private OptionalValue<float> m_TargetMinOffsetY;
    [SerializeField] private OptionalValue<float> m_TargetMaxOffsetX;
    [SerializeField] private OptionalValue<float> m_TargetMaxOffsetY;
    [Space]
    [SerializeField] private OptionalValue<float> m_OriginalMinOffsetX;
    [SerializeField] private OptionalValue<float> m_OriginalMinOffsetY;
    [SerializeField] private OptionalValue<float> m_OriginalMaxOffsetX;
    [SerializeField] private OptionalValue<float> m_OriginalMaxOffsetY;

    public async UniTask Play(float duration, CancellationToken cancellationToken) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, m_RectTransform.GetCancellationTokenOnDestroy());

        var sequence = LSequence.Create();
        if (m_TargetMinOffsetX.Enabled)
            sequence.Join(LMotion.Create(m_RectTransform.offsetMin.x, m_TargetMinOffsetX.Value, duration).WithDefaults().Bind(m_RectTransform, static (value, rt) => rt.offsetMin = new Vector2(value, rt.offsetMin.y)));
        if (m_TargetMinOffsetY.Enabled)
            sequence.Join(LMotion.Create(m_RectTransform.offsetMin.y, m_TargetMinOffsetY.Value, duration).WithDefaults().Bind(m_RectTransform, static (value, rt) => rt.offsetMin = new Vector2(rt.offsetMin.x, value)));
        if (m_TargetMaxOffsetX.Enabled)
            sequence.Join(LMotion.Create(m_RectTransform.offsetMax.x, m_TargetMaxOffsetX.Value, duration).WithDefaults().Bind(m_RectTransform, static (value, rt) => rt.offsetMax = new Vector2(value, rt.offsetMax.y)));
        if (m_TargetMaxOffsetY.Enabled)
            sequence.Join(LMotion.Create(m_RectTransform.offsetMax.y, m_TargetMaxOffsetY.Value, duration).WithDefaults().Bind(m_RectTransform, static (value, rt) => rt.offsetMax = new Vector2(rt.offsetMax.x, value)));
        await sequence.Run().CancelOnDestroy(m_RectTransform).ToUniTask(cts.Token);
    }

    public async UniTask PlayReverse(float duration, CancellationToken cancellationToken) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, m_RectTransform.GetCancellationTokenOnDestroy());

        var sequence = LSequence.Create();
        if (m_OriginalMinOffsetX.Enabled)
            sequence.Join(LMotion.Create(m_RectTransform.offsetMin.x, m_OriginalMinOffsetX.Value, duration).WithDefaults().Bind(m_RectTransform, static (value, rt) => rt.offsetMin = new Vector2(value, rt.offsetMin.y)));
        if (m_OriginalMinOffsetY.Enabled)
            sequence.Join(LMotion.Create(m_RectTransform.offsetMin.y, m_OriginalMinOffsetY.Value, duration).WithDefaults().Bind(m_RectTransform, static (value, rt) => rt.offsetMin = new Vector2(rt.offsetMin.x, value)));
        if (m_OriginalMaxOffsetX.Enabled)
            sequence.Join(LMotion.Create(m_RectTransform.offsetMax.x, m_OriginalMaxOffsetX.Value, duration).WithDefaults().Bind(m_RectTransform, static (value, rt) => rt.offsetMax = new Vector2(value, rt.offsetMax.y)));
        if (m_OriginalMaxOffsetY.Enabled)
            sequence.Join(LMotion.Create(m_RectTransform.offsetMax.y, m_OriginalMaxOffsetY.Value, duration).WithDefaults().Bind(m_RectTransform, static (value, rt) => rt.offsetMax = new Vector2(rt.offsetMax.x, value)));
        await sequence.Run().CancelOnDestroy(m_RectTransform).ToUniTask(cts.Token);
    }

    public void Play() {
        if (m_RectTransform == null) return;

        var offsetMin = m_RectTransform.offsetMin;
        var offsetMax = m_RectTransform.offsetMax;

        if (m_TargetMinOffsetX.Enabled)
            offsetMin.x = m_TargetMinOffsetX.Value;
        if (m_TargetMinOffsetY.Enabled)
            offsetMin.y = m_TargetMinOffsetY.Value;
        if (m_TargetMaxOffsetX.Enabled)
            offsetMax.x = m_TargetMaxOffsetX.Value;
        if (m_TargetMaxOffsetY.Enabled)
            offsetMax.y = m_TargetMaxOffsetY.Value;

        m_RectTransform.offsetMin = offsetMin;
        m_RectTransform.offsetMax = offsetMax;
    }

    public void PlayReverse() {
        if (m_RectTransform == null) return;

        var offsetMin = m_RectTransform.offsetMin;
        var offsetMax = m_RectTransform.offsetMax;

        if (m_OriginalMinOffsetX.Enabled)
            offsetMin.x = m_OriginalMinOffsetX.Value;
        if (m_OriginalMinOffsetY.Enabled)
            offsetMin.y = m_OriginalMinOffsetY.Value;
        if (m_OriginalMaxOffsetX.Enabled)
            offsetMax.x = m_OriginalMaxOffsetX.Value;
        if (m_OriginalMaxOffsetY.Enabled)
            offsetMax.y = m_OriginalMaxOffsetY.Value;

        m_RectTransform.offsetMin = offsetMin;
        m_RectTransform.offsetMax = offsetMax;
    }
}
