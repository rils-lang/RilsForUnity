#nullable enable
using System;
using UnityEngine;

namespace Rils.Unity
{
    /// The Unity representation of one imported Rils .rilslib artifact.
    [PreferBinarySerialization]
    public sealed class RilsLibraryAsset : ScriptableObject
    {
        [SerializeField]
        private string _libraryName = string.Empty;

        [SerializeField, HideInInspector]
        private string _contentHash = string.Empty;

        [SerializeField, HideInInspector]
        private byte[] _library = Array.Empty<byte>();

        public string LibraryName => _libraryName;

        public string ContentHash => _contentHash;

        public int ByteLength => _library.Length;

        public byte[] GetLibraryBytes() => (byte[])_library.Clone();

        internal void Initialize(string libraryName, string contentHash, byte[] library)
        {
            _libraryName = !string.IsNullOrWhiteSpace(libraryName)
                ? libraryName
                : throw new ArgumentException("Library name cannot be empty.", nameof(libraryName));
            _contentHash = !string.IsNullOrWhiteSpace(contentHash)
                ? contentHash
                : throw new ArgumentException("Content hash cannot be empty.", nameof(contentHash));
            _library = library != null
                ? (byte[])library.Clone()
                : throw new ArgumentNullException(nameof(library));
        }
    }
}
