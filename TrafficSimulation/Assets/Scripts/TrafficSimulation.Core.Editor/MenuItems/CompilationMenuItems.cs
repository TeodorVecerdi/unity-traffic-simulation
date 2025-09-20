using System.IO;
using Microsoft.Extensions.Logging;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditorInternal;
using UnityEngine;
using Vecerdi.Extensions.Logging;

namespace TrafficSimulation.Core.Editor.MenuItems;

internal static class CompilationMenuItems {
    [MenuItem("Traffic Simulation/Recompile Code")]
    private static void TriggerRecompilation() {
        CompilationPipeline.RequestScriptCompilation();
    }

    [MenuItem("Assets/Scripting/Configure Assembly Definition(s)", isValidateFunction: true)]
    private static bool CheckAssemblyDefinitionSelected() {
        return Selection.objects.Any(obj => obj is AssemblyDefinitionAsset);
    }

    [MenuItem("Assets/Scripting/Configure Assembly Definition(s)", priority = -2001)]
    private static void ConfigureAssemblyDefinition() {
        foreach (var assemblyDefinitionAsset in Selection.objects.OfType<AssemblyDefinitionAsset>()) {
            ProcessAssemblyDefinition(assemblyDefinitionAsset);
        }

        AssetDatabase.Refresh();
    }

    private static void ProcessAssemblyDefinition(AssemblyDefinitionAsset assemblyDefinition) {
        var logger = UnityLoggerFactory.CreateLogger(typeof(CompilationMenuItems).FullName ?? nameof(CompilationMenuItems));
        var jsonElement = JsonDocument.Parse(assemblyDefinition.text).RootElement;
        var name = jsonElement.GetProperty("name").GetString();
        if (string.IsNullOrEmpty(name)) {
            logger.LogError("Assembly Definition is missing the 'name' property");
            return;
        }

        var isFirstPartyAssembly = name!.StartsWith("TrafficSimulation.") || name.StartsWith("Vecerdi.");
        var projectRootPath = Path.Combine(Application.dataPath, "..");
        var sourceRspPath = Path.Combine(projectRootPath, "Build", isFirstPartyAssembly ? "FirstParty" : "ThirdParty", "csc.rsp");
        if (!File.Exists(sourceRspPath)) {
            logger.LogError("Missing csc.rsp file at {SourceRspPath}", sourceRspPath);
            return;
        }

        var assemblyDefinitionPath = AssetDatabase.GetAssetPath(assemblyDefinition);
        var assemblyDefinitionDirectory = Path.GetDirectoryName(assemblyDefinitionPath);
        if (string.IsNullOrEmpty(assemblyDefinitionDirectory)) {
            logger.LogError("Failed to get directory for {AssemblyDefinitionPath}", assemblyDefinitionPath);
            return;
        }

        var destinationRspPath = Path.Combine(assemblyDefinitionDirectory, "csc.rsp");
        File.Copy(sourceRspPath, destinationRspPath, overwrite: true);
    }
}
