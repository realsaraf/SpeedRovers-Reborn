"""Encode production-simulation framebuffer captures; no synthesized frames."""
from pathlib import Path
import subprocess,sys
import imageio_ffmpeg

source=Path(sys.argv[1]).resolve()/'motion'
target=Path(__file__).resolve().parents[1]/'Preview/Rush/race.mp4'
target.parent.mkdir(parents=True,exist_ok=True)
frames=list(source.glob('*.png'))
assert len(frames)>=300, f'Insufficient motion capture: {len(frames)} frames'
assert all((source/f'{i:03}.png').exists() for i in range(len(frames))), 'Capture sequence has gaps'
subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-hide_banner','-loglevel','error','-y',
               '-framerate','30','-i',str(source/'%03d.png'),'-c:v','libx264','-crf','18',
               '-pix_fmt','yuv420p','-movflags','+faststart',str(target)],check=True)
print(target)
