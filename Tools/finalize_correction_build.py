"""Verify and copy Windows 0.7 with evidence of the actual review outcome."""
from pathlib import Path
from zipfile import ZipFile
import hashlib,json,re,shutil
root=Path(__file__).resolve().parents[1]
evidence=Path(r'C:\SpeedRoversCapture')
captures=evidence/'CorrectionsFinal'
play=Path(r'C:\DRIVE\src\Speed Rovers Reborn\Play')
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
archive=root/'Builds/Speed-Rovers-Reborn-Windows-Rush.zip'
with ZipFile(archive) as z: assert z.testzip() is None
assert 'Build Finished, Result: Success.' in (evidence/'correction-build-final.log').read_text(errors='replace')
log=(evidence/'correction-player-final.log').read_text(errors='replace')
assert 'Rush review completed' in log
assert not any(s in log for s in ['NullReferenceException','IndexOutOfRangeException','ArgumentException','Shader error'])
frames=list((captures/'motion').glob('*.png'));assert len(frames)>=300
assert sha(captures/'paused.png')==sha(captures/'paused-later.png')
checks=int(re.search(r'checks passed: (\d+)',(evidence/'correction-checks-final.log').read_text(errors='replace'))[1])
pace=(evidence/'CorrectionPaceFinal/summary.txt').read_text()
manifest=root/'Builds/build-artifacts.json';rows=json.loads(manifest.read_text())
entry=next(e for e in rows if e['file']==archive.name)
entry.update(version='0.7.0',bytes=archive.stat().st_size,sha256=sha(archive),zipError=None)
manifest.write_text(json.dumps(rows,indent=2)+'\n')
shutil.copytree(root/'Builds/WindowsRush',play,dirs_exist_ok=True)
for file in (root/'Builds/WindowsRush').rglob('*'):
    if file.is_file():assert sha(file)==sha(play/file.relative_to(root/'Builds/WindowsRush'))
preview=root/'Preview/Rush';preview.mkdir(parents=True,exist_ok=True)
for name,m in [('downtown',100),('roadworks',390),('bridge',735),('bridge-exit',1188),('outskirts',1460),('city-day',2280),('italy-coast',3020),('tokyo-night',3710),('alps',4480)]:
    shutil.copy2(captures/f'route-{m}.png',preview/(name+'.png'))
for name in ['pace.csv','summary.txt']:
    shutil.copy2(evidence/'CorrectionPaceFinal'/name,preview/('pace-'+name))
report=dict(version='0.7.0',date='2026-10-13',windowsBuild='success',gameplayAssertions=checks,
    captureMode='Native Windows framebuffer; scripted viewpoints and production simulation with automatic steering',
    motionFrames=len(frames),motionFps=30,motionSeconds=len(frames)/30,
    motionEndsInCollision='Rush review collision' in log,pauseFramesIdentical=True,playerLogExceptions=False,
    realtimePace=pace,interactiveInput='Pending separate normal-player check',visualReferenceParity='Not established',
    packageSha256=sha(archive),playDirectory=str(play))
(preview/'verification.json').write_text(json.dumps(report,indent=2)+'\n')
shutil.copytree(preview,play.parent/'Preview',dirs_exist_ok=True)
print(json.dumps(report,indent=2))
