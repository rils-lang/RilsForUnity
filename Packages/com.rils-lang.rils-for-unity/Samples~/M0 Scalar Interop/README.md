# M0 Scalar Interop

Add `RilsInteropPrototype` to a GameObject and enter Play Mode. The component verifies both directions
of the first interop slice:

- C# calls the public Rils function `double`.
- Rils calls the registered C# host function `unity::math::add`.

Only synchronous scalar values are supported in this prototype. Host registration must finish before the
runtime is frozen, and all calls must happen on the Unity thread that created the runtime.
