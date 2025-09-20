using LitMotion;
using LitMotion.Adapters;
using TrafficSimulation.Core.Tweening;
using TrafficSimulation.Core.UI.Interfaces;
using UnityEngine;

namespace TrafficSimulation.Core.UI;

public sealed class FadeableBehaviour(CanvasGroup canvasGroup) : IFadeable {
    private readonly CanvasGroup m_CanvasGroup = canvasGroup;
    private MotionHandle m_AnimationHandle;

    public bool IsValid => m_CanvasGroup != null;
    public bool SupportsRaycasting => true;

    public float Alpha {
        get => m_CanvasGroup.alpha;
        set {
            m_CanvasGroup.alpha = value;
            if (ManageRaycasting) {
                m_CanvasGroup.blocksRaycasts = value > 0.0f;
            }
        }
    }

    public bool ManageRaycasting {
        get;
        set {
            field = value;
            if (value) {
                m_CanvasGroup.blocksRaycasts = Alpha > 0.0f;
            }
        }
    }

    public bool RaycastTarget {
        get => m_CanvasGroup.blocksRaycasts;
        set => m_CanvasGroup.blocksRaycasts = value;
    }

    public MotionHandle AnimateAlpha(float alpha, float duration, Action<MotionBuilder<float, NoOptions, FloatMotionAdapter>>? configure = null) {
        return AnimateAlpha(Alpha, alpha, duration, configure);
    }

    public MotionHandle AnimateAlpha(float fromAlpha, float toAlpha, float duration, Action<MotionBuilder<float, NoOptions, FloatMotionAdapter>>? configure = null) {
        m_AnimationHandle.TryCancel();
        var builder = LMotion.Create(fromAlpha, toAlpha, duration).WithDefaults().WithDebugName("FadeableBehaviour");
        configure?.Invoke(builder);
        return m_AnimationHandle = builder.Bind(this, static (value, instance) => {
            if (instance.m_CanvasGroup == null)
                return;
            instance.Alpha = value;
        }).CancelOnDestroy(m_CanvasGroup);
    }

    public void CancelAlphaAnimation() {
        m_AnimationHandle.TryCancel();
    }
}
