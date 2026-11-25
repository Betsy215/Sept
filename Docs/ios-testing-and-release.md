# Release guide: test builds, TestFlight and the App Store

Our release process, step by step, as used since 18 November 2026. Written for Claude Code driving the open Unity
editor through the bridge; the owner only signs in, tests and says yes. Refreshed 24 November 2026.

| Item | Value |
| --- | --- |
| Unity | 2022.3.50f1, iOS target 15.0, iPhone and iPad, portrait |
| Bundle id / app id / team | `com.BetzzzGame.FoodTruckCafe` / 6755611355 / XY39924J9T |
| Exports, archives, logs | `Desktop/Sept/Builds/iOS-<ver>-<n>`, `Builds/archives/` (`FoodTruckCafe-<ver>-<n>.xcarchive`, `archive-*.log`, `upload-*.log`, `exportOptions-upload.plist`) |
| API key | `~/.private_keys/AuthKey_<id>.p8` and `asc.json` (key id, issuer). **Never** copy them or their values into the repo (it is public) |
| Tools | `Docs/tools/test_build.sh`, `Docs/tools/asc_release.py`, `Docs/tools/asc_token.py`, `Docs/tools/asc_media_r33.py` (newest media script), recipes `Docs/tools/recipes/setver.cs`, `getver.cs` |

## The rules

- **Test builds** go up when the owner asks for one ("build", "push it to a test build"). Fine without further asking.
- **Anything on the store** (create a version, submit, release, What's New, description, promotional text,
  screenshots, header, pricing, availability) needs the owner's explicit yes in the same conversation, each time.
- **Version rule** (owner, 18 November 2026): version = the build day as `YYYY.MM.DD`, build `0, 1, 2` within the day,
  back to 0 the next day. Keep the four-digit year (old `26.11.22`-style versions sort above two-digit-year dates).
  A version that went live closes its train: Apple takes no more builds of it, so the next test build takes the next
  day's date ("Invalid Pre-Release Train ... closed" in the upload log means exactly this).

## 1. A test build (about 5 to 10 minutes)

1. **Set the version** in `Docs/tools/recipes/setver.cs` (edit `Version` and `Build`), then
   `python3 Docs/tools/unity_mcp.py exec Docs/tools/recipes/setver.cs`.
2. **Check it**: `python3 Docs/tools/unity_mcp.py exec Docs/tools/recipes/getver.cs` returns e.g. `2026.11.24 (1)`.
   Play mode must be off (the script stops it anyway).
3. **Build**: `bash Docs/tools/test_build.sh <ver>-<n>` (e.g. `2026.11.24-1`), run in the background. It exports from
   the open editor through the bridge (`unity_mcp_long.py` + `build_in_editor.cs`; the call may answer
   `success:false` early, the `Builds/export-<ver>-<n>.result` file is the truth), prints the Info.plist version,
   archives with xcodebuild (`ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES=NO` is required) and uploads with
   `-exportArchive`. Incremental export under a minute, archive about 3 minutes. `... <ver>-<n> upload` repeats only
   the upload for an archive that exists.
4. **Reset the export's change to ProjectSettings**: the export writes an entry into `preloadedAssets:` in
   `ProjectSettings/ProjectSettings.asset`. Set that line back to `  preloadedAssets: []` by hand. **Never**
   `git checkout ProjectSettings/ProjectSettings.asset`: it reverts the version too (the comment inside
   `test_build.sh` still suggests the checkout; ignore it).
5. **Commit and push**: `Version <ver> (<n>), test build: <what is in it>`, then `git push origin main`.
6. **Processing**: Apple mails "ready to test" in 5 to 20 minutes, or poll
   `GET /v1/builds?filter[app]=6755611355&sort=-uploadedDate` (token from `python3 Docs/tools/asc_token.py`;
   `asc_release.py <ver> status` lists the newest builds) until `VALID`. The internal TestFlight group gets it
   by itself. Tell the owner plainly when a build they asked for did not reach TestFlight.
7. **The owner tests on TestFlight.** TestFlight builds show Unity test ads (sandbox receipt check,
   `Assets/Plugins/iOS/TestFlightCheck.mm`, `AdsInitializer.IsTestFlightBuild`), skip the between-days ad, report
   analytics to the `development` environment, and show the "Chapter 2 test" button on the main menu
   (`Chapter2TestButton`). The owner's TestFlight device id (memory note `unity-analytics`) is left out of every
   player count.

Upload failures seen: "Failed to Use Accounts" or "session has expired" = the Apple ID in Xcode signed out (only the
owner can sign in: Xcode > Settings > Accounts; the archive stays good, rerun with `upload`); "PLA Update available"
= the owner must accept Apple's new agreement; "Invalid Pre-Release Train" = the version is closed (see the rules).
The API key cannot replace the Xcode sign-in (the distribution certificate is cloud-managed).

If the bridge is down: delete `Docs/tools/.mcp_session` and retry; if it still fails the owner clicks Start in
Window > MCP for Unity. With the editor closed, the batchmode export still works:
`Unity -batchmode -quit -projectPath . -buildTarget iOS -executeMethod CommandLineBuild.iOS -exportPath ../Builds/iOS-<ver>-<n> -logFile ../Builds/export-<ver>-<n>.log`,
then `test_build.sh` steps by hand (or set the version in `ProjectSettings.asset`: `bundleVersion`, `buildNumber: iPhone:`).

