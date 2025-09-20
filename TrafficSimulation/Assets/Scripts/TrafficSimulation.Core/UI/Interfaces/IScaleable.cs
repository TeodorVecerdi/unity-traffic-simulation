using LitMotion;
using LitMotion.Adapters;
using UnityEngine;

namespace TrafficSimulation.Core.UI.Interfaces;

public interface IScaleable {
    public bool IsValid { get; }
    public Vector3 Scale { get; set; }

    public MotionHandle AnimateScale(Vector3 scale, float duration, Action<MotionBuilder<Vector3, NoOptions, Vector3MotionAdapter>>? configure = null);
    public MotionHandle AnimateScale(Vector3 fromScale, Vector3 toScale, float duration, Action<MotionBuilder<Vector3, NoOptions, Vector3MotionAdapter>>? configure = null);
    public void CancelScaleAnimation();
}
