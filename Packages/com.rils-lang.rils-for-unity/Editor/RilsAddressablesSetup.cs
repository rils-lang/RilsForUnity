using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using Rils.Unity;

namespace Rils.Unity.Editor
{
    internal static class RilsAddressablesSetup
    {
        private const string GroupName = "Rils Bytecode";
        private const string LabelName = "rils-bytecode";

        [MenuItem("Rils/Setup Selected Bytecode Addressable")]
        private static void ConfigureSelectedBytecodeAddressable()
        {
            UnityEngine.Object selected = Selection.activeObject;
            string assetPath = selected is RilsEntryAsset
                ? AssetDatabase.GetAssetPath(selected)
                : string.Empty;
            string guid = string.IsNullOrEmpty(assetPath)
                ? string.Empty
                : AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
            {
                throw new System.InvalidOperationException(
                    "Select a RilsEntryAsset before configuring an Addressable.");
            }

            AddressableAssetSettings settings =
                AddressableAssetSettingsDefaultObject.GetSettings(true);
            AddressableAssetGroup group = settings.FindGroup(GroupName);
            if (group == null)
            {
                group = settings.CreateGroup(
                    GroupName,
                    false,
                    false,
                    true,
                    null,
                    typeof(BundledAssetGroupSchema),
                    typeof(ContentUpdateGroupSchema));
            }

            settings.AddLabel(LabelName);
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group, false, false);
            entry.address = assetPath + "[" + selected.name + "]";
            entry.SetLabel(LabelName, true, true, false);
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryModified, entry, true);
            AssetDatabase.SaveAssets();
        }
    }
}
