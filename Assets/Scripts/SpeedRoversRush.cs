using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace SpeedRovers.Reborn
{
    public sealed partial class SpeedRoversGame
    {
        [Serializable] sealed class RushRegions { public ArtRegion[] bikes, traffic, buildings, hazards, hero, oncoming; }
        Texture2D rushOncoming, rushHero, rushBikes, rushTraffic, rushBuildings, rushHazards;
        RushRegions rushRegions;
        [Serializable] sealed class RaceSpriteRegions { public ArtRegion[] frames; }
        Texture2D rivalNinePoses;
        RaceSpriteRegions rivalPoseRegions;
        float laneVelocity, hitPulse;
        int nextWork, nextPolice;


        void InitRushArt()
        {
            rushOncoming=Resources.Load<Texture2D>("Painted/RushOncoming");
            rushHero=Resources.Load<Texture2D>("Painted/RushHero");
            rushBikes=Resources.Load<Texture2D>("Painted/RushBikes");
            rushTraffic=Resources.Load<Texture2D>("Painted/RushTraffic");
            rushBuildings=Resources.Load<Texture2D>("Painted/RushBuildings");
            rushHazards=Resources.Load<Texture2D>("Painted/RushHazards");
            rushRegions=JsonUtility.FromJson<RushRegions>(Resources.Load<TextAsset>("Painted/RushRegions").text);
            InitGrounding();
            InitDistrictArt();
            InitAlignedTraffic();
            rivalNinePoses=Resources.Load<Texture2D>("Painted/RivalNinePoses");
            InitDenseRiders();
            rivalPoseRegions=JsonUtility.FromJson<RaceSpriteRegions>(Resources.Load<TextAsset>("Painted/RivalNinePosesRegions").text);
        }

        void SpawnRushTraffic()
        {
            while(spawnDistance < distance + RoadProjection.ViewDistance)
            {
                int district=RoadRoute.District(spawnDistance);
                int openLane=random.Next(-1,2);
                int count=1;
                for(int n=0;n<count;n++)
                {
                    int spawnLane=(openLane+1+n+3)%3-1;
                    double roll=random.NextDouble();
                    // Competitors are persistent racers seeded at the start.
                    ThingKind kind=roll<.36?ThingKind.Traffic:ThingKind.Coin;
                    int closure=RoadRoute.WorkIndex(spawnDistance+35);
                    if(closure>=0 && spawnDistance<=RoadRoute.WorkStart(closure)+44 && spawnLane==RoadRoute.WorkLane(closure)) continue;
                    int variant=random.Next(0,60);
                    if(spawnLane==-1 && kind==ThingKind.Traffic && variant%3!=0) kind=ThingKind.Oncoming;
                    bool vehicle=kind==ThingKind.Rival || kind==ThingKind.Traffic || kind==ThingKind.Oncoming;
                    int visibleVehicles=0;bool crowded=false;
                    foreach(Thing existing in things)
                    {
                        if(existing.used || existing.distance<distance-8 || existing.distance>distance+470)continue;
                        if(existing.kind!=ThingKind.Rival && existing.kind!=ThingKind.Traffic && existing.kind!=ThingKind.Oncoming)continue;
                        visibleVehicles++;
                        if(existing.lane==spawnLane && Mathf.Abs(existing.distance-spawnDistance)<85)crowded=true;
                    }
                    if(vehicle && (visibleVehicles>=(RoadRoute.Urban(spawnDistance)?4:3) || crowded))continue;
                    things.Add(new Thing {distance=spawnDistance+n*8,lane=spawnLane,kind=kind,variant=variant,police=kind==ThingKind.Oncoming && (variant%8==4 || variant%8==5),
                        travelSpeed=kind==ThingKind.Oncoming?-20-variant%17:kind==ThingKind.Rival?40+variant%19:kind==ThingKind.Traffic?14+variant%16:0});
                }
                spawnDistance += RoadRoute.Urban(spawnDistance)?125:165;
            }
            while(700+nextPolice*1600<distance+470)
            {
                things.Add(new Thing{distance=700+nextPolice*1600,lane=-1,kind=ThingKind.Oncoming,variant=nextPolice%2==0?4:5,police=true,travelSpeed=-28});
                nextPolice++;
            }
            while(RoadRoute.WorkStart(nextWork)<distance+470)
            {
                float start=RoadRoute.WorkStart(nextWork);
                int workLane=RoadRoute.WorkLane(nextWork);
                things.Add(new Thing{distance=start+5,lane=workLane,kind=ThingKind.Roadworks,variant=nextWork});
                things.Add(new Thing{distance=start+27,lane=workLane,kind=ThingKind.Roadworks,variant=nextWork});
                things.Add(new Thing{distance=start+100,lane=-workLane,kind=ThingKind.Pothole,variant=nextWork});
                nextWork++;
            }
        }

        void DrawRouteSurface(float world,float center,float half,float y,float h,float depth,bool junction)
        {
            float junctionOffset=RoadRoute.JunctionOffset(world);
            if(Mathf.Abs(junctionOffset)>9 && Mathf.Abs(junctionOffset)<11.5f)
            {
                // Zebra crossings are painted onto the road plane, so all
                // stripes accelerate and widen with the same perspective.
                for(int stripe=-5;stripe<=5;stripe++)
                    Fill(center+stripe*half*.175f-half*.055f,y,half*.11f,h,new Color(.77f,.76f,.70f));
            }
            if(junction && Mathf.Abs(junctionOffset)<.23f)
            {
                Fill(0,y,Mathf.Max(0,center-half),h,new Color(.78f,.66f,.36f));
                Fill(center+half,y,Mathf.Max(0,540-center-half),h,new Color(.78f,.66f,.36f));
            }
            if(RoadRoute.Work(world))
            {
                float x=center+RoadRoute.WorkLane(RoadRoute.WorkIndex(world))*half*RoadProjection.LaneSpacing;
                Fill(x-half*.27f,y,half*.54f,h,new Color(.17f,.16f,.145f));
                // Milled pavement, exposed aggregate and a ragged repair edge.
                float rough=Mathf.PerlinNoise(world*2.7f,0)*half*.055f;
                Fill(x-half*.27f-rough,y,rough,h,new Color(.38f,.34f,.27f));
                if(Mathf.Repeat(world,.8f)<.16f)
                    Fill(x-half*.23f,y,half*.46f,h,new Color(.23f,.21f,.185f));
            }
            if(RoadRoute.District(world)==2)
            {
                // Continuous guardrails provide a near-camera speed reference.
                Fill(center-half*1.18f,y,Mathf.Max(1,half*.027f),h,new Color(.51f,.57f,.6f));
                Fill(center+half*1.16f,y,Mathf.Max(1,half*.027f),h,new Color(.51f,.57f,.6f));
            }
        }

        void DrawStreetBlock(int city,RoadSprite entry,float depth,int district)
        {
            int variant=district==0?(entry.index+city+entry.side*3)%4:4+(entry.index+city+entry.side)%4;
            ArtRegion region=rushRegions.buildings[variant];
            Texture2D buildingAtlas=rushBuildings;
            if(district==0)
            {
                if(cityBuildings==null) { cityBuildings=Resources.Load<Texture2D>("Painted/CityBuildingsV2"); cityBuildingRegions=JsonUtility.FromJson<RaceSpriteRegions>(Resources.Load<TextAsset>("Painted/CityBuildingsV2Regions").text); }
                region=cityBuildingRegions.frames[variant%4]; buildingAtlas=cityBuildings;
            }
            float scale=.022f+depth*.978f;
            float height=RoadProjection.MetresToPixels(variant<4?20:8.7f,depth);
            float width=height*region.w/region.h;
            Vector2 at=RoadProjection.Project(distance,entry.z,0,lane);
            float edge=RoadProjection.HalfWidth(depth)*1.24f;
            float x=entry.side==0?at.x-edge-width:at.x+edge;
            if(x>550 || x+width< -10) return;
            Color tint=GUI.color;
            Region(buildingAtlas,region,x,at.y-height,width,height,entry.side==1);
            // Illuminated storefronts pulse very slowly, independent of travel.
            Glow(x+width*.52f,at.y-height*.12f,width*.25f,
                new Color(1,.66f,.30f,.10f+.03f*Mathf.Sin(animationTime*.9f+entry.index)));
            GUI.color=tint;
        }

        void DrawConstruction(RoadSprite entry,float depth)
        {
            float workLane=RoadRoute.WorkLane(entry.index);
            float laneEdge=workLane*.52f;
            Vector2 at=RoadProjection.Project(distance,entry.z,laneEdge,lane);
            float scale=.025f+.975f*depth;
            Color tint=GUI.color;
            ArtRegion cone=rushRegions.hazards[0];
            float h=64*scale,w=h*cone.w/cone.h;
            Region(rushHazards,cone,at.x-w/2,at.y-h,w,h);
            if(entry.side==7)
            {
                at=RoadProjection.Project(distance,entry.z,workLane*1.95f,lane);
                ArtRegion excavator=rushRegions.hazards[2];
                h=300*scale;w=h*excavator.w/excavator.h;
                Region(rushHazards,excavator,at.x-w/2,at.y-h,w,h);
                Glow(at.x,at.y-h*.8f,24*scale,new Color(1,.5f,.08f,.5f+.4f*Mathf.Sin(animationTime*9)));
            }
            GUI.color=tint;
        }

        void DrawRushThing(Thing thing,Vector2 at,float depth)
        {
            float scale=RoadProjection.ObjectScale(depth);
            Color tint=GUI.color;
            if(thing.kind==ThingKind.Rival)
            {
                DrawRushBike(at.x,at.y,RoadProjection.MetresToPixels(2.05f,depth),thing.variant%5,thing.lean,false,Mathf.Abs(thing.travelSpeed)*3.6f,thing.rollingPhase);
            }
            else if(thing.kind==ThingKind.Traffic || thing.kind==ThingKind.Oncoming)
            {
                bool front=thing.kind==ThingKind.Oncoming;
                int variant=thing.variant%(front?8:12);
                DrawAlignedVehicle(thing,at,depth,front,variant,tint);
            }
            else if(thing.kind==ThingKind.Roadworks || thing.kind==ThingKind.Pothole || thing.kind==ThingKind.Barrier)
            {
                int index=thing.kind==ThingKind.Pothole?3:thing.variant%2==0?1:5;
                ArtRegion region=rushRegions.hazards[index];
                float width=(index==3?155:172)*scale;
                float height=width*region.h/region.w;
                Region(rushHazards,region,at.x-width/2,at.y-height,width,height);
                if(index==1) Glow(at.x,at.y-height,27*scale,new Color(1,.5f,.04f,.5f+.4f*Mathf.Sin(animationTime*10)));
            }
            else DrawPaintedThing(thing.kind,at,depth,thing.variant);
            GUI.color=tint;
        }

        // Sprite strips provide a continuous lean between directional poses.
        // Unlike rotating the whole scene, this cannot distort road geometry.
        void SkewSprite(Texture2D atlas,ArtRegion region,float x,float y,float w,float h,float lean)
        {
            if(Mathf.Abs(lean)<.35f) {Region(atlas,region,x,y,w,h);return;}
            const int strips=8;
            for(int row=0;row<strips;row++)
            {
                float t=row/(float)strips;
                Rect uv=new Rect(region.x/atlas.width,1-(region.y+region.h*(t+1f/strips))/atlas.height,
                    region.w/atlas.width,region.h/strips/atlas.height);
                GUI.DrawTextureWithTexCoords(new Rect(x+lean*(1-t)*(1-t),y+h*t,w,h/strips+.5f),atlas,uv,true);
            }
        }

        void DrawRushBike(float x,float y,float height,int variant,float lean,bool player,float rivalVelocity=160,float rollingPhase=0)
        {
            lean=Mathf.Clamp(lean,-1,1);
            int identity=player && variant==0?0:1+Mathf.Abs(variant-1)%3;
            float velocity=player?speed:rivalVelocity;
            // Red/blue have fifteen sequential banks. Green/orange have five
            // banks in three distinct riding postures, selected by speed.
            int frame=identity<2?Mathf.Clamp(Mathf.RoundToInt(lean*7)+7,0,14):
                Mathf.Clamp(Mathf.FloorToInt(velocity/150),0,2)*5+Mathf.Clamp(Mathf.RoundToInt(lean*2)+2,0,4);
            if(identity==0 && frame==14)frame=13; // Rejected mirrored exhaust in generated extreme pose.
            ArtRegion region=denseRiderRegions[identity].frames[frame];
            Texture2D atlas=denseRiders[identity];
            float width=height*region.w/region.h;
            float scale=height/165;
            Color tint=GUI.color;
            SoftGroundShadow(x-lean*6*scale,y-3*scale,80*scale,22*scale,.5f);
            Disc(x-17*scale,y-3*scale,34*scale,5*scale,new Color(.01f,.009f,.008f,.78f));
            GUI.color=tint;
            float bounce=player?hitPulse*2:0;
            DrawBikeBody(atlas,region,x,y,width,height+bounce,lean,player?wheelTurns:rollingPhase,player?speed:rivalVelocity);
            Glow(x-lean*8*scale,y-height*.35f,13*scale,new Color(1,.07f,.025f,.33f));
            GUI.color=tint;
        }

        IEnumerator CaptureRushReview(string directory)
        {
            verificationRun=captureHold=true;
            Directory.CreateDirectory(directory);
            yield return new WaitForSeconds(1);
            StartRun();selectedCity=selectedRider=0;bannerUntil=0;
            float[] scenes={100,350,390,580,650,687,735,748,756,759,761,780,980,1188,1250,1320,1460,2280,3020,3710,4480};
            foreach(float location in scenes)
            {
                distance=location*RoadRoute.PacingScale;animationTime=location/75;speed=427.5f;ResetEngine(speed);lane=visualLean=0;
                things.Clear();spawnDistance=distance+25;nextWork=Mathf.Max(0,RoadRoute.WorkIndex(distance));
                SpawnRushTraffic();
                things.Add(new Thing{kind=ThingKind.Rival,distance=distance+45,lane=-1,variant=1});
                yield return new WaitForSeconds(.15f);
                ScreenCapture.CaptureScreenshot(Path.Combine(directory,"route-"+location+".png"));
                yield return new WaitForSeconds(.15f);
            }
            // Exercise every newly authored traffic identity at visible road depth.
            for(int vehicle=0;vehicle<18;vehicle++)
            {
                distance=1200;lane=visualLean=0;speed=240;things.Clear();
                things.Add(new Thing {kind=vehicle<12?ThingKind.Traffic:ThingKind.Oncoming,
                    variant=vehicle<12?vehicle:vehicle-12,distance=distance+34,lane=0,travelSpeed=vehicle<12?40:-35,rollingPhase=.3f});
                yield return new WaitForSeconds(.08f);
                ScreenCapture.CaptureScreenshot(Path.Combine(directory,"vehicle-"+vehicle+".png"));
                yield return new WaitForSeconds(.08f);
            }
            for(int rider=0;rider<4;rider++)
            {
                selectedRider=rider;
                for(int pose=0;pose<3;pose++)
                {
                    visualLean=pose-1;things.Clear();
                    things.Add(new Thing {kind=ThingKind.Rival,variant=rider,distance=distance+25,lane=0,lean=pose-1,travelSpeed=65,rollingPhase=.3f});
                    yield return new WaitForSeconds(.08f);
                    ScreenCapture.CaptureScreenshot(Path.Combine(directory,"rider-"+rider+"-"+pose+".png"));
                    yield return new WaitForSeconds(.08f);
                }
            }
            selectedRider=0;
            Directory.CreateDirectory(Path.Combine(directory,"motion"));
            StartRun();random=new System.Random(904);bannerUntil=0;
            // The production simulation, 30 fps. Automated lane selection is
            // confined to this review mode; collisions and traffic remain live.
            for(int frame=0;frame<540;frame++)
            {
                float bestCost=float.MaxValue;
                int bestLane=targetLane;
                for(int candidate=-1;candidate<=1;candidate++)
                {
                    float cost=.08f*Mathf.Abs(candidate-targetLane);
                    int work=Mathf.Max(0,RoadRoute.WorkIndex(distance+35));
                    for(int closure=work;closure<=work+1;closure++)
                    {
                        float ahead=RoadRoute.WorkStart(closure)-distance;
                        if(ahead> -50 && ahead<260 && candidate==RoadRoute.WorkLane(closure))cost+=1000;
                    }
                    float predictedLane=lane, velocity=laneVelocity;
                    for(int step=1;step<=20;step++)
                    {
                        float t=step*.08f;
                        predictedLane=Mathf.SmoothDamp(predictedLane,candidate,ref velocity,.16f,8,.08f);
                        foreach(Thing obstacle in things)
                        {
                            if(obstacle.used || obstacle.kind==ThingKind.Coin || obstacle.kind==ThingKind.Clock)continue;
                            float z=obstacle.distance-distance+(obstacle.travelSpeed-speed/3.6f)*RoadProjection.TravelScale*t;
                            float separation=Mathf.Abs(predictedLane-obstacle.lane-obstacle.lateral);
                            if(Mathf.Abs(z)<14 && separation<.85f) cost+=(.85f-separation)*25/(t+.1f);
                        }
                    }
                    if(cost<bestCost) {bestCost=cost;bestLane=candidate;}
                }
                targetLane=bestLane;
                AdvanceElapsedTime(1f/30);
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(directory,"motion",frame.ToString("D3")+".png"));
                yield return null;
                if(mode!=ScreenMode.Running) {Debug.LogError("Rush review collision at frame "+frame+" result="+result+" lane="+lane);break;}
            }
            mode=ScreenMode.Paused;
            yield return new WaitForSeconds(.2f);
            ScreenCapture.CaptureScreenshot(Path.Combine(directory,"paused.png"));
            yield return new WaitForSeconds(.5f);
            ScreenCapture.CaptureScreenshot(Path.Combine(directory,"paused-later.png"));
            yield return new WaitForSeconds(.3f);
            Debug.Log("Rush review completed at "+distance+" metres. No preferences saved.");
            Application.Quit(0);
        }
    }
}

