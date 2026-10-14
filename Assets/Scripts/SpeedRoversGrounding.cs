using UnityEngine;

namespace SpeedRovers.Reborn
{
    public sealed partial class SpeedRoversGame
    {
        Texture2D fastAsphalt, rollingTire, spinningRim;
        Material tireMotion;
        void InitGrounding()
        {
            tireMotion=new Material(Resources.Load<Shader>("Painted/TireMotion"));
            fastAsphalt=new Texture2D(256,256,TextureFormat.RGB24,false);
            fastAsphalt.wrapMode=TextureWrapMode.Repeat;
            for(int y=0;y<256;y++)for(int x=0;x<256;x++)
            {
                float v=.42f+Mathf.PerlinNoise(x*.7f,y*.027f)*.09f+Mathf.PerlinNoise(x*.14f,y*.008f)*.065f;
                fastAsphalt.SetPixel(x,y,new Color(v*.94f,v*.97f,v));
            }
            fastAsphalt.Apply(false,true);
            // A full rolling rubber surface replaces the static painted tread.
            // Sixteen consecutive phases, slow tread and high-speed blurred bands.
            rollingTire=new Texture2D(32*16,96*2,TextureFormat.RGBA32,false);
            spinningRim=new Texture2D(32*16,48,TextureFormat.RGBA32,false);
            for(int f=0;f<16;f++)
            {
                for(int y=0;y<96;y++)for(int x=0;x<32;x++)
                {
                    float nx=(x-15.5f)/16,ny=(y-47.5f)/48;
                    float mask=Mathf.Clamp01((1-nx*nx-Mathf.Pow(Mathf.Abs(ny),5))*12);
                    float crown=Mathf.Sqrt(Mathf.Max(0,1-nx*nx));
                    float baseValue=.055f+crown*(.095f+.14f*(y/95f));
                    baseValue+=.035f*Mathf.Exp(-Mathf.Pow((nx+.25f)*3,2));
                    float phase=y+f*5.5f+Mathf.Abs(x-15.5f)*1.9f;
                    float groove=Mathf.Repeat(phase,19)<2.4f?-.075f:0;
                    float blurred=.008f*Mathf.Sin(x*.69f+f*.23f)+.007f*Mathf.Sin(y*.16f+f*.39f);
                    for(int fast=0;fast<2;fast++)
                    {
                        float v=baseValue+(fast==0?groove:blurred);
                        rollingTire.SetPixel(f*32+x,fast*96+y,new Color(v*.91f,v*.95f,v,mask));
                    }
                }
                for(int y=0;y<48;y++)for(int x=0;x<32;x++)
                {
                    float nx=(x-15.5f)/16,ny=(y-23.5f)/24;
                    float radius=Mathf.Sqrt(nx*nx+ny*ny);
                    float angle=Mathf.Atan2(ny,nx)+f*Mathf.PI/8;
                    float spokes=Mathf.Pow(Mathf.Max(0,Mathf.Cos(angle*5)),8);
                    float v=radius>.73f?.05f:.15f+spokes*.32f;
                    float alpha=Mathf.Clamp01((1-radius)*16);
                    spinningRim.SetPixel(f*32+x,y,new Color(v*.93f,v*.96f,v,alpha));
                }
            }
            foreach(var t in new[]{rollingTire,spinningRim}) {t.filterMode=FilterMode.Bilinear;t.Apply(false,true);}
        }

        void DrawRollingRear(float x,float y,float height,float lean,float turns,float velocity,Color tint)
        {
            int frame=Mathf.FloorToInt(turns*16)%16;
            float w=height*.25f,h=height*.54f;
            float blur=Mathf.SmoothStep(0,1,Mathf.InverseLerp(75,175,velocity));
            float tireX=x-w/2+lean*height*.01f;
            GUI.color=tint;
            GUI.DrawTextureWithTexCoords(new Rect(tireX,y-h,w,h),rollingTire,new Rect(frame/16f,0,1/16f,.5f));
            GUI.color=new Color(tint.r,tint.g,tint.b,tint.a*blur);
            GUI.DrawTextureWithTexCoords(new Rect(tireX,y-h,w,h),rollingTire,new Rect(frame/16f,.5f,1/16f,.5f));
            GUI.color=tint;
        }

