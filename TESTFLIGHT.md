# TestFlight release workflow

App: Speed Rovers (Apple ID 916983500). iOS bundle: `com.philino.speedrovers`.
The revival's first iOS release is 2.0.0, independent of the Windows prototype's 0.7.0 version.

`codemagic.yaml` archives and signs the Unity export on a Mac, then uploads the IPA to the existing Apple app. The existing App Store Connect Users internal group automatically receives eligible builds; realsaraf@gmail.com has been added to it. The workflow does not submit a public App Store release.

The workflow reuses the account's existing Apple Distribution certificate. The reference provisioning profile provides its public certificate identity; `CI/ensure_profile.py` reuses or creates a separate Speed Rovers App Store profile. No new certificate or API key is created. Signing secrets remain in Codemagic.

For a new gameplay release:

1. Edit this repository's Unity project and run relevant gameplay checks.
2. Run `python Tools/prepare_testflight_export.py` using Unity 6000.6.0f1 with the iOS build module installed. This exports the current source and refreshes the archive, checksum and normalized source fingerprint.
3. Commit the game changes and refreshed `CI/` export to `main`. Git LFS stores `CI/ios-export.tar.gz`.
4. The repository's push webhook starts Codemagic. Source verification rejects an outdated export. The IPA build number is 100 plus Codemagic's build number.
5. Check both Codemagic upload success and Apple's processing/Testing status. A completed Mac build alone does not prove TestFlight availability or device installation.

Source and iOS export were pushed on 2026-10-13. Apple device validation remains necessary.

Build app: https://codemagic.io/app/6acee7d9e28a90791b094ade/settings
TestFlight: https://appstoreconnect.apple.com/teams/69a6de88-0957-47e3-e053-5b8c7c11a4d1/apps/916983500/testflight
