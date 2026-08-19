using System;
using System.Collections.Generic;
using System.IO;
using Rils.CSharp;
using RilsForUnity;
using UnityEditor;
using UnityEngine;

namespace Rils.Unity.Editor
{
    /// Generates the editor/runtime host contract outside Assets so it is not
    /// imported as a Unity asset or accidentally shipped as project content.
    internal static class RilsUnityManifestGenerator
    {
        private const string RelativeManifestDirectory = ".rils/manifest/unity-engine";
        private const string LegacyManifestPath = ".rils/manifest/unity.object.rilhm";
        private static bool _validationScheduled;

        [InitializeOnLoadMethod]
        private static void ScheduleValidation()
        {
            if (_validationScheduled)
            {
                return;
            }
            _validationScheduled = true;
            EditorApplication.delayCall += ValidateManifestOnEditorLoad;
        }

        private static void ValidateManifestOnEditorLoad()
        {
            _validationScheduled = false;
            try
            {
                IReadOnlyDictionary<string, byte[]> expected = BuildManifests();
                if (!SynchronizeManifests(expected))
                {
                    return;
                }
                ReimportRilsAssets();
                Debug.Log(
                    $"Regenerated {expected.Count} missing, damaged, or outdated Rils Unity host manifest modules under {RelativeManifestDirectory}.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        [MenuItem("Rils/Generate Unity Host Manifest")]
        private static void GenerateFromMenu()
        {
            try
            {
                IReadOnlyDictionary<string, byte[]> expected = BuildManifests();
                SynchronizeManifests(expected, forceWrite: true);
                ReimportRilsAssets();
                Debug.Log(
                    $"Generated {expected.Count} Rils Unity host manifest modules under {RelativeManifestDirectory}.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private static bool SynchronizeManifests(
            IReadOnlyDictionary<string, byte[]> expected,
            bool forceWrite = false)
        {
            bool changed = false;
            string projectRoot = ProjectRoot();
            string manifestDirectory = Path.Combine(projectRoot, RelativeManifestDirectory);
            var expectedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, byte[]> fragment in expected)
            {
                string manifestPath = Path.GetFullPath(Path.Combine(projectRoot, fragment.Key));
                expectedPaths.Add(manifestPath);
                if (!forceWrite && File.Exists(manifestPath) &&
                    ContentsEqual(File.ReadAllBytes(manifestPath), fragment.Value))
                {
                    continue;
                }
                WriteManifestAtomically(manifestPath, fragment.Value);
                changed = true;
            }

            if (Directory.Exists(manifestDirectory))
            {
                foreach (string existing in Directory.GetFiles(
                    manifestDirectory,
                    "*.rilhm",
                    SearchOption.AllDirectories))
                {
                    string fullPath = Path.GetFullPath(existing);
                    if (!expectedPaths.Contains(fullPath))
                    {
                        File.Delete(fullPath);
                        changed = true;
                    }
                }
            }

            string legacyPath = Path.Combine(projectRoot, LegacyManifestPath);
            if (File.Exists(legacyPath))
            {
                File.Delete(legacyPath);
                changed = true;
            }
            return changed;
        }

        private static void WriteManifestAtomically(string manifestPath, byte[] manifest)
        {
            string? directory = Path.GetDirectoryName(manifestPath);
            if (string.IsNullOrEmpty(directory))
            {
                throw new InvalidOperationException("Rils Unity host manifest has no parent directory.");
            }
            Directory.CreateDirectory(directory);

            string temporaryPath = manifestPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllBytes(temporaryPath, manifest);
                if (File.Exists(manifestPath))
                {
                    File.Replace(temporaryPath, manifestPath, null);
                }
                else
                {
                    File.Move(temporaryPath, manifestPath);
                }
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }

        private static bool ContentsEqual(byte[] left, byte[] right)
        {
            if (left.Length != right.Length)
            {
                return false;
            }
            for (int index = 0; index < left.Length; index++)
            {
                if (left[index] != right[index])
                {
                    return false;
                }
            }
            return true;
        }

        private static IReadOnlyDictionary<string, byte[]> BuildManifests()
        {
            var fragments = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            foreach (UnityHostBindingModule module in UnityEngineBindingCatalog.Modules)
            {
                const string prefix = "unity_engine::";
                string moduleName = module.Descriptor.Name;
                if (!moduleName.StartsWith(prefix, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"UnityEngine host module '{moduleName}' does not use the '{prefix}' prefix.");
                }
                string relativeModule = moduleName.Substring(prefix.Length).Replace("::", "/");
                string relativePath = $"{RelativeManifestDirectory}/{relativeModule}.rilhm";
                fragments.Add(relativePath, RilsHostManifestBuilder.Build(module.Descriptor));
            }
            return fragments;
        }

        private static void ReimportRilsAssets()
        {
            AssetDatabase.Refresh();
            string assetsRoot = Application.dataPath;
            foreach (string sourcePath in Directory.GetFiles(
                assetsRoot,
                "*.rils",
                SearchOption.AllDirectories))
            {
                string assetPath = "Assets" + sourcePath.Substring(assetsRoot.Length)
                    .Replace(Path.DirectorySeparatorChar, '/');
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            }
        }

        private static string ProjectRoot()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }
    }
}
