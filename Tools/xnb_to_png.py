"""Extract uncompressed XNB Texture2D assets from the original Speed Rovers tree.

This is intentionally limited to the project's uncompressed Texture2D format.
It refuses any other reader or surface format instead of silently corrupting art.
"""

from __future__ import annotations

import argparse
import struct
from pathlib import Path

from PIL import Image


def read7(data: bytes, offset: int) -> tuple[int, int]:
    value = 0
    shift = 0
    while True:
        byte = data[offset]
        offset += 1
        value |= (byte & 0x7F) << shift
        if byte < 0x80:
            return value, offset
        shift += 7


def extract(source: Path, destination: Path) -> tuple[int, int]:
    data = source.read_bytes()
    if data[:3] != b"XNB" or data[5] & 0x80:
        raise ValueError(f"Not an uncompressed XNB: {source}")
    if struct.unpack_from("<I", data, 6)[0] != len(data):
        raise ValueError(f"XNB file length mismatch: {source}")
    offset = 10
    reader_count, offset = read7(data, offset)
    if reader_count != 1:
        raise ValueError(f"Expected one type reader: {source}")
    name_len, offset = read7(data, offset)
    reader = data[offset:offset + name_len].decode("utf-8")
    offset += name_len + 4  # name and reader version
    if "Texture2DReader" not in reader:
        raise ValueError(f"Not a texture reader: {source}")
    shared_count, offset = read7(data, offset)
    reader_index, offset = read7(data, offset)
    if shared_count != 0 or reader_index != 1:
        raise ValueError(f"Unexpected shared resources: {source}")
    surface, width, height, mip_count = struct.unpack_from("<iiii", data, offset)
    offset += 16
    if surface != 0 or width <= 0 or height <= 0 or mip_count < 1:
        raise ValueError(f"Unsupported surface format {surface}: {source}")
    byte_count = struct.unpack_from("<i", data, offset)[0]
    offset += 4
    if byte_count != width * height * 4:
        raise ValueError(f"Unexpected pixel count: {source}")
    image = Image.frombytes("RGBA", (width, height), data[offset:offset + byte_count])
    destination.parent.mkdir(parents=True, exist_ok=True)
    image.save(destination)
    return width, height


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("source", type=Path)
    parser.add_argument("destination", type=Path)
    args = parser.parse_args()
    if args.source.is_dir():
        converted = 0
        for source_file in args.source.rglob("*.xnb"):
            relative = source_file.relative_to(args.source).with_suffix(".png")
            try:
                extract(source_file, args.destination / relative)
                converted += 1
            except ValueError as error:
                print(error)
        print(f"Converted {converted} textures")
    else:
        print(extract(args.source, args.destination))
