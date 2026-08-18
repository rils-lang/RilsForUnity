using System;
using System.Threading.Tasks;
using Rils.CSharp;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Rils.Unity.Addressables
{
    public sealed class RilsAddressableInstance : IDisposable
    {
        private RilsRuntime? _runtime;
        private readonly RilsInstance _instance;

        private RilsAddressableInstance(
            string sourceName,
            RilsRuntime runtime,
            RilsInstance instance)
        {
            SourceName = sourceName;
            _runtime = runtime;
            _instance = instance;
        }

        public string SourceName { get; }

        public bool IsDisposed => _runtime == null;

        public static async Task<RilsAddressableInstance> LoadAsync(object key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            AsyncOperationHandle<RilsEntryAsset> handle =
                UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<RilsEntryAsset>(key);
            try
            {
                RilsEntryAsset asset = await handle.Task;
                if (handle.Status != AsyncOperationStatus.Succeeded || asset == null)
                {
                    throw handle.OperationException ??
                        new InvalidOperationException($"Failed to load Rils bytecode asset '{key}'.");
                }

                var runtime = new RilsRuntime();
                try
                {
                    RilsModule module = runtime.LoadBytecode(asset.GetBytecode());
                    RilsInstance instance = module.CreateInstance();
                    return new RilsAddressableInstance(asset.SourceName, runtime, instance);
                }
                catch
                {
                    runtime.Dispose();
                    throw;
                }
            }
            finally
            {
                if (handle.IsValid())
                {
                    UnityEngine.AddressableAssets.Addressables.Release(handle);
                }
            }
        }

        public RilsValue Execute()
        {
            EnsureUsable();
            return _instance.Execute();
        }

        public RilsValue Call(string functionName, params RilsValue[] arguments)
        {
            EnsureUsable();
            return _instance.Call(functionName, arguments);
        }

        public void Dispose()
        {
            if (_runtime == null)
            {
                return;
            }

            _runtime.Dispose();
            _runtime = null;
        }

        private void EnsureUsable()
        {
            if (_runtime == null)
            {
                throw new ObjectDisposedException(nameof(RilsAddressableInstance));
            }
        }
    }
}
