using System.IO;
using UnityEditor;
using UnityEngine;

namespace TrafficSimulation.Core.Editor.MenuItems;

public static class MiscMenuItems {
    [MenuItem("Traffic Simulation/Open Persistent Data Path")]
    private static void OpenPersistentDataPath() {
        var path = Application.persistentDataPath;
        var firstEntry = Directory.EnumerateFileSystemEntries(path).FirstOrDefault();
        EditorUtility.RevealInFinder(firstEntry ?? path);
    }
}
