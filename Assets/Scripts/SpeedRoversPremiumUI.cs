using UnityEngine;

namespace SpeedRovers.Reborn
{
    public sealed partial class SpeedRoversGame
    {
        Texture2D premiumHome;
        GUIStyle premiumTitleStyle;
        readonly Color menuInk=new Color(.025f,.045f,.08f,.92f);
        readonly Color menuGold=new Color(1,.68f,.25f);
        readonly Color menuMuted=new Color(.65f,.75f,.84f);

        void PremiumBackdrop(string heading,string subtitle)
        {
            if(premiumHome==null)premiumHome=Resources.Load<Texture2D>("Painted/PremiumHome");
            float t=Time.unscaledTime;
            Image(premiumHome,-16+Mathf.Sin(t*.10f)*9,-16,572,992);
            Fill(0,0,540,960,new Color(.015f,.035f,.065f,.25f));
            // Light trails move independently of the painted backdrop.
            for(int i=0;i<12;i++)
            {
                float f=Mathf.Repeat(t*.17f+i*.083f,1);
                float side=i%2==0?-1:1;
                Vector2 a=new Vector2(270+side*(40+f*f*420),530+f*f*430);
                RoadLine(a,a+new Vector2(side*18*f,35*f),1+f*2,new Color(i%2==0?.1f:1,.7f,.8f,.12f*f));
            }
            Fill(0,0,540,220,new Color(.015f,.035f,.065f,.5f));
            if(premiumTitleStyle==null)premiumTitleStyle=Style(42,FontStyle.Bold,TextAnchor.MiddleCenter);
            Text(heading,30,62,480,90,premiumTitleStyle,Color.white);
            Text(subtitle,35,152,470,36,smallStyle,menuGold);
        }

        bool PremiumButton(string label,float x,float y,float w,float h,bool primary=false)
        {
            Color color=primary?menuGold:menuInk;
            Fill(x,y,w,h,color);
            Fill(x,y,w,1,new Color(1,1,1,primary?.5f:.16f));
            Text(label,x+8,y+4,w-16,h-8,buttonStyle,primary?new Color(.025f,.045f,.075f):Color.white);
            return GUI.Button(new Rect(x,y,w,h),GUIContent.none,GUIStyle.none);
        }

        void DrawTitle()
        {
            PremiumBackdrop("SPEED ROVERS 2","RIDE FAST. RACE TO WIN.");
            float savedTime=animationTime,savedSpeed=speed,savedTurns=wheelTurns;
            animationTime=Time.unscaledTime;speed=75;wheelTurns=animationTime*10;
            DrawRushBike(270,655,275,0,Mathf.Sin(animationTime*.65f)*.22f,true);
            animationTime=savedTime;speed=savedSpeed;wheelTurns=savedTurns;
            Fill(34,686,472,54,menuInk);
            Text("60 KM  /  7 LANDSCAPES  /  4 RACERS",40,696,460,32,smallStyle,menuMuted);
            if(PremiumButton("START RACE",34,756,472,76,true))StartRun();
            if(PremiumButton("RIDERS & ROUTES",34,850,290,62))OpenMarket();
            if(PremiumButton("SETTINGS",340,850,166,62))mode=ScreenMode.Settings;
        }

        void DrawMarket()
        {
            PremiumBackdrop("YOUR RIDE","MAKE THE RACE YOUR OWN");
            if(PremiumButton("BACK",34,24,94,38))mode=ScreenMode.Title;
            Fill(34,260,472,220,menuInk);
            Text("RIDERS",54,284,432,54,headingStyle,Color.white);
            Text(Riders[selectedRider],54,350,432,32,bodyStyle,menuMuted);
            if(PremiumButton("CHOOSE RIDER",64,402,412,56,true))mode=ScreenMode.Characters;
            Fill(34,510,472,220,menuInk);
            Text("ROUTES",54,534,432,54,headingStyle,Color.white);
            Text(Cities[selectedCity],54,600,432,32,bodyStyle,menuMuted);
            if(PremiumButton("CHOOSE ROUTE",64,652,412,56,true))mode=ScreenMode.Cities;
            Text(wallet+" COINS",34,814,472,42,headingStyle,menuGold);
        }

