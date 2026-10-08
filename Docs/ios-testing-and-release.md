# iOS testing and release guide

Everything needed to get a build from Unity onto a phone, into TestFlight, and into the App Store.
Based on the March 2026 release notes, with the gaps that caused trouble filled in.

Project facts (as of October 2026):

| Item | Value |
| --- | --- |
| Unity | 2022.3.50f1 |
| Bundle ID | `com.BetzzzGame.FoodTruckCafe` |
| Apple team | XY39924J9T |
| Minimum iOS | 14.0 |
| Devices | iPhone and iPad (Player Settings, Target Device) |
| Orientation | Portrait only |
| Ads | Unity Ads 4.17, game ID 6074412, delivered through CocoaPods |
| Unity project | `Desktop/Sept/FoodTruckCafe` |
| Xcode exports | `Desktop/Sept/Builds/iOS-<version>` |

## 1. Version and build number

Two numbers ship with every upload. Both are set in Unity, not Xcode.

| Name | Where in Unity | Example | Rule |
| --- | --- | --- | --- |
| Version | Player Settings, Version | `26.03.29` | The App Store listing and the submission draft are tied to this. Change it for every store release. |
| Build | Player Settings, iOS tab, Build | `1` | App Store Connect rejects an upload if a build with the same version and build already exists. Bump it for every upload, even TestFlight-only ones. |

Shown together as `26.03.29 (1)`. The March release went up as build `0` because the Build field was empty; set it to `1` or higher from now on.

Path: Edit, Project Settings, Player, iOS tab (the Apple icon), Other Settings, Identification.
Set Bundle Identifier there too, so every export carries the right ID without editing Xcode.

## 2. Build from Unity

1. Save all scenes. Confirm the build list in File, Build Settings has MainMenu, GameSceneOne, Shop, KitchenScene checked.
2. In Build Settings, platform iOS, make sure **Development Build is unchecked**. That is the "release, not debug" check. A development build logs everything, runs slower, and is rejected by App Store review if uploaded.
3. Player Settings, iOS, Other Settings, Target SDK must be **Device SDK** for TestFlight and the store. Simulator SDK is only for screenshots (section 6).
4. Click Build. Choose a new folder under `Desktop/Sept/Builds`, named by version, for example `iOS-26.04.10`.
   Building into the previous folder is also fine: Unity asks Append or Replace. Append keeps your Xcode signing settings; Replace throws them away and you must redo section 3.
5. Unity runs CocoaPods after the export to pull in the Unity Ads framework. This creates `Podfile`, `Pods/` and `Unity-iPhone.xcworkspace` inside the build folder. If the `.xcworkspace` is missing, open Terminal in the build folder and run `pod install`.

After the export, the post-build script in `Assets/Editor/IOSPostBuild.cs` writes two keys into Info.plist automatically:

- `NSUserTrackingUsageDescription`, which iOS requires before it will show the tracking prompt the ads code asks for. The March build did not have it, so the prompt never appeared.
- `ITSAppUsesNonExemptEncryption = NO`, which answers the export compliance question so builds do not sit in "Missing Compliance" in TestFlight.

## 3. Xcode: sign and archive

1. Open **`Unity-iPhone.xcworkspace`**, not the `.xcodeproj`. The workspace includes the Pods. Opening the project file alone gives "framework not found UnityAds" link errors.
2. Signing. Select the Unity-iPhone project in the left pane, then each target in turn: **Unity-iPhone, UnityFramework, Unity-iPhone Tests, and GameAssembly** if listed. For each, Signing & Capabilities, tick **Automatically manage signing**, Team = your Apple team. This is the fix for every "signing issue" error so far.
3. Check Bundle Identifier on the Unity-iPhone target matches App Store Connect exactly.
4. In the device dropdown at the top, choose **Any iOS Device (arm64)**. Product, Archive is greyed out while a simulator is selected.
5. Product, **Archive**. Takes 5 to 15 minutes. Xcode opens the Organizer when done.
6. In the Organizer: Distribute App, **App Store Connect**, Upload, keep the defaults, Upload. Do not use Xcode Cloud.

To test on your own phone without TestFlight: plug it in, pick it in the device dropdown, press Run. The first time, trust the developer certificate on the phone in Settings, General, VPN & Device Management.

