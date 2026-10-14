"""Record atlas coordinates only; preserve original generated RGBA pixels."""
from pathlib import Path
import json
from PIL import Image
import numpy as np
from scipy.ndimage import label,find_objects
root=Path(__file__).resolve().parents[1]/'Assets/Resources/Painted'
def bounds(name, rectangles):
    a=np.asarray(Image.open(root/(name+'.png')).convert('RGBA'))[:,:,3]
    assert (a==0).mean()>.1, name+' requires actual transparency'
    result=[]
    for x,y,w,h in rectangles:
        components,_=label(a[y:y+h,x:x+w]>64)
        counts=np.bincount(components.ravel());counts[0]=0
        ys,xs=find_objects(components)[counts.argmax()-1]
        foot=np.nonzero(a[y+ys.stop-4:y+ys.stop,x+xs.start:x+xs.stop]>64)[1]
        pivot=float(np.median(foot)/(xs.stop-xs.start)) if len(foot) else .5
        result.append(dict(x=int(x+xs.start),y=int(y+ys.start),w=int(xs.stop-xs.start),h=int(ys.stop-ys.start),pivot=pivot))
    return result
data=dict(skies=[dict(x=0,y=y,w=1024,h=h) for y,h in [(0,510),(513,510),(1025,407)]],
    bridge=bounds('BridgeStructures',[(25,0,770,500),(808,0,728,500),(240,503,330,520),(1060,501,265,523)]),
    day=bounds('DayStreetProps',[(0,0,440,490),(450,0,435,490),(888,220,548,264),(1390,0,384,490),(0,505,520,382),(603,510,287,377),(1080,487,120,400),(1442,500,260,387)]),
    landscapes=[dict(x=0,y=0,w=1024,h=766),dict(x=0,y=770,w=1024,h=766)],
    travel=bounds('TravelProps',[(50,0,500,514),(650,0,280,514),(45,520,510,497),(583,520,401,503),(75,1020,450,516),(545,1028,479,508)]))
data['tree']=bounds('DayTree',[(0,0,1024,1536)])
data['portal']=bounds('BridgePortalV2',[(0,0,1536,1024)])
(root/'DistrictRegions.json').write_text(json.dumps(data,indent=2)+'\n')
print('District atlas regions:', {k:len(v) for k,v in data.items()})
