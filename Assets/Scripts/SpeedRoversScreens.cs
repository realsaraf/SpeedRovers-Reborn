using System.Collections.Generic;
using UnityEngine;

namespace SpeedRovers.Reborn
{
    public sealed partial class SpeedRoversGame
    {
        // Layouts and artwork follow Shared.PCL/Screens/{MainMenu,Market,Cities,
        // Characters,OptionsMenu,Pause,GameOver}Screen.cs from the supplied game.
        readonly Dictionary<string, Texture2D> interfaceArt = new Dictionary<string, Texture2D>();
        bool soundEnabled = true, musicEnabled = true, easyMode;
        AudioSource music;
        AudioClip menuMusic, gameMusic;

        Texture2D UI(string path)
        {
            if (!interfaceArt.TryGetValue(path, out Texture2D art))
            {
                art = Art("Menu/" + path);
                interfaceArt[path] = art;
            }
            return art;
        }

        void InitOriginalScreens()
        {
            soundEnabled = PlayerPrefs.GetInt("sr_sound", 1) == 1;
            musicEnabled = PlayerPrefs.GetInt("sr_music", 1) == 1;
            easyMode = PlayerPrefs.GetInt("sr_easy", 0) == 1;
            music = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            music.playOnAwake = false;
            music.volume = .3f;
            menuMusic = Resources.Load<AudioClip>("Original/Audio/MenuSound");
            gameMusic = Resources.Load<AudioClip>("Original/Audio/JungleBeat");
        }

        void UpdateMusic()
        {
            if (music == null) return;
            bool race = mode == ScreenMode.Running;
            if (!musicEnabled || (verificationRun && !audioReview) || mode == ScreenMode.Paused || mode == ScreenMode.Results)
            {
                if (music.isPlaying) music.Pause();
                return;
            }
            AudioClip wanted = race ? gameMusic : menuMusic;
            if (music.clip != wanted) { music.Stop(); music.clip = wanted; }
            if (wanted != null && !music.isPlaying) { music.UnPause(); if(!music.isPlaying)music.Play(); }
        }

        bool Icon(string name, float x, float y, float size)
        {
            return ArtButton(UI("Buttons/" + name), x, y, size, size);
        }

        void OpenMarket()
        {
            marketCity = selectedCity;
            marketRider = selectedRider;
            mode = ScreenMode.Market;
        }

        void DrawTitle()
        {
            Image(homeBackground, 0, 0, 540, 960);
            float width = 540 * .6f;
            if (ArtButton(playButton, (540 - width) / 2, 960 * .68f, width,
                          width * playButton.height / playButton.width)) StartRun();
            // The original footer's market and settings are implemented. Store
            // rating and Game Center are omitted until their services are wired.
            if (Icon("MarketsButton", 162, 862.8f, 81)) OpenMarket();
            if (Icon("SettingsButton", 297, 862.8f, 81)) mode = ScreenMode.Settings;
        }

        void DrawHud()
        {
            // Original HoodScreen: coins at top left, distance at top right.
            Text(runCoins.ToString(), 20, 14, 75, 46, headingStyle, Color.white);
            Image(Art("Game/Common/yellowcoin"), 98, 20, 38, 38);
            Text(Mathf.RoundToInt(distance) + "m", 349, 10, 167, 48, headingStyle, Color.white);
            Texture2D pause = UI("Buttons/PauseButton");
            if (ArtButton(pause, 408, 72, 108, 108f * pause.height / pause.width)) mode = ScreenMode.Paused;
            // The timer remains visible because this Unity version still ends
            // the run on expiry. It does not cover the original skyline.
            Text(Mathf.CeilToInt(timeLeft) + "s", 231, 15, 78, 38, bodyStyle,
                 timeLeft < 10 ? new Color(1f,.35f,.25f) : Color.white);
            Fill(18,896,156,48,new Color(.035f,.05f,.07f,.80f));
            Text(Mathf.RoundToInt(speed)+" km/h",20,900,152,36,bodyStyle,Color.white);
            Text("GEAR "+gear,20,876,152,20,smallStyle,new Color(.85f,.89f,.94f));
            int place=1,racers=1;
            foreach(Thing opponent in things)
                if(opponent.kind==ThingKind.Rival && !opponent.used) {racers++;if(opponent.distance>distance)place++;}
            Text("PLACE "+place+" / "+racers,295,876,228,24,smallStyle,new Color(.85f,.89f,.94f));
            Text(RoadRoute.Name(distance),295,908,228,28,smallStyle,new Color(.85f,.89f,.94f));
            if (Time.time < bannerUntil && distance > 70)
                Text(banner, 65, 138, 410, 42, bodyStyle, new Color(1f,.85f,.36f));
        }

