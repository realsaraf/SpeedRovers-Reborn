"""Atlas metadata only. Generated RGBA source pixels remain unchanged."""
from pathlib import Path
import json
import numpy as np
from PIL import Image
from scipy.ndimage import label, find_objects

root=Path(__file__).resolve().parents[1]/'Assets/Resources/Painted'
def regions(name, cols, rows, cuts=None):
    image=Image.open(root/(name+'.png')).convert('RGBA')
    alpha=np.array(image.getchannel('A'))
    assert (alpha==0).mean()>.08, f'{name} lacks transparent gutters'
    cuts=cuts or [round(i*image.height/rows) for i in range(rows+1)]
    result=[]
    for row in range(rows):
        for col in range(cols):
            x0,x1=round(col*image.width/cols),round((col+1)*image.width/cols)
            y0,y1=cuts[row:row+2]
            mask=alpha[y0:y1,x0:x1]>40
            parts,_=label(mask)
            counts=np.bincount(parts.ravel());counts[0]=0
            ys,xs=find_objects(parts)[counts.argmax()-1]
            feet=np.nonzero(mask[max(ys.start,ys.stop-5):ys.stop,xs.start:xs.stop])[1]
            pivot=float(np.median(feet)/(xs.stop-xs.start)) if len(feet) else .5
            result.append(dict(x=x0+xs.start,y=y0+ys.start,w=xs.stop-xs.start,h=ys.stop-ys.start,pivot=pivot))
    print(name,image.size,len(result),'regions', 'transparent',round(float((alpha==0).mean()),2))
    return result

data=dict(bikes=regions('RushBikes',3,5),traffic=regions('RushTraffic',4,3),
          buildings=regions('RushBuildings',4,2,[0,512,886]),hazards=regions('RushHazards',3,2),hero=regions('RushHero',3,3),oncoming=regions('RushOncoming',4,2))
(root/'RushRegions.json').write_text(json.dumps(data,indent=2)+'\n')
