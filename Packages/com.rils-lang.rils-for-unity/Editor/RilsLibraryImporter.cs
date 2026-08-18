#nullable enable
using System;
using System.IO;
using System.Text;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Rils.Unity.Editor
{
    [ScriptedImporter(1, "rilslib")]
    internal sealed class RilsLibraryImporter : ScriptedImporter
    {
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("RILSLIB\0");
        private const int HeaderLength = 64;
        private const uint Fnv1A128HashAlgorithm = 1;
        private const int MaximumLibraryBytes = 64 * 1024 * 1024;

        public override void OnImportAsset(AssetImportContext context)
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(context.assetPath);
                string libraryName = ReadLibraryName(bytes, out string contentHash);
                var asset = ScriptableObject.CreateInstance<RilsLibraryAsset>();
                asset.name = libraryName;
                asset.Initialize(libraryName, contentHash, bytes);
                context.AddObjectToAsset("library", asset);
                context.SetMainObject(asset);
            }
            catch (Exception exception)
            {
                context.LogImportError(
                    $"Failed to import Rils library '{context.assetPath}': {exception.Message}");
            }
        }

        private static string ReadLibraryName(byte[] bytes, out string contentHash)
        {
            if (bytes.Length < HeaderLength || bytes.Length > MaximumLibraryBytes)
            {
                throw new InvalidDataException("The Rils library has an invalid size.");
            }
            for (int index = 0; index < Magic.Length; index++)
            {
                if (bytes[index] != Magic[index])
                {
                    throw new InvalidDataException("The Rils library has invalid magic bytes.");
                }
            }
            if (ReadUInt16(bytes, 8) != 1 || ReadUInt16(bytes, 10) != HeaderLength)
            {
                throw new InvalidDataException("The Rils library format is not supported.");
            }
            if (ReadUInt32(bytes, 40) != Fnv1A128HashAlgorithm || ReadUInt32(bytes, 44) != 0)
            {
                throw new InvalidDataException("The Rils library content hash is not supported.");
            }
            int nameLength = checked((int)ReadUInt32(bytes, 28));
            int moduleLength = checked((int)ReadUInt32(bytes, 32));
            int payloadEnd = checked(HeaderLength + nameLength + moduleLength);
            if (nameLength <= 0 || nameLength > 255 || payloadEnd != bytes.Length)
            {
                throw new InvalidDataException("The Rils library payload length is invalid.");
            }
            if (ReadUInt32(bytes, 36) != ComputeCrc32(bytes, HeaderLength, bytes.Length - HeaderLength))
            {
                throw new InvalidDataException("The Rils library payload checksum is invalid.");
            }
            ComputeFnv1A128(bytes, HeaderLength, bytes.Length - HeaderLength, out ulong hashLow, out ulong hashHigh);
            if (ReadUInt64(bytes, 48) != hashLow || ReadUInt64(bytes, 56) != hashHigh)
            {
                throw new InvalidDataException("The Rils library content hash is invalid.");
            }
            contentHash = hashHigh.ToString("x16") + hashLow.ToString("x16");
            return new UTF8Encoding(false, true).GetString(bytes, HeaderLength, nameLength);
        }

        private static uint ComputeCrc32(byte[] bytes, int offset, int count)
        {
            uint crc = uint.MaxValue;
            for (int index = offset; index < offset + count; index++)
            {
                crc ^= bytes[index];
                for (int bit = 0; bit < 8; bit++)
                {
                    crc = (crc >> 1) ^ (0xedb88320u & (uint)-(int)(crc & 1));
                }
            }
            return ~crc;
        }

        private static void ComputeFnv1A128(
            byte[] bytes,
            int offset,
            int count,
            out ulong low,
            out ulong high)
        {
            low = 0x62b821756295c58dUL;
            high = 0x6c62272e07bb0142UL;
            for (int index = offset; index < offset + count; index++)
            {
                low ^= bytes[index];
                ulong carry = MultiplyHighBy315(low);
                high = unchecked((high * 315UL) + carry + (low << 24));
                low = unchecked(low * 315UL);
            }
        }

        private static ulong MultiplyHighBy315(ulong value)
        {
            ulong lowProduct = (value & uint.MaxValue) * 315UL;
            ulong combined = (value >> 32) * 315UL + (lowProduct >> 32);
            return combined >> 32;
        }

        private static ushort ReadUInt16(byte[] bytes, int offset) =>
            (ushort)(bytes[offset] | (bytes[offset + 1] << 8));

        private static uint ReadUInt32(byte[] bytes, int offset) =>
            (uint)(bytes[offset]
                | (bytes[offset + 1] << 8)
                | (bytes[offset + 2] << 16)
                | (bytes[offset + 3] << 24));

        private static ulong ReadUInt64(byte[] bytes, int offset) =>
            ReadUInt32(bytes, offset) | ((ulong)ReadUInt32(bytes, offset + 4) << 32);
    }
}
