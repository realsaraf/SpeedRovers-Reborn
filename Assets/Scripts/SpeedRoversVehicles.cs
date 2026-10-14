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
            for(int i=0;i<3;i++)alignedTraffic[i]=Resources.Load<Texture2D>("Painted/TrafficAligned"+(char)('A'+i));
            alignedRegions=JsonUtility.FromJson<AlignedRegions>(Resources.Load<TextAsset>("Painted/AlignedTrafficRegions").text);
            vehicleMotion=new Material(Resources.Load<Shader>("Painted/VehicleMotion"));
            alignedOncoming=Resources.Load<Texture2D>("Painted/OncomingAligned");
            oncomingPoseRegions=JsonUtility.FromJson<RaceSpriteRegions>(Resources.Load<TextAsset>("Painted/OncomingAlignedRegions").text);
        }

        ArtRegion[] VehicleRegions(int set) => set==0?alignedRegions.a:set==1?alignedRegions.b:alignedRegions.c;

        void VehicleFrame(Texture2D sheet,ArtRegion r,float x,float y,float height,float turns,float velocity,Color tint)
        {
            float width=height*r.w/r.h;
            Rect uv=new Rect(r.x/sheet.width,1-(r.y+r.h)/sheet.height,r.w/sheet.width,r.h/sheet.height);
            vehicleMotion.SetVector("_Region",new Vector4(uv.x,uv.y,uv.width,uv.height));
            vehicleMotion.SetFloat("_BodyPhase",animationTime*13);
            vehicleMotion.SetFloat("_Phase",turns);vehicleMotion.SetFloat("_Velocity",velocity);
            Graphics.DrawTexture(new Rect(x-width*.5f,y-height,width,height),sheet,uv,0,0,0,0,tint,vehicleMotion);
        }

        void DrawAlignedVehicle(Thing thing,Vector2 at,float depth,bool front,int variant,Color tint)
        {
            float scale=.024f+.976f*depth;
            if(front)variant%=6;
            Texture2D sheet=front?alignedOncoming:alignedTraffic[variant/4];
            ArtRegion[] views=front?oncomingPoseRegions.frames:VehicleRegions(variant/4);
            int row=front?variant*3:(variant%4)*3;
            ArtRegion reference=views[row+1];
            float width=(front && variant==5?112:!front && (variant==9||variant==10)?315:!front && variant==8?270:250)*scale;
            float height=width*reference.h/reference.w;
            // Suspension moves the body in the shader, leaving tire contacts fixed.
            SoftGroundShadow(at.x,at.y-height*.035f,width*1.22f,height*.27f,.62f);
            SoftGroundShadow(at.x,at.y-2*scale,width*.94f,12*scale,.9f);
            Disc(at.x-width*.43f,at.y-3*scale,width*.17f,7*scale,new Color(0,0,0,.85f));
            Disc(at.x+width*.26f,at.y-3*scale,width*.17f,7*scale,new Color(0,0,0,.85f));
            float velocity=Mathf.Abs(thing.travelSpeed)*3.6f;
            float turns=animationTime*Mathf.Abs(thing.travelSpeed)/2.1f;
            {
                float view=Mathf.Clamp((270-at.x)/(140+Mathf.Max(0,thing.distance-distance)*.8f)+thing.lean*.2f,-1,1);
                int pose=view<-.35f?0:view>.35f?2:1;
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