        void DrawBikeBody(Texture2D atlas,ArtRegion region,float x,float y,float width,float height,float lean,float turns,float velocity)
        {
            if(Event.current.type!=EventType.Repaint)return;
            Rect uv=new Rect(region.x/atlas.width,1-(region.y+region.h)/atlas.height,region.w/atlas.width,region.h/atlas.height);
            tireMotion.SetVector("_Region",new Vector4(uv.x,uv.y,uv.width,uv.height));
            tireMotion.SetVector("_Tire",new Vector4(region.pivot,height*.105f/width,.49f,0));
            tireMotion.SetFloat("_Phase",turns);
            tireMotion.SetFloat("_Velocity",velocity);
            tireMotion.SetFloat("_BodyPhase",animationTime*5+velocity*.007f);
            Graphics.DrawTexture(new Rect(x-width*region.pivot,y-height,width,height),atlas,uv,0,0,0,0,GUI.color,tireMotion);
        }

        void BikeBand(Texture2D atlas,ArtRegion r,float x,float y,float w,float h,float lean,float top,float bottom,float left,float right)
        {
            float shift=lean*h*.24f*(1-top)*(1-top);
            Rect uv=new Rect((r.x+r.w*left)/atlas.width,1-(r.y+r.h*bottom)/atlas.height,r.w*(right-left)/atlas.width,r.h*(bottom-top)/atlas.height);
            GUI.DrawTextureWithTexCoords(new Rect(x-w*r.pivot+w*left+shift,y-h+h*top,w*(right-left),h*(bottom-top)+.2f),atlas,uv,true);
        }

        void DrawCarWheels(float x,float y,float w,float h,int variant,bool front,float turns,Color tint)
        {
            int frame=Mathf.FloorToInt(turns*16)%16;
            // Rear/front quarter art: near-side tire exposes its wheel face.
            // The farther tire presents mostly tread. Both stay at road contact.
            float wheelH=h*(front?.21f:.25f),wheelW=w*.09f;
            float left=x-w*.40f,right=x+w*.36f;
            GUI.color=tint;
            GUI.DrawTextureWithTexCoords(new Rect(left-wheelW/2,y-wheelH,wheelW,wheelH),rollingTire,new Rect(frame/16f,.5f,1/16f,.5f));
            GUI.DrawTextureWithTexCoords(new Rect(right-wheelW/2,y-wheelH,wheelW,wheelH),spinningRim,new Rect(frame/16f,0,1/16f,1));
            Disc(left-wheelW*.4f,y-1,wheelW*.8f,2,new Color(0,0,0,.7f));
            Disc(right-wheelW*.4f,y-1,wheelW*.8f,2,new Color(0,0,0,.7f));
            GUI.color=tint;
        }

        void DrawRoadLightPools()
        {
            int first=Mathf.FloorToInt(distance/96);
            for(int i=first;i<=first+3;i++)for(int side=0;side<2;side++)
            {
                float z=i*96+48+side*6-distance;
                if(z<5 || z>180)continue;
                int district=RoadRoute.District(distance+z);
                if(district>1 && district!=5)continue;
                float d=RoadProjection.Depth(z);
                Vector2 p=RoadProjection.Project(distance,z,side==0?-1.25f:1.25f,lane);
                float h=RoadProjection.ScreenY(distance,RoadProjection.Depth(z-5))-RoadProjection.ScreenY(distance,RoadProjection.Depth(z+10));
                GUI.color=new Color(1,.70f,.35f,.17f);
                GUI.DrawTextureWithTexCoords(new Rect(p.x-140*d,p.y-h*.55f,280*d,h),glowSheet,new Rect(0,0,.125f,1));
            }
            GUI.color=Color.white;
        }
    }
}
