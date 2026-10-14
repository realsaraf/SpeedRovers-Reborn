using UnityEngine;

namespace SpeedRovers.Reborn
{
    public sealed partial class SpeedRoversGame
    {
        readonly float[] environmentWeights=new float[7];
        Color sceneHaze,sceneRoad,sceneGround,sceneMarking;
        void UpdateEnvironment()
        {
            for(int i=0;i<7;i++)environmentWeights[i]=0;
            float look=distance+220;
            int district=RoadRoute.District(look);
            float boundary=Mathf.Floor(look/RoadRoute.Length)*RoadRoute.Length+RoadRoute.Starts[district];
            float blend=Mathf.SmoothStep(0,1,(distance-boundary+220)/360);
            if(distance<480) {district=0;blend=1;}
            // Keep the sunset panorama through the river landing; change the
            // distant landscape only once roadside vegetation encloses it.
            float route=Mathf.Repeat(distance,RoadRoute.Length);
            if(route>=1030 && route<1680)
            {district=2;blend=Mathf.SmoothStep(0,1,(route-1320)/360);}
            environmentWeights[district]=blend;
            environmentWeights[(district+6)%7]=1-blend;
            sceneHaze=sceneRoad=sceneGround=sceneMarking=Color.clear;
            for(int i=0;i<7;i++)
            {
                float w=environmentWeights[i];
                Color haze=i==0?HazeColors[selectedCity]:i==5?new Color(.20f,.29f,.46f):i==1?new Color(.76f,.57f,.42f):new Color(.67f,.79f,.85f);
                Color road=i==1?new Color(.86f,.79f,.70f):i==5?new Color(.51f,.57f,.65f):i==0?new Color(.77f,.78f,.80f):new Color(.93f,.94f,.94f);
                Color ground=i==2 || i==6?new Color(.44f,.55f,.29f):i==3?new Color(.68f,.65f,.57f):i==4?new Color(.63f,.57f,.41f):i==1?new Color(.50f,.48f,.37f):GroundColors[selectedCity];
                sceneHaze+=haze*w;sceneRoad+=road*w;sceneGround+=ground*w;
                sceneMarking+=(i==0?new Color(.89f,.77f,.43f):new Color(.90f,.89f,.82f))*w;
            }
        }

        void Panorama(int district,out Texture2D sheet,out ArtRegion region)
        {
            sheet=district==0 || district==5?paintedWorlds:district==4 || district==6?travelSkies:districtSkies;
            region=district==0?paintedRegions.worlds[selectedCity]:district==5?paintedRegions.worlds[4]:district==4 || district==6?districtRegions.landscapes[district==4?0:1]:districtRegions.skies[district-1];
        }

        void DrawEnvironmentWater(float y,float h,float z,float d)
        {
            float total=0;
            for(int i=0;i<7;i++)
            {
                float weight=environmentWeights[i];if(weight<.001f)continue;
                // Inland panoramas contain trees at their bottom edge. Never
                // stretch those pixels into the river during a transition.
                int waterDistrict=i==2 || i==3?1:i;
                Panorama(waterDistrict,out Texture2D sheet,out ArtRegion r);
                total+=weight;GUI.color=new Color(1,1,1,weight/total);
                float sample=.996f-Mathf.Clamp01(d)*.17f+.003f*Mathf.Sin(animationTime*1.3f+z*.055f);
                float horizon=RoadProjection.ScreenY(distance,0);
                float width=Mathf.Max(620,horizon*r.w/r.h),pan=Mathf.Sin(distance/350)*30+lane*8;
                float u=(width-540)*.5f/width-pan/width;
                float span=540/width;
                if(i==4 || i==6) {u=.68f;span=.30f;sample=.88f+.025f*Mathf.Sin(animationTime*.5f+d*4);}
                GUI.DrawTextureWithTexCoords(new Rect(0,y,540,h),sheet,new Rect((r.x+r.w*u)/sheet.width,1-(r.y+r.h*sample)/sheet.height,r.w*span/sheet.width,.0005f));
            }
            GUI.color=Color.white;
        }

        Rect LandStrip(float world,float center,float half,float y,float h)
        {
            float m=Mathf.Repeat(world,RoadRoute.Length);
            if(m>430 && m<1520)
            {
                float bank=Mathf.Max(700-m,m-1250);
                float width=bank<=0?0:half*(1.7f+bank*.055f);
                return new Rect(center-width,y,width*2,h);
            }
            float coastDistance=Mathf.Max(2800-m,m-3500);
            float alpsDistance=Mathf.Max(4250-m,m-5000);
            if(coastDistance<200 || alpsDistance<200)
            {
                bool alpine=alpsDistance<coastDistance;
                float outside=Mathf.Max(0,alpine?alpsDistance:coastDistance);
                float edge=center+half*((alpine?2.2f:1.24f)+outside*.075f);
                return new Rect(0,y,Mathf.Max(0,edge),h);
            }
            return new Rect(0,y,540,h);
        }

        void DrawRiverbank(RoadSprite entry,float depth)
        {
            float m=Mathf.Repeat(distance+entry.z,RoadRoute.Length);
            float bankHalf=1.7f+Mathf.Max(0,m-1250)*.055f;
            int side=entry.side%2==0?-1:1;
            float edge=1.9f+(entry.side/2)*1.7f;
            if(edge>bankHalf-.15f)return;
            Vector2 p=RoadProjection.Project(distance,entry.z,side*edge/RoadProjection.LaneSpacing,lane);
            ArtRegion tree=districtRegions.tree[0];
            float h=(330+55*Mathf.Sin(entry.index*1.7f+entry.side))*(.02f+depth*.98f);
            float w=h*tree.w/tree.h;
            if(p.x+w*.5f<0 || p.x-w*.5f>540)return;
            Region(dayTree,tree,p.x-w*.5f,p.y-h,w,h,side<0);
        }
    }
}
