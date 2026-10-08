# TODO and suggestions

Status as of 7 October 2026. Done items are kept for a while so the history is visible.

## Done on 7 October 2026

- [x] Committed the kitchen scene, levels 5 to 14, three prefabs, audio and art that had never reached GitHub
- [x] Upgrade levels survive app restarts (were silently dropped from the save file)
- [x] Play asks for a second tap before erasing a saved game
- [x] Food cannot be served while paused or behind a popup; a hold-to-refill no longer also serves
- [x] Music: menu music restarts on return, mute/unmute resumes the right track, shop music stops when leaving to the menu
- [x] Rewarded ad button retries failed loads and only claims a reward when coins were added; coins kept at whole cents
- [x] Orders: perfect-order sound reachable, last customer finishes its walk-out, Kid orders clamped to the level maximum, LevelData minimum no longer overridden
- [x] Shop: pay before record with refund on failure, visible failure and success messages, upgrades capped at level 3
- [x] Level-complete money count-up no longer double-counts tips
- [x] Kitchen timers cannot double-fire when the kitchen is reopened mid-bake; pop effects play before items hide
- [x] iOS post-build writes the tracking usage description and the export-compliance key
- [x] Debug logs filtered out of release builds
- [x] Deleted 83 unreferenced images, audio clips, prefabs and template leftovers (10.9 MB)
- [x] Five 2048 px Choux sprites capped at 1024 px (about 7 MB off the app); music clips set to streaming at 70 percent quality (about 10 MB off the app, about 80 MB less RAM)
- [x] Stale SampleScene and Menu entries removed from Build Settings; level names no longer have a double space
- [x] Days 10 to 14 re-tuned: orders 9, 9, 10, 10, 11 with star thresholds rising 50 per day from 450/550/650 to 650/750/850
- [x] iOS build number set to 1 in Player Settings (bundle ID was already set)
- [x] Second pass (7 October, evening): refill holds cannot serve or be hijacked by a second finger; coffee upgrades apply regardless of start order; the coffee machine uses EventSystem clicks and respects pause; kitchen open/close guarded against double taps; new game clears kitchen timers; drags owned by one finger and ended on phase switch; table bounds use the real safe-area rect; scene transitions always reset; shop buttons no longer fire twice; purchase popup blocks the buttons beneath it; camera far clip moved off the UI plane

## In progress

- [ ] **Interstitial ads between level and Shop.** Code is in (`InterstitialAdService`, called from `LevelManager.LoadNextLevel`), shows after every level by default. Needs the `Interstitial_iOS` ad unit created on the Unity Ads dashboard, then a device test: finish Day 1, tap Next Level, the ad plays, the Shop opens after it or after skip. Also confirm the Shop opens with no delay when offline.
- [ ] **Refill review.** Catalogue each food item's counts, timers, hold-to-refill flag and kitchen source (see Docs/maintenance.md, Refill system) before deciding whether hold-to-refill should be enabled on more than Melon.

## Leave alone (owner's decision, 7 October 2026)

The game is live and working. These were reviewed and the owner chose not to change them. Do not "fix" them.

- Main menu music set to None in the MainMenu scene (silent menu is accepted).
- Unused packages (Visual Scripting, XR Management, Mobile feature set, 2D feature set extras, Timeline): keep, they may be needed for the Xcode export.
- `Assets/Plugins/iOS/NSUserTrackingUsageDescription.plist`: leave.
- Choux sprite cropping: cannot be tested on device yet; stays below under Someday.
- MP3 sound effects re-encoded to Vorbis: fine as is.
- Debug flag defaults: leave.
- Buttons under the notch and home indicator: leave for now. `SafeAreaFitter` exists if this changes.
- AudioManager prefab vs scene overrides: fine, not every clip is meant to be wired.
- Hold-to-refill enabled only on Melon: do not change until each food item's refill behaviour has been reviewed and understood in detail (see Docs/maintenance.md, refill notes).
- Small scene leftovers (missing script on MainMenu Canvas, dead Inspector call on kitchen back button, silent pause button, four unreferenced scripts): leave unless they break something.
- Upgrade shop items are one-shot by design (verified: see maintenance notes).

## Packages to remove (do this in Unity, Window > Package Manager)

Removing editor-only packages changes nothing in the app; the first three do shrink the iOS build.

| Package | Why | Build effect |
| --- | --- | --- |
| Visual Scripting | Never used; the one `using` line is gone | Removes several MB of runtime assemblies |
| Mobile feature set (com.unity.feature.mobile) | Pulls Mobile Notifications, which asked for notification permission on first launch, plus Adaptive Performance which is inert. The launch prompt is now off, but the plugin still ships. After removing, delete `Assets/Editor/com.unity.mobile.notifications` and `Assets/Adaptive Performance` | Under 2 MB and one less permission prompt |
| XR Management | No XR anywhere; it was set to initialise on start (now off). After removing, delete `Assets/XR` | About 1 MB and a faster start |
| 2D feature set, replace with 2D Sprite + 2D Pixel Perfect | Only Pixel Perfect is used; the feature set drags in Burst, Collections, Sprite Shape and Tilemap | A few MB |
| Timeline | No timelines in the project | Small |
| Muse Chat, Muse Sprite, Collaborate, Device Simulator devices, VS Code IDE 1.2.5 | Editor-only tools not in use (keep mcp-unity if you use it with Claude) | None, faster editor import |

