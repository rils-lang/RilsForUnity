using System;
using UnityEngine;

namespace Rils.Unity
{
    [PreferBinarySerialization]
    public sealed class RilsBytecodeAsset : ScriptableObject
    {
        [SerializeField]
        private string _sourceName = string.Empty;

        [SerializeField, HideInInspector]
        private byte[] _bytecode = Array.Empty<byte>();

        public string SourceName => _sourceName;

        public int BytecodeLength => _bytecode.Length;

        public byte[] GetBytecode()
        {
            return (byte[])_bytecode.Clone();
        }

        internal void Initialize(string sourceName, byte[] bytecode)
        {
            _sourceName = sourceName ?? throw new ArgumentNullException(nameof(sourceName));
            _bytecode = bytecode != null
                ? (byte[])bytecode.Clone()
                : throw new ArgumentNullException(nameof(bytecode));
        }
    }
}
