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

        [MenuItem("Rils/Generate Unity Host Manifest")]
        private static void GenerateFromMenu()
        {
            try
            {
                string projectRoot = ProjectRoot();
                string manifestPath = Path.Combine(projectRoot, ".rils", "manifest", "unity.object.rilhm");
                Directory.CreateDirectory(Path.GetDirectoryName(manifestPath)!);
                File.WriteAllBytes(manifestPath, BuildManifest());
                ReimportRilsAssets();
                Debug.Log($"Generated Rils Unity host manifest: {RelativeManifestPath}");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
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
    }
}
