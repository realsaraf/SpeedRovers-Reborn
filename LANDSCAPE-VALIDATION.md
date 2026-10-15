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

Codemagic signing/upload and Apple TestFlight processing require separate confirmation. Desktop results do not verify physical iPhone performance, installation, audio quality or haptics. NFS visual parity is not demonstrated: environment geometry and assembled riders remain simplified. App Store Connect browser navigation currently fails with a network error even after the user signed in.
