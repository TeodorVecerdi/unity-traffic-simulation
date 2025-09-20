using System.IO;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace TrafficSimulation.Core.Editor;

[InitializeOnLoad]
public static class EditorResources {
    private const string DirectoryLabel = "EditorResources";
    private static readonly string[] s_EditorResourcesDirectoryPaths;
    private static readonly string[] s_EditorResourcesDirectoryFullPaths;

    private static readonly Dictionary<string, string> s_Index = new();

    static EditorResources() {
        s_EditorResourcesDirectoryPaths = AssetDatabase.FindAssets($"l:{DirectoryLabel} t:Folder")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(path => path.Replace('\\', '/'))
            .ToArray();

        s_EditorResourcesDirectoryFullPaths = s_EditorResourcesDirectoryPaths.Select(path => Path.Combine(Application.dataPath, path["Assets/".Length..]))
            .Select(path => path.Replace('\\', '/'))
            .ToArray();
    }

    [MustUseReturnValue]
    public static string? GetAssetPath<T>(string path) where T : Object {
        var indexKey = $"{path} : Path";
        if (s_Index.TryGetValue(indexKey, out var indexedAssetPath)) {
            var asset = AssetDatabase.LoadAssetAtPath<T>(indexedAssetPath);
            if (asset != null) {
                return indexedAssetPath;
            }

            s_Index.Remove(indexKey);
        }

        // If the path already contains an extension, try to load directly
        var extension = Path.GetExtension(path);
        if (!string.IsNullOrEmpty(extension)) {
            foreach (var editorResourcesPath in s_EditorResourcesDirectoryPaths) {
                var assetPath = Path.Combine(editorResourcesPath, path);
                if (AssetDatabase.LoadAssetAtPath<T>(assetPath) is not null) {
                    s_Index[indexKey] = assetPath;
                    return assetPath;
                }
            }

            return null;
        }

        // Otherwise, search for the asset
        var dataPath = Application.dataPath;
        foreach (var editorResourcesPath in s_EditorResourcesDirectoryFullPaths) {
            var fullAssetPath = Path.Combine(editorResourcesPath, path);
            var directoryPath = Path.GetDirectoryName(fullAssetPath)!;
            var fileName = Path.GetFileNameWithoutExtension(fullAssetPath);
            if (!Directory.Exists(directoryPath)) continue;

            foreach (var file in Directory.EnumerateFiles(directoryPath, $"{fileName}.*")) {
                if (Path.GetExtension(file) == ".meta") continue;

                var assetPath = $"Assets/{file[dataPath.Length..]}";
                if (AssetDatabase.LoadAssetAtPath<T>(assetPath) is not null) {
                    s_Index[indexKey] = assetPath;
                    return assetPath;
                }
            }
        }

        return null;
    }

    [MustUseReturnValue]
    public static T? Load<T>(string path) where T : Object {
        var assetPath = GetAssetPath<T>(path);
        return !string.IsNullOrEmpty(assetPath) ? AssetDatabase.LoadAssetAtPath<T>(assetPath) : null;
    }

    [MustUseReturnValue]
    public static T? Find<T>(string query) where T : Object {
        if (!query.Contains('/', StringComparison.Ordinal)) {
            var assetGuids = AssetDatabase.FindAssets($"{query} t:{typeof(T).Name}", s_EditorResourcesDirectoryPaths);
            if (assetGuids.Length is 0) return null;
            var assetPath = AssetDatabase.GUIDToAssetPath(assetGuids[0]);
            return AssetDatabase.LoadAssetAtPath<T>(assetPath);
        }

        return FindAssetAtPath<T>(query);
    }

    [MustUseReturnValue]
    public static IEnumerable<T> FindAll<T>(string query) where T : Object {
        if (!query.Contains('/', StringComparison.Ordinal)) {
            var assetGuids = AssetDatabase.FindAssets($"{query} t:{typeof(T).Name}", s_EditorResourcesDirectoryPaths);
            if (assetGuids.Length is 0) return Array.Empty<T>();

            return assetGuids.Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<T>);
        }

        return FindAllAssetsAtPath<T>(query);
    }

    private static T? FindAssetAtPath<T>(string query) where T : Object {
        var lastSlashIndex = query.LastIndexOf('/');
        var name = query[(lastSlashIndex + 1)..];
        var path = query[..lastSlashIndex];
        var assetGuids = AssetDatabase.FindAssets($"{name} t:{typeof(T).Name}", s_EditorResourcesDirectoryPaths);

        foreach (var assetGuid in assetGuids) {
            var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid).Replace('\\', '/');
            foreach (var editorResourcesPath in s_EditorResourcesDirectoryPaths) {
                if (!assetPath.StartsWith(editorResourcesPath, StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }

                var relativePath = assetPath[editorResourcesPath.Length..];
                if (relativePath.Contains(path, StringComparison.OrdinalIgnoreCase)) {
                    return AssetDatabase.LoadAssetAtPath<T>(assetPath);
                }
            }

            if (assetPath.Contains(path, StringComparison.OrdinalIgnoreCase)) {
                return AssetDatabase.LoadAssetAtPath<T>(assetPath);
            }
        }

        return null;
    }

    private static IEnumerable<T> FindAllAssetsAtPath<T>(string query) where T : Object {
        var lastSlashIndex = query.LastIndexOf('/');
        var name = query[(lastSlashIndex + 1)..];
        var path = query[..lastSlashIndex];
        var assetGuids = AssetDatabase.FindAssets($"{name} t:{typeof(T).Name}", s_EditorResourcesDirectoryPaths);

        foreach (var assetGuid in assetGuids) {
            var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid).Replace('\\', '/');
            var found = false;
            foreach (var editorResourcesPath in s_EditorResourcesDirectoryPaths) {
                if (!assetPath.StartsWith(editorResourcesPath, StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }

                var relativePath = assetPath[editorResourcesPath.Length..];
                if (relativePath.Contains(path, StringComparison.OrdinalIgnoreCase)) {
                    yield return AssetDatabase.LoadAssetAtPath<T>(assetPath);
                    found = true;
                    break;
                }
            }

            if (!found) {
                if (assetPath.Contains(path, StringComparison.OrdinalIgnoreCase)) {
                    yield return AssetDatabase.LoadAssetAtPath<T>(assetPath);
                }
            }
        }
    }
}
