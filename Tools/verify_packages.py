"""Check packaged artifacts and refresh their checksums after a rebuild."""
from hashlib import sha256
import json
from pathlib import Path
import struct
from zipfile import ZipFile

builds = Path(__file__).resolve().parents[1] / "Builds"
files = sorted(builds.glob("Speed-Rovers-Reborn-*.zip"))
files.append(builds / "Android" / "Speed Rovers Reborn.apk")
records = []
for path in files:
    with ZipFile(path) as archive:
        error = archive.testzip()
        if error:
            raise SystemExit(f"Corrupt archive member: {path.name}: {error}")
        if path.name == "Speed-Rovers-Reborn-Linux.zip":
            launcher = archive.getinfo("Speed Rovers Reborn/launch.sh")
            assert (launcher.external_attr >> 16) & 0o111, "Linux launcher must be executable"
            assert b"\r\n" not in archive.read(launcher), "Linux launcher needs Unix line endings"
    records.append({"file": path.relative_to(builds).as_posix(),
                    "bytes": path.stat().st_size,
                    "sha256": sha256(path.read_bytes()).hexdigest(), "zipError": None})

binary = builds / "macOS" / "Speed Rovers Reborn.app" / "Contents" / "MacOS" / "Speed Rovers Reborn"
header = binary.read_bytes()[:48]
magic, count = struct.unpack_from(">II", header)
assert magic == 0xCAFEBABE and count == 2, "Expected a universal Mach-O player"
cpus = {struct.unpack_from(">I", header, 8 + index * 20)[0] for index in range(count)}
assert cpus == {0x01000007, 0x0100000C}, "Expected both Intel x64 and Apple Silicon"
(builds / "build-artifacts.json").write_text(json.dumps(records, indent=2) + "\n", encoding="utf-8")
print(f"Verified {len(records)} archives; macOS includes Intel x64 and Apple Silicon.")
