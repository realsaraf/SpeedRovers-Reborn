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

        void InitAlignedTraffic()
        {
            for(int i=0;i<3;i++)alignedTraffic[i]=Resources.Load<Texture2D>("Painted/TrafficAligned"+(char)('A'+i));
            alignedRegions=JsonUtility.FromJson<AlignedRegions>(Resources.Load<TextAsset>("Painted/AlignedTrafficRegions").text);
            vehicleMotion=new Material(Resources.Load<Shader>("Painted/VehicleMotion"));
        }

        ArtRegion[] VehicleRegions(int set) => set==0?alignedRegions.a:set==1?alignedRegions.b:alignedRegions.c;

        void VehicleFrame(Texture2D sheet,ArtRegion r,float x,float y,float height,float turns,float velocity,Color tint)
        {
            float width=height*r.w/r.h;
            Rect uv=new Rect(r.x/sheet.width,1-(r.y+r.h)/sheet.height,r.w/sheet.width,r.h/sheet.height);
            vehicleMotion.SetVector("_Region",new Vector4(uv.x,uv.y,uv.width,uv.height));
            vehicleMotion.SetFloat("_Phase",turns);vehicleMotion.SetFloat("_Velocity",velocity);
            Graphics.DrawTexture(new Rect(x-width*r.pivot,y-height,width,height),sheet,uv,0,0,0,0,tint,vehicleMotion);
        }

        void DrawAlignedVehicle(Thing thing,Vector2 at,float depth,bool front,int variant,Color tint)
        {
            float scale=.024f+.976f*depth;
            Texture2D sheet=front?rushOncoming:alignedTraffic[variant/4];
            ArtRegion[] views=front?rushRegions.oncoming:VehicleRegions(variant/4);
            int row=front?variant:(variant%4)*3;
            ArtRegion reference=views[front?row:row+1];
            float width=(front && variant==5?112:!front && (variant==9||variant==10)?315:!front && variant==8?270:250)*scale;
            float height=width*reference.h/reference.w;
            float bump=Mathf.Sin(animationTime*19+thing.variant)*.5f*scale;
            Disc(at.x-width*.48f,at.y-height*.07f,width*.96f,height*.085f,new Color(.02f,.02f,.02f,.27f));
            Disc(at.x-width*.43f,at.y-2*scale,width*.86f,4*scale,new Color(.01f,.01f,.01f,.60f));
            float velocity=Mathf.Abs(thing.travelSpeed)*3.6f;
            float turns=animationTime*Mathf.Abs(thing.travelSpeed)/2.1f;
            if(front)VehicleFrame(sheet,reference,at.x,at.y,height+bump,turns,velocity,tint);
            else
            {
                float view=Mathf.Clamp((270-at.x)/(140+Mathf.Max(0,thing.distance-distance)*.8f)+thing.lean*.2f,-1,1);
                int pose=view<-.35f?0:view>.35f?2:1;
                // One complete silhouette per car. Crossfading poses would
                // draw ghost tires at their different contact positions.
                VehicleFrame(sheet,views[row+pose],at.x,at.y,height+bump,turns,velocity,tint);
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
