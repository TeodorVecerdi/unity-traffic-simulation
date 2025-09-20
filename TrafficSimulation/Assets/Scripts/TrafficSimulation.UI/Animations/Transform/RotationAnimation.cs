using LitMotion;
using LitMotion.Extensions;
using TrafficSimulation.Core.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TrafficSimulation.UI.Animations;

[Serializable]
public sealed class RotationAnimation : IUIAnimation {
    [SerializeField, Required] private Transform m_Transform = null!;
    [SerializeField] private Quaternion m_TargetRotation = Quaternion.identity;
    [SerializeField] private Quaternion m_OriginalRotation = Quaternion.identity;

    public async UniTask Play(float duration, CancellationToken cancellationToken) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, m_Transform.GetCancellationTokenOnDestroy());
        await LMotion.Create(m_Transform.localRotation, m_TargetRotation, duration)
            .WithDefaults().BindToLocalRotation(m_Transform).CancelOnDestroy(m_Transform).ToUniTask(cts.Token);
    }

    public async UniTask PlayReverse(float duration, CancellationToken cancellationToken) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, m_Transform.GetCancellationTokenOnDestroy());
        await LMotion.Create(m_Transform.localRotation, m_OriginalRotation, duration)
            .WithDefaults().BindToLocalRotation(m_Transform).CancelOnDestroy(m_Transform).ToUniTask(cts.Token);
    }

    public void Play() {
        if (m_Transform == null) return;
        m_Transform.localRotation = m_TargetRotation;
    }

    public void PlayReverse() {
        if (m_Transform == null) return;
        m_Transform.localRotation = m_OriginalRotation;
    }
}
