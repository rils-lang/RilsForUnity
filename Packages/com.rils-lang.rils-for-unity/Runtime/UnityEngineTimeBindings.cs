#nullable enable
using Rils.CSharp;
using UnityEngine;

namespace RilsForUnity
{
    internal static class UnityEngineTimeBindings
    {
        internal static UnityHostBindingModule CreateModule()
        {
            const string capability = "unity_engine.time";
            return new UnityHostBindingModule(
                "unity_engine::time",
                Scalar(
                    "UnityEngine.CoreModule:UnityEngine.Time.get_deltaTime():System.Single",
                    "delta_time",
                    () => RilsValue.From(Time.deltaTime)),
                Scalar(
                    "UnityEngine.CoreModule:UnityEngine.Time.get_fixedDeltaTime():System.Single",
                    "fixed_delta_time",
                    () => RilsValue.From(Time.fixedDeltaTime)),
                Scalar(
                    "UnityEngine.CoreModule:UnityEngine.Time.get_time():System.Single",
                    "time",
                    () => RilsValue.From(Time.time)),
                new UnityHostFunctionBinding(
                    "UnityEngine.CoreModule:UnityEngine.Time.get_frameCount():System.Int32",
                    "unity_engine::time::frame_count",
                    capability,
                    new RilsHostParameter(RilsValueTag.I32),
                    System.Array.Empty<RilsHostParameter>(),
                    (_, _) => RilsValue.From(Time.frameCount)));
        }

        private static UnityHostFunctionBinding Scalar(
            string canonicalManagedName,
            string name,
            System.Func<RilsValue> value)
        {
            return new UnityHostFunctionBinding(
                canonicalManagedName,
                $"unity_engine::time::{name}",
                "unity_engine.time",
                new RilsHostParameter(RilsValueTag.F32),
                System.Array.Empty<RilsHostParameter>(),
                (_, _) => value());
        }
    }
}
