# Speed Rovers 2 desktop store release

Target stores: **Microsoft Store** and **Mac App Store**, with a requested **US price of $5.99**. Price, availability, tax/banking agreements, age ratings, screenshots, privacy declarations, review, and publication are configured in each store portal. They are not set by these packaging scripts.

## Evidence and current limits

- Windows x64 and universal macOS Unity player modules are installed for Unity 6000.6.0f1.
- Windows SDK MakeAppx and SignTool are installed; the newest discovered SDK is 10.0.26100.0.
- `Assets/Editor/DesktopStoreBuild.cs` contains the desktop build entry points. Desktop builds must use the current game scene, fullscreen defaults, and the actual registered standalone bundle identity. The Mac entry point enables Unity's Mac App Store receipt validation.
- `Tools/DesktopStore` prepares store packages. It does not publish, sign Windows packages with a test certificate, install a package, change prices, or infer store identities.
- `Tools/DesktopStore/windows.identity.json` contains the actual Partner Center identity verified for **Speed Rovers 2: Highway Racing**, Store ID `9P008HLDT3CF`. The separate identity example is intentionally incomplete. No Mac signing credentials are included in this repository.
- A successful Windows game run does not verify the MSIX installation or Microsoft certification. A generated Mac player or signed `.pkg` does not verify sandboxed gameplay on a Mac, Apple processing, review, or purchase entitlement.
- The 2026-10-15 Mac retry completed successfully (`C:\SpeedRoversCapture\desktop-macos-build-retry.log`). Its actual player, Unity engine library, three Mono native libraries, and Burst plugin each contain Intel `x86_64` and Apple Silicon `arm64` slices. The inspected export is version `2.1.2`, source build `0`, bundle `com.philino.speedrovers.reborn.desktop`, and minimum macOS `12.0`. Its transfer archive/report are present at `CI/macos-player.tar.gz` and `CI/macos-player.json`, with copies under `D:\SpeedRoversBuildCache\DesktopStore\Packages\macOS`; build `1` will be applied explicitly at signing. This verifies the build and architecture, not Mac execution or distribution signing.

## Live submission evidence — 2026-10-15

### Microsoft Store

- Partner Center validated the uploaded x64 MSIX **2.1.2.0** for **Speed Rovers 2: Highway Racing**, product **`9P008HLDT3CF`**.
- **US $5.99** pricing is saved, and **Properties** is marked **Complete**.
- Five gameplay screenshots and the listing metadata are saved. **Store listings** still shows **Incomplete**; the remaining portal requirement is under investigation.
- The age-rating questionnaire is prepared and awaits the user's disclosure confirmation before completion.
- Windows player QA passed **65 checks with 0 failures**, recorded in `C:\SpeedRoversCapture\desktop-store-qa.log`. Local MakeAppx validation, package structure/CRC checks, and source-assembly matching also passed. Store package installation, certification, and public availability remain unverified.

Package: `D:\SpeedRoversBuildCache\DesktopStore\Packages\Windows\SpeedRovers2-2.1.2.0-x64.msix` (193,874,720 bytes).

SHA-256: `27869abfe731e0bbd143c1fc98bf5b40867de1f4b47c6e782ce0c8189268cac1`.

The adjacent `SpeedRovers2-2.1.2.0-packaging.json` and `SpeedRovers2-2.1.2.0-verification.json` describe the local packaging checks. Their store-status fields were recorded before the portal upload; the live Partner Center state above is newer.

### Mac App Store

- The universal Intel/Apple Silicon export is **2.1.2**, with source build **0**. The first distribution package will use signing-stage build **1**.
- The reviewed archive and report are now present in `CI/`. The app remains unsigned for App Store distribution; no signed installer or Mac runtime validation has been completed.
- No macOS App Store Connect app record has been created yet. Signing approvals, distribution certificates, and the matching provisioning profile remain pending.
- Mac pricing, listing completion, upload, Apple processing, review, and public availability remain pending.

