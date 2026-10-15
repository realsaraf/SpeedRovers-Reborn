"""Inspect a Unity Mac player on Windows or macOS and optionally archive it for signing.

Inspection proves file structure/architectures only, not Mac runtime compatibility.
Archives restore executable bits for native code when exporting from Windows.
"""
from __future__ import annotations

import argparse
import datetime as dt
import hashlib
import json
from pathlib import Path
import plistlib
import struct
import tarfile

CPUS = {0x01000007: "x86_64", 0x0100000C: "arm64", 7: "i386", 12: "arm"}
THIN = {b"\xce\xfa\xed\xfe": "<", b"\xcf\xfa\xed\xfe": "<",
        b"\xfe\xed\xfa\xce": ">", b"\xfe\xed\xfa\xcf": ">"}
FAT = {b"\xca\xfe\xba\xbe": (">", False), b"\xbe\xba\xfe\xca": ("<", False),
       b"\xca\xfe\xba\xbf": (">", True), b"\xbf\xba\xfe\xca": ("<", True)}


def digest(path: Path) -> str:
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def macho(path: Path) -> list[dict]:
    """Read native architecture tables without loading entire large binaries."""
    if path.is_symlink() or not path.is_file():
        return []
    size = path.stat().st_size
    with path.open("rb") as stream:
        first = stream.read(32)
        if len(first) < 16:
            return []
        if first[:4] in THIN:
            endian = THIN[first[:4]]
            cpu, _, kind = struct.unpack_from(endian + "III", first, 4)
            return [{"architecture": CPUS.get(cpu, hex(cpu)), "file_type": kind}]
        if first[:4] not in FAT:
            return []
        endian, is64 = FAT[first[:4]]
        count = struct.unpack_from(endian + "I", first, 4)[0]
        if count < 1 or count > 32:
            return []  # CAFEBABE may also be a Java class file.
        record_size = 32 if is64 else 20
        stream.seek(8)
        records = stream.read(count * record_size)
        if len(records) != count * record_size:
            raise ValueError(f"Truncated Mach-O architecture table: {path}")
        result = []
        for i in range(count):
            record = records[i * record_size:(i + 1) * record_size]
            cpu = struct.unpack_from(endian + "I", record)[0]
            offset, length = struct.unpack_from(endian + ("QQ" if is64 else "II"), record, 8)
            if offset < 8 + count * record_size or length < 16 or offset + length > size:
                raise ValueError(f"Invalid Mach-O slice bounds: {path}")
            stream.seek(offset)
            header = stream.read(16)
            if header[:4] not in THIN:
                raise ValueError(f"Invalid Mach-O slice: {path}")
            slice_cpu, _, kind = struct.unpack_from(THIN[header[:4]] + "III", header, 4)
            if slice_cpu != cpu:
                raise ValueError(f"Mach-O architecture table/header mismatch: {path}")
            result.append({"architecture": CPUS.get(cpu, hex(cpu)), "file_type": kind})
        return result


def inspect(app: Path) -> dict:
    app = app.resolve()
    plist = app / "Contents/Info.plist"
    if app.suffix != ".app" or not plist.is_file():
        raise ValueError(f"No complete Mac .app with Contents/Info.plist at: {app}")
    info = plistlib.loads(plist.read_bytes())
    executable = info.get("CFBundleExecutable", "")
    if not executable or Path(executable).name != executable:
        raise ValueError("Invalid CFBundleExecutable filename.")
    main = app / "Contents/MacOS" / executable
    arch = {entry["architecture"] for entry in macho(main)}
    if not {"x86_64", "arm64"}.issubset(arch):
        raise ValueError(f"Main executable is not universal Intel + Apple Silicon: {sorted(arch)}")
    data = app / "Contents/Resources/Data"
    if not (data / "globalgamemanagers").is_file() or not (data / "level0").is_file():
        raise ValueError("Incomplete Unity player data: globalgamemanagers/level0 is missing.")
    native = {}
    mono = []
    files = []
    for path in sorted(app.rglob("*")):
        if path.is_symlink():
            if not path.resolve().is_relative_to(app):
                raise ValueError(f"App contains an external symbolic link: {path}")
            continue
        if not path.is_file():
            continue
        files.append(path)
        slices = macho(path)
        relative = path.relative_to(app).as_posix()
        if slices:
            native[relative] = slices
            if "mono" in path.name.lower():
                mono.append(relative)
    return {"state": "mac_player_structure_verified", "app": str(app), "app_name": app.name,
            "checked_at_utc": dt.datetime.now(dt.timezone.utc).isoformat(),
            "bundle_id": info.get("CFBundleIdentifier"), "version": info.get("CFBundleShortVersionString"),
            "build": info.get("CFBundleVersion"), "minimum_macos": info.get("LSMinimumSystemVersion"),
            "main_executable": executable, "main_architectures": sorted(arch), "main_sha256": digest(main),
            "native_code": native, "mono_native_files": mono,
            "scripting_backend_evidence": "Mono native runtime present" if mono else "No Mono native runtime found; inspect build configuration",
            "file_count": len(files), "uncompressed_bytes": sum(p.stat().st_size for p in files),
            "receipt_validation_verified": False, "signature_verified": False, "mac_runtime_verified": False}


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--app", type=Path, required=True)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--archive-out", type=Path)
    args = parser.parse_args()
    try:
        report = inspect(args.app)
    except (ValueError, FileNotFoundError) as error:
        raise SystemExit(str(error))
    if args.report.resolve().is_relative_to(args.app.resolve()):
        raise SystemExit("Report must be outside the input app.")
    if args.archive_out:
        archive = args.archive_out.resolve()
        app = args.app.resolve()
        if archive.is_relative_to(app) or args.report.resolve().is_relative_to(app):
            raise SystemExit("Archive and report must be outside the input app.")
        if archive.exists():
            raise SystemExit(f"Refusing to overwrite archive: {archive}")
        archive.parent.mkdir(parents=True, exist_ok=True)
        native_names = set(report["native_code"])
        def permissions(member: tarfile.TarInfo) -> tarfile.TarInfo:
            relative = member.name.removeprefix(app.name + "/")
            member.uid = member.gid = 0
            member.uname = member.gname = ""
            if member.isdir() or relative in native_names or relative.startswith("Contents/MacOS/"):
                member.mode = 0o755
            elif member.isfile():
                member.mode = 0o644
            return member
        with tarfile.open(archive, "w:gz", compresslevel=6, dereference=False) as output:
            output.add(app, arcname=app.name, filter=permissions)
        report.update(archive=str(archive), archive_sha256=digest(archive), archive_bytes=archive.stat().st_size)
    args.report.parent.mkdir(parents=True, exist_ok=True)
    args.report.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(f"Verified universal player structure: {report['bundle_id']} {report['version']} ({report['build']})")
    print(f"Report: {args.report}")
    print("Mac launch, receipt validation, and sandbox/runtime compatibility remain unverified.")


if __name__ == "__main__":
    main()
