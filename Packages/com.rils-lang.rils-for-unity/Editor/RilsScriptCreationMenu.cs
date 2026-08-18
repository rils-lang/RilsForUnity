#nullable enable
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

namespace Rils.Unity.Editor
{
    internal static class RilsScriptCreationMenu
    {
        [MenuItem("Assets/Create/Rils/Empty Script", priority = 80)]
        private static void CreateEmptyScript()
        {
            StartCreating("NewRilsScript.rils", false);
        }

        [MenuItem("Assets/Create/Rils/RilsBehaviour Script", priority = 81)]
        private static void CreateBehaviourScript()
        {
            StartCreating("NewRilsBehaviour.rils", true);
        }

        private static void StartCreating(string defaultName, bool createBehaviour)
        {
            string path = RilsScriptCreateAction.ModuleSafeUniquePath(
                Path.Combine(SelectedFolder(), defaultName).Replace('\\', '/'));
            var action = ScriptableObject.CreateInstance<RilsScriptCreateAction>();
            action.Initialize(createBehaviour);
            Texture2D? icon = EditorGUIUtility.IconContent("TextAsset Icon").image as Texture2D;
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                0,
                action,
                path,
                icon,
                null);
        }

        private static string SelectedFolder()
        {
            string selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(selectedPath))
            {
                return "Assets";
            }
            if (AssetDatabase.IsValidFolder(selectedPath))
            {
                return selectedPath;
            }
            return Path.GetDirectoryName(selectedPath)?.Replace('\\', '/') ?? "Assets";
        }
    }

    internal sealed class RilsScriptCreateAction : EndNameEditAction
    {
        [SerializeField]
        private bool _createBehaviour;

        internal void Initialize(bool createBehaviour)
        {
            _createBehaviour = createBehaviour;
        }

        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            pathName = ModuleSafeUniquePath(pathName);
            string contents = _createBehaviour
                ? BehaviourTemplate(IdentifierFromPath(pathName))
                : string.Empty;
            File.WriteAllText(Path.GetFullPath(pathName), contents, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(pathName, ImportAssetOptions.ForceUpdate);
            UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(pathName);
            ProjectWindowUtil.ShowCreatedAsset(asset);
        }

        internal static string ModuleSafeUniquePath(string pathName)
        {
            string directory = Path.GetDirectoryName(pathName)?.Replace('\\', '/') ?? "Assets";
            string identifier = IdentifierFromPath(pathName);
            string candidate = $"{directory}/{identifier}.rils";
            int suffix = 1;
            while (File.Exists(Path.GetFullPath(candidate)))
            {
                candidate = $"{directory}/{identifier}_{suffix}.rils";
                suffix++;
            }
            return candidate;
        }

        private static string IdentifierFromPath(string pathName)
        {
            string name = Path.GetFileNameWithoutExtension(pathName);
            var identifier = new StringBuilder(name.Length + 1);
            for (int index = 0; index < name.Length; index++)
            {
                char character = name[index];
                identifier.Append(character == '_' || char.IsLetterOrDigit(character) ? character : '_');
            }
            if (identifier.Length == 0)
            {
                identifier.Append("RilsBehaviour");
            }
            else if (char.IsDigit(identifier[0]))
            {
                identifier.Insert(0, '_');
            }
            if (IsReservedIdentifier(identifier.ToString()))
            {
                identifier.Insert(0, '_');
            }
            return identifier.ToString();
        }

        private static bool IsReservedIdentifier(string identifier)
        {
            switch (identifier)
            {
                case "let":
                case "mut":
                case "fn":
                case "macro":
                case "if":
                case "else":
                case "while":
                case "loop":
                case "match":
                case "struct":
                case "enum":
                case "impl":
                case "trait":
                case "type":
                case "for":
                case "in":
                case "as":
                case "return":
                case "break":
                case "continue":
                case "mod":
                case "use":
                case "crate":
                case "self":
                case "super":
                case "pub":
                case "true":
                case "false":
                case "nil":
                case "core":
                case "std":
                case "prelude":
                    return true;
                default:
                    return false;
            }
        }

        private static string BehaviourTemplate(string typeName)
        {
            return string.Join("\n", new[]
            {
                "#[derive(Default)]",
                $"pub struct {typeName};",
                string.Empty,
                $"impl RilsBehaviour for {typeName} {{",
                "    fn awake(&mut self, host: HostHandle) {",
                "    }",
                string.Empty,
                "    fn start(&mut self, host: HostHandle) {",
                "    }",
                string.Empty,
                "    fn update(&mut self, host: HostHandle, delta_seconds: f32) {",
                "    }",
                string.Empty,
                "    fn on_destroy(&mut self, host: HostHandle) {",
                "    }",
                "}",
                string.Empty,
            });
        }
    }
}
