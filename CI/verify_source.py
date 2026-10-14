"""Reject a stale export when Unity source or settings have changed."""
import hashlib,json
from pathlib import Path
root=Path(__file__).resolve().parents[1]
def digest(path):
    data=path.read_bytes()
    if b'\0' not in data:
        try: data.decode('utf-8');data=data.replace(b'\r\n',b'\n')
        except UnicodeDecodeError: pass
    return hashlib.sha256(data).hexdigest()
def source_hashes():
    return {p.relative_to(root).as_posix():digest(p)
            for folder in ['Assets','Packages','ProjectSettings']
            for p in sorted((root/folder).rglob('*')) if p.is_file()}
if __name__=='__main__':
    expected=json.loads((root/'CI/source-manifest.json').read_text())['files']
    actual=source_hashes()
    changed=sorted(k for k in set(actual)|set(expected) if actual.get(k)!=expected.get(k))
    if changed: raise SystemExit('Unity export is stale. Run Tools/prepare_testflight_export.py and commit it. Changed files: '+', '.join(changed[:15]))
    print(f'Export matches {len(actual)} Unity source/settings files.')
