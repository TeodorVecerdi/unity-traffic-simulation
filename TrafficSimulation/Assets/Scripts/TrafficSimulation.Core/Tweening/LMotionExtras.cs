using LitMotion;
using UnityEngine;
using UnityEngine.UI;

namespace TrafficSimulation.Core.Tweening;

public static class LMotionExtras {
    public static MotionHandle UpdateLayout(float duration, RectTransform layoutRoot) {
        return LMotion.Create(0.0f, 1.0f, duration)
            .WithDefaults()
            .Bind(layoutRoot, static (_, rt) => LayoutRebuilder.MarkLayoutForRebuild(rt))
            .CancelOnDestroy(layoutRoot);
    }
}
