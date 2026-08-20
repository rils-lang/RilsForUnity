#nullable enable
using System;
using System.Collections.Generic;
using Rils.CSharp;

namespace RilsForUnity
{
    internal static class UnityEngineHostTypes
    {
        private static readonly RilsHostTypeDescriptor[] TypeDescriptors =
        {
            new RilsHostTypeDescriptor("unity_engine::Object"),
            new RilsHostTypeDescriptor("unity_engine::Component", "unity_engine::Object"),
            new RilsHostTypeDescriptor("unity_engine::GameObject", "unity_engine::Object"),
            new RilsHostTypeDescriptor("unity_engine::Transform", "unity_engine::Component"),
        };

        private static readonly IReadOnlyList<RilsHostTypeDescriptor> ReadOnlyTypeDescriptors =
            Array.AsReadOnly(TypeDescriptors);

        internal static IReadOnlyList<RilsHostTypeDescriptor> All => ReadOnlyTypeDescriptors;
    }
}
