"""Extract alpha bounds and tire contact anchors; preserve generated pixels."""
from pathlib import Path
import json, uuid
import numpy as np
from PIL import Image
from scipy.ndimage import label, find_objects
root=Path(__file__).resolve().parents[1]/'Assets/Resources/Painted'
for name,columns,rows in [('RivalNinePoses',9,3),('OncomingAligned',3,6)]:
    im=Image.open(root/(name+'.png')).convert('RGBA')
    alpha=np.asarray(im)[:,:,3]
    print(name,im.size,'transparent',float((alpha==0).mean()))
    assert (alpha==0).mean()>.08, 'Atlas must have genuine alpha transparency'
    regions=[]
    for row in range(rows):
        for col in range(columns):
            x0,x1=round(col*im.width/columns),round((col+1)*im.width/columns)
            y0,y1=round(row*im.height/rows),round((row+1)*im.height/rows)
            labels,_=label(alpha[y0:y1,x0:x1]>100)
            counts=np.bincount(labels.ravel());counts[0]=0
            ys,xs=find_objects(labels)[counts.argmax()-1]
            feet=np.nonzero(alpha[y0+ys.stop-5:y0+ys.stop,x0+xs.start:x0+xs.stop]>100)[1]
            pivot=float(np.median(feet)/(xs.stop-xs.start)) if name=='RivalNinePoses' else .5
            regions.append(dict(x=x0+xs.start,y=y0+ys.start,w=xs.stop-xs.start,h=ys.stop-ys.start,pivot=pivot))
    (root/(name+'Regions.json')).write_text(json.dumps({'frames':regions},indent=2)+'\n')
    target=root/(name+'.png.meta')
    if not target.exists():
        source=(root/'RushHero.png.meta').read_text()
        import re
        target.write_text(re.sub(r'guid: [a-f0-9]+','guid: '+uuid.uuid4().hex,source,count=1))
