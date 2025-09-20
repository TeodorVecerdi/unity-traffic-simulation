using System.Diagnostics.CodeAnalysis;
using Sirenix.OdinInspector;
using UnityEngine;
using Vecerdi.Extensions.DependencyInjection;

namespace TrafficSimulation.Core.UI;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public sealed class MatchRectTransform : BaseMonoBehaviour {
    [SerializeField, Required] private RectTransform m_Target = null!;
    [SerializeField] private bool m_UpdateInEditor = true;

    [field: MaybeNull] private RectTransform RectTransform => field ??= (RectTransform)transform;

    public RectTransform Target {
        get => m_Target;
        set => m_Target = value;
    }

    private void LateUpdate() {
        if (m_Target != null && (m_UpdateInEditor || Application.isPlaying)) {
            MatchSize();
        }
    }

    private void MatchSize() {
        if (m_Target == null)
            return;

        // Copy transform properties
        var self = RectTransform;
        self.position = m_Target.position;
        self.rotation = m_Target.rotation;
        self.localScale = m_Target.localScale;
        self.pivot = m_Target.pivot;
        self.anchorMin = new Vector2(0.5f, 0.5f);
        self.anchorMax = new Vector2(0.5f, 0.5f);
        self.sizeDelta = m_Target.rect.size;
    }
}
