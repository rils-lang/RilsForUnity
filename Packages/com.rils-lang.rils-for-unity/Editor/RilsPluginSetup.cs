using UnityEditor;

namespace Rils.Unity.Editor
{
    internal static class RilsPluginSetup
    {
        private const string PluginPath =
            "Packages/com.rils-lang.rils-for-unity/Runtime/Rils.CSharp/Internal/x86_64/rils_capi.dll";

        [MenuItem("Rils/Setup Windows x64 Plugin")]
        private static void ConfigureWindowsPlugin()
        {
            PluginImporter importer = AssetImporter.GetAtPath(PluginPath) as PluginImporter;
            if (importer == null)
            {
                throw new System.InvalidOperationException(
                    $"Rils native plugin was not found at {PluginPath}.");
            }

            importer.SetCompatibleWithAnyPlatform(false);
            importer.SetCompatibleWithEditor(true);
            importer.SetEditorData("OS", "Windows");
            importer.SetEditorData("CPU", "x86_64");

            importer.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows, false);
            importer.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows64, true);
            importer.SetPlatformData(BuildTarget.StandaloneWindows64, "CPU", "x86_64");
            importer.SetCompatibleWithPlatform(BuildTarget.StandaloneLinux64, false);
            importer.SetCompatibleWithPlatform(BuildTarget.StandaloneOSX, false);
            importer.SaveAndReimport();
        }
    }
}