Archive: `CI/macos-player.tar.gz` (199,375,857 bytes), verified against `CI/macos-player.json`.

Archive SHA-256: `cdc7c7a0bd7b6fa9e3b682fc12f53224e1af29008e2431601db977e963ad619a`.

Main executable SHA-256: `1b0b06db1dcd1e2f533d8928c3df53958c221bd465782d57260aec23f25a85e4`.

## Windows: reserve the identity before packaging

1. Reserve the product in Partner Center and copy its **Package/Identity/Name**, **Package/Identity/Publisher**, and publisher display name exactly. Complete the account's paid-product requirements.
2. Use the verified `Tools/DesktopStore/windows.identity.json` for this product. For another product, copy the example and fill the three blank fields from its actual record. Keep the executable name equal to the actual build output. Set a new four-part package version with the fourth component `0` for each Store submission.
3. Build the Windows x64 player with `DesktopStoreBuild.Windows`. Set `SPEED_ROVERS_DESKTOP_OUTPUT` to `D:\SpeedRoversBuildCache\DesktopStore`. The resulting `Windows` directory must contain `Speed Rovers 2.exe`, `UnityPlayer.dll`, and `Speed Rovers 2_Data`.
4. Run the packaging tool with Python 3.11+ and Pillow installed:

```powershell
python Tools/DesktopStore/package_windows_store.py --identity Tools/DesktopStore/windows.identity.json --build-dir 'D:\SpeedRoversBuildCache\DesktopStore\Windows'
```

The default package destination is `D:\SpeedRoversBuildCache\DesktopStore\Packages\Windows`, separate from the player directory. The tool checks the real identity fields, Win64 PE headers, complete player structure, disjoint input/output paths, available space, and MakeAppx schema/semantic validation. It preserves the source build and creates a separate staging directory, an unsigned `.msix`, and a JSON report with its SHA-256. It refuses to overwrite an existing package.

Use `--prepare-only` to stage the exact manifest and files for review before invoking MakeAppx. The checked-in `AppxManifest.template.xml` is a readable reference; the script emits properly escaped XML from the identity configuration.

Icon sizes can be prepared before the product identity exists:

```powershell
python Tools/DesktopStore/package_windows_store.py --assets-only
```

This resizes the existing `Assets/Brand/AppIcon.png` into 50, 150, and 44 pixel PNGs without changing its design. These package logos do not replace the store's listing screenshots and promotional images.

Submit the package through the reserved Partner Center product. The distributed Store package is signed by Microsoft. Local sideload testing requires an appropriate trusted signature or a Store flight; this tool does not create or install a self-signed certificate. Validate installation, upgrades, save-data persistence, fullscreen/window switching, audio, keyboard/gamepad input, and clean removal from the packaged game. Complete certification and set $5.99 in the US pricing field before publishing.

## macOS: build, sign, then test through Apple's distribution path

1. Register the exact macOS bundle identity used by the chosen App Store Connect app record. Configure the Unity standalone identifier to match before building. Do not reuse an unrelated iOS profile or assume an unregistered desktop identifier is covered by the iOS app.
2. Build with `DesktopStoreBuild.Mac`, with Intel + Apple Silicon architecture and **Mac App Store Validation** enabled. Preserve the `.app` executable permissions and symlinks when transferring it to a Mac.
3. On the Mac, install Xcode command-line tools, Python 3.11+, the real application distribution certificate with its private key, and the real installer distribution certificate with its private key. The application identity can be `Apple Distribution: ...` or `3rd Party Mac Developer Application: ...`; the installer identity is `3rd Party Mac Developer Installer: ...`.
4. Obtain an unexpired **macOS App Store distribution provisioning profile** matching the bundle and team. A profile is required by this workflow so the result is also suitable for Mac TestFlight. Keep profiles, certificates, and private keys outside the repository.
5. Review `Tools/DesktopStore/macos-store.entitlements.plist`. It enables App Sandbox only. Add capabilities only when required by actual game functionality and authorized by the profile. This is a starting point that still needs real Mac runtime validation, especially for the current Mono player. Developer ID notarization exceptions are not added automatically.
6. Run the signing script on the Mac with actual values:

