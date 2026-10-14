using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using SpeedRovers.Reborn;

// Batch entry point: -executeMethod SpeedRoversChecks.Run
// Uses the production simulation and loaded assets, with preference writes disabled.
public static class SpeedRoversChecks
{
    const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    static SpeedRoversGame game;
    static int checks;

    static object Get(string name) => typeof(SpeedRoversGame).GetField(name, Fields).GetValue(game);
    static void Set(string name, object value) => typeof(SpeedRoversGame).GetField(name, Fields).SetValue(game, value);
    static void Call(string name, params object[] values) => typeof(SpeedRoversGame).GetMethod(name, Fields).Invoke(game, values);
    static void Check(bool valid, string message)
    {
        if (!valid) throw new Exception("Speed Rovers regression: " + message);
        checks++;
    }
    static float Number(string name) => (float)Get(name);
    static void Mode(string name)
    {
        Type type = typeof(SpeedRoversGame).GetField("mode", Fields).FieldType;
        Set("mode", Enum.Parse(type, name));
    }
    static void ClearRoad() => ((IList)Get("things")).Clear();
    static void Start()
    {
        Call("StartRun");
        Set("spawnDistance", 100000f);
        ClearRoad();
    }
    static void AddThing(string kind, int lane)
    {
        Type type = typeof(SpeedRoversGame).GetNestedType("Thing", BindingFlags.NonPublic);
        object thing = Activator.CreateInstance(type, true);
        type.GetField("distance").SetValue(thing, Number("distance") + 3f);
        type.GetField("lane").SetValue(thing, lane);
        FieldInfo kindField = type.GetField("kind");
        kindField.SetValue(thing, Enum.Parse(kindField.FieldType, kind));
        ((IList)Get("things")).Add(thing);
    }

