# Screenshot corrections — Windows 0.7.0

## Changes by screenshot

1. Traffic now looks ahead for the complete cone taper and work zone. Vehicles merge into a clear adjacent lane, brake while merging, and wait before the cones if the lane is occupied. Forward and oncoming approaches are covered. Spawns exclude the entire closed lane, including the taper.
2. Removed the second tire drawn over the bike. A sprite shader now animates and blurs rubber inside the original painted tire silhouette, preserving its lighting and bodywork. The same effect applies to rival bikes.
3. Increased normal car width from 180 to 250 projected units; police cars use that same scale. Rivals use the player's 190-unit height. Enlarged the foreground road to maintain lane clearance.
4. Asphalt and markings use one interpolated scene palette. There is no per-strip district color switch across the road.
5–6. New original bridge portal artwork has no baked cables. Projected cables meet tower stations and terminate at the deck. Continuous concrete shoulders support towers, railings and lamp feet. Bridge exits use widening banks with vegetation.
7–8. Removed checkpoint gantries from the renderer. The existing 500 m time reward remains as a brief +16 SECONDS message.
9. Water, road and atmosphere share transition state. Water samples stay inside water artwork; inland tree pixels are no longer stretched into the river. The sunset background continues through the bridge landing before the outskirts transition.

## Speed

Road and vehicle travel use an explicit 1.65 arcade pace multiplier. HUD km/h continues to represent engine speed. The old 0.05-second frame clamp discarded elapsed time below 20 FPS; the update now consumes elapsed time in small simulation steps. Static scenery draw calls were reduced. This changes travel and passing speed, not just the number on the speedometer.

## Evidence

- Native Windows build: `C:\SpeedRoversCapture\correction-build-final.log`.
- Gameplay checks: 7,103 assertions, including forward/oncoming roadwork avoidance, waiting with an occupied merge lane, and equivalent elapsed-time travel at 10 FPS. `correction-checks-final.log`.
- Final route captures and motion sequence: `C:\SpeedRoversCapture\CorrectionsFinal`. The scripted driver can crash; that outcome is recorded in `Preview/Rush/verification.json`, not treated as a completed race.
- The real-time pace review runs the native renderer for 12 seconds with traffic spawning disabled. Its raw CSV and measured results are retained with the verification report. It is not a full traffic performance benchmark.
- ZIP integrity, hashes and copied play files are verified by `Tools/finalize_correction_build.py`.

The scene remains a 2D projected-strip and sprite renderer. Distant panoramas still crossfade between some districts; fully occluding hills, physically accurate shadows and reference-image parity are not established. Other platform builds have not been rebuilt for 0.7.

## Art provenance

`Assets/Resources/Painted/BridgePortalV2.png`: original transparent artwork generated with built-in imagegen on 2026-10-13. Prompt: `Tools/bridge-portal-v2-prompt.txt`. Atlas preparation records bounds and the foot pivot without changing generated pixels. Tire animation is a runtime shader; source bike sprites remain intact.
