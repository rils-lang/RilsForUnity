#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Rils.Unity
{
    [Flags]
    public enum RilsLifecycleFlags
    {
        None = 0,
        Awake = 1 << 0,
        Start = 1 << 1,
        Update = 1 << 2,
        OnDestroy = 1 << 3,
        All = Awake | Start | Update | OnDestroy,
    }

    /// A selectable RilsBehaviour implementation produced as a .rils sub-asset.
    public sealed class RilsEntryAsset : ScriptableObject
    {
        [SerializeField]
        private RilsScriptAsset? _script;

        [SerializeField]
        private string _entryId = string.Empty;

        [SerializeField, HideInInspector]
        private RilsLifecycleFlags _lifecycleFlags;

        [SerializeField, HideInInspector]
        private RilsLibraryAsset[] _libraries = Array.Empty<RilsLibraryAsset>();

        public RilsScriptAsset Script => _script != null
            ? _script
            : throw new InvalidOperationException("The Rils entry has no script asset.");

        public string EntryId => _entryId;

        public string SourceName => Script.SourceName;

        public RilsLifecycleFlags LifecycleFlags => _lifecycleFlags;

        public IReadOnlyList<RilsLibraryAsset> Libraries => _libraries;

        public byte[] GetBytecode() => Script.GetBytecode();

        internal byte[] GetHostManifest() => Script.GetHostManifest();

        internal void Initialize(
            RilsScriptAsset script,
            string entryId,
            RilsLifecycleFlags lifecycleFlags,
            IReadOnlyList<RilsLibraryAsset>? libraries = null)
        {
            _script = script != null ? script : throw new ArgumentNullException(nameof(script));
            _entryId = !string.IsNullOrWhiteSpace(entryId)
                ? entryId
                : throw new ArgumentException("Entry ID cannot be empty.", nameof(entryId));
            _lifecycleFlags = lifecycleFlags;
            _libraries = libraries != null ? libraries.ToArray() : Array.Empty<RilsLibraryAsset>();
        }
    }
}
