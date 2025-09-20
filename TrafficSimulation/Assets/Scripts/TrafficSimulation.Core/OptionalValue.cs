using Sirenix.OdinInspector;
using UnityEngine;

namespace TrafficSimulation.Core;

[Serializable, InlineProperty]
public struct OptionalValue<T>(T value, bool enabled = false) {
    [SerializeField, HorizontalGroup]
    [EnableIf(nameof(m_Enabled)), HideLabel]
    private T m_Value = value;

    [SerializeField, HorizontalGroup(16), HideLabel, PropertyOrder(-1), PropertySpace(2)]
    private bool m_Enabled = enabled;

    /// <summary>
    /// Gets or sets the value of the optional.
    /// </summary>
    public T Value {
        get => m_Value;
        set => m_Value = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether this <see cref="OptionalValue{T}"/> is enabled.
    /// </summary>
    public bool Enabled {
        get => m_Enabled;
        set => m_Enabled = value;
    }

    public T? GetValueOrDefault(T? defaultValue = default) => m_Enabled ? m_Value : defaultValue;

    public static implicit operator OptionalValue<T>(T value) => new(value, true);
}
