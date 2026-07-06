#!/usr/bin/env python3
"""Frisson Release Publisher

Usage:
    python publish.py [--platform {windows|macos|linux} ...] [--version VERSION]

Default: reads version from the unique SemVer Git tag on HEAD.
With --version: skips Git tag check (for test builds, NOT reproducible).
"""

import argparse
import subprocess
import re
import shutil
import sys
import os


def run(cmd, **kwargs):
    result = subprocess.run(cmd, capture_output=True, text=True, **kwargs)
    return result.stdout.strip(), result.returncode


def parse_version(tag):
    """Parse a SemVer-compliant Git tag.

    Tag MUST start with 'v', followed by a SemVer 2.0.0 version string.
    The 'v' prefix is stripped before being used elsewhere.

    SemVer format: MAJOR.MINOR.PATCH[-PRERELEASE][+BUILD]
        v1.0.0
        v1.0.0-beta
        v1.0.0-beta.1
        v1.0.0-rc.1+abc123
        v1.0.0+20260409

    Returns: (numeric_version, semver_version)
        - numeric: "MAJOR.MINOR.PATCH" (for AssemblyVersion/FileVersion;
                   PE file version requires pure numeric segments)
        - semver:  full SemVer string without 'v' prefix
                   (for Version/InformationalVersion)

    Returns (None, None) if tag is invalid.
    """
    pattern = (
        r'^v'
        r'(?P<major>0|[1-9]\d*)'
        r'\.(?P<minor>0|[1-9]\d*)'
        r'\.(?P<patch>0|[1-9]\d*)'
        r'(?:-(?P<prerelease>'
            r'(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)'
            r'(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*'
        r'))?'
        r'(?:\+(?P<build>[0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?'
        r'$'
    )
    match = re.match(pattern, tag)
    if not match:
        return None, None

    major = match.group("major")
    minor = match.group("minor")
    patch = match.group("patch")
    prerelease = match.group("prerelease")
    build = match.group("build")

    numeric = f"{major}.{minor}.{patch}"
    semver = numeric
    if prerelease:
        semver += f"-{prerelease}"
    if build:
        semver += f"+{build}"

    return numeric, semver


def build_windows(tag):
    """Build and pack for Windows.

    Produces two installers per release:
      - Frisson-Setup.exe                (framework-dependent, smaller)
      - Frisson-Setup-SelfContained.exe  (bundles .NET 10 runtime, larger)
    """
    numeric_ver, semver_ver = parse_version(tag)
    if not numeric_ver:
        print(f"Error: Invalid version tag '{tag}'")
        sys.exit(1)

    csproj = "src/Frisson.Desktop/Frisson.Desktop.csproj"
    common_props = [
        f"-p:Version={semver_ver}",
        f"-p:AssemblyVersion={numeric_ver}.1",
        f"-p:FileVersion={numeric_ver}",
        f"-p:InformationalVersion={semver_ver}",
    ]

    # Two configurations driven by .NET PublishProfiles (the official way):
    #   src/Frisson.Desktop/Properties/PublishProfiles/win-x64-framework.pubxml
    #   src/Frisson.Desktop/Properties/PublishProfiles/win-x64-selfcontained.pubxml
    #
    # Each profile owns its own PublishDir, so outputs do not collide and
    # MSBuild keeps separate obj/ folders per RID/Configuration combination.
    builds = [
        {
            "kind": "framework",
            "profile": "win-x64-framework",
            "exe_path": "src/Frisson.Desktop/publish/win-x64-framework/Frisson.Desktop.exe",
            "installer_name": "Frisson-Setup.exe",
        },
        {
            "kind": "selfcontained",
            "profile": "win-x64-selfcontained",
            "exe_path": "src/Frisson.Desktop/publish/win-x64-selfcontained/Frisson.Desktop.exe",
            "installer_name": "Frisson-Setup-SelfContained.exe",
        },
    ]
    
    iscc = shutil.which("ISCC") or shutil.which("ISCC.exe")
    if os.name == "nt" and not iscc:
        print("Error: ISCC.exe not found in PATH.")
        print("Please add Inno Setup install directory to your system PATH.")
        print("Install from: https://jrsoftware.org/isdl.php")
        sys.exit(1)

    for b in builds:
        print(f"\n=== Publishing {b['kind']} variant (profile: {b['profile']}) ===")
        # Use 'dotnet publish' with PublishProfile (the official .NET way).
        result = subprocess.run([
            "dotnet", "publish", csproj,
            f"-p:PublishProfile={b['profile']}",
            *common_props,
        ])
        if result.returncode != 0:
            sys.exit(1)

        # Verify built executable version
        if os.name == "nt" and os.path.exists(b["exe_path"]):
            try:
                fv, _ = run([
                    "powershell", "-Command",
                    f'[System.Diagnostics.FileVersionInfo]::GetVersionInfo("{b["exe_path"]}").FileVersion'
                ])
                print(f"Built {b['kind']} FileVersion: {fv}")
            except Exception:
                pass

        # Compile Inno Setup installer (Windows only)
        if os.name == "nt":
            print(f"=== Packing {b['kind']} installer ===")
            print(f"Using ISCC: {iscc}")
            rc = subprocess.run([iscc, f"/DBuildKind={b['kind']}", "setup.iss"]).returncode
            if rc != 0:
                print(f"Error: ISCC failed for {b['kind']} (exit {rc})")
                sys.exit(1)
            print(f"Installer created at: installer/{b['installer_name']}")


