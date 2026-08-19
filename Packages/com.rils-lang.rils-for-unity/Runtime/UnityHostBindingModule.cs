#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Rils.CSharp;

namespace RilsForUnity
{
    /// A Unity host module whose declarations can be serialized without creating
    /// runtime state and whose handlers are attached only when a player starts.
    public sealed class UnityHostBindingModule
    {
        private readonly IReadOnlyList<UnityHostFunctionBinding> _bindings;

        internal UnityHostBindingModule(
            string name,
            params UnityHostFunctionBinding[] bindings)
        {
            if (bindings == null) throw new ArgumentNullException(nameof(bindings));
            _bindings = Array.AsReadOnly((UnityHostFunctionBinding[])bindings.Clone());
            Descriptor = new RilsHostModuleDescriptor(
                name,
                1,
                _bindings.Select(binding => binding.Descriptor).ToArray());
        }

        public RilsHostModuleDescriptor Descriptor { get; }

        public void Register(
            RilsHostRegistry registry,
            UnityObjectHandleTable handles)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            if (handles == null) throw new ArgumentNullException(nameof(handles));
            for (int index = 0; index < _bindings.Count; index++)
            {
                UnityHostFunctionBinding binding = _bindings[index];
                registry.Register(new RilsHostFunction(
                    binding.Descriptor,
                    arguments => binding.Handler(handles, arguments)));
            }
        }
    }

    internal sealed class UnityHostFunctionBinding
    {
        internal UnityHostFunctionBinding(
            string canonicalManagedName,
            string rilsName,
            string capability,
            RilsHostParameter returnParameter,
            IReadOnlyList<RilsHostParameter> parameters,
            Func<UnityObjectHandleTable, RilsValue[], RilsValue> handler,
            RilsHostReceiver receiver = RilsHostReceiver.None)
        {
            Handler = handler ?? throw new ArgumentNullException(nameof(handler));
            Descriptor = new RilsHostFunctionDescriptor(
                RilsHostStableId.FromCanonicalName(canonicalManagedName),
                rilsName,
                capability,
                returnParameter,
                parameters,
                receiver: receiver,
                managedMemberName: canonicalManagedName);
        }

        internal RilsHostFunctionDescriptor Descriptor { get; }
        internal Func<UnityObjectHandleTable, RilsValue[], RilsValue> Handler { get; }
    }
}
