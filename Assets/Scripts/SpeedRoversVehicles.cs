using System;
using UnityEngine;

namespace SpeedRovers.Reborn
{
    public sealed partial class SpeedRoversGame
    {
        [Serializable] sealed class AlignedRegions { public ArtRegion[] a,b,c; }
        readonly Texture2D[] alignedTraffic=new Texture2D[3];
        AlignedRegions alignedRegions;
        Material vehicleMotion;
        Texture2D alignedOncoming;
        RaceSpriteRegions oncomingPoseRegions;

        void InitAlignedTraffic()
        {
            for(int i=0;i<3;i++)alignedTraffic[i]=Resources.Load<Texture2D>("Painted/TrafficMotion"+(char)('A'+i));
            alignedRegions=new AlignedRegions { a=LoadMotionRegions("TrafficMotionA"),b=LoadMotionRegions("TrafficMotionB"),c=LoadMotionRegions("TrafficMotionC") };
            vehicleMotion=new Material(Resources.Load<Shader>("Painted/VehicleMotion"));
            alignedOncoming=Resources.Load<Texture2D>("Painted/OncomingMotion");
            oncomingPoseRegions=JsonUtility.FromJson<RaceSpriteRegions>(Resources.Load<TextAsset>("Painted/OncomingMotionRegions").text);
        }

        ArtRegion[] LoadMotionRegions(string name) => JsonUtility.FromJson<RaceSpriteRegions>(Resources.Load<TextAsset>("Painted/"+name+"Regions").text).frames;

        ArtRegion[] VehicleRegions(int set) => set==0?alignedRegions.a:set==1?alignedRegions.b:alignedRegions.c;

        void VehicleFrame(Texture2D sheet,ArtRegion r,float x,float y,float height,float turns,float velocity,Color tint)
        {
            float width=height*r.w/r.h;
            Rect uv=new Rect(r.x/sheet.width,1-(r.y+r.h)/sheet.height,r.w/sheet.width,r.h/sheet.height);
            vehicleMotion.SetVector("_Region",new Vector4(uv.x,uv.y,uv.width,uv.height));
            vehicleMotion.SetVector("_Wheels",new Vector4(r.wheelL,r.wheelR,r.wheelWidth,.13f));
            vehicleMotion.SetFloat("_BodyPhase",animationTime*10+r.x*.017f);
            vehicleMotion.SetFloat("_Phase",turns);vehicleMotion.SetFloat("_Velocity",velocity);
            Graphics.DrawTexture(new Rect(x-width*r.pivot,y-height,width,height),sheet,uv,0,0,0,0,tint,vehicleMotion);
        }

        void DrawAlignedVehicle(Thing thing,Vector2 at,float depth,bool front,int variant,Color tint)
        {
            float scale=RoadProjection.ObjectScale(depth);
            if(front)variant%=6;
            Texture2D sheet=front?alignedOncoming:alignedTraffic[variant/4];
            ArtRegion[] views=front?oncomingPoseRegions.frames:VehicleRegions(variant/4);
            int row=front?variant*5:(variant%4)*5;
            ArtRegion reference=views[row+2];
            float metres=front?(variant==1?2.05f:variant==3?1.95f:1.85f):
                (variant==9||variant==10?2.55f:variant==1||variant==8?2.05f:variant==2||variant==5?1.95f:1.85f);
            float width=RoadProjection.MetresToPixels(metres,depth);
            float height=width*reference.h/reference.w;
            if(front && variant==5) {height=RoadProjection.MetresToPixels(2.05f,depth);width=height*reference.w/reference.h;}
            // Suspension moves the body in the shader, leaving tire contacts fixed.
            SoftGroundShadow(at.x,at.y-height*.035f,width*1.22f,height*.27f,.62f);
            SoftGroundShadow(at.x,at.y-2*scale,width*.94f,12*scale,.9f);

            float velocity=Mathf.Abs(thing.travelSpeed)*3.6f;
            float turns=thing.rollingPhase;
            {
                float view=Mathf.Clamp((270-at.x)/(140+Mathf.Max(0,thing.distance-distance)*.8f)+thing.lean*.2f,-1,1);
                int pose=Mathf.Clamp(Mathf.RoundToInt(view*2)+2,0,4);
                ArtRegion chosen=views[row+pose];
                float drawnWidth=height*chosen.w/chosen.h;
                SoftGroundShadow(at.x+drawnWidth*(chosen.wheelL-chosen.pivot),at.y-1*scale,drawnWidth*chosen.wheelWidth*2,5*scale,.85f);
                SoftGroundShadow(at.x+drawnWidth*(chosen.wheelR-chosen.pivot),at.y-1*scale,drawnWidth*chosen.wheelWidth*2,5*scale,.85f);
                // One complete silhouette per car. Crossfading poses would
                // draw ghost tires at their different contact positions.
                VehicleFrame(sheet,views[row+pose],at.x,at.y,height,turns,velocity,tint);
            }
            GUI.color=tint;
            Color lamps=front?new Color(1,.89f,.62f,.45f):new Color(1,.11f,.03f,.16f);
            Glow(at.x-width*.34f,at.y-height*.36f,width*.12f,lamps);
            Glow(at.x+width*.34f,at.y-height*.36f,width*.12f,lamps);
            if(thing.police || !front && variant==11)
            {
                float flash=Mathf.Sin(animationTime*18);
                Glow(at.x-width*.22f,at.y-height*.87f,width*.2f,new Color(1,.02f,.02f,flash>0?.8f:.05f));
                Glow(at.x+width*.22f,at.y-height*.87f,width*.2f,new Color(.03f,.25f,1,flash<0?.8f:.05f));
            }
        }
    }
}

