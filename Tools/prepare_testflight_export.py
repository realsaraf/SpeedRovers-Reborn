"""Export current Unity source, then package and fingerprint it for Mac CI."""
from pathlib import Path
import argparse,hashlib,json,plistlib,subprocess,sys,tarfile
root=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(root/'CI'))
from verify_source import source_hashes
parser=argparse.ArgumentParser()
parser.add_argument('--package-only',action='store_true')
parser.add_argument('--unity',default=r'C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe')
args=parser.parse_args()
if not args.package_only:
    log=root/'ios-export.log'
    subprocess.run([args.unity,'-batchmode','-nographics','-quit','-projectPath',str(root),'-executeMethod','SpeedRoversBuild.BuildIOS','-logFile',str(log)],check=True)
    if 'Build Finished, Result: Success.' not in log.read_text(errors='replace'):raise SystemExit('Unity did not confirm export success')
source=root/'Builds/iOS'
with (source/'Info.plist').open('rb') as f:info=plistlib.load(f)
project=(source/'Unity-iPhone.xcodeproj/project.pbxproj').read_text()
assert 'PRODUCT_BUNDLE_IDENTIFIER = com.philino.speedrovers;' in project
assert info['CFBundleShortVersionString'] in ['2.0.0','2.0.1']
ci=root/'CI';ci.mkdir(exist_ok=True)
archive=ci/'ios-export.tar.gz'
with tarfile.open(archive,'w:gz',compresslevel=6) as tar:
    for file in sorted(source.iterdir()):tar.add(file,arcname=file.name)
digest=hashlib.sha256(archive.read_bytes()).hexdigest()
(ci/'ios-export.sha256').write_text(digest+'  CI/ios-export.tar.gz\n')
manifest=dict(unity='6000.6.0f1',bundleId='com.philino.speedrovers',version=info['CFBundleShortVersionString'],exportSha256=digest,files=source_hashes())
(ci/'source-manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
print(f'Packaged {archive.stat().st_size/1024/1024:.1f} MiB; {len(manifest["files"])} source files; SHA-256 {digest}')
