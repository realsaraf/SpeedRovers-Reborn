using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SpeedRovers.Reborn
{
    public sealed partial class SpeedRoversGame
    {
        [Serializable] sealed class ArtRegion { public float x, y, w, h, pivot = .5f; }
        [Serializable] sealed class PaintedRegions
        {
            public ArtRegion[] rider, scenery, traffic, worlds, foliage, water;
        }
        struct RoadSprite
        {
            public float z;
            public int type, index, side;
            public RoadSprite(float z, int type, int index, int side = 0)
            { this.z = z; this.type = type; this.index = index; this.side = side; }
        }
        readonly List<RoadSprite> roadSprites = new List<RoadSprite>(100);
        Texture2D paintedRider, paintedWorlds, paintedScenery, paintedTraffic, asphalt;
        PaintedRegions paintedRegions;
        float visualLean;
        bool captureHold;
        static readonly Color[] GroundColors = {
            new Color(.21f,.24f,.23f), new Color(.49f,.39f,.26f), new Color(.25f,.29f,.30f),
            new Color(.25f,.32f,.24f), new Color(.23f,.28f,.26f)
        };
        static readonly Color[] HazeColors = {
            new Color(.33f,.32f,.41f), new Color(.65f,.52f,.42f), new Color(.42f,.36f,.47f),
            new Color(.48f,.53f,.55f), new Color(.40f,.35f,.48f)
        };

        void InitPaintedArt()
        {
            paintedRider = Resources.Load<Texture2D>("Painted/RiderPerspective");
            paintedWorlds = Resources.Load<Texture2D>("Painted/Worlds");
            paintedScenery = Resources.Load<Texture2D>("Painted/Scenery");
            paintedTraffic = Resources.Load<Texture2D>("Painted/TrafficAnimated");
            paintedRegions = JsonUtility.FromJson<PaintedRegions>(Resources.Load<TextAsset>("Painted/Regions").text);
            // A world-space road surface: deterministic gravel grain, fine seams
            // and faint tire tracks. No meshes, 3D models or scene lighting.
            const int size = 256;
            asphalt = new Texture2D(size, size, TextureFormat.RGB24, false);
            asphalt.wrapMode = TextureWrapMode.Repeat;
            asphalt.filterMode = FilterMode.Bilinear;
            var grain = new System.Random(9147);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float v = .49f + ((float)grain.NextDouble() - .5f) * .13f;
                    v += Mathf.Sin(x * .15f + Mathf.Sin(y * .035f)) * .014f;
                    float track = Mathf.Min(Mathf.Abs(x - 86), Mathf.Abs(x - 172));
                    if (track < 7) v -= .035f * (1 - track / 7);
                    if (y == 37 && x > 140 && x < 210) v -= .06f;
                    pixels[y * size + x] = new Color(v * .91f, v * .94f, v);
                }
            asphalt.SetPixels(pixels);
            asphalt.Apply(false, true);
            InitSpriteEffects();
            InitRushArt();
        }

        void Region(Texture2D atlas, ArtRegion region, float x, float y, float w, float h, bool flip = false)
        {
            Rect uv = new Rect(region.x / atlas.width, 1 - (region.y + region.h) / atlas.height,
                               region.w / atlas.width, region.h / atlas.height);
            if (flip) { uv.x += uv.width; uv.width = -uv.width; }
            GUI.DrawTextureWithTexCoords(new Rect(x, y, w, h), atlas, uv, true);
        }

        void DrawWorld(int city)
        {
            if (Event.current.type != EventType.Repaint) return;
            UpdateEnvironment();
            float horizon = RoadProjection.ScreenY(distance, 0);
            DrawDistrictSky(city,horizon);
            DrawSurface(city);
            DrawRoadLightPools();
            DrawBridgeCables();
            DrawOpenRails();

            // All opaque sprites share one depth order, including roadside
            // objects and vehicles. This prevents distant cars painting over trees.
            roadSprites.Clear();
            QueueBridgeTowers();
            int first = Mathf.FloorToInt(distance / 12f);
            for (int slot = 1; slot <= 40; slot++)
            {
                int number = first + slot;
                float z = number * 12 - distance;
                if (z < 0 || z > RoadProjection.ViewDistance) continue;
                roadSprites.Add(new RoadSprite(z, 0, number, 0));
                roadSprites.Add(new RoadSprite(z + 6, 0, number, 1));
                float route=Mathf.Repeat(number*12,RoadRoute.Length);
                if(route>=1250 && route<1530 && number%2==0)
                    for(int bank=0;bank<6;bank++)roadSprites.Add(new RoadSprite(z,5,number,bank));
            }
            for (int i = 0; i < things.Count; i++)
            {
                float z = things[i].distance - distance;
                if (!things[i].used && z >= -6 && z <= RoadProjection.ViewDistance)
                    roadSprites.Add(new RoadSprite(z, 1, i));
            }
            for (int i = Mathf.Max(0,RoadRoute.WorkIndex(distance)-1); i <= RoadRoute.WorkIndex(distance+470)+1; i++)
                for(int n=0;n<9;n++)
                {
                    float z=RoadRoute.WorkStart(i)-35+n*8-distance;
                    if(z>0 && z<470) roadSprites.Add(new RoadSprite(z,3,i,n));
                }
            roadSprites.Sort((a, b) => b.z.CompareTo(a.z));
            foreach (RoadSprite entry in roadSprites)
            {
                float depth = RoadProjection.Depth(entry.z);
                float fog = Mathf.Pow(1 - depth, 5) * .65f;
                GUI.color = Color.Lerp(Color.white, DistrictHaze(distance+entry.z), fog*.6f);
                if (entry.type == 0) DrawPaintedRoadside(city, entry, depth);
                else if (entry.type == 1)
                {
                    Thing thing = things[entry.index];
                    Vector2 at = RoadProjection.Project(distance, entry.z, thing.lane + thing.lateral, lane);
                    DrawRushThing(thing, at, depth);
                }
                else if (entry.type == 3) DrawConstruction(entry, depth);
                else if (entry.type == 4) DrawBridgeTower(entry,depth);
                else if (entry.type == 5) DrawRiverbank(entry,depth);
                GUI.color = Color.white;
            }
            DrawRushEffects();
            DrawRushBike(270 + Mathf.Sin(animationTime*45)*hitPulse*5, 885, 190, selectedRider, visualLean, true);
        }

        void DrawSurface(int city)
        {
            const int strips = 220;
            float previousY = RoadProjection.ScreenY(distance, 0);
            float endDepth = (960 - previousY) / (885 - previousY);
            Color ground = GroundColors[city];
            Texture2D roadGrain=speed>130?fastAsphalt:asphalt;
            for (int row = 0; row < strips; row++)
            {
                float d = (row + 1f) / strips * endDepth;
                float z = RoadProjection.DistanceAtDepth(d);
                float y = RoadProjection.ScreenY(distance, d);
                float h = Mathf.Max(1, y - previousY + .6f);
                float center = RoadProjection.Center(distance, z, lane);
                float half = RoadProjection.HalfWidth(d);
                float fog = Mathf.Pow(1 - d, 4);
                bool alternate = Mathf.FloorToInt((distance + z) / 10) % 2 == 0;
                float v = (distance + z) * .042f;
                float dv = Mathf.Max(.003f, (RoadProjection.DistanceAtDepth(row / (float)strips * endDepth) - z) * .042f);
                int district=RoadRoute.District(distance+z);
                Color haze=DistrictHaze(distance+z);
                DrawEnvironmentWater(previousY,h,z,d);
                GUI.color=Color.Lerp(sceneGround,sceneHaze,fog*.5f);
                Rect land=LandStrip(distance+z,center,half,previousY,h);
                if(land.width>0)GUI.DrawTextureWithTexCoords(land,asphalt,new Rect(0,v,3,dv));
                GUI.color=Color.white;
                bool junction = RoadRoute.Junction(distance+z);
                bool urban = RoadRoute.Urban(distance+z);
                float shoulder = half * (district==1 ? .70f : urban ? .38f : .075f) + .6f;
                Color curb = Color.Lerp(urban ? new Color(.51f,.49f,.44f) : new Color(.44f,.46f,.42f), haze, fog);
                Fill(center - half - shoulder, previousY, 2 * (half + shoulder), h, curb);
                if(urban)
                {
                    Fill(center-half-shoulder,previousY,Mathf.Max(1,half*.018f),h,new Color(.58f,.55f,.48f));
                    Fill(center+half+shoulder-half*.018f,previousY,Mathf.Max(1,half*.018f),h,new Color(.58f,.55f,.48f));
                    if(Mathf.Repeat(distance+z,2.5f)<.12f)
                    {
                        Fill(center-half-shoulder,previousY,shoulder,h,new Color(.30f,.29f,.27f));
                        Fill(center+half,previousY,shoulder,h,new Color(.30f,.29f,.27f));
                    }
                }
                GUI.color=Color.Lerp(sceneRoad,sceneHaze,fog*.5f);
                GUI.DrawTextureWithTexCoords(new Rect(junction ? 0 : center - half, previousY, junction ? 540 : half * 2, h), roadGrain, new Rect(0, v, 1, dv));
                GUI.color = Color.white;
                Color edge = Color.Lerp(new Color(.89f,.87f,.79f), haze, fog);
                float lineWidth = Mathf.Max(.6f, half * .014f);
                if (!junction) Fill(center - half + half * .022f, previousY, lineWidth, h, edge);
                if (!junction) Fill(center + half - half * .022f - lineWidth, previousY, lineWidth, h, edge);
                if (!junction && Mathf.Repeat(distance + z, 7) < 3)
                {
                    Color marking=Color.Lerp(sceneMarking,haze,fog);
                    Fill(center - half / 3, previousY, lineWidth, h, marking);
                    Fill(center + half / 3, previousY, lineWidth, h, marking);
                }
                DrawRouteSurface(distance+z,center,half,previousY,h,d,junction);
                DrawDistrictSurface(district,distance+z,center,half,previousY,h,d);
                previousY = y;
            }
        }

        void DrawPaintedRoadside(int city, RoadSprite entry, float depth)
        {
            float world = distance + entry.z;
            if (Mathf.Abs(RoadRoute.JunctionOffset(world)) < 20) return;
            int district=RoadRoute.District(world);
            if(district!=0) {DrawDistrictRoadside(entry,depth,district);return;}
            int kind = (entry.index + entry.side * 2) % 7;
            if (district == 0 || district == 3)
            {
                if (entry.index%4!=0) { DrawStreetBlock(city,entry,depth,district); return; }
                kind = entry.index%8==0 ? 2 : 3;
            }
            else if(district==1)
            {
                if(entry.index%3==0) {DrawStreetBlock(city,entry,depth,district);return;}
                kind=entry.index%2==0?2:3;
            }
            else
            {
                if(entry.index%3!=0)return;
                kind=entry.index%2==0?3:2;
            }
            ArtRegion region = paintedRegions.scenery[city * 4 + kind];
            float scale = .035f + depth * .965f;
            float height = (kind < 2 ? 1200 : kind == 2 ? (district==2?440:680) : 620) * scale;
            float width = height * region.w / region.h;
            Vector2 at = RoadProjection.Project(distance, entry.z, 0, lane);
            float shoulder = RoadProjection.HalfWidth(depth) * (district==2?1.7f:kind == 3 ? 1.18f : 1.32f);
            float x = entry.side == 0 ? at.x - shoulder - width : at.x + shoulder;
            if (kind >= 2) x = at.x + (entry.side == 0 ? -shoulder : shoulder) - width / 2;
            if (x > 555 || x + width < -15) return;
            Color tint = GUI.color;
            Disc(x + width * .12f, at.y - 4 * scale, width * .86f, 16 * scale, new Color(.02f,.02f,.035f,.2f));
            GUI.color = tint;
            if (kind == 2) DrawAnimatedPlant(city, x + width / 2, at.y, height, entry.index, tint);
            else
            {
                Region(paintedScenery, region, x, at.y - height, width, height, entry.side == 1);
                DrawPropLights(city, kind, entry.index, entry.side == 1, x, at.y - height, width, height, depth);
            }
        }

        void DrawPaintedThing(ThingKind kind, Vector2 at, float depth, int variant)
        {
            float scale = .028f + depth * .972f;
            Color tint = GUI.color;
            if (kind == ThingKind.Traffic)
            {
                int phase = Mathf.FloorToInt(animationTime * 8 + variant) % 3;
                ArtRegion car = paintedRegions.traffic[phase * 4 + variant % 4];
                float width = (variant % 4 == 3 ? 275 : 220) * scale;
                ArtRegion reference = paintedRegions.traffic[variant % 4];
                float height = width * reference.h / reference.w;
                Disc(at.x - width * .51f, at.y - 7 * scale, width * 1.02f, 20 * scale, new Color(0,0,0,.4f));
                GUI.color = tint;
                Region(paintedTraffic, car, at.x - width / 2, at.y - height, width, height);
                float tailY = at.y - height * (variant % 4 < 2 ? .43f : .35f);
                float pulse = .18f + .06f * Mathf.Sin(animationTime * 7 + variant);
                Glow(at.x - width * .39f, tailY, width * .13f, new Color(1,.16f,.04f,pulse));
                Glow(at.x + width * .39f, tailY, width * .13f, new Color(1,.16f,.04f,pulse));
            }
            else if (kind == ThingKind.Barrier)
            {
                Image(roadblockArt, at.x - 75 * scale, at.y - 92 * scale, 150 * scale, 92 * scale);
            }
            else
            {
                float bounce = Mathf.Sin(animationTime * 4 + variant) * 3 * scale;
                float width = 49 * scale;
                if (kind == ThingKind.Coin) width *= .32f + .68f * Mathf.Abs(Mathf.Cos(animationTime * 4 + variant));
                Disc(at.x - 22 * scale, at.y - 3 * scale, 44 * scale, 9 * scale, new Color(0,0,0,.22f));
                GUI.color = tint;
                Image(kind == ThingKind.Coin ? Art("Game/Common/yellowcoin") : clockArt,
                      at.x - width / 2, at.y - 65 * scale + bounce, width, 49 * scale);
            }
        }

        void DrawRider(float x, float y, int rider, float size)
        {
            int lean = Mathf.RoundToInt(Mathf.Clamp(visualLean, -1, 1) * 2) + 2;
            float bump = Mathf.Sin(animationTime * (speed < 110 ? 13 : 20)) * 1.1f;
            Disc(x - 42 - visualLean * 13, y - 5, 84 + Mathf.Abs(visualLean) * 20, 13, new Color(0,0,0,.46f));
            if (rider == 0)
            {
                int phase = Mathf.FloorToInt(animationTime * (speed < 110 ? 7 : 10)) % 4;
                ArtRegion reference = paintedRegions.rider[lean];
                ArtRegion moving = paintedRegions.rider[phase * 5 + lean];
                float pixelScale = 200f / paintedRegions.rider[2].h;
                float w = reference.w * pixelScale, h = reference.h * pixelScale;
                float left = x - w * moving.pivot;
                // A complete frame preserves the silhouette, while a fixed
                // rear-tire pivot keeps every animated pose on the road.
                Region(paintedRider, moving, left, y - h, w, h);
                DrawTireMotion(x,y);
                Glow(x - visualLean * 13, y - h * .52f, 21, new Color(1,.11f,.025f,.19f));
            }
            else
            {
                bool slow = speed < 110;
                int frame = Mathf.FloorToInt(animationTime * (slow ? 6 : 10)) % (slow ? 3 : 5);
                Image(slow ? slowRiderFrames[rider, lean, frame] : riderFrames[rider, lean, frame],
                      x - 84 * size, y - 263 * size + bump, 168 * size, 263 * size);
            }
        }

        IEnumerator CapturePaintedReview(string directory)
        {
            verificationRun = captureHold = true;
            Directory.CreateDirectory(directory);
            yield return new WaitForSeconds(1);
            StartRun();
            selectedRider = 0;
            distance = 170;
            speed = 132;
            timeLeft = 31;
            bannerUntil = 0;
            animationTime = 1;
            things.Clear();
            things.Add(new Thing { distance = 197, lane = 1, kind = ThingKind.Traffic });
            things.Add(new Thing { distance = 228, lane = -1, kind = ThingKind.Traffic });
            things.Add(new Thing { distance = 290, lane = 0, kind = ThingKind.Traffic });
            for (int i = 0; i < 6; i++)
                things.Add(new Thing { distance = 192 + i * 18, lane = 0, kind = ThingKind.Coin });
            for (int city = 0; city < 5; city++)
            {
                selectedCity = city;
                yield return new WaitForSeconds(.25f);
                ScreenCapture.CaptureScreenshot(Path.Combine(directory, CityAssetNames[city] + ".png"));
                yield return new WaitForSeconds(.25f);
            }
            selectedCity = 0;
            for (int pose = -1; pose <= 1; pose += 2)
            {
                visualLean = pose;
                lane = pose * .45f;
                yield return new WaitForSeconds(.25f);
                ScreenCapture.CaptureScreenshot(Path.Combine(directory, "lean" + pose + ".png"));
                yield return new WaitForSeconds(.25f);
            }
            visualLean = lane = 0;
            Directory.CreateDirectory(Path.Combine(directory, "motion"));
            for (int frame = 0; frame < 90; frame++)
            {
                animationTime = frame / 30f;
                speed = 220;
                distance = 170 + frame * (220f / 3.6f / 30f);
                lane = Mathf.Sin(frame / 90f * Mathf.PI * 2) * .55f;
                visualLean = Mathf.Cos(frame / 90f * Mathf.PI * 2) * .9f;
                yield return new WaitForSeconds(.04f);
                ScreenCapture.CaptureScreenshot(Path.Combine(directory, "motion", frame.ToString("D3") + ".png"));
                yield return new WaitForSeconds(.04f);
            }
            mode = ScreenMode.Paused;
            yield return new WaitForSeconds(.25f);
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, "paused.png"));
            yield return new WaitForSeconds(.5f);
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, "paused-later.png"));
            yield return new WaitForSeconds(.5f);
            Debug.Log("Painted sprite review completed; no preferences were saved.");
            Application.Quit(0);
        }
    }
}
