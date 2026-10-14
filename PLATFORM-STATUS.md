# Platform verification — 2026-10-13

Scope: a Unity project for the Speed Rovers revival, with desktop as the first release. The project is still a playable prototype; full feature parity is not claimed. Current Windows Rush/source are 0.7.0. Earlier Windows packages remain for comparison; other packaged targets remain 0.2.0.

## Windows 0.7.0 screenshot corrections

Native Windows build completed successfully. The gameplay suite passed 7,103 assertions. Final framebuffer captures cover 17 route positions. The scripted 496-frame recording ends in a traffic collision at 2,177 m; it is not a clean-race test. Pause frames are identical. Real-time pace data, package hashes and input-test status are in `Preview/Rush/verification.json`. See `CORRECTIONS-0.7.md`. macOS/Linux/mobile/WebGL packages were not rebuilt.

## Earlier Windows 0.6.0 district and motion revision

Seven scenery sections on a 5 km loop: downtown, bridge, outskirts, city day, Italian coast, Tokyo night, Alps. Sparse traffic, sixteen-phase wheel surfaces, fixed tire-contact suspension, district lighting and surface treatments. All graphics remain projected 2D sprites/strips.

Unity build succeeded. Gameplay checks passed 5,537 assertions (not distinct scenarios). The final native review captured twelve route viewpoints and 540 consecutive production-simulation frames at a scripted 30 fps (18 seconds), completing at 1,434.459 m without an exception or collision. Paused frames match. This is not a frame-rate benchmark. Evidence: C:\SpeedRoversCapture\seven-district-checks.log, district-final-build.log, district-final-player.log and DistrictFinal\; Preview/Rush/verification.json records the package hash.

The normal executable was launched from C:\DRIVE\src\Speed Rovers Reborn\Play. Native window captures showed live play at 918 m and 2,594 m. A right-arrow input was delivered; concurrent user input was detected before the pause check, so automation stopped. A complete interactive control test is not claimed. Scripted gameplay/pause checks passed. Visual parity with the supplied targets remains unestablished; terrain depth, billboard repetition and lighting remain limitations. Mac/Linux were not rebuilt for this graphics pass.

The six-gear engine, coast, wind, passing and police layers are retained from the previous audio pass. Its actual Windows listener recording is Preview/Rush/race-mix.wav (48 kHz stereo, 20.117 seconds, zero clipped samples, silent paused ending). That recording is separate from the silent 18-second visual capture.

## Windows 0.3.0 UI correction

The original Market, Characters, Cities, Settings, pause and results presentation replaces the invented Garage UI. Gameplay steering arrows were removed. Original menu/game music was imported. The original road generator and tuning have not yet been ported.

Unity compiled successfully and `SpeedRoversChecks.Run` passed 5,484 assertions (asset, projection and gameplay checks; not 5,484 distinct scenarios). Native Windows scripted captures cover all menu modes, gameplay, steering and pause. The two paused frames match byte for byte. No exceptions were reported by the native player.

