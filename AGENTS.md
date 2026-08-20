# Project tools

- Project automation lives in `Tools/`. Read `Tools/README.md` before opening or closing the Unity
  project manually.
- Use `Tools/OpenProject.bat` and `Tools/CloseProject.bat` as the Windows user-facing entry points.
  Their shared implementation is `Tools/rils_for_unity.py`.
- Keep tool-local machine state under `Tools/.local/`. It is intentionally ignored by Git and must
  not be committed.
- When adding, removing, renaming, or changing the usage of a tool, update `Tools/README.md` in the
  same change. The README must list every supported entry point, its purpose, prerequisites, and
  generated or locally cached files.
- Prefer Python for new tool implementation. Keep platform wrappers thin and put reusable behavior
  in the Python entry point.
