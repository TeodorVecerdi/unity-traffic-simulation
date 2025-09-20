using LitMotion;
using LitMotion.Extensions;
using TrafficSimulation.Core.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TrafficSimulation.UI.Animations;

[Serializable]
public sealed class ScaleAnimation : IUIAnimation {
    [SerializeField, Required] private Transform m_Transform = null!;
    [SerializeField] private Vector3 m_TargetScale = Vector2.zero;
    [SerializeField] private Vector3 m_OriginalScale = Vector2.zero;
    [SerializeField] private bool m_IgnoreCurrentValues;

    public Transform Transform {
        get => m_Transform;
        set => m_Transform = value ?? throw new ArgumentNullException(nameof(value));
    }

    public Vector3 TargetScale {
        get => m_TargetScale;
        set => m_TargetScale = value;
    }

    public Vector3 OriginalScale {
        get => m_OriginalScale;
        set => m_OriginalScale = value;
    }

    public bool IgnoreCurrentValues {
        get => m_IgnoreCurrentValues;
        set => m_IgnoreCurrentValues = value;
    }

    public async UniTask Play(float duration, CancellationToken cancellationToken) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, m_Transform.GetCancellationTokenOnDestroy());
        var fromScale = m_IgnoreCurrentValues ? m_OriginalScale : m_Transform.localScale;
        await LMotion.Create(fromScale, m_TargetScale, duration)
            .WithDefaults().BindToLocalScale(m_Transform).CancelOnDestroy(m_Transform).ToUniTask(cts.Token);
    }

    public async UniTask PlayReverse(float duration, CancellationToken cancellationToken) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, m_Transform.GetCancellationTokenOnDestroy());
        var fromScale = m_IgnoreCurrentValues ? m_TargetScale : m_Transform.localScale;
        await LMotion.Create(fromScale, m_OriginalScale, duration)
            .WithDefaults().BindToLocalScale(m_Transform).CancelOnDestroy(m_Transform).ToUniTask(cts.Token);
    }

    public void Play() {
        if (m_Transform == null) return;
        m_Transform.localScale = m_TargetScale;
    }

    public void PlayReverse() {
        if (m_Transform == null) return;
        m_Transform.localScale = m_OriginalScale;
    }
}
