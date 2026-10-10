# Food Truck Cafe: manual for Claude

Read this first at the start of every session on this project. It says what the game is, the owner's rules, where everything lives, and how each recurring job is done. Deeper detail is in the docs it links to; this file is the map.

Last updated 9 October 2026.

## 1. State of the project

| Item | Value |
| --- | --- |
| Game | Food Truck Cafe, a portrait 2D bakery-truck game for iPhone and iPad |
| Engine | Unity 2022.3.50f1, C#, Unity Ads 4.17 (iOS SDK 4.19) |
| Owner | Betsy (GitHub Betsy215, developer name BetzzzGame) |
| Bundle id | `com.BetzzzGame.FoodTruckCafe` |
| App Store app id | 6755611355 |
| Apple team | XY39924J9T |
| Repo | https://github.com/Betsy215/Sept (**public**: never commit keys, issuer ids or tokens), branch `main` |
| Live version | 26.03.29 (March 2026) |
| In review | 26.10.09 build 4, submitted 9 October 2026, release type MANUAL (the owner presses Release after approval) |

What 26.10.09 added: 24 days (was 14), two more orders per day, evening and night tint for the last orders of a day, an interactive Level 0 tutorial, four shop regulars with habits (Grandma tips x2, Chef orders an extra item, Yoga lady has 1.5x time, Businessman tips x3 with 0.75x time), interstitial ads between a day and the Shop, and the fixes listed under Done in `Docs/TODO.md`.

Business picture (9 October): 55 lifetime downloads, all from App Store search, ad revenue in cents. The decision is to stay free and work on the store page and marketing, not more levels.

## 2. The owner's rules

These override any default habit.

