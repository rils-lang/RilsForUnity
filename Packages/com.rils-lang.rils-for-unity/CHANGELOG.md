# Changelog

## [0.2.0] - Unreleased

- Initial embedded Unity package layout.
- Added `.rils` asset importing and bytecode assets.
- Added the C# runtime facade and Windows x86_64 native plugin hook.
- Added the M0 synchronous scalar interop bridge; the Unity project contains its validation case.
- Added the session-bound Unity object handle table used by the interop validation scene.
- Added initial `unity::object::is_valid` and `unity::object::instance_id` bindings.
- Added `RilsBehaviour` asset binding with optional `awake`, `start`, `update`, and `on_destroy` callbacks.
- Added generated `.rils/manifest/` Unity manifest workflow and embedded merged manifest data in bytecode assets.
- Added Addressables runtime loading support.
