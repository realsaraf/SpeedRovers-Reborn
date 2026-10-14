"""Prepare this verified Unity export for Codemagic's Mac archive host."""
import os,plistlib,re
from pathlib import Path
root=Path('build/ios')
project=root/'Unity-iPhone.xcodeproj/project.pbxproj'
text=project.read_text()
text,removed=re.subn(r'    --usymtool-path=.*?\\n','',text)
project.write_text(text)
for path in root.rglob('*'):
    if path.is_file() and (path.suffix=='.sh' or path.name in {'il2cpp','il2cpp-compile','bee_backend','Unity.IL2CPP.Building.Builder'}):
        path.chmod(path.stat().st_mode|0o111)
info_path=root/'Info.plist'
with info_path.open('rb') as f: info=plistlib.load(f)
assert 'PRODUCT_BUNDLE_IDENTIFIER = com.philino.speedrovers.reborn;' in text
info['CFBundleIdentifier']='com.philino.speedrovers.reborn'
info['CFBundleDisplayName']='Speed Rovers 2'
info['CFBundleName']='Speed Rovers 2'
info['CFBundleVersion']=str(100+int(os.environ.get('BUILD_NUMBER','1')))
info['ITSAppUsesNonExemptEncryption']=False
with info_path.open('wb') as f: plistlib.dump(info,f)
print(f"Prepared {info['CFBundleIdentifier']} {info['CFBundleShortVersionString']} ({info['CFBundleVersion']}); removed {removed} Windows symbol paths")