    public static void Run()
    {
        var root = new GameObject("Speed Rovers regression checks");
        try
        {
            game = root.AddComponent<SpeedRoversGame>();
            if (Get("pixel") == null) Call("Awake");
            Set("verificationRun", true);
            var surfaceImporter=(TextureImporter)AssetImporter.GetAtPath("Assets/Resources/Painted/CityAsphalt.png");
            Check(surfaceImporter!=null && surfaceImporter.mipmapEnabled && surfaceImporter.wrapMode==TextureWrapMode.Repeat,"city asphalt must tile with mip filtering");
            foreach(string shaderPath in new[]{"Painted/CitySurface","Painted/VehicleMotion","Painted/FoliageWind","Painted/TireMotion"})
            {
                Shader shader=Resources.Load<Shader>(shaderPath);
                Check(shader!=null,"missing shader "+shaderPath);
                foreach(var message in ShaderUtil.GetShaderMessages(shader))
                    Check(message.severity!=UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error,"shader compile: "+message.message);
            }
            Check(root.GetComponentInChildren<AudioListener>()!=null,"no audio listener: mixer would be silent");
            foreach (string name in new[] { "districtSkies", "bridgeStructures", "dayStreetProps", "fastAsphalt", "rollingTire", "spinningRim", "rushOncoming", "rushHero", "rushBikes", "rushTraffic", "rushBuildings", "rushHazards", "paintedRider", "paintedWorlds", "paintedScenery", "paintedTraffic", "asphalt", "foliageSheet", "waterSheet", "glowSheet", "cloudSheet", "tireSheet" })
                Check(Get(name) != null, "missing painted art: " + name);
            foreach (string name in new[] { "riderFrames", "slowRiderFrames", "trafficFrames", "buildingFrames",
                         "skyArt", "skylineArt", "grassArt", "roadDarkArt", "roadLightArt", "cityCardArt", "riderCardArt" })
                foreach (object art in (Array)Get(name)) Check(art != null, "missing texture in " + name);

            for (int run = 0; run <= 2000; run += 250)
                for (int lane = -1; lane <= 1; lane++)
                {
                    Vector2 contact = RoadProjection.Project(run, 0, lane, lane);
                    Check(Mathf.Abs(contact.x - 270) < .001f, "camera and matching lane diverge at rider");
                    float lastY = 0;
                    for (int z = 470; z >= 0; z -= 5)
                    {
                        Vector2 point = RoadProjection.Project(run, z, lane, lane);
                        Check(point.y >= lastY && !float.IsNaN(point.x), "invalid perspective ordering");
                        float roundTrip = RoadProjection.DistanceAtDepth(RoadProjection.Depth(z));
                        Check(Mathf.Abs(roundTrip - z) < .001f, "road and object depth disagree");
                        lastY = point.y;
                    }
                }

            Start();
            for (int n = 0; n < 120; n++) Call("AdvanceElapsedTime", 1f / 60f);
            float fineDistance = Number("distance");
            Start();
            for (int n = 0; n < 60; n++) Call("AdvanceElapsedTime", 1f / 30f);
            Check(Mathf.Abs(Number("distance") - fineDistance) < .12f, "frame rate changes travel distance");
            Start();
            for(int n=0;n<20;n++)Call("AdvanceElapsedTime",.1f);
            Check(Mathf.Abs(Number("distance")-fineDistance)<.2f,"10 fps rendering loses simulation time");

            Start();
            Call("BeginSteering", new Vector2(270, 200), 0f);
            Call("MoveSteering", new Vector2(315, 200), .4f, false);
            Check(Mathf.Abs(Number("targetLane") - .25f) < .001f, "held thumb must steer between lanes");
            Call("MoveSteering", new Vector2(360, 200), .8f, true);
            Check(Mathf.Abs(Number("targetLane") - .5f) < .001f, "release must retain fractional position");
            Call("BeginSteering", new Vector2(270, 200), 1f);
            Call("MoveSteering", new Vector2(900, 200), 2f, false);
            Check(Number("targetLane") == 1.32f, "drag must stop before road edge");
            Call("MoveSteering", new Vector2(-900, 200), 3f, true);
            Check(Number("targetLane") == -1.32f, "left edge must bound dragging");
            Start();
            Call("BeginSteering", new Vector2(270, 200), 0f);
            Call("MoveSteering", new Vector2(320, 200), .1f, true);
            Check(Number("targetLane") == 1f, "quick swipe must step once");
            Call("BeginSteering", new Vector2(490, 920), 1f);
            Check(!(bool)Get("touchStarted"), "pause HUD must not begin steering");

            Set("distance", 420f);
            Set("lane", .3f);
            Set("targetLane", 1f);
            Mode("Paused");
            float pausedDistance = Number("distance"), pausedTime = Number("timeLeft"), pausedAnimation = Number("animationTime");
            Call("AdvanceRun", 1f);
            Check(Number("distance") == pausedDistance && Number("timeLeft") == pausedTime
                && Number("animationTime") == pausedAnimation && Number("lane") == .3f,
                "pause advances scenery, rider or timer");
            Mode("Running");
            Call("AdvanceRun", .02f);
            Check(Number("distance") > pausedDistance && Number("lane") > .3f, "resume does not continue run");

            Start();
            AddThing("Coin", 0);
            Call("AdvanceRun", .02f);
            Check((int)Get("runCoins") == 1, "coin pickup missing");
            Call("AdvanceRun", .02f);
            Check((int)Get("runCoins") == 1, "coin counted twice");
            AddThing("Clock", 0);
            float beforeClock = Number("timeLeft");
            Call("AdvanceRun", .02f);
            Check(Mathf.Abs(Number("timeLeft") - beforeClock - 3.98f) < .001f, "clock reward missing");

            Start();
            Set("distance", 499.9f);
            float beforeGate = Number("timeLeft");
            Call("AdvanceRun", .02f);
            Check((int)Get("checkpoints") == 1 && Number("timeLeft") > beforeGate + 15f, "checkpoint not awarded");
            Call("AdvanceRun", .02f);
            Check((int)Get("checkpoints") == 1, "checkpoint counted twice");

            Start();
            AddThing("Traffic", 1);
            object movingCar = ((IList)Get("things"))[0];
            Type movingType = movingCar.GetType();
            movingType.GetField("travelSpeed").SetValue(movingCar, 10f);
            float beforeTraffic = (float)movingType.GetField("distance").GetValue(movingCar);
            Call("AdvanceRun", .02f);
            Check(Mathf.Abs((float)movingType.GetField("distance").GetValue(movingCar)-beforeTraffic-.2f*RoadProjection.TravelScale)<.001f,
                "moving traffic does not advance in world metres");
            Check(Get("mode").ToString() == "Running", "traffic in another lane causes collision");
            AddThing("Traffic", 0);
            Call("AdvanceRun", .02f);
            Check(Get("mode").ToString() == "Running" && Number("hitPulse")>0, "traffic impact must slow the rider and keep the race running");
            Start();
            AddThing("Barrier", 0);
            Call("AdvanceRun", .02f);
            Check(Get("mode").ToString() == "Running" && Number("speed")<65, "roadblock impact must slow the rider and keep the race running");
            Start();
            Set("timeLeft", .001f);
            Call("AdvanceRun", .02f);
            Check(Get("mode").ToString() == "Running", "elapsed time must not end a race");
            Start();AddThing("Pothole",0);float beforeHit=Number("speed");Call("AdvanceRun",.02f);
            Check(Get("mode").ToString()=="Running" && Number("speed")<beforeHit,"broken pavement must slow the bike without ending the race");
            Start();AddThing("Rival",0);Call("AdvanceRun",.02f);
            Check(Get("mode").ToString()=="Running" && Number("hitPulse")>0,"bike contact response missing");
            Start();AddThing("Roadworks",0);Call("AdvanceRun",.02f);
            Check(Get("mode").ToString()=="Running" && Number("hitPulse")>0,"construction must cost speed rather than end the race");
            Start();AddThing("Traffic",1);Call("AdvanceRun",.02f);
            object passing=((IList)Get("things"))[0];
            Check(!(bool)passing.GetType().GetField("used").GetValue(passing),"other-lane vehicle disappears before passing camera");
            for(int d=0;d<7;d++)
            {
                Check(RoadRoute.District(RoadRoute.Starts[d]+100)==d,"route district start disagrees with scenery");
                float end=d==6?RoadRoute.Length:RoadRoute.Starts[d+1];
                Check((end-RoadRoute.Starts[d])/(465f/3.6f*RoadProjection.TravelScale)>=30,"scenery section lasts less than 30 seconds at maximum cruising speed");
            }
            Check(RoadRoute.District(RoadRoute.Length)==0,"route does not loop to downtown");
            Check(Resources.Load<Texture2D>("Painted/TravelSkies")!=null && Resources.Load<Texture2D>("Painted/TravelProps")!=null,"travel district art missing");
            Check(!RoadRoute.Junction(3020*RoadRoute.PacingScale) && !RoadRoute.Junction(4520*RoadRoute.PacingScale),"cross streets appear on coastal or alpine roads");
            Check(RoadRoute.Junction(200) && !RoadRoute.Junction(240) && !RoadRoute.Junction(RoadRoute.Starts[1]+100),"crossroads are not confined to correct route segments");
            Check(!RoadRoute.Work(419) && RoadRoute.Work(420) && RoadRoute.Work(461) && !RoadRoute.Work(463),"roadwork patch and hazard boundaries disagree");
            Call("StartRun");Call("SpawnRushTraffic");
            int bikes=0,cars=0;foreach(object t in (IList)Get("things"))
            {string k=t.GetType().GetField("kind").GetValue(t).ToString();if(k=="Rival")bikes++;if(k=="Traffic")cars++;Check(k!="Barrier","legacy roadblocks still spawn");}
            Check(bikes>=2 && bikes+cars<=5,"opening traffic exceeds sparse composition or lacks rivals");
            Start();Set("distance",3000f);Set("speed",427.5f);Call("ResetEngine",427.5f);AddThing("Rival",-1);
            object racer=((IList)Get("things"))[0];Type racerType=racer.GetType();
            racerType.GetField("distance").SetValue(racer,3100f);racerType.GetField("travelSpeed").SetValue(racer,427.5f/3.6f);
            for(int tick=0;tick<125;tick++)
            {
                Call("AdvanceTraffic",racer,.02f);
                Set("distance",Number("distance")+427.5f/3.6f*RoadProjection.TravelScale*.02f);
            }
            Check((float)racerType.GetField("travelSpeed").GetValue(racer)>427.5f/3.6f*.9f,"competition bike cannot sustain racing pace");
            Check(Mathf.Abs((float)racerType.GetField("distance").GetValue(racer)-Number("distance"))<180,"rival is rapidly abandoned at cruising speed");
            racerType.GetField("distance").SetValue(racer,Number("distance")-25);
            Call("AdvanceRun",1f/120);
            Check(!(bool)racerType.GetField("used").GetValue(racer),"overtaken competitor is removed instead of continuing the race");
            Check(Resources.Load<Shader>("Painted/ProjectedLine")!=null,"clipped bridge line shader missing");
            Start();AddThing("Oncoming",1);
            object toward=((IList)Get("things"))[0];toward.GetType().GetField("travelSpeed").SetValue(toward,-25f);
            float ahead=(float)toward.GetType().GetField("distance").GetValue(toward);Call("AdvanceRun",.02f);
            Check((float)toward.GetType().GetField("distance").GetValue(toward)<ahead,"oncoming traffic moves away from player");
            Start();AddThing("Oncoming",0);Call("AdvanceRun",.02f);Check(Get("mode").ToString()=="Running" && Number("hitPulse")>0,"oncoming impact must preserve the race");
            Start();Set("distance",RoadRoute.Length-.5f);Set("speed",428f);Call("ResetEngine",428f);Call("AdvanceRun",.02f);
            Check(Get("mode").ToString()=="Results" && (int)Get("finishPlace")==1,"finish line does not determine race result");
            Start();Set("distance",RoadRoute.Length-.5f);Set("speed",428f);Call("ResetEngine",428f);AddThing("Rival",-1);
            object winner=((IList)Get("things"))[0];winner.GetType().GetField("finishTime").SetValue(winner,0f);winner.GetType().GetField("distance").SetValue(winner,RoadRoute.Length);
            Call("AdvanceRun",.02f);
            Check((int)Get("finishPlace")==2 && Get("result").ToString()=="RACE FINISHED","opponent finishing first is not counted in result");
            Start();Set("speed",428f);Call("ResetEngine",428f);AddThing("Traffic",0);
            object smallCar=((IList)Get("things"))[0];Call("ApplyRaceImpact",smallCar);float carAfter=Number("speed");
            Set("speed",428f);smallCar.GetType().GetField("variant").SetValue(smallCar,9);Call("ApplyRaceImpact",smallCar);
            Check(Number("speed")<carAfter,"truck impact is no harder than a car impact");
            Set("speed",428f);smallCar.GetType().GetField("variant").SetValue(smallCar,0);smallCar.GetType().GetField("travelSpeed").SetValue(smallCar,100f);Call("ApplyRaceImpact",smallCar);float sameDirection=Number("speed");
            Set("speed",428f);smallCar.GetType().GetField("travelSpeed").SetValue(smallCar,-100f);Call("ApplyRaceImpact",smallCar);
            Check(Number("speed")<sameDirection,"oncoming closing speed does not affect impact severity");
            Check((int)Get("gear")<6,"impact does not downshift");
            Start();Set("speed",291f);Call("ResetEngine",291f);float rpmBefore=Number("engineRpm");
            Set("speed",296f);Call("AdvanceEngine",.05f,true);
            Check((int)Get("gear")==5 && Number("engineRpm")<rpmBefore,"upshift does not drop RPM");
            Set("speed",80f);for(int n=0;n<30;n++)Call("AdvanceEngine",.05f,true);
            Check((int)Get("gear")<5,"slowing down does not downshift");
            Start();Set("easyMode",false);Set("speed",65f);Call("ResetEngine",65f);
            Check((int)Get("gear")==1,"race does not begin in first gear");
            for(int n=0;n<240;n++) {Call("AdvanceDrive",1f/120);Call("AdvanceEngine",1f/120,true);}
            Check(Number("speed")>100 && Number("speed")<190,"acceleration jumps directly to cruising pace");
            for(int n=0;n<1680;n++) {Call("AdvanceDrive",1f/120);Call("AdvanceEngine",1f/120,true);}
            Check(Mathf.Abs(Number("speed")-427.5f)<.1f && (int)Get("gear")==6,"cruising pace is not 50 percent above 285 km/h");
            AddThing("Rival",0);Call("AdvanceRun",1f/120);
            float bumped=Number("speed");int bumpedGear=(int)Get("gear");
            Check(bumped<350 && bumpedGear<6,"bike contact does not slow and downshift clearly");
            Check(!((bool)((IList)Get("things"))[0].GetType().GetField("used").GetValue(((IList)Get("things"))[0])),"rival disappears when bumped");
            for(int n=0;n<120;n++) {Call("AdvanceDrive",1f/120);Call("AdvanceEngine",1f/120,true);}
            Check(Number("speed")>bumped && Number("speed")<bumped+60,"bike contact recovery is immediate");
            for(int n=0;n<960;n++) {Call("AdvanceDrive",1f/120);Call("AdvanceEngine",1f/120,true);}
            Check(Number("speed")>=427 && (int)Get("gear")==6,"bike fails to regain cruising pace through upshifts");
            foreach(string art in new[]{"TrafficAlignedA","TrafficAlignedB","TrafficAlignedC"})
                Check(Resources.Load<Texture2D>("Painted/"+art)!=null,"aligned car art missing: "+art);
            Start();float wheelBefore=Number("wheelTurns");Call("AdvanceRun",.02f);
            Check(Number("wheelTurns")>wheelBefore,"rear wheel phase does not follow distance");
            Mode("Paused");wheelBefore=Number("wheelTurns");Call("AdvanceRun",1f);
            Check(Number("wheelTurns")==wheelBefore,"wheels move while paused");
            foreach(string clip in new[]{"engine-load","engine-coast","wind","police-wail","pass-by","impact"})
                Check(Resources.Load<AudioClip>("Audio/Rush/"+clip)!=null,"missing layered audio: "+clip);
            // Rival approaches the first closure; inspect every movement step.
            Start();AddThing("Rival",1);
            object merging=((IList)Get("things"))[0];Type trafficType=merging.GetType();
            trafficType.GetField("distance").SetValue(merging,240f);
            trafficType.GetField("travelSpeed").SetValue(merging,48f);
            for(int step=0;step<720;step++)
            {
                Call("AdvanceTraffic",merging,1f/120);
                float at=(float)trafficType.GetField("distance").GetValue(merging);
                float across=(int)trafficType.GetField("lane").GetValue(merging)+(float)trafficType.GetField("lateral").GetValue(merging);
                Check(at<385 || at>464 || Mathf.Abs(across-1)>.69f,"rival penetrates roadwork lane");
            }
            Check((float)trafficType.GetField("distance").GetValue(merging)>464,"rival cannot complete safe merge");
            Start();AddThing("Traffic",1);AddThing("Traffic",0);
            object waiting=((IList)Get("things"))[0],blocker=((IList)Get("things"))[1];
            trafficType.GetField("distance").SetValue(waiting,340f);
            trafficType.GetField("travelSpeed").SetValue(waiting,45f);
            trafficType.GetField("distance").SetValue(blocker,360f);
            for(int step=0;step<480;step++)Call("AdvanceTraffic",waiting,1f/120);
            Check((float)trafficType.GetField("distance").GetValue(waiting)<=380.01f,"blocked traffic drives through cones instead of stopping");
            Check(Mathf.Abs((float)trafficType.GetField("travelSpeed").GetValue(waiting))<.1f,"waiting traffic does not stop");
            Start();AddThing("Oncoming",-1);
            object arriving=((IList)Get("things"))[0];
            trafficType.GetField("distance").SetValue(arriving,1490f);
            trafficType.GetField("travelSpeed").SetValue(arriving,-35f);
            for(int step=0;step<840;step++)
            {
                Call("AdvanceTraffic",arriving,1f/120);
                float at=(float)trafficType.GetField("distance").GetValue(arriving);
                float across=(int)trafficType.GetField("lane").GetValue(arriving)+(float)trafficType.GetField("lateral").GetValue(arriving);
                Check(at<1235 || at>1314 || Mathf.Abs(across+1)>.69f,"oncoming vehicle penetrates roadwork lane");
            }
            Check((float)trafficType.GetField("distance").GetValue(arriving)<1235,"oncoming vehicle cannot complete safe merge");
            Check(Resources.Load<Texture2D>("Painted/BridgePortalV2")!=null,"supported bridge portal asset missing");
            foreach(string asset in new[]{"HomeHero","RiderGallery","RouteGallery"})
                Check(Resources.Load<Texture2D>("Interface/"+asset)!=null,"missing presentation art: "+asset);
            Set("wallet",39);Set("selectedRider",0);
            Call("Purchase",false,2);
            Check((int)Get("wallet")==39 && (int)Get("selectedRider")==0,"insufficient balance must not spend or equip");
            Call("Equip",false,2);
            Check((int)Get("selectedRider")==0,"locked rider was equipped without purchase");
            Set("wallet",100);Call("Purchase",false,2);
            Check((int)Get("wallet")==60 && (int)Get("selectedRider")==2,"purchase must spend exact price and equip");
            Call("Purchase",false,2);
            Check((int)Get("wallet")==60,"repeat purchase charged twice");
            Call("Purchase",true,2);
            Check((int)Get("wallet")==60,"rider ownership must not unlock the matching city");
            Set("wallet",100);Call("Purchase",true,2);
            Check((int)Get("wallet")==30 && (int)Get("selectedCity")==2,"city purchase failed");
            Call("Equip",false,0);Call("Equip",false,2);
            Check((int)Get("wallet")==30 && (int)Get("selectedRider")==2,"owned rider re-equip must be free");
            Call("Purchase",false,-1);Call("Purchase",true,5);
            Check((int)Get("wallet")==30,"invalid collection index spent coins");
            foreach(float depth in new[]{0f,.1f,.4f,1f})
            {
                float lanePixels=RoadProjection.HalfWidth(depth)*RoadProjection.LaneSpacing;
                Check(RoadProjection.MetresToPixels(2.55f,depth)<lanePixels,"bus exceeds lane width");
                Check(Mathf.Abs(RoadProjection.MetresToPixels(1.85f,depth)/lanePixels-1.85f/3.4f)<.0001f,"vehicle scale drifts with distance");
            }
            // Wheel rotation integrates actual travel and stops with a blocked vehicle.
            Start();AddThing("Traffic",0);
            object rolling=((IList)Get("things"))[0];
            trafficType.GetField("travelSpeed").SetValue(rolling,40f);
            trafficType.GetField("distance").SetValue(rolling,1000f);
            Call("AdvanceTraffic",rolling,.1f);
            Check((float)trafficType.GetField("rollingPhase").GetValue(rolling)>0,"traffic wheel phase did not advance");
            Start();AddThing("Traffic",0);
            object stopped=((IList)Get("things"))[0];
            trafficType.GetField("distance").SetValue(stopped,2000f);
            Call("AdvanceTraffic",stopped,.25f);
            Check((float)trafficType.GetField("rollingPhase").GetValue(stopped)==0,"stationary vehicle wheels rotate");
            foreach(string name in new[]{"TrafficMotionA","TrafficMotionB","TrafficMotionC","OncomingMotion","RiderMotionRed","RiderMotionBlue","RiderMotionGreen","RiderMotionOrange"})
            {
                Check(Resources.Load<Texture2D>("Painted/"+name)!=null,"missing motion atlas "+name);
                Check(Resources.Load<TextAsset>("Painted/"+name+"Regions")!=null,"missing motion anchors "+name);
            }
            Debug.Log("Speed Rovers gameplay checks passed: " + checks);
        }
        finally
        {
            if (game != null)
                foreach (string name in new[] { "pixel", "disc", "asphalt", "fastAsphalt", "rollingTire", "spinningRim", "glowSheet", "cloudSheet", "tireSheet" })
                    if (Get(name) is Texture2D texture) UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}


