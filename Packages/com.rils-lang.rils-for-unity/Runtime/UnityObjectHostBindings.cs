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
                },
                receiver: RilsHostReceiver.RefSelf));

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
                },
                receiver: RilsHostReceiver.RefSelf));

            registry.Register(new RilsHostFunction(
                105,
                "unity::game_object::active_self",
                "unity.game_object",
                new RilsHostParameter(RilsValueTag.Bool),
                new[] { new RilsHostParameter(RilsValueTag.HostHandle, RilsHostTransferMode.Handle) },
                arguments =>
                {
                    RilsObjectHandle handle = arguments[0].AsHostHandle(handles.SessionId);
                    if (!handles.TryResolve<GameObject>(handle, out GameObject? target))
                    {
                        throw new RilsHostException(new RilsHostError(
                            RilsHostErrorCode.ObjectDestroyed,
                            "The GameObject handle is no longer valid."));
                    }
                    return RilsValue.From(target.activeSelf);
                },
                receiver: RilsHostReceiver.RefSelf));

            registry.Register(new RilsHostFunction(
                106,
                "unity::game_object::set_active",
                "unity.game_object",
                new RilsHostParameter(RilsValueTag.Unit),
                new[]
                {
                    new RilsHostParameter(RilsValueTag.HostHandle, RilsHostTransferMode.Handle),
                    new RilsHostParameter(RilsValueTag.Bool),
                },
                arguments =>
                {
                    RilsObjectHandle handle = arguments[0].AsHostHandle(handles.SessionId);
                    if (!handles.TryResolve<GameObject>(handle, out GameObject? target))
                    {
                        throw new RilsHostException(new RilsHostError(
                            RilsHostErrorCode.ObjectDestroyed,
                            "The GameObject handle is no longer valid."));
                    }
                    target.SetActive(arguments[1].AsBool());
                    return RilsValue.Unit;
                },
                receiver: RilsHostReceiver.RefMutSelf));

            registry.Register(new RilsHostFunction(
                107,
                "unity::game_object::transform",
                "unity.game_object",
                new RilsHostParameter(RilsValueTag.HostHandle, RilsHostTransferMode.Handle),
                new[] { new RilsHostParameter(RilsValueTag.HostHandle, RilsHostTransferMode.Handle) },
                arguments =>
                {
                    RilsObjectHandle handle = arguments[0].AsHostHandle(handles.SessionId);
                    if (!handles.TryResolve<GameObject>(handle, out GameObject? target))
                    {
                        throw new RilsHostException(new RilsHostError(
                            RilsHostErrorCode.ObjectDestroyed,
                            "The GameObject handle is no longer valid."));
                    }
                    return RilsValue.From(handles.Acquire(target.transform));
                },
                receiver: RilsHostReceiver.RefSelf));

            RegisterTransformCoordinate(registry, handles, 108, "x", value => value.x);
            RegisterTransformCoordinate(registry, handles, 109, "y", value => value.y);
            RegisterTransformCoordinate(registry, handles, 110, "z", value => value.z);

            registry.Register(new RilsHostFunction(
                111,
                "unity::transform::set_position",
                "unity.transform",
                new RilsHostParameter(RilsValueTag.Unit),
                new[]
                {
                    new RilsHostParameter(RilsValueTag.HostHandle, RilsHostTransferMode.Handle),
                    new RilsHostParameter(RilsValueTag.F32),
                    new RilsHostParameter(RilsValueTag.F32),
                    new RilsHostParameter(RilsValueTag.F32),
                },
                arguments =>
                {
                    Transform target = ResolveTransform(handles, arguments[0]);
                    target.position = new Vector3(
                        arguments[1].AsF32(), arguments[2].AsF32(), arguments[3].AsF32());
                    return RilsValue.Unit;
                },
                receiver: RilsHostReceiver.RefMutSelf));

            registry.Register(new RilsHostFunction(
                112,
                "unity::component::game_object",
                "unity.component",
                new RilsHostParameter(RilsValueTag.HostHandle, RilsHostTransferMode.Handle),
                new[] { new RilsHostParameter(RilsValueTag.HostHandle, RilsHostTransferMode.Handle) },
                arguments =>
                {
                    RilsObjectHandle handle = arguments[0].AsHostHandle(handles.SessionId);
                    if (!handles.TryResolve<Component>(handle, out Component? target))
                    {
                        throw new RilsHostException(new RilsHostError(
                            RilsHostErrorCode.ObjectDestroyed,
                            "The Component handle is no longer valid."));
                    }
                    return RilsValue.From(handles.Acquire(target.gameObject));
                },
                receiver: RilsHostReceiver.RefSelf));
        }

        private static void RegisterTransformCoordinate(
            RilsHostRegistry registry,
            UnityObjectHandleTable handles,
            ulong functionId,
            string name,
            Func<Vector3, float> getter)
        {
            registry.Register(new RilsHostFunction(
                functionId,
                $"unity::transform::position_{name}",
                "unity.transform",
                new RilsHostParameter(RilsValueTag.F32),
                new[] { new RilsHostParameter(RilsValueTag.HostHandle, RilsHostTransferMode.Handle) },
                arguments => getter(ResolveTransform(handles, arguments[0]).position),
                receiver: RilsHostReceiver.RefSelf));
        }

        private static Transform ResolveTransform(
            UnityObjectHandleTable handles,
            RilsValue value)
        {
            RilsObjectHandle handle = value.AsHostHandle(handles.SessionId);
            if (handles.TryResolve<Transform>(handle, out Transform? target)) return target;
            throw new RilsHostException(new RilsHostError(
                RilsHostErrorCode.ObjectDestroyed,
                "The Transform handle is no longer valid."));
        }
    }
}
