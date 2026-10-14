using System;
using UnityEngine;

namespace SpeedRovers.Reborn
{
    public sealed partial class SpeedRoversGame
    {
        [Serializable] sealed class DistrictRegions { public ArtRegion[] skies, bridge, day, travel, landscapes, tree, portal; }
        Texture2D districtSkies, bridgeStructures, dayStreetProps, travelSkies, travelProps, dayTree, bridgePortal;
        DistrictRegions districtRegions;
        Material projectedLine;

        void InitDistrictArt()
        {
            districtSkies=Resources.Load<Texture2D>("Painted/DistrictSkies");
            bridgeStructures=Resources.Load<Texture2D>("Painted/BridgeStructures");
            dayStreetProps=Resources.Load<Texture2D>("Painted/DayStreetProps");
            travelSkies=Resources.Load<Texture2D>("Painted/TravelSkies");
            travelProps=Resources.Load<Texture2D>("Painted/TravelProps");
            dayTree=Resources.Load<Texture2D>("Painted/DayTree");
            bridgePortal=Resources.Load<Texture2D>("Painted/BridgePortalV2");
            districtRegions=JsonUtility.FromJson<DistrictRegions>(Resources.Load<TextAsset>("Painted/DistrictRegions").text);
            projectedLine=new Material(Resources.Load<Shader>("Painted/ProjectedLine"));
        }

        Color DistrictHaze(float world) => sceneHaze;

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

        void QueueBridgeTowers()
        {
            int circuit=Mathf.FloorToInt(distance/RoadRoute.Length);
            for(int lap=circuit;lap<=circuit+1;lap++)for(int n=0;n<3;n++)
            {
                float z=lap*RoadRoute.Length+(760+n*200)*RoadRoute.PacingScale-distance;
                if(z>=-8 && z<470)roadSprites.Add(new RoadSprite(z,4,n));
            }
        }

        void DrawBridgeWater(float y,float height,float z,float depth)
        {
            ArtRegion r=districtRegions.skies[0];
            float wave=Mathf.Repeat(animationTime*.018f+(distance+z)*.005f,.15f);
            GUI.color=new Color(.90f,.83f,.72f);
            GUI.DrawTextureWithTexCoords(new Rect(0,y,540,height),districtSkies,
                new Rect(0,1-(r.y+r.h*(.82f+wave))/districtSkies.height,1,.0015f));
            GUI.color=Color.white;
        }

        void DrawBridgeTower(RoadSprite entry,float depth)
        {
            Vector2 p=RoadProjection.Project(distance,entry.z,0,lane);
            ArtRegion r=districtRegions.portal[0];
            float w=RoadProjection.HalfWidth(depth)*3.2f,h=w*r.h/r.w;
            Region(bridgePortal,r,p.x-w*.5f,p.y-h,w,h);
        }

        void RoadLine(Vector2 a,Vector2 b,float width,Color color)
        {
            // Rotating IMGUI inside the scaled safe-area group displaced
            // cables into the letterbox. Keep the drawing quad axis-aligned.
            float pad=width*.5f+1;
            float x=Mathf.Max(0,Mathf.Min(a.x,b.x)-pad),y=Mathf.Max(0,Mathf.Min(a.y,b.y)-pad);
            float right=Mathf.Min(540,Mathf.Max(a.x,b.x)+pad),bottom=Mathf.Min(960,Mathf.Max(a.y,b.y)+pad);
            if(right<=x || bottom<=y || float.IsNaN(x+y+right+bottom))return;
            projectedLine.SetVector("_Endpoints",new Vector4(a.x-x,a.y-y,b.x-x,b.y-y));
            projectedLine.SetVector("_Size",new Vector4(right-x,bottom-y,width,0));
            Graphics.DrawTexture(new Rect(x,y,right-x,bottom-y),pixel,new Rect(0,0,1,1),0,0,0,0,color,projectedLine);
        }

        Vector2 BridgePoint(float z,int side,float height,float edge=1.60f)
        {
            float d=RoadProjection.Depth(z);
            Vector2 p=RoadProjection.Project(distance,z,side*edge/RoadProjection.LaneSpacing,lane);
            p.y-=height*(.019f+d*.981f);
            return p;
        }

