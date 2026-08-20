# Rils for Unity

This package integrates the Rils runtime with Unity 2022.3 LTS and later in the
2022.3 line. It provides:

- the host-neutral `Rils.CSharp` facade and native plugin location;
- a synchronous scalar host bridge for the first C# ↔ Rils interop prototype;
- a session-bound `UnityObjectHandleTable` for Unity-side opaque object identity;
- descriptor-backed `unity_engine` object, component, transform, and time host modules;
- a `ScriptedImporter` for `.rils` source assets;
- `RilsScriptAsset` for each imported source and `RilsEntryAsset` sub-assets for each detected
  `RilsBehaviour` implementation;
- `RilsLibraryAsset` importing explicitly distributed `.rilslib` artifacts;
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

## Host interop

The interop validation scene lives in the Unity project's
`Assets/RilsTests/Interop/` directory rather than in the distributable package.
The persistent `Assets/Scenes/Interop.unity` scene contains the runner and
verifies both directions of the initial bridge, object-handle round trips, and
basic object validity/identity queries:

- C# calls a public Rils function;
- Rils calls a registered scalar C# host function.

Host registration must be completed before freezing the registry, and calls
must stay on the thread that created the `RilsRuntime` (the Unity main thread
in normal use). Primitive values and session-bound Unity object handles are
supported; string, collection, and value-struct transports remain future work.

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

Attach `RilsBehaviour` to a GameObject and assign one of the `RilsEntryAsset` sub-assets generated
under an imported `.rils` source. A lifecycle script implements the package-provided trait:

Create scripts from the Project window with `Assets > Create > Rils > Empty Script` or
`Assets > Create > Rils > RilsBehaviour Script`. Both commands use Unity's normal create-and-rename
flow; the behaviour template derives its Rils type name from the chosen file name.

```rils
#[derive(Default)]
pub struct PlayerBehaviour { }

impl RilsBehaviour for PlayerBehaviour {
    fn awake(&mut self, host: unity_engine::GameObject) { }
    fn start(&mut self, host: unity_engine::GameObject) { }
    fn update(&mut self, host: unity_engine::GameObject, delta_seconds: f32) { }
    fn on_destroy(&mut self, host: unity_engine::GameObject) { }
}
```

The logical `unity_engine::GameObject` value identifies the owning GameObject
for the lifetime of that runtime instance. It uses `HostHandle` transport and
becomes invalid when the component is destroyed.

The importer discovers entries from verified bytecode trait metadata rather than source text and
filters them by declaring source, so each main asset owns only the entries declared in that `.rils` file.
At runtime the selected entry is constructed through `Default::default()`, retained as one opaque
Rils value for the component lifetime, and invoked through `RilsBehaviour` trait-method identity.

The first UnityEngine binding catalog is available through these modules:

- `unity_engine::object`: validity and instance ID checks.
- `unity_engine::game_object`: active-state queries, activation changes, and `Transform` lookup.
- `unity_engine::transform`: position component queries and position updates.
- `unity_engine::component`: lookup of the owning `GameObject`.
- `unity_engine::time`: delta time, fixed delta time, elapsed time, and frame count.

Each catalog module owns immutable function descriptors with deterministic IDs and separate static
handlers. The Editor serializes descriptors without executing handlers; the Player binds those same
descriptors to direct C# calls, keeping the path AOT/IL2CPP friendly. Host Manifest v2 declares
`unity_engine::Object`, `Component : Object`, `GameObject : Object`, and
`Transform : Component`. Rils and the Analyzer retain these logical types and inherited methods,
while C ABI version 4 transports their values as opaque `HostHandle` records. String, value-struct,
collection, enum, constant, and general project API exports require later work.

## Host manifests

When the Editor loads, RilsForUnity synchronizes one fragment per catalog module under
`.rils/manifest/unity-engine/`. Missing, damaged, outdated, and stale owned fragments are updated
atomically and `.rils` assets are reimported automatically. `Rils > Generate Unity Host Manifest`
remains available as an explicit force-regenerate command. Project modules can add independent
`.rilhm` fragments outside the owned `unity-engine` directory. Player builds do not need the project
source directory; generated manifest files are ignored by the integration repository.

The runtime can still load v1 fragments, but every fragment generated by this package uses Host
Manifest v2. Regeneration is therefore also the migration path for stale v1 built-in fragments.

## Rils library artifacts

The importer recognizes `.rilslib` files as `RilsLibraryAsset`. Source dependencies remain the
default development workflow and continue to be compiled automatically from their `rils.toml`
projects. Explicit library export is intended for independent distribution; entry-to-library
dynamic linking is being introduced separately and is not silently emulated by embedding a
`.rilslib` into every entry.

Library prelude files remain special compiler inputs, but they are imported as normal
`RilsScriptAsset` main assets. Compiling a prelude asset injects it exactly once and includes the
rest of its library project without treating the prelude as a regular module path.