Disk: each build adds about 1.5 GB (export, archive, DerivedData). After a build is VALID keep the newest export and
the archives of store-released builds only.

## 2. A store release (owner's yes first)

1. **Texts**: put the owner's own words into the constants at the top of `Docs/tools/asc_release.py`: `WHATS_NEW`,
   `PROMO_TEXT` ("" keeps the live one), `DESCRIPTION_EDITS` (`{"old words": "new words"}`, only on the owner's word).
   Keep earlier What's New texts as `WHATS_NEW_<date>` lines for history.
2. `python3 -I Docs/tools/asc_release.py <ver> create`: creates the version (MANUAL release), copies the live
   version's texts with the edits and sets What's New. App Store Connect copies the screenshots and previews over.
3. `python3 -I Docs/tools/asc_release.py <ver> build <n>`: attaches a `VALID` build of that version.
4. `python3 -I Docs/tools/asc_release.py <ver> whatsnew <file>`: only if the owner's words change after `create`.
5. `python3 -I Docs/tools/asc_release.py <ver> auto`: release by itself once approved (AFTER_APPROVAL), when the owner
   wants that (they did for 2026.11.20 and 2026.11.24). Without it the version waits for `release`.
6. `python3 -I Docs/tools/asc_release.py <ver> submit`: creates (or reuses) the review submission and submits.
7. **Watch** with `asc_release.py <ver> status` (WAITING_FOR_REVIEW, IN_REVIEW, PENDING_DEVELOPER_RELEASE,
   READY_FOR_DISTRIBUTION). Review took hours to a day so far. A manual-release version goes out with
   `asc_release.py <ver> release`. Once live, the date's train is closed: the next test build takes the next date.
8. Update MANUAL section 1 (Live / In review) and the report.

New screenshots or preview (owner's yes): capture with the editor (MANUAL section 7, `capture_r33.sh` is the newest
set), compose into `Store/screenshots/final_<rNN>`, then copy the newest `Docs/tools/asc_media_rNN.py`, set `VERSION`,
`SHOTS`, `VIDEO`, and run `status`, `screenshots`, `ipad`. Sizes: iPhone 6.9-inch `APP_IPHONE_67` 1320 x 2868, iPad
13-inch `APP_IPAD_PRO_3GEN_129` 2064 x 2752, preview 886 x 1920, 15 to 30 s of in-game footage (14.5 s failed
processing). iPhone Duo screenshots are required from April 2027 (`asc_fill.py duo`).

Header and other page assets (the store page header since 24 November 2026): there is no API. The owner uploads the
picture to the **Asset Library** in the App Store Connect web UI and submits it for review; once Apple approves it, on
the version page: **Header > Browse Assets > pick it > Next > Publish**.
**The header does not carry over to a new version** (2026.11.24 went live on 25 November 2026 with an empty slot).
Owner, 25 November 2026: keep the header on every release. So, every release: after `create`, set the header on the
new version page (Header > Browse Assets > `header_E3_upload.png` > Next); and on the day the version goes live, open
the live version page and check "1 of 1 Header Asset", else pick it again and Publish. This standing request is the
owner's yes for re-selecting the SAME approved header; a different header still needs their yes.

Things with no API, done by the owner in the web UI: App Privacy (the label: Usage Data / Product Interaction, not
linked, not tracking, for Unity Analytics), pricing and availability (China mainland is off sale since
18 November 2026; Hong Kong, Macau and Taiwan stay), agreements, tax and banking.

Pull a submitted version back for an edit: `PATCH reviewSubmissions/<id>` with `canceled: true`, then submit again.
In-app purchases: Remove Ads (non-consumable, US$1.99) and 15 Stars (consumable, US$0.99) exist; `Docs/tools/asc_iap.py`
made them. A new purchase goes in with a version through the web UI's draft submission.

## 3. Release checklist

- [ ] Owner asked for the build; version is today's date, next free build number (`getver`)
- [ ] `test_build.sh` printed the right version and "Upload succeeded" / "EXPORT SUCCEEDED"
- [ ] `preloadedAssets: []` restored by hand; version commit pushed
- [ ] Build `VALID`, owner tested it on TestFlight
- [ ] Owner's yes for the release, their What's New words in `asc_release.py`
- [ ] `create`, `build <n>`, (`auto`), `submit`; status watched; MANUAL section 1 and the report updated

## 4. Background

- `Assets/Editor/IOSPostBuild.cs` writes `NSUserTrackingUsageDescription` and `ITSAppUsesNonExemptEncryption = NO`
  into Info.plist on every export; `Assets/Editor/CommandLineBuild.cs` is the batchmode entry.
- Unity Ads 4.17 comes through CocoaPods; the archive uses `Unity-iPhone.xcworkspace`, never the `.xcodeproj`.
- Signing is automatic with team XY39924J9T. Errors from the Xcode UI era (March 2026): an unsigned target = automatic
  signing on all targets; "bundle version must be higher" = bump the build; Missing Compliance = the post-build key.
- `Docs/tools/asc_fill.py` (texts, build, screenshots, preview, duo, status) is the older version filler; ids at the top.
- `Docs/tools/preview_cut.py` composes preview videos (frames from `Assets/Editor/PreviewRecorder.cs`).
