using UnityEngine;
using UnityEngine.Rendering;

namespace TrafficSimulation.Core.Hacks;

file static class RenderingDebugManagerDisabler {
    [RuntimeInitializeOnLoadMethod]
    private static void Run() => DebugManager.instance.enableRuntimeUI = false;
}
