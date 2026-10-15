"""Prepare or package the Unity Win64 player for Microsoft Store MSIX submission.

This tool does not publish, self-sign, install, or change pricing. An actual reserved
Partner Center identity is mandatory. Use --assets-only to prepare logo sizes first.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import struct
import subprocess
import tempfile
import xml.etree.ElementTree as ET

PROJECT = Path(__file__).resolve().parents[2]
DEFAULT_OUTPUT = Path(r"D:\SpeedRoversBuildCache\DesktopStore\Packages\Windows")
FOUNDATION = "http://schemas.microsoft.com/appx/manifest/foundation/windows10"
UAP = "http://schemas.microsoft.com/appx/manifest/uap/windows10"
RESCAP = "http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities"


def fail(message: str) -> None:
    raise SystemExit(message)


def sha256(path: Path) -> str:
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def validate_x64_pe(path: Path) -> None:
    with path.open("rb") as stream:
        header = stream.read(64)
        if len(header) != 64 or header[:2] != b"MZ":
            fail(f"Expected a Windows PE binary: {path}")
        stream.seek(struct.unpack_from("<I", header, 60)[0])
        signature = stream.read(6)
        if len(signature) != 6 or signature[:4] != b"PE\0\0" or struct.unpack_from("<H", signature, 4)[0] != 0x8664:
            fail(f"Expected Windows x64 machine type in: {path}")


def load_identity(path: Path) -> dict:
    config = json.loads(path.read_text(encoding="utf-8-sig"))
    for key in ("identity_name", "publisher", "publisher_display_name", "display_name", "version", "executable"):
        if not isinstance(config.get(key), str) or not config[key].strip():
            fail(f"Missing {key}. Copy the actual reserved app identity from Partner Center; do not invent it.")
        if "__" in config[key] or "REPLACE" in config[key].upper():
            fail(f"Unresolved placeholder in {key}.")
    if not re.fullmatch(r"[A-Za-z0-9.-]{3,50}", config["identity_name"]):
        fail("Identity Name must be the exact Partner Center package identity (3-50 valid characters).")
    if not config["publisher"].startswith("CN="):
        fail("Publisher must be the full CN= identity supplied by Partner Center.")
    version = config["version"].split(".")
    if len(version) != 4 or not all(x.isdigit() and 0 <= int(x) <= 65535 for x in version):
        fail("Version must have four numeric components.")
    if int(version[-1]) != 0:
        fail("Microsoft Store package revision (fourth component) must be 0.")
    if config.get("architecture", "x64") != "x64":
        fail("This package workflow currently validates Windows x64 only.")
    executable = Path(config["executable"])
    if executable.name != config["executable"] or executable.suffix.lower() != ".exe":
        fail("Executable must name the .exe directly inside the Unity build directory.")
    return config


def prepare_assets(icon: Path, target: Path) -> dict:
    from PIL import Image
    target.mkdir(parents=True, exist_ok=True)
    with Image.open(icon) as source:
        if source.width != source.height or source.width < 150:
            fail("The existing app icon must be square and at least 150 pixels wide.")
        # Packaging conversion only: preserve the existing icon design and aspect ratio.
        source = source.convert("RGBA")
        for name, size in {"StoreLogo": 50, "Square150x150Logo": 150, "Square44x44Logo": 44}.items():
            source.resize((size, size), Image.Resampling.LANCZOS).save(target / f"{name}.png")
    return {"source_icon": str(icon), "source_sha256": sha256(icon)}


def write_manifest(config: dict, target: Path) -> None:
    ET.register_namespace("", FOUNDATION)
    ET.register_namespace("uap", UAP)
    ET.register_namespace("rescap", RESCAP)
    tag = lambda name: f"{{{FOUNDATION}}}{name}"
    root = ET.Element(tag("Package"), {"IgnorableNamespaces": "uap rescap"})
    ET.SubElement(root, tag("Identity"), {
        "Name": config["identity_name"], "Publisher": config["publisher"],
        "Version": config["version"], "ProcessorArchitecture": "x64"})
    properties = ET.SubElement(root, tag("Properties"))
    for name, value in {"DisplayName": config["display_name"], "PublisherDisplayName": config["publisher_display_name"],
                        "Logo": r"StoreAssets\StoreLogo.png"}.items():
        ET.SubElement(properties, tag(name)).text = value
    resources = ET.SubElement(root, tag("Resources"))
    ET.SubElement(resources, tag("Resource"), {"Language": "en-us"})
    dependencies = ET.SubElement(root, tag("Dependencies"))
    ET.SubElement(dependencies, tag("TargetDeviceFamily"), {"Name": "Windows.Desktop",
        "MinVersion": config.get("minimum_windows_version", "10.0.17763.0"),
        "MaxVersionTested": config.get("maximum_windows_version_tested", "10.0.26100.0")})
    apps = ET.SubElement(root, tag("Applications"))
    app = ET.SubElement(apps, tag("Application"), {"Id": "SpeedRovers2", "Executable": config["executable"],
        "EntryPoint": "Windows.FullTrustApplication"})
    ET.SubElement(app, f"{{{UAP}}}VisualElements", {"DisplayName": config["display_name"],
        "Description": config.get("description", "A first-person motorcycle racing game."),
        "BackgroundColor": "transparent", "Square150x150Logo": r"StoreAssets\Square150x150Logo.png",
        "Square44x44Logo": r"StoreAssets\Square44x44Logo.png"})
    caps = ET.SubElement(root, tag("Capabilities"))
    ET.SubElement(caps, f"{{{RESCAP}}}Capability", {"Name": "runFullTrust"})
    ET.indent(root, space="  ")
    ET.ElementTree(root).write(target, encoding="utf-8", xml_declaration=True)


def find_makeappx(explicit: str | None) -> Path:
    if explicit:
        path = Path(explicit)
        if not path.is_file():
            fail(f"MakeAppx does not exist: {path}")
        return path
    sdk = Path(os.environ.get("ProgramFiles(x86)", r"C:\Program Files (x86)")) / "Windows Kits/10/bin"
    versions = [p for p in sdk.glob("*/x64/makeappx.exe") if re.fullmatch(r"\d+(\.\d+){3}", p.parent.parent.name)]
    if not versions:
        fail("Install the Windows SDK x64 MakeAppx tool or pass --makeappx.")
    return max(versions, key=lambda p: tuple(map(int, p.parent.parent.name.split("."))))


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--identity", type=Path)
    parser.add_argument("--build-dir", type=Path)
    parser.add_argument("--output-dir", type=Path, default=DEFAULT_OUTPUT)
    parser.add_argument("--icon", type=Path, default=PROJECT / "Assets/Brand/AppIcon.png")
    parser.add_argument("--assets-only", action="store_true")
    parser.add_argument("--prepare-only", action="store_true", help="Stage a reviewed manifest and files without MakeAppx.")
    parser.add_argument("--makeappx")
    args = parser.parse_args()
    output = args.output_dir.resolve()
    if args.assets_only:
        provenance = prepare_assets(args.icon.resolve(), output / "StoreAssets")
        (output / "icon-source.json").write_text(json.dumps(provenance, indent=2) + "\n", encoding="utf-8")
        print(f"Prepared existing icon sizes: {output / 'StoreAssets'}")
        return
    if not args.identity or not args.build_dir:
        parser.error("--identity and --build-dir are required; no identity or store IDs are inferred.")
    config = load_identity(args.identity)
    build = args.build_dir.resolve()
    executable = build / config["executable"]
    data = build / (executable.stem + "_Data")
    for required in (executable, build / "UnityPlayer.dll", data):
        if not required.exists():
            fail(f"Incomplete Unity Windows build: {required}")
    if not data.is_dir():
        fail(f"Unity player data must be a directory: {data}")
    validate_x64_pe(executable)
    validate_x64_pe(build / "UnityPlayer.dll")
    if output == build or build in output.parents or output in build.parents:
        fail("Build and output directories must be separate and must not contain one another.")
    if any(p.is_symlink() for p in build.rglob("*")):
        fail("Unexpected symbolic link in the Windows build; inspect it before packaging.")
    output.mkdir(parents=True, exist_ok=True)
    package = output / f"SpeedRovers2-{config['version']}-x64.msix"
    if package.exists():
        fail(f"Refusing to overwrite existing artifact: {package}")
    tool = None if args.prepare_only else find_makeappx(args.makeappx)
    build_bytes = sum(p.stat().st_size for p in build.rglob("*") if p.is_file())
    required_bytes = build_bytes * (1 if args.prepare_only else 2) + 128 * 1024 * 1024
    if shutil.disk_usage(output).free < required_bytes:
        fail(f"Insufficient staging/package space. Need approximately {required_bytes / (1024 ** 3):.1f} GiB in {output}.")
    stage = Path(tempfile.mkdtemp(prefix="staging-", dir=output))
    shutil.copytree(build, stage, dirs_exist_ok=True,
                    ignore=shutil.ignore_patterns("*_BurstDebugInformation_DoNotShip", "*.pdb"))
    provenance = prepare_assets(args.icon.resolve(), stage / "StoreAssets")
    write_manifest(config, stage / "AppxManifest.xml")
    manifest = {"state": "staged_not_packaged", "build_directory": str(build), "staging_directory": str(stage),
                "identity": config, "icon": provenance, "store_submission": False, "store_price_set": False}
    report = output / f"SpeedRovers2-{config['version']}-packaging.json"
    if not args.prepare_only:
        # Keep schema/semantic validation enabled. No /nv or test certificate is used.
        subprocess.run([str(tool), "pack", "/d", str(stage), "/p", str(package)], check=True)
        manifest.update(state="unsigned_msix_created", artifact=str(package), bytes=package.stat().st_size,
                        sha256=sha256(package))
    report.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    print(f"Packaging report: {report}")
    print("No store submission, sale price, signature, or installation was performed.")


if __name__ == "__main__":
    main()
