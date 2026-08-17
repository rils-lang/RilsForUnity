# Rils for Unity

This package integrates the Rils runtime with Unity 2022.3 LTS and later in the
2022.3 line. It provides:

- the host-neutral `Rils.CSharp` facade and native plugin location;
- a synchronous scalar host bridge for the first C# ↔ Rils interop prototype;
- a session-bound `UnityObjectHandleTable` for Unity-side opaque object identity;
- initial `unity::object::is_valid` and `unity::object::instance_id` host bindings;
- a `ScriptedImporter` for `.rils` source assets;
- `RilsBytecodeAsset` for serialized bytecode;
- the `rils_for_unity` Rils library, including the `RilsBehaviour` lifecycle trait;
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

## RilsForUnity project dependency

The package ships its Unity-specific language surface under `Runtime/Rils/`.
Add it as a path dependency in the project's `rils.toml`:

```toml
[dependencies.rils_for_unity]
path = "Packages/com.rils-lang.rils-for-unity/Runtime/Rils"
prelude = true
```

This makes the library available to the compiler and Analyzer through the
`crate::rils_for_unity::` path. The package prelude is loaded automatically.

## Lifecycle scripts

Attach `RilsBehaviour` to a GameObject and assign any imported `.rils` asset to
its `Script` field. A lifecycle script implements the package-provided trait:

```rils
use crate::rils_for_unity::behaviour::RilsBehaviour;

pub struct PlayerBehaviour { }

impl RilsBehaviour for PlayerBehaviour {
    fn awake(&mut self, host: HostHandle) { }
    fn start(&mut self, host: HostHandle) { }
    fn update(&mut self, host: HostHandle, delta_seconds: f32) { }
    fn on_destroy(&mut self, host: HostHandle) { }
}
```

The `HostHandle` identifies the owning
GameObject for the lifetime of that runtime instance; it becomes invalid when
the component is destroyed.

The first manual Unity host surface is available through these modules:

- `unity::object`: validity and instance ID checks.
- `unity::game_object`: active-state queries, activation changes, and `Transform` lookup.
- `unity::transform`: position component queries and position updates.
- `unity::component`: lookup of the owning `GameObject`.

These bindings intentionally use opaque `HostHandle` values and scalar arguments only. String,
struct, collection, and general reflection-based exports are reserved for a later compatible
binding layer.

## Host manifests

Use `Rils > Generate Unity Host Manifest` after adding or changing Unity host
bindings. The command writes the Unity fragment to
`.rils/manifest/unity.object.rilhm`, outside `Assets`, reimports `.rils` assets,
and embeds the deterministically merged manifest into each bytecode asset. Other
modules can add independent `.rilhm` fragments to the same directory. Player
builds do not need the project source directory; generated manifest files are
ignored by the integration repository.
