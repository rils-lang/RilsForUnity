using System;
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
    }

    [PreferBinarySerialization]
    public sealed class RilsBytecodeAsset : ScriptableObject
    {
        [SerializeField]
        private string _sourceName = string.Empty;

        [SerializeField, HideInInspector]
        private byte[] _bytecode = Array.Empty<byte>();

        [SerializeField, HideInInspector]
        private RilsLifecycleFlags _lifecycleFlags;

        [SerializeField, HideInInspector]
        private byte[] _hostManifest = Array.Empty<byte>();

        public string SourceName => _sourceName;

        public int BytecodeLength => _bytecode.Length;

        public RilsLifecycleFlags LifecycleFlags => _lifecycleFlags;

        public bool HasHostManifest => _hostManifest.Length != 0;

        public byte[] GetHostManifest()
        {
            return (byte[])_hostManifest.Clone();
        }

        public byte[] GetBytecode()
        {
            return (byte[])_bytecode.Clone();
        }

        internal void Initialize(
            string sourceName,
            byte[] bytecode,
            RilsLifecycleFlags lifecycleFlags,
            byte[]? hostManifest = null)
        {
            _sourceName = sourceName ?? throw new ArgumentNullException(nameof(sourceName));
            _bytecode = bytecode != null
                ? (byte[])bytecode.Clone()
                : throw new ArgumentNullException(nameof(bytecode));
            _lifecycleFlags = lifecycleFlags;
            _hostManifest = hostManifest != null
                ? (byte[])hostManifest.Clone()
                : Array.Empty<byte>();
        }
    }
}
