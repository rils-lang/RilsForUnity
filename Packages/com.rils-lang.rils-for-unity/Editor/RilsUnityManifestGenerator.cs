using System;
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
        private const string RelativeManifestPath = ".rils/manifest/unity.object.rilhm";
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
                byte[] expected = BuildManifest();
                string manifestPath = ManifestPath();
                if (File.Exists(manifestPath)
                    && ContentsEqual(File.ReadAllBytes(manifestPath), expected))
                {
                    return;
                }

                WriteManifestAtomically(manifestPath, expected);
                ReimportRilsAssets();
                Debug.Log($"Regenerated missing, damaged, or outdated Rils Unity host manifest: {RelativeManifestPath}");
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
                WriteManifestAtomically(ManifestPath(), BuildManifest());
                ReimportRilsAssets();
                Debug.Log($"Generated Rils Unity host manifest: {RelativeManifestPath}");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
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

        private static byte[] BuildManifest()
        {
            using (var runtime = new RilsRuntime())
            using (var handles = new UnityObjectHandleTable())
            using (var hosts = new RilsHostRegistry(runtime))
            {
                UnityObjectHostBindings.Register(hosts, handles);
                hosts.AllowCapability("unity.object");
                hosts.AllowCapability("unity.game_object");
                hosts.AllowCapability("unity.transform");
                hosts.AllowCapability("unity.component");
                return runtime.GetHostManifest();
            }
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

        private static string ManifestPath()
        {
            return Path.Combine(ProjectRoot(), ".rils", "manifest", "unity.object.rilhm");
        }
    }
}
