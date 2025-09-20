using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using Screen = UnityEngine.Device.Screen;

namespace TrafficSimulation.UI.Layout;

public sealed class ScreenSizeBreakpointHandler : MonoBehaviour {
    [SerializeField] private Mode m_Mode = Mode.Width;
    [SerializeField, Required, Delayed] private List<Breakpoint> m_Breakpoints = [];
    private int m_BreakpointIndex = -1;

    private void OnEnable() {
        ScreenSizeChangeDetector.Instance.ScreenSizeChanged += OnScreenSizeChanged;
    }

    private void OnDisable() {
        ScreenSizeChangeDetector.Instance.ScreenSizeChanged -= OnScreenSizeChanged;
    }

    private void OnScreenSizeChanged() {
        HandleDimensionsChanged(Screen.width, Screen.height);
    }

    private void HandleDimensionsChanged(int width, int height) {
        if (m_Breakpoints.Count == 0)
            return;
        var breakpointIndex = FindBreakpointIndex(m_Mode is Mode.Width ? width : height);
        if (m_BreakpointIndex == breakpointIndex)
            return;
        m_BreakpointIndex = breakpointIndex;
        m_Breakpoints[m_BreakpointIndex].Handler.Invoke();
    }

    private int FindBreakpointIndex(int size) {
        m_Breakpoints.Sort((a, b) => a.Size.CompareTo(b.Size));
        var active = 0;
        for (var i = 0; i < m_Breakpoints.Count; i++) {
            var breakpoint = m_Breakpoints[i];
            if (size >= breakpoint.Size) {
                active = i;
            } else {
                break;
            }
        }

        return active;
    }

    [Serializable]
    private class Breakpoint {
        [SerializeField] private int m_Size;
        [SerializeField, Required] private UnityEvent m_Handler = new();
        public int Size => m_Size;
        public UnityEvent Handler => m_Handler;
    }

    private enum Mode {
        Width,
        Height,
    }
}
