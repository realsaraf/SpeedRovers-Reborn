"""Package the Unity desktop builds, retaining Unix executable bits."""
from pathlib import Path
import argparse
from shutil import copyfile
from stat import S_IFREG
from zipfile import ZIP_DEFLATED, ZipFile, ZipInfo

project = Path(__file__).resolve().parents[1]
builds = project / "Builds"
targets = (
    ("Windows", "Speed-Rovers-Reborn-Windows.zip"),
    ("WindowsRush", "Speed-Rovers-Reborn-Windows-Rush.zip"),
    ("WindowsPainted", "Speed-Rovers-Reborn-Windows-Painted.zip"),
    ("macOS", "Speed-Rovers-Reborn-macOS.zip"),
    ("Linux", "Speed-Rovers-Reborn-Linux.zip"),
)
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--target", choices=[target[0] for target in targets])
args = parser.parse_args()

for folder, archive_name in targets:
    if folder in ("WindowsPainted", "WindowsRush") and not args.target:
        continue
    if args.target and folder != args.target:
        continue
    source = builds / folder
    if not source.is_dir():
        raise SystemExit(f"Missing {folder} build: {source}")
    if folder == "Linux":
        copyfile(project / "Tools" / "launch-linux.sh", source / "launch.sh")
    archive = builds / archive_name
    with ZipFile(archive, "w", ZIP_DEFLATED, compresslevel=6) as zipped:
        for file in sorted(source.rglob("*")):
            if not file.is_file():
                continue
            relative = Path("Speed Rovers Reborn") / file.relative_to(source)
            info = ZipInfo(relative.as_posix())
            info.create_system = 3
            is_executable = (
                folder == "macOS" and "MacOS" in file.parts
                or folder == "Linux" and file.suffix in (".x86_64", ".so", ".sh")
            )
            info.external_attr = (S_IFREG | (0o755 if is_executable else 0o644)) << 16
            info.compress_type = ZIP_DEFLATED
            with file.open("rb") as data:
                zipped.writestr(info, data.read())
    print(f"{folder}: {archive} ({archive.stat().st_size / 1024 / 1024:.1f} MiB)")
