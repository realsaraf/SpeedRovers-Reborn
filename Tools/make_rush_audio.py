"""Original synthesis, no sampled or copied Road Rash audio."""
from pathlib import Path
import numpy as np
import soundfile as sf

root=Path(__file__).resolve().parents[1]/'Assets/Resources/Audio/Rush'
root.mkdir(parents=True,exist_ok=True)
rate=44100
rng=np.random.default_rng(7421)
def save(name,a):
    a=np.asarray(a)
    a=a/max(1,float(np.abs(a).max())/.88)
    sf.write(root/(name+'.wav'),a,rate,subtype='PCM_16')
    print(name,len(a)/rate,'seconds',round(float(np.max(np.abs(a))),3),'peak')

t=np.arange(rate*2)/rate
# Four-cylinder combustion fundamental at 4500 RPM; many harmonics make an
# exhaust growl instead of a pure oscillator whistle. Integer frequencies loop.
phase=2*np.pi*150*t+.085*np.sin(2*np.pi*75*t)
exhaust=np.zeros_like(t)
for harmonic in range(1,19):
    formant=1+.9*np.exp(-((150*harmonic-900)/500)**2)
    exhaust+=np.sin(phase*harmonic+.23*harmonic)*formant/(harmonic**1.1)
exhaust=np.tanh(exhaust*1.5)*.65
exhaust*=.88+.12*np.sin(2*np.pi*37.5*t)
save('engine-load',exhaust)
save('engine-coast',.44*np.sin(phase)+.18*np.sin(phase*2)+.08*np.sin(phase*5))
# Periodic filtered noise makes seamless air and tire loops.
noise=rng.normal(size=len(t))
freq=np.fft.rfftfreq(len(t),1/rate)
wind=np.fft.irfft(np.fft.rfft(noise)/(1+(freq/1700)**2),len(t))
save('wind',wind*.8)
sirent=np.arange(rate*4)/rate
frequency=720+340*(.5-.5*np.cos(2*np.pi*sirent/2))
p=2*np.pi*np.cumsum(frequency)/rate
siren=.52*np.sin(p)+.13*np.sin(p*3)+.05*np.sin(p*5)
# Short end crossfade prevents a boundary click.
fade=400
siren[-fade:]=siren[-fade:]*(1-np.arange(fade)/fade)+siren[:fade]*(np.arange(fade)/fade)
save('police-wail',siren)
pt=np.arange(int(rate*.9))/rate
env=np.sin(np.pi*pt/.9)**2
whoosh=rng.normal(size=len(pt))*.18+np.sin(2*np.pi*(190*pt-45*pt**2))*.30
save('pass-by',whoosh*env)
ct=np.arange(int(rate*1.1))/rate
impact=rng.normal(size=len(ct))*np.exp(-ct*9)*.75
impact+=np.sin(2*np.pi*(80*ct-22*ct**2))*np.exp(-ct*7)*.4
impact+=rng.normal(size=len(ct))*.17*np.exp(-ct*3)
save('impact',impact)
