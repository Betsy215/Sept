# Food Truck Cafe: manual for Claude

Read this first in every session on this project. It is the current state: what the game is, the owner's rules,
where things live, how the game works now, and how each recurring job is done. Dated change notes are in
`Docs/history/` (the 12 to 13 October log, the pre-rewrite manual and TODO, the October system notes and the
finished plans are there).

Last updated 24 November 2026: the whole manual was refreshed against the code, the git log and the memory notes (sections 1 and 2 earlier the same day, sections 3 to 11 in the evening).

**Review of 25 November 2026:** `Docs/review-2026-11-25.md` lists the open bugs (R1-R19), surprises for newcomers and
the market research. Read it before changing code. The out-of-date docs it found were cleaned up on 26 November 2026:
`Docs/architecture.md` and `Docs/systems/*.md` were rewritten against the code (each says when it was last verified),
the October notes moved to `Docs/history/`.

**Chapter 3 (winter) and the secret chapter (Day of the Dead):** the owner's plan for the rest of 2026 is in
`Docs/chapter3-and-secret-plan.md` (15 November 2026). Read it before any work on either.

## 1. State of the project

| Item | Value |
| --- | --- |
| Game | Food Truck Cafe, a portrait 2D bakery-truck game for iPhone and iPad |
| Engine | Unity 2022.3.50f1, C#, Unity Ads 4.17 (iOS SDK 4.19), iOS target 15.0 |
| Owner | Betsy (GitHub Betsy215, developer name BetzzzGame). Use they/them unless told otherwise |
| Bundle id / app id / team | `com.BetzzzGame.FoodTruckCafe` / 6755611355 / XY39924J9T |
| Repo | https://github.com/Betsy215/Sept (**public**: never commit keys, issuer ids or tokens), branch `main` |
| Live | **2026.11.24 (build 1)**, live since 25 November 2026 (approved overnight, released by itself; the store header E3 was set again by hand that day: it does not carry over). Before it: 2026.11.23 (2), 2026.11.20 (0), 2026.11.18 (3), 26.11.22 (35, with the 15 Stars purchase), 26.11.21 (28, with Remove Ads US$1.99), 26.11.13 (24) |
| On main, in no build yet (26 November 2026) | Day 9 gift card without text; menu stickers in Delius with bigger prices; waffle machine 6 s; Kid orders like Girl and Boy, Chef the day's maximum; no notification prompt. The owner has not asked for a test build yet |
| Version rule | Since 18 November 2026: version = YYYY.MM.DD of the build day, builds 0, 1, 2 that day; a version that went live closes its train (next test builds take the next day's date). Set with the setver recipe, check with getver; `Docs/tools/test_build.sh <ver>-<n>` exports, archives and uploads to TestFlight; afterwards only reset `preloadedAssets:` to `[]` in ProjectSettings.asset (a `git checkout` of ProjectSettings reverts the version) |
| Release tool | `Docs/tools/asc_release.py` (status, create, build, whatsnew, auto, submit, release); store texts as constants in it. No submission, release or store-text change without the owner's yes each time |
| Next | The C chapter (chapter 3): art brainstorm since 24 November 2026 (indoor cabin cafe; winter and desert ideas; candy-pink corduroy cloth). Drafts in `Desktop/Sept/Store/cchapter_2026-11-24c`, shown in the report's Current section |
| Unity Ads | Dashboard project 568be95c, game id 6074412, placements `Interstitial_iOS` and `Rewarded_iOS`; TestFlight builds show test ads (sandbox receipt check) |

## 2. The owner's rules

1. **The game is live. Change code only for a real, confirmed bug** or what the owner asks for. No refactors or
   re-tuning on your own; write ideas into `Docs/TODO.md`. The "Leave alone" list there holds things the owner chose
   not to fix.
2. **No App Store submission, release, pricing or availability change without an explicit yes in the same
   conversation.** A TestFlight upload is fine when the owner asks for a build.
3. **The owner reads the report, not the terminal.** Since 9 November 2026 the report is the HTML page
   "Food Truck Cafe", https://claude.ai/artifact/VeYt7HSLpyotQEW5sTMvPP (slug `e7fe0ca8-59d9-4f3f-b8c6-7ed7a3495e06`),
   in the style of the owner's bakery pages (they asked for it: "the style is cuter"; the Claude Doc stopped
   scrolling on their MacBook). Source, structure and update recipe: `Docs/tools/report_page/README.md`.
   The structure is fixed (owner, 9 November: "the report had fixed sections ... stop keep changing it"):
   **Next** (You / Me), **Q&A** (the owner's questions, newest first), **Current** (today's work, newest first),
   **MVP**, **Backlog**, **Analysis** (store and ads numbers only), **Marketing** (since 18 November 2026: the App
   Store page, ratings, keywords, events; always there), plus two pages that "clear the report" never touches:
   **Visuals** (real captures) and **Mechanisms** (game data in numbers: prices, kitchen times, days, customers per
   day, day lengths and money). The C chapter page existed 19 to 24 November 2026; the owner removed it ("this becomes
   the main focus now") and C chapter work now goes in the main sections (Current with ticks, Next).
   **Q&A is back since 27 November 2026** (the owner meant clear its content, not remove it; 26 Nov it was removed by mistake): the sections are now Next, Current,
   MVP, Backlog, Analysis, Marketing; an answer to the owner's question goes in Current.
   Never add, rename, remove or reorder a section unless the owner asks.
   At the end of every exchange: read the ticks, drop approved cards (approved work moves to MVP), refresh Next, publish.
   Every Q&A and Current entry ends with check boxes ("Seen, remove this", then the choices); they are the
   owner's action buttons and are saved in the page's db (doc `ticks/state`). Every session that touches the
   report reads them first (`db_get`), deletes the Seen entries and acts on the ticked choices before any other write.
   Fewer words always; one picture beats a paragraph; real captures only; no instruction text in the page.
   The old Claude Doc "FoodTruckCafe Code Review", https://claude.ai/code/artifact/726347e1-dd8c-42fd-917b-be80474ff9ae,
   holds everything up to 9 November (Report and Visuals tabs) and is history only.
   Chat replies are one or two lines plus the link; substance goes in the page.
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
| `FoodTruckCafe/Assets/Scripts` | Game code by area: `LevelControl`, `ItemsAndOrders`, `Customer`, `Food`, `Kitchen`, `Map`, `Shop`, `Session`, `Scoring`, `Ads`, `Purchases`, `Analytics`, `DailyTreats`, `Tutorial`, `Utils` | yes |
| `FoodTruckCafe/Assets/Entity/Level*Data.asset` | Day data. The scene's `LevelManager.allLevels` uses Level1..12 (chapter 1) and Level17..32 (chapter 2); Level13..16 are unused leftovers of the 16-day chapter 1 | yes |
| `FoodTruckCafe/Assets/Resources` | Pictures and sounds loaded by name (`Chapter2/`, `Kitchen/`, `IceCream/`, `Cookies/`, `Journal/`, `Map/`, `UI/`, `Decor/`, `Yoga/`, `Audio/`, ...) | yes |
| `FoodTruckCafe/Docs/tools` | Release tools (`test_build.sh`, `asc_*.py`), the bridge client (`unity_mcp.py`, `unity_mcp_long.py`), art helpers (`glow_brush.py`, `cloth_front.py`, `icecream_row_cut.py`, ...), `report_page/` (the report's source) | yes |
| `FoodTruckCafe/Docs/tools/recipes` | Editor recipes (`*.cs` method bodies for `execute_code`), capture scripts `capture_*.sh`, per-round folders `r26`..`r34` (newest last), `ap/` session and play recipes | yes |
| `FoodTruckCafe/Docs/tools/autoplay` | Autoplay drivers (`driver_now.py` chapter 1, `driver_ch2_now.py` chapter 2) and their recipes | yes |
| `FoodTruckCafe/Art/kitchen_layers` | The owner's kitchen painting as 21 layer PNGs | yes |
| `FoodTruckCafe/Captures`, `Assets/Screenshots` | Capture logs; where the bridge's `shot` writes | no (gitignored) |
| `Desktop/Sept/Builds` | Xcode exports `iOS-<ver>-<n>`, `archives/` (archives, logs, `exportOptions-upload.plist`) | no |
| `Desktop/Sept/Store/screenshots/final_r33` | The live store screenshots (2026.11.23 on: seven iPhone 6.9-inch, iPad 13-inch) | no |
| `Desktop/Sept/Store/video` | Preview cuts (`cut_r31.py`, `FoodTruckCafe_preview_r31.mp4` is the live preview) | no |
| `Desktop/Sept/Store/report` | Visuals page builders (`visuals_r33.py` newest) and old report pictures | no |
| `Desktop/Sept/Store/<topic>_<date>` | Art work per topic and day (e.g. `cookies_2026-11-17`, `cchapter_2026-11-24c`); each has its scripts and `prompts.md` for ChatGPT/Gemini briefs | no |
| `Desktop/Sept/Store/redesign`, `Store/ui`, `Store/characters`, `Store/chapter2`, `Store/artkit` | Older art work: table items and kitchen flow (`redesign/ai`, `redesign/kitchen_flow_2026-11-20`), UI papers and glass, customer sprites (the 13 November 2026 repaints cut by `characters/cut_pil2.py`, the Boy's own head by `boy_headswap.py`), chapter 2 art, the art scripts and brush engine (`artkit/brush`) | no |
| `Desktop/Sept/Store/artkit/venv` | Python with numpy, scipy, pillow, opencv, psd-tools (plain `python3` has no numpy) | no |
| iCloud Drive `FoodTruckCafe art/` | Hand-off to the owner: one flat folder, one object per PNG. Their drop places: `Updates/` (art; check it at the start of every art batch) and `bugs/` (TestFlight bug screenshots; check every session, delete an image once its bug is fixed). Never delete owner-made folders | no |
| `~/.private_keys/AuthKey_<id>.p8`, `asc.json` | App Store Connect API key, key id, issuer | never |

## 4. Doc map

| File | Read when |
| --- | --- |
| `CLAUDE.md` | Always loaded. Coding rules and the offline compile command |
| `Docs/ios-testing-and-release.md` | **The release guide**: test builds, TestFlight, store releases, store assets, upload errors |
| `Docs/tools/report_page/README.md` | **The report guide**: the page's source, sections, ticks and the update recipe |
| `Docs/art-style.md` | **The art style guide**: the game's looks and how new art is made and briefed |
| `Docs/architecture.md` | Before touching gameplay code: scenes and flow, objects that outlive a scene, which script does what, where to change things (verified 26 November 2026) |
| `Docs/systems/*.md` | Before changing one system; short notes verified against the code on 26 November 2026 (index in `Docs/systems/README.md`): 01 scene flow and startup, 02 days/orders/customers, 03 food/drag/stock, 04 kitchen, 05 money/stars/save/shop, 06 audio/UI/fades/iOS settings, 07 identifiers and PlayerPrefs keys, 08 map and chapters, 09 ads/analytics/purchases. Correct the note in the same commit as the code |
| `Docs/chapter3-and-secret-plan.md` | Before any chapter 3 (winter) or secret chapter work |
| `Docs/TODO.md` | Open items, Leave alone, known issues |
| `Docs/history/` | History only, never the current state: dated logs, the old manual and TODO, the October 2026 system notes (`systems_2026-10/`, `maintenance_2026-10-07.md`), the finished plans for chapter 2 and the menu book (`chapter2-plan_2026-10-14.md`, `menu-book-plan_2026-10-14.md`) and the October economy model (`economy_sim_2026-10-12.py`) |
| Memory notes (`~/.claude/projects/-Users-betzthebest-Desktop-Sept/memory/`) | The owner's picks and the detail behind each feature, by topic; code wins where they disagree |

## 5. Working in the code

- Press Play from `Assets/Scenes/MainMenu.unity`; other scenes need the managers it creates. Scenes: MainMenu, Map,
  GameSceneOne (the table), KitchenScene (loaded over the table), Shop.
- Compile without Unity: the csc command in `CLAUDE.md` (no output = compiles). Use the newest `Library/Bee/artifacts/*.dag`.
- Strings are identifiers (foods, characters, upgrades, shop cards): grep everywhere before renaming. Add serialized
  fields, never rename them. Three Shop methods are wired in scenes (`LoadNextGameLevel`, `ConfirmPurchase`, `CancelPurchase`).
- Input is EventSystem-based (3D BoxColliders plus a PhysicsRaycaster). No `OnMouse*`.
- Money is whole dollars since 26 November 2026 (still a float through `SessionManager.RoundToCents`, `CanAfford`):
  round anything new that reaches the wallet with `Money.Whole`, show every amount with `Money.Format` ("$ 1,814").
- Most new UI is built at runtime in code (map, receipts, cards, tutorials, journal, gift cards); look for the builder
  class before searching the scene.
- **Never edit a scene file on disk while the editor has it open** (a "modified externally" dialog blocks the bridge).
  Edit scenes through `execute_code` (open, change, save, reopen MainMenu) or with the editor closed.
- A new food needs its sale price in the ScoreManager points table, a `SessionManager.FoodStation` entry, a menu sticker
  and a shop card with a cookie picture (`Resources/Cookies/<card name>.png`).
- Days are counted with one running index in the save and analytics (chapter 2's first day is index 12); the player
  sees days per chapter (`Chapter.DayInChapter`). Go through `Chapter.FirstDay / DaysIn / Of / TotalDays`, never 16 or 12.
- Hidden work stays out of releases behind `Chapter.Count` and the TestFlight-only test buttons
  (`AdsInitializer.IsTestFlightBuild`); see `Docs/chapter3-and-secret-plan.md`.

## 6. The game as it is now (24 November 2026)

Numbers players feel (sale prices, kitchen times, customers per day, day lengths, money) are on the report's
Mechanisms page, source `Docs/tools/report_page/mechanisms.html`; keep it current when they change.

- **Flow:** MainMenu (scrolling iced-cookie background, `ScrollingBg`; cookie truck; Daily Treats) → Play/Continue →
  new game: the chapter 1 tutorial ("Day 0", `TutorialDirector`, its Ready-to-go card on doily paper) → Map →
  tap a day → day card → Play → between-days interstitial (from day 3 on, `MapScreen.FirstAdDay`; Remove Ads, TestFlight
  and a 45 s spacing skip it) → GameSceneOne: arrangement (none on day 1, `GamePhaseManager.SkipsArrangement`; day 2
  teaches it on the real coffee machine and coffee bags, `ArrangeLesson`), Ready, the day → day-end journal page
  (stars, sale, tips; the Cookie sprinkles celebration, `StarCelebration`) → Next → Shop → Next → Map. Pause Home →
  Map. `UpdateGate` blocks an outdated copy with an UPDATE card once a store version is 36 h old (from 2026.11.23 on).
  `RatingPrompt` asks Apple's rating popup once, in the first shop after day 3.
- **Chapters:** `Chapter.DaysPerChapter = { 12, 16 }`, `Chapter.Count = 2`. Chapter 1 is the picnic field, chapter 2
  the beach (`Resources/Chapter2`: day and night backgrounds, cloth, palm, shop background; `Chapter.Art`). Day numbers
  restart per chapter for the player. **Chapter 2 opens after chapter 1 day 9** (`SessionManager.Chapter2OpensAfterDays`):
  day 9's star comes on a gift card when day 9 is reached, and when day 9 is finished the map opens chapter 2 by itself
  (`chapter2Claimed`, a chime, the truck drives to the arrow); days 10 to 12 stay playable. **Bonus days:** each
  chapter's last three days are hidden until the day before them is reached, then revealed once on the map
  (`MapScreen.BonusDays`, `SessionData.bonusShown`). The map truck is the truck cookie, stands at the next day and
  drives to a tapped day before its card opens. Chapter 2's first day: its own tutorial (waffle machine, blender,
  ice cream) and the gift of a fourth patience flower; every ice cream that day is a waffle bowl of one or two scoops.
- **Days:** a day lasts `dayDurationSeconds` (chapter 1: 48 s on days 1-2, 58 s from day 3; chapter 2: 95 s, set
  24 November 2026). A customer sets off every `timeBetweenOrders` (4 s chapter 1, 5 s chapter 2), at most two waiting.
  The day ends when the clock is out and no customer is left (`LevelManager.TryEndDay`); `ordersPerLevel` no longer
  ends a day. No countdown; `DayNightTint` goes to dusk at 65 % and night at 85 % of the clock (fireflies and lit truck
  windows at night). The day's end, Restart, Home and the shop's Next stop the whole kitchen (`KitchenDay.End`).
- **Orders and patience:** `OrderSystem` keeps two slots (`ActiveOrder`; `TryServeItem` serves the oldest order needing
  the food). A customer thinks 2 s (three dots, `OrderSystem.ThinkSeconds`) before the order shows. Patience is three
  flowers in chapter 1, four hibiscus in chapter 2 (`PatienceMeter.BaseFlowers / Chapter2Flowers`), each as long;
  every served item fills the meter up to the next whole flower. The receipt rises where the bubble was with a tip
  level by flowers left: Perfect (green tag, the cha-ching), Nice (yellow), Just OK (grey) (`OrderSystem.ServedLevel`,
  sounds in `Resources/Audio/Orders`). Haptics on serve (`Haptics.cs`).
- **Money:** sale price per item (ScoreManager points table; Mechanisms has the list) plus a tip of price x seconds
  left x 10 % x the customer's tip multiplier; a replayed day pays half (`LevelManager.ReplayPayFactor`); three stars
  pay $100 once per day (`ThreeStarBonus`). `SessionManager` holds the wallet; the hen board (`MoneyFlipClock`) counts it.
- **Stars:** from customers served whole, not money (`StarRule`, owner 14 November 2026): 3 = every customer, 2 = 80 %
  (`TwoStarPercent`, rounded to whole customers), 1 = more than half. The hen shows the projected end of the day.
  `LevelData.starThreshold1..3` are no longer read. Stars are also a currency: earned stars + bought (15 Stars, US$0.99)
  + granted (day 9) + bought with coins (1 star for 999) - spent (`SessionManager.StarsAvailable`).
- **Table:** cloth with a front drop into the bottom safe area (`TableLayer.UpdateClothFront`, `Resources/Table/cloth_front`,
  `Chapter2/cloth_front`; made by `Docs/tools/cloth_front.py`). The back band holds the fixed things aligned on one line
  (`TableBackRowAlign`): the pull handle to the kitchen (`PullHandle`, switch `KitchenButtonOverTable.Handle`; movable up
  and down while arranging), the hen, the flower pots (`DecorGate`, +15 % patience each) and the candy jar. Every food
  starts with its whole box inside the drag area. The coffee station row (machine, cups, tray; `FixedStation`) keeps
  its moved spot. A swipe right opens the kitchen, left closes it (`KitchenSwipe`).
- **Candy jar:** not on the table and not sold before day 3; when day 3's play begins the jar arrives on a gift card
  with two candies (`CandyGift`), then candies are a shop buy (three per buy). A tap gives one candy to the customer
  lowest on patience and fills their flowers to full (`CandyJar`, `SessionManager.Candies`).
- **Menu book:** `MenuBook` (button beside Ready during the arrangement): sticker carousel on the oven-note paper; a food
  plays when bought AND on (`SessionManager.IsFoodItemActive`, `SessionData.enabledFoodItems`, saved across days);
  no caps (`StationCap` 99), at least two on (`CanDisableFood`). Chapter-only foods (`SessionManager.FoodChapter`):
  Apple and Coffee chapter 1, IceCream and Coconut chapter 2; Cookies and Coconut stay locked in the shop until
  chapter 2 opens (`UnlockChapter`).
- **Kitchen** (`KitchenScene`, slides in over the table; its clock stops on pause, `KitchenClock`):
  ovens (one at the start, `OvenUpgrade` card 500 for the second and third, `SessionManager.OvensOwned`); a tap on an
  idle oven opens the recipe picker (`OvenPicker` in `OvenAssigner.cs`); bake times `OvenKitchenBase.BakeTimes`
  (Bread 5 s, Choux, Cake, Cookies 3 s). **Kitchen flow for choux and cake** (`KitchenFlow`): the bake lands on the
  baking tray or cake stand in the kitchen, the mixer runs, the fruit board cuts, the done picture sparkles
  (`DoneStyle 1`) and the table refills; it runs by itself (a tap starts the next step at once). Apples and coconuts
  grow back; the blender refills the ice cream wells (`KitchenBlender`). **Kitchen arrangement** (`KitchenArrange`):
  blender, board, whisk, basket, cake, coffee bags, baking tray, mixer and juice can be moved on the counter top
  (cabinet openings snap); a red piece keeps Ready off. Tap-me glows on usable things (`KitchenGlow`, `GlowFx`).
  The **Kitchen upgrade** (15 stars, `KitchenSpeed.Factor` 0.75) makes every kitchen wait a quarter shorter.
- **Chapter 2 foods:** **Ice cream** (`IceCreamStation` on the table, no purchase): wells of four scoops per flavour,
  two flavours until the "Two more flavours" card (900, `IceCreamUpgrade`); cones from a row of six refilled by the
  waffle machine behind the station (tap: a session bakes two); build a cone by tapping wells (up to three scoops), a
  tap on the cone serves the matching order (count and flavours, `OrderItemInstance.variant`). **Coconut** (700): picked
  from the palm in the chapter 2 kitchen, five on the tray.
- **Shop** (`ShopManager`, `ShopCookies`, `ShopGlass`): a liquid-glass panel with three glass tab buttons, Stars | Coins | $
  (Coins first). Every card is an iced cookie; a bought cookie is eaten in bites with a crunch and the item leaves the
  shop for the book (`ShopManager.IsOwned`; candies, the star pack and the oven card until three ovens stay). NEW tags
  on unseen cards and tabs (`NewTag`); cards the player cannot afford show a red price and do not open. Locked cards
  show a padlock: Chef (20 stars) and Dining table (25 stars) "Coming soon". Coin prices (chapter 1): Juice 110,
  Candies 100, Apple 300, Grandma 380, Extra cup 380, PotGreen 450, Cookies 600, Coffee machine 650, Chef customer 700,
  Choux 800, PotFlowers 900, Cake 1000, Businessman 1400 (the yoga customer card 220); chapter 2 adds Coconut 700,
  Oven 500 (x2), Two more flavours 900. Stars tab: the **yoga lady** (staff, 15 stars, `YogaHire`: poses in the
  background, each finished pose fills every waiting customer to full), Kitchen upgrade 15. $ tab: Remove Ads US$1.99
  (`RemoveAds`, stops the between-days ad only) and 15 Stars US$0.99 (`StarPack`, consumable), shown only while the
  App Store offers them; Restore purchases.
- **The book** (button above the shop's up arrow): a red book with three tabs: Journal (milestone pages, earned by
  `SessionManager.EarnJournal`: open, star3, icecream, cake, choux, machine, kitchen; `Resources/Journal`), Staff (once
  someone is hired), My cookies (owned things). A new page shows itself in the shop and flies into the book.
- **Customers:** Girl, Boy, Kid, Grandma, Businessman, Chef, Yoga (`Assets/Scripts/Customer`); the repainted set of
  13 November 2026 (faces kept). The Girl is forced only while a tutorial runs.
- **Daily Treats** (`DailyTreats.cs`, MainMenu): a four-day row on a mint card, 50 coins a day and a gift on day 4,
  paid into the next Continue or new game (`ftc.daily.*` PlayerPrefs), waits until the first tutorial is done; "One more
  treat" rewarded ad after collecting (`DailyTreatsAd`).
- **UI look:** buttons are cookies that press down with a crunch (`CookiePress`), panels are paper (doily day card,
  journal page day end, receipt), hand-written font Delius in the shop popups and the book. 60 fps (`FrameRate`).
- **Ads, purchases, analytics:** Unity Ads (`AdsInitializer` retries a failed start: 5, 15, 30, 60 s then every 120 s;
  `InterstitialAdService`, `RewardedAdButton`); Unity IAP (`RemoveAds.cs`); Unity Analytics (`AnalyticsLog`: tutorial
  steps, day start/finish/left, purchases, ads; store builds to `production`, editor and TestFlight to `development`;
  a new event or parameter must be defined on the Unity dashboard first).
- **Test-only:** the "Chapter 2 test" main-menu button (editor and TestFlight only, `Chapter2TestButton`: everything
  bought, chapter 2's first day, 100 stars); a test-build count line on the day-end receipt.

## 7. Driving the Unity editor

- Open: `open -a /Applications/Unity/Hub/Editor/2022.3.50f1/Unity.app --args -projectPath <repo>`. The MCP for Unity
  bridge (http://127.0.0.1:8080/mcp) starts on load (Auto-Start).
- From the shell: `python3 Docs/tools/unity_mcp.py exec <file.cs>` (runs the file as a method body in the editor),
  `call <tool> '<json>'` (e.g. `call refresh_unity '{}'` after editing scripts, stop play first; `call manage_editor
  '{"action":"play"}'`; `call read_console` with `include_stacktrace` false), `shot <name>` (Game view capture to
  `Assets/Screenshots/<name>.png`), `reset`. **When a call fails, delete `Docs/tools/.mcp_session` first and retry**
  (needed after every editor restart). `no_unity_session` while Unity runs = the plugin ended its session: only the
  owner's click in Window > MCP for Unity (or a script reload) brings it back. `unity_mcp_long.py` is the same client
  with a 2400 s timeout (exports). Two editors open: set `UNITY_INSTANCE`.
- Recipes in `Docs/tools/recipes` (exec in place): sessions `ap/fresh_session.cs`, `ap/ch1_session.cs`,
  `ap/ch2_full.cs` (chapter 2 with everything bought), `newgame.cs`; play `startday.cs`, `state.cs`, `serve.cs`,
  `kitchen.cs`, `bake.cs`, `closekitchen.cs`, `tomap.cs`, `toshop.cs`, `tomenu.cs`, `ff_dusk.cs` / `ff_night.cs`,
  `ap/stop.cs`; Game view size `gv_<Store1320|iPad|iPhoneSE|Preview886|iPhone16ProMax>.cs` (set it **before** Play:
  `TableLayer` sizes the cloth once); version `setver.cs` / `getver.cs`. The newest round's recipes are in `r33/`, `r34/`.
- **Captures:** `recipes/capture_r33.sh` (the store set, iPhone 6.9-inch and iPad 13-inch; the newest of the
  `capture_rNN.sh` series, each a copy of the last with new names); `recipes/r34/*_capture.py` record frame-by-frame
  videos with the editor paused and stepped (star celebrations, candy gift, bonus days, map). The report's Visuals page:
  run a capture, then `Store/report/visuals_r33.py` (art venv) writes `Docs/tools/report_page/visuals/`.
  Real captures only in the report.
- **Autoplay:** `mkdir -p /tmp/ap && cp Docs/tools/recipes/gv_Store1320.cs /tmp/`, then
  `python3 Docs/tools/autoplay/driver_now.py 1.5 1.0` (time scale, seconds per action: fresh session, tutorial,
  chapter 1's 12 days with the shop between) or `driver_ch2_now.py` (chapter 2's 16 days from a full session); logs in
  `/tmp/ap/run.log` and `/tmp/ap/days.tsv` (`run_ch2.log`, `days_ch2.tsv`). Kill stray drivers first
  (`pgrep -f driver`). The older `driver.py` / `driver_ch2.py` predate the 12-day chapter.
- Editor quirks: Unity Ads shows a "Placeholder" ad in play mode; a recompile wipes editor statics; after an iOS
  export a modal "No Adaptive Performance provider found" can block Play (only a human click clears it; the empty
  `com.unity.adaptiveperformance.none` package is meant to stop it); never `git checkout` during an export.

## 8. Art pipeline

The style rules, the looks per place and how to brief ChatGPT and Gemini are in `Docs/art-style.md`. In short:

- **Owner's art first.** AI only adds missing parts or repaints at a new angle with the art kept; never regenerate a
  face. New art = three drafts (ChatGPT, Gemini, mine) side by side in the report; nothing goes in before the owner picks.
- Work lives in `Store/<topic>_<date>/` with its scripts and `prompts.md`. ChatGPT's free plan gives about three
  pictures a day; pull pictures out of the pages with the clipboard trick or `Docs/tools/recv.py` (memory notes
  `chatgpt-drafts-nov9`, `night-fixes-2026-10-26`).
- Table items are seen from a 40-degree camera (ellipse 0.77); table shadows are one near-black (20, 16, 24) at 60 %
  in each item's own shape; kitchen pieces keep their painted shadows. Tap-me glows: `Docs/tools/glow_brush.py`.
- Buttons and shop cards are iced cookies (Gemini's look, `Store/cookies_2026-11-17`); panels are paper.
- Use `Store/artkit/venv/bin/python` for image scripts. Hand-offs go to the flat iCloud folder (section 3).

## 9. Store and release

The steps are in `Docs/ios-testing-and-release.md` (test build: setver/getver, `bash Docs/tools/test_build.sh
<ver>-<n>`, reset only `preloadedAssets:` to `[]`, commit, push; release: `asc_release.py <ver> create`, `build <n>`,
`whatsnew <file>`, `auto`, `submit`, `release`, `status`). No submission, release or store-text change without the
owner's yes each time.

- **Store page now (2026.11.24):** the owner's What's New, promotional text and description edits of 24 November 2026
  (the "two customers" line is gone), screenshots `Store/screenshots/final_r33` (from 2026.11.23), preview
  `Store/video/FoodTruckCafe_preview_r31.mp4` (from 2026.11.20), header E3 (`Store/store_header_2026-11-24/tray/header_E3_upload.png`) live
  since 24 November 2026. Purchases: Remove Ads US$1.99, 15 Stars US$0.99. Not on sale in China mainland (since
  18 November 2026). App Store page analysis and ideas: memory note `app-store-page-aso`.
- Store rules learned: previews 15 to 30 s, 886 x 1920, in-game footage only; screenshots 1320 x 2868 (iPhone 6.9-inch)
  and 2064 x 2752 (iPad 13-inch); iPhone Duo screenshots required from April 2027; age rating Advertising = Yes.
- Player data: Unity Analytics SQL Data Explorer, production environment (recipe in memory note `unity-analytics`);
  the report's Analysis section holds the reads. Leave the owner's own devices out of every count.

## 10. Open work

See the report's Next and MVP and `Docs/TODO.md`. On 24 November 2026: 2026.11.24 (build 1) is in review and goes
live by itself; the MVP list for the next release is empty.

**C chapter (chapter 3).** The owner's main focus since 24 November 2026, at the art brainstorm stage: an indoor
winter cabin cafe (shop page: the cabin big in front with the truck parked behind; game scene: the inside of a real
cabin that reads like a dessert shop, a big window with winter outside, customers seated facing the viewer behind
table cloths, the serving table a little smaller; candy-pink and white gingham cloth with the front drop). Winter is a
must. Drafts and briefs: `Desktop/Sept/Store/cchapter_2026-11-24c` (`prompts.md`; earlier rounds in
`cchapter_2026-11-21`, `-23`, `-24`, `-24b`); the owner's picks are ticks in the report's Current; detail and
measurements in the memory note `cchapter-indoor-cafe`. **Nothing is built in code yet:** `Chapter.DaysPerChapter`
holds two chapters and `Chapter.Count` is 2, there is no `Resources/Chapter3` and no level data for it. Building it
means a third `DaysPerChapter` entry, its Level assets in `allLevels`, `Resources/Chapter3` art, a map page, and
keeping it hidden behind `Chapter.Count` until release (plan, dates and the secret Day of the Dead chapter:
`Docs/chapter3-and-secret-plan.md`; target mid-December 2026).

## 11. Keeping this manual current

Change it when a fact changes, in place: this file is the current state, not a diary. Put dated notes in
`Docs/history/`, game numbers on the report's Mechanisms page, and save new editor recipes to `Docs/tools/recipes`
(`/tmp` and the scratchpad are wiped).
