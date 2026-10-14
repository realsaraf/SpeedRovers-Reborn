using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SpeedRovers.Reborn
{
    // Portrait checkpoint racing rebuilt with the original game's exported artwork.
    public sealed partial class SpeedRoversGame : MonoBehaviour
    {
        enum ScreenMode { Title, Market, Cities, Characters, Settings, Running, Paused, Results }
        enum ThingKind { Coin, Clock, Traffic, Barrier, Rival, Pothole, Roadworks, Oncoming }

        sealed class Thing
        {
            public float distance;
            public int lane;
            public ThingKind kind;
            public bool used;
            public float travelSpeed;
            public int variant;
            public float lateral, lean;
            public bool police, passSound;
            public bool trafficReady;
            public float cruiseSpeed;
            public float contactAfter;
            public float nextRaceDecision;
        }

        static readonly string[] Cities = { "NEW YORK", "MEXICO CITY", "SHANGHAI", "LONDON", "TOKYO" };
        static readonly string[] Riders = { "MICHELANGELO", "MONALISA", "FARADAY", "LEONARDO", "ADA" };
        static readonly string[] CityAssetNames = { "NewYork", "Mexico", "Shanghai", "London", "Tokyo" };
        static readonly string[] CityCardNames = { "Newyork", "Mexico", "Shanghai", "London", "Tokyo" };
        static readonly string[] RiderSheetNames = { "Michalangelo", "Monalisa", "Faraday", "Leonardo", "Ada" };
        static readonly string[] RiderCardNames = { "Michelangelo", "Monalisa", "Faraday", "Leonardo", "Ada" };
        static readonly Color[] CitySky = {
            new Color(.12f,.31f,.51f), new Color(.61f,.31f,.25f), new Color(.27f,.27f,.55f),
            new Color(.29f,.39f,.48f), new Color(.24f,.25f,.53f)
        };
        static readonly Color[] RiderColors = {
            new Color(1f,.52f,.21f), new Color(.96f,.32f,.49f), new Color(.30f,.82f,.94f),
            new Color(.75f,.59f,.98f), new Color(.51f,.96f,.52f)
        };

        readonly List<Thing> things = new List<Thing>();
        System.Random random;
        ScreenMode mode = ScreenMode.Title;
        Texture2D pixel;
        Texture2D disc;
        Texture2D homeBackground, menuBackground, playButton, selectButton, unlockButton;
        Texture2D coinArt, clockArt, roadblockArt, checkpointArt;
        readonly Texture2D[] skyArt = new Texture2D[5], skylineArt = new Texture2D[5], grassArt = new Texture2D[5];
        readonly Texture2D[] roadDarkArt = new Texture2D[5], roadLightArt = new Texture2D[5];
        readonly Texture2D[,,] riderFrames = new Texture2D[5, 5, 5];
        readonly Texture2D[,,] slowRiderFrames = new Texture2D[5, 5, 3];
        readonly Texture2D[,] trafficFrames = new Texture2D[5, 4];
        readonly Texture2D[,,] buildingFrames = new Texture2D[5, 2, 6];
        readonly Texture2D[] cityCardArt = new Texture2D[5], riderCardArt = new Texture2D[5];
        GUIStyle titleStyle, headingStyle, bodyStyle, smallStyle, buttonStyle;
        float distance, speed, timeLeft, lane, spawnDistance, bannerUntil, animationTime;
        int targetLane, runCoins, checkpoints, best, wallet, selectedCity, selectedRider;
        int marketCity, marketRider;
        string banner = "";
        string result = "";
        float touchStartX, touchStartY;
        float previousHorizontalAxis;
        bool touchStarted;
        // Editor regression checks exercise the real run loop without saving a
        // synthetic score or playing audio into the user's desktop session.
        bool verificationRun;
        AudioSource sound;

        void Awake()
        {
            Application.targetFrameRate = 60;
#if UNITY_ANDROID || UNITY_IOS
            Screen.orientation = ScreenOrientation.Portrait;
#endif
            pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            pixel.SetPixel(0, 0, Color.white);
            pixel.Apply();
            disc = MakeDisc();
            LoadOriginalArt();
            InitPaintedArt();
            wallet = PlayerPrefs.GetInt("sr_wallet", 0);
            best = PlayerPrefs.GetInt("sr_best", 0);
            selectedCity = Mathf.Clamp(PlayerPrefs.GetInt("sr_city", 0), 0, Cities.Length - 1);
            selectedRider = Mathf.Clamp(PlayerPrefs.GetInt("sr_rider", 0), 0, Riders.Length - 1);
            marketCity = selectedCity;
            marketRider = selectedRider;
            sound = gameObject.AddComponent<AudioSource>();
            sound.playOnAwake = false;
            sound.volume = .20f;
            InitOriginalScreens();
            var listener = new GameObject("Race Audio Listener");
            listener.transform.SetParent(transform);
            listener.AddComponent<AudioListener>();
            InitRushAudio();
            foreach (string arg in Environment.GetCommandLineArgs())
            {
                if (arg.StartsWith("-audio-review-dir=", StringComparison.Ordinal))
                    StartCoroutine(CaptureAudioReview(arg.Substring("-audio-review-dir=".Length)));
                if (arg.StartsWith("-capture-dir=", StringComparison.Ordinal))
                    StartCoroutine(CaptureScreens(arg.Substring("-capture-dir=".Length)));
                if (arg.StartsWith("-rush-review-dir=", StringComparison.Ordinal))
                    StartCoroutine(CaptureRushReview(arg.Substring("-rush-review-dir=".Length)));
                if(arg.StartsWith("-pace-review-dir=",StringComparison.Ordinal))
                    StartCoroutine(CapturePaceReview(arg.Substring("-pace-review-dir=".Length)));
                if (arg.StartsWith("-art-review-dir=", StringComparison.Ordinal))
                    StartCoroutine(CapturePaintedReview(arg.Substring("-art-review-dir=".Length)));
            }
        }

        IEnumerator CaptureScreens(string directory)
        {
            verificationRun = true;
            Directory.CreateDirectory(directory);
            yield return new WaitForSeconds(1f);
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, "title.png"));
            yield return new WaitForSeconds(1f);
            marketCity = selectedCity;
            marketRider = selectedRider;
            mode = ScreenMode.Market;
            yield return new WaitForSeconds(.5f);
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, "market.png"));
            yield return new WaitForSeconds(.4f);
            mode = ScreenMode.Cities;
            yield return new WaitForSeconds(.4f);
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, "cities.png"));
            yield return new WaitForSeconds(.4f);
            mode = ScreenMode.Characters;
            yield return new WaitForSeconds(.4f);
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, "characters.png"));
            yield return new WaitForSeconds(.4f);
            mode = ScreenMode.Settings;
            yield return new WaitForSeconds(.4f);
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, "settings.png"));
            yield return new WaitForSeconds(.4f);
            StartRun();
            yield return new WaitForSeconds(2f);
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, "race.png"));
            yield return new WaitForSeconds(.15f);
            Steer(-1);
            yield return new WaitForSeconds(.5f);
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, "steer-left.png"));
            yield return new WaitForSeconds(.3f);
            mode = ScreenMode.Paused;
            yield return new WaitForSeconds(.1f);
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, "paused.png"));
            yield return new WaitForSeconds(.5f);
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, "paused-later.png"));
            yield return new WaitForSeconds(.5f);
            mode = ScreenMode.Results;
            yield return new WaitForSeconds(.4f);
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, "results.png"));
            yield return new WaitForSeconds(.4f);
            Application.Quit(0);
        }

        static Texture2D Art(string path) { return Resources.Load<Texture2D>("Original/" + path); }

        void LoadOriginalArt()
        {
            homeBackground = Art("Menu/Backgrounds/HomeBackground");
            menuBackground = Art("Menu/Backgrounds/MenuBackground");
            playButton = Art("Menu/Buttons/PlayWideButton");
            selectButton = Art("Menu/Buttons/SelectButton");
            unlockButton = Art("Menu/Buttons/UnlockButton");
            coinArt = Art("Game/Common/Coin");
            clockArt = Art("Game/Common/time");
            roadblockArt = Art("Game/Common/roadblock");
            checkpointArt = Art("Game/Common/Checkpoint");
            for (int i = 0; i < Cities.Length; i++)
            {
                string city = "Game/Sprites/" + CityAssetNames[i] + "/";
                skyArt[i] = Art(city + "background/Sky");
                skylineArt[i] = Art(city + "background/Skyline");
                grassArt[i] = Art(city + "background/Grass");
                roadDarkArt[i] = Art(city + "roadDark");
                roadLightArt[i] = Art(city + "roadlight");
                for (int column = 0; column < 4; column++)
                    trafficFrames[i, column] = Resources.Load<Texture2D>("Slices/Traffic/" + CityAssetNames[i] + "/" + column);
                for (int side = 0; side < 2; side++)
                    for (int building = 0; building < 6; building++)
                        buildingFrames[i, side, building] = Resources.Load<Texture2D>(
                            "Slices/Buildings/" + CityAssetNames[i] + "/" + side + "/" + building);
                for (int column = 0; column < 5; column++)
                {
                    for (int row = 0; row < 5; row++)
                        riderFrames[i, column, row] = Resources.Load<Texture2D>(
                            "Slices/Riders/" + RiderSheetNames[i] + "/" + column + "_" + row);
                    for (int row = 0; row < 3; row++)
                        slowRiderFrames[i, column, row] = Resources.Load<Texture2D>(
                            "Slices/RidersSlow/" + RiderSheetNames[i] + "/" + column + "_" + row);
                }
                cityCardArt[i] = Art("Menu/Markets/" + CityCardNames[i]);
                riderCardArt[i] = Art("Menu/Markets/" + RiderCardNames[i]);
            }
        }

        static Texture2D MakeDisc()
        {
            const int size = 64;
            var image = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + .5f - size / 2f) / (size / 2f);
                    float dy = (y + .5f - size / 2f) / (size / 2f);
                    image.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01((1 - Mathf.Sqrt(dx * dx + dy * dy)) * 12)));
                }
            image.Apply();
            return image;
        }

        void StartRun()
        {
            random = new System.Random(9071 + selectedCity * 373 + DateTime.UtcNow.Millisecond);
            things.Clear();
            distance = 0;
            animationTime = 0;
            visualLean = 0;
            speed = easyMode ? 55 : 65;
            laneVelocity = hitPulse = 0;
            ResetEngine(speed);
            impactRecovery=0;
            nextWork = nextPolice = 0;
            timeLeft = 38;
            lane = targetLane = 0;
            runCoins = checkpoints = 0;
            spawnDistance = 230;
            for (int i=0;i<3;i++)
                things.Add(new Thing {distance=48+i*55,lane=i==0?-1:i==1?1:0,kind=ThingKind.Rival,variant=i+1,travelSpeed=(speed+i*3)/3.6f});
            banner = "RACE THE PACK";
            bannerUntil = Time.time + 2.4f;
            mode = ScreenMode.Running;
        }

        void Update()
        {
            UpdateMusic();
            UpdateRushAudio();
            if (captureHold) return;
            if (mode != ScreenMode.Running)
            {
                bool accept = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.JoystickButton0);
                bool back = Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton1);
                if (mode == ScreenMode.Paused && (back || accept)) mode = ScreenMode.Running;
                else if ((mode == ScreenMode.Market || mode == ScreenMode.Settings) && back) mode = ScreenMode.Title;
                else if ((mode == ScreenMode.Cities || mode == ScreenMode.Characters) && back) mode = ScreenMode.Market;
                else if ((mode == ScreenMode.Title || mode == ScreenMode.Results) && accept) StartRun();
                return;
            }

            bool keyLeft = Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A);
            bool keyRight = Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D);
            if (keyLeft) Steer(-1);
            if (keyRight) Steer(1);
            float horizontalAxis = Input.GetAxisRaw("Horizontal");
            if (!keyLeft && horizontalAxis < -.5f && previousHorizontalAxis >= -.5f) Steer(-1);
            if (!keyRight && horizontalAxis > .5f && previousHorizontalAxis <= .5f) Steer(1);
            previousHorizontalAxis = horizontalAxis;
            if (Input.GetKeyDown(KeyCode.Escape)) { mode = ScreenMode.Paused; return; }
            braking=Input.GetKey(KeyCode.DownArrow)||Input.GetKey(KeyCode.S);
            ReadSwipe();

            // Consume elapsed time in collision-safe steps. Rendering below
            // 20 fps must not turn 285 km/h into slow motion.
            AdvanceElapsedTime(Time.deltaTime);
        }

        void AdvanceElapsedTime(float remaining)
        {
            while(remaining>0 && mode==ScreenMode.Running)
            {
                float step=Mathf.Min(remaining,1f/120);
                AdvanceRun(step);
                remaining-=step;
            }
        }

        void AdvanceRun(float dt)
        {
            if (mode != ScreenMode.Running) return;
            animationTime += dt;
            visualLean = Mathf.Lerp(visualLean, Mathf.Clamp(laneVelocity * .28f, -1, 1), 1-Mathf.Exp(-dt*12));
            hitPulse = Mathf.Max(0,hitPulse-dt*2);
            float previousSpeed = speed;
            AdvanceDrive(dt);
            AdvanceEngine(dt, mode==ScreenMode.Running);
            wheelTurns += (previousSpeed+speed)*.5f/3.6f/2.05f*dt;
            distance += (previousSpeed + speed) * .5f / 3.6f * RoadProjection.TravelScale * dt;
            timeLeft -= dt;
            lane = Mathf.SmoothDamp(lane, targetLane, ref laneVelocity, .16f, 8, dt);
            if (timeLeft <= 0) { Finish("TIME UP"); return; }

            SpawnRushTraffic();

            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                AdvanceTraffic(thing,dt);
                if(!thing.passSound && thing.distance<distance+9 && (thing.kind==ThingKind.Traffic || thing.kind==ThingKind.Oncoming || thing.kind==ThingKind.Rival))
                {thing.passSound=true;PlayPass(thing);}
                bool pickup=thing.kind==ThingKind.Coin || thing.kind==ThingKind.Clock;
                if (thing.used || thing.distance > distance + (pickup?8:3.5f)) continue;
                if(thing.distance<distance-8) {if(thing.kind!=ThingKind.Rival)thing.used=true;continue;}
                if (Mathf.Abs(lane - thing.lane - thing.lateral) > (thing.kind == ThingKind.Rival ? .38f : thing.kind==ThingKind.Traffic || thing.kind==ThingKind.Oncoming ? .63f : .42f)) continue;
                if(thing.kind==ThingKind.Rival && animationTime<thing.contactAfter)continue;
                if(thing.kind!=ThingKind.Rival)thing.used = true;
                if (thing.kind == ThingKind.Coin)
                {
                    runCoins++;
                    ShowBanner("+1 COIN", .65f);
                    Beep(880, .07f);
                }
                else if (thing.kind == ThingKind.Clock)
                {
                    timeLeft += 4;
                    ShowBanner("+4 SECONDS", .9f);
                    Beep(620, .12f);
                }
                else if (thing.kind == ThingKind.Pothole || thing.kind == ThingKind.Rival)
                {
                    speed = Mathf.Max(45, speed * (thing.kind == ThingKind.Pothole ? .80f : .58f));
                    ImpactDownshift();
                    if(thing.kind==ThingKind.Rival)
                    {
                        thing.contactAfter=animationTime+1.25f;
                        thing.travelSpeed*=.78f;
                        thing.lateral+=lane>=thing.lane+thing.lateral?-.18f:.18f;
                    }
                    timeLeft -= 1.5f;
                    hitPulse = 1;
                    ShowBanner(thing.kind == ThingKind.Pothole ? "BROKEN ROAD" : "BIKE CONTACT", .6f);
                    Beep(100, .12f);
                }
                else
                {
                    PlayCrash();
                    Finish(thing.kind == ThingKind.Traffic || thing.kind == ThingKind.Oncoming ? "TRAFFIC COLLISION" : "ROADWORKS COLLISION");
                    return;
                }
            }
            things.RemoveAll(t => t.used && t.distance < distance - 25);

            if (distance >= (checkpoints + 1) * 500)
            {
                checkpoints++;
                timeLeft += 16;
                ShowBanner("+16 SECONDS", 1f);
                Beep(1200, .16f);
            }
        }

        void ReadSwipe()
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began) { touchStartX = touch.position.x; touchStartY = touch.position.y; touchStarted = true; }
                if (touch.phase == TouchPhase.Canceled) touchStarted = false;
                if (touchStarted && touch.phase == TouchPhase.Ended)
                {
                    float dx = touch.position.x - touchStartX;
                    if (Mathf.Abs(dx) > 40 && Mathf.Abs(dx) > Mathf.Abs(touch.position.y - touchStartY)) Steer(dx < 0 ? -1 : 1);
                    touchStarted = false;
                }
            }
            else if (Input.GetMouseButtonDown(0)) { touchStartX = Input.mousePosition.x; touchStarted = true; }
            else if (touchStarted && Input.GetMouseButtonUp(0))
            {
                float dx = Input.mousePosition.x - touchStartX;
                if (Mathf.Abs(dx) > 40) Steer(dx < 0 ? -1 : 1);
                touchStarted = false;
            }
        }

        void Steer(int direction) { targetLane = Mathf.Clamp(targetLane + direction, -1, 1); }
        void ShowBanner(string message, float duration) { banner = message; bannerUntil = Time.time + duration; }

        void OnApplicationPause(bool paused)
        {
            if (paused && mode == ScreenMode.Running && !captureHold) mode = ScreenMode.Paused;
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused && mode == ScreenMode.Running && !captureHold) mode = ScreenMode.Paused;
        }

        void Finish(string reason)
        {
            result = reason;
            mode = ScreenMode.Results;
            speed = 0;
            wallet += runCoins;
            best = Mathf.Max(best, Mathf.RoundToInt(distance));
            if (verificationRun) return;
            PlayerPrefs.SetInt("sr_wallet", wallet);
            PlayerPrefs.SetInt("sr_best", best);
            PlayerPrefs.Save();
        }

        void Beep(float frequency, float duration)
        {
            if (verificationRun || !soundEnabled) return;
            const int rate = 22050;
            int count = Mathf.RoundToInt(rate * duration);
            var samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float envelope = Mathf.Min(1, i / (rate * .008f)) * (1 - i / (float)count);
                samples[i] = Mathf.Sin(i * frequency * Mathf.PI * 2 / rate) * envelope * .35f;
            }
            AudioClip clip = AudioClip.Create("arcade cue", count, 1, rate, false);
            clip.SetData(samples, 0);
            sound.PlayOneShot(clip);
            Destroy(clip, duration + .2f);
        }

        void InitStyles()
        {
            if (titleStyle != null) return;
            titleStyle = Style(62, FontStyle.Bold, TextAnchor.MiddleCenter);
            headingStyle = Style(33, FontStyle.Bold, TextAnchor.MiddleCenter);
            bodyStyle = Style(23, FontStyle.Normal, TextAnchor.MiddleCenter);
            smallStyle = Style(17, FontStyle.Bold, TextAnchor.MiddleCenter);
            buttonStyle = Style(25, FontStyle.Bold, TextAnchor.MiddleCenter);
            buttonStyle.normal.textColor = Color.white;
        }

        static GUIStyle Style(int size, FontStyle weight, TextAnchor align)
        {
            var style = new GUIStyle(GUI.skin.label);
            style.fontSize = size;
            style.fontStyle = weight;
            style.alignment = align;
            style.normal.textColor = Color.white;
            style.wordWrap = true;
            return style;
        }

        void OnGUI()
        {
            if (pixel == null) return;
            InitStyles();
            GUI.matrix = Matrix4x4.identity;
            Fill(0, 0, Screen.width, Screen.height, new Color(.025f,.055f,.105f));
            Rect safe = Screen.safeArea;
            float scale = Mathf.Min(safe.width / 540f, safe.height / 960f);
            float xOffset = safe.xMin + (safe.width - 540 * scale) / 2;
            float yOffset = Screen.height - safe.yMax + (safe.height - 960 * scale) / 2;
            GUI.matrix = Matrix4x4.TRS(new Vector3(xOffset, yOffset, 0), Quaternion.identity, new Vector3(scale, scale, 1));
            GUI.BeginGroup(new Rect(0, 0, 540, 960));
            if (mode == ScreenMode.Running || mode == ScreenMode.Paused || mode == ScreenMode.Results) DrawWorld(selectedCity);
            if (mode == ScreenMode.Running) DrawHud();
            if (mode == ScreenMode.Title) DrawTitle();
            if (mode == ScreenMode.Market) DrawMarket();
            if (mode == ScreenMode.Cities) DrawSelection(true);
            if (mode == ScreenMode.Characters) DrawSelection(false);
            if (mode == ScreenMode.Settings) DrawSettings();
            if (mode == ScreenMode.Paused) DrawPause();
            if (mode == ScreenMode.Results) DrawResults();
            GUI.EndGroup();
            GUI.color = Color.white;
            GUI.matrix = Matrix4x4.identity;
        }

        void Fill(float x, float y, float w, float h, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(new Rect(x, y, w, h), pixel);
            GUI.color = Color.white;
        }

        void Disc(float x, float y, float w, float h, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(new Rect(x, y, w, h), disc);
            GUI.color = Color.white;
        }

        void Image(Texture2D art, float x, float y, float w, float h)
        {
            if (art != null) GUI.DrawTexture(new Rect(x, y, w, h), art, ScaleMode.StretchToFill, true);
        }

        void Sprite(Texture2D art, float sourceX, float sourceY, float sourceW, float sourceH,
                    float x, float y, float w, float h)
        {
            if (art == null) return;
            Rect uv = new Rect(sourceX / art.width, 1f - (sourceY + sourceH) / art.height,
                               sourceW / art.width, sourceH / art.height);
            GUI.DrawTextureWithTexCoords(new Rect(x, y, w, h), art, uv, true);
        }

        bool ArtButton(Texture2D art, float x, float y, float w, float h)
        {
            Image(art, x, y, w, h);
            return GUI.Button(new Rect(x, y, w, h), GUIContent.none, GUIStyle.none);
        }

        void Text(string value, float x, float y, float w, float h, GUIStyle style, Color color)
        {
            style.normal.textColor = color;
            GUI.Label(new Rect(x, y, w, h), value, style);
        }

        bool Button(string value, float x, float y, float w, float h, Color fill, bool enabled = true)
        {
            Fill(x, y + 5, w, h, new Color(.02f,.06f,.13f,.8f));
            Fill(x, y, w, h, enabled ? fill : new Color(.31f,.36f,.43f));
            Fill(x + 5, y + 5, 5, h - 10, Color.white);
            return enabled && GUI.Button(new Rect(x, y, w, h), value, buttonStyle);
        }

    }
}
