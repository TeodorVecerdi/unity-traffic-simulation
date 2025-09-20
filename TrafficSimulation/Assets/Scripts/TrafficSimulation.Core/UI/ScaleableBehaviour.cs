using LitMotion;
using LitMotion.Adapters;
using LitMotion.Extensions;
using TrafficSimulation.Core.Tweening;
using TrafficSimulation.Core.UI.Interfaces;
using UnityEngine;

namespace TrafficSimulation.Core.UI;

public sealed class ScaleableBehaviour(Transform transform) : IScaleable {
    public bool IsValid => transform != null;

    public Vector3 Scale {
        get => transform.localScale;
        set => transform.localScale = value;
    }

    private MotionHandle m_AnimationHandle;

    public MotionHandle AnimateScale(Vector3 scale, float duration, Action<MotionBuilder<Vector3, NoOptions, Vector3MotionAdapter>>? configure = null) {
        return AnimateScale(Scale, scale, duration, configure);
    }

    public MotionHandle AnimateScale(Vector3 fromScale, Vector3 toScale, float duration, Action<MotionBuilder<Vector3, NoOptions, Vector3MotionAdapter>>? configure = null) {
        m_AnimationHandle.TryCancel();
        var motionBuilder = LMotion.Create(fromScale, toScale, duration).WithDefaults();
        configure?.Invoke(motionBuilder);
        return m_AnimationHandle = motionBuilder.BindToLocalScale(transform).CancelOnDestroy(transform);
    }

    public void CancelScaleAnimation() {
        m_AnimationHandle.TryCancel();
    }
}
