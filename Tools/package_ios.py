"""Package the exported Xcode project with executable tool permissions for macOS."""
from pathlib import Path
from stat import S_IFREG
from zipfile import ZIP_DEFLATED, ZipFile, ZipInfo

project = Path(__file__).resolve().parents[1]
source = project / "Builds" / "iOS"
if not (source / "Unity-iPhone.xcodeproj" / "project.pbxproj").is_file():
    raise SystemExit("Export the iOS Xcode project before packaging it.")
archive = project / "Builds" / "Speed-Rovers-Reborn-iOS-Xcode.zip"
mach_o_headers = {b"\xcf\xfa\xed\xfe", b"\xce\xfa\xed\xfe", b"\xca\xfe\xba\xbe", b"\xbe\xba\xfe\xca"}
with ZipFile(archive, "w", ZIP_DEFLATED, compresslevel=6) as zipped:
    for file in sorted(source.rglob("*")):
        if not file.is_file():
            continue
        data = file.read_bytes()
        executable = data[:4] in mach_o_headers or file.suffix == ".sh"
        info = ZipInfo((Path("Speed Rovers iOS") / file.relative_to(source)).as_posix())
        info.create_system = 3
        info.external_attr = (S_IFREG | (0o755 if executable else 0o644)) << 16
        info.compress_type = ZIP_DEFLATED
        zipped.writestr(info, data)
print(f"{archive} ({archive.stat().st_size / 1024 / 1024:.1f} MiB)")
