#nullable enable
using System;
using Rils.CSharp;
using RilsForUnity;
using UnityEngine;

namespace Rils.Unity
{
    /// Runs a compiled RilsBytecodeAsset using Unity lifecycle conventions.
    public sealed class RilsBehaviour : MonoBehaviour
    {
        [SerializeField] private RilsBytecodeAsset? _script;
        [SerializeField] private bool _disableOnError = true;

        private RilsRuntime? _runtime;
        private RilsHostRegistry? _hosts;
        private RilsModule? _module;
        private RilsInstance? _instance;
        private UnityObjectHandleTable? _handles;
        private RilsObjectHandle _selfHandle;
        private bool _destroyed;

        public RilsBytecodeAsset? Script => _script;

        private void Awake()
        {
            if (_script == null)
            {
                Debug.LogWarning("RilsBehaviour has no RilsBytecodeAsset assigned.", this);
                return;
            }

            try
            {
                _runtime = new RilsRuntime();
                byte[] hostManifest = _script.GetHostManifest();
                if (hostManifest.Length != 0)
                {
                    _runtime.RegisterHostManifest(hostManifest);
                }
                _handles = new UnityObjectHandleTable();
                _selfHandle = _handles.Acquire(gameObject);
                _hosts = new RilsHostRegistry(_runtime);
                UnityObjectHostBindings.Register(_hosts, _handles);
                _hosts.AllowCapability("unity.object");
                _hosts.AllowCapability("unity.game_object");
                _hosts.AllowCapability("unity.transform");
                _hosts.AllowCapability("unity.component");
                _hosts.Freeze();
                _module = _runtime.LoadBytecode(_script.GetBytecode());
                _module.ValidateHost();
                _instance = _module.CreateInstance();
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
            if (!Has(RilsLifecycleFlags.Update) || _instance == null) return;
            try
            {
                _instance.Call("update", RilsValue.From(_selfHandle), RilsValue.From(Time.deltaTime));
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
            _instance?.Dispose();
            _module?.Dispose();
            _runtime?.Dispose();
            _hosts?.Dispose();
            _handles?.Dispose();
        }

        private void InvokeIfPresent(RilsLifecycleFlags flag, string functionName)
        {
            if (!Has(flag) || _instance == null || _destroyed) return;
            try
            {
                _instance.Call(functionName, RilsValue.From(_selfHandle));
            }
            catch (Exception exception)
            {
                Fail(functionName, exception);
            }
        }

        private bool Has(RilsLifecycleFlags flag) =>
            _script != null && (_script.LifecycleFlags & flag) != 0;

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