1. **The game is live and works. Change code only for a real, confirmed bug.** No refactors, re-tuning or tidy-ups unless asked. Write ideas into `Docs/TODO.md` instead. The "Leave alone" list in `Docs/TODO.md` holds things the owner chose not to fix.
2. **No App Store submission or release without an explicit yes in the same conversation.** A TestFlight upload is fine when the owner asks for a build. Creating or submitting a store version, releasing, and changing pricing or availability each need a fresh yes.
3. **The owner reads the report, not the terminal.** The report is the Claude Doc "FoodTruckCafe Code Review" (https://claude.ai/code/artifact/726347e1-dd8c-42fd-917b-be80474ff9ae). Put substantive answers there and reply in chat with a line and the link. The report holds only open work; remove items once they are done. Its layout, set by the owner on 9 October: **Next** is always the first section and shows the step each of us is on right now (update it whenever the task changes); **MVP for the next release** comes right after it; then the later work, backlog and reference sections. How-to knowledge belongs in this repo, not in the report.
4. **Ask before anything outward-facing:** pushing a release, posting, sending messages, filling forms on Apple's site. Never type passwords; the owner signs in to App Store Connect and Unity themselves.
5. Commit after each working change with a message that says what was broken or what was added, and push to `origin main`. End commit messages with the Claude co-author line given in the session.
6. Keep the Godot game at Desktop/Godot separate. Never mix it with this one.

## 3. Where things are

| Path | What | In git |
| --- | --- | --- |
| `Desktop/Sept/FoodTruckCafe` | Unity project and repo | yes |
| `Desktop/Sept/Builds/iOS-<version>-<build>` | Xcode exports, one folder per build | no |
| `Desktop/Sept/Builds/archives` | `.xcarchive`s, archive and upload logs, `exportOptions-upload.plist` | no |
| `Desktop/Sept/Store/screenshots/raw` and `final` | Store screenshots: raw captures, then captioned finals (`iphone-0N`, `ipad-0N`, `duo-0N`) and `review-sheet.png` | no |
| `Desktop/Sept/Store/video` | Preview video: `frames/` (PNG dumps), `clips/`, `cut2.py`, `FoodTruckCafe_appstore_v2.mp4` (29.9 s, uploaded), `FoodTruckCafe_extended_cut_v2.mp4` (112.8 s, for social media) | no |
| `Desktop/Sept/Store/characters`, `reference` | Customer art from ChatGPT (raw, cut sprites, cast sheet) and reference poses | no |
| `FoodTruckCafe/Captures` | Editor screenshots from the MCP camera tool | no (gitignored) |
| `~/.private_keys/AuthKey_<id>.p8` and `asc.json` | App Store Connect API key and its key id and issuer id | never |

Copies of the store scripts that matter are kept in `Docs/tools` (see section 7), because the Store folder is not backed up by git.

## 4. Doc map

| File | Read when |
| --- | --- |
| `CLAUDE.md` | Always loaded. Coding rules and the offline compile command |
| `Docs/architecture.md` | Before touching gameplay code. Scene flow, singletons, systems, where to change things |
| `Docs/systems/*.md` | Before changing one system. Deep notes with file:line references (01 scenes, 02 levels/orders/customers, 03 food/drag/refill, 04 kitchen, 05 scoring/save/shop, 06 audio/UI/ads/iOS, 07 every string key and tunable) |
| `Docs/maintenance.md` | Scene wiring facts invisible in code, scoring, shop table, refill catalogue |
| `Docs/ios-testing-and-release.md` | Anything build or store related. Manual steps, errors seen, and the automated chain |
| `Docs/TODO.md` | Open work, Leave alone list, known minor issues, device test checklist |
| `Docs/tools/` | Scripts: `asc_token.py`, `asc_fill.py`, `preview_cut.py`, `store_caption.py` |

## 5. Working in the code

- Open the project in Unity Hub with 2022.3.50f1. Always press Play from `Assets/Scenes/MainMenu.unity`; the other scenes need the managers it creates.
- Compile without Unity: the csc command in `CLAUDE.md`. No output means it compiles. Unity batchmode fails while the editor has the project open.
- Strings are identifiers. Food, character and upgrade names are matched across scripts, scene data and shop items. Grep everywhere before renaming one.
- Serialized field names and three public Shop methods are wired in scenes. Add new fields rather than renaming old ones.
- Input is EventSystem-based, using 3D BoxColliders plus a PhysicsRaycaster in GameSceneOne. Don't use `OnMouse*` handlers or 2D colliders.
- Money is a float kept at whole cents (`SessionManager.RoundToCents`, `CanAfford`).
- Add a level: duplicate a `Assets/Entity/Level<N>Data.asset` and append it to `LevelManager.allLevels` in GameSceneOne. The last entry ends the game.
- Any new food needs an entry in the ScoreManager points table, or it scores 0.

## 6. Driving the Unity editor from Claude Code (MCP for Unity)

The bridge is at http://127.0.0.1:8080/mcp and starts with Unity. If it is unreachable, open Window, MCP for Unity, and click Start Server. The tools are deferred; load them with ToolSearch (`mcp__UnityMCP__...`). Main tools:

| Tool | Use |
| --- | --- |
| `manage_editor` | play, stop. Always stop play mode when finished |
| `execute_code` | Runs C# in the editor; the main way to set up and drive play-tests |
| `manage_camera` screenshot | Captures the Game view (pass `include_image=true` to see it) |
| `read_console` | Pass `include_stacktrace=false` and a `filter_text`; entries are huge otherwise |
| `refresh_unity` | After editing files on disk |
| `manage_build` | The iOS export (section 8) |

Proven recipes. Wrap each in `execute_code`, and guard for null if it runs right after play starts, because SessionManager is not ready for a frame or two.

- **Fresh game, skip the tutorial:** `var sm = SessionManager.Instance; sm.StartNewSession(); sm.MarkTutorialSeen(); SceneTransitionManager.Instance.TransitionToScene("GameSceneOne");`
- **Unlock things:** add strings to `sm.GetCurrentSession().purchasedFoodItems` or `.purchasedCharacters` before the transition. Defaults are Bread and Coffee, and Girl, Boy and Kid.
- **Start the day:** `FindObjectOfType<GamePhaseManager>().OnDoneButtonClicked()`. It saves the table and starts play.
- **Freeze the order timer:** `OrderSystem.freezeTimer = true`.
- **Jump to dusk or night:** set the private `OrderSystem.ordersCompleted` field by reflection to orders-minus-2 for dusk, or orders-minus-1 for night.
- **Serve an item:** call `OnPointerDown`, `OnPointerUp` and `OnPointerClick` on its `ServeableItem` with a `PointerEventData`.
- **Kitchen:** `KitchenSceneManager.Instance.OpenKitchen()`. Then bake bread by calling `OvenKitchenBase.OnPointerClick` on `GameObject.Find("oven2")` (ovens: oven1 Choux, oven2 Bread, oven3 Cake).
- **Move table items:** `ArrangeDemo.Instant(dict)` or `ArrangeDemo.Start(moves, framesPerMove, pause)` (`Assets/Editor/ArrangeDemo.cs`), during the arrangement phase only.
- **Hold a moment on screen** (a receipt popup, a pop-in): `Time.timeScale = 0.03f` to `0.25f`, then put it back to 1.
- **Full automated playthrough:** `AutoPlayTest.Start()` in play mode, then read `AutoPlayTest.Report` (`Assets/Editor/AutoPlayTest.cs`). It plays every day through the real handlers and collects errors.
- **Set the Game view resolution:** use the GameViewSizes reflection snippet in section 7. **Set it before entering play mode.** `TableLayer` sizes the tablecloth once in Awake, so a mid-play change leaves a phone-width cloth on iPad.

Editor quirks:
- Unity Ads shows a placeholder ad in play mode after Next Level (an object called "Placeholder" with Skip and Close). Closing it continues to the Shop.
- A recompile wipes editor-static state, such as a running ArrangeDemo or PreviewRecorder.
- Never run `git checkout` while Unity is exporting or compiling.

## 7. Store assets: screenshots, video, store page

**Screenshot sizes App Store Connect accepts** (verified 9 October 2026):

| Slot | API display type | Size | Game view preset name |
| --- | --- | --- | --- |
| iPhone 6.9 inch | `APP_IPHONE_67` | 1320 x 2868 | Store1320 |
| iPad 13 inch | `APP_IPAD_PRO_3GEN_129` | 2064 x 2752 | StoreiPad |
| iPhone Duo | `APP_IPHONE_DUO` | 2007 x 2853 (or 1398 x 2034) | DuoInner, DuoOuter |
| Preview video | `IPHONE_67`, `IPHONE_DUO` | 886 x 1920, at most 30 s | Preview886 |

**Game view size by reflection** (adds the preset if missing, then selects it). Change the name and size per slot:

```csharp
var asm = typeof(Editor).Assembly;
var sizesType = asm.GetType("UnityEditor.GameViewSizes");
var instance = typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null);
var group = sizesType.GetMethod("GetGroup").Invoke(instance, new[] { sizesType.GetProperty("currentGroupType").GetValue(instance) });
var groupClass = asm.GetType("UnityEditor.GameViewSizeGroup");
var texts = (string[])groupClass.GetMethod("GetDisplayTexts").Invoke(group, null);
int idx = System.Array.FindIndex(texts, t => t.Contains("Store1320"));
if (idx < 0) {
    var sizeType = asm.GetType("UnityEditor.GameViewSize"); var kind = asm.GetType("UnityEditor.GameViewSizeType");
    var size = sizeType.GetConstructor(new[] { kind, typeof(int), typeof(int), typeof(string) })
        .Invoke(new object[] { System.Enum.Parse(kind, "FixedResolution"), 1320, 2868, "Store1320" });
    groupClass.GetMethod("AddCustomSize").Invoke(group, new[] { size });
    idx = (int)groupClass.GetMethod("GetTotalCount").Invoke(group, null) - 1;
}
var gvType = asm.GetType("UnityEditor.GameView"); var gv = EditorWindow.GetWindow(gvType);
gvType.GetProperty("selectedSizeIndex", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic).SetValue(gv, idx);
```

**Screenshots:**
1. Set the size and press Play.
2. Set up the scene with the recipes in section 6 and capture with `manage_camera`. Raw captures go to `Captures/store`, then `Store/screenshots/raw`.
3. Caption each one: `python3 Docs/tools/store_caption.py raw.png out.png "Caption" W H`. It adds a cream band at the bottom in the game font (beachday.otf).

The owner's choices for the 26.10.09 set:
- Girl by day and Grandma by night. Don't use the Yoga girl.
- Three or four foods on the table, except one shot with every item in a rearranged layout.
- Five shots, in this order: serve, kitchen, full table with receipt, night, shop.
- Captions:
  1. "Serve coffee and pastries"
  2. "Bake fresh bread in your kitchen"
  3. "Unlock treats and earn big tips"
  4. "Play from morning to night"
  5. "Unlock new foods and regulars"

**Preview video:**
1. Record with `PreviewRecorder.Start(folder, 30)` (`Assets/Editor/PreviewRecorder.cs`). It sets `Time.captureFramerate`, so every frame is a smooth 1/30 s step however slow the editor is. Call `PreviewRecorder.Stop()` when done.
2. Composite with `Docs/tools/preview_cut.py`, the same as `Store/video/cut2.py`. It needs PIL and ffmpeg (use `-fps_mode passthrough`, not `-vsync`).
   - A list of Shot objects sets frame range, zoom, captions and the transition (cut, xfade, dip, iris, slide).
   - Music is `cute-music-26476.mp3`, mixed with the game's own sounds.

Apple's rule (guideline 2.3.4): in-app footage only. Captions, overlays and music are allowed. Device frames, marketing cards and footage not from the game are not.

**Store page text for 26.10.09:** the constants `DESCRIPTION`, `KEYWORDS`, `SUBTITLE` and `NOTES` in `Docs/tools/asc_fill.py`. The owner edited the promo text and the What's New in the browser afterwards, so App Store Connect holds the final wording. Run `asc_fill.py status` to read the lengths.

App Privacy has no API and is set in the browser. Ten data types are declared from Unity Ads' privacy survey. Device ID is linked and used for tracking. Product Interaction, Advertising Data, Other Usage Data, Performance Data and Other Data Types are linked but not used for tracking. The other declared types are in App Store Connect.

## 8. Building and releasing (summary)

The full steps, commands and error fixes are in `Docs/ios-testing-and-release.md`, section "Automated chain from Claude Code". In short:

1. **Bump the version and build** in `ProjectSettings/ProjectSettings.asset`: `bundleVersion`, and `buildNumber` under `iPhone:`. Every upload needs a new build number; a new store version needs a new version string. Currently 26.10.09 (4).
2. **Export** with MCP `manage_build`, target ios, to `Builds/iOS-<version>-<build>`. Play mode must be off. Success shows in `~/Library/Logs/Unity/Editor.log` as "Build Finished, Result: Success."
3. **Archive** with `xcodebuild ... archive`, keeping `ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES=NO`. It takes 10 to 15 minutes; run it in the background.
4. **Upload** with `xcodebuild -exportArchive` and `Builds/archives/exportOptions-upload.plist`. This uses the Apple ID signed into Xcode.
5. **Wait for processing:** poll the builds endpoint until the state is `VALID`, usually 10 to 20 minutes. The internal TestFlight group gets it automatically.
6. **Stop here** unless the owner says to go further. The store version (`asc_fill.py texts|build|screenshots|preview|duo`, after updating the ids at the top for the new version) and the Submit and Release buttons all need their go.

**App Store Connect API:** `TOKEN=$(python3 -I Docs/tools/asc_token.py)`. The script reads the key id and issuer from `~/.private_keys/asc.json` and the key from `~/.private_keys/AuthKey_<id>.p8`. Base URL `https://api.appstoreconnect.apple.com/v1`; URL-encode the brackets in filters. The token lasts 15 minutes.

**For the next version:** create the version in App Store Connect (with the owner's go). Then read the new version, localization, review detail and app info localization ids from the API (`/apps/6755611355/appStoreVersions`, then the relationships), and put them at the top of `asc_fill.py`.

## 9. Browser work (Claude in Chrome)

The owner sometimes asks for help on App Store Connect pages.
- Load the Chrome tools in one ToolSearch call and open a new tab.
- On App Store Connect, each App Privacy dialog publishes its data type on its own.
- If the session lands on the login page, stop and ask the owner to sign in. Never type credentials.
- After two or three failed clicks, stop and report what happened.

## 10. Open work

The report's Next, MVP and Backlog sections are the live list. MVP for the next release (owner's list, 9 October; the owner will explain each): daily check-in rewards; economy balance (coins, stars, shop prices: three stars achievable, an always-3-star player unlocks everything about two-thirds through, an always-2-star player near the end); a level map; more equipment or items for the empty kitchen and counter slots. Other open work as of 9 October:
- Press Release after approval (the owner).
- Marketing research and plan.
- The App Store header and search-results banner.
- An in-game rating prompt after Day 3.
- A one-page marketing site.
- A TikTok plan (the owner creates the account).
- New art: Chef, Yoga and Businessman need distinct faces and poses; Girl and Grandma need full-body versions. ChatGPT's image quota resets 10 October, 1:17 PM.
- Unity Ads payout profile.
- Merge Unity's SKAdNetwork list into `IOSPostBuild.cs`.
- Review the analytics in November.

Minor known issues are in `Docs/TODO.md`. Don't fix them without the owner's yes.

## 11. Keeping this manual current

When a session learns something a future session would need (a new recipe, a path, a decision by the owner, a store rule), add it here or in the linked doc and commit it. Change the "Last updated" date. Remove anything that stops being true.
