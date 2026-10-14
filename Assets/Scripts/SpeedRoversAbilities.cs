using UnityEngine;
using System.Collections;
using System.IO;
namespace SpeedRovers.Reborn
{
    public sealed partial class SpeedRoversGame
    {
        float fuel=1, nitro=.5f, jumpHeight, jumpVelocity, jumpCooldown;
        bool nitroActive;
        float nextFuelDistance;
        readonly AudioClip[] abilityClips=new AudioClip[4];
        Material coinMaterial;
        float HudScale => Mathf.Min(Screen.safeArea.width/540f,Screen.safeArea.height/960f);
        float HudHeight => Screen.safeArea.height/Mathf.Max(.001f,HudScale);
        Rect NitroButton => new Rect(360,HudHeight-168,162,72);
        Rect JumpButton => new Rect(360,HudHeight-86,162,68);
        void ResetRaceAbilities()
        {
            fuel=1; nitro=.5f; nitroActive=false;
            jumpHeight=jumpVelocity=jumpCooldown=0; nextFuelDistance=650;
        }
        void AdvanceAbilities(float dt)
        {
            fuel=Mathf.Max(0,fuel-dt/65f*(nitroActive?1.35f:1));
            if(nitroActive) {
                nitro=Mathf.Max(0,nitro-dt*.2f);
                if(nitro<=0 || fuel<=0)nitroActive=false;
            } else nitro=Mathf.Min(1,nitro+dt*.025f);
            jumpCooldown=Mathf.Max(0,jumpCooldown-dt);
            if(jumpVelocity!=0 || jumpHeight>0) {
                jumpVelocity-=11f*dt;
                jumpHeight=Mathf.Max(0,jumpHeight+jumpVelocity*dt);
                if(jumpHeight<=0)jumpVelocity=0;
            }
            if(fuel<=0 && speed<=2)Finish("OUT OF FUEL");
        }
        void TryNitro()
        {
            if(mode!=ScreenMode.Running || nitroActive || nitro<.5f || fuel<=0)return;
            nitroActive=true; AbilityCue(0); SoftImpactFeedback();
            ShowBanner("NITRO BOOST",.8f);
        }
        void TryJump()
        {
            if(mode!=ScreenMode.Running || jumpHeight>0 || jumpCooldown>0 || speed<10)return;
            jumpVelocity=5.6f; jumpCooldown=1.2f;
            AbilityCue(1); SoftImpactFeedback();
        }
        int AbilityButtonAt(Vector2 point)
        {
            point.y=HudHeight-point.y;
            return NitroButton.Contains(point)?1:JumpButton.Contains(point)?2:0;
        }
        bool HandleAbilityTap(Vector2 point)
        {
            int action=AbilityButtonAt(point);
            if(action==1)TryNitro(); else if(action==2)TryJump();
            return action!=0;
        }
        void SpawnFuel()
        {
            while(nextFuelDistance<distance+RoadProjection.ViewDistance) {
                int slot=Mathf.RoundToInt(nextFuelDistance/1500);
                int pickupLane=slot%3-1;
                int work=RoadRoute.WorkIndex(nextFuelDistance+35);
                if(work>=0 && nextFuelDistance<RoadRoute.WorkStart(work)+70 && pickupLane==RoadRoute.WorkLane(work))pickupLane=-pickupLane;
                things.Add(new Thing{kind=ThingKind.Fuel,distance=nextFuelDistance,lane=pickupLane});
                nextFuelDistance+=1500;
            }
        }
        void DrawAbilityHud()
        {
            Color cyan=new Color(.15f,.84f,1);
            Panel(NitroButton.x,NitroButton.y,NitroButton.width,NitroButton.height,new Color(.025f,.06f,.10f,.94f));
            float bottom=HudHeight-960;
            DrawNitroCylinder(376,801+bottom,24,29,nitroActive||nitro>=.5f);
            Label(nitroActive?"BOOSTING":nitro>=.5f?"NITRO":"CHARGING",405,798+bottom,105,29,17,nitro>=.5f||nitroActive?cyan:new Color(.6f,.68f,.74f),true,TextAnchor.MiddleCenter);
            Fill(374,833+bottom,134,6,new Color(1,1,1,.13f)); Fill(374,833+bottom,134*nitro,6,cyan);
            Fill(440,830+bottom,2,12,Color.white);
            Label(Mathf.CeilToInt(nitro*100)+"%  /  READY AT 50%",370,844+bottom,142,16,9,Color.white,true,TextAnchor.MiddleCenter);
            Panel(JumpButton.x,JumpButton.y,JumpButton.width,JumpButton.height,new Color(.055f,.07f,.08f,.94f));
            Label(jumpHeight>0?"AIRBORNE":"JUMP  ^",374,881+bottom,134,29,20,jumpCooldown<=0?menuAccent:Color.white,true,TextAnchor.MiddleCenter);
            Label("COLLECT GOLD IN THE AIR",367,915+bottom,148,16,9,Color.white,true,TextAnchor.MiddleCenter);
            if(fuel<.2f && Mathf.Sin(animationTime*4)>0)Label("LOW FUEL - FIND A CAN",175,178,180,20,12,new Color(1,.58f,.18f),true);
        }
        void DrawNitroCylinder(float x,float y,float w,float h,bool ready)
        {
            Color old=GUI.color;
            if(ready)Glow(x+w*.5f,y+h*.5f,w*1.8f,new Color(.12f,.7f,1,nitroActive?.45f:.18f));
            Fill(x+w*.39f,y-7,w*.22f,6,new Color(.7f,.79f,.86f));
            Fill(x+w*.26f,y-8,w*.48f,3,new Color(.1f,.18f,.24f));
            Disc(x,y-3,w,8,new Color(.42f,.78f,.92f));
            for(int i=0;i<16;i++) {
                float t=i/15f;
                Color metal=Color.Lerp(new Color(.025f,.16f,.3f),new Color(.22f,.75f,1),Mathf.Pow(Mathf.Sin(t*Mathf.PI),1.5f));
                Fill(x+w*t,y,w/15f,h,metal);
            }
            Disc(x,y+h-4,w,8,new Color(.04f,.24f,.40f));
            Fill(x+w*.21f,y+2,w*.08f,h-5,new Color(.74f,.96f,1,.75f));
            Label("N2O",x,y+h*.28f,w,h*.5f,8,Color.white,true,TextAnchor.MiddleCenter);
            GUI.color=old;
        }
        void DrawNitroExhaust(float x,float y,float height,float lean)
        {
            if(!nitroActive)return;
            float scale=height/165;
            // Anchor the flame at the visible right-side exhaust, not at the rider's boots.
            float nozzle=x+height*.23f-lean*height*.10f;
            float top=y-height*.34f;
            float length=(80+12*Mathf.Sin(animationTime*43))*scale;
            for(int i=11;i>=0;i--) {
                float t=i/12f;
                float width=Mathf.Lerp(13,1,t)*scale;
                float tipX=nozzle+Mathf.Sin(animationTime*33+i*.7f)*t*2*scale;
                RoadLine(new Vector2(tipX,top+t*length),new Vector2(tipX,top+(t+1f/12)*length),width,new Color(.04f,.55f,1,1-t*.85f));
                RoadLine(new Vector2(tipX,top+t*length),new Vector2(tipX,top+(t+1f/12)*length),width*.38f,new Color(.65f,.96f,1,1-t));
            }
            Glow(nozzle,top+15*scale,30*scale,new Color(.18f,.7f,1,.5f));
        }

