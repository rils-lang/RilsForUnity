#!/usr/bin/env python3
"""Open or close the RilsForUnity Unity project on Windows."""

from __future__ import annotations

import argparse
import ctypes
import json
import os
import re
import subprocess
import sys
import time
import urllib.error
import urllib.request
from pathlib import Path
from typing import Any, Iterable


UNITY_VERSION_PREFIX = "2022.3.62f3"
PROJECT_NAME = "RilsForUnity"
EXECUTE_METHOD = "RilsForUnity.Tools.CommandLine.StartSkillsServer"
SKILLS_PORTS = range(8090, 8101)

TOOLS_DIR = Path(__file__).resolve().parent
PROJECT_DIR = TOOLS_DIR.parent
CONFIG_PATH = TOOLS_DIR / ".local" / "config.json"


class ToolError(RuntimeError):
    pass


def _normalized(path: Path) -> str:
    return os.path.normcase(os.path.abspath(str(path)))


def _read_config() -> dict[str, Any]:
    if not CONFIG_PATH.is_file():
        return {}
    try:
        value = json.loads(CONFIG_PATH.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as error:
        print(f"Ignoring invalid local config {CONFIG_PATH}: {error}", file=sys.stderr)
        return {}
    return value if isinstance(value, dict) else {}


def _write_config(unity_path: Path) -> None:
    CONFIG_PATH.parent.mkdir(parents=True, exist_ok=True)
    temporary = CONFIG_PATH.with_suffix(".tmp")
    temporary.write_text(
        _serialized_config(unity_path),
        encoding="utf-8",
    )
    temporary.replace(CONFIG_PATH)


def _serialized_config(unity_path: Path) -> str:
    return (
        json.dumps(
            {
                "unity_path": str(unity_path),
                "version_prefix": UNITY_VERSION_PREFIX,
            },
            ensure_ascii=False,
            indent=2,
        )
        + "\n"
    )


def _hub_roots() -> list[Path]:
    roots: list[Path] = []
    for environment_name in ("ProgramFiles", "ProgramFiles(x86)"):
        value = os.environ.get(environment_name)
        if value:
            roots.append(Path(value) / "Unity" / "Hub" / "Editor")
    roots.extend(
        [
            Path(r"C:\Program Files\Unity\Hub\Editor"),
            Path(r"C:\Program Files (x86)\Unity\Hub\Editor"),
        ]
    )
    unique: list[Path] = []
    seen: set[str] = set()
    for root in roots:
        key = _normalized(root)
        if key not in seen:
            seen.add(key)
            unique.append(root)
    return unique


def find_unity_in_hub_roots(roots: Iterable[Path]) -> Path | None:
    candidates: list[tuple[int, str, Path]] = []
    for root in roots:
        if not root.is_dir():
            continue
        for installation in root.iterdir():
            if not installation.is_dir() or not installation.name.startswith(UNITY_VERSION_PREFIX):
                continue
            executable = installation / "Editor" / "Unity.exe"
            if executable.is_file():
                exact_rank = 0 if installation.name == UNITY_VERSION_PREFIX else 1
                candidates.append((exact_rank, installation.name, executable))
    return min(candidates, default=None, key=lambda item: (item[0], item[1]))[2] if candidates else None


def _choose_unity_with_dialog() -> Path | None:
    try:
        import tkinter as tk
        from tkinter import filedialog

        root = tk.Tk()
        root.withdraw()
        root.attributes("-topmost", True)
        selected = filedialog.askopenfilename(
            parent=root,
            title=f"Select Unity {UNITY_VERSION_PREFIX} Editor",
            filetypes=(("Unity Editor", "Unity.exe"), ("Executable", "*.exe")),
        )
        root.destroy()
        return Path(selected) if selected else None
    except Exception as error:  # tkinter can be unavailable in minimal Python installs.
        raise ToolError(
            "Unity was not found in the default Hub directories and the file picker failed. "
            "Pass --unity <path-to-Unity.exe>."
        ) from error


def _reported_unity_version(unity_path: Path) -> str | None:
    creation_flags = subprocess.CREATE_NO_WINDOW if os.name == "nt" else 0
    try:
        result = subprocess.run(
            [str(unity_path), "-version"],
            capture_output=True,
            text=True,
            timeout=30,
            creationflags=creation_flags,
            check=False,
        )
    except (OSError, subprocess.TimeoutExpired):
        return None
    output = f"{result.stdout}\n{result.stderr}"
    match = re.search(r"20\d{2}\.\d+\.\d+[abfp]\d+(?:c\d+)?", output)
    return match.group(0) if match else None


def _validate_unity_path(unity_path: Path) -> Path:
    unity_path = unity_path.expanduser().resolve()
    if not unity_path.is_file() or unity_path.name.lower() != "unity.exe":
        raise ToolError(f"Expected a Unity.exe file, got: {unity_path}")
    reported_version = _reported_unity_version(unity_path)
    if reported_version and not reported_version.startswith(UNITY_VERSION_PREFIX):
        raise ToolError(
            f"Unity {reported_version} is not supported; select Unity {UNITY_VERSION_PREFIX}."
        )
    if not reported_version and UNITY_VERSION_PREFIX not in str(unity_path):
        print(
            "Warning: Unity did not report its version and the path does not contain "
            f"{UNITY_VERSION_PREFIX}; continuing with the selected editor.",
            file=sys.stderr,
        )
    return unity_path


def resolve_unity_path(explicit: str | None) -> Path:
    if explicit:
        selected = _validate_unity_path(Path(explicit))
        _write_config(selected)
        return selected

    saved = _read_config().get("unity_path")
    if isinstance(saved, str):
        try:
            return _validate_unity_path(Path(saved))
        except ToolError as error:
            print(f"Saved Unity path is unavailable: {error}", file=sys.stderr)

    discovered = find_unity_in_hub_roots(_hub_roots())
    selected = discovered or _choose_unity_with_dialog()
    if selected is None:
        raise ToolError("Unity selection was cancelled.")
    selected = _validate_unity_path(selected)
    _write_config(selected)
    return selected


def _request_json(url: str, body: dict[str, Any] | None = None, timeout: float = 2.0) -> Any:
    data = None
    headers: dict[str, str] = {}
    if body is not None:
        data = json.dumps(body).encode("utf-8")
        headers["Content-Type"] = "application/json"
    request = urllib.request.Request(url, data=data, headers=headers, method="POST" if data else "GET")
    with urllib.request.urlopen(request, timeout=timeout) as response:
        return json.loads(response.read().decode("utf-8"))


def find_project_service() -> tuple[int, dict[str, Any]] | None:
    for port in SKILLS_PORTS:
        try:
            health = _request_json(f"http://127.0.0.1:{port}/health")
        except (OSError, ValueError, urllib.error.URLError):
            continue
        if isinstance(health, dict) and health.get("projectName") == PROJECT_NAME:
            return port, health
    return None


def _unity_processes() -> list[dict[str, Any]]:
    if os.name != "nt":
        return []
    command = (
        "Get-CimInstance Win32_Process -Filter \"Name = 'Unity.exe'\" | "
        "Select-Object ProcessId,ExecutablePath,CommandLine | ConvertTo-Json -Compress"
    )
    try:
        result = subprocess.run(
            ["powershell.exe", "-NoProfile", "-NonInteractive", "-Command", command],
            capture_output=True,
            text=True,
            timeout=15,
            creationflags=subprocess.CREATE_NO_WINDOW,
            check=False,
        )
        if result.returncode != 0 or not result.stdout.strip():
            return []
        value = json.loads(result.stdout)
    except (OSError, subprocess.TimeoutExpired, json.JSONDecodeError):
        return []
    items = value if isinstance(value, list) else [value]
    project = _normalized(PROJECT_DIR)
    return [
        item
        for item in items
        if isinstance(item, dict)
        and project in os.path.normcase(str(item.get("CommandLine") or ""))
    ]


def _post_wm_close(process_ids: Iterable[int]) -> int:
    if os.name != "nt":
        return 0
    user32 = ctypes.windll.user32
    wm_close = 0x0010
    targets = {int(process_id) for process_id in process_ids}
    closed = 0
    callback_type = ctypes.WINFUNCTYPE(ctypes.c_bool, ctypes.c_void_p, ctypes.c_void_p)

    @callback_type
    def visit_window(window: int, _parameter: int) -> bool:
        nonlocal closed
        process_id = ctypes.c_ulong()
        user32.GetWindowThreadProcessId(window, ctypes.byref(process_id))
        if process_id.value in targets and user32.IsWindowVisible(window):
            user32.PostMessageW(window, wm_close, 0, 0)
            closed += 1
        return True

    user32.EnumWindows(visit_window, 0)
    return closed


def open_project(arguments: argparse.Namespace) -> int:
    existing = find_project_service()
    if existing:
        port, health = existing
        print(f"RilsForUnity is already open; Skills is running on port {port} ({health.get('instanceId')}).")
        return 0

    unity_path = resolve_unity_path(arguments.unity)
    command = [
        str(unity_path),
        "-projectPath",
        str(PROJECT_DIR),
        "-executeMethod",
        EXECUTE_METHOD,
    ]
    creation_flags = 0
    if os.name == "nt":
        creation_flags = subprocess.DETACHED_PROCESS | subprocess.CREATE_NEW_PROCESS_GROUP
    process = subprocess.Popen(command, close_fds=True, creationflags=creation_flags)
    print(f"Started Unity {UNITY_VERSION_PREFIX} for {PROJECT_DIR} (PID {process.pid}).")
    if arguments.no_wait:
        return 0

    deadline = time.monotonic() + arguments.timeout
    while time.monotonic() < deadline:
        service = find_project_service()
        if service:
            port, health = service
            print(f"Unity Skills is ready on http://localhost:{port}/ ({health.get('instanceId')}).")
            return 0
        if process.poll() is not None:
            raise ToolError(f"Unity exited during startup with code {process.returncode}.")
        time.sleep(1.0)
    raise ToolError(
        f"Unity started, but its Skills service was not ready within {arguments.timeout:.0f} seconds."
    )


def close_project(arguments: argparse.Namespace) -> int:
    processes = _unity_processes()
    service = find_project_service()
    if not processes and not service:
        print("RilsForUnity is already closed.")
        return 0

    requested = False
    if service:
        port, _health = service
        try:
            result = _request_json(
                f"http://127.0.0.1:{port}/skill/editor_execute_menu",
                {"menuPath": "File/Exit"},
                timeout=15.0,
            )
            requested = isinstance(result, dict) and result.get("status") == "success"
        except (OSError, ValueError, urllib.error.URLError):
            # Unity may close the listener before the HTTP response is returned.
            requested = True

    if not requested and processes:
        process_ids = [int(item["ProcessId"]) for item in processes]
        requested = _post_wm_close(process_ids) > 0

    if not requested:
        raise ToolError("Could not request the RilsForUnity Editor window to close.")

    print("Close requested. Waiting for the RilsForUnity Editor to exit...")
    deadline = time.monotonic() + arguments.timeout
    while time.monotonic() < deadline:
        if not _unity_processes() and not find_project_service():
            print("RilsForUnity closed.")
            return 0
        time.sleep(0.5)
    raise ToolError(
        "Unity is still open. Check for an unsaved-changes or other modal confirmation dialog."
    )


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    subparsers = parser.add_subparsers(dest="command", required=True)

    open_parser = subparsers.add_parser("open", help="Open the project and start Unity Skills")
    open_parser.add_argument("--unity", help="Path to Unity.exe; validated and saved locally")
    open_parser.add_argument("--no-wait", action="store_true", help="Return immediately after launch")
    open_parser.add_argument("--timeout", type=float, default=180.0, help="Skills startup timeout in seconds")
    open_parser.set_defaults(handler=open_project)

    close_parser = subparsers.add_parser("close", help="Close the matching Unity Editor normally")
    close_parser.add_argument("--timeout", type=float, default=30.0, help="Editor shutdown timeout in seconds")
    close_parser.set_defaults(handler=close_project)
    return parser


def main() -> int:
    parser = build_parser()
    arguments = parser.parse_args()
    try:
        return int(arguments.handler(arguments))
    except ToolError as error:
        print(f"error: {error}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
