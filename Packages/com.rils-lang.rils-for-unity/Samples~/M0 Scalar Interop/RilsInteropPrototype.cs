using UnityEngine;
using Rils.CSharp;

namespace RilsForUnity.Samples
{
    /// Minimal M0 proof: C# invokes a Rils function and Rils invokes a scalar C# host function.
    public sealed class RilsInteropPrototype : MonoBehaviour
    {
        private RilsRuntime _runtime;
        private RilsHostRegistry _hosts;
        private RilsModule _module;
        private RilsInstance _instance;

        private void Start()
        {
            _runtime = new RilsRuntime();
            _hosts = new RilsHostRegistry(_runtime);
            _hosts.Register(new RilsHostFunction(
                100,
                "unity::math::add",
                "unity.math",
                RilsValueTag.I32,
                new[] { RilsValueTag.I32, RilsValueTag.I32 },
                arguments => RilsValue.From(arguments[0].AsI32() + arguments[1].AsI32())));
            _hosts.AllowCapability("unity.math");
            _hosts.Freeze();

            _module = _runtime.Compile(@"
                pub fn double(value: i32) -> i32 { value * 2 }
                pub fn host_add(value: i32) -> i32 {
                    unity::math::add(value, 2)
                }
            ", "M0Interop.rils");
            _module.ValidateHost();
            _instance = _module.CreateInstance();

            int fromRils = _instance.Call("double", 21).AsI32();
            int fromHost = _instance.Call("host_add", 40).AsI32();
            Debug.Log($"RilsForUnity M0 interop: C# -> Rils = {fromRils}, Rils -> C# = {fromHost}");
        }

        private void OnDestroy()
        {
            // The native runtime owns the frozen callback until it is destroyed.
            _instance?.Dispose();
            _module?.Dispose();
            _runtime?.Dispose();
            _hosts?.Dispose();
        }
    }
}
