#nullable enable
using System;
using System.Collections.Generic;
using Rils.CSharp;
using UnityEngine;

namespace RilsForUnity
{
    /// Owns the Unity-side mapping for opaque Rils object handles.
    /// Handles are runtime-session bound and never transfer Unity object ownership.
    public sealed class UnityObjectHandleTable : IDisposable
    {
        private sealed class Entry
        {
            public Entry(UnityEngine.Object target, uint generation, uint typeId)
            {
                Target = new WeakReference<UnityEngine.Object>(target);
                Generation = generation;
                TypeId = typeId;
            }

            public WeakReference<UnityEngine.Object> Target { get; }
            public uint Generation { get; }
            public uint TypeId { get; }
        }

        private readonly Dictionary<int, Entry> _entries = new Dictionary<int, Entry>();
        private readonly Dictionary<int, uint> _generations = new Dictionary<int, uint>();
        private bool _disposed;

        public UnityObjectHandleTable()
        {
            SessionId = CreateSessionId();
        }

        public ulong SessionId { get; }

        public RilsObjectHandle Acquire(UnityEngine.Object target)
        {
            EnsureOpen();
            if (target == null) throw new ArgumentNullException(nameof(target));

            int objectId = target.GetInstanceID();
            if (objectId == 0) throw new InvalidOperationException("Unity returned an invalid instance ID.");
            if (_entries.TryGetValue(objectId, out Entry? existing) &&
                existing.Target.TryGetTarget(out UnityEngine.Object? current) && current != null)
            {
                if (current.GetType() != target.GetType())
                {
                    throw new InvalidOperationException("A Unity instance ID is already bound to another type.");
                }
                return new RilsObjectHandle(SessionId, objectId, existing.Generation, existing.TypeId);
            }

            uint generation = NextGeneration(objectId);
            uint typeId = StableTypeId(target.GetType());
            _entries[objectId] = new Entry(target, generation, typeId);
            return new RilsObjectHandle(SessionId, objectId, generation, typeId);
        }

        public bool TryResolve<T>(RilsObjectHandle handle, out T? target) where T : UnityEngine.Object
        {
            target = null;
            if (_disposed || handle.SessionId != SessionId || handle.ObjectId == 0)
            {
                return false;
            }
            if (!_entries.TryGetValue(checked((int)handle.ObjectId), out Entry? entry) ||
                entry.Generation != handle.Generation || entry.TypeId != handle.TypeId ||
                !entry.Target.TryGetTarget(out UnityEngine.Object? candidate) || candidate == null)
            {
                return false;
            }
            target = candidate as T;
            return target != null;
        }

        public bool Invalidate(RilsObjectHandle handle)
        {
            if (_disposed || handle.SessionId != SessionId || handle.ObjectId == 0)
            {
                return false;
            }
            if (!_entries.TryGetValue(checked((int)handle.ObjectId), out Entry? entry) ||
                entry.Generation != handle.Generation || entry.TypeId != handle.TypeId)
            {
                return false;
            }
            _entries.Remove(checked((int)handle.ObjectId));
            return true;
        }

        public void PruneDestroyed()
        {
            if (_disposed) return;
            var destroyed = new List<int>();
            foreach (KeyValuePair<int, Entry> pair in _entries)
            {
                if (!pair.Value.Target.TryGetTarget(out UnityEngine.Object? target) || target == null)
                {
                    destroyed.Add(pair.Key);
                }
            }
            foreach (int objectId in destroyed) _entries.Remove(objectId);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _entries.Clear();
            _generations.Clear();
            _disposed = true;
        }

        private uint NextGeneration(int objectId)
        {
            uint generation = _generations.TryGetValue(objectId, out uint previous)
                ? checked(previous + 1)
                : 1;
            _generations[objectId] = generation;
            return generation;
        }

        private void EnsureOpen()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(UnityObjectHandleTable));
        }

        private static ulong CreateSessionId()
        {
            ulong value = unchecked((ulong)DateTime.UtcNow.Ticks);
            value ^= unchecked((ulong)Guid.NewGuid().GetHashCode()) << 32;
            return value == 0 ? 1UL : value;
        }

        private static uint StableTypeId(Type type)
        {
            const uint offset = 2166136261;
            const uint prime = 16777619;
            uint hash = offset;
            string name = type.FullName ?? type.Name;
            for (int index = 0; index < name.Length; index++)
            {
                hash ^= name[index];
                hash *= prime;
            }
            return hash == 0 ? 1U : hash;
        }
    }
}
