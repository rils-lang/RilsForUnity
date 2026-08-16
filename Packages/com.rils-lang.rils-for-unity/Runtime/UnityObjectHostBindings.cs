#nullable enable
using System;
using Rils.CSharp;
using UnityEngine;

namespace RilsForUnity
{
    /// Registers the first host-neutral object queries backed by Unity handles.
    public static class UnityObjectHostBindings
    {
        public static void Register(RilsHostRegistry registry, UnityObjectHandleTable handles)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            if (handles == null) throw new ArgumentNullException(nameof(handles));

            registry.Register(new RilsHostFunction(
                103,
                "unity::object::is_valid",
                "unity.object",
                new RilsHostParameter(RilsValueTag.Bool),
                new[] { new RilsHostParameter(RilsValueTag.HostHandle, RilsHostTransferMode.Handle) },
                arguments =>
                {
                    RilsObjectHandle handle = arguments[0].AsHostHandle(handles.SessionId);
                    return RilsValue.From(handles.TryResolve<UnityEngine.Object>(handle, out _));
                }));

            registry.Register(new RilsHostFunction(
                104,
                "unity::object::instance_id",
                "unity.object",
                new RilsHostParameter(RilsValueTag.I64),
                new[] { new RilsHostParameter(RilsValueTag.HostHandle, RilsHostTransferMode.Handle) },
                arguments =>
                {
                    RilsObjectHandle handle = arguments[0].AsHostHandle(handles.SessionId);
                    if (!handles.TryResolve<UnityEngine.Object>(handle, out UnityEngine.Object? target))
                    {
                        throw new RilsHostException(new RilsHostError(
                            RilsHostErrorCode.ObjectDestroyed,
                            "The Unity object handle is no longer valid."));
                    }
                    return RilsValue.From((long)target.GetInstanceID());
                }));
        }
    }
}
