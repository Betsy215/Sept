# Food Truck Cafe: manual for Claude

Read this first at the start of every session on this project. It says what the game is, the owner's rules, where everything lives, and how each recurring job is done. Deeper detail is in the docs it links to; this file is the map.

Last updated 12 October 2026, evening.

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
| Live version | 26.10.09 build 4: approved and released by the owner on 12 October 2026 (before that 26.03.29, March 2026) |
| Next version | 26.10.13 planned for 13 October: everything on main since 26.10.09 (see the row below and section 9). Create the version in App Store Connect, attach the build, store text for 16-day chapters, submit only on the owner's go |
| TestFlight | 26.10.11 (1), 11 October: map, Daily Treats, cookie buttons, outlined customers, choux 2 by 2, table edge. 26.10.12 (1), 12 October 19:31: plus the three table fixes (stripe seams, flat choux shadow, blue cloth under the cake) |
| On main, not yet built (12 October evening) | Economy rebalance (replays pay half, Apple 20, new star thresholds), Cookies food, patience daisy + Decor_Vase / Decor_Bouquet shop upgrades, iOS target 15.0. Release planned 13 October after an editor play test; store text must say 16-day chapters, not 24 days |

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

**Age rating:** "Advertising" must be **Yes**, because the game shows Unity Ads. 26.10.09 was rejected on 10 October 2026 under guideline 2.3.6 because it was No. That question is new in Apple's 2025 age rating form. It can be set through the API: PATCH `ageRatingDeclarations/<id>` with `advertising: true`; the id comes from `appInfos/<id>/ageRatingDeclaration`. After a rejection, open the version, click **Update Review**, then **Resubmit to App Review** (only with the owner's yes).

App Privacy has no API and is set in the browser. Ten data types are declared from Unity Ads' privacy survey. Device ID is linked and used for tracking. Product Interaction, Advertising Data, Other Usage Data, Performance Data and Other Data Types are linked but not used for tracking. The other declared types are in App Store Connect.