Keep: Unity Ads, Unity Ads iOS Support, TextMeshPro, uGUI, 2D Pixel Perfect, and `Assets/MobileDependencyResolver` (it generates the Podfile that pulls the Unity Ads framework into Xcode).

## Scene edits needed in the Unity Editor (code cannot do these)

Found by the scene wiring audit. Each is a few clicks in the Inspector.

- [ ] **Buttons under the notch and home indicator.** On iPhones with a Dynamic Island, the pause button (60 to 180 px from the top in the 1080 x 1920 design), the settings button, the Shop's Next Level and Get 5 Coins buttons (bottom 400 px), and the kitchen back button all sit in the inset zones. Fix: add an empty full-stretch panel under each Canvas, put the new `SafeAreaFitter` component on it, and re-parent those buttons under the panel. No other change needed.
- [ ] **Apply the AudioManager overrides to the prefab.** The prefab asset has only seven clips; shop music, arrangement music, the gameplay playlist, walk-in, wrong-item, money sounds and volume 0.4 exist only as overrides on the MainMenu scene instance. Select that instance, Overrides, Apply All. Until then, GameSceneOne launched directly in the editor plays no music.
- [ ] **Main menu music and level-complete music are set to None** on the MainMenu scene instance (see Needs a decision).
- [ ] **Missing script on MainMenu's Canvas.** A disabled component with an unknown script GUID sits on Main Camera/Canvas. Remove it (Inspector shows "Missing (Mono Script)").
- [ ] **Hold-to-refill is off on every item except Melon**, and no refill bar objects exist, so the "Hold on item to refill" tutorial text is wrong for most foods. Either enable Enable Hold To Refill on each RefillableItem and add a RefillBar child, or change the tutorial text.
- [ ] **Kitchen back button has a dead Inspector call** (target None). Harmless; the code wires it. Remove the entry for tidiness.
- [ ] **PauseButton has no AudioSource or clips**, so it is silent. Add a source and the compress/uncompress clips if you want the click sound.
- [ ] **Tablecloth sits flush to the screen bottom**, under the home indicator. The drag bounds now respect the safe area, but the cloth art does not move. Lift it if you want a visible margin.
- [ ] **Four scripts are not used by any scene**: KitchenButtonGlowController, CloseKitchenButtonGlowController, ButtonGlowEffect, StartButtonScript. Wire them up or delete them.
- [ ] **Player Settings**: Scripting Backend and Architecture are not pinned (defaults resolve to IL2CPP and ARM64, which is fine); Metal API Validation is on, which only affects development builds. Android and Standalone bundle IDs still carry the template value; harmless for iOS.

## Code and project clean-up

- [ ] `Assets/Plugins/iOS/NSUserTrackingUsageDescription.plist` is never merged into Info.plist (Unity copies loose plists as resources). Its SKAdNetwork list is redundant with what the Unity Ads iOS Support package adds (77 IDs were present in the March export). Delete it to avoid confusion.
- [ ] `StartArrangementPhase` runs twice per level load (LevelManager and GamePhaseManager). Harmless now that it is idempotent, but one call should go.
- [ ] `ScoreManager` and `SessionManager` both hold money-related state. Level score and stars live in ScoreManager; coins live in SessionManager. Fine for now, but any new money feature should go through SessionManager only.
- [ ] Food, character and upgrade names are string-matched across scripts, scene data and shop items. A rename in one place breaks purchases silently. A shared `FoodType` constants class would make this safer.
- [ ] Git history is 722 MB because the Library folder was committed early on. Only worth rewriting if you clone the repo again.

## Someday (only with device testing available)

- [ ] Crop the Choux source art (`Untitled_Artwork 5` to `9`, 2048 x 2048 with the drawing in a 1259 x 1018 area) so it can sit at 512 px. Changes on-screen placement, so it needs a device check.

## Testing checklist for the next build

- [ ] Fresh install: tracking prompt appears once; no notification prompt
- [ ] Buy an upgrade, kill the app, reopen: upgrade still owned
- [ ] Tap Play with a saved game: warning appears, second tap starts over
- [ ] Pause mid-level and tap food beside the pause panel: nothing is served
- [ ] Hold a food item to refill: it refills and does not shake or serve on release
- [ ] Finish a level: last customer walks out, Today Sale counts each amount once
- [ ] Shop: try to buy with too few coins, message is visible; buy an item, coins drop once
- [ ] Watch a rewarded ad: coins added and shown; airplane mode: button shows Loading then retries
- [ ] Return to main menu from the shop: shop music stops
- [ ] Open and close the kitchen mid-bake: the bake completes once
