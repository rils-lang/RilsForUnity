using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                List<byte[]> hostManifestFragments = ReadHostManifestFragments();
                var runtime = new RilsRuntime();
                RilsHostRegistry? hosts = null;
                try
                {
                    if (hostManifestFragments.Count != 0)
                    {
                        hosts = new RilsHostRegistry(runtime);
                        foreach (byte[] fragment in hostManifestFragments)
                        {
                            runtime.RegisterHostManifest(fragment);
                        }
                        runtime.AllowCapability("unity.object");
                        runtime.AllowCapability("unity.game_object");
                        runtime.AllowCapability("unity.transform");
                        runtime.AllowCapability("unity.component");
                        runtime.FreezeHostRegistry();
                    }
                    using (RilsModule module = runtime.CompileFile(fullPath))
                    {
                        var script = ScriptableObject.CreateInstance<RilsScriptAsset>();
                        script.name = Path.GetFileNameWithoutExtension(context.assetPath);
                        script.Initialize(
                            context.assetPath,
                            module.GetBytecode(),
                            hostManifestFragments.Count == 0
                                ? Array.Empty<byte>()
                                : runtime.GetHostManifest());
                        context.AddObjectToAsset("script", script);
                        context.SetMainObject(script);

                        foreach (string entryId in module.GetTraitImplementations(
                                     "RilsBehaviour",
                                     fullPath))
                        {
                            var entry = ScriptableObject.CreateInstance<RilsEntryAsset>();
                            entry.name = entryId;
                            entry.Initialize(script, entryId, RilsLifecycleFlags.All);
                            context.AddObjectToAsset("entry:" + entryId, entry);
                        }
                    }
                }
                finally
                {
                    runtime.Dispose();
                    hosts?.Dispose();
                }
            }
            catch (Exception exception)
            {
                context.LogImportError(
                    $"Failed to compile Rils script '{context.assetPath}': {exception}");
            }
        }

        private static List<byte[]> ReadHostManifestFragments()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string manifestDirectory = Path.Combine(projectRoot, ".rils", "manifest");
            if (!Directory.Exists(manifestDirectory))
            {
                return new List<byte[]>();
            }
            return Directory.GetFiles(manifestDirectory, "*.rilhm", SearchOption.AllDirectories)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .Select(File.ReadAllBytes)
                .ToList();
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
