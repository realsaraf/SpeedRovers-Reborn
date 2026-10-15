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