        void DrawMarket()
        {
            Image(UI("Backgrounds/MenuBackground1"), 0, 0, 540, 960);
            if (Icon("PreviousButton", 16.2f, 16.2f, 81)) mode = ScreenMode.Title;
            Texture2D title = UI("Markets/MarketsTitle");
            Image(title, 260, 10, 270, 270f * title.height / title.width);
            Texture2D characters = UI("Markets/CharacterStore"), cities = UI("Markets/CitiesStore");
            float height = 432f * characters.height / characters.width;
            if (ArtButton(characters, 54, 508.8f - height, 432, height)) mode = ScreenMode.Characters;
            if (ArtButton(cities, 54, 518.4f, 432, height)) mode = ScreenMode.Cities;
        }

        void DrawSelection(bool cities)
        {
            Image(UI("Backgrounds/MenuBackground1"), 0, 0, 540, 960);
            if (Icon("PreviousButton", 16.2f, 16.2f, 81)) mode = ScreenMode.Market;
            int current = cities ? marketCity : marketRider;
            Texture2D[] cards = cities ? cityCardArt : riderCardArt;
            const float cardWidth = 324;
            float cardHeight = cardWidth * cards[current].height / cards[current].width;
            float titleHeight = 324f * UI(cities ? "Markets/CitiesTitle" : "Markets/CharactersTitle").height
                / UI(cities ? "Markets/CitiesTitle" : "Markets/CharactersTitle").width;
            float cardY = titleHeight + 19.2f;
            Image(cards[(current + 4) % 5], 108 - 351, cardY, cardWidth, cardHeight);
            Image(cards[current], 108, cardY, cardWidth, cardHeight);
            Image(cards[(current + 1) % 5], 108 + 351, cardY, cardWidth, cardHeight);
            float controlsY = (cardY + cardHeight + 960) / 2 - 54;
            if (Icon("PrevArrow", 27, controlsY, 108)) current = (current + 4) % 5;
            if (Icon("NextArrow", 405, controlsY, 108)) current = (current + 1) % 5;
            if (cities) marketCity = current; else marketRider = current;

            int selected = cities ? selectedCity : selectedRider;
            string key = cities ? "sr_city_owned_" : "sr_rider_owned_";
            bool owned = current == 0 || PlayerPrefs.GetInt(key + current, 0) == 1;
            int cost = current * (cities ? 35 : 20);
            Texture2D action = owned ? (selected == current ? UI("Buttons/ActiveButton") : selectButton) : unlockButton;
            if (ArtButton(action, 162, controlsY, 216, 108))
            {
                if (owned || wallet >= cost)
                {
                    if (!owned) { wallet -= cost; PlayerPrefs.SetInt(key + current, 1); PlayerPrefs.SetInt("sr_wallet", wallet); }
                    if (cities) { selectedCity = current; PlayerPrefs.SetInt("sr_city", current); }
                    else { selectedRider = current; PlayerPrefs.SetInt("sr_rider", current); }
                    PlayerPrefs.Save();
                }
            }
            if (!owned) Text(cost + " coins", 162, controlsY + 112, 216, 32, bodyStyle, Color.white);
            Text(wallet.ToString(), 365, 18, 91, 40, bodyStyle, Color.white);
            Image(Art("Game/Common/yellowcoin"), 465, 16, 43, 43);
        }

        void DrawSettings()
        {
            Image(UI("Backgrounds/MenuBackground1"), 0, 0, 540, 960);
            if (Icon("PreviousButton", 16.2f, 16.2f, 81)) mode = ScreenMode.Title;
            Texture2D title = UI("Markets/SettingsTitle");
            Image(title, 260, 10, 270, 270f * title.height / title.width);
            if (Icon(soundEnabled ? "SoundOnButton" : "SoundOffButton", 189, 204.6f, 162))
            { soundEnabled = !soundEnabled; PlayerPrefs.SetInt("sr_sound", soundEnabled ? 1 : 0); PlayerPrefs.Save(); }
            if (Icon(musicEnabled ? "MusicOnButton" : "MusicOffButton", 189, 399, 162))
            { musicEnabled = !musicEnabled; PlayerPrefs.SetInt("sr_music", musicEnabled ? 1 : 0); PlayerPrefs.Save(); }
            if (Icon(easyMode ? "EasyModeButton" : "NormalModeButton", 189, 593.4f, 162))
            { easyMode = !easyMode; PlayerPrefs.SetInt("sr_easy", easyMode ? 1 : 0); PlayerPrefs.Save(); }
        }

        void DrawPause()
        {
            Image(UI("Backgrounds/GamePausedLayer"), .4f, 0, 539.2f, 960);
            if (Icon("PlayButton", 162, 480, 216)) mode = ScreenMode.Running;
        }

        void DrawResults()
        {
            Image(UI("Backgrounds/GameOverLayer"), .4f, 0, 539.2f, 960);
            Text("Coins : " + runCoins, 70, 315, 400, 50, headingStyle, Color.white);
            Text("Distance : " + Mathf.RoundToInt(distance) + "m", 70, 379, 400, 50, headingStyle, Color.white);
            Text("Personal Best : " + best + "m", 70, 460, 400, 42, bodyStyle, new Color(1f,.77f,.38f));
            if (Icon("RestartButton", 108, 672, 135)) StartRun();
            if (Icon("HomeButton", 297, 672, 135)) mode = ScreenMode.Title;
        }
    }
}
