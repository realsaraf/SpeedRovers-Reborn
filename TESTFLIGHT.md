# TestFlight release workflow

App: Speed Rovers 2 (Apple ID 6822590400). iOS bundle: `com.philino.speedrovers.reborn`.
The user installed the new app's first release, 1.0.0 (104), successfully. Current release source is 1.0.2. Device testing of each update remains separate from Apple's Testing status.

`codemagic.yaml` archives and signs the Unity export on a Mac, then uploads the IPA to Speed Rovers 2. The new internal group automatically receives eligible builds for realsaraf@gmail.com. External beta review is deferred until the new app installs on a device. The workflow does not submit a public App Store release.

The workflow reuses the account's existing Apple Distribution certificate. The stored legacy `speed-rovers-appstore` profile identifies that certificate. `CI/ensure_profile.py` creates or reuses a separate Speed Rovers 2 App Store profile for the new bundle identifier. No new certificate or API key is created. Signing secrets remain in Codemagic.

The original Speed Rovers app (916983500, `com.philino.speedrovers`) remains intact. Builds 102/103 processed but the user reported installation failure. Apple shows a distribution restriction on that legacy record. The new app is a separate product and cannot update original-app installs.

For a new gameplay release:

1. Edit this repository's Unity project and run relevant gameplay checks.
2. Run `python Tools/prepare_testflight_export.py` using Unity 6000.6.0f1 with the iOS build module installed. This exports the current source and refreshes the archive, checksum and normalized source fingerprint.
3. Commit the game changes and refreshed `CI/` export to `main`. Git LFS stores `CI/ios-export.tar.gz`.
4. The repository's push webhook starts Codemagic. Source verification rejects an outdated export. The IPA build number is 100 plus Codemagic's build number.
5. Check both Codemagic upload success and Apple's processing/Testing status. A completed Mac build alone does not prove TestFlight availability or device installation.

The new app record and export were prepared on 2026-10-14. Apple device validation remains necessary.

Build app: https://codemagic.io/app/6acee7d9e28a90791b094ade/settings
TestFlight: https://appstoreconnect.apple.com/teams/69a6de88-0957-47e3-e053-5b8c7c11a4d1/apps/6822590400/testflight
