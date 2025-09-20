using JetBrains.Annotations;
using Sirenix.Utilities;
using UnityEngine;

namespace TrafficSimulation.Core.Extensions;

public static partial class ObjectExtensions {
    [ContractAnnotation("null => null; notnull => notnull")]
    public static T? OrNull<T>(this T? obj) where T : class {
        if (obj is Object unityObj)
            return unityObj.SafeIsUnityNull() ? null : obj;
        return obj;
    }

    [ContractAnnotation("null => null; notnull => notnull")]
    public static ref T? OrNull<T>(ref T? obj) where T : class {
        if (obj is Object unityObj && unityObj.SafeIsUnityNull()) {
            obj = null;
        }

        return ref obj;
    }

    public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component {
        return gameObject.GetComponent<T>().OrNull() ?? gameObject.AddComponent<T>();
    }

    public static T GetOrAddComponent<T>(this Component component) where T : Component {
        return GetOrAddComponent<T>(component.gameObject);
    }

    public static void DestroyObject(this Object? obj, bool allowDestroyingAssets = false) {
        if (obj.SafeIsUnityNull()) {
            return;
        }

        if (Application.isPlaying) {
            Object.Destroy(obj);
        } else {
            Object.DestroyImmediate(obj, allowDestroyingAssets);
        }
    }
}