Evidence: `C:\SpeedRoversCapture\ui-checks-1013.log`, `ui-build-windows-final-1013.log`, `ui-player-windows-final-1013.log`, and `WindowsSourceUIFinal1013\`. These are automated framebuffer/simulation checks. Interactive keyboard/controller testing remains unverified because desktop capture returned wallpaper and the user was away.

## Build evidence

Unity 6000.6.0f1 completed all six methods from `SpeedRoversBuild.BuildAllPlatforms` for version **0.2.0**. The current log is `build-fidelity-platforms.log` (ignored by Git), ending with editor exit code 0. The earlier `build-all-platforms.log` refers to version 0.1.0.

The new version corrects camera-relative steering, slow rider animations, asphalt edges and pause rendering based on the original source. `FIDELITY.md` records the comparison and remaining differences. `SpeedRoversChecks.Run` passed the asset, projection and gameplay checks in `gameplay-checks.log`. Windows and Linux/WSLg each produced two byte-identical paused framebuffer captures, 0.5 seconds apart.

| Target | Current artifact | Evidence | Remaining validation |
| --- | --- | --- | --- |
| Windows x64 | `Builds/Speed-Rovers-Reborn-Windows.zip` | Version 0.3.0 native player built and scripted framebuffer captures inspected; restored original menu structure and removed gameplay steering buttons. | Interactive keyboard/controller testing and longer gameplay |
| macOS | `Builds/Speed-Rovers-Reborn-macOS.zip` | Mach-O universal header has two CPU types: `0x01000007` (Intel x64) and `0x0100000c` (ARM64). ZIP stores executable permissions. | Run on a Mac; distribution signing/notarization |
| Linux x64 | `Builds/Speed-Rovers-Reborn-Linux.zip` | Version 0.2.0 rendered title, garage, race and steering through Ubuntu 22.04 on WSLg. Software rendering completed with exit 0; the GPU path crashes during driver cleanup (details below). Paused scene stays still. | Native Linux desktop/GPU run. WSLg users on the affected driver can use the included software-rendering launcher option. |
| Android ARM64 | `Builds/Android/Speed Rovers Reborn.apk` | Version 0.2.0/code 2 inspected; APK signature and 16 KB ZIP alignment passed. Earlier 0.1.0 installation into a fresh API 35 emulator succeeded, but graphics failed. No physical devices are connected in the latest ADB check. | Runtime on a compatible physical device; touch/audio/performance and suspend/resume checks |
| iOS / iPadOS | `Builds/Speed-Rovers-Reborn-iOS-Xcode.zip` | Unity exported `Unity-iPhone.xcodeproj`; bundle ID is `com.philino.speedrovers.reborn`, deployment target 15.0, device family `1,2`. | Compile with Xcode on macOS, choose signing team, run on iPhone/iPad |
| WebGL | `Builds/WebGL/` | Version 0.2.0 starts, accepts Enter and ArrowRight, and pauses with Escape. Actual paused framebuffer inspected at 390×844; document width is 390 with no overflow. Browser reports zero errors. | Broader browser/device testing |

## Linux shutdown diagnosis

Tracing the same 0.2.0 binary with `strace -f -k -e trace=exit,exit_group` captured a `SIGSEGV` in `/usr/lib/wsl/drivers/.../libnvwgf2umx.so` after Unity's cleanup. The regular WSLg log identifies the renderer as D3D12 (NVIDIA GeForce RTX 3060), via Mesa OpenGL. Evidence is saved locally in `C:\SpeedRoversCapture\linux-exit.trace`.

Running that binary with `LIBGL_ALWAYS_SOFTWARE=1` selects Mesa llvmpipe, renders all six scripted screenshots and exits with code **0**. This isolates the observed shutdown failure to the WSLg GPU path; it does not prove behavior on a native Linux graphics driver. The software-rendering log and captures are `C:\SpeedRoversCapture\linux-software.log` and `C:\SpeedRoversCapture\LinuxSoftware`.

The Linux ZIP now includes executable `launch.sh`. From its extracted directory, run `./launch.sh` normally, or `./launch.sh --software-rendering` for this workaround. No system graphics settings or drivers were modified. Software rendering can be slower than native GPU rendering.

## Android emulator findings

The ARM64 package installs through the emulator's ARM translation. Both the default renderer and SwiftShader fail to create an ES 3.1/3.2 context, so installation is not evidence of a successful game run. The error screen is saved at `Preview/android-emulator-error.png`. Unity 6000.6 explicitly reports that Android x86-64 support has been removed; an attempted x86-64 test build was rejected and that unsupported build method was removed.

The existing user emulator was opened read-only and found to have insufficient free data storage. A separate `SpeedRoversQA` AVD was created for testing; the user's emulator data was not wiped. All test emulator sessions were stopped, and the temporary `SpeedRoversQA` AVD was deleted after validation.

A WSL service failure during resource contention recovered after stopping the emulator; the final Linux player then rendered successfully. WebGL has one Unity persistence deprecation warning, but no startup error in the final browser run.

## Reproducible commands

Use the Unity command shown in `README.md` with `-executeMethod SpeedRoversChecks.Run` for gameplay checks, then `-executeMethod SpeedRoversBuild.BuildAllPlatforms` to build desktop first, followed by Android, iOS export and WebGL. Package desktop output with `Tools/package_desktop.py` and the Xcode export with `Tools/package_ios.py`. `Tools/verify_packages.py` passed integrity checks on all five archives, verified the universal macOS CPU types and refreshed `Builds/build-artifacts.json` with SHA-256 checksums.

For a Windows/Linux framebuffer capture, pass `-capture-dir=<absolute-directory>` to the player. It captures title, garage, race, left steering and two paused frames, then quits. Current local capture/log folders are `C:\SpeedRoversCapture\WindowsFidelity` and `C:\SpeedRoversCapture\LinuxFidelity`. The Android APK is development-signed. The iOS export contains no signing team. Nothing has been submitted to an app store.

The overall cross-platform goal remains open until the outstanding platform runtime checks are completed.
