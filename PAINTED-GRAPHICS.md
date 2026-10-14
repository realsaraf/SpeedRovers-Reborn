# Current Windows revision — 0.7.0

See `CORRECTIONS-0.7.md` for the latest screenshot fixes and verification. The 0.6 tire overlay and floating bridge assembly described below have been superseded.

---

# District, wheel and traffic pass — 0.6.0

## Correction from the user's second image

The initial 0.5 pass was too crowded. Current traffic starts with two spaced rivals, spawns one candidate every 125–165 m and caps regular visible vehicles at four (three on open roads); an occasional police vehicle may bring the total to five. Construction appears every 850 m instead of 540 m.

Full sixteen-phase tire surfaces and wheel-face sprites replace the previous small decorative overlays. The player's wheel phase follows travelled distance; fast tread blends into longitudinal blur. Car wheel contact stays fixed during suspension movement. Tight tire contact shadows, softer underbody shadows, warm projected lamp pools, paving seams and longitudinally blurred asphalt address the floating-cutout appearance. Small rival lane adjustments no longer trigger large quarter-view pose jumps.

This is a runtime approximation of the supplied composition, not a claim that the second still is reproduced exactly.

## Current changes

- Lower chase perspective (27 m focal distance versus 85 m), 190 px player rider, acceleration from 170 to 285 km/h in normal mode (225 easy), short world-space road markings.
- Repeating 5 km route: downtown (0 m), river bridge (700 m), green outskirts (1,250 m), city day (2,100 m), Italian coast (2,800 m), Tokyo night (3,500 m), and Alps (4,250 m). Projected suspension towers/cables and guardrails, district panoramas, daylight buildings, flower planters, coastal villas, rock faces, conifers, neon shops and cherry trees. Cross streets and zebra crossings occur only in urban sections. These are seven scenery sections in the existing game loop; the original city-unlock menu remains.
- Two spaced rivals start ahead. Five bike/rider designs, twelve forward vehicle designs, eight front-facing designs including police car and motorcycle. Oncoming traffic moves toward the player in the opposing lane.
- Nine steering frames for the red rider, anchored continuous lean/suspension motion, distance-driven sixteen-phase rear tire animation, traffic tire animation and emergency lights.
- Construction zones replace random original roadblocks: cones, milled pavement, warning barriers and excavators. Potholes and bike contact slow the rider; vehicle/construction impacts end the run.
- Six automatic gears with RPM drops on shifts. Layered engine load/coast, wind, pass-by, siren and impact audio; distance/pan/passing pitch for police; music ducking. Added the previously missing AudioListener.
- A/D or arrows steer, S/Down brakes, release accelerates, Esc pauses. Original menus and progress keys remain.

## Current provenance

New art is in Assets/Resources/Painted/Rush*.png, DistrictSkies.png, BridgeStructures.png, DayStreetProps.png, TravelSkies.png and TravelProps.png. District and travel prompts are in Tools/district-art-prompts.json and Tools/travel-art-prompts.json. prepare_district_atlas.py records connected-component bounds without editing generated pixels. The first malformed sky generation was rejected; the second is used. Built-in imagegen, 2026-10-13; prompts in Tools/rush-art-prompts.json, rush-hero-prompt.txt and rush-oncoming-prompt.txt. prepare_rush_atlas.py records bounds without editing generated pixels. Tread/glow/surface effects are procedural.

Assets/Resources/Audio/Rush contains original synthesis produced by Tools/make_rush_audio.py. Music remains the supplied soundtrack. Road Rash art, music and recordings were not imported.

User references: https://www.youtube.com/shorts/5LDt6mO3iig was visually inspected in the browser. The two later links, https://www.youtube.com/shorts/IRDbomUym_w and https://www.youtube.com/watch?v=GjtrBwrX2ZA, received automated video/audio analysis. The short analysis identifies rev rise, gear-shift pitch dips and passing-vehicle whoosh; this does not establish an exact sonic match.

## Current build and checks

SpeedRoversBuild.BuildWindowsRush produces Builds/WindowsRush/Speed Rovers Reborn.exe. Package with Tools/package_desktop.py --target WindowsRush. Other platforms are not rebuilt for this pass.

-rush-review-dir captures route views and a 540-frame production-simulation sequence with automated steering. Tools/encode_rush_preview.py encodes it at 30 fps. This is a scripted review, not a human playtest or performance benchmark.

-audio-review-dir captures the native listener mix through acceleration, police approach, braking, acceleration, crash and pause; saves WAV and RPM/gear telemetry. Review modes suppress progress writes. Audio-thread capture follows Unity documentation: https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/monobehaviour/onaudiofilterread .

