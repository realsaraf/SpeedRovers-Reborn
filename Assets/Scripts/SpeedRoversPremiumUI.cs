using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SpeedRovers.Reborn
{
    public sealed partial class SpeedRoversGame
    {
        Texture2D menuHero, riderGallery, routeGallery, menuGradient;
        readonly Dictionary<int, GUIStyle> menuFonts = new Dictionary<int, GUIStyle>();
        readonly HashSet<string> reviewOwnership = new HashSet<string>();
        readonly Color menuBase = new Color(.94f,.97f,.99f);
        readonly Color menuText = new Color(.08f,.17f,.23f);
        readonly Color menuPanel = new Color(1,1,1,.97f);
        readonly Color menuMuted = new Color(.34f,.43f,.50f);
        readonly Color menuAccent = new Color(.02f,.47f,.59f);
        readonly Color menuGold = new Color(.65f,.39f,.07f);
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
            {style=Style(size,bold?(size>=28?FontStyle.BoldAndItalic:FontStyle.Bold):FontStyle.Normal,align);menuFonts[key]=style;}
            return style;
        }
        void Label(string text,float x,float y,float w,float h,int size,Color color,bool bold=false,TextAnchor align=TextAnchor.MiddleLeft)
        {Text(text,x,y,w,h,MenuFont(size,bold,align),color);}
        void Panel(float x,float y,float w,float h,Color color,float radius=12)
        {GUI.DrawTexture(new Rect(x,y,w,h),pixel,ScaleMode.StretchToFill,true,0,color,0,radius);}
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
            Panel(x,y+5,w,h,primary?new Color(.02f,.29f,.42f):new Color(.68f,.78f,.84f),h*.24f);
            Panel(x,y+drop,w,h,color,h*.24f);
            if(primary&&enabled)
            {
                Fill(x+18,y+5+drop,w-36,2,new Color(1,1,1,.35f));
                float glint=Mathf.Repeat(Time.unscaledTime*.23f,1);
                Disc(x+18+glint*(w-45),y+8+drop,13,3,new Color(1,1,1,.5f));
            }
            Label(text,x+16,y,w-32,h,18,enabled?(primary?Color.white:menuText):menuMuted,true,TextAnchor.MiddleCenter);
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
            string[] names={"RACE","COLLECTION","SETTINGS"};
            for(int i=0;i<3;i++)
            {
                float x=28+i*166;
                if(i==selected)Fill(x+40,876,80,3,menuAccent);
                Label(names[i],x,887,152,52,13,i==selected?menuText:menuMuted,true,TextAnchor.MiddleCenter);
            if(UiButton(new Rect(x,885,152,60)))
                {if(i==0)mode=ScreenMode.Title;else if(i==1)OpenMarket();else mode=ScreenMode.Settings;}
            }
        }
        void DrawTitle()
        {
            Fill(0,0,540,960,menuBase);
            float drift=Mathf.Sin(Time.unscaledTime*.16f)*7;
            Image(menuHero,-45+drift,18,630,630);
            Fade(0,320,540,330);
            Fill(0,0,540,85,new Color(1,1,1,.80f));
            Label("SR / 02",28,27,200,40,20,menuText,true);Wallet();
            Panel(28,388,246,34,new Color(1,.80f,.24f),17);
            Label("LET'S RIDE!",42,388,220,34,16,menuText,true);
            Label("SPEED\nROVERS 2",26,421,488,137,57,menuText,true);
            Label("Chase the pack. Take the lead!",30,568,480,32,18,menuMuted);
            // Subtle moving accent reinforces motion without blocking the artwork.
            float pulse=.5f+.5f*Mathf.Sin(Time.unscaledTime*1.2f);
            Fill(30,610,44+26*pulse,3,menuAccent);
            Label("60 KM",30,629,150,34,23,menuText,true);
            Label("7 LANDSCAPES",205,629,168,34,15,menuMuted,true);
            Label("4 RACERS",384,629,126,34,15,menuMuted,true,TextAnchor.MiddleRight);
            if(Action("START RACE   >",28,683,484,68,true))StartRun();
            Label("RIDER",44,774,250,20,10,menuMuted,true);
            if(Action(Riders[selectedRider],28,797,282,41))
            {marketRider=selectedRider;mode=ScreenMode.Characters;}
            Label("CITY STYLE",338,774,158,20,10,menuMuted,true);
            if(Action(Cities[selectedCity],322,797,190,41))
            {marketCity=selectedCity;mode=ScreenMode.Cities;}
            Navigation(0);
        }
        void DrawMarket()
        {
            Header("YOUR COLLECTION","Make it yours.","Earn coins on the road. Unlock your next favourite.",ScreenMode.Title);
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
            Header("COLLECTION / "+(cities?"CITIES":"RIDERS"),cities?"Choose your city.":"Meet your rider.",cities?"A new city style for your next race.":"Five personalities. One place at the front.",ScreenMode.Market);
            Gallery(cities,current,new Rect(28,214,484,426));
            Fade(28,438,484,202);
            Label((current+1).ToString("00")+" / 05",48,231,160,28,12,menuText,true);
            Panel(368,233,124,28,new Color(1,1,1,.93f),14);
            bool owned=Owns(cities,current);
            bool equipped=(cities?selectedCity:selectedRider)==current;
            Label(equipped?"EQUIPPED":owned?"OWNED":"LOCKED",368,233,124,28,11,equipped?menuGold:menuText,true,TextAnchor.MiddleCenter);
            Label((cities?cityNotes:riderNotes)[current],48,540,420,25,12,menuGold,true);
            Label((cities?Cities:Riders)[current],48,572,424,48,30,menuText,true);
            if(Action("<",39,389,44,52))current=(current+4)%5;
            if(Action(">",457,389,44,52))current=(current+1)%5;
            for(int i=0;i<5;i++)
            {
                float x=28+i*99;
                Gallery(cities,i,new Rect(x,655,88,83));
                if(i==current)
                {Fill(x,736,88,5,menuAccent);Disc(x+33,742,22,5,new Color(.02f,.47f,.59f,.5f+.3f*Mathf.Sin(Time.unscaledTime*4)));}
                if(!Owns(cities,i)){Panel(x+63,660,20,20,new Color(.01f,.02f,.04f,.8f),10);Label("+",x+63,659,20,20,13,menuGold,true,TextAnchor.MiddleCenter);}
                if(UiButton(new Rect(x,655,88,83)))current=i;
            }
            if(cities)marketCity=current;else marketRider=current;
            owned=Owns(cities,current);equipped=(cities?selectedCity:selectedRider)==current;
            int cost=UnlockCost(cities,current);
            string action=equipped?"EQUIPPED":owned?"EQUIP "+(cities?"CITY":"RIDER"):"UNLOCK  /  "+cost+" COINS";
            if(Action(action,28,766,484,64,true,!equipped))
            {
                if(owned)Equip(cities,current);
                else {unlockCity=cities;unlockIndex=current;menuDialog=MenuDialog.Unlock;}
            }
            Label(cities?"Choose your city style. Race through all seven landscapes.":"A new look for the same relentless racer.",34,845,472,62,14,menuMuted,false,TextAnchor.MiddleCenter);
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
            Header("PREFERENCES","Find your rhythm.","Your sound. Your pace.",ScreenMode.Title);
            ToggleRow("Engine & effects","Engine, road and collision audio",soundEnabled,239,()=>{soundEnabled=!soundEnabled;SaveSetting("sr_sound",soundEnabled);});
            ToggleRow("Music","The soundtrack to your ride",musicEnabled,343,()=>{musicEnabled=!musicEnabled;SaveSetting("sr_music",musicEnabled);});
            ToggleRow("Relaxed pace","A gentler ride through traffic",easyMode,447,()=>{easyMode=!easyMode;SaveSetting("sr_easy",easyMode);});
            Panel(28,585,484,235,menuPanel);
            Label("ON THE ROAD",48,607,444,28,12,menuGold,true);
            Label("SWIPE",48,651,120,30,18,menuText,true);
            Label("Left or right to change lanes",180,651,310,30,16,menuMuted);
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
            Fill(0,0,540,960,new Color(.94f,.97f,.99f,.93f));
            Label("TAKE A BREATHER",28,208,484,28,12,menuGold,true,TextAnchor.MiddleCenter);
            Label("Race paused.",28,254,484,70,44,menuText,true,TextAnchor.MiddleCenter);
            Panel(28,359,484,116,menuPanel);
            Label(RaceClock(animationTime),48,377,210,40,28,menuText,true);
            Label("ELAPSED",48,422,210,24,11,menuMuted,true);
            Label((distance/1000f).ToString("0.0")+" / 60 KM",280,377,212,40,25,menuText,true,TextAnchor.MiddleRight);
            Label(RoadRoute.Name(distance),270,422,222,24,11,menuMuted,true,TextAnchor.MiddleRight);
            if(Action("BACK TO THE RACE   >",28,521,484,68,true))mode=ScreenMode.Running;
            if(Action("LEAVE RACE",28,607,484,60))menuDialog=MenuDialog.LeaveRace;
            Label("Your rivals can wait.",28,715,484,30,16,menuMuted,false,TextAnchor.MiddleCenter);
        }
        void DrawResults()
        {
            ArcadeCanvas();
            Gallery(false,selectedRider,new Rect(210,0,330,620));
            Fill(0,0,540,620,new Color(.94f,.97f,.99f,.08f));
            Fade(0,250,540,380);
            bool won=finishPlace==1;
            if(won)for(int i=0;i<26;i++)
            {float t=Mathf.Repeat(Time.unscaledTime*.14f+i*.137f,1);Fill(Mathf.Repeat(i*93,530),t*620,3,9,i%2==0?menuGold:menuAccent);}
            Label("RACE COMPLETE",28,45,484,28,13,menuGold,true);
            Label(won?"YOU\nOWNED IT.":"WHAT\nA RIDE.",28,108,484,137,49,menuText,true);
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
            Panel(18,18,430,66,new Color(.025f,.035f,.055f,.82f));
            Label("COINS",32,25,104,17,10,new Color(.74f,.81f,.86f),true);
            Label(runCoins.ToString(),32,43,104,31,23,new Color(1,.78f,.37f),true);
            Label("RACE TIME",157,25,132,17,10,new Color(.74f,.81f,.86f),true);
            Label(RaceClock(animationTime),157,43,132,31,23,Color.white,true);
            Label("DISTANCE",310,25,120,17,10,new Color(.74f,.81f,.86f),true);
            Label((distance/1000f).ToString("0.0")+" km",310,43,120,31,23,Color.white,true);
            if(Action("II",460,18,62,66))mode=ScreenMode.Paused;
            Fill(18,91,504,3,new Color(1,1,1,.15f));Fill(18,91,504*Mathf.Clamp01(distance/RoadRoute.Length),3,menuAccent);
            Panel(18,851,156,91,new Color(.025f,.035f,.055f,.86f));
            Label(Mathf.RoundToInt(speed).ToString(),32,858,116,46,35,Color.white,true);
            Label("KM/H   /   GEAR "+gear,32,908,136,22,11,new Color(.74f,.81f,.86f),true);
            int place=1,racers=1;
            foreach(Thing racer in things)if(racer.kind==ThingKind.Rival&&!racer.used){racers++;if(racer.distance>distance)place++;}
            Label(place+" / "+racers,319,858,193,42,28,Color.white,true,TextAnchor.MiddleRight);
            Label(RoadRoute.Name(distance),252,907,260,24,12,Color.white,true,TextAnchor.MiddleRight);
            if(Time.time<bannerUntil&&distance>70)
            {Panel(90,113,360,43,new Color(.025f,.035f,.055f,.80f));Label(banner,100,113,340,43,14,new Color(1,.78f,.37f),true,TextAnchor.MiddleCenter);}
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
                Label(unlock?"EXPAND YOUR COLLECTION":"LEAVING ALREADY?",52,250,436,30,12,menuGold,true);
                Label(unlock?"Make it yours.":"Leave this race?",52,291,436,60,34,menuText,true);
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
                Fill(0,0,540,960,new Color(.94f,.97f,.99f,(1-MenuAge/.20f)*.65f));
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
