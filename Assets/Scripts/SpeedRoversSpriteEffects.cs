using UnityEngine;

namespace SpeedRovers.Reborn
{
    public sealed partial class SpeedRoversGame
    {
        Texture2D foliageSheet, waterSheet, glowSheet, cloudSheet, tireSheet;

        void InitSpriteEffects()
        {
            foliageSheet = Resources.Load<Texture2D>("Painted/FoliageAnimated");
            waterSheet = Resources.Load<Texture2D>("Painted/WaterAnimated");
            glowSheet = new Texture2D(256, 32, TextureFormat.RGBA32, false);
            tireSheet = new Texture2D(128, 64, TextureFormat.RGBA32, false);
            cloudSheet = new Texture2D(512, 64, TextureFormat.RGBA32, false);
            for (int f = 0; f < 8; f++)
            {
                for (int y = 0; y < 32; y++)
                    for (int x = 0; x < 32; x++)
                    {
                        float radius = Vector2.Distance(new Vector2(x,y), new Vector2(15.5f,15.5f)) / 16;
                        float alpha = Mathf.Pow(Mathf.Clamp01(1-radius), 3) * (.8f + .2f*Mathf.Sin(f*.785f));
                        glowSheet.SetPixel(f*32+x,y,new Color(1,1,1,alpha));
                    }
                for (int y = 0; y < 64; y++)
                    for (int x = 0; x < 16; x++)
                    {
                        float envelope = Mathf.Clamp01(1-Mathf.Pow((x-7.5f)/8,2)-Mathf.Pow((y-31.5f)/35,2));
                        bool tread = Mathf.Repeat(y + Mathf.Abs(x-7.5f)*1.6f + f*2, 13)<2;
                        float rubber = .075f + .025f*Mathf.Cos((x-7.5f)*.22f);
                        float movingLight = Mathf.Pow(Mathf.Max(0,Mathf.Cos((y+f*8)*.098f)),8)*.095f;
                        float value = tread ? .025f : rubber+movingLight;
                        tireSheet.SetPixel(f*16+x,y,new Color(value*.93f,value*.97f,value,envelope*.82f));
                    }
            }
            for (int f = 0; f < 4; f++)
                for (int y = 0; y < 64; y++)
                    for (int x = 0; x < 128; x++)
                    {
                        float shape = Mathf.Clamp01(1-Mathf.Pow((x-63.5f)/64,2)-Mathf.Pow((y-31.5f)/32,2));
                        float noise = Mathf.PerlinNoise(x*.055f+f*.12f,y*.075f)*.6f + Mathf.PerlinNoise(x*.12f,y*.1f)*.4f;
                        cloudSheet.SetPixel(f*128+x,y,new Color(1,1,1,Mathf.Clamp01(noise-.35f)*shape));
                    }
            foreach (Texture2D sheet in new[]{glowSheet,tireSheet,cloudSheet})
            { sheet.wrapMode=TextureWrapMode.Clamp; sheet.filterMode=FilterMode.Bilinear; sheet.Apply(false,true); }
        }

        void Glow(float x, float y, float diameter, Color color)
        {
            int frame = Mathf.FloorToInt(animationTime*8) % 8;
            GUI.color = color;
            GUI.DrawTextureWithTexCoords(new Rect(x-diameter/2,y-diameter/2,diameter,diameter),glowSheet,new Rect(frame/8f,0,1/8f,1));
            GUI.color = Color.white;
        }

        void RegionPart(Texture2D sheet, ArtRegion region, float top, float bottom, float x, float y, float w, float h)
        {
            var part = new ArtRegion { x=region.x,y=region.y+region.h*top,w=region.w,h=region.h*(bottom-top) };
            Region(sheet,part,x,y+h*top,w,h*(bottom-top));
        }

        void DrawTireMotion(float x, float y)
        {
            Matrix4x4 old=GUI.matrix;
            GUIUtility.RotateAroundPivot(visualLean*22,new Vector2(x,y));
            GUI.color=new Color(1,1,1,.25f);
            int frame=Mathf.FloorToInt(animationTime*Mathf.Lerp(14,32,speed/190))%8;
            GUI.DrawTextureWithTexCoords(new Rect(x-15,y-81,30,77),tireSheet,new Rect(frame/8f,0,.125f,1));
            GUI.color=Color.white;
            GUI.matrix=old;
        }

        void DrawRushEffects()
        {
            float strength=Mathf.InverseLerp(100,220,speed);
            if(strength<=0) return;
            int frame=Mathf.FloorToInt(animationTime*12)%8;
            for(int i=0;i<7;i++)
            {
                float phase=Mathf.Repeat(animationTime*(.8f+strength*.65f)+i*.143f,1);
                float y=550+phase*400;
                float offset=phase*135;
                GUI.color=new Color(.75f,.83f,.91f,strength*phase*.13f);
                Rect uv=new Rect(frame*.125f,0,.125f,1);
                GUI.DrawTextureWithTexCoords(new Rect(58-offset,y,2+phase*2,20+phase*70),glowSheet,uv);
                GUI.DrawTextureWithTexCoords(new Rect(480+offset,y,2+phase*2,20+phase*70),glowSheet,uv);
            }
            GUI.color=Color.white;
        }

