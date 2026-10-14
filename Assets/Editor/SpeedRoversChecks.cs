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
            for (int n = 0; n < 120; n++) Call("AdvanceRun", 1f / 60f);
            float fineDistance = Number("distance");
            Start();
            for (int n = 0; n < 60; n++) Call("AdvanceRun", 1f / 30f);
            Check(Mathf.Abs(Number("distance") - fineDistance) < .12f, "frame rate changes travel distance");
            Start();
            for(int n=0;n<20;n++)Call("AdvanceElapsedTime",.1f);
            Check(Mathf.Abs(Number("distance")-fineDistance)<.2f,"10 fps rendering loses simulation time");

            Set("distance", 420f);
            Set("lane", .3f);
            Set("targetLane", 1);
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
            Check(Get("mode").ToString() == "Results", "traffic collision not detected");
            Start();
            AddThing("Barrier", 0);
            Call("AdvanceRun", .02f);
            Check(Get("mode").ToString() == "Results", "roadblock collision not detected");
            Start();
            Set("timeLeft", .001f);
            Call("AdvanceRun", .02f);
            Check(Get("result").ToString() == "TIME UP", "timer expiration does not end run");
            Start();AddThing("Pothole",0);float beforeHit=Number("speed");Call("AdvanceRun",.02f);
            Check(Get("mode").ToString()=="Running" && Number("speed")<beforeHit,"broken pavement must slow the bike without ending the race");
            Start();AddThing("Rival",0);Call("AdvanceRun",.02f);
            Check(Get("mode").ToString()=="Running" && Number("hitPulse")>0,"bike contact response missing");
            Start();AddThing("Roadworks",0);Call("AdvanceRun",.02f);
            Check(Get("mode").ToString()=="Results","construction collision missing");
            Start();AddThing("Traffic",1);Call("AdvanceRun",.02f);
            object passing=((IList)Get("things"))[0];
            Check(!(bool)passing.GetType().GetField("used").GetValue(passing),"other-lane vehicle disappears before passing camera");
            Check(RoadRoute.District(100)==0 && RoadRoute.District(700)==1 && RoadRoute.District(1500)==2 && RoadRoute.District(2300)==3 && RoadRoute.District(3020)==4 && RoadRoute.District(3710)==5 && RoadRoute.District(4480)==6 && RoadRoute.District(5000)==0,"route does not transition through all seven environments");
            Check(Resources.Load<Texture2D>("Painted/TravelSkies")!=null && Resources.Load<Texture2D>("Painted/TravelProps")!=null,"travel district art missing");
            Check(!RoadRoute.Junction(3020) && !RoadRoute.Junction(4520),"cross streets appear on coastal or alpine roads");
            Check(RoadRoute.Junction(200) && !RoadRoute.Junction(240) && !RoadRoute.Junction(1100),"crossroads are not confined to correct route segments");
            Check(!RoadRoute.Work(419) && RoadRoute.Work(420) && RoadRoute.Work(461) && !RoadRoute.Work(463),"roadwork patch and hazard boundaries disagree");
            Call("StartRun");Call("SpawnRushTraffic");
            int bikes=0,cars=0;foreach(object t in (IList)Get("things"))
            {string k=t.GetType().GetField("kind").GetValue(t).ToString();if(k=="Rival")bikes++;if(k=="Traffic")cars++;Check(k!="Barrier","legacy roadblocks still spawn");}
            Check(bikes>=2 && bikes+cars<=5,"opening traffic exceeds sparse composition or lacks rivals");
            Start();AddThing("Oncoming",1);
            object toward=((IList)Get("things"))[0];toward.GetType().GetField("travelSpeed").SetValue(toward,-25f);
            float ahead=(float)toward.GetType().GetField("distance").GetValue(toward);Call("AdvanceRun",.02f);
            Check((float)toward.GetType().GetField("distance").GetValue(toward)<ahead,"oncoming traffic moves away from player");
            Start();AddThing("Oncoming",0);Call("AdvanceRun",.02f);Check(Get("mode").ToString()=="Results","oncoming collision missing");
            Start();Set("speed",194f);Call("ResetEngine",194f);float rpmBefore=Number("engineRpm");
            Set("speed",198f);Call("AdvanceEngine",.05f,true);
            Check((int)Get("gear")==5 && Number("engineRpm")<rpmBefore,"upshift does not drop RPM");
            Set("speed",80f);for(int n=0;n<30;n++)Call("AdvanceEngine",.05f,true);
            Check((int)Get("gear")<5,"slowing down does not downshift");
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

