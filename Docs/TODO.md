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
- [x] Oven centre taps did nothing: each oven's bread-note sticker caught the tap and the oven rejected it (live since March). Fixed in code
- [x] Finishing Day 14 and leaving the popup early left a broken save (Continue then looped Day 1 with purchases failing). The session now completes when the last order ends; old broken saves are treated as finished
- [x] Pause blocked during the 3 s between the last order and the level-complete popup
- [x] GamePhaseManager food list no longer depends on Start order (a wrong order would have made every tap fail for a day)
- [x] Tracking (ATT) prompt now waits for the app to be active; it never showed in the March build and would have shown late with the new plist key
- [x] Cake and Choux awarded 0 points when served (missing from the ScoreManager points table, fallback 0). Now 35 each
- [x] Refill catalogue completed for every food item; recommendation recorded in Docs/maintenance.md (do not widen hold-to-refill)
- [x] Days 10 to 14 re-tuned: orders 9, 9, 10, 10, 11 with star thresholds rising 50 per day from 450/550/650 to 650/750/850
- [x] iOS build number set to 1 in Player Settings (bundle ID was already set)
- [x] Second pass (7 October, evening): refill holds cannot serve or be hijacked by a second finger; coffee upgrades apply regardless of start order; the coffee machine uses EventSystem clicks and respects pause; kitchen open/close guarded against double taps; new game clears kitchen timers; drags owned by one finger and ended on phase switch; table bounds use the real safe-area rect; scene transitions always reset; shop buttons no longer fire twice; purchase popup blocks the buttons beneath it; camera far clip moved off the UI plane

## Direction (decided 9 October 2026)

Lifetime: 55 downloads since 29 Nov 2025, all from App Store search, 0.9 percent page conversion, ad revenue in cents. The game is undiscovered, not under-monetised. Stay free, ship the interstitial, no paid version or Remove Ads purchase until downloads reach a few hundred a month. Next work is the store page and marketing, not more levels.

## Next

- [x] Days 15 to 24 added (9 Oct), see Docs/systems/02 for the table
- [ ] Ship 26.10.09 (fixes + interstitial): TestFlight phone test, App Privacy tracking declaration, then submit on the owner's explicit go
- [ ] Next build (branch feature/tutorial-level, 9 Oct): interactive Level 0 tutorial with arrow, two more customers per day with star thresholds scaled, evening and night tint (main scene and kitchen window). Editor-tested by Claude and the owner; needs a phone test, then merge and TestFlight on the owner's go
- [ ] App preview video for the store page: storyboard and shot list (Claude), gameplay capture (owner or editor), edit in iMovie or CapCut; 15 to 30 s portrait, Apple specs per device size
- [ ] Marketing discovery: plan with cost and effort for keyword work, Apple Search Ads test, short-video clips, cozy-game communities, Apple featuring request
- [ ] Store page text: title, subtitle, keyword field
- [ ] Re-read retention and conversion in November, then decide on content

Backlog from the owner, 9 October (after the goals above):

- [x] More default customers and bonus customers: done 9 Oct (Girl/Boy/Kid default; Grandma, Chef, Yoga, Businessman in the shop with bonus lines). Open: regenerate Chef/Yoga/Businessman with distinct faces and poses, and full-body Girl/Grandma keeping their poses (ChatGPT quota resets 10 Oct 1:17 PM); add the Businessman card art review
- [ ] Main scene visual pass: more detail, more harmonious palette (kitchen stays as is). Ideas with mock-ups
- [ ] Marketing plan and app page improvements; TikTok account is the owner's to create, Claude writes the plan and captions
- [ ] Country breakdown of downloads and play (App Store Connect Territory filter, Unity Ads country view) into the report

## Backlog (needed, no rush)

- [ ] Unity Ads payout profile
- [ ] Merge Unity's SKAdNetwork ID list into IOSPostBuild.cs (dashboard reports missing IDs)
- [ ] Paid Apps Agreement, bank and tax forms (only before adding a purchase; approval takes days)
- [ ] Remove Ads purchase (later)
- [ ] Interstitial frequency review once retention data exists
- [ ] Write the automated release chain into Docs/ios-testing-and-release.md after the first full release

## In progress

- [ ] **Interstitial ads between level and Shop.** Code is in (`InterstitialAdService`, called from `LevelManager.LoadNextLevel`), shows after every level by default. Needs the `Interstitial_iOS` ad unit created on the Unity Ads dashboard, then a device test: finish Day 1, tap Next Level, the ad plays, the Shop opens after it or after skip. Also confirm the Shop opens with no delay when offline.

## Leave alone (owner's decision, 7 October 2026)

The game is live and working. These were reviewed and the owner chose not to change them. Do not "fix" them.

- Main menu music set to None in the MainMenu scene (silent menu is accepted).
- Unused packages (Visual Scripting, XR Management, Mobile feature set, 2D feature set extras, Timeline): keep, they may be needed for the Xcode export.
- `Assets/Plugins/iOS/NSUserTrackingUsageDescription.plist`: leave. (It never reaches the Xcode export at all; harmless.)
- Choux sprite cropping: cannot be tested on device yet; stays below under Someday.
- MP3 sound effects re-encoded to Vorbis: fine as is.
- Debug flag defaults: leave.
- Buttons under the notch and home indicator: leave for now. `SafeAreaFitter` exists if this changes.
- AudioManager prefab vs scene overrides: fine, not every clip is meant to be wired.
- Hold-to-refill enabled only on Melon, and Melon is an inactive object no player can reach: reviewed 7 October, recommendation is to leave it (redundant with the kitchen, and it would need refill-bar art and tutorial text). Details in Docs/maintenance.md.
- Orphan entries Pie, Mont Blanc and Log Cake in the ScoreManager points table: harmless, no item uses those names.
- Kitchen bakes continuing through pause and into the Shop: logs an error when a bake lands in the Shop, but no player impact because levels start full.
- Small scene leftovers (missing script on MainMenu Canvas, dead Inspector call on kitchen back button, silent pause button, four unreferenced scripts): leave unless they break something.
- Upgrade shop items are one-shot by design (verified: see maintenance notes).

