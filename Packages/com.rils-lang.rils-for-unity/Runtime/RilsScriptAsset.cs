#nullable enable
using System;
using UnityEngine;

namespace Rils.Unity
{
    /// The imported representation of one .rils source file.
    [PreferBinarySerialization]
    public sealed class RilsScriptAsset : ScriptableObject
    {
        [SerializeField]
        private string _sourceName = string.Empty;

        [SerializeField, HideInInspector]
        private byte[] _bytecode = Array.Empty<byte>();

        [SerializeField, HideInInspector]
        private byte[] _hostManifest = Array.Empty<byte>();

        public string SourceName => _sourceName;

        public int BytecodeLength => _bytecode.Length;

        public bool HasHostManifest => _hostManifest.Length != 0;

        internal byte[] GetBytecode() => (byte[])_bytecode.Clone();

        internal byte[] GetHostManifest() => (byte[])_hostManifest.Clone();

        internal void Initialize(string sourceName, byte[] bytecode, byte[]? hostManifest)
        {
            _sourceName = sourceName ?? throw new ArgumentNullException(nameof(sourceName));
            _bytecode = bytecode != null
                ? (byte[])bytecode.Clone()
                : throw new ArgumentNullException(nameof(bytecode));
            _hostManifest = hostManifest != null
                ? (byte[])hostManifest.Clone()
                : Array.Empty<byte>();
        }
    }
}