Limits: no occluding hill crests; cross streets are visual intersections, not alternate routes; car animation combines continuous deformation, tire frames and lights rather than a full directional frame set for each car. The renderer still uses projected strips and billboards; steep terrain silhouettes, physically accurate light/shadow and fully directional traffic art are not implemented. No fighting or crash recovery. Road Rash visual/audio parity is not established.

---

# Earlier painted sprite pass — 0.4.0

The user selected the detailed painted look of the PC/3DO era. Speed Rovers stays a portrait, sprite-based checkpoint racer. All graphics are drawn as 2D images and projected road strips; there are no 3D models or 3D lighting rigs.

## Implemented

- Reciprocal perspective with a wider foreground road, curved vanishing point, and gentle changes in road grade.
- World-space asphalt grain, faint tire tracks, lane markings, road shoulders and distance haze.
- One depth-sorted list for scenery, traffic, collectibles and checkpoint gates.
- Five new painted city panoramas and twenty roadside sprites, covering New York, Mexico City, Shanghai, London and Tokyo.
- Four painted traffic vehicles with three driving frames each, suspension/tire detail, animated tail-lamp glow and forward travel at 7–13 m/s.
- Red-bike rider with five rear/rear-quarter steering views, four complete animation phases, tire-tread animation, lean-dependent shadow and a rear-tire contact pivot. Collectibles spin and bob.
- Five plants with three wind frames each, blended at staggered phases; smaller roadside shrubs share their own phase and scale.
- Eight river-water frames projected beside the New York and Shanghai roads, scrolling/reflection animation, drifting cloud sprites, distant city lights, window glows and streetlamp halos.
- All effects use the same animation clock and freeze on pause.
- Scale correction after user review: rider height 200 px instead of 274 px, larger traffic/roadside objects, lower camera, closer roadside spacing and subtle peripheral speed streaks. Normal-mode speed rises from 95 to 220 km/h; easy mode tops out at 160 km/h. Distance uses average speed over the frame to avoid acceleration changing with frame rate.

## Art provenance and limits

The PNG atlases in `Assets/Resources/Painted/` were generated using the built-in `image_gen` tool on 2026-10-13. The original Michelangelo sheet was supplied as the identity/pose reference for the red-bike rider; later rider/traffic animation sheets use the initial painted assets as references. No Road Rash assets were imported. Exact prompts are in `Tools/painted-art-prompts.json` and `Tools/painted-motion-prompts.json`. Earlier static sheets are retained as source references. Glow, cloud and tire-detail effect sheets are created procedurally at runtime.

`Tools/prepare_painted_atlas.py` records sprite bounds in `Regions.json` without altering the PNGs. The importer preserves their dimensions and transparency. Asphalt is generated by the renderer from deterministic noise.

This pass upgrades the main red-bike rider. The other four selectable riders retain their original animation sheets. Menus retain the original artwork. Fully occluding hills, crash animations, traffic lane changes and a consistent painted treatment for every rider remain further work. Light sprites suggest illumination; there is no physical lighting simulation. Road Rash is the quality reference; parity has not been established.

## Windows build

Use `SpeedRoversBuild.BuildWindowsPainted` for the separate `Builds/WindowsPainted/` build. The existing `Builds/Windows/` 0.3.0 build remains available for comparison. Other platform packages have not been rebuilt with these changes.

`-art-review-dir=<directory>` runs a deterministic native framebuffer capture across five cities, both full lean directions, a 90-frame motion sequence and pause. It does not write progress. Encode the 30 fps review clip with `python Tools/encode_motion_preview.py <directory>`. The clip is a scripted rendering demonstration, not a performance benchmark or live input recording. `SpeedRoversChecks.Run` checks assets, perspective alignment, simulation consistency, pickups, collisions, traffic motion and pause behavior.

## Verification on 2026-10-13

- Final gameplay suite: 5,495 assertions passed in `C:\SpeedRoversCapture\scale-speed-checks-final.log`.
- Final native Windows build succeeded in `scale-speed-build-final.log`.
- Native review completed without player exceptions in `scale-speed-player-final.log`. Captures in `C:\SpeedRoversCapture\ScaleSpeedFinal`; the two paused captures are byte-identical.
- Inspected upright and turning captures after correcting the scale and removing an IMGUI camera-roll artifact. The road surface is continuous through the turn.
- `Preview/Painted/motion.mp4` contains a three-second, 30 fps scripted framebuffer sequence. It demonstrates animation and simulated 220 km/h movement; it is not measured interactive frame-rate evidence.
- The updated normal player was launched. Windows UI automation twice reported `failed to activate captured window`; interactive keyboard testing was not completed in this pass.
- `Builds/Speed-Rovers-Reborn-Windows-Painted.zip` passed ZIP integrity checks; its SHA-256 is in `Builds/build-artifacts.json`.
