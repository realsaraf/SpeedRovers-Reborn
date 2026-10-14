# Speed Rovers Reborn

The standalone Unity source is in this repository. iOS 2.0.0 targets the existing Speed Rovers App Store Connect record (916983500); the automatic build/upload procedure is in [TESTFLIGHT.md](TESTFLIGHT.md). Exported iOS source is stored through Git LFS. Desktop packages and historical validation files described below are local artifacts and are not included in Git.

Latest Windows build: **0.7.0 Corrections/Rush**, `Builds/WindowsRush/Speed Rovers Reborn.exe`. Faster route travel, traffic that yields to roadworks, integrated tire animation, corrected vehicle scale, continuous bridge supports, removed checkpoint gantries, and smoother surface transitions. See `CORRECTIONS-0.7.md` for evidence and limits. Hold **S / Down** to brake. See `PAINTED-GRAPHICS.md` for scope and provenance. Earlier packages below are retained for comparison.

**Earlier graphics preview:** version 0.4.0 and `Builds/WindowsPainted/` use painted sprites, new scenery for all five cities, a new red-bike rider animation, and revised 2D road perspective. See `PAINTED-GRAPHICS.md`. The previous Windows 0.3.0 build remains in `Builds/Windows/`; other platform packages remain 0.2.0.

A desktop-first Unity 6 revival of the original portrait motorcycle checkpoint racer. The MonoGame/Xamarin source remains in `../src/`. The Unity project uses art decoded from that game's XNB files: its title screen, city backgrounds, road textures, rider animations, traffic, pickups, roadside buildings, and market cards.

## Desktop builds

The `Builds/` folder contains these local packages:

| Platform | Package | Status |
| --- | --- | --- |
| Windows | `Speed-Rovers-Reborn-Windows.zip` | Built, launched, and captured from the running player on Windows |
| macOS | `Speed-Rovers-Reborn-macOS.zip` | Intel + Apple Silicon universal binary verified; requires a macOS run check |
| Linux | `Speed-Rovers-Reborn-Linux.zip` | Player rendered through Ubuntu 22.04/WSLg; native Linux desktop testing remains |
| Android | `Android/Speed Rovers Reborn.apk` | ARM64 APK built and development-signed; emulator installation passed, runtime graphics validation remains |
| iOS / iPadOS | `Speed-Rovers-Reborn-iOS-Xcode.zip` | Xcode project exported; requires Xcode compilation, signing, and an Apple device run |
| WebGL | `WebGL/index.html` | Built and checked in a browser, including keyboard start/pause and a narrow viewport |

Extract the appropriate package and launch `Speed Rovers Reborn.exe` (Windows), `Speed Rovers Reborn.app` (macOS), or `Speed Rovers Reborn.x86_64` (Linux). The archives preserve executable permissions on macOS and Linux. The first release is portrait at a default 540 × 960 window; the window is resizable.

The Linux package also includes `launch.sh`. On WSLg hosts affected by the NVIDIA graphics shutdown crash, run `./launch.sh --software-rendering`. The tested software path renders the game and exits cleanly; see `PLATFORM-STATUS.md` for the driver trace and remaining native Linux check.

Windows 0.3.0 restores the original Market with separate Characters and Cities screens, original menu buttons and pause/results artwork. Gameplay has no on-screen steering arrows. Current Windows captures are in `Preview/title-windows.png`, `Preview/market-windows.png`, and `Preview/race-windows.png`. See `FIDELITY.md` for the source comparison and remaining differences.

The app identifier is `com.philino.speedrovers.reborn`. The original Windows package is version `0.3.0`; the separate painted preview is `0.4.0`, and the current Windows Rush source/build is `0.7.0`; other platform packages remain `0.2.0`. Android uses version code 2, targets API 36 with minimum API 26 and uses Unity's Android Debug signing certificate. The iOS project targets iOS 15+, iPhone and iPad, with no signing team configured. These are development packages; signing and device validation for distribution remain outstanding.

## Play

- Press **A/D** or **Left/Right**, or swipe, to move between three lanes.
- The rider remains centered while the camera follows the lane change, matching the original presentation.
- Avoid traffic and roadblocks. Collect coins and clock pickups.
- Reach each 500 m checkpoint for 16 extra seconds.
- Spend coins earned in runs on riders and cities through Market → Characters / Cities. Progress is local to each installation.
- Press **Esc** to pause or resume.
- Press **Enter** or controller button A to start from the title/results screen. Escape/controller B returns from menu screens.

This is a playable prototype with local progression, original menu/game music and procedural effect cues. Original ads, purchases and online services have not been reimplemented. Road generation and gameplay tuning still differ from the supplied game.

## Unity source and rebuilding

Open this folder with Unity **6000.6.0f1** and load `Assets/Scenes/SpeedRovers.unity`. `Assets/Scripts/SpeedRoversGame.cs` contains the playable prototype. Build from the command line with one of these editor methods:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath (Get-Location).Path -executeMethod SpeedRoversChecks.Run -logFile gameplay-checks.log
& 'C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath (Get-Location).Path -executeMethod SpeedRoversBuild.BuildWindows -logFile build-windows.log
& 'C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath (Get-Location).Path -executeMethod SpeedRoversBuild.BuildMac -logFile build-mac.log
& 'C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath (Get-Location).Path -executeMethod SpeedRoversBuild.BuildLinux -logFile build-linux.log
python Tools/package_desktop.py
```

The same editor command also accepts `SpeedRoversBuild.BuildAndroid`, `SpeedRoversBuild.BuildIOS`, `SpeedRoversBuild.BuildWebGL`, or `SpeedRoversBuild.BuildAllPlatforms`. Run `python Tools/package_ios.py` after exporting iOS. Serve `Builds/WebGL` with an HTTP server for browser testing.

`Tools/xnb_to_png.py` decodes the original uncompressed XNB textures to `Assets/Resources/Original/`. `Tools/slice_sprites.py` extracts rider, traffic, and roadside frames to `Assets/Resources/Slices/`. The decoded art is included in the Unity source, so these tools are only needed when refreshing from the original files.

All six version 0.2.0 Unity build targets completed in `build-fidelity-platforms.log`. Windows was rendered and captured from its native player. Linux rendered through WSLg, with identical paused captures and clean shutdown under software rendering; the GPU driver cleanup crash is documented separately. WebGL startup, steering, pause and narrow layout passed on 0.2.0. The Android APK passed signature and 16 KB ZIP alignment checks; version 0.1.0 installed in a clean API 35 emulator, but the emulator's ARM translation/graphics combination did not initialize the Unity renderer. Unity 6000.6 rejects Android x86-64 builds. Physical Android testing, Apple runtime checks and a native Linux desktop check remain necessary; see `PLATFORM-STATUS.md` for the verification record.

