# Food Truck Cafe: manual for Claude

Read this first in every session on this project. It is the current state: what the game is, the owner's rules,
where things live, how the game works now, and how each recurring job is done. Dated change notes are in
`Docs/history/` (the 12 to 13 October log and the pre-rewrite manual and TODO are there).

Last updated 21 October 2026 (eight bug fixes, Coconut a chapter 2 food, the verify21 recipes, recv.py; earlier: menu book, economy tune, chapter 2).

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
| Planned | Release of 26.10.18 on 19 October 2026 (owner, 18 October evening: more drawing time); build 2 first, with the owner's drawings, the table edge-shadow fixes and the popup texts of 18 October |
| TestFlight | **26.10.18 build 1** (18 October 14:28: chapter 2 on with the Cancer map, ice cream station and blender, four coconuts, per-scoop pay, saved station spots, gold locks, centred popups with names and the Chef and Yoga coach notes, navy table shadows, cloth G, Coconut and Ice cream stickers; exported from inside the editor, `Builds/iOS-26.10.18-2` folder holds it although Info.plist says build 1 because the editor had not re-read the bumped ProjectSettings; the next export must say build 2, check Info.plist before the upload). Before it: 26.10.17 build 2 (16 October 17:58: the shop layout, popup, prices, day-end note, carousel, longer days, stickers, white receipt, cookies; `Builds/iOS-26.10.17-2`) and build 1 (16 October evening, up to the shared ovens; `Builds/iOS-26.10.17-1`). Before it 26.10.13 builds 1 to 4 (4 = the submitted one). `ProjectSettings` is at 26.10.17 build 2 |
| On main after build 4 | 13 October night fixes, and 14 October: the menu book (grid, oven notes, station caps), Cookies back as a buy, the economy tune, Day 0 coffee bags and fruit tree, the gingham order receipt and finish panel, real star shapes, test ads on TestFlight builds, chapter 2 groundwork. Not in any build yet; 16 October: Unity Analytics (AnalyticsLog, development environment for editor and TestFlight), the menu as a single board that shrinks to a small copy of itself when closed (no table icon), shop prices in layers per category (16 October evening, lowered on the owner's "probably lower": Juice 110, Apple 300, Cookies 600, Choux 1,000, Cake 1,500; Yoga 220, Grandma 380, Chef 700, Businessman 1,400; plates 380, coffee machine 800; pots 450 and 900; $8,740 in all; the afternoon's set was $10,650, before that $5,350), the text system (black titles and money with a cream rim, brown rimless hints, orange buttons), the two flower pots as Decor shop items (600 and 1,400; +15 % order time each, `DecorGate`, `purchasedDecor`), Daily Treats' repeatable "One more treat" ad button (20 to 50 coins, `DailyTreatsAd`; the button says ONE MORE TREAT alone), the chapter 1 background with Gemini's bushes behind our grass (`Store/bg_polish/bush_blend.py`, the old file in `bg_original.png`), the pots cut along the objects, chapter-only foods and sticker paper colours (`SessionManager.FoodChapter`: Apple and Coffee chapter 1, Coconut and IceCream chapter 2; `IsFoodInSeason`; `MenuBook.PaperColour` mint/yellow/blue; another chapter's food greyed in the carousel), shared ovens (`OvenAssigner`, `OvenKitchenBase.BakeTimes`), the placed sound when a sticker lands (serve sound, Juice only today). In TestFlight 26.10.17 (1). After that upload: the menu stickers' first full run (`Store/ui/stickers/stickers_v2.py`: the old notes' paper, a corner flip and a thin edge on each, the food's own sprite with a white rim, no words; old files in `Store/ui/stickers/orig`), paper tilt on the menu 4 degrees at most; the order receipt back to the white one with the barcode (`paper 4.png` from before 14 October, text at (-5, 20)); the patience meter picks its flower by chapter (`PatienceMeter.Chapter2Kind`, sprites `Resources/Patience/<kind>_petal|_centre`, `PreviewKind` for editor previews; `Store/artkit/hibiscus_options.py`); the shop laid out at runtime (`ShopManager.LayoutCards`: sorted cheap to expensive, three columns, `cardScale` 0.62, the content box sized to the rows so the arrows and a drag share the same bounds and the lock shows at the bottom either way), purchase feedback in the label's own colour, the day-end money lines centred with the amount on its own line when it would not fit (`LevelManager.SetMoneyLine`), the menu carousel stickers doubled (280), days 1 to 8 five seconds longer and 9 to 16 ten seconds longer (`Level*Data.dayDurationSeconds`; star lines unchanged, checked with the driver). 16 October night: the shop popup in a chalk hand (Gloria Hallelujah SDF, OFL, in `TextMesh Pro/Fonts/Gloria Hallelujah`; blue price, a `specialNote` per card in chalk yellow), the heart cookies cut along their biscuit edge (`Store/artkit/cookie_hearts2.py`) and the plate with a porcelain rim (`cookie_plate.py`, originals in `Store/artkit/plate_orig`), Cookies bake 6 s, a fixed food per chapter on its own station (`SessionManager.FixedFood`: Coffee, chapter 2 IceCream; `FixedStation`, `StationGroup`: the machine is the handle, cups and tray follow; tray sprite `Resources/Table/coffee_station`), chapter 2 unlock items (`UnlockChapter`: Cookies, Coconut show with a lock in the shop until chapter 2 is started), the Coconut food scripts (table object and shop card from `/tmp/ap/add_coconut.cs` once the sprites exist), stickers' third run with names and serving prices; 16 October 21:00: the chapter 2 kitchen window as its own masked object (`KitchenWindow`, built at kitchen load, beach through the glass, the palm on the left with a pulsing glow on its coconuts, the apple tree hidden), the Coconut food in both scenes (`Assets/Images/coconut`, Gemini's tray in four states; `Assets/Prefabs/Coconut.prefab`; shop card 700, chapter 2 lock), the shop popup as a centred measured block (price, two-line description, one-line green note, texts in the Shop scene cards), the coffee cups deeper on the station tray; cookie art handed to the owner (`Store/handoff/cookies`); the cookie plate removed from the kitchen cabinet (cookies are an oven item only); the pot sprites re-centred (table objects moved back, `Store/artkit/pots_orig`); the chapter 2 window's night view (`KitchenWindowNight`, `DayNightTint.CurrentStage`) and the coconut cluster with the apples' halo (`Resources/Chapter2/coconuts`); a yellow lock for the shop cards (`Resources/Map/lock_yellow`); the Visuals tab back in its old section order (sprite sheets from `Store/visuals`); `DayNightTint.CurrentStage` is a static value written each frame (the tint's object is hidden while the kitchen is open). Late play test 16 October (`run_2026-10-16_night2_days.tsv`): 16 days clean, three stars on 15 days, 8,155 left after buying every food and person, so a perfect chapter 1 earns about 14,000 against the 8,740 shop (owner's balance rule not met; owner decides on prices). Chapter 2 build started 16 October night behind `Chapter.Count = 1` (a static field now, flipped by the `ch2_*` recipes for tests): `Level25..32Data`, the map in pages (`MapScreen.page`, `StarsCh2`, `Resources/Map/bg_ch2`, `title_ch2`, `arrow_right`, `arrow_left`), `Resources/Chapter2/bg_day`, `bg_night` (DayNightTint fades the painted night in), `cloth` (the owner's stripes), `shop_bg` (the owner's pick), all through `Chapter.Art` |

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
     **Next** (Who / Now / Waiting-on table, always first; each cell one bullet list, rewritten whole at the start
     and end of every batch: the owner reads it first), **Q&A** (only answers to questions the owner asked, every
     question gets one, cleared once ticked), **Current** (the task in hand: dated bold lead, short bullets, pictures,
     newest first; superseded entries removed), **MVP** (the next release), **Backlog**, **Analysis** (store and ads
     numbers only). The Chapter 2 section was removed at the owner's word on 18 October 2026; never re-add it.
     Questions to the owner go in Next's "You" row, not in Q&A. Every Q&A and Current entry ends with a check list
     (`- [ ] Seen, remove this` plus the choices); read the doc with `sinceRev` before each write, delete what is
     ticked, act on ticked choices. Removed entries are in the doc history (`view` with `atRev`). The full playbook,
     grammar and current block ids are in Claude's memory file `report-writing-playbook.md`.
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
| `FoodTruckCafe/Docs/tools/recipes` | Every editor recipe (`*.cs` method bodies for `execute_code`) and capture shell scripts; `recipes/ap/` holds the 18 October play, session, kitchen, popup and ice cream tap recipes; `capture_s19.sh` captures every screen of both chapters plus iPad and iPhone SE (bash, about 15 minutes) | yes |
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
| `Desktop/Sept/Store/handoff` | Hand-off history: `icloud_backup_2026-10-18_2030/` (the old per-purpose iCloud layout with the layered PSDs), `layered/` and `layers.py` (retired PSD builder), `owner_updates_<date>/` (copies of what the owner drops in iCloud). The live hand-off is iCloud Drive `FoodTruckCafe art/`: ONE flat folder, one object per PNG, named `<group>_<object>.png`, table items at 40 degrees, no docs, no PSDs, no sheets (owner, 18 October). The owner's own `Updates/` subfolder inside it is their drop place (never delete owner-made folders); check it at the start of every art batch, cut the files into the game, refresh the flat copies | no |
| `Desktop/Sept/Store/artkit/venv` | The durable Python for art scripts (numpy, pillow, scipy, opencv, pytoshop, psd-tools) | no |
| `~/.private_keys/AuthKey_<id>.p8`, `asc.json` | App Store Connect API key, key id, issuer | never |

Python with numpy, scipy, pillow, opencv, pytoshop, psd-tools: `Store/artkit/venv/bin/python` (made 19 October);
plain `python3` has no numpy.

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

- **Chapter 2 pieces (17 October):** the kitchen blender is its own object (`KitchenBlender.Apply` from the first
  oven's Awake): chapter 1 shows the owner's cream blender cut from the background (`Resources/Kitchen/blender`,
  the background `kitchen_bg_notree.png` has it filled in), chapter 2 Gemini's steel one (`blender2`) whose drawn
  swirl (`blender2_fill`, tinted) churns through the four ice cream colours on a tap (`BlenderAnim`) and then
  refills the ice cream station. **Ice cream** (`IceCreamStation` on the table object `IceCream`, the chapter 2
  fixed food, no purchase, no tray under it): four sections of three scoops, the holder; tap the holder or a section
  for a floating cone, section taps add scoops (three max), the red cross clears, a tap on the cone serves the
  oldest order wanting that count and those flavours in any order (`OrderItemInstance.variant`, sorted letters
  v s c m; `OrderSystem.TryServeItem(food, variant)`); the order bubble builds the cone from the parts
  (`IceCreamOrderIcon`, `Prefabs/IceCream`); 35 points flat. Sprites `Assets/Images/icecream` from
  `Store/chapter2/icecream_station/cut_station.py` (ChatGPT's station turned metal) and ChatGPT's cones sheet.
  **Coconuts:** four on the kitchen palm (`CoconutInKitchen` on the window's cluster, only when Coconut is on the
  menu), one per tap onto the tray (a full tray refuses), regrow after 8 s like the apples; the palm picture has no
  painted coconuts. Since 21 October Coconut is a chapter 2 food in `FoodChapter` (only the chapter 2 kitchen has a palm,
  so on a chapter 1 day it was unservable); test session recipes enable it after `SetCurrentLevel(16)`. The autoplay driver builds cones (`step.cs`); `driver_ch2.py` runs days 17 to 32.
  **At night (21 October evening)** the palm darkens like the chapter 1 apple tree: a navy copy of the palm sprite (`PalmNight`, a
  child of the window's Palm) fades in with the day's clock in `DayNightTint`'s window colours (dusk 0.40, night 0.78 alpha),
  the coconut cluster sits above it and stays bright, like the apples; the old multiplicative palm dim is gone. Captures:
  `recipes/capture_cn21.sh` (chapter 2 kitchen by day and at night, `night_hold.cs` freezes the clock at night).

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
- **21 October fixes, verified by `Docs/tools/recipes/verify21/run21.sh` (+ `run21c.sh`):** the chapter 2 map arrow opens
  only once chapter 1 is done (`MapScreen`), Continue works right after the tutorial, a food switched on in the menu is
  draggable at once (`LevelManager.ReapplyServeableItems` re-runs `EnableArrangementMode`), the blender refills the
  station as the blend starts (closing the kitchen mid-blend killed the coroutine), the palm cluster shows the real count
  after a kitchen reload and resets on a new game, out-of-season stickers are greyed in the carousel (`MenuBook.OwnsFood`),
  no ice cream taps under the finish board. Runs of 21 October: `run_2026-10-21_days.tsv` (16 days, three stars each),
  `run_2026-10-21_ch2_days.tsv` (day 17 two stars, the rest three), no exceptions.
- `Docs/tools/recv.py <outdir>`: local receiver (127.0.0.1:8765) to pull ChatGPT/Gemini images out of the page (POST the
  `blob:` src from the JS tool). ChatGPT free plan: exactly three pictures a day.
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
  in `Store/redesign/ai/<item>/prompts.md`; cut the states with `Store/redesign/ai/cut_states.py`. Done 13 October: cake, bread,
  apples (bread and apples then reverted to the originals at the owner's word). 21 October: a second round briefed with
  the owner's heart-rim plate as the camera reference (`Store/redesign/ai/chatgpt_2026-10-21/`, README there); the
  owner picked the apple basket and the bread basket, juice stays as drawn. Cut by `cut_pick.py` in that folder: the
  basket body scaled and placed on the old frame's basket footprint (canvas sizes kept, 366x379 and 496x496), ChatGPT's
  soft shadow repainted as the hard navy, the white inside the handle made transparent; the replaced frames are in
  `cut/orig_assets`. Still to brief: choux, coffee cups, machine, cake redo (flat napkin, no pink cut face). ChatGPT free plan: about three pictures a day; wait a few
  seconds after an upload before typing or the text is lost. Gemini keeps a style but redraws shapes.
- Other scripts: `daisy_sprites.py` (patience petals), `cloth_picnic.py` (cloth), `edge_soft.py` (edge shadow),
  `pots_round.py`, `money_hen.py` (fallback hen), brush engine `Store/artkit/brush` (26 brushes, Da Cookie grain).

- **Cookie plates (18 October night):** the plate holds six cookies (`Cookies.cs`, `customMaxCount` 6 in GameSceneOne,
  sprites `Assets/Images/cookies/k0..k6`, the full plate k6 is the shop card picture). Built by
  `Store/artkit/plate_hearts/plate_six2.py <plate> <red> <choc> <pink> <outdir>`: the owner's heart-rim plate
  (`Updates/plate_original_k0_empty.png`, rim ellipse 0.79, a 38-degree camera, left as drawn) with the three
  40-degree hearts (all three the owner's repainted hearts since 21 October 12:04, when their red landed in Updates; the old red cut is in `Store/artkit/plate_hearts/old_red_2026-10-18`; capture chain `recipes/capture_rc21.sh` with `ap/ch2_cookies.cs`),
  three where the originals sat and three on top turned 24, 20 and 10 degrees with a soft shadow; the same hearts
  are the order icons `cookie_red/choc/pink.png`. A driver run with six per plate scored the same as with three
  (`Docs/tools/autoplay/run_2026-10-19_sixcookies_days.tsv`), so no star line moved. Plain `python3` lacks numpy;
  use `Store/artkit/venv/bin/python`.
- **Table item shadows (18 October):** every table sprite carries the owner's hard navy shadow (rgb 45,45,66 at 82 %) to the lower left, light from the upper right; new sprites go through `Store/table/shadows/navy_shadows.py` (originals in `orig/`), and a padded canvas means converting any rect fractions that refer to the sprite (the ice cream station's in GameSceneOne).

## 9. Store and release

- **Build:** bump `bundleVersion` / `buildNumber: iPhone:` in `ProjectSettings/ProjectSettings.asset`; close the editor;
  `Unity -batchmode -quit -projectPath . -buildTarget iOS -executeMethod CommandLineBuild.iOS -exportPath
  ../Builds/iOS-<v>-<b> -logFile ../Builds/export-<v>-<b>.log` (about 4 minutes); archive with xcodebuild
  (`ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES=NO`, about 10 minutes), upload with `-exportArchive` and
  `Builds/archives/exportOptions-upload.plist`; poll `GET /v1/builds?filter[app]=6755611355&sort=-uploadedDate`
  until VALID (5 to 20 minutes). Full steps: `Docs/ios-testing-and-release.md`. Reopen the editor afterwards.
  Editor open, or batchmode without a licence (18 October: the Hub's licensing client needs a Mac admin password once):
  export from inside the editor with `Docs/tools/build_in_editor.cs` through the bridge (a `unity_mcp.py` copy with
  `timeout=2400`; the call blocks about 30 s for an incremental export), then the same archive and upload.
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
