# Changelog

## [0.1.0] - Unreleased

- `RilsBehaviour` now requires `Default`; generated behaviour scripts derive it automatically, and imports reject entry types that do not satisfy the constraint.
- Entry assets are now discovered from verified bytecode trait metadata. Runtime components construct the selected entry with `Default::default()` and dispatch lifecycle calls to one persistent opaque Rils value instead of calling same-named module functions.
- Project imports now filter trait entries by their declaring source file, so each `.rils` main asset owns only its own `RilsBehaviour` sub-assets.
- Replaced the public bytecode asset model with a `RilsScriptAsset` main asset and one
  `RilsEntryAsset` sub-asset per detected `RilsBehaviour` implementation.
- Added `.rilslib` importing as `RilsLibraryAsset` while keeping source packages as the default
  development dependency workflow.
- Unity host manifest startup validation now atomically regenerates missing, damaged, or outdated
  built-in manifests and reimports `.rils` assets without requiring a manual menu action.
- Library prelude sources now import as `RilsScriptAsset` main assets without duplicate prelude
  injection.
- Added `Assets/Create/Rils` commands for empty scripts and complete `RilsBehaviour` templates.
- `RilsBehaviour` templates now use unit structs and rely on the package prelude instead of emitting
  a redundant explicit trait import. Created filenames are normalized to valid Rils module
  identifiers, including duplicate-name suffixes.

- Initial embedded Unity package layout.
- Added `.rils` asset importing and bytecode assets.
- Added the C# runtime facade and Windows x86_64 native plugin hook.
- Added the M0 synchronous scalar interop bridge; the Unity project contains its validation case.
- Added the session-bound Unity object handle table used by the interop validation scene.
- Added initial `unity::object::is_valid` and `unity::object::instance_id` bindings.
- Added `RilsBehaviour` asset binding with optional `awake`, `start`, `update`, and `on_destroy` callbacks.
- Added generated `.rils/manifest/` Unity manifest workflow and embedded merged manifest data in bytecode assets.
- Added Addressables runtime loading support.