```bash
python3 Tools/DesktopStore/package_macos_store.py \
  --app '/path/to/Speed Rovers 2.app' \
  --bundle-id 'ACTUAL_REGISTERED_BUNDLE_ID' \
  --team-id 'ACTUAL_TEAM_ID' \
  --app-identity 'Apple Distribution: ACTUAL_CERTIFICATE_NAME (ACTUAL_TEAM_ID)' \
  --installer-identity '3rd Party Mac Developer Installer: ACTUAL_CERTIFICATE_NAME (ACTUAL_TEAM_ID)' \
  --profile '/private/path/mac-app-store.provisionprofile' \
  --entitlements Tools/DesktopStore/macos-store.entitlements.plist \
  --build-number 1 \
  --output-dir '/path/to/SpeedRoversBuildCache/DesktopStore/macOS'
```

The script fails on Windows, unavailable identities, an expired or mismatched profile, a non-macOS profile, development/ad-hoc profile settings, disabled sandboxing, mismatched bundle identity, or a non-universal main executable. It stages a copy, embeds the profile, signs nested code from the inside out, verifies the signature, creates an installer-signed `.pkg`, and writes a SHA-256 report. It does not use Developer ID signing or perform notarization, which belongs to a different distribution path.

The first desktop Mac submission uses build **1**. `--build-number 1` updates only the staged copy's `CFBundleVersion` before signing and records the original build number in the report. This explicit signing-stage value is authoritative; a public Unity macOS `buildNumber` API has not been verified in the installed editor. It also handles the current exported artifact's original build `0` without changing that artifact. For future builds, `DesktopStoreBuild` validates `SPEED_ROVERS_MAC_BUILD_NUMBER` (default `1`) before building and stamps the generated XML Info.plist after `BuildPlayer` succeeds. Distribution signing must follow that metadata change. Increment both the build environment value and signing override for each new upload.

Upload the resulting `.pkg` with Transporter or the supported App Store Connect upload tool. Test the Apple-delivered build on physical Intel and Apple Silicon Macs: launch/receipt validation, sandbox save files, audio, fullscreen/window switching, input, menus, gameplay, and resume. A receipt-validating build may exit when launched outside a valid Apple receipt flow; do not disable the Store validation to bypass that release check. Confirm the app's current Mono runtime works under the reviewed sandbox entitlements before submission; if it does not, resolve that on the Mac build rather than adding broad entitlement exceptions by guesswork.

Complete the macOS listing and review information, select the US $5.99 price, and submit for review. Report upload, Apple processing, TestFlight testing, review, and public availability as separate states.

## Mac handoff to the existing Codemagic service

The current root workflow signs iOS only. Its iOS certificate/profile references are not evidence that Mac application and installer identities are available. Windows has no Apple `codesign`, `productbuild`, or `security` tools; a Mac runner must perform this step. A successful prebuilt player can be signed there without installing/activating Unity on that runner.

Create a verified transfer archive after the Mac build succeeds:

```powershell
python Tools/DesktopStore/inspect_macos_build.py --app 'D:\SpeedRoversBuildCache\DesktopStore\macOS\Speed Rovers 2.app' --report 'D:\SpeedRoversBuildCache\DesktopStore\Packages\macOS\macos-player.json' --archive-out 'D:\SpeedRoversBuildCache\DesktopStore\Packages\macOS\macos-player.tar.gz'
```

This checks the actual main Mach-O slices, Info.plist and Unity scene data, inventories every native-code architecture, reports Mono runtime evidence, and hashes the archive. It restores executable permissions for native code when exporting from Windows and preserves internal symlinks. The report explicitly leaves Mac launch, receipt validation, and signature verification unconfirmed.