def build_macos(tag):
    """Build and pack for macOS — not yet implemented."""
    print("Error: macOS packaging is not yet implemented. Skipping.")


def build_linux(tag):
    """Build and pack for Linux — not yet implemented."""
    print("Error: Linux packaging is not yet implemented. Skipping.")


def main():
    parser = argparse.ArgumentParser(description="Frisson Release Publisher")
    parser.add_argument(
        "-p", "--platform",
        nargs="+",
        choices=["windows", "macos", "linux"],
        default=["windows"],
        help="Target platform(s) to pack for (default: windows)"
    )
    parser.add_argument(
        "--version",
        metavar="VERSION",
        help=(
            "Override version for testing (skip Git tag check). "
            "Format: SemVer 2.0.0 with or without 'v' prefix "
            "(e.g. 1.0.0, v1.0.0, 1.0.0-beta, 1.0.0-rc.1+abc123). "
            "WARNING: Builds with this flag are NOT reproducible from Git history."
        )
    )
    args = parser.parse_args()

    if args.version:
        # Test mode: use --version, skip Git tag check.
        # Accept both 'v1.0.0' and '1.0.0' for convenience.
        override = args.version if args.version.startswith("v") else f"v{args.version}"
        numeric_ver, semver_ver = parse_version(override)
        if not numeric_ver:
            print(f"Error: --version '{args.version}' is not a valid SemVer 2.0.0 string.")
            print("Examples: 1.0.0, 1.0.0-beta, 1.0.0-rc.1, 1.0.0+abc123")
            sys.exit(1)
        tag = override
        print("WARNING: --version override active. Skipping Git tag validation.")
    else:
        # Strict mode: read version from Git tags pointing at HEAD.
        # Reject if zero or multiple SemVer tags found on the same commit.
        out, code = run(["git", "tag", "--points-at", "HEAD"])
        tags_at_head = [t for t in out.splitlines() if t.strip()]
        semver_tags = [t for t in tags_at_head if parse_version(t)[0] is not None]

        if len(semver_tags) == 0:
            print(f"Error: No valid SemVer tag found on HEAD.")
            if tags_at_head:
                print(f"Tags on HEAD (none are valid SemVer): {tags_at_head}")
            print("Examples: v1.0.0, v1.0.0-beta, v1.0.0-rc.1, v1.0.0+abc123")
            print("To set a version, run: git tag v1.0.0 && git push --tags")
            print("For test builds, use: python publish.py --version 1.0.0")
            sys.exit(1)

        if len(semver_tags) > 1:
            print(f"Error: Multiple SemVer tags found on HEAD: {semver_tags}")
            print("Please remove redundant tags before publishing.")
            print("Use 'git tag -d <tag>' to delete a local tag,")
            print("and 'git push origin :refs/tags/<tag>' to delete a remote tag.")
            sys.exit(1)

        tag = semver_tags[0]
        numeric_ver, semver_ver = parse_version(tag)

    platforms = ", ".join(args.platform)
    print(f"Publishing Frisson {tag} (SemVer: {semver_ver}) for {platforms}")

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
