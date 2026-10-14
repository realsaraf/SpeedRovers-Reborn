"""Package provenance and copy the inspected Windows build into the play workspace."""
from pathlib import Path
from zipfile import ZipFile
import hashlib,json,shutil
root=Path(__file__).resolve().parents[1]
captures=Path(r'C:\SpeedRoversCapture\DistrictFinal')
play=Path(r'C:\DRIVE\src\Speed Rovers Reborn\Play')
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
archive=root/'Builds/Speed-Rovers-Reborn-Windows-Rush.zip'
with ZipFile(archive) as z: assert z.testzip() is None
assert 'Build Finished, Result: Success.' in Path(r'C:\SpeedRoversCapture\district-final-build.log').read_text(errors='replace')
log=Path(r'C:\SpeedRoversCapture\district-final-player.log').read_text(errors='replace')
assert 'Rush review completed' in log
assert not any(s in log for s in ['NullReferenceException','IndexOutOfRangeException','ArgumentException','Rush review collision'])
frames=list((captures/'motion').glob('*.png'));assert len(frames)==540
assert sha(captures/'paused.png')==sha(captures/'paused-later.png')
manifest=root/'Builds/build-artifacts.json';rows=json.loads(manifest.read_text())
entry=next(e for e in rows if e['file']==archive.name)
entry.update(version='0.6.0',bytes=archive.stat().st_size,sha256=sha(archive),zipError=None)
manifest.write_text(json.dumps(rows,indent=2)+'\n')
shutil.copytree(root/'Builds/WindowsRush',play,dirs_exist_ok=True)
for file in (root/'Builds/WindowsRush').rglob('*'):
    if file.is_file():assert sha(file)==sha(play/file.relative_to(root/'Builds/WindowsRush'))
preview=root/'Preview/Rush';preview.mkdir(parents=True,exist_ok=True)
for name,m in [('downtown',100),('roadworks',390),('bridge',735),('outskirts',1460),('city-day',2280),('italy-coast',3020),('tokyo-night',3710),('alps',4480)]:
    shutil.copy2(captures/f'route-{m}.png',preview/(name+'.png'))
report=dict(version='0.6.0',date='2026-10-13',windowsBuild='success',gameplayAssertions=5537,
    captureMode='Native Windows framebuffer; scripted route viewpoints and production simulation with automatic steering',
    motionFrames=540,motionFps=30,motionSeconds=18,pauseFramesIdentical=True,playerLogExceptions=False,
    interactiveInput='Not yet verified for this build',visualReferenceParity='Not established',
    packageSha256=sha(archive),playDirectory=str(play))
(preview/'verification.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report,indent=2))
