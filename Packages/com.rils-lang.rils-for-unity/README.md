# Rils for Unity

This package integrates the Rils runtime with Unity 2022.3 LTS and later in the
2022.3 line. It provides:

- the host-neutral `Rils.CSharp` facade and native plugin location;
- a synchronous scalar host bridge for the first C# ↔ Rils interop prototype;
- a session-bound `UnityObjectHandleTable` for Unity-side opaque object identity;
- initial `unity::object::is_valid` and `unity::object::instance_id` host bindings;
- a `ScriptedImporter` for `.rils` source assets;
- `RilsBytecodeAsset` for serialized bytecode;
- `RilsBehaviour`, which runs a dragged bytecode asset from Unity lifecycle callbacks;
- an editor command that generates Unity host manifest fragments under `.rils/manifest/`;
- optional Addressables loading support.

The package is embedded in the `RilsForUnity` development project so changes are
available to Unity immediately. The currently supported Windows x86_64 native
runtime is bundled under `Runtime/Rils.CSharp/Internal/x86_64/`.

## Layout

`Runtime/` contains Unity runtime assets and the generated `Rils.CSharp/` facade
with its native plugin contract, `Editor/` contains import and setup tools, and
`Addressables/` contains the optional Addressables adapter.

## M0 scalar interop

The interop validation scene lives in the Unity project's
`Assets/RilsTests/Interop/` directory rather than in the distributable package.
The persistent `Assets/Scenes/Interop.unity` scene contains the runner and
verifies both directions of the initial bridge, object-handle round trips, and
basic object validity/identity queries:

- C# calls a public Rils function;
- Rils calls a registered scalar C# host function.

Only synchronous scalar values are supported at this stage. Host registration
must be completed before freezing the registry, and calls must stay on the
thread that created the `RilsRuntime` (the Unity main thread in normal use).

## Lifecycle scripts

Attach `RilsBehaviour` to a GameObject and assign any imported `.rils` asset to
its `Script` field. The importer records these optional public functions:

```rils
pub fn awake(host: HostHandle) { }
pub fn start(host: HostHandle) { }
pub fn update(host: HostHandle, delta_seconds: f32) { }
pub fn on_destroy(host: HostHandle) { }
```

Only declared callbacks are invoked. The `HostHandle` identifies the owning
GameObject for the lifetime of that runtime instance; it becomes invalid when
the component is destroyed.

## Host manifests

Use `Rils > Generate Unity Host Manifest` after adding or changing Unity host
bindings. The command writes the Unity fragment to
`.rils/manifest/unity.object.rilhm`, outside `Assets`, reimports `.rils` assets,
and embeds the deterministically merged manifest into each bytecode asset. Other
modules can add independent `.rilhm` fragments to the same directory. Player
builds do not need the project source directory; generated manifest files are
ignored by the integration repository.
