using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public partial class TrafficRidePrototype
{
    enum Page { Home, Routes, Garage, Settings, Riding, Pause, Results }
    enum PickupKind { Coin, Fuel }
    sealed class Pickup { public Transform root; public PickupKind kind; public float at, x; public bool taken; }
    sealed class Competitor { public Transform root, body; public readonly List<Transform> tires=new List<Transform>(); public float progress, velocity, x, target, decision, finish; }
    readonly List<Pickup> pickups=new List<Pickup>();
    readonly List<Competitor> racers=new List<Competitor>();
    readonly List<Transform> hazards=new List<Transform>();
    readonly Dictionary<int,Vector2> touchStarts=new Dictionary<int,Vector2>();
    readonly string[] routeNames={"METRO RUN","COASTAL EXPRESS","SKYLINE GRAND PRIX"};
    readonly string[] routeDescriptions={"6 KM / DOWNTOWN → RIVERSIDE → GARDENS","8 KM / COAST → MARINA → CITY","10 KM / CITY LIGHTS → BRIDGE → SKYLINE"};
    readonly string[] bikeNames={"APEX 600","VECTOR R","PHANTOM RS"};
    readonly Color[] liveries={new Color(.1f,.8f,.91f),new Color(1,.32f,.12f),new Color(.7f,.48f,1)};
    Page page=Page.Home;
    int route, bike, credits, raceCoins, place=4, nearMisses, gear=1;
    float fuel=1,nitro=.5f,jump,jumpVelocity,jumpCooldown,elapsed,countdown,finishDistance=6000,shiftDelay,nextPickup=180;
    float noticeUntil, nextFeedback, steerInput, animationTime, finishTime, resultsReward, nextHazard=400;
    string notice="", resultHeading="", district="DOWNTOWN";
    bool boosting,soundOn=true,musicOn=true,hapticsOn=true,tiltControl,earned,qaMode;
    AudioSource music,effects,wind;
    AudioClip clickSound,coinSound,fuelSound,boostSound,jumpSound,hitSound,passSound;
    GUIStyle titleStyle,subStyle,bodyStyle,tinyStyle,buttonStyle;
    Font gameFont;
    Material riverMaterial, grassMaterial;
    Color accent=new Color(.2f,.9f,.85f), ink=new Color(.025f,.045f,.07f,.93f), muted=new Color(.65f,.73f,.8f);
    Vector2 pointerStart;
#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")] static extern void SRSoftImpact();
#endif
    void InitializeRaceGame()
    {
        qaMode=capture;
        foreach(var a in Environment.GetCommandLineArgs()) if(a.StartsWith("-race-qa=")){captureDir=a.Substring(9);qaMode=true;capture=true;}
        Screen.orientation=ScreenOrientation.LandscapeLeft;
        Screen.autorotateToLandscapeLeft=true;Screen.autorotateToLandscapeRight=true;
        Screen.autorotateToPortrait=false;Screen.autorotateToPortraitUpsideDown=false;
        Screen.sleepTimeout=SleepTimeout.NeverSleep;
        Input.multiTouchEnabled=true;
        credits=qaMode?0:PlayerPrefs.GetInt("sr2_credits",0);
        bike=qaMode?0:Mathf.Clamp(PlayerPrefs.GetInt("sr2_bike",0),0,2);
        route=0; soundOn=PlayerPrefs.GetInt("sr2_sound",1)==1;musicOn=PlayerPrefs.GetInt("sr2_music",1)==1;
        hapticsOn=PlayerPrefs.GetInt("sr2_haptics",1)==1;tiltControl=PlayerPrefs.GetInt("sr2_tilt",0)==1;
        gameFont=Resources.Load<Font>("Interface/Fonts/Rajdhani-Bold");
        riverMaterial=Mat("River water",new Color(.11f,.38f,.48f),.65f,.86f);
        grassMaterial=Mat("Park lawn",new Color(.25f,.38f,.2f));
        effects=gameObject.AddComponent<AudioSource>();effects.volume=.5f;
        music=gameObject.AddComponent<AudioSource>();music.loop=true;music.volume=.18f;
        music.clip=Resources.Load<AudioClip>("Original/Audio/MenuSound");if(music.clip!=null)music.Play();
        wind=gameObject.AddComponent<AudioSource>();wind.loop=true;wind.clip=Resources.Load<AudioClip>("Audio/Rush/wind");if(wind.clip!=null)wind.Play();
        hitSound=Resources.Load<AudioClip>("Audio/Rush/impact");passSound=Resources.Load<AudioClip>("Audio/Rush/pass-by");
        clickSound=Tone("UI",660,.07f);coinSound=Tone("Coin",1100,.15f);fuelSound=Tone("Fuel",440,.3f);
        boostSound=Tone("Nitro",150,.55f);jumpSound=Tone("Jump",800,.2f);
        for(int i=0;i<3;i++)CreateCompetitor(i);
        for(int i=0;i<24;i++)CreatePickup(i);
        for(int i=0;i<5;i++){
            var hazard=new GameObject("Roadwork island").transform;
            Box("Warning barricade",new Vector3(0,.5f,0),new Vector3(1.4f,.7f,.45f),amber,hazard);
            for(int k=0;k<3;k++)Box("Reflector",new Vector3(-.5f+k*.5f,.5f,-.24f),new Vector3(.15f,.62f,.02f),white,hazard);
            for(int k=-1;k<=1;k++)Bar("Cone",new Vector3(k*.8f,.1f,-1),new Vector3(k*.8f,.6f,-1),.18f,amber,hazard);
            hazard.position=new Vector3(5.5f,0,450+i*180);hazard.gameObject.SetActive(false);hazards.Add(hazard);
        }
        foreach(var r in racers)r.root.gameObject.SetActive(false);
        foreach(var p in pickups)p.root.gameObject.SetActive(false);
        if(qaMode)StartCoroutine(FullGameQA());
    }
    AudioClip Tone(string name,float hz,float seconds)
    {
        const int rate=22050;var data=new float[(int)(rate*seconds)];
        for(int i=0;i<data.Length;i++){float t=(float)i/rate;data[i]=Mathf.Sin(t*hz*(1+t)*Mathf.PI*2)*Mathf.Sin((float)i/data.Length*Mathf.PI)*.3f;}
        var c=AudioClip.Create(name,data.Length,1,rate,false);c.SetData(data,0);return c;
    }
    void Feedback(AudioClip clip,bool vibrate=true)
    {
        if(soundOn&&clip!=null)effects.PlayOneShot(clip);
        if(!vibrate||!hapticsOn||qaMode||Time.unscaledTime<nextFeedback)return;
        nextFeedback=Time.unscaledTime+.1f;
#if UNITY_IOS && !UNITY_EDITOR
        SRSoftImpact();
#elif UNITY_ANDROID && !UNITY_EDITOR
        Handheld.Vibrate();
#endif
    }
    void Announce(string message){notice=message;noticeUntil=Time.unscaledTime+2.8f;}
    float TopSpeed=>255+bike*12;
    float PlayerX=>1.85f+lateral;
    bool OwnBike(int n)=>n==0||(!qaMode&&PlayerPrefs.GetInt("sr2_bike_owned_"+n,0)==1);
    void SetPage(Page next)
    {
        page=next;paused=next!=Page.Riding;engineVolume=0;
        if(music!=null){var desired=Resources.Load<AudioClip>(next==Page.Riding||next==Page.Pause?"Original/Audio/JungleBeat":"Original/Audio/MenuSound");
            if(desired!=music.clip){music.clip=desired;if(desired!=null)music.Play();}}
        Feedback(clickSound,false);
    }
    void StartRace()
    {
        distance=0;speed=0;lateral=0;lean=0;impact=0;hitCooldown=0;fuel=1;nitro=.5f;boosting=false;jump=jumpVelocity=0;
        jumpCooldown=0;elapsed=0;countdown=3;raceCoins=0;nearMisses=0;gear=1;shiftDelay=0;place=4;earned=false;finishTime=0;
        finishDistance=6000+route*2000;nextPickup=150;nextHazard=400;noticeUntil=0;
        for(int i=0;i<blocks.Count;i++){blocks[i].position=new Vector3(0,0,i*80-80);ConfigureDistrict(blocks[i],i*80);}
        for(int i=0;i<cars.Count;i++){cars[i].position=new Vector3(i%3==0?-1.85f:i%2==0?5.5f:1.85f,0,150+i*43);}
        for(int i=0;i<racers.Count;i++){var r=racers[i];r.progress=12+i*14;r.velocity=0;r.x= i==0?1.85f:i==1?5.4f:-1.8f;r.target=r.x;r.finish=0;r.decision=1+i*.8f;r.root.gameObject.SetActive(true);}
        foreach(var p in pickups){p.taken=false;p.at=nextPickup;nextPickup+=p.kind==PickupKind.Fuel?55:22;p.x=p.kind==PickupKind.Fuel?1.85f:(p.at%2<1?1.85f:5.4f);p.root.gameObject.SetActive(true);}
        for(int i=0;i<hazards.Count;i++){hazards[i].position=new Vector3(i%2==0?5.5f:-5.5f,0,nextHazard);nextHazard+=350;hazards[i].gameObject.SetActive(true);}
        SetPage(Page.Riding);
    }
    void CreateCompetitor(int index)
    {
        var r=new Competitor();r.root=new GameObject("Racer "+(index+1)).transform;
        r.body=new GameObject("Leaning bike and rider").transform;r.body.SetParent(r.root,false);
        var color=Mat("Racer livery",liveries[index],.55f,.65f);
        // Actual three-dimensional assemblies maintain perspective and cast shadows from every angle.
        for(int z=-1;z<=1;z+=2){
            var wheel=GameObject.CreatePrimitive(PrimitiveType.Cylinder);wheel.name="Motorcycle tire";wheel.transform.SetParent(r.body,false);
            wheel.transform.localPosition=new Vector3(0,.34f,z*.72f);wheel.transform.localScale=new Vector3(.65f,.12f,.65f);wheel.transform.localRotation=Quaternion.Euler(0,0,90);
            wheel.GetComponent<Renderer>().sharedMaterial=rubber;Destroy(wheel.GetComponent<Collider>());r.tires.Add(wheel.transform);
            for(int side=-1;side<=1;side+=2)Bar("Fork",new Vector3(side*.14f,.34f,z*.72f),new Vector3(side*.14f,.83f,z*.4f),.06f,metal,r.body);
        }
        Ball("Sculpted tank",new Vector3(0,.83f,.15f),new Vector3(.48f,.46f,.8f),color,r.body);
        Ball("Sport fairing",new Vector3(0,.74f,.52f),new Vector3(.54f,.55f,.55f),color,r.body);
        Box("Seat",new Vector3(0,.85f,-.38f),new Vector3(.35f,.12f,.62f),rubber,r.body);
        Ball("Tail",new Vector3(0,.83f,-.73f),new Vector3(.34f,.2f,.3f),color,r.body);
        var tail=Mat("Brake lamp",new Color(1,.03f,.015f));tail.EnableKeyword("_EMISSION");tail.SetColor("_EmissionColor",Color.red*2);
        Box("Tail light",new Vector3(0,.85f,-.9f),new Vector3(.21f,.06f,.025f),tail,r.body);
        Ball("Armored jacket",new Vector3(0,1.22f,-.12f),new Vector3(.51f,.65f,.34f),color,r.body).transform.localRotation=Quaternion.Euler(28,0,0);
        Ball("Full face helmet",new Vector3(0,1.58f,.16f),new Vector3(.34f,.37f,.39f),color,r.body);
        Ball("Visor",new Vector3(0,1.59f,.32f),new Vector3(.30f,.13f,.13f),glass,r.body);
        for(int side=-1;side<=1;side+=2){
            Bar("Jacket arm",new Vector3(side*.22f,1.35f,0),new Vector3(side*.29f,1.02f,.45f),.15f,rubber,r.body);
            Ball("Glove",new Vector3(side*.29f,1.02f,.45f),Vector3.one*.17f,rubber,r.body);
            Bar("Riding leg",new Vector3(side*.17f,.99f,-.35f),new Vector3(side*.28f,.53f,-.05f),.19f,rubber,r.body);
            Bar("Boot",new Vector3(side*.28f,.53f,-.05f),new Vector3(side*.28f,.39f,-.25f),.16f,rubber,r.body);
        }
        Bar("Exhaust",new Vector3(.26f,.4f,-.4f),new Vector3(.26f,.5f,-.84f),.13f,metal,r.body);
        racers.Add(r);
    }
    void CreatePickup(int i)
    {
        var p=new Pickup{kind=i%6==0?PickupKind.Fuel:PickupKind.Coin};p.root=new GameObject(p.kind+" pickup").transform;
        if(p.kind==PickupKind.Coin){
            var coin=GameObject.CreatePrimitive(PrimitiveType.Cylinder);coin.transform.SetParent(p.root,false);coin.transform.localRotation=Quaternion.Euler(90,0,0);
            coin.transform.localScale=new Vector3(.65f,.075f,.65f);coin.GetComponent<Renderer>().sharedMaterial=Mat("Gold",new Color(1,.66f,.12f),.85f,.75f);Destroy(coin.GetComponent<Collider>());
            Ball("Coin emblem",Vector3.zero,new Vector3(.32f,.32f,.18f),amber,p.root);
        }else{
            var red=Mat("Fuel can red",new Color(.9f,.12f,.04f),.35f,.6f);
            Box("Fuel can",Vector3.zero,new Vector3(.6f,.75f,.3f),red,p.root);Box("Fuel label",new Vector3(0,0,-.16f),new Vector3(.32f,.36f,.02f),white,p.root);
            Bar("Handle",new Vector3(-.2f,.48f,0),new Vector3(.15f,.48f,0),.07f,metal,p.root);
        }
        pickups.Add(p);
    }
    void ActivateNitro()
    {
        if(page!=Page.Riding||countdown>0||boosting||nitro<.5f||fuel<=0)return;
        boosting=true;Feedback(boostSound);Announce("NITRO ENGAGED");
    }
    void JumpBike()
    {
        if(page!=Page.Riding||countdown>0||jump>.01f||jumpCooldown>0||speed<15)return;
        jumpVelocity=5.6f;jumpCooldown=1.2f;Feedback(jumpSound);
    }
    void Collision(float mass)
    {
        if(hitCooldown>0)return;
        float severity=Mathf.Clamp(.12f+speed/600+mass*.12f,.2f,.8f);
        speed*=1-severity;gear=Mathf.Clamp(1+(int)(speed/45),1,6);shiftDelay=.45f;impact=1;hitCooldown=1.2f;boosting=false;
        Feedback(hitSound);Announce("CONTACT  /  KEEP RACING");
    }
    void Update()
    {
        if(view==null)return;
        float dt=Mathf.Min(Time.deltaTime,.05f);animationTime+=dt;
        music.mute=!musicOn;engine.mute=!soundOn;wind.mute=!soundOn;wind.volume=page==Page.Riding?speed/330*.22f:0;
        if(Input.GetKeyDown(KeyCode.Escape)||Input.GetKeyDown(KeyCode.P)){
            if(page==Page.Riding)SetPage(Page.Pause);else if(page==Page.Pause)SetPage(Page.Riding);else SetPage(Page.Home);}
        if(page!=Page.Riding){engineVolume=0;if(page!=Page.Pause&&page!=Page.Results){view.transform.position=new Vector3(1.85f,1.65f,0);view.transform.rotation=Quaternion.Euler(0,Mathf.Sin(Time.time*.12f)*4,0);}return;}
        if(countdown>0){countdown=Mathf.Max(0,countdown-dt);if(countdown==0){Feedback(coinSound);Announce("GO / RACE TO THE FINISH");}return;}
        ReadControls();
        if(Input.GetKeyDown(KeyCode.Space))JumpBike();if(Input.GetKeyDown(KeyCode.LeftShift)||Input.GetKeyDown(KeyCode.N))ActivateNitro();
        if(qaMode){throttle=captureStage==0;brake=captureStage==1;steerInput=0;}
        float acceleration=Mathf.Lerp(34,13,speed/TopSpeed);
        shiftDelay=Mathf.Max(0,shiftDelay-dt);
        float cap=boosting?TopSpeed*1.3f:TopSpeed;
        float force=brake?-100:fuel<=0?-35:throttle?(shiftDelay>0?2:acceleration)*(boosting?2.2f:1):-10;
        speed=Mathf.Clamp(speed+force*dt,0,Mathf.Max(cap,speed-28*dt));
        int wanted=Mathf.Clamp(1+(int)(speed/45),1,6);
        if(wanted>gear&&shiftDelay==0){gear++;shiftDelay=.22f;Feedback(clickSound,false);}else if(wanted<gear)gear=wanted;
        fuel=Mathf.Max(0,fuel-dt/95*(boosting?1.35f:1));
        if(boosting){nitro=Mathf.Max(0,nitro-dt*.17f);if(nitro<=0||fuel<=0)boosting=false;}else nitro=Mathf.Min(1,nitro+dt*.035f);
        jumpCooldown=Mathf.Max(0,jumpCooldown-dt);
        if(jump>0||jumpVelocity>0){jumpVelocity-=12*dt;jump=Mathf.Max(0,jump+jumpVelocity*dt);if(jump==0){jumpVelocity=0;Feedback(clickSound);}}
        lateral=Mathf.Clamp(lateral+steerInput*dt*Mathf.Lerp(1.5f,5.8f,speed/130),-8.6f,4.9f);
        lean=Mathf.Lerp(lean,-steerInput*10,dt*7);impact=Mathf.MoveTowards(impact,0,dt*3);hitCooldown-=dt;
        float travel=speed/3.6f*dt;distance+=travel;elapsed+=dt;
        district=DistrictName(distance);
        foreach(var block in blocks){block.position-=Vector3.forward*travel;if(block.position.z< -160){block.position+=Vector3.forward*560;ConfigureDistrict(block,distance+block.position.z);}}
        AdvanceTraffic(dt,travel);AdvanceRacers(dt);AdvancePickups();
        foreach(var h in hazards){h.position-=Vector3.forward*travel;if(h.position.z<-20){h.position=new Vector3(h.position.x,0,nextHazard-distance);nextHazard+=400;}
            if(Mathf.Abs(h.position.z)<1.8f&&Mathf.Abs(h.position.x-PlayerX)<1.1f&&jump<.8f)Collision(.6f);}
        view.transform.position=new Vector3(PlayerX,1.55f+jump+Mathf.Sin(Time.time*24)*speed/300*.006f,0);
        view.transform.rotation=Quaternion.Euler(impact*3+(brake?1.5f:0),steerInput*1.5f,lean*.25f);
        view.fieldOfView=Mathf.Lerp(view.fieldOfView,75+speed/300*7+(boosting?5:0),dt*3);
        enginePitch=45+gear*8+(speed%45)*2;engineVolume=throttle?.055f:.025f;
        place=1;foreach(var r in racers)if(r.progress>distance)place++;
        if(distance>=finishDistance)FinishRace(true);
        else if(fuel<=0&&speed<=.1f)FinishRace(false);
    }
    void ReadControls()
    {
        throttle=Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow);brake=Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow);
        steerInput=Input.GetAxisRaw("Horizontal");
        if(Input.GetMouseButtonDown(0))pointerStart=Input.mousePosition;
        // Reserve top HUD and the centre ability controls from throttle/brake gestures.
        if(Input.touchCount==0&&Input.GetMouseButton(0)){
            int control=ControlAt(pointerStart);if(control==1){throttle=true;steerInput=Mathf.Clamp((Input.mousePosition.x-pointerStart.x)/(Screen.width*.1f),-1,1);}else if(control==-1)brake=true;}
        for(int i=0;i<Input.touchCount;i++){
            var t=Input.GetTouch(i);if(t.phase==TouchPhase.Began)touchStarts[t.fingerId]=t.position;
            if(t.phase==TouchPhase.Ended||t.phase==TouchPhase.Canceled){touchStarts.Remove(t.fingerId);continue;}
            if(!touchStarts.TryGetValue(t.fingerId,out var start))continue;
            int control=ControlAt(start);if(control==1){throttle=true;if(!tiltControl)steerInput=Mathf.Clamp((t.position.x-start.x)/(Screen.width*.1f),-1,1);}else if(control==-1)brake=true;
        }
        if(tiltControl&&Application.isMobilePlatform)steerInput=Mathf.Clamp(Input.acceleration.x*2.8f,-1,1);
    }
    int ControlAt(Vector2 point)
    {
        var safe=Screen.safeArea;float scale=Mathf.Min(safe.width/1280f,safe.height/720f);
        float left=safe.x+(safe.width-1280*scale)*.5f,top=Screen.height-safe.yMax+(safe.height-720*scale)*.5f;
        var p=new Vector2((point.x-left)/scale,(Screen.height-point.y-top)/scale);
        if(p.y<250||p.y>720)return 0;
        return p.x>=1000?1:p.x<=280?-1:0;
    }
    void AdvanceTraffic(float dt,float travel)
    {
        for(int i=0;i<cars.Count;i++){
            var car=cars[i];float own=i%3==0?-24:24+i*1.3f;float oldZ=car.position.z;
            // Brake behind a work site or slower vehicle instead of driving through it.
            foreach(var h in hazards)if(Mathf.Abs(h.position.x-car.position.x)<1.5f&&h.position.z-car.position.z>0&&h.position.z-car.position.z<25&&own>0)own=5;
            car.position+=Vector3.forward*(own*dt-travel);
            if(car.position.z<-35){car.position=new Vector3(i%3==0?-1.85f:i%2==0?5.5f:1.85f,0,350+i*31);}
            if(oldZ>0&&car.position.z<=0&&Mathf.Abs(car.position.x-PlayerX)>1.6f&&Mathf.Abs(car.position.x-PlayerX)<2.7f){nearMisses++;Feedback(passSound,false);Announce("CLOSE PASS +5");}
            if(Mathf.Abs(car.position.z)<2.8f&&Mathf.Abs(car.position.x-PlayerX)<1.55f)Collision(1.6f);
        }
        for(int w=0;w<wheels.Count;w++){float own=w/4%3==0?24:24+w/4*1.3f;wheels[w].Rotate(Vector3.right,own/.34f*Mathf.Rad2Deg*dt,Space.Self);}
    }
    void AdvanceRacers(float dt)
    {
        for(int i=0;i<racers.Count;i++){
            var r=racers[i];float gap=distance-r.progress;
            // Bounded catch-up: rivals race independently and can be beaten with clean riding and nitro.
            float target=TopSpeed*(.87f+i*.025f)+Mathf.Clamp(gap*.025f,-14,15);
            float avoid=0;
            foreach(var c in cars)if(c.position.z-(r.progress-distance)>0&&c.position.z-(r.progress-distance)<38&&Mathf.Abs(c.position.x-r.x)<2)avoid=c.position.x>2?1.85f:5.4f;
            foreach(var h in hazards)if(h.position.z-(r.progress-distance)>0&&h.position.z-(r.progress-distance)<30&&Mathf.Abs(h.position.x-r.x)<1.5f)avoid=1.85f;
            r.decision-=dt;if(avoid!=0)r.target=avoid;else if(r.decision<=0){r.target=(i+(int)(elapsed/7))%2==0?1.85f:5.4f;r.decision=4+i;}
            r.velocity=Mathf.MoveTowards(r.velocity,target,dt*(r.velocity<150?29:16));
            r.progress+=r.velocity/3.6f*dt;float old=r.x;r.x=Mathf.MoveTowards(r.x,r.target,dt*2.4f);
            r.root.position=new Vector3(r.x,0,r.progress-distance);r.body.localRotation=Quaternion.Euler(0,0,-(r.x-old)/Mathf.Max(dt,.001f)*6);
            foreach(var tire in r.tires)tire.Rotate(Vector3.up,r.velocity/3.6f/.325f*Mathf.Rad2Deg*dt,Space.Self);
            if(r.progress>=finishDistance&&r.finish==0)r.finish=elapsed;
            if(Mathf.Abs(r.root.position.z)<1.7f&&Mathf.Abs(r.x-PlayerX)<.65f){Collision(.4f);r.velocity*=.85f;}
        }
    }
    void AdvancePickups()
    {
        foreach(var p in pickups){
            if(p.at<distance-10){p.at=nextPickup;nextPickup+=p.kind==PickupKind.Fuel?55:22;p.taken=false;p.x=p.kind==PickupKind.Fuel?1.85f:(Mathf.FloorToInt(p.at/100)%2==0?1.85f:5.4f);}
            p.root.gameObject.SetActive(!p.taken&&p.at-distance<500);
            p.root.position=new Vector3(p.x,p.kind==PickupKind.Coin?2.15f:.65f,p.at-distance);
            p.root.rotation=Quaternion.Euler(0,Time.time*120,0);
            if(!p.taken&&Mathf.Abs(p.at-distance)<3&&Mathf.Abs(p.x-PlayerX)<1){
                if(p.kind==PickupKind.Coin&&jump>.4f){p.taken=true;raceCoins++;Feedback(coinSound);Announce("AIR COIN +1");}
                else if(p.kind==PickupKind.Fuel&&jump<.4f){p.taken=true;fuel=Mathf.Min(1,fuel+.4f);Feedback(fuelSound);Announce("FUEL +40%");}
            }
        }
    }
    string DistrictName(float at){int zone=((int)(at/2200)+route)%3;return zone==0?"DOWNTOWN":zone==1?"RIVERSIDE":"GARDEN DISTRICT";}
    void ConfigureDistrict(Transform block,float at)
    {
        int zone=((int)(Mathf.Max(0,at)/2200)+route)%3;
        foreach(Transform child in block){
            if(child.name=="Leafy street tree"||child.name=="Tree planter")child.gameObject.SetActive(zone!=1);
            if(child.name=="Building"||child.name=="Roof cornice"||child.name=="Masonry plinth")child.gameObject.SetActive(zone==0||(zone==2&&child.localPosition.z%30<10));
        }
        // Continuous road and curbs remain identical at district boundaries.
        var extra=block.Find("District scenery");if(extra!=null)Destroy(extra.gameObject);
        var scenery=new GameObject("District scenery").transform;scenery.SetParent(block,false);
        if(zone==1){
            var water=riverMaterial;
            Box("River",new Vector3(-110,-1,40),new Vector3(195,.2f,80),water,scenery);
            Box("River east",new Vector3(110,-1,40),new Vector3(195,.2f,80),water,scenery);
            for(int side=-1;side<=1;side+=2){
                Box("Bridge deck",new Vector3(side*9,-.25f,40),new Vector3(3,.5f,80),concrete,scenery);
                Bar("Guardrail",new Vector3(side*10.3f,1,0),new Vector3(side*10.3f,1,80),.09f,metal,scenery);
                for(int k=0;k<10;k++)Bar("Rail post",new Vector3(side*10.3f,0,k*8),new Vector3(side*10.3f,1,k*8),.08f,metal,scenery);
            }
        }else if(zone==2){
            var grass=grassMaterial;
            for(int side=-1;side<=1;side+=2)Box("Park",new Vector3(side*27,-.1f,40),new Vector3(33,.2f,80),grass,scenery);
        }
    }
    void FinishRace(bool finished)
    {
        if(earned)return;earned=true;finishTime=elapsed;boosting=false;
        resultHeading=finished?(place==1?"VICTORY":"RACE COMPLETE"):"OUT OF FUEL";
        resultsReward=raceCoins+nearMisses*5+(finished?(5-place)*30:0);credits+=(int)resultsReward;
        if(!qaMode){PlayerPrefs.SetInt("sr2_credits",credits);if(finished){float best=PlayerPrefs.GetFloat("sr2_best_"+route,float.MaxValue);PlayerPrefs.SetFloat("sr2_best_"+route,Mathf.Min(best,elapsed));}PlayerPrefs.Save();}
        SetPage(Page.Results);Feedback(coinSound);
    }
    void OnApplicationPause(bool value){if(value&&page==Page.Riding)SetPage(Page.Pause);}
    void OnApplicationFocus(bool value){if(!value&&page==Page.Riding&&!qaMode)SetPage(Page.Pause);}
}
