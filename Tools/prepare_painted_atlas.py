"""Record tightly bounded sprite regions without changing generated artwork."""
from pathlib import Path
import json
from PIL import Image
import numpy as np
from scipy.ndimage import label, find_objects

root = Path(__file__).resolve().parents[1] / 'Assets/Resources/Painted'

def bounds(image, box):
    region = image.crop(box)
    alpha = region.getchannel('A').point(lambda x: 255 if x > 24 else 0)
    components, count = label(np.array(alpha) > 0)
    sizes = np.bincount(components.ravel())
    sizes[0] = 0
    part = find_objects(components)[int(sizes.argmax())-1]
    b = (part[1].start, part[0].start, part[1].stop, part[0].stop)
    assert b, box
    x, y, r, bottom = b
    feet = np.array(alpha)[max(y,bottom-5):bottom,x:r]
    contact = np.nonzero(feet)[1]
    pivot = float(np.median(contact)/(r-x)) if len(contact) else .5
    return dict(x=box[0]+x, y=box[1]+y, w=r-x, h=bottom-y,pivot=pivot)

rider = Image.open(root/'RiderPerspective.png').convert('RGBA')
xs = [round(i*rider.width/5) for i in range(6)]
ys = [0, 371, 641, 910, rider.height]
riders = [bounds(rider,(xs[c],ys[r],xs[c+1],ys[r+1])) for r in range(4) for c in range(5)]
scenery = Image.open(root/'Scenery.png').convert('RGBA')
rows = [0,267,496,736,981,1254]
columns = [[0,340,640,1005,1254], [0,340,718,980,1254],
           [0,333,723,995,1254], [0,341,706,1025,1254], [0,320,711,1029,1254]]
props = [bounds(scenery,(columns[r][c],rows[r],columns[r][c+1],rows[r+1]))
         for r in range(5) for c in range(4)]
traffic = Image.open(root/'TrafficAnimated.png').convert('RGBA')
tys=[0,399,751,traffic.height]
vehicles = [bounds(traffic,(round(i*traffic.width/4),tys[r],round((i+1)*traffic.width/4),tys[r+1])) for r in range(3) for i in range(4)]
foliage=Image.open(root/'FoliageAnimated.png').convert('RGBA')
fys=[0,292,573,855,1136,foliage.height]
plants=[bounds(foliage,(round(c*foliage.width/3),fys[r],round((c+1)*foliage.width/3),fys[r+1])) for r in range(5) for c in range(3)]
water=Image.open(root/'WaterAnimated.png')
waves=[dict(x=round(c*water.width/4),y=round(r*water.height/2),w=water.width/4,h=water.height/2) for r in range(2) for c in range(4)]
worlds = Image.open(root/'Worlds.png')
cuts = [0,250,502,752,984,1254]
panoramas = [dict(x=0,y=cuts[i],w=worlds.width,h=cuts[i+1]-cuts[i]) for i in range(5)]
(root/'Regions.json').write_text(json.dumps(dict(rider=riders,scenery=props,traffic=vehicles,worlds=panoramas,foliage=plants,water=waves),indent=2)+'\n')
print('Recorded 20 rider poses, 20 props, 12 traffic frames, 15 foliage frames, eight water frames and five panoramas.')
