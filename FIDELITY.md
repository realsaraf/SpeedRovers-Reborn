# Original game comparison

This Unity revival is a playable prototype. Desktop (Windows, macOS, Linux) is the first release target. The original MonoGame code and decoded artwork are the reference; browser output is an additional build target.

## What the source establishes

- `../src/Shared.PCL/GameClasses/Components/PlayerComponent.cs`: the rider stays at the horizontal center of the screen. Five lean directions use separate three-frame slow and five-frame fast animations.
- `../src/Shared.PCL/Screens/MainGameScreen.cs`: road segments are projected relative to `PlayerX`; the camera follows steering. Checkpoints and clock pickups extend the timer, and traffic/roadblock collisions end the run.
- `../src/Shared.PCL/Screens/HoodScreen.cs`: the enabled gesture path moves between left, center and right positions. The touch-hold and auto-center paths are commented out. Three-position steering is therefore retained.
- `../src/Shared.PCL/GameClasses/Components/RoadComponent.cs`: asphalt comes from the city's light/dark textures. Mexico and London omit lane markings.
- `../src/Shared.PCL/GameClasses/Road.cs`: the original generates hills, bumps, straights and curves. The Unity prototype currently has a simpler curved road; its track generation is not equivalent.
- `../src/Shared.PCL/GameClasses/Cars.cs`: the original traffic update returns immediately, leaving parked obstacles. The prototype also uses stationary traffic obstacles.

## Changes in 0.2.0

- Centered rider and camera-relative road, scenery, vehicles, pickups and gates. A shared projection keeps these aligned during steering.
- Added all 75 original slow rider frames, alongside the 125 fast frames already present.
- Removed the repeated tapered texture shoulders that made the asphalt edges look like teeth. Narrower strips smooth the projected outline.
- Restored the absence of lane markings in Mexico and London.
- Pausing now freezes rider animation, road, scenery and the checkpoint gate. Previously the roadside jumped to an independent animation clock and the gate disappeared.

## Changes in 0.3.0 (Windows)

- Removed the invented Garage screen. Restored Market, Characters, Cities and Settings using the original screen source and decoded artwork.
- Removed gameplay steering buttons and the large generic HUD panels. Desktop steering uses A/D or arrow keys; touch steering uses swipes. Selection-screen carousel arrows match the original source.
- Restored the original pause/results layers and menu/game music from the supplied MP3 files.
- The capture sequence includes title, market, cities, characters, settings, race, left steering, two paused frames and results. Capture mode suppresses score saving.
- Other platform binaries remain at 0.2.0 until rebuilt.

## Prototype differences still present

Road generation and tuning, timer values, progression prices, menu transitions, bitmap typography, sound effects, power-ups and original service integrations are not yet a full recreation. No claim of full gameplay or visual parity is made.

## Verification

`SpeedRoversChecks.Run` exercises the production simulation without saving test scores. It checks original art availability, projection alignment across lanes and distances, frame-rate consistency, freeze/resume, single-award pickups and checkpoints, collision lane discrimination, roadblocks and timer expiry.

The player's `-capture-dir` option records the screens listed above from the running Unity framebuffer. Identical paused captures verify that the rendered scene remains still. These are scripted runtime captures; they do not establish physical keyboard/controller behavior.