        void DrawBridgeCables()
        {
            // Every support shares the road projection and moves past the camera.
            int first=Mathf.FloorToInt(distance/8);
            for(int i=first+58;i>=first-1;i--)
            {
                float world=i*8,z=world-distance;
                if(z< -7 || RoadRoute.District(world)!=1 || RoadRoute.District(world+8)!=1)continue;
                float height=CableHeight(world),heightNext=CableHeight(world+8);
                float scale=.02f+RoadProjection.Depth(z)*.98f;
                for(int side=-1;side<=1;side+=2)
                {
                    Vector2 basePoint=BridgePoint(z,side,0,1.33f),top=BridgePoint(z,side,height,1.33f);
                    RoadLine(basePoint,top,Mathf.Max(.6f,scale*4),new Color(.25f,.24f,.23f));
                    Disc(basePoint.x-5*scale,basePoint.y-2*scale,10*scale,4*scale,new Color(.025f,.03f,.035f,.65f));
                    Fill(basePoint.x-3*scale,basePoint.y-3*scale,6*scale,3*scale,new Color(.38f,.39f,.39f));
                    RoadLine(top,BridgePoint(z+8,side,heightNext,1.33f),Mathf.Max(1,scale*11),new Color(.42f,.32f,.23f));
                    RoadLine(top+Vector2.up,BridgePoint(z+8,side,heightNext,1.33f)+Vector2.up,Mathf.Max(.5f,scale*2),new Color(.91f,.68f,.38f));
                    for(int rail=0;rail<3;rail++)
                        RoadLine(BridgePoint(z,side,18+rail*17),BridgePoint(z+8,side,18+rail*17),Mathf.Max(.7f,scale*5),new Color(.69f,.59f,.44f));
                }
            }
        }

        float CableHeight(float world)
        {
            float m=RoadRoute.ArtMetres(world);
            float a,b,ha,hb,sag;
            if(m<760) {a=700;b=760;ha=24;hb=800;sag=75;}
            else if(m<960) {a=760;b=960;ha=hb=800;sag=540;}
            else if(m<1160) {a=960;b=1160;ha=hb=800;sag=540;}
            else {a=1160;b=1250;ha=800;hb=24;sag=85;}
            float t=Mathf.InverseLerp(a,b,m);
            return Mathf.Lerp(ha,hb,t)-sag*4*t*(1-t);
        }

        void DrawOpenRails()
        {
            int first=Mathf.FloorToInt(distance/8);
            for(int i=first+58;i>=first-1;i--)
            {
                float world=i*8,z=world-distance;
                int district=RoadRoute.District(world);
                if(z< -7 || (district!=2 && district!=4 && district!=6))continue;
                float scale=.02f+RoadProjection.Depth(z)*.98f;
                Color rail=district==4?new Color(.69f,.65f,.53f):new Color(.62f,.65f,.65f);
                for(int side=-1;side<=1;side+=2)
                {
                    RoadLine(BridgePoint(z,side,0,1.18f),BridgePoint(z,side,42,1.18f),Mathf.Max(1,scale*(district==4?24:7)),rail*.8f);
                    RoadLine(BridgePoint(z,side,36,1.18f),BridgePoint(z+8,side,36,1.18f),Mathf.Max(1,scale*(district==4?18:12)),rail);
                    RoadLine(BridgePoint(z,side,42,1.18f),BridgePoint(z+8,side,42,1.18f),Mathf.Max(.5f,scale*2),new Color(.91f,.91f,.83f));
                }
            }
        }

        void DrawDistrictRoadside(RoadSprite entry,float depth,int district)
        {
            if(district>=4) {DrawTravelRoadside(entry,depth,district);return;}
            float scale=.022f+depth*.978f;
            Vector2 p=RoadProjection.Project(distance,entry.z,0,lane);
            float half=RoadProjection.HalfWidth(depth);
            int variant;float height,edge;bool isTree=false;
            Texture2D sheet=dayStreetProps;ArtRegion r;
            if(district==1)
            {
                if(entry.index%4!=0)return;
                sheet=bridgeStructures;r=districtRegions.bridge[3];height=420*scale;edge=half*1.47f;
            }
            else
            {
                if(district==2)
                {
                    if(entry.index%3!=0)return;
                    variant=entry.index%15==0?7:3;height=(variant==3?520:220)*scale;edge=half*(variant==3?1.7f:1.3f);
                }
                else
                {
                    int slot=entry.index%8;
                    variant=slot<4?entry.index%2:slot==4?3:slot==5?6:slot==6?4:5;
                    height=(variant<2?1250:variant==3?620:variant==6?360:90)*scale;
                    edge=half*(variant<2?1.36f:variant==3?1.36f:1.18f);
                }
                r=districtRegions.day[variant];
                if(variant==3) {sheet=dayTree;r=districtRegions.tree[0];isTree=true;}
            }
            float w=height*r.w/r.h;
            bool building=district==3 && entry.index%8<4;
            float x=entry.side==0?p.x-edge-(building?w:w*.5f):p.x+edge-(building?0:w*.5f);
            bool flipLamp=district==1 && entry.side==1;
            if(district==1)x=p.x+(entry.side==0?-edge:edge)-w*(flipLamp?1-r.pivot:r.pivot);
            if(x>560 || x+w< -20)return;
            Color tint=GUI.color;
            if(district==1)
            {
                float foot=p.x+(entry.side==0?-edge:edge);
                Disc(foot-12*scale,p.y-3*scale,24*scale,8*scale,new Color(.01f,.02f,.025f,.72f));
                RoadLine(new Vector2(foot,p.y),new Vector2(foot+48*scale,p.y-16*scale),Mathf.Max(1,8*scale),new Color(.015f,.02f,.035f,.28f));
            }
            if(district>=2 && !building)
                Disc(x+w*.35f,p.y-height*.035f,w*1.1f,height*.09f,new Color(.08f,.12f,.15f,.23f));
            GUI.color=tint;
            float sway=isTree?Mathf.Sin(animationTime*1.2f+entry.index)*height*.008f:0;
            if(district==1)Region(sheet,r,x,p.y-height,w,height,flipLamp);
            else SkewSprite(sheet,r,x,p.y-height,w,height,sway);
            if(district==1)
                Glow(x+w*(flipLamp?.18f:.82f),p.y-height*.83f,35*scale,new Color(1,.68f,.24f,.5f+.07f*Mathf.Sin(animationTime*2+entry.index)));
            GUI.color=tint;
        }

