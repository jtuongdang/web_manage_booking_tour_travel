#!/usr/bin/env python3
"""Repository validation runner for DevAgent Orchestrator (ASP.NET 10 + Vite)."""
from __future__ import annotations

import shutil
import subprocess
import sys
from pathlib import Path


def run(cmd: list[str], cwd: Path) -> int:
    print(f"[validate] Running: {' '.join(cmd)} (cwd={cwd})", flush=True)
    proc = subprocess.run(cmd, cwd=str(cwd), check=False)
    return proc.returncode


def main() -> int:
    root = Path(__file__).resolve().parent.parent
    sln = root / "QuanLyTourDuLich.slnx"
    if sln.is_file():
        code = run(["dotnet", "build", str(sln), "--nologo"], root)
        if code != 0:
            return code

        test_projects = sorted(root.glob("*Tests/*.csproj")) + sorted(root.glob("*.Tests/*.csproj"))
        for test_proj in test_projects:
            code = run(["dotnet", "test", str(test_proj), "--no-build", "--nologo"], root)
            if code != 0:
                return code

    frontend = root / "frontend"
    node_modules = frontend / "node_modules"
    npm = shutil.which("npm")
    if frontend.is_dir() and node_modules.is_dir() and npm:
        for script_name in ("lint", "build"):
            code = run([npm, "run", script_name], frontend)
            if code != 0:
                return code
    elif frontend.is_dir():
        print("[validate] Skipping frontend npm lint/build: frontend/node_modules is not present in this worktree.", flush=True)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
