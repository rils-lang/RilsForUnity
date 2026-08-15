# Rils for Unity

This package integrates the Rils runtime with Unity 2022.3 LTS and later in the
2022.3 line. It provides:

- the host-neutral `Rils.CSharp` facade and native plugin location;
- a synchronous scalar host bridge for the first C# ↔ Rils interop prototype;
- a `ScriptedImporter` for `.rils` source assets;
- `RilsBytecodeAsset` for serialized bytecode;
- optional Addressables loading support.

The package is embedded in the `RilsForUnity` development project so changes are
available to Unity immediately. The currently supported Windows x86_64 native
runtime is bundled under `Runtime/Rils.CSharp/Internal/x86_64/`.

## Layout

`Runtime/` contains Unity runtime assets and the generated `Rils.CSharp/` facade
with its native plugin contract, `Editor/` contains import and setup tools, and
`Addressables/` contains the optional Addressables adapter.

## M0 scalar interop

Import the `M0 Scalar Interop` sample and add `RilsInteropPrototype` to a
GameObject. It verifies both directions of the initial bridge:

- C# calls a public Rils function;
- Rils calls a registered scalar C# host function.

Only synchronous scalar values are supported at this stage. Host registration
must be completed before freezing the registry, and calls must stay on the
thread that created the `RilsRuntime` (the Unity main thread in normal use).
