using System;
using System.Collections;
using UnityEngine;

public partial class TrafficRidePrototype
{
    void Styles()
    {
        if(titleStyle!=null)return;
        titleStyle=new GUIStyle(GUI.skin.label){font=gameFont,fontSize=76,fontStyle=FontStyle.Bold};titleStyle.normal.textColor=Color.white;
        subStyle=new GUIStyle(titleStyle){fontSize=32};bodyStyle=new GUIStyle(titleStyle){fontSize=22};tinyStyle=new GUIStyle(titleStyle){fontSize=16};
        buttonStyle=new GUIStyle(bodyStyle){alignment=TextAnchor.MiddleCenter};
    }
    void Fill(Rect r,Color color){var old=GUI.color;GUI.color=color;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
    void Text(string text,float x,float y,float w,float h,GUIStyle style,Color? color=null){var old=GUI.color;GUI.color=color??Color.white;GUI.Label(new Rect(x,y,w,h),text,style);GUI.color=old;}
    bool Button(string text,float x,float y,float w,float h,bool primary=false,bool active=true)
    {
        Rect r=new Rect(x,y,w,h);bool hover=r.Contains(Event.current.mousePosition);
        Fill(r,primary?Color.Lerp(accent,Color.white,hover?.15f:0):new Color(.07f,.11f,.16f,.96f));
        Fill(new Rect(x,y,primary?5:2,h),active?accent:muted);
        Text(text,x+10,y,w-20,h,buttonStyle,active?(primary?ink:Color.white):muted);
        bool pressed=active&&GUI.Button(r,GUIContent.none,GUIStyle.none);if(pressed)Feedback(clickSound,false);return pressed;
    }
    void Meter(string name,float value,float x,float y,float w,Color c)
    {
        Text(name,x,y,w,22,tinyStyle);Fill(new Rect(x,y+26,w,5),new Color(.2f,.26f,.3f));Fill(new Rect(x,y+26,w*Mathf.Clamp01(value),5),c);
    }
    void OnGUI()
    {
        if(view==null)return;Styles();
        float scale=Mathf.Min(Screen.safeArea.width/1280f,Screen.safeArea.height/720f);
        if(scale<=0)return;
        var safe=Screen.safeArea;float left=safe.x+(safe.width-1280*scale)*.5f;
        float top=Screen.height-safe.yMax+(safe.height-720*scale)*.5f;
        GUI.matrix=Matrix4x4.TRS(new Vector3(left,top,0),Quaternion.identity,Vector3.one*scale);
        bool racing=page==Page.Riding||page==Page.Pause||page==Page.Results;
        if(racing)DrawRideHUD();else DrawMenu();
        if(page==Page.Pause)DrawPause();if(page==Page.Results)DrawResults();
        if(Time.unscaledTime<noticeUntil){float alpha=Mathf.Clamp01((noticeUntil-Time.unscaledTime)*2);GUI.color=new Color(1,1,1,alpha);
            Fill(new Rect(390,125,500,42),ink);Text(notice,405,130,470,35,buttonStyle,accent);GUI.color=Color.white;}
        GUI.matrix=Matrix4x4.identity;
    }
    void DrawRideHUD()
    {
        DrawSpeedAir();
        // Cockpit stays at rider eye level during nitro. Only the explicit jump moves the camera.
        if(cockpitArt!=null){
            var old=GUI.matrix;GUIUtility.RotateAroundPivot(lean*.25f,new Vector2(640,720));
            GUI.matrix=GUI.matrix*Matrix4x4.Translate(new Vector3(0,Mathf.Sin(Time.time*22)*speed/300,0));
            GUI.DrawTexture(new Rect(-12,150,1304,734),cockpitArt,ScaleMode.StretchToFill,true);
            // Instrument text shares the artwork transform, including vibration and lean.
            var lcd=new Color(.48f,.79f,.73f,.87f);
            Text(Mathf.RoundToInt(speed).ToString(),593,500,75,30,new GUIStyle(bodyStyle){fontSize=25,alignment=TextAnchor.MiddleCenter},lcd);
            Text("KM/H",663,512,35,16,new GUIStyle(tinyStyle){fontSize=10},lcd);
            Text("GEAR "+gear,592,532,100,20,new GUIStyle(tinyStyle){fontSize=13,alignment=TextAnchor.MiddleCenter},lcd);
            GUI.matrix=old;
        }
        Fill(new Rect(20,18,1160,84),ink);Fill(new Rect(20,100,1160*Mathf.Clamp01(distance/finishDistance),3),accent);
        Text(Mathf.RoundToInt(speed).ToString(),42,23,115,52,new GUIStyle(titleStyle){fontSize=48});Text("KM/H   G"+gear,43,75,120,22,tinyStyle,muted);
        Text("POSITION",180,29,125,24,tinyStyle,muted);Text(place+" / 4",180,53,130,42,subStyle);
        Text("DISTANCE",320,29,140,24,tinyStyle,muted);Text((distance/1000).ToString("0.0")+" / "+(finishDistance/1000).ToString("0")+" KM",320,53,180,42,bodyStyle);
        Text("RACE TIME",510,29,140,24,tinyStyle,muted);Text(TimeSpan.FromSeconds(elapsed).ToString(@"mm\:ss"),510,53,145,42,subStyle);
        Meter("FUEL  "+Mathf.CeilToInt(fuel*100)+"%",fuel,680,34,180,fuel<.2f?new Color(1,.3f,.15f):accent);
        Text("AIR COINS",900,29,130,24,tinyStyle,muted);Text(raceCoins.ToString(),900,53,100,42,subStyle,new Color(1,.76f,.25f));
        Text(district,1030,36,140,50,new GUIStyle(tinyStyle){wordWrap=true},muted);
        if(Button("Ⅱ",1192,18,66,84)&&page==Page.Riding)SetPage(Page.Pause);
        if(page==Page.Riding){
            Fill(new Rect(26,570,240,112),brake?new Color(.7f,.18f,.08f,.9f):ink);Text("BRAKE",46,593,200,45,subStyle);Text("HOLD LEFT",46,641,200,24,tinyStyle,muted);
            Fill(new Rect(1014,570,240,112),throttle?new Color(.04f,.45f,.45f,.94f):ink);Text("THROTTLE",1034,593,210,45,subStyle);Text(tiltControl?"HOLD + TILT":"HOLD + DRAG TO STEER",1034,641,210,24,tinyStyle,muted);
            // Nitro cylinder is a distinct, large touch target with a visible charge fill.
            Fill(new Rect(824,550,146,124),ink);Fill(new Rect(875,558,42,8),metal.color);Fill(new Rect(870,568,52,56),new Color(.14f,.2f,.24f));
            Fill(new Rect(873,621-50*nitro,46,50*nitro),boosting?Color.white:accent);Text(boosting?"BOOSTING":"NITRO",824,625,146,28,buttonStyle,nitro>=.5f?accent:muted);
            Text(Mathf.RoundToInt(nitro*100)+"%",824,651,146,22,new GUIStyle(tinyStyle){alignment=TextAnchor.MiddleCenter},muted);
            if(GUI.Button(new Rect(824,550,146,124),GUIContent.none,GUIStyle.none))ActivateNitro();
            if(Button("JUMP ↑",310,590,150,78,false,jumpCooldown<=0))JumpBike();
            Text(Application.isMobilePlatform?(tiltControl?"TILT TO STEER":"DRAG WHILE HOLDING THROTTLE"):"W / S  THROTTLE / BRAKE     A / D  STEER     SPACE  JUMP     SHIFT  NITRO",320,692,700,22,tinyStyle,muted);
            if(countdown>0)Text(Mathf.CeilToInt(countdown).ToString(),540,220,200,150,new GUIStyle(titleStyle){alignment=TextAnchor.MiddleCenter,fontSize=120},accent);
            if(fuel<.2f&&Mathf.Sin(Time.time*5)>0)Text("LOW FUEL — COLLECT RED CANS",430,180,500,35,bodyStyle,new Color(1,.4f,.2f));
        }
        DrawGrandTourHUD();
        if(impact>0)Fill(new Rect(0,0,1280,720),new Color(1,.25f,.1f,impact*.17f));
    }
    void DrawMenu()
    {
        Fill(new Rect(0,0,1280,720),new Color(.01f,.025f,.045f,.42f));
        for(int i=0;i<5;i++){float x=(animationTime*30+i*300)%1550-150;Fill(new Rect(x,110,2,500),new Color(.15f,.8f,.9f,.06f));}
        Text("SPEED ROVERS 2",38,24,550,50,subStyle);Text("STREET RACING / FIRST PERSON",40,72,500,25,tinyStyle,accent);
        Fill(new Rect(1010,24,230,58),ink);Text(credits+"  CREDITS",1028,34,200,42,bodyStyle,new Color(1,.77f,.3f));
        if(page!=Page.Home&&Button("‹ BACK",40,623,170,60))SetPage(Page.Home);
        if(page==Page.Home){
            Fill(new Rect(26,139,610,430),new Color(.02f,.04f,.065f,.88f));
            Text("OWN THE\nOPEN ROAD.",50,155,590,192,titleStyle);
            Text("THREAD THE TRAFFIC. CHASE THE LEAD.",54,357,550,40,bodyStyle,accent);
            Text("Three rivals. Continuous steering. Every overtake counts.",54,408,545,70,new GUIStyle(bodyStyle){wordWrap=true},muted);
            if(Button("RACE NOW  ›",54,491,540,66,true))StartRace();
            // Use the actual cockpit art for a visual promise that matches gameplay.
            if(cockpitArt!=null)GUI.DrawTexture(new Rect(660,170,650,440),cockpitArt,ScaleMode.ScaleToFit,true);
            Text(bikeNames[bike],805,156,350,55,subStyle,accent);Text(routeNames[route],805,213,350,35,bodyStyle);
            if(Button("ROUTES",40,620,280,64))SetPage(Page.Routes);
            if(Button("GARAGE",340,620,280,64))SetPage(Page.Garage);
            if(Button("SETTINGS",640,620,280,64))SetPage(Page.Settings);
            if(Button("HOW TO RIDE",940,620,300,64))Announce("HOLD RIGHT + DRAG • LEFT BRAKES • JUMP FOR COINS");
        }else if(page==Page.Routes){
            Text("CHOOSE YOUR ROUTE",40,124,1000,90,titleStyle);
            for(int i=0;i<3;i++){float x=40+i*407;Fill(new Rect(x,240,385,330),ink);Fill(new Rect(x,240,385,5),i==route?accent:muted);
                Text("0"+(i+1),x+22,263,300,95,titleStyle,new Color(.25f,.4f,.48f));Text(routeNames[i],x+22,371,345,48,subStyle);
                Text(routeDescriptions[i],x+22,425,340,65,new GUIStyle(tinyStyle){wordWrap=true},muted);
                float best=PlayerPrefs.GetFloat("sr2_best_"+i,0);Text(best>0?"BEST  "+TimeSpan.FromSeconds(best).ToString(@"mm\:ss"):"SET YOUR FIRST RECORD",x+22,480,340,28,tinyStyle,accent);
                if(Button(i==route?"SELECTED":"SELECT ROUTE",x+22,517,340,42,i==route))route=i;
            }
            if(Button("START RACE  ›",940,623,300,60,true))StartRace();
        }else if(page==Page.Garage){
            Text("YOUR NEXT ADVANTAGE",40,124,1100,90,titleStyle);
            for(int i=0;i<3;i++){float x=40+i*407;bool owned=OwnBike(i);Fill(new Rect(x,240,385,330),ink);Fill(new Rect(x,240,385,5),liveries[i]);
                Text(bikeNames[i],x+22,266,350,50,subStyle,liveries[i]);
                Text((510+i*24)+" KM/H",x+22,337,340,58,new GUIStyle(titleStyle){fontSize=45});
                Meter("POWER",.72f+i*.1f,x+22,412,335,liveries[i]);Meter("NITRO",.8f,x+22,457,335,accent);
                if(Button(i==bike?"EQUIPPED":owned?"EQUIP":(i*250)+" CREDITS / UNLOCK",x+22,515,340,44,i==bike)){
                    if(owned||credits>=i*250){if(!owned){credits-=i*250;if(!qaMode)PlayerPrefs.SetInt("sr2_bike_owned_"+i,1);}bike=i;accent=liveries[i];
                        if(!qaMode){PlayerPrefs.SetInt("sr2_bike",bike);PlayerPrefs.SetInt("sr2_credits",credits);PlayerPrefs.Save();}Announce(bikeNames[i]+" READY");}
                    else Announce("RACE TO EARN "+(i*250-credits)+" MORE CREDITS");
                }
            }
        }else if(page==Page.Settings){
            Text("TUNE YOUR RIDE",40,124,1100,90,titleStyle);
            if(Button("ENGINE & EFFECTS   /   "+(soundOn?"ON":"OFF"),40,250,580,78)){soundOn=!soundOn;SaveOption("sound",soundOn);}
            if(Button("MUSIC   /   "+(musicOn?"ON":"OFF"),660,250,580,78)){musicOn=!musicOn;SaveOption("music",musicOn);}
            if(Button("SOFT HAPTICS   /   "+(hapticsOn?"ON":"OFF"),40,350,580,78)){hapticsOn=!hapticsOn;SaveOption("haptics",hapticsOn);}
            if(Button("STEERING   /   "+(tiltControl?"TILT":"THUMB DRAG"),660,350,580,78)){tiltControl=!tiltControl;SaveOption("tilt",tiltControl);}
            Text("HOLD RIGHT TO ACCELERATE. HOLD LEFT TO BRAKE.",44,475,1100,35,bodyStyle,accent);
            Text("Drag from your first touch to steer smoothly. Jump for airborne coins.\nNitro activates at 50% charge. Collect fuel cans to stay in the race.\nHits cost speed and bike health. Ride clean to repair; repeated hits can wreck the bike.",44,520,1100,100,new GUIStyle(bodyStyle){wordWrap=true},muted);
        }
    }
    void SaveOption(string name,bool value){if(qaMode)return;PlayerPrefs.SetInt("sr2_"+name,value?1:0);PlayerPrefs.Save();}
    void DrawPause()
    {
        Fill(new Rect(0,0,1280,720),new Color(.015f,.03f,.05f,.84f));Text("TAKE A BREATH.",340,180,700,110,titleStyle);
        if(Button("RESUME  ›",390,325,500,70,true))SetPage(Page.Riding);
        if(Button("RESTART RACE",390,415,500,64))StartRace();
        if(Button("BACK TO GARAGE",390,500,500,64))SetPage(Page.Home);
    }
    void DrawResults()
    {
        Fill(new Rect(0,0,1280,720),new Color(.015f,.03f,.05f,.88f));Text(resultHeading,70,115,1140,110,titleStyle,accent);
        Text("POSITION",75,270,240,35,bodyStyle,muted);Text(place+" / 4",75,315,240,100,titleStyle);
        Text("RACE TIME",390,270,240,35,bodyStyle,muted);Text(TimeSpan.FromSeconds(finishTime).ToString(@"mm\:ss"),390,315,300,100,titleStyle);
        Text("CREDITS EARNED",800,270,360,35,bodyStyle,muted);Text("+"+resultsReward,800,315,350,100,titleStyle,new Color(1,.76f,.25f));
        Text(raceCoins+" AIR COINS   /   "+nearMisses+" CLOSE PASSES",75,446,950,40,bodyStyle,muted);
        if(Button("RACE AGAIN",70,560,540,75,true))StartRace();if(Button("HOME / UPGRADES",650,560,560,75))SetPage(Page.Home);
    }
    IEnumerator Shot(string name)
    {
        yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(captureDir,name+".png"));yield return new WaitForSeconds(.2f);
    }
    int qaFailures;
    void Check(bool condition,string message){if(condition)Debug.Log("RACE QA PASS: "+message);else{qaFailures++;Debug.LogError("RACE QA FAIL: "+message);}}
    IEnumerator FullGameQA()
    {
        System.IO.Directory.CreateDirectory(captureDir);yield return new WaitForSeconds(2);yield return Shot("01-home");
        SetPage(Page.Garage);yield return Shot("02-garage");SetPage(Page.Routes);yield return Shot("03-routes");
        var safe=Screen.safeArea;float sc=Mathf.Min(safe.width/1280f,safe.height/720f);float lf=safe.x+(safe.width-1280*sc)*.5f,tp=Screen.height-safe.yMax+(safe.height-720*sc)*.5f;
        Check(ControlAt(new Vector2(lf+1100*sc,Screen.height-(tp+600*sc)))==1,"throttle aligns with wide-screen HUD");
        Check(ControlAt(new Vector2(lf+890*sc,Screen.height-(tp+600*sc)))==0,"nitro touch cannot trigger throttle");
        StartRace();countdown=0;hitCooldown=10;yield return new WaitForSeconds(1);Check(speed>20&&speed<60,"throttle accelerates gradually");
        speed=150;captureStage=1;yield return new WaitForSeconds(.5f);Check(speed<115,"held brake slows the bike");captureStage=0;
        speed=140;float y=jump;nitro=.8f;ActivateNitro();yield return new WaitForSeconds(.4f);Check(boosting&&nitro<.8f&&jump==y,"nitro consumes charge without lifting rider");
        boosting=false;nitro=.49f;ActivateNitro();Check(!boosting,"nitro requires half charge");
        speed=160;JumpBike();yield return new WaitForSeconds(.2f);Check(jump>.5f,"jump raises bike");
        var coin=pickups.Find(p=>p.kind==PickupKind.Coin);coin.at=distance;coin.x=PlayerX;coin.taken=false;int before=raceCoins;AdvancePickups();Check(raceCoins==before+1,"airborne coin collection");
        yield return new WaitForSeconds(1);Check(jump==0,"jump lands on road");
        fuel=.2f;var can=pickups.Find(p=>p.kind==PickupKind.Fuel);can.at=distance;can.x=PlayerX;can.taken=false;AdvancePickups();Check(fuel>.5f,"fuel pickup refills tank");
        hitCooldown=0;speed=240;gear=6;Collision(1.6f);Check(speed<150&&gear<6&&page==Page.Riding,"collision slows and downshifts without ending race");
        float after=speed;yield return new WaitForSeconds(.6f);Check(speed>after,"gradual collision recovery");
        SetPage(Page.Pause);float frozen=distance;yield return new WaitForSeconds(.3f);Check(distance==frozen,"pause freezes simulation");yield return Shot("04-pause");SetPage(Page.Riding);
        speed=180;hitCooldown=20;cars[1].position=new Vector3(1.85f,0,30);racers[0].progress=distance+20;racers[0].x=5.4f;yield return Shot("05-race");
        Check(racers.TrueForAll(r=>r.progress>0&&r.velocity>0),"all rivals advance independently");
        Check(DistrictName(500)==DistrictName(2100)&&DistrictName(3800)!=DistrictName(2100),"authored opening district lasts 3600 metres");
        Check(TopSpeed>=510&&cars.Count==3,"double baseline top speed and reduced traffic");
        float damaged=health;cleanTime=6;AdvanceGrandTour(1);Check(health>damaged,"bike health recovers after clean riding");
        Check(courseBook.routes.Length==3&&CourseNow.sections.Length==8&&finishDistance>=28000,"three fixed long courses with eight authored sections");
        for(int scene=0;scene<CourseNow.sections.Length;scene++){
            RestoreLogicalWorld();distance=CourseNow.sections[scene].start+800;speed=TopSpeed;fuel=1;health=1;hitCooldown=20;noticeUntil=0;
            for(int i=0;i<blocks.Count;i++){blocks[i].position=new Vector3(0,0,i*80-80);ConfigureDistrict(blocks[i],distance+blocks[i].position.z);}
            for(int i=0;i<racers.Count;i++){racers[i].progress=distance+25+i*18;racers[i].velocity=TopSpeed;}
            for(int i=0;i<cars.Count;i++)cars[i].position=new Vector3(i==0?-1.85f:5.4f,0,100+i*80);
            yield return new WaitForSeconds(.3f);yield return Shot("tour-"+scene);
            Check(!float.IsNaN(view.transform.position.y)&&Mathf.Abs(view.transform.position.y-CourseOffset(distance).y-1.55f)<.01f,"road and camera aligned in section "+scene);
        }
        health=.1f;hitCooldown=0;speed=400;Collision(1.6f);Check(page==Page.Results&&crashResult&&health==0,"accumulated impact damage wrecks the bike");
        StartRace();countdown=0;
        distance=finishDistance;FinishRace(true);int reward=credits;FinishRace(true);Check(credits==reward&&page==Page.Results,"finish awards credits exactly once");yield return Shot("07-results");
        StartRace();countdown=0;fuel=0;speed=0;yield return null;Check(page==Page.Results&&resultHeading=="OUT OF FUEL","empty fuel coasts to a result");
        Debug.Log("RACE QA COMPLETE: "+qaFailures+" failures");System.IO.File.WriteAllText(System.IO.Path.Combine(captureDir,"result.txt"),qaFailures+" failures");Application.Quit(qaFailures==0?0:1);
    }
}
