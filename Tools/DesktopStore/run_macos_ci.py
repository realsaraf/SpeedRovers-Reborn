"""Sign an explicitly supplied, hash-verified Mac player on a Codemagic Mac runner.

Manual invocation only. Does not fetch/create Apple identities, upload, or publish.
Reads distribution material from encrypted CI environment variables without logging it.
"""
from __future__ import annotations

import argparse
import base64
import hashlib
import json
import os
from pathlib import Path
import platform
import subprocess
import sys
import tarfile
import tempfile

from inspect_macos_build import inspect


def required(name: str) -> str:
    value = os.environ.get(name, "")
    if not value.strip():
        raise SystemExit(f"Missing required CI variable: {name}")
    return value


def private_file(folder: Path, variable: str, filename: str) -> Path:
    try:
        data = base64.b64decode("".join(required(variable).split()), validate=True)
    except ValueError:
        raise SystemExit(f"CI variable must contain valid base64: {variable}")
    if not data:
        raise SystemExit(f"CI variable decodes to an empty file: {variable}")
    path = folder / filename
    path.write_bytes(data)
    path.chmod(0o600)
    return path


def keychain(*arguments: str) -> None:
    # Do not log keychain output. Password arguments use environment references.
    result = subprocess.run(["keychain", *arguments], capture_output=True)
    if result.returncode:
        raise SystemExit("Codemagic keychain setup failed. Confirm certificate files, passwords, and private keys in the encrypted CI group.")


def import_certificate(certificate: Path, password_variable: str) -> None:
    command = ["add-certificates", "--certificate", str(certificate)]
    if password_variable in os.environ:
        # Codemagic resolves this reference internally; the secret never enters argv.
        command += ["--certificate-password", "@env:" + password_variable]
    keychain(*command)


def extract_player_archive(archive_path: Path, destination: Path, app_name: str) -> None:
    """Accept this project's link-free player export and reject unsafe members first."""
    if not destination.is_dir() or any(destination.iterdir()):
        raise ValueError("Archive destination must be a new, empty directory.")
    with tarfile.open(archive_path, "r:gz") as archive:
        members = archive.getmembers()
        seen = set()
        for member in members:
            name = member.name
            if "\\" in name or any(ord(character) < 32 for character in name):
                raise ValueError("Archive member contains a forbidden path character.")
            parts = name.split("/")
            if any(part in ("", ".", "..") for part in parts) or parts[0] != app_name:
                raise ValueError("Archive member is outside the expected app or contains traversal.")
            # macOS commonly uses a case-insensitive filesystem.
            normalized = name.casefold()
            if normalized in seen:
                raise ValueError("Archive contains duplicate or case-colliding member names.")
            seen.add(normalized)
            if member.issym() or member.islnk():
                raise ValueError("Player archive links are not allowed; the verified export needs none.")
            if not (member.isdir() or member.isfile()):
                raise ValueError("Player archive contains a special file type.")
        if not members:
            raise ValueError("Player archive is empty.")
        archive.extractall(destination, members=members, filter="data")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--archive", type=Path, required=True)
    parser.add_argument("--inspection-report", type=Path, required=True)
    parser.add_argument("--output-dir", type=Path, required=True)
    args = parser.parse_args()
    if platform.system() != "Darwin" or sys.version_info < (3, 12, 11):
        raise SystemExit("This handoff requires macOS and patched Python 3.12.11+ (safe tar extraction).")
    bundle = required("MAC_BUNDLE_ID")
    team = required("MAC_TEAM_ID")
    app_identity = required("MAC_APP_SIGNING_IDENTITY")
    installer_identity = required("MAC_INSTALLER_SIGNING_IDENTITY")
    for variable in ("CM_MAC_APPLICATION_CERTIFICATE", "CM_MAC_INSTALLER_CERTIFICATE", "CM_MAC_PROVISIONING_PROFILE"):
        required(variable)
    expected = json.loads(args.inspection_report.read_text(encoding="utf-8"))
    with args.archive.open("rb") as stream:
        actual_hash = hashlib.file_digest(stream, "sha256").hexdigest()
    if actual_hash != expected.get("archive_sha256") or expected.get("bundle_id") != bundle:
        raise SystemExit("Player archive hash or bundle ID does not match the reviewed inspection report.")
    app_name = expected.get("app_name", "")
    if not app_name.endswith(".app") or Path(app_name).name != app_name:
        raise SystemExit("The inspection report must specify a direct .app filename.")
    args.output_dir.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix="speedrovers-mac-signing-") as temporary:
        private = Path(temporary)
        extracted = private / "player"
        extracted.mkdir()
        extract_player_archive(args.archive, extracted, app_name)
        app = extracted / app_name
        checked = inspect(app)
        if checked["main_sha256"] != expected.get("main_sha256"):
            raise SystemExit("Extracted app executable does not match the reviewed build.")
        app_certificate = private_file(private, "CM_MAC_APPLICATION_CERTIFICATE", "application.p12")
        installer_certificate = private_file(private, "CM_MAC_INSTALLER_CERTIFICATE", "installer.p12")
        profile = private_file(private, "CM_MAC_PROVISIONING_PROFILE", "distribution.provisionprofile")
        keychain("initialize")
        for certificate, password_name in ((app_certificate, "CM_MAC_APPLICATION_CERTIFICATE_PASSWORD"),
                                           (installer_certificate, "CM_MAC_INSTALLER_CERTIFICATE_PASSWORD")):
            import_certificate(certificate, password_name)
        script = Path(__file__).with_name("package_macos_store.py")
        entitlements = Path(__file__).with_name("macos-store.entitlements.plist")
        command = [sys.executable, str(script), "--app", str(app), "--bundle-id", bundle,
                   "--team-id", team, "--app-identity", app_identity, "--installer-identity", installer_identity,
                   "--profile", str(profile), "--entitlements", str(entitlements),
                   "--output-dir", str(args.output_dir.resolve())]
        if os.environ.get("MAC_BUILD_NUMBER"):
            command += ["--build-number", os.environ["MAC_BUILD_NUMBER"]]
        subprocess.run(command, check=True)
    print("Signed package prepared. Upload, Apple processing, and sandboxed Mac gameplay are separate steps.")


if __name__ == "__main__":
    main()
