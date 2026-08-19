#nullable enable
using System;
using Rils.CSharp;
using UnityEngine;

namespace RilsForUnity
{
    /// Defines the first typed UnityEngine object modules. Logical object types
    /// currently lower to HostHandle in manifest v1 while remaining available to
    /// the binding model for a future nominal host-type manifest.
    public static class UnityObjectHostBindings
    {
        private static readonly RilsHostParameter ObjectType =
            RilsHostParameter.NamedHandle("unity_engine::Object");
        private static readonly RilsHostParameter GameObjectType =
            RilsHostParameter.NamedHandle("unity_engine::GameObject");
        private static readonly RilsHostParameter ComponentType =
            RilsHostParameter.NamedHandle("unity_engine::Component");
        private static readonly RilsHostParameter TransformType =
            RilsHostParameter.NamedHandle("unity_engine::Transform");

        /// Retained for host applications that registered the prototype object
        /// bindings directly. New hosts should register the complete catalog.
        public static void Register(
            RilsHostRegistry registry,
            UnityObjectHandleTable handles)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            if (handles == null) throw new ArgumentNullException(nameof(handles));
            UnityHostBindingModule[] modules = CreateModules();
            for (int index = 0; index < modules.Length; index++)
            {
                modules[index].Register(registry, handles);
            }
        }

        internal static UnityHostBindingModule[] CreateModules()
        {
            return new[]
            {
                CreateObjectModule(),
                CreateGameObjectModule(),
                CreateComponentModule(),
                CreateTransformModule(),
            };
        }

        private static UnityHostBindingModule CreateObjectModule()
        {
            const string capability = "unity_engine.object";
            return new UnityHostBindingModule(
                "unity_engine::object",
                new UnityHostFunctionBinding(
                    "UnityEngine.CoreModule:UnityEngine.Object.op_Implicit(UnityEngine.Object):System.Boolean",
                    "unity_engine::object::is_valid",
                    capability,
                    new RilsHostParameter(RilsValueTag.Bool),
                    new[] { ObjectType },
                    (handles, arguments) =>
                    {
                        RilsObjectHandle handle = arguments[0].AsHostHandle(handles.SessionId);
                        return RilsValue.From(handles.TryResolve<UnityEngine.Object>(handle, out _));
                    },
                    RilsHostReceiver.RefSelf),
                new UnityHostFunctionBinding(
                    "UnityEngine.CoreModule:UnityEngine.Object.GetInstanceID():System.Int32",
                    "unity_engine::object::instance_id",
                    capability,
                    new RilsHostParameter(RilsValueTag.I64),
                    new[] { ObjectType },
                    (handles, arguments) =>
                    {
                        UnityEngine.Object target = Resolve<UnityEngine.Object>(handles, arguments[0]);
                        return RilsValue.From((long)target.GetInstanceID());
                    },
                    RilsHostReceiver.RefSelf));
        }

        private static UnityHostBindingModule CreateGameObjectModule()
        {
            const string capability = "unity_engine.game_object";
            return new UnityHostBindingModule(
                "unity_engine::game_object",
                new UnityHostFunctionBinding(
                    "UnityEngine.CoreModule:UnityEngine.GameObject.get_activeSelf():System.Boolean",
                    "unity_engine::game_object::active_self",
                    capability,
                    new RilsHostParameter(RilsValueTag.Bool),
                    new[] { GameObjectType },
                    (handles, arguments) => RilsValue.From(
                        Resolve<GameObject>(handles, arguments[0]).activeSelf),
                    RilsHostReceiver.RefSelf),
                new UnityHostFunctionBinding(
                    "UnityEngine.CoreModule:UnityEngine.GameObject.SetActive(System.Boolean):System.Void",
                    "unity_engine::game_object::set_active",
                    capability,
                    new RilsHostParameter(RilsValueTag.Unit),
                    new[] { GameObjectType, new RilsHostParameter(RilsValueTag.Bool) },
                    (handles, arguments) =>
                    {
                        Resolve<GameObject>(handles, arguments[0]).SetActive(arguments[1].AsBool());
                        return RilsValue.Unit;
                    },
                    RilsHostReceiver.RefMutSelf),
                new UnityHostFunctionBinding(
                    "UnityEngine.CoreModule:UnityEngine.GameObject.get_transform():UnityEngine.Transform",
                    "unity_engine::game_object::transform",
                    capability,
                    TransformType,
                    new[] { GameObjectType },
                    (handles, arguments) => RilsValue.From(
                        handles.Acquire(Resolve<GameObject>(handles, arguments[0]).transform)),
                    RilsHostReceiver.RefSelf));
        }

        private static UnityHostBindingModule CreateComponentModule()
        {
            const string capability = "unity_engine.component";
            return new UnityHostBindingModule(
                "unity_engine::component",
                new UnityHostFunctionBinding(
                    "UnityEngine.CoreModule:UnityEngine.Component.get_gameObject():UnityEngine.GameObject",
                    "unity_engine::component::game_object",
                    capability,
                    GameObjectType,
                    new[] { ComponentType },
                    (handles, arguments) => RilsValue.From(
                        handles.Acquire(Resolve<Component>(handles, arguments[0]).gameObject)),
                    RilsHostReceiver.RefSelf));
        }

        private static UnityHostBindingModule CreateTransformModule()
        {
            const string capability = "unity_engine.transform";
            return new UnityHostBindingModule(
                "unity_engine::transform",
                TransformCoordinate("x", value => value.x),
                TransformCoordinate("y", value => value.y),
                TransformCoordinate("z", value => value.z),
                new UnityHostFunctionBinding(
                    "UnityEngine.CoreModule:UnityEngine.Transform.set_position(UnityEngine.Vector3):System.Void",
                    "unity_engine::transform::set_position",
                    capability,
                    new RilsHostParameter(RilsValueTag.Unit),
                    new[]
                    {
                        TransformType,
                        new RilsHostParameter(RilsValueTag.F32),
                        new RilsHostParameter(RilsValueTag.F32),
                        new RilsHostParameter(RilsValueTag.F32),
                    },
                    (handles, arguments) =>
                    {
                        Resolve<Transform>(handles, arguments[0]).position = new Vector3(
                            arguments[1].AsF32(),
                            arguments[2].AsF32(),
                            arguments[3].AsF32());
                        return RilsValue.Unit;
                    },
                    RilsHostReceiver.RefMutSelf));
        }

        private static UnityHostFunctionBinding TransformCoordinate(
            string name,
            Func<Vector3, float> getter)
        {
            return new UnityHostFunctionBinding(
                $"UnityEngine.CoreModule:UnityEngine.Transform.get_position().{name}:System.Single",
                $"unity_engine::transform::position_{name}",
                "unity_engine.transform",
                new RilsHostParameter(RilsValueTag.F32),
                new[] { TransformType },
                (handles, arguments) => getter(Resolve<Transform>(handles, arguments[0]).position),
                RilsHostReceiver.RefSelf);
        }

        private static T Resolve<T>(
            UnityObjectHandleTable handles,
            RilsValue value)
            where T : UnityEngine.Object
        {
            RilsObjectHandle handle = value.AsHostHandle(handles.SessionId);
            if (handles.TryResolve<T>(handle, out T? target)) return target!;
            throw new RilsHostException(new RilsHostError(
                RilsHostErrorCode.ObjectDestroyed,
                $"The Unity {typeof(T).Name} handle is no longer valid."));
        }
    }
}
