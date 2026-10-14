#!/usr/bin/env bash
set -euo pipefail

# Packaged beside the Linux player. The optional Mesa software path is useful
# on WSLg hosts whose NVIDIA D3D12 driver crashes while destroying the GL context.
sr_launcher_directory="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
if [[ "${1:-}" == "--software-rendering" ]]; then
    export LIBGL_ALWAYS_SOFTWARE=1
    shift
fi
exec "$sr_launcher_directory/Speed Rovers Reborn.x86_64" "$@"