**Character sprites from ChatGPT** (free plan, about 2 to 3 images a day, each back 24 h after use):
- One landscape image per character, with the neutral and happy poses side by side.
- Attach `Store/characters/cast-sheet.png` for style. Ask for a plain white background, not a transparent one.
- Getting the result out of ChatGPT without a browser download: in the page, fetch the generated image's blob URL and write it to the clipboard (`navigator.clipboard.write` with a ClipboardItem; click the page first so it has focus, run it fire-and-forget and poll a window variable, since awaiting it hangs the tool), then save the clipboard with `osascript` («class PNGf»). Raw images go in `Store/characters/raw`. The owner can also save it by hand as `~/Desktop/ftc_<name>.png`.
- Cut with `Docs/tools/cut_sprites.py` (needs numpy, scipy and pillow; a scratchpad venv is fine): `cut_sprites.py raw.png out.png outhappy.png [match.png matchhappy.png]`. Pass the match files when replacing a sprite, so the canvas size and pivot stay the same. The script removes the white rim between hair strands.
- **Gemini** (gemini.google.com, the owner's account) is better than ChatGPT at editing an existing sprite while keeping it unchanged, for example adding legs to a cut-off character. It is not limited to 2 or 3 images a day the way ChatGPT is.
  - Pad the sprite on white with empty space where the new part goes, then ask Gemini to draw only there. Paste the original pixels back over the result (`fullbody.py`-style compositing).
  - To attach a file without the system file picker: in the page, patch `HTMLInputElement.prototype.click` so a file input shows itself with an aria-label instead of opening the picker. Choose "Upload files", then attach with the Chrome file_upload tool on that input.
  - To get the result without a browser download: click Gemini's Copy button, then save the clipboard to a file with `osascript` («class PNGf»).
- Cut tool options for hard cases: `RIM=30 WHITE=185 HOLES=1` for dark-haired characters with no white in their design (it clears white trapped deep in hair or enclosed by an arm). Use `RIM=4` for white-haired characters such as Grandma. `SINGLE=1` cuts one figure per image.
- Sprites go in `Assets/Images/Customers` with a chest pivot of (0.5, 0.6).
- **Three-way drafts** (12 October, chapter 2): for new art the owner wants three versions to choose from: ChatGPT, Gemini and Claude's own painting (`Store/artkit/brush`). Claude writes the brief: the closest existing game art as the style reference, plus a layout map for scenes. Never give Gemini an image with words on it; it paints the words in. Describe the layout to Gemini in words. Gemini's free images are 572 x 1024. ChatGPT's are 941 x 1672.
- **Backgrounds on iPhone:** the 1080 x 1920 background fills the screen height, so about 9% is cut off at each side. The tablecloth starts at 50% of the height. `Store/chapter2/beach_bg/mock.py bg.png out.png [night]` shows a background in the game with the cloth, a customer and the truck.
- The prompts and the daily queue are in the report, under Customer art.

**In-game caption card** (10 October): the tutorial captions (`TutorialDirector`, CaptionBox) and the arrange hint (GameSceneOne, ArrangementUI/HintCard behind the Text) use `Assets/Resources/Tutorial/card.png`, a 9-sliced gingham recipe card (border 48), brown text 0.36/0.22/0.09, auto-sized 36 to 64. The table top edge has only `TableOuterShadow` (Assets/Images/tableedge.png), a child of TableCloth so it follows the cloth when it is stretched at runtime; the soft brown TableEdgeShadow was removed at the owner's request.

**Cookie buttons in the game** (10 October): `ready.png`, `continue.png`, `new.png`, `coinsbutton.png`, `cursor.png` (the arrange hand) and `Resources/Tutorial/arrow.png` are the Gemini cookie set with frosting letters, generated by `Store/artkit/ingame_cookies.py` at the ORIGINAL canvas sizes and opaque boxes, so no scene changed. Re-run that script (then copy from its output folder) to change a label or colour; `collect.png` is the same style for Daily Treats.

**Daily Treats** (10 October): `Assets/Scripts/DailyTreats/DailyTreats.cs` on the `DailyTreats` object in MainMenu. First open of each local day shows the popup (+50; day 5 of a row adds +100, revealed on Collect; a missed day resets the row; a clock moved back gives nothing). PlayerPrefs keys `ftc.daily.last` (yyyy-MM-dd), `ftc.daily.streak` (1..5), `ftc.daily.pending` (every Collect goes here; `SessionManager.StartNewSession` and `ContinueSession` pay it, so starting a new game can't wipe a treat). The date is written in the invariant culture and is the day the popup opened on (a tap just after midnight doesn't use up the next day). The popup is built in code on its own canvas (1080x1920, order 40) from `Resources/DailyTreats/*.png`, drawn by `Store/artkit/treats_pieces.py` with the mock's code; the board, Collect and star sprites are Inspector fields. To test in the editor: play MainMenu, then `FindObjectOfType<DailyTreats>().DebugShow(5)`; clear the three keys to see it again.

**Customer sprites and sizes** (10 October): the 14 customer files (boy, boyhappy, girl2, girl 2, kid, happykid, gran 1, gran2, Customers/chef, chefhappy, yoga, yogahappy, businessman, businessmanhappy) are the outlined set from `Store/characters/outlined` (made by `Store/artkit/rim6.py`), copied in place by `Store/artkit/swap_customers.py`, which also prints each file's pivot and each prefab's scale. Rule: adults 4.14 units of body (the Chef 0.90 of it, his hat made him tower), Girl 0.97, Grandma 0.93, Boy 0.85, Kid 0.78 of it; feet 2.48 units below the prefab origin. HappySprite children sit at scale 1 under the root. The Boy's happy pose got its shins and sneakers from Gemini via `boy_feet.py` (original pixels on top). Choux stock pictures (`Untitled_Artwork 5/9/8/7/6`, 0 to 4 choux) sit 2 by 2 on a short, squarish straight-on board like the kitchen cutting board, from `Store/artkit/choux_board2.py` (board by Gemini with its long middle cut out, choux = the game's own sprite; the earlier long board was `choux_board.py`, the old files are in `Store/artkit/choux_orig`). `coinsbutton.png` has mint frosting. Tablecloth (`knockout 1.PNG`) stripes converge toward the top, from `cloth_perspective.py` (original kept as `Store/artkit/cloth_orig.png`); its painted top hem (rows 0 to 5) was overwritten with the stripes below it so nothing shows under the edge shadow.

