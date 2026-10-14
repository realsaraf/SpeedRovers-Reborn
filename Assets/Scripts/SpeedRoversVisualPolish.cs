using UnityEngine;
namespace SpeedRovers.Reborn
{
    public sealed partial class SpeedRoversGame
    {
        Material foliageWind;
        Texture2D softContactShadow;

        void SoftGroundShadow(float x,float y,float width,float height,float opacity)
        {
            if(softContactShadow==null)
            {
                const int size=64;
                softContactShadow=new Texture2D(size,size,TextureFormat.RGBA32,false);
                softContactShadow.wrapMode=TextureWrapMode.Clamp;
                for(int row=0;row<size;row++)for(int col=0;col<size;col++)
                {
                    float u=(col+.5f)/size*2-1,v=(row+.5f)/size*2-1;
                    float radius=u*u+v*v;
                    float a=Mathf.Exp(-radius*4)*Mathf.Clamp01((1-radius)*5);
                    softContactShadow.SetPixel(col,row,new Color(.015f,.018f,.024f,a));
                }
                softContactShadow.Apply(false,true);
            }
            Color old=GUI.color;
            GUI.color=new Color(1,1,1,opacity);
            GUI.DrawTexture(new Rect(x-width*.5f,y-height*.5f,width,height),softContactShadow);
            GUI.color=old;
        }

        void WindSprite(Texture2D sheet,ArtRegion r,float x,float y,float w,float h,int seed)
        {
            if(foliageWind==null)foliageWind=new Material(Resources.Load<Shader>("Painted/FoliageWind"));
            Rect uv=new Rect(r.x/sheet.width,1-(r.y+r.h)/sheet.height,r.w/sheet.width,r.h/sheet.height);
            foliageWind.SetVector("_Region",new Vector4(uv.x,uv.y,uv.width,uv.height));
            foliageWind.SetFloat("_Phase",animationTime+seed*1.713f);
            Graphics.DrawTexture(new Rect(x,y,w,h),sheet,uv,0,0,0,0,GUI.color,foliageWind);
        }
    }
}
