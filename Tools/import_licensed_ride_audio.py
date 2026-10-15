"""Prepare the documented CC0 recordings used by the landscape racer.

Requires numpy, scipy and soundfile. Downloads only from the artists' source
pages/official asset hosts; caches source bytes outside Assets. No TTS/music AI.
"""
from pathlib import Path
import hashlib, json, os, urllib.request, zipfile
import numpy as np
import soundfile as sf
from scipy.signal import butter, sosfiltfilt, resample_poly

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/Resources/GrandTour/Audio'
CACHE = Path(os.environ.get('TEMP', '/tmp')) / 'sr2-audio-refresh'
RATE = 44100
SOURCES = {
    'pure_raceway.ogg': ('https://opengameart.org/sites/default/files/pure_raceway_bpm160_0.ogg', 'Pure Raceway', 'MintoDog', 'https://opengameart.org/content/pure-raceway'),
    'racing_title.ogg': ('https://opengameart.org/sites/default/files/racing_game_title_bpm140_0.ogg', 'Racing Game Title', 'MintoDog', 'https://opengameart.org/content/racing-game-title'),
    'motorcycle.ogg': ('https://bigsoundbank.com/UPLOAD/ogg/0281.ogg', 'Motorcycle #0281', 'DenisChardonnet', 'https://bigsoundbank.com/motorcycle-s0281.html'),
    'impact.zip': ('https://kenney.nl/media/pages/assets/impact-sounds/87b4ddecda-1677589768/kenney_impact-sounds.zip', 'Impact Sounds 1.0', 'Kenney', 'https://kenney.nl/assets/impact-sounds'),
    'interface-sounds.zip': ('https://kenney.nl/media/pages/assets/interface-sounds/fa43c1dd4d-1677589452/kenney_interface-sounds.zip', 'Interface Sounds 1.0', 'Kenney', 'https://kenney.nl/assets/interface-sounds'),
    'sci-fi-sounds.zip': ('https://kenney.nl/media/pages/assets/sci-fi-sounds/6b296f9ecf-1677589334/kenney_sci-fi-sounds.zip', 'Sci-fi Sounds 1.0', 'Kenney', 'https://kenney.nl/assets/sci-fi-sounds'),
}

