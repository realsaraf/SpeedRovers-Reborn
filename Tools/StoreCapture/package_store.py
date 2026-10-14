from pathlib import Path
import json,subprocess,shutil
from PIL import Image,ImageDraw
import numpy as np
root=Path(r'C:\DRIVE\src\Speed Rovers Reborn\AppStore\1.0.6')
ffmpeg=r'C:\Users\reals\miniconda3\Lib\site-packages\imageio_ffmpeg\binaries\ffmpeg-win-x86_64-v7.1.exe'
order=[(1,'downtown'),(5,'italian-coast'),(6,'tokyo-night'),(2,'river-bridge'),(7,'alps'),(4,'city-day'),(3,'outskirts'),(8,'riders'),(9,'cities'),(10,'home')]
checks=[]
for device,dimensions in [('iphone',(1320,2868)),('ipad',(2064,2752))]:
 src=root/'captures-final'/device
 if not (src/'02-continuous-race-DRAFT.f32').exists() or (src/'02-continuous-race-DRAFT.f32').stat().st_size != 18*48000*2*4:continue
 shots=root/'screenshots'/device;shots.mkdir(parents=True,exist_ok=True)
 vids=root/'video-drafts'/device;vids.mkdir(parents=True,exist_ok=True)
 for i,(original,name) in enumerate(order,1):
  path=src/(f'{original:02d}-gameplay.png' if original<8 else {8:'08-riders.png',9:'09-cities.png',10:'10-home.png'}[original]);im=Image.open(path);assert im.size==dimensions,(path,im.size)
  dest=shots/f'{i:02d}-{name}.png';im.convert('RGB').save(dest)
  checks.append({'file':str(dest.relative_to(root)),'size':im.size,'mode':'RGB','pass':True})
 for stem,duration in [('01-race-worlds-DRAFT',24),('02-continuous-race-DRAFT',18)]:
  pcm=src/(stem+'.f32');samples=np.fromfile(pcm,dtype='<f4');assert len(samples)==duration*48000*2,(pcm,len(samples));assert abs(samples).max()>.01
  dest=vids/(stem+'.mp4')
  subprocess.run([ffmpeg,'-y','-loglevel','error','-i',str(src/(stem+'-silent.mp4')),'-f','f32le','-ar','48000','-ac','2','-i',str(pcm),'-map','0:v:0','-map','1:a:0','-vf','scale=in_range=pc:out_range=tv:in_color_matrix=bt601:out_color_matrix=bt709,format=yuv420p','-c:v','libx264','-color_range','tv','-colorspace','bt709','-color_primaries','bt709','-color_trc','bt709','-profile:v','high','-level:v','4.0','-preset','fast','-b:v','10M','-maxrate','12M','-bufsize','24M','-pix_fmt','yuv420p','-r','30','-c:a','aac','-b:a','256k','-ar','48000','-ac','2','-af',f'afade=t=in:d=0.1,afade=t=out:st={duration-.25}:d=0.25','-movflags','+faststart','-t',str(duration),str(dest)],check=True)
  subprocess.run([ffmpeg,'-y','-loglevel','error','-ss','5','-i',str(dest),'-frames:v','1',str(vids/(stem+'-poster.png'))],check=True)
  checks.append({'file':str(dest.relative_to(root)),'duration':duration,'fps':30,'audio_rate':48000,'audio_channels':2,'peak':float(abs(samples).max()),'rms':float(np.sqrt(np.mean(samples*samples))),'bytes':dest.stat().st_size,'pass':dest.stat().st_size<500*1024*1024,'submission':'device recording required'})
 canvas=Image.new('RGB',(1250,1160),(19,25,35));d=ImageDraw.Draw(canvas)
 for n,path in enumerate(sorted(shots.glob('*.png'))):
  im=Image.open(path);im.thumbnail((240,530));x=n%5*250;y=n//5*580;canvas.paste(im,(x+(250-im.width)//2,y+30));d.text((x+10,y+8),path.stem,fill='white')
 canvas.save(root/(device+'-contact-sheet.jpg'))
icon=Image.open(Path(r'C:\DRIVE\src\Speed Rovers Reborn\Source\Assets\Brand\AppIcon.png')).convert('RGB');icon.resize((1024,1024),Image.Resampling.LANCZOS).save(root/'app-icon-1024.png')
(root/'asset-validation.json').write_text(json.dumps(checks,indent=2))
print('Validated',len(checks),'media assets')
