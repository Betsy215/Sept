# Food Truck Cafe: manual for Claude

Read this first in every session on this project. It is the current state: what the game is, the owner's rules,
where things live, how the game works now, and how each recurring job is done. Dated change notes are in
`Docs/history/` (the 12 to 13 October log and the pre-rewrite manual and TODO are there).

Last updated 14 October 2026, evening (menu book, economy tune, tutorial additions, test ads on TestFlight; chapter 2 parked).

## 1. State of the project

| Item | Value |
| --- | --- |
| Game | Food Truck Cafe, a portrait 2D bakery-truck game for iPhone and iPad |
| Engine | Unity 2022.3.50f1, C#, Unity Ads 4.17 (iOS SDK 4.19), iOS target 15.0 |
| Owner | Betsy (GitHub Betsy215, developer name BetzzzGame). Use they/them unless told otherwise |
| Bundle id / app id / team | `com.BetzzzGame.FoodTruckCafe` / 6755611355 / XY39924J9T |
| Repo | https://github.com/Betsy215/Sept (**public**: never commit keys, issuer ids or tokens), branch `main` |
| Live | **26.10.13 build 4** (released by the owner by 16 October 2026; version id 0abde827-2bd8-47e1-959e-c9f7d63d06a4). Before it: 26.10.09 build 4, released 12 October |
| In review | Nothing |
| TestFlight | 26.10.17 build 1 (16 October evening, everything on main up to the shared ovens; batchmode export, `Builds/iOS-26.10.17-1`). Before it 26.10.13 builds 1 to 4 (4 = the submitted one). `ProjectSettings` is at 26.10.17 build 1 |
| On main after build 4 | 13 October night fixes, and 14 October: the menu book (grid, oven notes, station caps), Cookies back as a buy, the economy tune, Day 0 coffee bags and fruit tree, the gingham order receipt and finish panel, real star shapes, test ads on TestFlight builds, chapter 2 groundwork. Not in any build yet; 16 October: Unity Analytics (AnalyticsLog, development environment for editor and TestFlight), the menu as a single board that shrinks to a small copy of itself when closed (no table icon), shop prices in layers per category (Juice 150, Apple 400, Cookies 900, Choux 1,500, Cake 2,200; Yoga 300, Grandma 500, Chef 1,000, Businessman 2,000; plates 500, coffee machine 1,200; $10,650 in all, was $5,350), the text system (black titles and money with a cream rim, brown rimless hints, orange buttons), the two flower pots as Decor shop items (600 and 1,400; +15 % order time each, `DecorGate`, `purchasedDecor`), Daily Treats' repeatable "One more treat" ad button (20 to 50 coins, `DailyTreatsAd`; the button says ONE MORE TREAT alone), the chapter 1 background with Gemini's bushes behind our grass (`Store/bg_polish/bush_blend.py`, the old file in `bg_original.png`), the pots cut along the objects, chapter-only foods and sticker paper colours (`SessionManager.FoodChapter`: Apple and Coffee chapter 1, Coconut and IceCream chapter 2; `IsFoodInSeason`; `MenuBook.PaperColour` mint/yellow/blue; another chapter's food greyed in the carousel), shared ovens (`OvenAssigner`, `OvenKitchenBase.BakeTimes`), the placed sound when a sticker lands (serve sound, Juice only today). Not in any build yet |

What 26.10.13 brings over 26.10.09: two customers at the counter at once, days on a clock (dusk and night as the
day ends), patience as three daisies per order (each serve brings one back), the chapter map (16 days, stars,
three-star bonus, replays at half pay), Daily Treats, the hen money board and flower pots on a picnic cloth, the
owner's kitchen art on the table, Cookies held back for a later release.

Business (9 October): 55 lifetime downloads, all from App Store search, ads earn cents. Unity Ads (checked 14 October,
dashboard project 568be95c, game id 6074412): `Interstitial_iOS` and `Rewarded_iOS` are active and serving; no payout
profile yet. Unity counts TestFlight devices (test mode is off, `AdsInitializer._testMode`); the 7 to 13 October
impressions (6) fall on the owner's TestFlight days (9, 12, 13 October), so they are not player data. To separate
them: the dashboard's Testing page takes test devices by advertising id. Stay free; work on the
store page and marketing before more content. Re-read the numbers in November.

## 2. The owner's rules

1. **The game is live. Change code only for a real, confirmed bug** or what the owner asks for. No refactors or
   re-tuning on your own; write ideas into `Docs/TODO.md`. The "Leave alone" list there holds things the owner chose
   not to fix.
2. **No App Store submission, release, pricing or availability change without an explicit yes in the same
   conversation.** A TestFlight upload is fine when the owner asks for a build.
3. **The owner reads the report, not the terminal.** Report: Claude Doc "FoodTruckCafe Code Review",
   https://claude.ai/code/artifact/726347e1-dd8c-42fd-917b-be80474ff9ae, two tabs:
   - **Report** (body node `9a385004-98ab`): fixed sections with short headings only. **Never add, rename or
     remove a section without the owner asking** (owner, 14 October: "stop changing the report structures"). Order:
     **Next** (Who / Now / Waiting-on table, always first), **Q&A** (only answers to questions the owner asked;
     cleared once they have read them), **Current** (the task in hand: its drawings, captures and short bullets;
     the newest pass replaces the old one), **MVP** (the next release), **Backlog**, **Chapter 2**, **Analysis**
     (store and ads numbers). Questions to the owner go in Next's "You" row, not in Q&A.
   - **Visuals** (body node `fa3ac61b-f7d8`, owner 14 October: keep as it is, they like it): real game captures only, never mock-ups ("everything needs to be from
     real game play, not by guess"). Holds the store-page assets of the current version, plus a temporary
     "Test before release" section when asked (remove it after the submission).
   - Images: upload with the Artifact tool (`asset: true`), then Docs `create` a blob from the asset id, then
     `![alt](blob/<id>)` in markdown.
   - Write in short, plain sentences. Chat replies are one or two lines plus the link; substance goes in the report.
     Direct questions in chat get a short chat answer too, and the answer also goes in Q&A.
4. **Decide, don't ask**, except for real owner choices (store wording, art direction, anything outward-facing).
5. **Never type passwords**; the owner signs in to App Store Connect, Unity and Apple themselves.
6. Commit each working change with a message saying what was broken or added, push to `origin main`, end the
   message with the Claude co-author line from the session.
7. Keep the Godot game (Desktop/Godot) separate. Stay inside Desktop/Sept.
8. Art: the owner's own paintings are kept; AI only adds missing parts or repaints at a new angle with the art kept
   (see section 8). Never regenerate faces.

## 3. Where things are

| Path | What | In git |
| --- | --- | --- |
| `Desktop/Sept/FoodTruckCafe` | Unity project and repo | yes |
| `FoodTruckCafe/Art/kitchen_layers` | The owner's kitchen painting as 21 layer PNGs (1080 x 1920, alpha) with a README | yes |
| `FoodTruckCafe/Docs/tools/recipes` | Every editor recipe (`*.cs` method bodies for `execute_code`) and capture shell scripts | yes |
| `FoodTruckCafe/Docs/tools/autoplay` | Full-chapter autoplay driver (`driver.py`) and its recipes; `run_2026-10-13_days.tsv` | yes |
| `FoodTruckCafe/Captures` | Editor captures (`visual3`, `release-26.10.13`) | no (gitignored) |
| `FoodTruckCafe/Assets/Screenshots` | Where the MCP `shot` writes | no (gitignored) |
| `Desktop/Sept/Builds/iOS-<v>-<b>`, `Builds/archives` | Xcode exports; archives, logs, `exportOptions-upload.plist` | no |
| `Desktop/Sept/Store/screenshots/final2` | 26.10.13 store screenshots, captioned (`iphone-01..06`, `ipad-01..06`); `caption.py` | no |
| `Desktop/Sept/Store/video` | `frames2/` (26.10.13 footage), `cut3.py` (`short` = the 15.5 s store preview), `FoodTruckCafe_appstore_v3.mp4` | no |
| `Desktop/Sept/Store/artkit` | Art scripts (section 8) and the brush engine (`brush/`) | no |
| `Desktop/Sept/Store/redesign` | Table redesign: 40-degree angle guides (`*40.py`), ChatGPT briefs `ai/<item>/` | no |
| `Desktop/Sept/Store/characters` | Customer art: `plain` (no outline), `shadowed` (the in-game set), `orig_game` | no |
| `Desktop/Sept/Store/chapter2` | Beach background, Shop background picks | no |
| `~/.private_keys/AuthKey_<id>.p8`, `asc.json` | App Store Connect API key, key id, issuer | never |

Scratch Python with numpy, scipy, pillow, opencv: the session scratchpad venv (`.../scratchpad/venv/bin/python`);
plain `python3` has no numpy. Make a new venv if the scratchpad is gone.

## 4. Doc map

| File | Read when |
| --- | --- |
| `CLAUDE.md` | Always loaded. Coding rules and the offline compile command |
| `Docs/architecture.md` | Before touching gameplay code: scene flow, singletons, systems |
| `Docs/systems/*.md` | Before changing one system (01 scenes, 02 levels/orders/customers, 03 food/drag/refill, 04 kitchen, 05 scoring/save/shop, 06 audio/UI/ads/iOS, 07 identifiers and tunables). Written before 12 October; check section 6 here for what changed since |
| `Docs/maintenance.md` | Scene wiring invisible in code, scoring, shop table, refill catalogue |
| `Docs/ios-testing-and-release.md` | Builds and the store: manual steps, errors seen, the automated chain |
| `Docs/TODO.md` | Open work, Leave alone, known issues, economy findings, test checklist |
| `Docs/economy_sim.py` | The economy model (see TODO for the 13 October play-test numbers) |
| `Docs/history/` | Dated logs; the old manual and TODO |

## 5. Working in the code

- Press Play from `Assets/Scenes/MainMenu.unity`; other scenes need the managers it creates.
- Compile without Unity: the csc command in `CLAUDE.md` (no output = compiles). Use the newest `Library/Bee/artifacts/*.dag`.
- Strings are identifiers (foods, characters, upgrades): grep everywhere before renaming. Add serialized fields,
  never rename them. Three Shop methods are wired in scenes (`LoadNextGameLevel`, `ConfirmPurchase`, `CancelPurchase`).
- Input is EventSystem-based (3D BoxColliders plus a PhysicsRaycaster). No `OnMouse*`.
- Money is a float at whole cents (`SessionManager.RoundToCents`, `CanAfford`). Show it with `Mathf.FloorToInt`,
  never a rounding format, or $299.60 reads as $300.
- **Never edit a scene file on disk while the editor has it open**: Unity raises a "modified externally" dialog that
  blocks the bridge (13 October). Edit scenes through `execute_code` (open, change, save, reopen MainMenu; recipe
  `Docs/tools/recipes/decor.cs`) or with the editor closed.
- A new food needs an entry in the ScoreManager points table or it scores 0.

## 6. The game as it is now (13 October)

- **Flow:** MainMenu → (new game: tutorial "Day 0" in GameSceneOne, Let's go → Map) → Map → day board → GameSceneOne
  (arrangement, then the day) → finish card → Next → Map → Play → interstitial → Shop → Next → the day.
  Continue opens the map (enabled once a day is done). Pause home → Map.
- **Days:** 16 per chapter (`MapScreen.ChapterDays`), `Level1..16Data.asset`. A day is `dayDurationSeconds` long
  (35 s on day 1 to 70 s on day 16). Customers set off every `timeBetweenOrders` (5 s) while the clock runs, at most
  two at the counter. The day ends when the clock is 0 and no order or customer is left (`LevelManager.TryEndDay`).
  No countdown on screen; `DayNightTint` goes to dusk at 65 % and night at 85 % of the day.
- **Two customers:** `OrderSystem` keeps a list of `ActiveOrder` (slot 0 right, slot 1 left); `TryServeItem` serves
  the oldest order needing the food. `CustomerManager.Spot` spaces the two so two big bubbles leave three equal
  margins (`OrderSystem.TwoSlotBigBubbleWidth`); the second customer is never the same person as the first.
  `OrderSystem.PlaceBubbleOverCustomer` puts each bubble on its customer's head. The tutorial forces one slot.
- **Patience:** three daisies per order (`PatienceMeter`, sprites in `Resources/Patience`), centred on the bubble's
  top (`OrderSystem.MeterPosition`); each wilts in turn; each served item refills the draining one
  (`OrderSystem.RefillPatience`). Tip = item points x seconds left x 0.1 x the customer's tip multiplier.
- **Money and stars:** `ScoreManager` (level score, tips, `payFactor` 0.5 on replays of a finished day). Star lines
  (`Level1..16Data.starThreshold1..3`) were reset on 14 October from a perfect fast player's earnings: 30 / 55 / 80 %
  (`Docs/tools/recipes/economy_tune.cs`; check run: three stars on 14 of 16 days);
  `SessionManager` holds the wallet, `levelStars`, `starBonusPaidDays` (three-star bonus $100 once per day).
  The finish card shows today's sale and tips (the replay share), counts into Earned, and shows the day's stars.
- **Table:** cloth `Assets/Images/knockout 1.PNG` (from `Store/redesign/cloth_picnic.py`), edge shadow
  `decor/tableedge_soft.png` on `TableOuterShadow`. The back band (0.75 units from the cloth's top) takes no food
  (`TableLayer.topReserve`): it holds the truck button (`KitchenButtonOverTable`, `OverlapShare` 0.56), the hen
  money board (`MoneyFlipClock`, sprite `Resources/UI/money_hen.png`, follows the truck's right edge so iPad keeps
  the same gap) and the pots (`TableDecor/KitchenPots` in GameSceneOne).
- **Menu book (14 October):** `SessionData.enabledFoodItems` beside `purchasedFoodItems`; a food is in play when
  bought AND on (`SessionManager.IsFoodItemActive`, read by the table, `KitchenFoodGate`, and so the orders).
  Stations and caps in `SessionManager.FoodStation` / `StationCap` (oven 3, machine 1, counter 2), at least two on.
  `MenuBook.cs` draws the placeholder panel in code (button left of Ready during the arrangement; never on Day 0).
  Cookies are a shop buy again (600, fourth row, `BottomBoundary` 2700); `FoodChapter` is empty until chapter 2. The book
  is a grid; the baked foods' icons are their oven notes (`Resources/UI/stickers`). Ovens are shared since
  16 October (`OvenAssigner`, run from the first oven's Awake): the oven foods that are on go to the ovens top to
  bottom (spot1, spot2, spot3) in the order they were switched on, each oven's first `sticker` child takes that
  food's note (pictures collected from the scene's own stickers), its `KitchenFoodGate` is pointed at the food
  (`keepVisible`: no food = no note, baking asleep), the bake time is the recipe's (`OvenKitchenBase.BakeTimes`:
  Bread 8, Choux 10, Cake 15, Cookies 12 s), a running bake is keyed by the food. Cookies bake in an oven; the
  shelf plate (`CookiesInKitchen`) is disabled, a decoration. The three oven subclasses are empty shells. The coffee machine follows Coffee.
- **Shop:** three rows (`BottomBoundary` 1800); at the bottom the down arrow shows a lock (more next chapter);
  people icons share one height (`EvenOutCharacterIcons`). Prices: Juice 150, Grandma 300, Chef 300, Coffee up 350,
  Apple 350, Yoga 500, Machine up 600, Businessman 700, Choux 750, Cake 750.
- **Customers' art:** the shadowed set (`Store/characters/shadowed`, via `Store/artkit/rim_shadow.py` and
  `swap_customers_shadow.py`, then `girl_head_center.py` for the Girl's happy head). Walk-out: after one pass of the
  looping clip the code walks them off (`CustomerController.WalkOffScreen`).
- **Map:** `MapScreen` (title under the Dynamic Island, pills above the home indicator, today's circle bounces).
- **Daily Treats:** `DailyTreats.cs` in MainMenu, +50 a day, +100 extra on day 5, paid into the next Continue or new
  game (`ftc.daily.*` PlayerPrefs).

## 7. Driving the Unity editor

- Open: `open -a /Applications/Unity/Hub/Editor/2022.3.50f1/Unity.app --args -projectPath <repo>`. The MCP for Unity
  bridge (http://127.0.0.1:8080/mcp) starts on load because `MCPForUnity.AutoStartOnLoad` is set in
  `com.unity3d.UnityEditor5.x` defaults; without it someone must click Start Server.
- From the shell: `python3 Docs/tools/unity_mcp.py call <tool> '<json>'`, `exec <file.cs>`, `shot <name>`
  (Game view to `Assets/Screenshots/<name>.png`), `reset` (new MCP session; do it after an editor restart).
  `manage_scene get_active` tells you the editor is responsive. `refresh_unity` after editing scripts (stop play first).
- Recipes (copy from `Docs/tools/recipes` to `/tmp` or exec in place): `newgame.cs` (all foods and people, $420,
  day 3), `startday.cs`, `state.cs`, `serve.cs`, `kitchen.cs`, `bake.cs` (needs a raycast in the event, as written),
  `closekitchen.cs`, `tomap.cs`, `toshop.cs`, `freshshop.cs`, `tut.cs`, `ff_dusk.cs` / `ff_night.cs` (jump the day
  clock), `gv_<Store1320|iPad|iPhoneSE|Preview886>.cs` (Game view size; set it **before** Play), `decor.cs` (scene
  edits for the table decor, truck sprite, edge shadow, kitchen cookies off).
- Capture scripts: `capture_one.sh <size>` (table + two-order play), `capture_store.sh <size>` (the five store
  shots), `capture_release.sh` (12-screen release series), `record_video.sh` (preview footage with `PreviewRecorder`).
- **Full chapter test:** `python3 Docs/tools/autoplay/driver.py 1.5 1.0` (time scale, seconds per action) plays the
  tutorial through its real taps, then days 1 to 16 with the shop between days, logging state, purchases and the
  console to `/tmp/ap/run.log` and `/tmp/ap/days.tsv`. About 20 minutes. Its recipes must sit in `/tmp/ap` (copy
  `Docs/tools/autoplay/*.cs` there first). Kill stray drivers before starting another (`pgrep -f driver.py`).
- Editor quirks: Unity Ads shows a "Placeholder" ad in play mode after Next; a recompile wipes editor statics;
  `ClickContinue` reads the session only on Start/OnEnable (refresh it with `SendMessage("UpdateButtonState")`
  in tests); never `git checkout` during an export.

## 8. Art pipeline

- **Owner's art first.** Kitchen layers are in `Art/kitchen_layers`; cut pieces with `Store/artkit/kitchen_layers.py`
  and bake contact shadows with `Store/artkit/contact_shadow.py`. The choux board is the owner's painting turned level
  (`choux_board5.py`, `K` = 1.0; the 40-degree stretch K = 1.52 is parked for the next release).
- **Table redesign (next release):** one camera for everything, 40 degrees above the table, long lens (approved
  13 October). Per item give ChatGPT the original painting plus the angle guide from `Store/redesign/*40.py`, prompt
  in `Store/redesign/ai/<item>/prompts.md`; cut the states with `Store/redesign/ai/cut_states.py`. Done: cake, bread,
  apples (then reverted to the originals at the owner's word). Briefs ready: juice, choux, cookies, coffee cups,
  machine, cake redo (flat napkin, no pink cut face). ChatGPT free plan: about three pictures a day; wait a few
  seconds after an upload before typing or the text is lost. Gemini keeps a style but redraws shapes.
- Other scripts: `daisy_sprites.py` (patience petals), `cloth_picnic.py` (cloth), `edge_soft.py` (edge shadow),
  `pots_round.py`, `money_hen.py` (fallback hen), brush engine `Store/artkit/brush` (26 brushes, Da Cookie grain).

## 9. Store and release

- **Build:** bump `bundleVersion` / `buildNumber: iPhone:` in `ProjectSettings/ProjectSettings.asset`; close the editor;
  `Unity -batchmode -quit -projectPath . -buildTarget iOS -executeMethod CommandLineBuild.iOS -exportPath
  ../Builds/iOS-<v>-<b> -logFile ../Builds/export-<v>-<b>.log` (about 4 minutes); archive with xcodebuild
  (`ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES=NO`, about 10 minutes), upload with `-exportArchive` and
  `Builds/archives/exportOptions-upload.plist`; poll `GET /v1/builds?filter[app]=6755611355&sort=-uploadedDate`
  until VALID (5 to 20 minutes). Full steps: `Docs/ios-testing-and-release.md`. Reopen the editor afterwards.
- **API:** `python3 Docs/tools/asc_token.py` gives a 15-minute JWT. `Docs/tools/asc_fill.py texts|build|screenshots|
  preview IPHONE_67|status` fills a version (ids, `BUILD` and asset paths at the top). Create a version:
  `POST /v1/appStoreVersions` (platform IOS, versionString, releaseType MANUAL, app). Submit: `POST /v1/reviewSubmissions`,
  `POST /v1/reviewSubmissionItems` (the appStoreVersion), `PATCH reviewSubmissions/<id>` with `submitted: true`.
  Pull back for edits: `PATCH` with `canceled: true` (state DEVELOPER_REJECTED, editable), then submit anew.
  Add one screenshot: `POST /v1/appScreenshots` to the set, upload operations, `PATCH uploaded: true` + md5; order a
  set with `PATCH appScreenshotSets/<id>/relationships/appScreenshots`. TestFlight feedback screenshots:
  `GET /v1/apps/6755611355/betaFeedbackScreenshotSubmissions`.
- **Store rules learned:** previews 15 to 30 s (14.5 s failed processing), 886 x 1920, in-game footage only;
  screenshots `APP_IPHONE_67` 1320 x 2868, `APP_IPAD_PRO_3GEN_129` 2064 x 2752; age rating Advertising = Yes.
- **26.10.13 page:** the owner's What's New (New patience system / New order mechanism / Visual updates / A new map /
  Daily rewards), captions (Serve quickly to earn tips; Bake fresh bread in your kitchen; Earn tips and stars; Play
  from morning to night; Unlock new foods and regulars; Earn stars along the road). The description and promo still
  mention two customers; the owner may want that reworded (asked 13 October, no answer yet).

## 10. Open work

See the report's Next and MVP and `Docs/TODO.md`. In short: watch the 26.10.13 review and answer anything Apple
raises; after release, the next version. **Its goal (owner, 14 October, afternoon): the menu book** (`Docs/menu-book-plan.md`); chapter 2 (the beach,
`Docs/chapter2-plan.md`) is parked again behind it. Also: the table redesign at 40 degrees, the
choux board stretch, Cookies, the economy tune (TODO has the play-test numbers: no player reaches 3 stars now),
store text. New art follows the kitchen style: `Store/brushes/STYLE.md` rules 1 to 22 (third study 14 October),
painter `Store/artkit/brush/ks2.py`; ice cream brief in `Store/chapter2/icecream`.

## 11. Keeping this manual current

Change it when a fact changes, in place: this file is the current state, not a diary. Put dated notes in
`Docs/history/`. Save new editor recipes to `Docs/tools/recipes` (`/tmp` is wiped on reboot).