        void DrawSkyEffects(int city,float horizon,float panoramaLeft,float panoramaWidth)
        {
            int cloudFrame=Mathf.FloorToInt(animationTime*.5f)%4;
            for(int n=0;n<4;n++)
            {
                float x=Mathf.Repeat(n*237+animationTime*(2+n*.4f)-lane*7,900)-280;
                GUI.color=new Color(.64f,.7f,.85f,city==1||city==3?.18f:.10f);
                GUI.DrawTextureWithTexCoords(new Rect(x,30+n*38,340,90),cloudSheet,new Rect(cloudFrame*.25f,0,.25f,1));
            }
            GUI.color=Color.white;
            // Distant building lights brighten independently, at city scale.
            for(int i=0;i<22;i++)
            {
                float x=panoramaLeft+panoramaWidth*(.15f+Mathf.Repeat(i*.0731f,.7f));
                float y=horizon*(.69f+Mathf.Repeat(i*.137f,.15f));
                float alpha=(city==1||city==3?.08f:.24f)*(.65f+.35f*Mathf.Sin(animationTime*.6f+i*2.9f));
                Glow(x,y,5+Mathf.Repeat(i*1.7f,4),new Color(1,.68f,.29f,alpha));
            }
        }

        void DrawWaterStrip(float bank,float y,float height,float z,float depth)
        {
            if(bank<=0) return;
            int frame=Mathf.FloorToInt(animationTime*9)%8;
            ArtRegion water=paintedRegions.water[frame];
            float v=Mathf.Repeat((distance+z)*.012f+animationTime*.018f,.96f);
            Rect uv=new Rect(water.x/waterSheet.width,1-(water.y+water.h*v)/waterSheet.height,
                             water.w/waterSheet.width, -.003f);
            GUI.color=Color.Lerp(new Color(.65f,.72f,.83f),HazeColors[selectedCity],Mathf.Pow(1-Mathf.Clamp01(depth),4)*.5f);
            GUI.DrawTextureWithTexCoords(new Rect(0,y,bank,height),waterSheet,uv);
            GUI.color=Color.white;
            Fill(bank-2,y,2,height,new Color(.19f,.23f,.24f));
        }

        void DrawAnimatedPlant(int city,float x,float y,float height,int seed,Color tint)
        {
            float phase=animationTime*.65f+seed*.39f;
            int beat=Mathf.FloorToInt(phase)%4;
            int current=beat==3?2:beat==1?1:0;
            int next=(beat+1)%4==3?2:(beat+1)%4==1?1:0;
            float mix=Mathf.SmoothStep(0,1,Mathf.Repeat(phase,1));
            ArtRegion reference=paintedRegions.foliage[city*3];
            float width=height*reference.w/reference.h;
            for(int layer=0;layer<2;layer++)
            {
                ArtRegion region=paintedRegions.foliage[city*3+(layer==0?current:next)];
                GUI.color=new Color(tint.r,tint.g,tint.b,tint.a*(layer==0?1-mix:mix));
                Region(foliageSheet,region,x-width*region.pivot,y-height,width,height);
            }
            GUI.color=tint;
            // Small plants at the verge share the same wind frames, at a
            // different phase and scale so the roadside does not move in unison.
            float shrub=height*.18f;
            ArtRegion small=paintedRegions.foliage[city*3+current];
            Region(foliageSheet,small,x+width*.2f-shrub*.6f,y-shrub,shrub*1.2f,shrub);
        }

        void DrawPropLights(int city,int kind,int seed,bool flip,float x,float y,float width,float height,float depth)
        {
            float night=city==1?.2f:city==3?.5f:1;
            if(kind==3 && city!=1)
            {
                float headX=x+width*(flip?.23f:.77f);
                float headY=y+height*.09f;
                float pulse=.75f+.1f*Mathf.Sin(animationTime*2+seed);
                Glow(headX,headY,Mathf.Max(3,height*.19f),new Color(1,.68f,.26f,pulse*night));
                Glow(x+width*.5f,y+height-3,height*.26f,new Color(1,.6f,.22f,.06f*night));
            }
            else if(kind<2)
            {
                for(int row=0;row<3;row++)
                    for(int col=0;col<3;col++)
                    {
                        float clock=animationTime*.24f+seed*2+row*.73f+col*1.61f;
                        float glow=.10f+.13f*Mathf.SmoothStep(.4f,.8f,Mathf.Sin(clock));
                        float wx=.23f+col*.18f;
                        if(flip)wx=1-wx;
                        Glow(x+width*wx,y+height*(.22f+row*.16f),Mathf.Max(2,width*.09f),new Color(1,.72f,.35f,glow*night));
                    }
            }
        }
    }
}