        void DrawFuelPickup(Vector2 at,float depth)
        {
            float size=RoadProjection.MetresToPixels(.65f,depth);
            Color old=GUI.color;
            SoftGroundShadow(at.x,at.y, size*1.3f,size*.28f,.4f);
            float y=at.y-size*1.2f;
            Glow(at.x,y+size*.5f,size*1.6f,new Color(.35f,1,.1f,.22f));
            Fill(at.x-size*.44f,y,size*.88f,size,new Color(.15f,.66f,.28f));
            Fill(at.x-size*.31f,y+size*.08f,size*.62f,size*.8f,new Color(.29f,.94f,.40f));
            Fill(at.x-size*.44f,y,size*.10f,size,new Color(.65f,1,.67f));
            Fill(at.x+size*.33f,y+size*.08f,size*.11f,size*.92f,new Color(.07f,.32f,.16f));
            Fill(at.x-size*.24f,y-size*.2f,size*.48f,size*.14f,new Color(.7f,1,.73f));
            Fill(at.x+size*.23f,y-size*.14f,size*.22f,size*.14f,new Color(.1f,.22f,.14f));
            Label("F",at.x-size*.4f,y,size*.8f,size,Mathf.Max(7,Mathf.RoundToInt(size*.7f)),Color.white,true,TextAnchor.MiddleCenter);
            GUI.color=old;
        }
        void DrawSpinningCoin(Vector2 at,float depth,int variant)
        {
            if(Event.current.type!=EventType.Repaint)return;
            if(coinMaterial==null)coinMaterial=new Material(Resources.Load<Shader>("Painted/GoldCoin"));
            float size=RoadProjection.MetresToPixels(.52f,depth);
            SoftGroundShadow(at.x,at.y,size*.85f,size*.22f,.3f);
            float elevation=RoadProjection.MetresToPixels(1.05f,depth);
            coinMaterial.SetFloat("_Phase",animationTime*4.5f+variant*.32f);
            Graphics.DrawTexture(new Rect(at.x-size/2,at.y-elevation-size/2,size,size),Texture2D.whiteTexture,coinMaterial);
        }
        IEnumerator CaptureAbilities(string directory)
        {
            verificationRun=captureHold=true;
            Directory.CreateDirectory(directory);
            yield return new WaitForSeconds(1);
            StartRun();distance=1800;speed=360;ResetEngine(speed);bannerUntil=0;
            things.Clear();spawnDistance=distance+900;nextFuelDistance=distance+900;
            for(int i=0;i<3;i++)things.Add(new Thing{kind=ThingKind.Coin,distance=distance+22+i*24,lane=0,variant=i});
            things.Add(new Thing{kind=ThingKind.Fuel,distance=distance+26,lane=1});
            yield return new WaitForSeconds(.2f);
            ScreenCapture.CaptureScreenshot(Path.Combine(directory,"01-ready.png"));
            yield return new WaitForSeconds(.2f);
            TryNitro();
            animationTime+=.3f;
            yield return new WaitForSeconds(.2f);
            ScreenCapture.CaptureScreenshot(Path.Combine(directory,"02-grounded-nitro.png"));
            yield return new WaitForSeconds(.2f);
            nitroActive=false;TryJump();
            for(int i=0;i<45;i++)AdvanceAbilities(.01f);
            animationTime+=.45f;
            yield return new WaitForSeconds(.2f);
            ScreenCapture.CaptureScreenshot(Path.Combine(directory,"03-jump.png"));
            yield return new WaitForSeconds(.2f);
            jumpHeight=jumpVelocity=0;nitroActive=false;nitro=.23f;fuel=.13f;
            animationTime=2;
            yield return new WaitForSeconds(.2f);
            ScreenCapture.CaptureScreenshot(Path.Combine(directory,"04-low-fuel.png"));
            yield return new WaitForSeconds(.2f);
            Application.Quit();
        }
        void AbilityCue(int kind)
        {
            if((verificationRun&&!audioReview)||!soundEnabled||sound==null)return;
            if(abilityClips[kind]==null) {
                const int rate=22050; float duration=kind==0?.6f:kind==1?.25f:kind==2?.17f:.36f;
                float[] samples=new float[Mathf.RoundToInt(rate*duration)];
                float phase=0;
                for(int i=0;i<samples.Length;i++) {
                    float t=i/(float)rate,p=t/duration;
                    float hz=kind==0?90+650*p:kind==1?260+850*p:kind==2?1320+660*Mathf.Floor(p*3)/3:440+440*Mathf.Floor(p*4)/4;
                    phase+=2*Mathf.PI*hz/rate;
                    float wave=Mathf.Sin(phase)+.25f*Mathf.Sin(phase*2);
                    float envelope=Mathf.Min(1,p*30)*Mathf.Pow(1-p,1.4f);
                    samples[i]=wave*envelope*.65f;
                }
                abilityClips[kind]=AudioClip.Create("race ability "+kind,samples.Length,1,rate,false);
                abilityClips[kind].SetData(samples,0);
            }
            sound.PlayOneShot(abilityClips[kind],kind==0?1.3f:1);
        }
    }
}
