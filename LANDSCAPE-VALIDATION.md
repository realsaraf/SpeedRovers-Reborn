# Engine, fuel and collision balance 2.1.2 - 2026-10-15

Changes: engine idle/load recordings remastered for phone-speaker midrange; dedicated resident engine sources start immediately and recover stopped playback. Music retained. Removed road coins and related HUD/help/result references; finish and close-pass credits remain. Fuel cans carry rotating luminous arrows above rider height, with a target screen height of about 18 pixels at 844 x 390. Fixed fuel stops begin at 5.2 km, recur every 5.6 km, refill 30%, and exclude the final 1.5 km. Impact health damage is divided by 1.5 for 50% more effective durability, retaining speed loss, haptics, cooldown and clean-riding repair.

## Current local evidence

- Final Windows build succeeded: C:/SpeedRoversCapture/engine-fuel-build.log.
- 59 runtime checks, zero failures: C:/SpeedRoversCapture/engine-fuel-runtime-release.log and engine-fuel-qa-release/result.txt. Includes removal of coin actors, fuel spacing/refill, marker rotation, surviving three full-speed car hits, fourth-hit wreck, duplicate-contact cooldown, controls, music and isolated engine output.
- Inspected actual approach and close captures: engine-fuel-qa-release/10-fuel-marker-approach.png and 11-fuel-marker-close.png.
- Isolated engine output captured in engine-fuel-qa-release/engine-only.wav. Checks measure listener audio at idle, throttle, maximum speed, stopped-source recovery, pause silence and resume. Engine recording: -25.70 dBFS RMS, -11.30 dBFS peak; full mix: -21.30 dBFS RMS, -5.59 dBFS peak. Both have zero clipped samples. Collision QA spaces contacts by 1.3 seconds, matching the production cooldown rather than stacking four impact sounds in one frame. This is desktop audio evidence; iPhone speaker balance still requires physical testing.
- Full 28 km race passed in 233.23 seconds through all eight sections, collecting four fuel stops and finishing with 22.1% fuel. Minimum bike health was 48.6%, peak speed 663 km/h. Evidence: C:/SpeedRoversCapture/engine-fuel-soak/soak.txt. Automated steering follows the same pickup rules as the player; no health or fuel grants. An iOS export ran concurrently, so this run is used for gameplay balance rather than performance claims.
- iOS export succeeded: C:/SpeedRoversCapture/engine-fuel-ios.log. Version 2.1.2; 334.3 MiB package; 1248 source/settings files. SHA256: 00519822406909712331be331ba558624a9ac0b7afc890707f75b99758ad147c. Native signing and tester availability are verified separately after the push.

# Sound, motion and transparent HUD 2.1.1 - 2026-10-15

Changes: composed CC0 menu/race music, recorded motorcycle idle/load audio, Kenney CC0 effects, crossfade and interruption recovery. Phone tilt defaults on with neutral calibration and optional thumb-drag mode. Dedicated severity-based Core Haptics collision pulses have UIKit/vibrator fallback. Settings includes recenter and impact-vibration test controls. Multitouch abilities work while throttle is held, including a hold begun during countdown.

Visual direction: translucent brake/throttle grip and lever outlines with pressed glow, unobstructed jump/nitro icons, a narrow right-side speed/distance/time/fuel/health stack, small position top-left, and faint distance-only rival markers. No full-width top bar, rival name list, central event panel or opaque pedal buttons.

## Local evidence

