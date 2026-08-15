using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Rils.CSharp;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Rils.Unity.Editor
{
    [ScriptedImporter(1, "rils")]
    internal sealed class RilsScriptImporter : ScriptedImporter
    {
        private static readonly Regex ModuleDeclaration = new Regex(
            @"^\s*mod\s+([A-Za-z_][A-Za-z0-9_]*)\s*;",
            RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.CultureInvariant);

        public override void OnImportAsset(AssetImportContext context)
        {
            string fullPath = Path.GetFullPath(context.assetPath);
            RegisterModuleDependencies(context, fullPath, new HashSet<string>(StringComparer.OrdinalIgnoreCase));

            try
            {
                using (var runtime = new RilsRuntime())
                using (RilsModule module = runtime.CompileFile(fullPath))
                {
                    var asset = ScriptableObject.CreateInstance<RilsBytecodeAsset>();
                    asset.name = Path.GetFileNameWithoutExtension(context.assetPath);
                    asset.Initialize(context.assetPath, module.GetBytecode());
                    context.AddObjectToAsset("bytecode", asset);
                    context.SetMainObject(asset);
                }
            }
            catch (Exception exception)
            {
                context.LogImportError(
                    $"Failed to compile Rils script '{context.assetPath}': {exception}");
            }
        }

        private static void RegisterModuleDependencies(
            AssetImportContext context,
            string sourcePath,
            HashSet<string> visited)
        {
            sourcePath = Path.GetFullPath(sourcePath);
            if (!visited.Add(sourcePath) || !File.Exists(sourcePath))
            {
                return;
            }

            string source = File.ReadAllText(sourcePath, Encoding.UTF8);
            string directory = Path.GetDirectoryName(sourcePath) ?? string.Empty;
            foreach (Match match in ModuleDeclaration.Matches(source))
            {
                string moduleName = match.Groups[1].Value;
                string siblingPath = Path.Combine(directory, moduleName + ".rils");
                string nestedPath = Path.Combine(directory, moduleName, "mod.rils");
                string dependencyPath = File.Exists(siblingPath) ? siblingPath : nestedPath;
                if (!File.Exists(dependencyPath))
                {
                    continue;
                }

                string assetPath = ToAssetPath(dependencyPath);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    context.DependsOnSourceAsset(assetPath);
                }
                RegisterModuleDependencies(context, dependencyPath, visited);
            }
        }

        private static string ToAssetPath(string fullPath)
        {
            string projectPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."))
                .Replace('\\', '/');
            string normalizedPath = Path.GetFullPath(fullPath).Replace('\\', '/');
            string prefix = projectPath.EndsWith("/", StringComparison.Ordinal)
                ? projectPath
                : projectPath + "/";
            return normalizedPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                ? normalizedPath.Substring(prefix.Length)
                : string.Empty;
        }
    }
}
