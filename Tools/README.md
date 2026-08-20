# RilsForUnity project tools

This directory is the documented home for project automation. Every supported tool entry point
must be listed here. When a tool is added, removed, renamed, or its command-line usage changes,
update this README in the same change.

## Available tools

Windows users can double-click:

- `OpenProject.bat` to locate Unity `2022.3.62f3`, open this project, and start the Unity Skills HTTP service.
- `CloseProject.bat` to ask the matching Unity Editor instance to close normally.

The first launch checks the default Unity Hub installation directories. If no matching editor is
found, a file picker asks for `Unity.exe`. The selected path is stored in
`Tools/.local/config.json`; this local file is ignored by Git.

The Python entry point is also available directly:

```powershell
python Tools/rils_for_unity.py open
python Tools/rils_for_unity.py close
```

Use `open --unity D:\path\to\Unity.exe` to replace the saved path. The selected editor version must
start with `2022.3.62f3`; Unity China builds such as `2022.3.62f3c1` are supported.

Closing uses the running Skills service first and falls back to sending `WM_CLOSE` to the Unity
window for this project. Unity can still show its normal confirmation dialog for unsaved changes.

## Maintenance

- Implement reusable behavior in Python; keep `.bat` files as thin user-facing wrappers.
- Store machine-specific settings and other local state under `Tools/.local/`. This directory is
  ignored by Git.
- Do not commit `__pycache__`, generated files, or locally selected Unity paths.
- Document each new tool here, including its purpose, invocation, prerequisites, and any generated
  or cached files.
