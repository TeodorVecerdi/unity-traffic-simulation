using LitMotion;
using LitMotion.Extensions;
using TrafficSimulation.Core.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace TrafficSimulation.UI.Animations;

[Serializable]
public sealed class FontSizeAnimation : IUIAnimation {
    [SerializeField, Required] private TextMeshProUGUI m_Label = null!;
    [SerializeField] private float m_TargetFontSize = 36.0f;
    [SerializeField] private float m_OriginalFontSize = 36.0f;

    public async UniTask Play(float duration, CancellationToken cancellationToken) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, m_Label.GetCancellationTokenOnDestroy());
        await LMotion.Create(m_Label.fontSize, m_TargetFontSize, duration)
            .WithDefaults().BindToFontSize(m_Label).CancelOnDestroy(m_Label).ToUniTask(cts.Token);
    }

    public async UniTask PlayReverse(float duration, CancellationToken cancellationToken) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, m_Label.GetCancellationTokenOnDestroy());
        await LMotion.Create(m_Label.fontSize, m_OriginalFontSize, duration)
            .WithDefaults().BindToFontSize(m_Label).CancelOnDestroy(m_Label).ToUniTask(cts.Token);
    }

    public void Play() {
        if (m_Label == null) return;
        m_Label.fontSize = m_TargetFontSize;
    }

    public void PlayReverse() {
        if (m_Label == null) return;
        m_Label.fontSize = m_OriginalFontSize;
    }
}
