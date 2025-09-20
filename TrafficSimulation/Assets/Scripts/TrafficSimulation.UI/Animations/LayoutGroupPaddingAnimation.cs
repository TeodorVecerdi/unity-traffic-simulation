using LitMotion;
using TrafficSimulation.Core;
using TrafficSimulation.Core.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace TrafficSimulation.UI.Animations;

[Serializable]
public sealed class LayoutGroupPaddingAnimation : IUIAnimation {
    [SerializeField, Required] private LayoutGroup m_LayoutGroup = null!;
    [SerializeField] private OptionalValue<int> m_TargetLeft;
    [SerializeField] private OptionalValue<int> m_TargetRight;
    [SerializeField] private OptionalValue<int> m_TargetTop;
    [SerializeField] private OptionalValue<int> m_TargetBottom;
    [Space]
    [SerializeField] private OptionalValue<int> m_OriginalLeft;
    [SerializeField] private OptionalValue<int> m_OriginalRight;
    [SerializeField] private OptionalValue<int> m_OriginalTop;
    [SerializeField] private OptionalValue<int> m_OriginalBottom;

    public async UniTask Play(float duration, CancellationToken cancellationToken) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, m_LayoutGroup.GetCancellationTokenOnDestroy());

        Vector4 from = new(m_LayoutGroup.padding.left, m_LayoutGroup.padding.right, m_LayoutGroup.padding.top, m_LayoutGroup.padding.bottom);
        Vector4 to = new(
            m_TargetLeft.GetValueOrDefault(m_LayoutGroup.padding.left),
            m_TargetRight.GetValueOrDefault(m_LayoutGroup.padding.right),
            m_TargetTop.GetValueOrDefault(m_LayoutGroup.padding.top),
            m_TargetBottom.GetValueOrDefault(m_LayoutGroup.padding.bottom)
        );

        await LMotion.Create(from, to, duration)
            .WithDefaults().Bind(m_LayoutGroup, static (value, target) => target.padding = new RectOffset((int)value.x, (int)value.y, (int)value.z, (int)value.w))
            .CancelOnDestroy(m_LayoutGroup).ToUniTask(cts.Token);
    }

    public async UniTask PlayReverse(float duration, CancellationToken cancellationToken) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, m_LayoutGroup.GetCancellationTokenOnDestroy());

        Vector4 from = new(m_LayoutGroup.padding.left, m_LayoutGroup.padding.right, m_LayoutGroup.padding.top, m_LayoutGroup.padding.bottom);
        Vector4 to = new(
            m_OriginalLeft.GetValueOrDefault(m_LayoutGroup.padding.left),
            m_OriginalRight.GetValueOrDefault(m_LayoutGroup.padding.right),
            m_OriginalTop.GetValueOrDefault(m_LayoutGroup.padding.top),
            m_OriginalBottom.GetValueOrDefault(m_LayoutGroup.padding.bottom)
        );

        await LMotion.Create(from, to, duration)
            .WithDefaults().Bind(m_LayoutGroup, static (value, target) => target.padding = new RectOffset((int)value.x, (int)value.y, (int)value.z, (int)value.w))
            .CancelOnDestroy(m_LayoutGroup).ToUniTask(cts.Token);
    }

    public void Play() {
        if (m_LayoutGroup == null) return;
        m_LayoutGroup.padding = new RectOffset(
            m_TargetLeft.GetValueOrDefault(m_LayoutGroup.padding.left),
            m_TargetRight.GetValueOrDefault(m_LayoutGroup.padding.right),
            m_TargetTop.GetValueOrDefault(m_LayoutGroup.padding.top),
            m_TargetBottom.GetValueOrDefault(m_LayoutGroup.padding.bottom)
        );
    }

    public void PlayReverse() {
        if (m_LayoutGroup == null) return;
        m_LayoutGroup.padding = new RectOffset(
            m_OriginalLeft.GetValueOrDefault(m_LayoutGroup.padding.left),
            m_OriginalRight.GetValueOrDefault(m_LayoutGroup.padding.right),
            m_OriginalTop.GetValueOrDefault(m_LayoutGroup.padding.top),
            m_OriginalBottom.GetValueOrDefault(m_LayoutGroup.padding.bottom)
        );
    }
}