        void DrawDistrictSurface(int district,float world,float center,float half,float y,float h,float d)
        {
            if(district==1)
            {
                // Long warm reflections stay on the asphalt plane.
                for(int side=-1;side<=1;side+=2)
                    Fill(center+side*half*.89f-half*.02f,y,half*.04f,h,new Color(1,.68f,.3f,.10f+.05f*Mathf.Sin(world*2)));
            }
            else if(district==5)
            {
                // Reflected neon stretches along the wet road, moving in world metres.
                float shimmer=.45f+.55f*Mathf.PerlinNoise(world*.22f,animationTime*.45f);
                Fill(center-half*.82f,y,half*.12f,h,new Color(.95f,.13f,.45f,.12f*shimmer));
                Fill(center+half*.68f,y,half*.16f,h,new Color(.18f,.51f,1,.16f*shimmer));
                Fill(center-half*.23f,y,half*.07f,h,new Color(1,.74f,.38f,.08f*shimmer));
            }
            else if(district>=2)
            {
                // Broken tree canopy shadows sweep across the same road coordinates.
                float shade=Mathf.PerlinNoise(world*.13f,1.7f);
                if(shade>.47f)
                {
                    float alpha=(shade-.47f)*.46f;
                    Fill(center-half,y,half*.6f,h,new Color(.09f,.15f,.20f,alpha));
                    Fill(center+half*.62f,y,half*.38f,h,new Color(.09f,.15f,.20f,alpha*.65f));
                }
            }
        }

        void DrawTravelWater(float center,float half,float y,float h,float z,int district)
        {
            float start=Mathf.Max(0,center+half*(district==6?2.2f:1.24f));
            if(start>540)return;
            ArtRegion r=districtRegions.landscapes[district==4?0:1];
            float v=.80f+Mathf.Repeat((distance+z)*.003f+animationTime*.012f,.17f);
            GUI.color=Color.white;
            GUI.DrawTextureWithTexCoords(new Rect(start,y,540-start,h),travelSkies,new Rect(.67f,1-(r.y+r.h*v)/travelSkies.height,.30f,.0015f));
        }

        void DrawTravelRoadside(RoadSprite entry,float depth,int district)
        {
            float scale=.022f+depth*.978f;
            float half=RoadProjection.HalfWidth(depth);
            Vector2 p=RoadProjection.Project(distance,entry.z,0,lane);
            int variant;float height;
            if(district==4)
            {
                if(entry.side==1)return;
                variant=entry.side==0 && entry.index%3!=0?0:entry.index%2==0?1:2;
                height=(variant==0?650:variant==1?700:260)*scale;
            }
            else if(district==6)
            {
                if(entry.side==1 && entry.index%4!=0)return;
                variant=entry.side==0 && entry.index%3!=0?2:3;
                height=(variant==2?730:800)*scale;
            }
            else
            {
                variant=entry.index%4==0?5:4;
                height=(variant==4?950:550)*scale;
            }
            ArtRegion r=districtRegions.travel[variant];
            float w=height*r.w/r.h;
            bool block=variant==0 || variant==4 || variant==2;
            float edge=half*(block?1.23f:1.5f);
            float x=entry.side==0?p.x-edge-(block?w:w*.5f):p.x+edge-(block?0:w*.5f);
            if(x>560 || x+w< -20)return;
            Color tint=GUI.color;
            float sway=block?0:Mathf.Sin(animationTime*1.15f+entry.index)*height*.009f;
            SkewSprite(travelProps,r,x,p.y-height,w,height,sway);
            if(district==5)
            {
                Color neon=entry.index%2==0?new Color(1,.1f,.3f,.27f):new Color(.12f,.44f,1,.28f);
                Glow(x+w*.2f,p.y-height*.55f,w*.18f,neon);
                Glow(x+w*.5f,p.y-height*.05f,w*.38f,new Color(1,.68f,.32f,.14f));
            }
            GUI.color=tint;
        }
    }
}
