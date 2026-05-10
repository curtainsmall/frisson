#!/usr/bin/env python3
"""CoyoteStudio Release Publisher

Usage: python publish.py [--platform {windows|macos|linux}]
Reads version from Git tag, builds Release, and compiles the installer.
"""

import argparse
import subprocess
import re
import sys
import os


def run(cmd, **kwargs):
    result = subprocess.run(cmd, capture_output=True, text=True, **kwargs)
    return result.stdout.strip(), result.returncode


def parse_version(tag):
    """Parse Git tag into numeric version and optional beta suffix.
    
    Supports: v1.0.0, v1.0.0b
    Returns: (numeric_version, dotnet_version)
    - numeric: "1.0.0" (for AssemblyVersion/FileVersion)
    - dotnet:  "1.0.0-b" (for Version/InformationalVersion)
    """
    match = re.match(r'^v(\d+(?:\.\d+){0,3})(b)?$', tag)
    if not match:
        return None, None
    numeric = match.group(1)
    suffix = match.group(2)
    if suffix:
        return numeric, f"{numeric}-{suffix}"
    return numeric, numeric


def build_windows(tag):
    """Build and pack for Windows."""
    numeric_ver, dotnet_ver = parse_version(tag)
    if not numeric_ver:
        print(f"Error: Invalid version tag '{tag}'")
        sys.exit(1)

    # 1. Build Release with version override
    result = subprocess.run([
        "dotnet", "build",
        "src/CoyoteStudio.App/CoyoteStudio.App.csproj",
        "-c", "Release",
        f"-p:Version={dotnet_ver}",
        f"-p:AssemblyVersion={numeric_ver}",
        f"-p:FileVersion={numeric_ver}"
    ])
    if result.returncode != 0:
        sys.exit(1)

    # 2. Verify the built executable version (Windows only)
    exe_path = "src/CoyoteStudio.App/bin/Release/net10.0/CoyoteStudio.App.exe"
    if os.name == "nt" and os.path.exists(exe_path):
        try:
            fv, _ = run([
                "powershell", "-Command",
                f'[System.Diagnostics.FileVersionInfo]::GetVersionInfo("{exe_path}").FileVersion'
            ])
            print(f"Built executable FileVersion: {fv}")
        except Exception:
            pass

    # 3. Compile Inno Setup installer (Windows only)
    if os.name == "nt":
        iscc = r"C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
        if os.path.exists(iscc):
            subprocess.run([iscc, "setup.iss"])
            print("Installer created at: installer/CoyoteStudio-Setup.exe")
        else:
            print("Warning: Inno Setup not found. Please install it from https://jrsoftware.org/isdl.php")


def build_macos(tag):
    """Build and pack for macOS — not yet implemented."""
    print("Error: macOS packaging is not yet implemented. Skipping.")


def build_linux(tag):
    """Build and pack for Linux — not yet implemented."""
    print("Error: Linux packaging is not yet implemented. Skipping.")


def main():
    parser = argparse.ArgumentParser(description="CoyoteStudio Release Publisher")
    parser.add_argument(
        "-p", "--platform",
        nargs="+",
        choices=["windows", "macos", "linux"],
        default=["windows"],
        help="Target platform(s) to pack for (default: windows)"
    )
    args = parser.parse_args()

    # Read version from Git tag
    raw_tag, code = run(["git", "describe", "--tags", "--always"])
    tag = raw_tag.strip()

    # Validate version format (e.g., v1.0.0, v1.0.0b)
    numeric_ver, dotnet_ver = parse_version(tag)
    if not numeric_ver:
        print(f"Error: No valid Git tag found (current: '{tag}').")
        print("Valid formats: v1.0.0, v1.0.0b")
        print("To set a version, run: git tag v1.0.0 && git push --tags")
        print("For beta: git tag v1.0.0b && git push --tags")
        sys.exit(1)

    platforms = ", ".join(args.platform)
    print(f"Publishing CoyoteStudio {tag} for {platforms}")

    for platform in args.platform:
        print(f"\n--- Building for {platform} ---")
        if platform == "windows":
            build_windows(tag)
        elif platform == "macos":
            build_macos(tag)
        elif platform == "linux":
            build_linux(tag)

    print("\nDone.")


if __name__ == "__main__":
    main()