- Windows build succeeded: C:/SpeedRoversCapture/minimal-hud-build.log.
- 43 runtime checks, zero failures: C:/SpeedRoversCapture/minimal-hud-qa/result.txt and C:/SpeedRoversCapture/minimal-hud-runtime.log. Includes simultaneous throttle/abilities, countdown hold, pause release, injected tilt calibration/left/right/dead-zone input, music playback recovery and mute preservation.
- Inspected real runtime screenshots at 844 x 390: C:/SpeedRoversCapture/minimal-hud-qa/05-race.png and 08-new-controls.png. This exercises a translated, scaled landscape UI; it is not an iPhone screenshot.
- Listener output captured in C:/SpeedRoversCapture/minimal-hud-qa/audio-mix.wav. Earlier same audio mix capture measured -20.63 dBFS RMS, -4.25 dBFS peak and zero clipped samples. Runtime confirms 60-second menu and 96-second race tracks loaded and playing.
- Audio source licenses, transformations and hashes are in Assets/Resources/GrandTour/Audio/LICENSED-AUDIO.json and PROVENANCE.txt.
- iOS export succeeded: C:/SpeedRoversCapture/minimal-hud-ios.log. Version 2.1.1; 334.2 MiB export; 1244 Unity source/settings files; SHA256 6773284fb62c8084f0913f669b1e522cc8a5452777b1d41f12921dc44954d8eb. CoreHaptics is linked and both native audio/feedback plugins are included in the Xcode project.
- Native haptic/audio-session code and motion framework linkage require the Mac build. Physical iPhone tilt direction/feel, vibration, interruption handling and speaker balance still require device testing; desktop injected inputs do not prove these.

# Grand Tour 2.1.0 validation - 2026-10-14

Implemented: clear cockpit; 510/534/558 km/h bike caps plus nitro; reduced traffic; peripheral airflow; cumulative damage, delayed regeneration and crash; impact haptics; three rivals; fixed 28/32/36 km courses with eight sections, authored bends/elevations, weather and lighting; replacement original audio.

Latest corrections: dashboard text shares cockpit bob and lean; nearest rival shows only translucent distance; fuel cans have handle/cap/label and spawn 3.2 km apart; waterfront towers have facades; rock uses world-space triplanar detail on slopes and tunnel walls; denser terrain has irregular silhouettes.

## Verified locally

- Windows build succeeded: C:/SpeedRoversCapture/grand-tour-scenery-build.log.
- 29 runtime checks, zero failures: C:/SpeedRoversCapture/grand-tour-scenery-qa/result.txt.
- Inspected coastal, mountain and tunnel captures in that folder.
- Full 28 km race completed in 240.6 seconds, all eight sections; average 59.88 FPS on this PC; 9 of 14425 frames above 33.4 ms; peak 663 km/h with nitro. Minimum health 4.7%; nearby rival on 65.5% of frames. Evidence: C:/SpeedRoversCapture/grand-tour-scenery-soak/soak.txt. Autopilot steering is a test aid; manual inputs can also affect the run.
- iOS export succeeded: C:/SpeedRoversCapture/grand-tour-scenery-ios.log.
- Version 2.1.0; export 330.8 MiB; manifest matches 1214 Unity files.
- SHA256: 60ef59927b27e779af826f2d5b2f762ee27486075a7cef0c473178b72b32f097.

## Remaining verification and limitations

Desktop results do not verify physical iPhone performance, installation, audio quality or haptics. NFS visual parity is not demonstrated: environment geometry and assembled riders remain simplified. App Store Connect browser navigation currently fails with a network error even after the user signed in.

## Upload evidence

Commit 785249b was pushed to main. Codemagic build 20 successfully signed and uploaded version 2.1.0 (120) to Apple with no errors. Delivery UUID: 8e712b93-7cb6-4478-84b3-59d6051228c1. Build URL: https://codemagic.io/app/6acee7d9e28a90791b094ade/build/6ad0493ea90f016d29d5bdf0. Apple completed processing at 2026-10-15 03:41:46 UTC. TestFlight emailed realsaraf@gmail.com at 03:41:49 UTC that Speed Rovers 2 2.1.0 (120) is ready to test. Gmail message IDs: 1a13da6d612e3b63 (processing), 1a13da6e6ce80461 (tester availability). The tester message was read and confirms iOS 15.0 or later. This proves tester-account availability, not physical-device installation.
