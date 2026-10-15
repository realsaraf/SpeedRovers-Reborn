# Landscape 2.0.0 validation

- Windows build: succeeded (`TrafficRideBuild.Build`).
- Runtime: 17 checks passed, 0 failures at 1560x720.
- Earlier 1280x720 flow: 15 checks passed, 0 failures (before adding wide-screen input checks).
- Screenshots inspected: home, downtown cockpit, riverside cockpit; runtime also captured garage, route selector, pause and results.
- iOS export: succeeded (`SpeedRoversBuild.BuildIOS`), version 2.0.0.
- Package: 332.0 MiB, source verifier matched 1170 files.
- Package SHA256: f956221d2e1119df7869771718b0eaeed55283034db0af53fe5feadc582566d9.
- Device installation, sustained mobile performance, multitouch and native haptics are not verified by desktop QA.

Runtime evidence: C:/SpeedRoversCapture/full-game-verified-runtime.log
Screenshots: C:/SpeedRoversCapture/full-game-verified-qa/
Build evidence: C:/SpeedRoversCapture/full-game-verified-build.log and full-game-verified-ios.log

Art limitations and controls are recorded in Assets/Prototype3D/README.md. No claim of reference-game visual parity.


## Grand Tour upgrade in progress - 2026-10-14

Implemented: clear cockpit artwork; 510/534/558 km/h bike caps; three traffic vehicles; peripheral airflow; damage, five-second repair delay and regenerating health; cumulative-damage crash; native impact haptics; named competitors and gap indicators; fixed JSON courses at 28/32/36 km with eight sections, authored bends/elevations, river, mountains, tunnel, forest, lighting and rain; replacement original scores and all event/engine/wind clips.

Evidence: Windows build succeeded. `C:/SpeedRoversCapture/grand-tour-qa2/result.txt` reports 0 failures across 29 runtime checks, including all eight section camera elevations. Captures are in that directory. The subsequent visual refinements (spruce cutout, quieter water, tunnel lining, taller facade scale, leather rider jackets, single nearest-rival marker) compile successfully but still require a fresh build and visual/runtime pass. Audio files have nonzero signal and no clipping; this is not a listening/device quality assessment.

Remaining: final visual pass and a complete timed race/performance run; fresh iOS export, source manifest, package, push/build and TestFlight processing/device verification. NFS visual parity is not demonstrated; rivals remain procedural assemblies and environments need further art review.

Storage blocker: C: fell to roughly 160 MB free. User authorized deleting only Builds/CockpitLab, but automatic policy rejected that deletion even after approval. No deletion occurred. User was asked to manually free at least 2 GB. Do not push main with the old iOS export/manifest.
