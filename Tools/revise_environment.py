from pathlib import Path
root=Path(__file__).resolve().parents[1]/'Assets/Scripts'
p=root/'SpeedRoversDistricts.cs';s=p.read_text()
a=s.index('        Color DistrictHaze(');b=s.index('        void QueueBridgeTowers()',a)
s=s[:a]+'''        Color DistrictHaze(float world) => sceneHaze;

        void DrawDistrictSky(int city,float horizon)
        {
            float total=0;
            for(int i=0;i<7;i++)
            {
                float weight=environmentWeights[i];if(weight<.001f)continue;
                Panorama(i,out Texture2D sheet,out ArtRegion r);
                total+=weight;
                float width=Mathf.Max(620,horizon*r.w/r.h);
                float pan=Mathf.Sin(distance/350)*30+lane*8;
                GUI.color=new Color(1,1,1,weight/total);
                Region(sheet,r,(540-width)/2+pan,0,width,horizon+2);
            }
            GUI.color=Color.white;
        }

'''+s[b:]
p.write_text(s)
p=root/'SpeedRoversPainted.cs';s=p.read_text()
s=s.replace('float horizon = RoadProjection.ScreenY(distance, 0);','UpdateEnvironment();\n            float horizon = RoadProjection.ScreenY(distance, 0);',1)
a=s.index('                Color verge=');b=s.index('                bool junction',a)
s=s[:a]+'''                DrawEnvironmentWater(previousY,h,z,d);
                GUI.color=Color.Lerp(sceneGround,sceneHaze,fog*.5f);
                Rect land=LandStrip(distance+z,center,half,previousY,h);
                if(land.width>0)GUI.DrawTextureWithTexCoords(land,asphalt,new Rect(0,v,3,dv));
                GUI.color=Color.white;
'''+s[b:]
s=s.replace('float shoulder = half * (urban ? .38f : .075f) + .6f;','float shoulder = half * (district==1 ? .70f : urban ? .38f : .075f) + .6f;')
a=s.index('                Color roadTint=');b=s.index('                GUI.DrawTextureWithTexCoords(new Rect(junction',a)
s=s[:a]+'''                GUI.color=Color.Lerp(sceneRoad,sceneHaze,fog*.5f);
'''+s[b:]
s=s.replace('Color marking = Color.Lerp(district==0?new Color(.89f,.77f,.43f):new Color(.90f,.89f,.82f), haze, fog);','Color marking=Color.Lerp(sceneMarking,haze,fog);')
p.write_text(s)
print('Unified sky, water and surface lighting; continuous shore geometry')
