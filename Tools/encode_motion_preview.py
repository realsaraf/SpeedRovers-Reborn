"""Encode actual Unity framebuffer captures as a 30 fps review clip."""
from pathlib import Path
import subprocess
import sys
import imageio_ffmpeg

source=Path(sys.argv[1]).resolve()/'motion'
target=Path(__file__).resolve().parents[1]/'Preview/Painted/motion.mp4'
target.parent.mkdir(parents=True,exist_ok=True)
assert len(list(source.glob('*.png')))==90, 'Expected the complete 90-frame sequence'
subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-hide_banner','-loglevel','error','-y',
                '-framerate','30','-i',str(source/'%03d.png'),'-c:v','libx264','-crf','18',
                '-pix_fmt','yuv420p','-movflags','+faststart',str(target)],check=True)
print(target)
