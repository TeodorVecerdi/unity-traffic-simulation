using LitMotion;
using UnityEngine;

namespace TrafficSimulation.Core.Tweening;

public static class MotionHandleExtensions {
    public static MotionHandle CancelOnDestroy<T>(this MotionHandle handle, T target) where T : Component {
        target.GetCancellationTokenOnDestroy().Register(() => handle.TryCancel(), false);
        return handle;
    }

    public static MotionHandle CancelOnDestroy(this MotionHandle handle, GameObject target) {
        target.GetCancellationTokenOnDestroy().Register(() => handle.TryCancel(), false);
        return handle;
    }
}