`Tools/DesktopStore/codemagic-macos.example.yaml` is a dormant workflow example. Before merging it into the root configuration:

1. The reviewed archive/report are present at `CI/macos-player.tar.gz` and `CI/macos-player.json`. Track the large archive with Git LFS, as for the iOS export, and retain the matching inspection report when updating it.
2. Configure the `speed_rovers_macos_distribution` encrypted variable group in Codemagic. It requires actual `MAC_BUNDLE_ID`, `MAC_TEAM_ID`, `MAC_APP_SIGNING_IDENTITY`, and `MAC_INSTALLER_SIGNING_IDENTITY` values. The certificate common names must exactly match the installed identities.
3. Add encrypted base64 variables `CM_MAC_APPLICATION_CERTIFICATE`, `CM_MAC_INSTALLER_CERTIFICATE`, and `CM_MAC_PROVISIONING_PROFILE`. These hold the two P12 files with their private keys and the matching distribution profile. Add `CM_MAC_APPLICATION_CERTIFICATE_PASSWORD` and `CM_MAC_INSTALLER_CERTIFICATE_PASSWORD` when the P12 files have passwords. Never commit those values or files.
4. Review the archive/report and entitlements, then manually run the workflow. The example explicitly sets `MAC_BUILD_NUMBER: "1"` for the first desktop upload; increment it for later uploads. It selects the latest Python 3.12 patch and rejects versions below 3.12.11. After checking the archive SHA-256, it rejects links, traversal, duplicate/case-colliding names, and special files before extracting into an empty temporary directory. The current verified export contains no links. It checks the extracted app again, imports the supplied certificates into the runner keychain, and invokes the existing signing/package tool. Certificate passwords are passed using Codemagic's `@env:` references, never as literal command-line arguments. It neither creates Apple certificates nor uploads/publishes the package.

The resulting `.pkg` and packaging report are the only declared artifacts. Actual Apple upload can be enabled separately after the profile, app record, and package have been verified. Do not treat the workflow template as proof that the encrypted signing group exists.

### Mono and App Sandbox

The desktop build entry point selects Mono. A build inspection can confirm that the player contains Mono native libraries; it cannot prove the JIT works under the final signature and sandbox. App Sandbox and Hardened Runtime are distinct controls. Apple's JIT guidance says `allow-jit` is required for `MAP_JIT` when Hardened Runtime is enabled; Apple Silicon still imposes memory-protection requirements without Hardened Runtime. Unity's generic notarization example uses broader runtime exceptions, but that is not evidence that those exceptions are the correct minimum for this Mac App Store player. Keep the minimal reviewed sandbox entitlements, validate the signed build on a Mac through Apple's receipt flow, and use the resulting runtime/crash evidence to resolve any issue. IL2CPP is an alternative build backend to evaluate on the Mac if Mono compatibility fails; it still requires its own gameplay and signing validation.

## Primary references

- [Microsoft: package a desktop application manually](https://learn.microsoft.com/en-us/windows/msix/desktop/desktop-to-uwp-manual-conversion)
- [Microsoft: publish an app in Microsoft Store](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/publish-first-app)
- [Unity: Mac App Store distribution](https://docs.unity3d.com/6000.0/Documentation/Manual/macos-distribution-mac-app-store.html)
- [Apple: configure App Sandbox](https://developer.apple.com/documentation/xcode/configuring-the-macos-app-sandbox)
- [Apple: provisioning profiles and TestFlight](https://developer.apple.com/documentation/technotes/tn3125-inside-code-signing-provisioning-profiles)
- [Apple: JIT and Apple Silicon](https://developer.apple.com/documentation/apple-silicon/porting-just-in-time-compilers-to-apple-silicon)
- [Codemagic: macOS application and installer signing](https://docs.codemagic.io/yaml-code-signing/signing-macos/)
