#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Rils.CSharp;

namespace RilsForUnity
{
    /// The single catalog consumed by both Editor manifest generation and Player binding.
    public static class UnityEngineBindingCatalog
    {
        private static readonly UnityHostBindingModule[] BindingModules = CreateModules();
        private static readonly IReadOnlyList<UnityHostBindingModule> ReadOnlyBindingModules =
            Array.AsReadOnly(BindingModules);
        private static readonly string[] BindingCapabilities = BindingModules
            .SelectMany(module => module.Descriptor.Functions)
            .Select(function => function.Capability)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(capability => capability, StringComparer.Ordinal)
            .ToArray();
        private static readonly IReadOnlyList<string> ReadOnlyBindingCapabilities =
            Array.AsReadOnly(BindingCapabilities);

        public static IReadOnlyList<UnityHostBindingModule> Modules => ReadOnlyBindingModules;
        public static IReadOnlyList<string> Capabilities => ReadOnlyBindingCapabilities;

        public static void RegisterAll(
            RilsHostRegistry registry,
            UnityObjectHandleTable handles)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            if (handles == null) throw new ArgumentNullException(nameof(handles));
            for (int index = 0; index < BindingModules.Length; index++)
            {
                BindingModules[index].Register(registry, handles);
            }
        }

        public static void AllowAllCapabilities(RilsHostRegistry registry)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            for (int index = 0; index < BindingCapabilities.Length; index++)
            {
                registry.AllowCapability(BindingCapabilities[index]);
            }
        }

        public static void AllowAllCapabilities(RilsRuntime runtime)
        {
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));
            for (int index = 0; index < BindingCapabilities.Length; index++)
            {
                runtime.AllowCapability(BindingCapabilities[index]);
            }
        }

        private static UnityHostBindingModule[] CreateModules()
        {
            UnityHostBindingModule[] objects = UnityObjectHostBindings.CreateModules();
            var modules = new UnityHostBindingModule[objects.Length + 1];
            Array.Copy(objects, modules, objects.Length);
            modules[objects.Length] = UnityEngineTimeBindings.CreateModule();
            return modules;
        }
    }
}
