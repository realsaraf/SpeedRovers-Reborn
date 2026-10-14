"""Use the existing Apple distribution certificate for this app's profile.

No certificate, private signing key, or API key is created. An app-specific
App Store provisioning profile is reused or created under the existing team.
"""
import base64,hashlib,json,os,plistlib,subprocess,time,urllib.error,urllib.parse,urllib.request
from pathlib import Path
from cryptography.hazmat.primitives import hashes,serialization
from cryptography.hazmat.primitives.asymmetric import ec
from cryptography.hazmat.primitives.asymmetric.utils import decode_dss_signature
def b64(data): return base64.urlsafe_b64encode(data).rstrip(b'=')
key_value=os.environ['APP_STORE_CONNECT_PRIVATE_KEY']
if 'BEGIN' not in key_value:key_value=Path(key_value).read_text()
private=serialization.load_pem_private_key(key_value.encode(),password=None)
header=b64(json.dumps({'alg':'ES256','kid':os.environ['APP_STORE_CONNECT_KEY_IDENTIFIER'],'typ':'JWT'},separators=(',',':')).encode())
payload=b64(json.dumps({'iss':os.environ['APP_STORE_CONNECT_ISSUER_ID'],'iat':int(time.time()),'exp':int(time.time())+600,'aud':'appstoreconnect-v1'},separators=(',',':')).encode())
unsigned=header+b'.'+payload
r,s=decode_dss_signature(private.sign(unsigned,ec.ECDSA(hashes.SHA256())))
token=(unsigned+b'.'+b64(r.to_bytes(32,'big')+s.to_bytes(32,'big'))).decode()
def api(path,body=None):
    request=urllib.request.Request('https://api.appstoreconnect.apple.com'+path,data=None if body is None else json.dumps(body).encode(),headers={'Authorization':'Bearer '+token,'Content-Type':'application/json'})
    try:
        with urllib.request.urlopen(request,timeout=60) as response:return json.load(response)
    except urllib.error.HTTPError as error:
        details=json.loads(error.read()).get('errors',[])
        raise RuntimeError('; '.join(e.get('detail',e.get('title','Apple API error')) for e in details)) from None
reference=plistlib.loads(subprocess.check_output(['security','cms','-D','-i',os.environ['SIGNING_REFERENCE_PROFILE']]))
fingerprints={hashlib.sha256(c).hexdigest() for c in reference['DeveloperCertificates']}
certificates=api('/v1/certificates?limit=200')['data']
matching=[c for c in certificates if hashlib.sha256(base64.b64decode(c['attributes']['certificateContent'])).hexdigest() in fingerprints]
assert len(matching)==1,'Expected the existing distribution certificate to match the reference profile'
certificate=matching[0]
bundle=os.environ['BUNDLE_ID']
bundles=api('/v1/bundleIds?'+urllib.parse.urlencode({'filter[identifier]':bundle,'limit':200}))['data']
assert len(bundles)==1,'Existing Speed Rovers bundle identifier was not found'
bundle_id=bundles[0]['id']
profiles=api('/v1/bundleIds/'+bundle_id+'/profiles?limit=200')['data']
chosen=None
for profile in profiles:
    attributes=profile['attributes']
    if attributes['profileType']!='IOS_APP_STORE' or attributes['profileState']!='ACTIVE':continue
    ids={c['id'] for c in api('/v1/profiles/'+profile['id']+'/certificates')['data']}
    if certificate['id'] in ids:chosen=profile;break
if chosen is None:
    chosen=api('/v1/profiles',{'data':{'type':'profiles','attributes':{'name':'Speed Rovers Reborn App Store','profileType':'IOS_APP_STORE'},'relationships':{'bundleId':{'data':{'type':'bundleIds','id':bundle_id}},'certificates':{'data':[{'type':'certificates','id':certificate['id']}]}}}})['data']
directory=Path.home()/'Library/MobileDevice/Provisioning Profiles';directory.mkdir(parents=True,exist_ok=True)
(directory/'speed-rovers-appstore.mobileprovision').write_bytes(base64.b64decode(chosen['attributes']['profileContent']))
print('Ready: '+chosen['attributes']['name']+' for '+bundle+'; reused existing distribution certificate.')
