"""Record generated sprite bounds without changing source RGBA artwork."""
from pathlib import Path
import json
from PIL import Image
import numpy as np
from scipy.ndimage import label,find_objects
root=Path(__file__).resolve().parents[1]/'Assets/Resources/Painted'
data={}
for name in ['TrafficAlignedA','TrafficAlignedB','TrafficAlignedC']:
    im=Image.open(root/(name+'.png')).convert('RGBA');a=np.asarray(im)[:,:,3]
    assert (a==0).mean()>.08,'Actual transparency is required'
    regions=[]
    for row in range(4):
        for col in range(3):
            x0,x1=round(col*im.width/3),round((col+1)*im.width/3)
            y0,y1=round(row*im.height/4),round((row+1)*im.height/4)
            labels,_=label(a[y0:y1,x0:x1]>64)
            counts=np.bincount(labels.ravel());counts[0]=0
            ys,xs=find_objects(labels)[counts.argmax()-1]
            feet=np.nonzero(a[y0+ys.stop-5:y0+ys.stop,x0+xs.start:x0+xs.stop]>64)[1]
            regions.append(dict(x=x0+xs.start,y=y0+ys.start,w=xs.stop-xs.start,h=ys.stop-ys.start,pivot=float(np.median(feet)/(xs.stop-xs.start))))
    data[name[-1].lower()]=regions
    print(name,im.size,len(regions),'regions')
(root/'AlignedTrafficRegions.json').write_text(json.dumps(data,indent=2)+'\n')