def load(name):
    x, rate = sf.read(CACHE / name, always_2d=True)
    if rate != RATE:
        from math import gcd
        g = gcd(rate, RATE)
        x = resample_poly(x, RATE//g, rate//g)
    return x

def gain(x, rms=.18, peak=.84):
    x = x - x.mean(axis=0)
    return x * min(rms / max(1e-8, np.sqrt(np.mean(x*x))), peak / max(1e-8, np.max(np.abs(x))))

def edge(x, fade=.007):
    x=x.copy(); n=min(int(fade*RATE),len(x)//2)
    x[:n] *= np.linspace(0,1,n)[:,None]
    x[-n:] *= np.linspace(1,0,n)[:,None]
    return x

def loop(x, fade=.1):
    # Equal-power overlap at the physical loop seam, without a recurring silence.
    n=min(int(fade*RATE),len(x)//4); theta=np.linspace(0,np.pi/2,n)[:,None]
    cross=x[-n:]*np.cos(theta)+x[:n]*np.sin(theta)
    return np.concatenate((cross,x[n:-n]))

def lowpass(x, hz):
    return sosfiltfilt(butter(3,hz,fs=RATE,output='sos'),x,axis=0)

def mix(parts):
    channels=max(x.shape[1] for x,_ in parts)
    y=np.zeros((max(len(x)+int(t*RATE) for x,t in parts),channels))
    for x,t in parts:
        start=int(t*RATE); y[start:start+len(x)] += np.tile(x,(1,channels//x.shape[1]))
    return y

def stretched(x, factor):
    return resample_poly(x, int(1000*factor), 1000)

def main():
    CACHE.mkdir(parents=True,exist_ok=True); OUT.mkdir(parents=True,exist_ok=True)
    provenance=[]
    for filename,(url,title,author,page) in SOURCES.items():
        path=CACHE/filename
        if not path.exists():
            req=urllib.request.Request(url,headers={'User-Agent':'SpeedRoversAudioAssetPreparation/1.0'})
            path.write_bytes(urllib.request.urlopen(req).read())
        if filename.endswith('.zip'):
            with zipfile.ZipFile(path) as z: z.extractall(CACHE/path.stem)
        provenance.append(dict(file=filename,title=title,author=author,source=page,download=url,license='CC0-1.0',sha256=hashlib.sha256(path.read_bytes()).hexdigest()))

    outputs={}
    def save(name,x,source,processing,looped=False):
        sf.write(OUT/(name+'.wav'),x,RATE,subtype='PCM_16')
        outputs[name]=dict(sources=source,processing=processing,duration_seconds=round(len(x)/RATE,4),sample_rate=RATE,channels=x.shape[1],rms_dbfs=round(20*np.log10(max(1e-10,np.sqrt(np.mean(x*x)))),2),peak_dbfs=round(20*np.log10(max(1e-10,np.max(np.abs(x)))),2),clipped_samples=int(np.sum(np.abs(x)>=1)),sha256=hashlib.sha256((OUT/(name+'.wav')).read_bytes()).hexdigest())
        if looped: outputs[name]['loop_boundary_delta']=float(np.max(np.abs(x[-1]-x[0])))

    # Music keeps the composer's complete form (96/60 seconds), no short motif generation.
    save('RaceScore',gain(edge(load('pure_raceway.ogg'),.004),.205,.88),['pure_raceway.ogg'],'Full 160 BPM score, stereo 44.1 kHz PCM; DC removal, level and 4 ms boundary fade.',True)
    save('MenuScore',gain(edge(load('racing_title.ogg'),.004),.205,.88),['racing_title.ogg'],'Full 140 BPM title score, stereo 44.1 kHz PCM; DC removal, level and 4 ms boundary fade.',True)

    motor=load('motorcycle.ogg')
    save('EngineIdle',gain(loop(lowpass(motor[int(5.5*RATE):int(6.9*RATE)],2600),.12),.21,.8),['motorcycle.ogg'],'Recorded motorcycle excerpt 5.5-6.9 s, low-pass 2.6 kHz, 120 ms overlap loop, level normalization.',True)
    save('Engine',gain(loop(motor[int(1.05*RATE):int(2.85*RATE)],.12),.21,.8),['motorcycle.ogg'],'Recorded motorcycle excerpt 1.05-2.85 s, 120 ms overlap loop, level normalization.',True)
    ui=lambda f:load('interface-sounds/Audio/'+f+'.ogg')
    impact=lambda f:load('impact/Audio/'+f+'.ogg')
    scifi=lambda f:load('sci-fi-sounds/Audio/'+f+'.ogg')
    save('UI',gain(edge(ui('click_003')),.13,.6),['interface-sounds.zip:Audio/click_003.ogg'],'DC removal, level normalization, click-free edges.')
    save('Shift',gain(edge(ui('switch_004')),.18,.7),['interface-sounds.zip:Audio/switch_004.ogg'],'Mechanical shift click, DC removal, level normalization, click-free edges.')
    save('Coin',gain(edge(ui('confirmation_001')),.2,.8),['interface-sounds.zip:Audio/confirmation_001.ogg'],'Short collection cue, level normalization, click-free edges.')
    save('Fuel',gain(edge(ui('confirmation_002')),.2,.8),['interface-sounds.zip:Audio/confirmation_002.ogg'],'Distinct longer pickup cue, level normalization, click-free edges.')
    save('Jump',gain(edge(ui('open_003')),.16,.72),['interface-sounds.zip:Audio/open_003.ogg'],'Short upward gesture sound, level normalization, click-free edges.')
    save('Impact',gain(edge(mix([(impact('impactMetal_heavy_001'),0),(lowpass(impact('impactPlate_heavy_002'),2600)*.65,.025)])),.23,.82),['impact.zip:Audio/impactMetal_heavy_001.ogg','impact.zip:Audio/impactPlate_heavy_002.ogg'],'Layered recorded metal impacts, low-pass on plate layer, level normalization, click-free edges.')
    save('Crash',gain(edge(mix([(impact('impactMetal_heavy_001'),0),(impact('impactMetal_heavy_003'),.09),(impact('impactGlass_heavy_001')*.7,.04)])),.23,.82),['impact.zip:Audio/impactMetal_heavy_001.ogg','impact.zip:Audio/impactMetal_heavy_003.ogg','impact.zip:Audio/impactGlass_heavy_001.ogg'],'Layered impacts and short glass debris, level normalization, click-free edges.')
    thruster=lowpass(scifi('thrusterFire_000'),9000)
    save('Boost',gain(edge(thruster[:int(1.5*RATE)],.08),.21,.8),['sci-fi-sounds.zip:Audio/thrusterFire_000.ogg'],'1.5 s air/combustion burst, low-pass 9 kHz, level normalization, 80 ms release.')
    save('BoostLoop',gain(loop(thruster[int(1.2*RATE):int(3.2*RATE)],.15),.16,.7),['sci-fi-sounds.zip:Audio/thrusterFire_000.ogg'],'1.2-3.2 s combustion excerpt, 150 ms overlap loop, low-pass 9 kHz, level normalization.',True)
    save('Pass',gain(edge(stretched(ui('open_002'),2.4),.02),.17,.75),['interface-sounds.zip:Audio/open_002.ogg'],'Time-stretched airy sweep with level normalization and 20 ms edge fades.')
    save('Finish',gain(edge(mix([(ui('confirmation_004'),0),(ui('confirmation_001')*.65,.4)])),.18,.8),['interface-sounds.zip:Audio/confirmation_004.ogg','interface-sounds.zip:Audio/confirmation_001.ogg'],'Two-stage finish cue, level normalization, click-free edges.')
    # Airflow is a quiet broad-band texture instead of another pitched effect.
    wind=np.random.default_rng(1826).normal(0,1,(RATE*8,2))
    wind=sosfiltfilt(butter(2,[220,4200],btype='bandpass',fs=RATE,output='sos'),wind,axis=0)
    save('Wind',gain(loop(wind,.12),.12,.6),['Original procedural wind: Tools/import_licensed_ride_audio.py'],'Original deterministic band-limited noise, 220 Hz-4.2 kHz, 120 ms overlap seam, level normalization.',True)

    (OUT/'LICENSED-AUDIO.json').write_text(json.dumps(dict(verified_date='2026-10-14',license_url='https://creativecommons.org/publicdomain/zero/1.0/',sources=provenance,outputs=outputs),indent=2)+'\n')
    for pack in ['impact','interface-sounds','sci-fi-sounds']:
        (OUT/('KENNEY-'+pack.upper()+'-LICENSE.txt')).write_bytes((CACHE/pack/'License.txt').read_bytes())
    license_url='https://creativecommons.org/publicdomain/zero/1.0/legalcode.txt'
    if not (OUT/'CC0-1.0.txt').exists():
        request=urllib.request.Request(license_url,headers={'User-Agent':'Mozilla/5.0'})
        (OUT/'CC0-1.0.txt').write_bytes(urllib.request.urlopen(request).read())
    print(json.dumps(outputs,indent=2))

if __name__=='__main__': main()