## 4. TestFlight

1. Go to App Store Connect, My Apps, FoodTruckCafe, **TestFlight** tab.
2. The upload appears under iOS Builds within about 30 minutes. Status moves from Processing to Ready to Test. If it says Missing Compliance, click it and answer No to the encryption question (the post-build script should prevent this).
3. Add yourself under Internal Testing. Install the TestFlight app on the phone and accept the invite. New builds auto-install from then on.
4. Test on the real device: pause and resume, phone call interruption, kill the app mid-level and reopen (progress should survive), the rewarded ad in the shop, and the tracking prompt on a fresh install.

## 5. Submit to the App Store

1. App Store Connect, **Distribution** tab (sometimes labelled App Store).
2. If there is no draft for this version, press **+** next to iOS App and type the version number. The draft is tied to the version, not the build.
3. Scroll to Build, press +, pick the processed build.
4. Fill in What's New, confirm screenshots, and answer the advertising identifier question **Yes** (Unity Ads uses it) with "Serve advertisements within the app" ticked.
5. Save, then Add for Review, then Submit. Review usually takes one to two days.

## 6. Screenshots with the simulator

The store needs screenshots per device size. The simulator is the easiest source, but it needs a separate simulator build.

1. Unity, Player Settings, iOS, Other Settings, Target SDK, **Simulator SDK**.
2. Build into a separate folder, for example `Desktop/Sept/Builds/iOS-simulator`. Do not mix it with the device build.
3. Open the workspace in Xcode, pick a simulator from the device dropdown, press Run. Xcode downloads the simulator runtime the first time.
4. Press Cmd+S in the simulator to save a screenshot to the Desktop.
5. **Switch Target SDK back to Device SDK** in Unity when done. A device archive made from a simulator build fails to upload.

Sizes App Store Connect asked for at the March 2026 release. Apple changes these when new devices ship, so check the exact pixel sizes shown on the screenshot upload page before capturing.

| Slot | Simulator to use | Pixel size |
| --- | --- | --- |
| iPhone 6.9" | iPhone 16 Pro Max | 1320 x 2868 |
| iPhone 6.5" | iPhone 11 Pro Max or 14 Plus | 1284 x 2778 |
| iPad 13" | iPad Pro 13" (M4) | 2064 x 2752 |

iPad screenshots are required because the project supports iPad. If you only ever want iPhone, set Target Device to iPhone Only in Unity and the iPad slot disappears. Do not change Supported Destinations in Xcode for this; Unity overwrites it on the next export.

## 7. Errors seen so far and their fixes

| Error | Cause | Fix |
| --- | --- | --- |
| Signing requires a development team / no profiles | A target is not on automatic signing | Section 3 step 2, check all four targets |
| Archive menu item greyed out | A simulator is selected | Choose Any iOS Device (arm64) |
| The bundle version must be higher than the previously uploaded version | Same version and build already uploaded | Bump Build in Unity Player Settings, export again |
| Missing Compliance in TestFlight | Export compliance unanswered | Answer No, or rely on the post-build script |
| Framework not found UnityAds | Opened `.xcodeproj` instead of `.xcworkspace` | Open the workspace |
| No such module / Pods not found | CocoaPods did not run | `pod install` in the build folder |
| Tracking prompt never appears | `NSUserTrackingUsageDescription` missing | Fixed by `IOSPostBuild.cs` |
| Build processes but never shows in TestFlight | Uploaded with Development Build on, or wrong bundle ID | Rebuild with Development Build off; check the ID |
| Could not launch on device, untrusted developer | First install from Xcode | Trust the certificate in iOS Settings |

## 8. Release checklist

- [ ] Version bumped, Build bumped
- [ ] Development Build unchecked, Target SDK = Device SDK
- [ ] Bundle ID in Unity = App Store Connect
- [ ] Exported to `Desktop/Sept/Builds/iOS-<version>`
- [ ] Opened `.xcworkspace`, all targets on automatic signing
- [ ] Any iOS Device selected, Product > Archive, Upload to App Store Connect
- [ ] Build visible in TestFlight, tested on a real phone
- [ ] Draft for this version exists under Distribution, build attached, submitted
- [ ] Git: changes committed and pushed, export folder not committed
