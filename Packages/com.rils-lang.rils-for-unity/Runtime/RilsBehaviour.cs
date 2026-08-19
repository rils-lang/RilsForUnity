#nullable enable
using System;
using Rils.CSharp;
using RilsForUnity;
using UnityEngine;

namespace Rils.Unity
{
    /// Runs one imported RilsBehaviour entry using Unity lifecycle conventions.
    public sealed class RilsBehaviour : MonoBehaviour
    {
        [SerializeField] private RilsEntryAsset? _entry;
        [SerializeField] private bool _disableOnError = true;

        private RilsRuntime? _runtime;
        private RilsHostRegistry? _hosts;
        private RilsModule? _module;
        private RilsInstance? _instance;
        private RilsScriptValue? _state;
        private UnityObjectHandleTable? _handles;
        private RilsObjectHandle _selfHandle;
        private bool _destroyed;

        public RilsEntryAsset? Entry => _entry;

        private void Awake()
        {
            if (_entry == null)
            {
                Debug.LogWarning("RilsBehaviour has no RilsEntryAsset assigned.", this);
                return;
            }

            try
            {
                _runtime = new RilsRuntime();
                byte[] hostManifest = _entry.GetHostManifest();
                if (hostManifest.Length != 0)
                {
                    _runtime.RegisterHostManifest(hostManifest);
                }
                _handles = new UnityObjectHandleTable();
                _selfHandle = _handles.Acquire(gameObject);
                _hosts = new RilsHostRegistry(_runtime);
                UnityEngineBindingCatalog.RegisterAll(_hosts, _handles);
                UnityEngineBindingCatalog.AllowAllCapabilities(_hosts);
                _hosts.Freeze();
                _module = _runtime.LoadBytecode(_entry.GetBytecode());
                _module.ValidateHost();
                _instance = _module.CreateInstance();
                _state = _instance.CreateDefaultValue(_entry.EntryId);
                InvokeIfPresent(RilsLifecycleFlags.Awake, "awake");
            }
            catch (Exception exception)
            {
                Fail("awake", exception);
            }
        }

        private void Start() => InvokeIfPresent(RilsLifecycleFlags.Start, "start");

        private void Update()
        {
            if (!Has(RilsLifecycleFlags.Update) || _state == null) return;
            try
            {
                _state.CallTrait(
                    "RilsBehaviour",
                    "update",
                    RilsValue.From(_selfHandle),
                    RilsValue.From(Time.deltaTime));
            }
            catch (Exception exception)
            {
                Fail("update", exception);
            }
        }

        private void OnDestroy()
        {
            if (_destroyed) return;
            InvokeIfPresent(RilsLifecycleFlags.OnDestroy, "on_destroy");
            _destroyed = true;
            _state?.Dispose();
            _instance?.Dispose();
            _module?.Dispose();
            _runtime?.Dispose();
            _hosts?.Dispose();
            _handles?.Dispose();
        }

        private void InvokeIfPresent(RilsLifecycleFlags flag, string functionName)
        {
            if (!Has(flag) || _state == null || _destroyed) return;
            try
            {
                _state.CallTrait(
                    "RilsBehaviour",
                    functionName,
                    RilsValue.From(_selfHandle));
            }
            catch (Exception exception)
            {
                Fail(functionName, exception);
            }
        }

        private bool Has(RilsLifecycleFlags flag) =>
            _entry != null && (_entry.LifecycleFlags & flag) != 0;

        private void Fail(string functionName, Exception exception)
        {
            Debug.LogException(
                new InvalidOperationException(
                    $"Rils lifecycle function '{functionName}' failed for '{name}'.", exception),
                this);
            if (_disableOnError) enabled = false;
        }
    }
}
