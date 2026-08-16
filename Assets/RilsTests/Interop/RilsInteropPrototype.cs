using System;
using UnityEngine;
using Rils.CSharp;

namespace RilsForUnity.Tests
{
    /// Minimal M0 proof: C# invokes a Rils function and Rils invokes a scalar C# host function.
    public sealed class RilsInteropPrototype : MonoBehaviour
    {
        private RilsRuntime _runtime;
        private RilsHostRegistry _hosts;
        private RilsModule _module;
        private RilsInstance _instance;
        private UnityObjectHandleTable _handles;

        private void Start()
        {
            _runtime = new RilsRuntime();
            _handles = new UnityObjectHandleTable();
            RilsObjectHandle selfHandle = _handles.Acquire(gameObject);
            if (!_handles.TryResolve<GameObject>(selfHandle, out GameObject resolved) || resolved != gameObject)
            {
                throw new InvalidOperationException("Unity object handle round-trip failed.");
            }
            Debug.Log($"RilsForUnity interop handle: session={selfHandle.SessionId}, type={selfHandle.TypeId}");

            _hosts = new RilsHostRegistry(_runtime);
            _hosts.Register(new RilsHostFunction(
                100,
                "unity::math::add",
                "unity.math",
                new RilsHostParameter(RilsValueTag.I32),
                new[]
                {
                    new RilsHostParameter(RilsValueTag.I32),
                    new RilsHostParameter(RilsValueTag.I32),
                },
                arguments => RilsValue.From(arguments[0].AsI32() + arguments[1].AsI32())));
            _hosts.Register(new RilsHostFunction(
                101,
                "unity::object::self_handle",
                "unity.object",
                new RilsHostParameter(RilsValueTag.HostHandle, RilsHostTransferMode.Handle),
                Array.Empty<RilsHostParameter>(),
                arguments => RilsValue.From(selfHandle)));
            _hosts.Register(new RilsHostFunction(
                102,
                "unity::object::echo_handle",
                "unity.object",
                new RilsHostParameter(RilsValueTag.HostHandle, RilsHostTransferMode.Handle),
                new[] { new RilsHostParameter(RilsValueTag.HostHandle, RilsHostTransferMode.Handle) },
                arguments =>
                {
                    RilsObjectHandle handle = arguments[0].AsHostHandle(_handles.SessionId);
                    if (!_handles.TryResolve<GameObject>(handle, out _))
                    {
                        throw new RilsHostException(new RilsHostError(
                            RilsHostErrorCode.ObjectDestroyed,
                            "The Unity object handle is no longer valid."));
                    }
                    return RilsValue.From(handle);
                }));
            UnityObjectHostBindings.Register(_hosts, _handles);
            _hosts.AllowCapability("unity.math");
            _hosts.AllowCapability("unity.object");
            _hosts.Freeze();

            _module = _runtime.Compile(@"
                pub fn double(value: i32) -> i32 { value * 2 }
                pub fn host_add(value: i32) -> i32 {
                    unity::math::add(value, 2)
                }
                pub fn get_handle() -> HostHandle {
                    unity::object::self_handle()
                }
                pub fn echo_handle(handle: HostHandle) -> HostHandle {
                    unity::object::echo_handle(handle)
                }
                pub fn is_valid(handle: HostHandle) -> bool {
                    unity::object::is_valid(handle)
                }
                pub fn instance_id(handle: HostHandle) -> i64 {
                    unity::object::instance_id(handle)
                }
            ", "M0Interop.rils");
            _module.ValidateHost();
            _instance = _module.CreateInstance();

            int fromRils = _instance.Call("double", 21).AsI32();
            int fromHost = _instance.Call("host_add", 40).AsI32();
            RilsObjectHandle fromRilsHandle = _instance.Call("get_handle").AsHostHandle(_handles.SessionId);
            RilsObjectHandle echoedHandle = _instance.Call("echo_handle", RilsValue.From(fromRilsHandle))
                .AsHostHandle(_handles.SessionId);
            if (!_handles.TryResolve<GameObject>(echoedHandle, out _))
            {
                throw new InvalidOperationException("Rils host-handle round-trip failed.");
            }
            bool isValid = _instance.Call("is_valid", RilsValue.From(echoedHandle)).AsBool();
            long instanceId = _instance.Call("instance_id", RilsValue.From(echoedHandle)).AsI64();
            if (!isValid || instanceId != gameObject.GetInstanceID())
            {
                throw new InvalidOperationException("Unity object query binding failed.");
            }
            Debug.Log($"RilsForUnity M0 interop: C# -> Rils = {fromRils}, Rils -> C# = {fromHost}");
            Debug.Log($"RilsForUnity host handle round-trip: object={echoedHandle.ObjectId}");
            Debug.Log($"RilsForUnity object query: valid={isValid}, instance_id={instanceId}");
        }

        private void OnDestroy()
        {
            // The native runtime owns the frozen callback until it is destroyed.
            _instance?.Dispose();
            _module?.Dispose();
            _runtime?.Dispose();
            _hosts?.Dispose();
            _handles?.Dispose();
        }
    }
}
