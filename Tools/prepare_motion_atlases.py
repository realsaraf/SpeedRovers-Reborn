"""Copy generated RGBA masters unchanged and record bounds/contact metadata."""
from pathlib import Path
import json, shutil
import numpy as np
from PIL import Image
from scipy.ndimage import label, find_objects

root=Path(__file__).resolve().parents[1]
generated=Path(r'C:\Users\reals\.codex\generated_images\01a128f2-902d-7951-9b91-d1a6a3f14850')
assets=root/'Assets/Resources/Painted'
masters={
 'TrafficMotionA':('982631b4-48e2-49b6-bf09-43da08f6b6fd',5,4),
 'TrafficMotionB':('7927a2cd-5d86-4b3c-95cd-4d701ded9a5e',5,4),
 'TrafficMotionC':('cafab0cf-36d3-4fab-a3ad-636f0c5cba66',5,4),
 'OncomingMotion':('f83fa9bd-508e-4377-9210-f989d8949f8f',5,6),
 'RiderMotionRed':('732c55a6-c752-4d2b-b5a1-58ff0698a251',5,3),
 'RiderMotionBlue':('c1411c48-6353-4404-be25-052c388f6a9e',5,3),
 'RiderMotionGreen':('0f88f5eb-2f95-496f-b88b-784fad9c6897',5,3),
 'RiderMotionOrange':('cffa7f2c-bf63-40a3-bf29-d197a6e98dd9',5,3),
}

def boundaries(profile,n):
    size=len(profile); result=[0]
    labels,_=label(profile<max(3,profile.max()*.015))
    gaps=[s[0] for s in find_objects(labels) if s[0].start>0 and s[0].stop<size and s[0].stop-s[0].start>=3]
    if len(gaps)>=n-1:
        gaps=sorted(sorted(gaps,key=lambda s:s.stop-s.start,reverse=True)[:n-1],key=lambda s:s.start)
        return [0]+[(s.start+s.stop)//2 for s in gaps]+[size]
    for i in range(1,n):
        center=round(i*size/n); radius=round(size/n*.20)
        lo,hi=center-radius,center+radius
        window=profile[lo:hi]; minima=np.flatnonzero(window==window.min())+lo
        result.append(int(minima[np.argmin(abs(minima-center))]))
    return result+[size]

for name,(key,cols,rows) in masters.items():
    source=generated/('exec-'+key+'.png'); target=assets/(name+'.png')
    shutil.copyfile(source,target)
    a=np.asarray(Image.open(source).convert('RGBA'))[:,:,3]
    assert (a==0).mean()>.15, name+' lacks transparency'
    yy=boundaries((a>100).sum(axis=1),rows); frames=[]
    for row in range(rows):
        xx=boundaries((a[yy[row]:yy[row+1]]>100).sum(axis=0),cols)
        for col in range(cols):
            x0,y0=xx[col],yy[row]; tile=a[y0:yy[row+1],x0:xx[col+1]]
            labels,_=label(tile>100); counts=np.bincount(labels.ravel());counts[0]=0
            ys,xs=find_objects(labels)[counts.argmax()-1]
            w,h=xs.stop-xs.start,ys.stop-ys.start
            assert w>45 and h>80,(name,row,col,w,h)
            assert (row==0 or ys.start>0) and (row==rows-1 or ys.stop<tile.shape[0]), (name,row,col,'row clips silhouette')
            crop=tile[ys,xs]; foot=np.flatnonzero((crop[-max(3,int(h*.035)):] >100).any(axis=0))
            # Contact midpoint across both wheels, not median of tire-pixel counts.
            pivot=float((foot[0]+foot[-1])*.5/w)
            low=(crop[-max(4,int(h*.10)):] >100).any(axis=0)
            groups,n=label(low); spans=find_objects(groups)
            contacts=sorted([(s[0].stop-s[0].start,(s[0].start+s[0].stop)*.5/w) for s in spans],reverse=True)
            centers=sorted([c[1] for c in contacts[:2]])
            if len(centers)<2:centers=[pivot,pivot]
            tireRow=np.flatnonzero(crop[int(h*.82)]>100)
            slope=((tireRow[0]+tireRow[-1])*.5/w-pivot)/.18 if len(tireRow) else 0
            frames.append(dict(x=x0+xs.start,y=y0+ys.start,w=w,h=h,pivot=pivot,
                wheelL=centers[0],wheelR=centers[-1],tireSlope=float(slope),
                wheelWidth=float(min(.14,max(.045,contacts[0][0]/w*.5)))))
    (assets/(name+'Regions.json')).write_text(json.dumps({'frames':frames},indent=2)+'\n')
    print(name,len(frames),'frames',Image.open(source).size)
