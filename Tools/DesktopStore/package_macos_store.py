"""Sign a universal Unity .app and create a Mac App Store .pkg on a Mac.

Requires explicit real distribution identities, a matching distribution profile,
and reviewed entitlements. Does not upload, notarize, change pricing, or publish.
"""
from __future__ import annotations

import argparse
import datetime as dt
import hashlib
import json
from pathlib import Path
import plistlib
import platform
import re
import shutil
import struct
import subprocess
import tempfile


def fail(message: str) -> None:
    raise SystemExit(message)


def run(*args: str, capture: bool = False) -> bytes:
    result = subprocess.run(list(args), check=True, stdout=subprocess.PIPE if capture else None)
    return result.stdout or b""


def macho_kind(path: Path) -> int | None:
    """Return MH filetype, also for a universal binary's first architecture."""
    if path.is_symlink() or not path.is_file():
        return None
    with path.open("rb") as stream:
        header = stream.read(32)
        if len(header) < 16:
            return None
        if header[:4] in (b"\xca\xfe\xba\xbe", b"\xca\xfe\xba\xbf"):
            offset = struct.unpack_from(">Q" if header[:4] == b"\xca\xfe\xba\xbf" else ">I", header, 16)[0]
            stream.seek(offset)
            header = stream.read(16)
        if len(header) < 16:
            return None
        if header[:4] in (b"\xce\xfa\xed\xfe", b"\xcf\xfa\xed\xfe"):
            return struct.unpack_from("<I", header, 12)[0]
        if header[:4] in (b"\xfe\xed\xfa\xce", b"\xfe\xed\xfa\xcf"):
            return struct.unpack_from(">I", header, 12)[0]
    return None


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--app", required=True, type=Path)
    parser.add_argument("--bundle-id", required=True)
    parser.add_argument("--team-id", required=True)
    parser.add_argument("--app-identity", required=True)
    parser.add_argument("--installer-identity", required=True)
    parser.add_argument("--profile", required=True, type=Path)
    parser.add_argument("--entitlements", required=True, type=Path)
    parser.add_argument("--build-number", help="Explicit positive Mac Store build number; updates only the staged app before signing.")
    parser.add_argument("--output-dir", required=True, type=Path, help="Mac workspace output path; never the source .app directory.")
    args = parser.parse_args()
    if platform.system() != "Darwin":
        fail("Mac App Store signing requires a Mac with Xcode command-line tools and the real distribution certificates.")
    if not args.app_identity.startswith(("Apple Distribution:", "3rd Party Mac Developer Application:")):
        fail("Supply a Mac App Store application distribution identity, not a Developer ID or ad hoc identity.")
    if not args.installer_identity.startswith("3rd Party Mac Developer Installer:"):
        fail("Supply the actual Mac Installer Distribution identity (3rd Party Mac Developer Installer: ...).")
    identities = run("security", "find-identity", "-v", capture=True).decode("utf-8", errors="replace")
    for identity in (args.app_identity, args.installer_identity):
        if f'"{identity}"' not in identities:
            fail("A specified distribution identity/private-key pair is unavailable in the current keychain.")
    profile = plistlib.loads(run("security", "cms", "-D", "-i", str(args.profile.resolve()), capture=True))
    if "OSX" not in profile.get("Platform", []):
        fail(f"Expected a macOS provisioning profile; supplied Platform is {profile.get('Platform', [])!r}.")
    if args.team_id not in profile.get("TeamIdentifier", []):
        fail("Provisioning profile team does not match --team-id.")
    expiry = profile.get("ExpirationDate")
    if not expiry or expiry.replace(tzinfo=dt.timezone.utc) <= dt.datetime.now(dt.timezone.utc):
        fail("Provisioning profile is missing an expiration date or has expired.")
    permitted = profile.get("Entitlements", {})
    app_identifier = permitted.get("com.apple.application-identifier", permitted.get("application-identifier", ""))
    if app_identifier != f"{args.team_id}.{args.bundle_id}":
        fail("Provisioning profile must explicitly match this Mac application's team and bundle identifier.")
    if profile.get("ProvisionedDevices") or profile.get("ProvisionsAllDevices") or permitted.get("get-task-allow") or permitted.get("com.apple.security.get-task-allow"):
        fail("This is not a Mac App Store distribution provisioning profile.")
    entitlement = plistlib.loads(args.entitlements.read_bytes())
    if entitlement.get("com.apple.security.app-sandbox") is not True:
        fail("Mac App Store entitlements must enable App Sandbox.")
    if entitlement.get("get-task-allow") or entitlement.get("com.apple.security.get-task-allow"):
        fail("Debug task access is not allowed in this distribution package.")
    source = args.app.resolve()
    if source.suffix != ".app" or not source.is_dir():
        fail("--app must identify a complete Unity .app bundle.")
    info = plistlib.loads((source / "Contents/Info.plist").read_bytes())
    if info.get("CFBundleIdentifier") != args.bundle_id:
        fail("The built app's bundle identifier does not match --bundle-id; rebuild with the correct identity.")
    if Path(info.get("CFBundleExecutable", "")).name != info.get("CFBundleExecutable"):
        fail("CFBundleExecutable must be a direct filename inside Contents/MacOS.")
    main_binary = source / "Contents/MacOS" / info["CFBundleExecutable"]
    architectures = run("lipo", "-archs", str(main_binary), capture=True).decode().split()
    if not {"arm64", "x86_64"}.issubset(architectures):
        fail("Expected the validated universal Intel + Apple Silicon player.")
    output = args.output_dir.resolve()
    if output == source or source in output.parents or output in source.parents:
        fail("The output and input app paths must be separate and must not contain each other.")
    output.mkdir(parents=True, exist_ok=True)
    version = info.get("CFBundleShortVersionString", "unknown")
    source_build_number = str(info.get("CFBundleVersion", "unknown"))
    build_number = args.build_number if args.build_number is not None else source_build_number
    if not re.fullmatch(r"\d+\.\d+\.\d+", str(version)):
        fail("CFBundleShortVersionString must have three numeric components.")
    if not re.fullmatch(r"[1-9]\d{0,3}(?:\.\d{1,2}){0,2}", build_number):
        fail("Mac CFBundleVersion must start with a positive integer. Supply an explicit --build-number if the Unity export contains 0.")
    pkg = output / f"SpeedRovers2-{version}-{build_number}-MacAppStore.pkg"
    if pkg.exists():
        fail(f"Refusing to overwrite an existing package: {pkg}")
    stage = Path(tempfile.mkdtemp(prefix="mac-store-", dir=output))
    app = stage / source.name
    run("ditto", str(source), str(app))
    if args.build_number is not None:
        info["CFBundleVersion"] = build_number
        (app / "Contents/Info.plist").write_bytes(plistlib.dumps(info))
    shutil.copy2(args.profile, app / "Contents/embedded.provisionprofile")
    # Bind the profile's real application/team identifiers into the top-level signature.
    entitlement["com.apple.application-identifier"] = app_identifier
    entitlement["com.apple.developer.team-identifier"] = args.team_id
    parent_entitlements = stage / "app.entitlements.plist"
    parent_entitlements.write_bytes(plistlib.dumps(entitlement))
    child_entitlements = stage / "child.entitlements.plist"
    child_entitlements.write_bytes(plistlib.dumps({"com.apple.security.app-sandbox": True, "com.apple.security.inherit": True}))
    main_binary = app / "Contents/MacOS" / info["CFBundleExecutable"]
    # Sign nested Mach-O code from the inside out; do not use --deep as a signing shortcut.
    for path in sorted(app.rglob("*"), key=lambda p: len(p.parts), reverse=True):
        kind = macho_kind(path)
        if kind is None or path == main_binary:
            continue
        command = ["codesign", "--force", "--timestamp", "--sign", args.app_identity]
        if kind == 2:  # MH_EXECUTE helpers inherit the parent sandbox.
            command += ["--entitlements", str(child_entitlements)]
        run(*command, str(path))
    bundles = [p for p in app.rglob("*") if p.is_dir() and not p.is_symlink() and p.suffix in (".framework", ".bundle", ".app", ".xpc")]
    for path in sorted(bundles, key=lambda p: len(p.parts), reverse=True):
        command = ["codesign", "--force", "--timestamp", "--sign", args.app_identity]
        if path.suffix in (".app", ".xpc"):
            command += ["--entitlements", str(child_entitlements)]
        run(*command, str(path))
    run("codesign", "--force", "--timestamp", "--entitlements", str(parent_entitlements), "--sign", args.app_identity, str(app))
    run("codesign", "--verify", "--deep", "--strict", "--verbose=2", str(app))
    run("productbuild", "--component", str(app), "/Applications", "--sign", args.installer_identity, str(pkg))
    run("pkgutil", "--check-signature", str(pkg))
    with pkg.open("rb") as stream:
        digest = hashlib.file_digest(stream, "sha256").hexdigest()
    report = {"state": "signed_pkg_created", "artifact": str(pkg), "sha256": digest, "bytes": pkg.stat().st_size,
              "bundle_id": args.bundle_id, "version": version, "build": build_number,
              "source_build": source_build_number, "build_number_stamped_in_staging": args.build_number is not None,
              "architectures": architectures,
              "store_submission": False, "store_price_set": False, "mac_runtime_verified": False}
    (output / f"SpeedRovers2-{version}-{build_number}-packaging.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(f"Prepared Mac App Store package: {pkg}")
    print("Not uploaded. Verify the sandboxed game on a Mac before submitting through Transporter or App Store Connect.")


if __name__ == "__main__":
    main()