**Chapter map** (10 October): scene `Map` (in Build Settings), built in code by `Assets/Scripts/Map/MapScreen.cs` from `Resources/Map/*` (drawn by `Store/artkit/map_assets.py`). Chapter 1 = Scorpio, 16 days (`MapScreen.ChapterDays`); days 17 to 24 stay in `allLevels` for chapter 2 and are never loaded. Flow: main menu Play and Continue open the map (ClickPlay/ClickContinue `_sceneName` = Map); tapping a played day or today opens the day board (Play, Cancel); Play goes straight to GameSceneOne, or, after a finished day (`MapScreen.CameFromDay`, set by `LevelManager.LoadNextLevel`), shows the interstitial and then the Shop, whose Next starts the chosen day. The pause menu's home opens the map (`LevelManager.GoToMap`); the map's home opens the main menu. Session: `levelStars` (best per day) and `starBonusPaidDays`; `SessionManager.RecordLevelStars` pays `LevelManager.ThreeStarBonus` ($100) once per day (Restart on the finish board keeps it, and the finish board's Earned count-up shows it as today's money); `OnLevelCompleted` keeps `levelsCompleted` at its maximum so replays never move progress back. The truck sits right of today's star, below it on day 5 (near the screen edge), above day 16 when the chapter is done. Day 16 no longer ends the session; it shows "Chapter 1 complete!" and Next goes to the map. Test: play MainMenu, then start a session, call `OnLevelCompleted(i)` and `RecordLevelStars(i, stars, 0)` for a few days, and transition to "Map".

