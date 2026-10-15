import json, pathlib
root=pathlib.Path('Assets/Resources/GrandTour');root.mkdir(parents=True,exist_ok=True)
sections=[('OLD TOWN',0,0,1.15,0,0),('FINANCIAL DISTRICT',3600,1,1.25,0,0),('SUSPENSION CROSSING',7200,2,.95,.1,0),('COASTAL SWEEP',10800,3,.7,.25,0),('STORM PASS',14400,4,.45,.85,.85),('MOUNTAIN TUNNEL',18000,5,.32,.5,0),('PINE VALLEY',21200,6,.7,.3,.2),('GOLDEN MILE',24600,1,.85,.1,0)]
tracks=[]
# Authored control-point pattern is compiled to a static file, never randomized at runtime.
x=[0,0,35,100,140,80,-40,-140,-180,-90,50,180,220,120,0,-110,-160,-80,40,120,70,-30,-90,-30,40,0,0]
y=[0,0,0,1,3,2,0,0,4,8,8,4,1,0,8,20,32,24,12,4,0,0,2,6,3,0,0]
for i,(name,length) in enumerate([('GRAND TOUR',28000),('COAST TO SUMMIT',32000),('STORM CHASER',36000)]):
    scale=length/28000
    seg=[]
    for label,start,theme,light,cloud,rain in sections:
        seg.append(dict(name=label,start=round(start*scale),theme=theme,light=light,rain=min(1,rain+(0.15 if i==2 and theme in [3,4,6] else 0)),cloud=cloud))
    pts=[dict(z=round(j*length/(len(x)-1)),x=v*(1 if i!=1 else -1),y=y[j]) for j,v in enumerate(x)]
    tracks.append(dict(name=name,length=length,sections=seg,points=pts))
(root/'courses.json').write_text(json.dumps(dict(routes=tracks),indent=2))
print('Wrote three predefined courses')