## Unused packages (reference only: the owner chose to keep them, see Leave alone)

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

## Scene edits found by the audit (reference: most are on the Leave alone list)

Found by the scene wiring audit. Each is a few clicks in the Inspector.

- [ ] **Buttons under the notch and home indicator.** On iPhones with a Dynamic Island, the pause button (60 to 180 px from the top in the 1080 x 1920 design), the settings button, the Shop's Next Level and Get 5 Coins buttons (bottom 400 px), and the kitchen back button all sit in the inset zones. Fix: add an empty full-stretch panel under each Canvas, put the new `SafeAreaFitter` component on it, and re-parent those buttons under the panel. No other change needed.
- [ ] **Apply the AudioManager overrides to the prefab.** The prefab asset has only seven clips; shop music, arrangement music, the gameplay playlist, walk-in, wrong-item, money sounds and volume 0.4 exist only as overrides on the MainMenu scene instance. Select that instance, Overrides, Apply All. Until then, GameSceneOne launched directly in the editor plays no music.
- [ ] **Main menu music and level-complete music are set to None** on the MainMenu scene instance (see Needs a decision).
- [ ] **Missing script on MainMenu's Canvas.** A disabled component with an unknown script GUID sits on Main Camera/Canvas. Remove it (Inspector shows "Missing (Mono Script)").
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

## Known issues

- On tall phones (iPhone 6.9 inch, 1320 x 2868) the Shop's "EARNED: $ 1320.00" label wraps onto two lines once earnings reach four digits (seen in the editor at that size on 9 Oct). Cosmetic; fix is a smaller font or auto-size on that text., documented and not fixed

Found in the deep review of 7 to 8 October. Each is functional but minor, or a judgement call. Details and file:line references are in Docs/systems.

- Double-tapping Next Level while an interstitial is pending can run the Shop transition twice (the Shop slide-in replays). Fix: disable the button on the first tap. (01, 06, 07)
- "Unlocked Food" on the level-complete popup counts against `totalFoodItems: 8` on the MainMenu SessionManager; only 6 foods exist, so it never reaches 0. Fix: set it to 6. (01, 05, 07)
- Leaving a level through Pause, Main Menu (or killing the app) keeps the coins earned so far and the day replays, so coins can be farmed; Restart rolls them back. Design choice. (02, 05)
- Returning players from 26.03.29 lost bought coffee upgrades on every app restart in that build (it never saved them). The new build saves them, but those players see the upgrades for sale again. No fix possible; nothing was saved. (05)
- The level-complete count-up shows whole dollars while the rest of the game shows cents, and the labels change wording mid-animation. Cosmetic. (02)
- The item-served sound plays twice per correct serve; on perfect orders two identical clips stack. (02)
- Bread and Cake reject a wrong tap silently (Use Audio Manager is off on both); other foods buzz. (03)
- The coffee machine's table position is not saved; food is not overlap-checked against the machine, only the machine turns red. (03)
- Shop Next Level briefly plays gameplay music before arrangement music. (01, 06)
- Play and Continue button clicks ignore Sound Off. (06)
- Kitchen bake timers carry over through Restart and Main Menu; the oven ding can play in the Shop. Harmless. (04)
- Rewarded ad status labels ("Loading ad...", "+$5 coins added!") are unassigned in the Shop scene, so they never show. (05)
- `drag.anim` has an animation event with an empty function name; the editor logs an error every 4 s while the old scene TutorialPanel hand animation plays. (02, 07)
- Several clips were deleted before March and are unassigned (button click, item pickup, wrong item, level win). Leave-alone list covers AudioManager clips. (06, 07)
- Settings panel animator runs on scaled time, so it freezes while paused. (06)
- The iPhone ringer switch mutes the game (Ambient audio session). Standard for casual games. (06)

## Someday (only with device testing available)

- [ ] Crop the Choux source art (`Untitled_Artwork 5` to `9`, 2048 x 2048 with the drawing in a 1259 x 1018 area) so it can sit at 512 px. Changes on-screen placement, so it needs a device check.

## Testing checklist for the next build

- [ ] Fresh install (delete the app first): the tracking prompt appears over the menu within a second or two of launch
- [ ] Kitchen: tap the middle of each oven window (the bread note); it starts baking
- [ ] Finish Day 14 and tap Main Menu while the money is still counting: Continue is greyed out, Play starts fresh
- [ ] Day 2 onwards: table positions load and every food can be served

- [ ] Fresh install: tracking prompt appears once; no notification prompt
- [ ] Buy an upgrade, kill the app, reopen: upgrade still owned
- [ ] Tap Play with a saved game: warning appears, second tap starts over
- [ ] Pause mid-level and tap food beside the pause panel: nothing is served
- [ ] Hold a food item to refill: it refills and does not shake or serve on release
- [ ] Finish a level: last customer walks out, Today Sale counts each amount once
- [ ] Shop: try to buy with too few coins, message is visible; buy an item, coins drop once
- [ ] Watch a rewarded ad: coins added and shown; airplane mode: button shows Loading then retries
- [ ] Open and close the kitchen mid-bake: the bake completes once
