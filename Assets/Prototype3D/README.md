# Speed Rovers 2 — landscape game

The landscape scene is now the main build entry point. This is a new game with independent `sr2_` save keys; no legacy save migration is performed.

## Play

Run `Play Speed Rovers 2.cmd` in the parent folder, or `Builds/Landscape/Speed Rovers 2.exe`.

- W / Up: hold accelerator; S / Down: hold brake.
- A / D or arrows: continuous steering, bounded by the road.
- Mouse: hold the right control and drag to steer; hold the left control to brake.
- Phone: simultaneous left/right controls, thumb-drag steering by default; optional tilt in Settings.
- Space: jump. Shift / N: nitro. P / Escape: pause.
- Jump for gold coins; collect red cans on the ground for fuel.
- Nitro requires at least 50% charge, recharges automatically, and never lifts the bike.

## Game loop

Home, route selection, garage, settings, countdown, race, pause/restart, finish results, credits and saved upgrades. Three race lengths (6 / 8 / 10 km), three competitor motorcycles, bidirectional traffic, roadwork obstacles, close-pass rewards, fuel exhaustion, collision slowdowns and downshifts, progressive acceleration and music/effects/haptic settings. Districts last 2.2 km with continuous road geometry. All routes currently share a straight road layout, changing the district order and race length.

## Builds and verification

Unity 6000.6.0f1. Desktop: `-executeMethod TrafficRideBuild.Build`. iOS export: `-executeMethod SpeedRoversBuild.BuildIOS`. Both use `Assets/Prototype3D/TrafficRide.unity`.

Launch the desktop executable with `-race-qa=C:\path\to\output` to run deterministic runtime checks and capture home, garage, routes, pause, gameplay, riverside and results. QA never writes progression. Output `result.txt` contains the failure count. Checks include control alignment, gradual acceleration, braking, grounded nitro, charge gating, jump/landing, pickups, collisions, recovery, pause, rival advancement, district duration and one-time rewards.

## Current visual limitations

This is a playable complete race loop, not final reference-quality art. Rival motorcycles/riders are procedural 3D assemblies. Traffic currently uses one detailed sports-car model with colour variants. The cockpit is layered image art; mirrors are not live rear-view renders, and hands are not individually animated. Streets use modular textured facades, tree cutouts and simple river/park scenery. The 213k-triangle car needs a mobile performance/LOD pass. Physical-device input, haptics, performance and installation still require TestFlight verification.

Asset provenance remains in `SOURCES.json`, `Models/LICENSE.md` and `Art/GENERATION.md`.