**Art kit** (`Desktop/Sept/Store/artkit`, scripts need the scratchpad venv with numpy, scipy, pillow):
- `painterly.py`: turns flat vector shapes into the game's soft watercolour look (paper grain, pigment pooling at edges, blotches, wobbly edge, top-left light, warm rim) plus a contact shadow. Good for UI pieces and simple props; not for characters or detailed food, which still come from Gemini or ChatGPT with the style prompt.
- `ui_v5.py`: cookie buttons, arrow and hand sampled from the real square cookie buttons (colour profile edge to centre, pressed-in text in the icon's burnt orange); `ui_style.py` and `ui_v3.py` hold paper panels and notes.
- `rim6.py`: the current sprite finisher. `defringe` first: it compares each edge pixel with the colour a few pixels inside the figure and removes or recolours the lighter ones, so the white and grey matte left by a cut-out goes while white parts of the design (chef hat, white hair, hoodie) stay. Then the tan 7 px stroke and the all-around shadow (owner's choice, style G), one scale per character for both poses. Writes `Store/characters/outlined` and a lineup sheet. `rim5.py` is the older stroke-only version.
- **Brush set** (11 October): `Store/artkit/brush/` holds a stamp brush engine like Procreate's (`engine.py`: `Canvas.stroke` with shape + canvas-fixed grain, pressure, spacing, scatter, wet edge, multiply/screen, blur and smudge; `Canvas.wash` for a flat colour fill; `Canvas.form_shade` for light and shade from a shape) and 26 brushes in `brushset.py`: the owner's Procreate "Picnic" set (6 with their own textures, 4 with my copies of Procreate built-ins) plus 16 made from textures cut from the game art (`textures.py`). Sheets: `Store/brushes/full_set.png`; catalog `Store/brushes/README.md`; example painting `props_test.py`. Shapes are white = paint, grains tile.
- `ui_v6.py`: cookie buttons, arrow and hand built from measurements of the real pause cookie (colour gradient top-left cream to bottom-right amber, blotch and grain spread, grain quilted from the real image), frosting letters (`frosting_text`: icon-orange letters with a piped outline, white by default), and `note_card6`, the option B gingham caption card. `checkin2.py` is the daily treats mock on top of it; `tableedge.py` mocks the table's top edge (shadow line and cloth drop) on a screenshot.
- `cut_sprites.py`, `headswap3.py`: cut-out and the one-body head swap for the two poses of a character.
- `cookies_gemini_raw.png`, `cookie_wide|arrow|hand(_matched).png`: Gemini's cookie button, arrow and hand drawn from the real square cookies (reference `Store/mockups/cookie-ref.png`), cut out and colour-matched to the pause cookie's mean and spread per channel. `ui_v7_demo.py` stretches the wide one with a 9-slice and adds frosting letters.
- Gemini's result without a download: its Copy button puts the image on the clipboard; save with `osascript` («class PNGf»).
- Mock-ups made with these live in `Store/mockups`.

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

The report's Next, Current, MVP and Backlog sections are the live list (layout and rules: section 2 and the report itself). As of 11 October:
- **Current: the drawing tool.** A 26-brush set is built (catalog `Desktop/Sept/Store/brushes/README.md`, engine `Store/artkit/brush`, see section 7). Next the owner wants to discuss how to draw and try drawings together. A Procreate .brushset for the owner can be made using `Store/brushes/from_procreate/Picnic` as the file template.
- **MVP (next release):** store text for 16-day chapters and the map; economy rebalance (replays pay full coins; three-star bonus $100 once per day; players from the live game can re-earn it on all 16 days); App Store header and search banner; rating prompt after Day 3.
- Press Release on 26.10.09 after approval (the owner). TestFlight of what's on main only when asked.
- Backlog: marketing research, TikTok, one-page site, improvement ideas, main scene visual pass, two small fixes, safe-area buttons, Earned label wrap, Unity payout profile, SKAdNetwork list, check the Interstitial_iOS unit, Remove Ads later. Analytics review in November.

Minor known issues are in `Docs/TODO.md`. Don't fix them without the owner's yes.

## 11. Keeping this manual current

When a session learns something a future session would need (a new recipe, a path, a decision by the owner, a store rule), add it here or in the linked doc and commit it. Change the "Last updated" date. Remove anything that stops being true.

## 9. 12 October evening: what changed and how it was done

- **Command-line export** (`Assets/Editor/CommandLineBuild.cs`): when the editor is closed, `Unity -batchmode -quit -projectPath . -buildTarget iOS -executeMethod CommandLineBuild.iOS -exportPath ../Builds/iOS-<v>-<b> -logFile ../Builds/archives/export-<v>-<b>.log`, then the archive and upload steps from `Docs/ios-testing-and-release.md`. The export took about 4 minutes; the log says "Exiting batchmode successfully". Apple's ITMS-90068 on 26.10.12 (1) was about the iOS 14 target; it is 15.0 now.
- **Economy** (`Docs/economy_sim.py` is the model): replays of a finished day pay half the order money (`ScoreManager.payFactor`, set in `LevelManager.LoadLevel` from `levelIndex < GetLevelsCompleted()`), stars still use the full score. Apple is 20 points. Star thresholds in `Level1..16Data.asset` = natural-path income at 60% timer left (3 stars), 75% of that (2), 50% (1).
- **Cookies** (`Docs/tools/add_cookies.py` did every scene edit by YAML): id `Cookies`, $600, 30 points, table object copied from Choux at (-1.56, -2.32) scale 0.22 with `Cookies.cs` (4 sprites `Images/cookies/k0..k3`, 680 px canvas, shadow baked in), kitchen plate under `cabright` with `CookiesInKitchen.cs` (tap, 8 s respawn, like the apples), bubble icon `Prefabs/Cookies.prefab`, shop card in a new row at (-400, -2680). `totalFoodItems` is 7. Sprites come from ChatGPT's sheet via `Store/decor/table_sprites.py`.
- **Patience** (`PatienceMeter.cs`, `PatienceDecor.cs`): the daisy is drawn from runtime textures at world (-1, 3.35); petals = `CustomerManager` sets `OrderSystem.patiencePetals = round(level time + PatienceDecor.BonusSeconds())`; `OrderSystem.DisplayOrder` shows it, `CompleteOrder` / `ExpireOrder` hide it. Decorations are Shop upgrades keyed `Decor_Vase` (-1.85, -0.75) and `Decor_Bouquet` (1.8, -0.6), sprites in `Resources/Decor`, placed by `LevelManager.ApplyLevelSettings`. Cards added by `Docs/tools/add_decor_shop.py`; `ShopManager` scroll limit is 3300.
- **Art**: `Store/brushes/STYLE.md` has the owner's second reference (soft textured gouache, pastel). ChatGPT free tier gives about three images a day; typing into its box right after an upload loses the text, so wait a few seconds first. Gemini keeps a style but not a shape; for a same-shape repaint use ChatGPT with the original as image 1.
- **Not done**: bread basket restyle (Gemini's two tries changed the shape), the lantern decoration, chapter 2 picks.
- **Owner's 26.10.12 test pass (12 October, late)**: tablecloth joins now follow the real cream stripe (`cloth_perspective2.py`, no straight-line fit); the Girl's happy sprite had its head turned 10 degrees and moved so it sits like the neutral pose (`Store/artkit/girl_head_align.py`, original in `girl_orig/`); the tutorial caption card grows 72 px per extra line (`TutorialDirector.Say`); a new game goes straight to the tutorial as "Day 0" (`ClickPlay` → GameSceneOne when `TutorialDirector.IsDue`), and Let's go leads to the map instead of reloading into day 1. Open: the owner sees a small upward curve at the cloth's top edge that is not in the files; waiting for a screenshot.
- **Later the same evening**: customer outlines are 4 px (`rim6.py`, was 7; the 7 px set is kept in `Store/characters/outlined_7px`); `swap_customers.py` copied the set, the metas' pivots were left as they were (the canvases did not change, except the Boy's happy pose, which rim6 now makes from the larger `cut/boy_happy_full.png` and which was scaled back onto its 228 x 546 canvas), and the Girl's head alignment was re-applied on the new file. The kitchen button (the truck) moves at runtime into its own canvas on the FoodItems layer so it can sit a third onto the cloth (`KitchenButtonOverTable.Setup`, from `LevelManager.ApplyLevelSettings`; `TutorialDirector` still finds it by name).
- **Two customers at once (12 October, night; owner's request, needs her play test)**: `OrderSystem` now holds a list of `ActiveOrder` (slot 0 right, slot 1 left), each with items, timer, patience petals, tip multiplier and points; `TryServeItem` serves the oldest open order that needs the food; `StartOrderForCustomer(customer, slot, minItems, displayTime, tipMultiplier, petals)` opens an order. Slot 0 uses the scene's Bubble1/2/4 and OrderContainer gathered under a runtime "OrderSlot0" root, slot 1 uses clones under "OrderSlot1"; with `maxConcurrentOrders` 2 the roots move to `slot0BubbleOffset` / `slot1BubbleOffset` at `twoSlotBubbleScale`, with 1 (set by `TutorialDirector.Begin`) everything is where the scene put it. `CustomerManager` keeps two slots under spot parents at `twoSlotCustomerX` (the walk-in clip animates local x -3 to 1, so a spot's x is the offset), scales customers by `twoSlotCustomerScale`, and schedules the next arrival `secondCustomerDelay` (2.5 s) after a customer reaches the counter or `nextCustomerSpawnDelay` after one leaves, never more than `ordersPerLevel` customers. `ScoreManager` got per-order overloads (`AwardItemPointsValue`, `AwardOrderCompletionBonus(time, points, tip)`, `ApplyOrderExpiredPenalty(points)`); `PatienceMeter` is one daisy per order. `CustomerController.Slot` says where a customer stands. Open questions are in the report (arrival gap, order counts per day, economy re-check, the day sign overlap until the flip clock).
- **Review pass (12 October, night)**: two read-only code reviews of everything since 26.10.09 and a sprite scan. Fixed: `BalatroWobble` on the KitchenButton rewrote its rest position every frame, so `KitchenButtonOverTable` never showed (now `Rebase()` after the move); a Restart from the finish board after a first completion paid the re-run at the replay rate (`LevelManager.restartPayFactor`); the map canvas is `Expand` so the title and coins pill clear the days on an iPad; the truck branch at day 4; Cookies shop icon is the square plate (the wide cookie squashed in the 200 x 200 icon), its bubble icon scale 0.27, the stray 5th sprite removed, Cookies in SceneRoots; shop scroll limit 3160; decorations moved inside the iPhone width and above the Apple/Choux slots; two-customer: enter from off screen (spot slides in), walk out until off the right edge, right slot drawn in front, stale-order guard, 1 s arrival gap. Known and left: two finish popups within a second replace each other; dusk timing with two customers counts completions.
- **Timed days (13 October, owner's design)**: a day is `LevelData.dayDurationSeconds` long (set to the old order count x 5: day 1 35 s, day 16 70 s); customers set off one by one every `LevelData.timeBetweenOrders` seconds (5) while the clock runs, each waiting for a free slot (two at most) and for the previous one to reach the counter (`CustomerManager.SpawnLoop`). When the clock hits zero no more come; the day ends when the last order is closed and the last customer has left (`LevelManager.TryEndDay`, called from `Update`, `OrderSystem.AfterOrderClosed` and `CustomerManager.OnCustomerExited`). The countdown is a copy of the day label under the sign (`BuildDayClockText`, orange in the last 10 s); it pauses with `OrderSystem.freezeTimer` and does not exist in the tutorial. `DayNightTint` goes to dusk at 65% and night at 85% of the day. `ordersPerLevel` stays for the editor auto-play and the no-customer flow. Economy: the customer count per day is unchanged (duration / 5), so the thresholds hold; re-check after a play test.
- **Flip clock and three flowers (13 October)**: `MoneyFlipClock.Setup(scoreManager)` (from `LevelManager.ApplyLevelSettings`) builds the money box at world `MoneyFlipClock.Position` (-0.12, 0.12), 1.4 x 0.62 units, on the FoodItems layer: four split cards (TextMeshPro glyphs in the beachday font, a fold animation on change), three procedural stars on top coloured from `StarProgressBar.GetEarnedStarsCount()`. The old top sign (the Board, parent of `levelInfoText`) is deactivated; the day countdown is a copy of `levelInfoText` under the canvas root at the top centre. `PatienceMeter` is three flowers per order (`Flowers = 3`, spacing 0.62): `Fill(order, i)` from the timer, wilt and droop by fill, `OrderSystem.RefillPatience` moves the timer up to the next flower boundary on every served item and `PatienceMeter.OnItemServed` pops the flower. Decorations are gone (no `PatienceDecor`).
- **Driving the editor when the MCP client did not connect (13 October)**: open the editor (`open -a .../Unity.app --args -projectPath .`), then `python3 Docs/tools/unity_mcp.py` talks to the bridge at 127.0.0.1:8080/mcp over plain HTTP: `list`, `call <tool> '<json>'`, `exec <file.cs>` (execute_code with a method body), `shot <name>` (Game view to `Assets/Screenshots/<name>.png`, now gitignored). Recipes used for the 13 October captures are the ones in section 6 (`/tmp/newgame.cs`, `startday.cs`, `state.cs`, `serve.cs` in the session log); `manage_editor` has no get_state, use `manage_scene get_active` to see the editor is responsive, and `refresh_unity` after editing scripts (stop play first).
- **Two-customer layout after the real captures (13 October)**: `OrderSystem` slot roots at (1.5, 1.8) and (-0.4, 1.8), bubble sets at 0.7 (0.85 reached the top buttons); `PatienceMeter.WorldPosition` (-0.75, 3.43) so the left set clears the settings button, flowers 0.45 apart; the countdown at canvas y -40 at the top centre.
- **Visual tab refreshed 13 October** from real editor captures (`Captures/visual3`, sheets in `Store/visual3`): menu + Daily Treats, map + day board, arrange, play (two customers, refill, receipt, dusk, night, finish), kitchen + shop, people (4 px outlines), food (with Cookies). The receipt popup now spawns at canvas (0, 520), the centre between the two bubbles. The `Assets/Screenshots` folder the bridge writes to is gitignored.
- **Table redesign, the method that stuck (13 October, afternoon)**: the owner wants the items' paintings kept and only the camera changed. The flat gouache set in `Store/redesign/*40.py` is the angle guide, not the art. Per item: give ChatGPT the original painting (on white, 2x) plus the gouache drawing of the same object, ask for the original's art at that camera (prompt in `Store/redesign/ai/<item>/prompts.md`); Gemini redraws the art instead, so it is not used for this. Frames: ask for all states in one picture with wide gaps (a touching strip clips the napkins), cut with `Store/redesign/ai/cut_states.py <grid> <original frame> <out dir> <names full..empty>` (flood from the border with a tight threshold and only pure white 250+ as background, so off-white icing stays; `HOLLOW=1` also clears enclosed white areas such as the inside of a basket handle, never use it on plates or cakes; edge un-matted; scaled by the full state to the original's footprint, same bottom, centre pivot). The cake used `ai/cake/cut_grid.py`, the same logic. ChatGPT's free plan gives about three pictures a day. The cloth is `Store/redesign/cloth_picnic.py` (committed).

- **Owner's pass of 13 October, evening** (all real captures in `Captures/visual3/v4-*`): two customers stand 1.7 apart (`CustomerManager.twoSlotCustomerX` {0.2, -1.5} → x 1.2 and -0.5, `twoSlotCustomerY` -0.3 so their knees are behind the cloth); bubble sets at 0.65 with roots (1.92, 1.56) / (0.22, 1.56), which puts each bubble's centre over its customer at y 2.95 (bubble centre = root + scale × (-1.11, 2.14)); `PatienceMeter.WorldPosition` (-1.11, 3.45) is centred on the bubble's top edge and the set is `PatienceMeter.Scale` 0.8. The daisies are painted (`Assets/Resources/Patience/petal.png`, `centre.png`, from `Store/artkit/daisy_sprites.py`; each petal turned ±7° and sized ±8% per flower; the procedural daisy is the fallback). The day countdown text is gone (the evening tint says the day is ending); the clock still runs. `MoneyFlipClock`: dark warm cards, cream digits, the digits use their own material (no outline, no green underlay). Kitchen decorations on the table: `Assets/Images/decor/kitchen_pots.png` at (1.72, 0.12) scale 0.28 and `kitchen_bouquet.png` at (2.0, -1.65) rotated -30° scale 0.2, cut from the kitchen painting by `Store/artkit/kitchen_flowers.py`, scene objects under `TableDecor` in GameSceneOne (FoodItems, order 1), decoration only. The picnic cloth's stripes were sharpened (`cloth_picnic.py`: edge blur 1.0, coral 226/118/94).
- **Same evening, second pass**: the money board shows one number ("$420") in the game font and text style (black face, the font material's green glow), with a small bounce on change; no cards, no flipping (`MoneyFlipClock`, name kept). Contact shadows: baked into the sprites by `Store/artkit/contact_shadow.py` (the silhouette near the base, shifted down-right, blurred) for `decor/kitchen_pots.png`, `decor/kitchen_bouquet.png` and `decor/kit1_table.png` (a copy of the truck `kit1.png`; GameSceneOne's KitchenButton Image now uses it, its rect scaled with the canvas; the kitchen's back button still uses kit1), and drawn in code under the money board. Bouquet lies rotated 90° at (0.9, 0.1) scale 0.17 between the board and the pots at (1.74, 0.1). Pots recut with their bases and soft edges (`kitchen_flowers.py`, 2x then a colour-distance alpha ramp). Cookie plate frames rebuilt by `Store/decor/cookies_fix.py` from the old k0/k3 (kept in `Store/decor/cookies`): green rim and dots, clean missing states, chocolate cookie with a zigzag drizzle. Cloth: cream 255/253/246, coral 220/102/80, cross mix 0.30. Daisy petal a plain oval.
- **13 October, third pass (owner's screenshot notes)**: customers without the tan outline and halo (`Store/artkit/rim0.py` → `Store/characters/plain`, swapped by `swap_customers_plain.py`; the pre-outline Boy, Kid and Chef files came from git, kept in `Store/characters/orig_game`); daisy petals flat ovals with one thin line (`daisy_sprites.py`, 48 x 104 at 400 px per unit); cloth white cells pure white, horizontal stripes at 0.46 (`cloth_picnic.py`); pots redrawn as flat white pots under the kitchen's flowers (`kitchen_flowers.py draw_pots`), bouquet daisy centre filled; the pale "paper" under the apples, bread, cake and choux removed (`Store/artkit/under_clean.py`, originals in `Store/decor/food_before`) and the apple basket's napkin and doily with it (`apple_bare.py`); apples, bread and choux carry a baked contact shadow. Cookies held back: `SessionManager.HeldBackFoods` (`IsFoodItemPurchased` false, `LevelManager` skips the table item, `ShopManager.Start` hides the card), `totalFoodItems` 6 (MainMenu still serializes 7; the field in the scene should be set to 6 at the next scene save). The money board is the kitchen hen (`Resources/UI/money_hen.png` from `Store/artkit/money_hen.py`, `MoneyFlipClock.HenScale` 0.2): number brown with a cream outline, character spacing 9, on the white body; stars on the red base. Truck button rect 214 x 130. `TableOuterShadow` uses `decor/tableedge_curved.png` (`edge_curve.py`: the 4 x 64 strip shifted by the cloth's sag, 1173 x 80, scale x 1). Bubble roots y 1.38.
