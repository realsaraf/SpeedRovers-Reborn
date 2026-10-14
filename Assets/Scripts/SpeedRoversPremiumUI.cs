using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SpeedRovers.Reborn
{
    public sealed partial class SpeedRoversGame
    {
        Texture2D menuHero, riderGallery, routeGallery, menuGradient, cutShape;
        Font racingBold, racingMedium;
        readonly Dictionary<int, GUIStyle> menuFonts = new Dictionary<int, GUIStyle>();
        readonly HashSet<string> reviewOwnership = new HashSet<string>();
        readonly Color menuBase = new Color(.085f,.105f,.16f);
        readonly Color menuText = new Color(.96f,.98f,1);
        readonly Color menuPanel = new Color(.15f,.19f,.27f,.96f);
        readonly Color menuMuted = new Color(.68f,.75f,.84f);
        readonly Color menuAccent = new Color(.76f,1,.14f);
        readonly Color menuGold = new Color(1,.78f,.25f);
        readonly string[] riderNotes = { "THE ORIGINAL", "COOL UNDER PRESSURE", "FIND YOUR RHYTHM", "BORN TO CHASE", "OWN THE NIGHT" };
        readonly string[] cityNotes = { "SKYLINES AFTER DARK", "GOLDEN HOUR STREETS", "LIGHTS ON THE WATER", "TIMELESS CITY LIGHTS", "NEON AFTER HOURS" };
        ScreenMode presentedMode = (ScreenMode)(-1);
        float menuEntered, noticeUntil;
        string menuNotice;
        enum MenuDialog { None, Unlock, LeaveRace }
        MenuDialog menuDialog;
        bool unlockCity;
        int unlockIndex;
        AudioSource interfaceAudio;
        AudioClip uiTap,uiSweep,uiReward;
        float rewardUntil;
        int presentedSelection=-1;
        float selectionEntered;

        void InitMenuSound()
        {
            if(interfaceAudio!=null)return;
            interfaceAudio=gameObject.AddComponent<AudioSource>();
            interfaceAudio.playOnAwake=false;interfaceAudio.spatialBlend=0;interfaceAudio.volume=.28f;
            uiTap=MenuTone("UI / Tap",new[]{620f,830f},.045f);
            uiSweep=MenuTone("UI / Move",new[]{330f,440f,660f},.055f);
            uiReward=MenuTone("UI / Unlock",new[]{523.25f,659.25f,783.99f,1046.5f},.105f);
        }
        static AudioClip MenuTone(string name,float[] notes,float duration)
        {
            const int rate=22050;int segment=Mathf.RoundToInt(rate*duration);
            float[] samples=new float[segment*notes.Length];
            for(int i=0;i<samples.Length;i++)
            {
                int n=i/segment;float t=(i%segment)/(float)rate;
                float env=Mathf.Min(t/.005f,1)*Mathf.Pow(1-t/duration,1.8f);
                samples[i]=(.24f*Mathf.Sin(t*notes[n]*Mathf.PI*2)+.06f*Mathf.Sin(t*notes[n]*Mathf.PI*4))*env;
            }
            var clip=AudioClip.Create(name,samples.Length,1,rate,false);clip.SetData(samples,0);return clip;
        }
        void MenuSound(AudioClip clip)
        {if(soundEnabled&&!verificationRun&&interfaceAudio!=null&&clip!=null)interfaceAudio.PlayOneShot(clip);}
        bool UiButton(Rect rect)
        {bool hit=GUI.Button(rect,GUIContent.none,GUIStyle.none);if(hit)MenuSound(uiTap);return hit;}
        void ArcadeCanvas()
        {
            Fill(0,0,540,960,menuBase);
            // Broad, airy colour fields and drifting track dashes keep menus alive.
            Disc(260,-120,530,530,new Color(.25f,.77f,1,.13f));
            Disc(-240,500,600,600,new Color(1,.72f,.16f,.10f));
            for(int i=0;i<9;i++)
            {
                float y=Mathf.Repeat(i*128-Time.unscaledTime*13,1150)-100;
                RoadLine(new Vector2(480,y),new Vector2(530,y-70),3,new Color(.12f,.56f,.79f,.10f));
            }
        }

        void InitPresentation()
        {
            InitMenuSound();
            if(racingBold==null)racingBold=Resources.Load<Font>("Interface/Fonts/Rajdhani-Bold");
            if(racingMedium==null)racingMedium=Resources.Load<Font>("Interface/Fonts/Rajdhani-Medium");
            if(menuHero==null)menuHero=Resources.Load<Texture2D>("Interface/HomeHero");
            if(riderGallery==null)riderGallery=Resources.Load<Texture2D>("Interface/RiderGallery");
            if(routeGallery==null)routeGallery=Resources.Load<Texture2D>("Interface/RouteGallery");
            if(menuGradient==null)
            {
                menuGradient=new Texture2D(1,128,TextureFormat.RGBA32,false);
                menuGradient.wrapMode=TextureWrapMode.Clamp;
                for(int i=0;i<128;i++)menuGradient.SetPixel(0,i,new Color(menuBase.r,menuBase.g,menuBase.b,1-i/127f));
                menuGradient.Apply();
            }
            if(presentedMode!=mode)
            {presentedMode=mode;menuEntered=Time.unscaledTime;if(mode!=ScreenMode.Running)MenuSound(mode==ScreenMode.Results?uiReward:uiSweep);}
        }
        float MenuAge => Time.unscaledTime-menuEntered;
        GUIStyle MenuFont(int size,bool bold=false,TextAnchor align=TextAnchor.MiddleLeft)
        {
            int key=size*100+(bold?10:0)+(int)align;
            if(!menuFonts.TryGetValue(key,out var style))
            {style=Style(Mathf.RoundToInt(size*1.12f),FontStyle.Normal,align);style.font=bold?racingBold:racingMedium;menuFonts[key]=style;}
            return style;
        }
        void Label(string text,float x,float y,float w,float h,int size,Color color,bool bold=false,TextAnchor align=TextAnchor.MiddleLeft)
        {Text(text,x,y,w,h,MenuFont(size,bold,align),color);}
        void Panel(float x,float y,float w,float h,Color color,float radius=12)
        {
            if(cutShape==null)
            {
                cutShape=new Texture2D(64,64,TextureFormat.RGBA32,false);cutShape.wrapMode=TextureWrapMode.Clamp;
                for(int py=0;py<64;py++)for(int px=0;px<64;px++)
                {float edge=Mathf.Min(px+py-9,117-px-py);cutShape.SetPixel(px,py,new Color(1,1,1,Mathf.Clamp01(edge)));}
                cutShape.Apply();
            }
            GUI.color=color;GUI.DrawTexture(new Rect(x,y,w,h),cutShape);GUI.color=Color.white;
        }
        void Fade(float x,float y,float w,float h)
        {GUI.DrawTexture(new Rect(x,y,w,h),menuGradient);}
        bool Action(string text,float x,float y,float w,float h,bool primary=false,bool enabled=true)
        {
            Rect rect=new Rect(x,y,w,h);
            bool hot=GUI.enabled&&enabled&&rect.Contains(Event.current.mousePosition);
            Color color=primary?menuAccent:menuPanel;
            if(hot)color=Color.Lerp(color,menuText,.10f);
            if(!enabled)color=Color.Lerp(color,menuBase,.6f);
            bool pressed=hot&&Input.GetMouseButton(0);
            float drop=pressed?3:0;
            Panel(x,y+5,w,h,primary?new Color(.37f,.51f,.06f):new Color(.04f,.06f,.1f),h*.24f);
            Panel(x,y+drop,w,h,color,h*.24f);
            if(primary&&enabled)
            {
                Fill(x+18,y+5+drop,w-36,2,new Color(1,1,1,.35f));
                float glint=Mathf.Repeat(Time.unscaledTime*.23f,1);
                Disc(x+18+glint*(w-45),y+8+drop,13,3,new Color(1,1,1,.5f));
            }
            Label(text,x+16,y,w-32,h,18,enabled?(primary?menuBase:menuText):menuMuted,true,TextAnchor.MiddleCenter);
            bool old=GUI.enabled;GUI.enabled=old&&enabled;
            bool clicked=UiButton(rect);
            GUI.enabled=old;return clicked;
        }
        void Wallet(float y=27)
        {
            Panel(376,y,136,40,menuPanel,20);
            Disc(387,y+9,22,22,new Color(1,.66f,.08f));
            Disc(390,y+12,16,16,new Color(1,.84f,.30f));
            Label("+",389,y+5,18,28,13,new Color(.58f,.29f,.01f),true,TextAnchor.MiddleCenter);
            Label(wallet.ToString("N0"),414,y,84,40,16,menuGold,true,TextAnchor.MiddleRight);
        }
        void Header(string eyebrow,string title,string subtitle,ScreenMode back)
        {
            ArcadeCanvas();
            if(Action("<",28,28,48,40))mode=back;
            Label(eyebrow,92,28,270,40,13,menuMuted,true);
            Wallet();
            float entrance=12*(1-Mathf.SmoothStep(0,1,MenuAge/.32f));
            Label(title,28,98+entrance,484,52,38,menuText,true);
            Label(subtitle,30,151+entrance,480,48,16,menuMuted);
        }
        void Gallery(bool cities,int index,Rect dest)
        {
            Texture2D art=cities?routeGallery:riderGallery;
            if(art==null)return;
            // Aspect-fill each isolated atlas cell; no adjacent character leaks.
            float cell=art.width/5f,sourceH=art.height,sourceW=cell;
            float ratio=dest.width/dest.height;
            if(sourceW/sourceH>ratio)sourceW=sourceH*ratio;else sourceH=sourceW/ratio;
            float drift=Mathf.Min(8,art.height-sourceH)*(.5f+.5f*Mathf.Sin(Time.unscaledTime*.22f+index));
            Sprite(art,index*cell+(cell-sourceW)*.5f,drift,sourceW,sourceH,dest.x,dest.y,dest.width,dest.height);
        }
        void Navigation(int selected)
        {
            Fill(0,876,540,84,menuBase);
            Fill(28,876,484,1,new Color(.1f,.25f,.35f,.12f));
            string[] names={"RACE","RIDERS + CITIES","SETUP"};
            for(int i=0;i<3;i++)
            {
                float x=28+i*166;
                if(i==selected)Panel(x,881,152,70,menuAccent);
                Color ink=i==selected?menuBase:menuMuted;
                float cx=x+76;
                if(i==0)
                {Fill(cx-13,890,2,25,ink);for(int r=0;r<3;r++)for(int c=0;c<4;c++)if((r+c)%2==0)Fill(cx-10+c*6,890+r*6,6,6,ink);}
                else if(i==1)
                {Fill(cx-18,896,9,18,ink);Fill(cx-5,890,10,24,ink);Fill(cx+9,896,9,18,ink);}
                else
                {for(int n=0;n<3;n++){Fill(cx-17,893+n*8,34,2,ink);Disc(cx-10+n*7,890+n*8,7,7,ink);}}
                Label(names[i],x,918,152,28,13,ink,true,TextAnchor.MiddleCenter);
                if(UiButton(new Rect(x,885,152,66)))
                {if(i==0)mode=ScreenMode.Title;else if(i==1)OpenMarket();else mode=ScreenMode.Settings;}
            }
        }
        void DrawTitle()
        {
            Fill(0,0,540,960,menuBase);
            float drift=Mathf.Sin(Time.unscaledTime*.16f)*7;
            Image(menuHero,-12+drift,-16,564,1000);
            for(int i=0;i<9;i++)
            {float t=Mathf.Repeat(Time.unscaledTime*.07f+i*.131f,1);Disc(30+i*59,550-t*320,2,2,new Color(.55f,.86f,1,.3f*Mathf.Sin(t*Mathf.PI)));}
            Fill(0,0,540,84,new Color(.06f,.08f,.13f,.83f));
            Label("SR / 02",28,27,210,40,23,Color.white,true);Wallet();
            Label("OWN THE STREETS",28,106,484,28,14,menuAccent,true);
            Label("SPEED ROVERS",24,136,492,72,52,Color.white,true);
            Fill(28,215,54,4,menuAccent);
            Fade(0,526,540,434);
            Panel(28,628,484,117,new Color(.07f,.10f,.17f,.91f));
            Fill(28,650,4,69,menuAccent);
            Label("STREET RACE",48,639,426,48,32,Color.white,true);
            Label("60 KM   /   4 RACERS   /   7 LANDSCAPES",48,697,426,28,15,menuMuted,true);
            if(Action("RACE NOW   >>>",28,766,484,72,true))StartRun();
            Navigation(0);
        }

        void DrawMarket()
        {
            Header("RIDER HQ","BUILD YOUR IDENTITY","Your riders. Your cities. Your next race.",ScreenMode.Title);
            CollectionCard(false,selectedRider,221);
            CollectionCard(true,selectedCity,535);
            Navigation(1);
        }

        void CollectionCard(bool cities,int current,float y)
        {
            Gallery(cities,current,new Rect(28,y,484,286));
            Fade(28,y+80,484,206);
            Label(cities?"CITY STYLES":"RIDER COLLECTION",48,y+155,420,26,12,menuGold,true);
            Label(cities?Cities[current]:Riders[current],48,y+182,400,40,26,menuText,true);
            int count=0;for(int i=0;i<5;i++)if(Owns(cities,i))count++;
            Label(count+" / 5 UNLOCKED",48,y+231,300,30,13,menuMuted,true);
            if(Action(">",436,y+221,54,44)){if(cities){marketCity=selectedCity;mode=ScreenMode.Cities;}else{marketRider=selectedRider;mode=ScreenMode.Characters;}}
            if(UiButton(new Rect(28,y,400,280))){if(cities){marketCity=selectedCity;mode=ScreenMode.Cities;}else{marketRider=selectedRider;mode=ScreenMode.Characters;}}
        }
        void DrawSelection(bool cities)
        {
            int current=cities?marketCity:marketRider;
            int selection=current+(cities?5:0);
            if(presentedSelection!=selection){presentedSelection=selection;selectionEntered=Time.unscaledTime;}
            ArcadeCanvas();
            GUI.color=new Color(1,1,1,.55f+.45f*Mathf.SmoothStep(0,1,(Time.unscaledTime-selectionEntered)/.24f));
            if(cities)Gallery(true,current,new Rect(0,78,540,580));
            else Gallery(false,current,new Rect(160,84,380,598));
            GUI.color=Color.white;
            Fade(0,423,540,259);
            Fill(0,0,540,84,new Color(.055f,.075f,.12f,.96f));
            if(Action("<",28,26,48,42))mode=ScreenMode.Market;
            Label(cities?"CITY SELECT":"RIDER SELECT",92,26,260,42,20,Color.white,true);Wallet();
            Panel(28,110,105,34,menuAccent);
            Label((current+1).ToString("00")+" / 05",28,110,105,34,16,menuBase,true,TextAnchor.MiddleCenter);
            bool owned=Owns(cities,current);
            bool equipped=(cities?selectedCity:selectedRider)==current;
            Panel(28,482,132,30,new Color(.07f,.10f,.17f,.92f));
            Label(equipped?"EQUIPPED":owned?"OWNED":"LOCKED",40,482,112,30,13,equipped?menuAccent:Color.white,true);
            Label((cities?Cities:Riders)[current],26,521,488,62,42,Color.white,true);
            Label((cities?cityNotes:riderNotes)[current],28,590,474,30,15,menuMuted,true);
            if(Action("<",28,323,48,58))current=(current+4)%5;
            if(Action(">",464,323,48,58))current=(current+1)%5;
            for(int i=0;i<5;i++)
            {
                float x=28+i*99;
                Gallery(cities,i,new Rect(x,653,88,100));
                if(i==current)
                {Fill(x,651,88,3,menuAccent);Fill(x,753,88,5,menuAccent);Fill(x,651,3,106,menuAccent);Fill(x+85,651,3,106,menuAccent);}
                if(!Owns(cities,i))
                {Panel(x+57,658,25,24,new Color(.03f,.05f,.09f,.9f));Label("+",x+57,658,25,24,16,menuGold,true,TextAnchor.MiddleCenter);}
                if(UiButton(new Rect(x,653,88,103)))current=i;
            }
            if(cities)marketCity=current;else marketRider=current;
            owned=Owns(cities,current);equipped=(cities?selectedCity:selectedRider)==current;
            int cost=UnlockCost(cities,current);
            string action=equipped?"EQUIPPED":owned?"EQUIP "+(cities?"CITY":"RIDER"):"UNLOCK  /  "+cost+" COINS";
            if(Action(action,28,789,484,68,true,!equipped))
            {
                if(owned)Equip(cities,current);
                else {unlockCity=cities;unlockIndex=current;menuDialog=MenuDialog.Unlock;}
            }
            Label(cities?"5 CITY STYLES / 7 LANDSCAPES / ONE FINISH LINE":"YOUR STYLE. YOUR RACE.",28,881,484,48,14,menuMuted,false,TextAnchor.MiddleCenter);
        }
        static string OwnershipKey(bool cities,int index)=>(cities?"sr_city_owned_":"sr_rider_owned_")+index;
        static int UnlockCost(bool cities,int index)=>index*(cities?35:20);
        bool Owns(bool cities,int index)=>index==0 || (verificationRun?reviewOwnership.Contains(OwnershipKey(cities,index)):PlayerPrefs.GetInt(OwnershipKey(cities,index),0)==1);

        void Notice(string text){menuNotice=text;noticeUntil=Time.unscaledTime+3;}
        void Equip(bool cities,int index)
        {
            if(index<0||index>4||!Owns(cities,index))return;
            if(cities)selectedCity=index;else selectedRider=index;
            if(!verificationRun){PlayerPrefs.SetInt(cities?"sr_city":"sr_rider",index);PlayerPrefs.Save();}
            Notice((cities?Cities:Riders)[index]+" equipped");
        }
        bool Purchase(bool cities,int index)
        {
            if(index<0||index>4)return false;
            if(Owns(cities,index)){Equip(cities,index);return true;}
            int cost=UnlockCost(cities,index);
            if(wallet<cost){Notice("Collect "+(cost-wallet)+" more coins to unlock");return false;}
            wallet-=cost;
            if(verificationRun)reviewOwnership.Add(OwnershipKey(cities,index));
            else{PlayerPrefs.SetInt(OwnershipKey(cities,index),1);PlayerPrefs.SetInt("sr_wallet",wallet);}
            Equip(cities,index);Notice("Unlocked! "+(cities?Cities:Riders)[index]+" is ready.");rewardUntil=Time.unscaledTime+2.2f;MenuSound(uiReward);return true;
        }
        void DrawSettings()
        {
            Header("SETUP","RACE SETTINGS","SET YOUR SOUND. SET YOUR PACE.",ScreenMode.Title);
            ToggleRow("Engine & effects","Engine, road and collision audio",soundEnabled,239,()=>{soundEnabled=!soundEnabled;SaveSetting("sr_sound",soundEnabled);});
            ToggleRow("Music","The soundtrack to your ride",musicEnabled,343,()=>{musicEnabled=!musicEnabled;SaveSetting("sr_music",musicEnabled);});
            ToggleRow("Relaxed pace","A gentler ride through traffic",easyMode,447,()=>{easyMode=!easyMode;SaveSetting("sr_easy",easyMode);});
            Panel(28,585,484,235,menuPanel);
            Label("ON THE ROAD",48,607,444,28,12,menuGold,true);
            Label("STEER",48,651,120,30,18,menuText,true);
            Label("Hold + drag freely. Swipe to step.",180,651,310,30,15,menuMuted);
            Fill(48,696,444,1,new Color(.1f,.25f,.35f,.10f));
            Label("KEYBOARD",48,712,130,30,15,menuText,true);
            Label("A / D or arrows  /  Down to brake",180,712,310,42,15,menuMuted);
            Label("Collisions slow you down. Stay in the race.",48,770,444,28,14,menuMuted);
            Navigation(2);
        }
        void SaveSetting(string key,bool value){if(!verificationRun){PlayerPrefs.SetInt(key,value?1:0);PlayerPrefs.Save();}}
        void ToggleRow(string name,string help,bool active,float y,System.Action change)
        {
            Panel(28,y,484,88,menuPanel);
            Label(name,48,y+13,330,30,20,menuText,true);
            Label(help,48,y+46,337,25,14,menuMuted);
            Panel(420,y+28,64,32,active?menuAccent:new Color(.19f,.23f,.29f),16);
            Disc(active?453:424,y+32,24,24,Color.white);
            if(UiButton(new Rect(28,y,484,88)))change();
        }
        void DrawPause()
        {
            Fill(0,0,540,960,new Color(.08f,.11f,.18f,.87f));
            Label("PIT STOP",28,208,484,28,12,menuGold,true,TextAnchor.MiddleCenter);
            Label("PAUSED",28,254,484,70,44,menuText,true,TextAnchor.MiddleCenter);
            Panel(28,359,484,116,menuPanel);
            Label(RaceClock(animationTime),48,377,210,40,28,menuText,true);
            Label("ELAPSED",48,422,210,24,11,menuMuted,true);
            Label((distance/1000f).ToString("0.0")+" / 60 KM",280,377,212,40,25,menuText,true,TextAnchor.MiddleRight);
            Label(RoadRoute.Name(distance),270,422,222,24,11,menuMuted,true,TextAnchor.MiddleRight);
            if(Action("BACK TO THE RACE   >",28,521,484,68,true))mode=ScreenMode.Running;
            if(Action("LEAVE RACE",28,607,484,60))menuDialog=MenuDialog.LeaveRace;
            Label("READY WHEN YOU ARE.",28,715,484,30,16,menuMuted,false,TextAnchor.MiddleCenter);
        }
        void DrawResults()
        {
            ArcadeCanvas();
            Gallery(false,selectedRider,new Rect(210,0,330,620));
            Fill(0,0,540,620,new Color(.08f,.11f,.18f,.15f));
            Fade(0,250,540,380);
            bool won=finishPlace==1;
            if(won)for(int i=0;i<26;i++)
            {float t=Mathf.Repeat(Time.unscaledTime*.14f+i*.137f,1);Fill(Mathf.Repeat(i*93,530),t*620,3,9,i%2==0?menuGold:menuAccent);}
            Label("RACE COMPLETE",28,45,484,28,13,menuGold,true);
            Label(won?"VICTORY": "RACE\nCOMPLETE",28,108,484,137,49,menuText,true);
            Label(finishPlace>0?finishPlace.ToString("00"):"-",27,275,240,130,108,menuText,true);
            Label("/ 04  FINISH POSITION",33,411,330,26,13,menuMuted,true);
            Panel(28,485,484,134,menuPanel);
            Label(RaceClock(playerFinishTime),48,505,220,49,34,menuText,true);
            int revealed=Mathf.RoundToInt(runCoins*Mathf.SmoothStep(0,1,MenuAge/1.1f));
            Label("+"+revealed,288,505,202,49,34,menuGold,true,TextAnchor.MiddleRight);
            Label("RACE TIME",48,570,220,25,12,menuMuted,true);
            Label("COINS BANKED",288,570,202,25,12,menuMuted,true,TextAnchor.MiddleRight);
            Label(won?"First across the line. Enjoy the view.":"Every finish is another shot at first.",28,638,484,40,17,menuMuted,false,TextAnchor.MiddleCenter);
            if(Action("RACE AGAIN   >",28,709,484,68,true))StartRun();
            if(Action("COLLECTION",28,795,236,58))OpenMarket();
            if(Action("HOME",276,795,236,58))mode=ScreenMode.Title;
            Label("WALLET  /  "+wallet+" COINS",28,899,484,28,13,menuGold,true,TextAnchor.MiddleCenter);
        }
        void DrawHud()
        {
            Color muted=new Color(.65f,.73f,.80f);
            Panel(18,18,504,148,new Color(.025f,.035f,.055f,.94f));
            Fill(30,28,3,38,menuAccent);
            Label("COINS",44,27,92,17,10,muted,true);
            Label(runCoins.ToString(),44,44,92,31,23,menuGold,true);
            Label("RACE TIME",158,27,132,17,10,muted,true);
            Label(RaceClock(animationTime),158,44,132,31,23,Color.white,true);
            Label("DISTANCE",310,27,120,17,10,muted,true);
            Label((distance/1000f).ToString("0.0")+" km",310,44,120,31,23,Color.white,true);
            if(Action("II",460,28,48,48))mode=ScreenMode.Paused;
            Fill(32,84,476,1,new Color(1,1,1,.12f));
            Label("SPEED",32,93,100,15,9,muted,true);
            Label(Mathf.RoundToInt(speed).ToString(),30,105,116,45,36,nitroActive?new Color(.2f,.86f,1):Color.white,true);
            Label("KM/H",142,113,65,17,11,muted,true);
            Label("GEAR "+gear,142,133,65,17,11,Color.white,true);
            Label("FUEL  "+Mathf.CeilToInt(fuel*100)+"%",222,98,150,22,11,fuel<.2f?new Color(1,.48f,.18f):muted,true);
            Fill(222,125,150,7,new Color(1,1,1,.12f));
            Fill(222,125,150*fuel,7,fuel<.2f?new Color(1,.48f,.18f):menuAccent);
            Label(RoadRoute.Name(distance),222,139,166,17,9,muted,true);
            int place=1,racers=1;
            foreach(Thing racer in things)if(racer.kind==ThingKind.Rival&&!racer.used){racers++;if(racer.distance>distance)place++;}
            Label("POSITION",403,98,105,17,9,muted,true,TextAnchor.MiddleRight);
            Label(place+" / "+racers,398,119,110,34,25,Color.white,true,TextAnchor.MiddleRight);
            Fill(18,171,504,3,new Color(1,1,1,.15f));
            Fill(18,171,504*Mathf.Clamp01(distance/RoadRoute.Length),3,menuAccent);
            DrawAbilityHud();
            if(Time.time<bannerUntil&&distance>70)
            {Panel(90,188,360,36,new Color(.025f,.035f,.055f,.84f));Label(banner,100,188,340,36,13,menuGold,true,TextAnchor.MiddleCenter);}
        }
        void DrawPresentationOverlay()
        {
            if(Time.unscaledTime<rewardUntil && mode!=ScreenMode.Running)
            {
                float progress=1-(rewardUntil-Time.unscaledTime)/2.2f;
                for(int i=0;i<28;i++)
                {
                    float angle=i*2.4f;
                    float x=270+Mathf.Cos(angle)*(40+progress*310);
                    float y=620-Mathf.Sin(angle)*progress*230+progress*progress*270;
                    Disc(x,y,7,7,i%2==0?new Color(1,.67f,.08f):menuAccent);
                }
            }
            if(menuDialog!=MenuDialog.None)
            {
                Fill(0,0,540,960,new Color(.008f,.015f,.025f,.9f));
                bool unlock=menuDialog==MenuDialog.Unlock;
                Panel(28,225,484,510,menuPanel,18);
                Label(unlock?"NEW UNLOCK":"LEAVING ALREADY?",52,250,436,30,12,menuGold,true);
                Label(unlock?"READY TO RIDE?":"LEAVE RACE?",52,291,436,60,34,menuText,true);
                if(unlock)
                {
                    Gallery(unlockCity,unlockIndex,new Rect(52,373,105,160));
                    Label((unlockCity?Cities:Riders)[unlockIndex],177,370,305,44,23,menuText,true);
                    int cost=UnlockCost(unlockCity,unlockIndex);
                    Label(cost+" coins",177,423,305,36,23,menuGold,true);
                    Label("Your balance: "+wallet,177,467,305,32,15,menuMuted);
                    bool enough=wallet>=cost;
                    Label(enough?"Unlock once. Yours for every race.":"Collect "+(cost-wallet)+" more coins on the road.",52,548,436,32,15,menuMuted);
                    if(Action(enough?"UNLOCK & EQUIP":"NOT ENOUGH COINS",52,599,436,58,true,enough))
                    {if(Purchase(unlockCity,unlockIndex))menuDialog=MenuDialog.None;}
                }
                else
                {
                    Label("Your race position and unbanked coins will be lost. You can start a fresh race from home.",52,375,436,121,21,menuMuted);
                    if(Action("LEAVE RACE",52,599,436,58,true)){menuDialog=MenuDialog.None;mode=ScreenMode.Title;}
                }
                if(Action(unlock?"NOT NOW":"KEEP RACING",52,670,436,43))menuDialog=MenuDialog.None;
            }
            else if(mode!=ScreenMode.Running && mode!=ScreenMode.Paused && Time.unscaledTime<noticeUntil&&!string.IsNullOrEmpty(menuNotice))
            {Panel(28,903,484,40,menuPanel,20);Label(menuNotice,42,903,456,40,14,menuGold,true,TextAnchor.MiddleCenter);}
            if(mode!=ScreenMode.Running && MenuAge<.20f)
                Fill(0,0,540,960,new Color(.10f,.16f,.24f,(1-MenuAge/.20f)*.65f));
        }
        IEnumerator CapturePresentation(string directory)
        {
            verificationRun=true;captureHold=true;Directory.CreateDirectory(directory);
            wallet=95;selectedCity=selectedRider=marketCity=marketRider=0;
            var states=new[]{ScreenMode.Title,ScreenMode.Market,ScreenMode.Characters,ScreenMode.Cities,ScreenMode.Settings};
            foreach(var state in states){mode=state;yield return new WaitForSecondsRealtime(1.3f);ScreenCapture.CaptureScreenshot(Path.Combine(directory,state+".png"));yield return new WaitForSecondsRealtime(.25f);yield return new WaitForSecondsRealtime(.2f);}
            mode=ScreenMode.Characters;marketRider=2;yield return new WaitForSecondsRealtime(.5f);ScreenCapture.CaptureScreenshot(Path.Combine(directory,"RiderLocked.png"));yield return new WaitForSecondsRealtime(.25f);
            unlockCity=false;unlockIndex=2;menuDialog=MenuDialog.Unlock;yield return new WaitForSecondsRealtime(.5f);ScreenCapture.CaptureScreenshot(Path.Combine(directory,"UnlockConfirm.png"));yield return new WaitForSecondsRealtime(.25f);
            Purchase(false,2);menuDialog=MenuDialog.None;yield return new WaitForSecondsRealtime(.5f);ScreenCapture.CaptureScreenshot(Path.Combine(directory,"Unlocked.png"));yield return new WaitForSecondsRealtime(.25f);
            unlockIndex=4;menuDialog=MenuDialog.Unlock;yield return new WaitForSecondsRealtime(.5f);ScreenCapture.CaptureScreenshot(Path.Combine(directory,"InsufficientCoins.png"));yield return new WaitForSecondsRealtime(.25f);
            menuDialog=MenuDialog.None;StartRun();distance=2400;speed=300;animationTime=38;gear=5;runCoins=16;
            yield return new WaitForSecondsRealtime(.5f);ScreenCapture.CaptureScreenshot(Path.Combine(directory,"Hud.png"));yield return new WaitForSecondsRealtime(.25f);
            mode=ScreenMode.Paused;yield return new WaitForSecondsRealtime(.5f);ScreenCapture.CaptureScreenshot(Path.Combine(directory,"Pause.png"));yield return new WaitForSecondsRealtime(.25f);
            menuDialog=MenuDialog.LeaveRace;yield return new WaitForSecondsRealtime(.5f);ScreenCapture.CaptureScreenshot(Path.Combine(directory,"LeaveConfirmation.png"));yield return new WaitForSecondsRealtime(.25f);
            menuDialog=MenuDialog.None;mode=ScreenMode.Results;finishPlace=1;playerFinishTime=571;runCoins=42;wallet=97;
            yield return new WaitForSecondsRealtime(1.4f);ScreenCapture.CaptureScreenshot(Path.Combine(directory,"Victory.png"));yield return new WaitForSecondsRealtime(.25f);
            finishPlace=3;yield return new WaitForSecondsRealtime(.5f);ScreenCapture.CaptureScreenshot(Path.Combine(directory,"Results.png"));yield return new WaitForSecondsRealtime(.25f);
            yield return new WaitForSecondsRealtime(.4f);Debug.Log("PRESENTATION_REVIEW_COMPLETE");Application.Quit();
        }
    }
}