        void DrawSelection(bool cities)
        {
            PremiumBackdrop(cities?"ROUTES":"RIDERS",cities?"SEVEN LANDSCAPES. ONE FINISH LINE.":"PICK YOUR RACING COLOURS");
            if(PremiumButton("BACK",34,24,94,38))mode=ScreenMode.Market;
            int current=cities?marketCity:marketRider;
            Texture2D card=cities?cityCardArt[current]:riderCardArt[current];
            Fill(70,250,400,400,menuInk);
            float h=Mathf.Min(330,330f*card.height/card.width),w=h*card.width/card.height;
            Image(card,270-w*.5f,285,w,h);
            Text(cities?Cities[current]:Riders[current],65,661,410,44,headingStyle,Color.white);
            if(PremiumButton("<",34,470,62,62))current=(current+4)%5;
            if(PremiumButton(">",444,470,62,62))current=(current+1)%5;
            if(cities)marketCity=current;else marketRider=current;
            int selected=cities?selectedCity:selectedRider;
            string key=cities?"sr_city_owned_":"sr_rider_owned_";
            bool owned=current==0 || PlayerPrefs.GetInt(key+current,0)==1;
            int cost=current*(cities?35:20);
            string action=owned?(selected==current?"SELECTED":"SELECT"):"UNLOCK — "+cost+" COINS";
            if(PremiumButton(action,70,744,400,72,true) && (owned || wallet>=cost))
            {
                if(!owned){wallet-=cost;PlayerPrefs.SetInt(key+current,1);PlayerPrefs.SetInt("sr_wallet",wallet);}
                if(cities){selectedCity=current;PlayerPrefs.SetInt("sr_city",current);}
                else{selectedRider=current;PlayerPrefs.SetInt("sr_rider",current);}
                PlayerPrefs.Save();
            }
            Text(wallet+" COINS AVAILABLE",70,840,400,36,smallStyle,menuMuted);
        }

        void DrawSettings()
        {
            PremiumBackdrop("SETTINGS","TUNE YOUR RIDE");
            if(PremiumButton("BACK",34,24,94,38))mode=ScreenMode.Title;
            if(PremiumButton("ENGINE & EFFECTS   "+(soundEnabled?"ON":"OFF"),34,310,472,84))
            {soundEnabled=!soundEnabled;PlayerPrefs.SetInt("sr_sound",soundEnabled?1:0);PlayerPrefs.Save();}
            if(PremiumButton("MUSIC   "+(musicEnabled?"ON":"OFF"),34,426,472,84))
            {musicEnabled=!musicEnabled;PlayerPrefs.SetInt("sr_music",musicEnabled?1:0);PlayerPrefs.Save();}
            if(PremiumButton("PACE   "+(easyMode?"RELAXED":"RACING"),34,542,472,84))
            {easyMode=!easyMode;PlayerPrefs.SetInt("sr_easy",easyMode?1:0);PlayerPrefs.Save();}
            Text("Swipe left or right to change lanes.\nOn desktop: arrow keys or A / D.\nCollisions slow you down. Keep racing.",44,722,452,120,bodyStyle,menuMuted);
        }

        void DrawPause()
        {
            Fill(0,0,540,960,new Color(.015f,.03f,.05f,.86f));
            Text("RACE PAUSED",30,276,480,80,titleStyle,Color.white);
            if(PremiumButton("RESUME RACE",54,420,432,76,true))mode=ScreenMode.Running;
            if(PremiumButton("RETURN HOME",54,522,432,64))mode=ScreenMode.Title;
        }

        void DrawResults()
        {
            PremiumBackdrop(result,finishPlace==1?"FIRST ACROSS THE LINE":"THE FINISH IS ONLY THE BEGINNING");
            Fill(34,286,472,318,menuInk);
            Text("PLACE "+finishPlace+" / 4",54,315,432,66,titleStyle,menuGold);
            Text("RACE TIME   "+RaceClock(playerFinishTime),54,414,432,42,bodyStyle,Color.white);
            Text("COINS EARNED   "+runCoins,54,480,432,42,bodyStyle,menuMuted);
            if(PremiumButton("RACE AGAIN",34,708,472,76,true))StartRun();
            if(PremiumButton("HOME",34,812,472,64))mode=ScreenMode.Title;
        }
    }
}
