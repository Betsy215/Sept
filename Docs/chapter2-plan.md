# Chapter 2 (the beach): implementation plan

Written 14 October 2026 from a full read of the code. Chapter 2 is the goal of the next version. Everything is
built behind `Chapter.Count` (`Assets/Scripts/Map/Chapter.cs`), which stays 1 until the last step, so chapter 1 and
existing saves behave exactly as now.

## Facts found

- `allLevels` in GameSceneOne already has 24 entries; the game stops at day 16 only because of the clamp
  `LastDayIndex` (`LevelManager.cs:82`).
- Cookies are already wired everywhere (table item, points 30, orders list, kitchen gate, shop card) and held back
  by `SessionManager.HeldBackFoods`. Cookies are the template for every new food.
- `LevelData.backgroundSprite` does nothing today (`backgroundRenderer` is null in the scene); the background is the
  `Canvas/bg` Image.

## Chapter number

No save field. Chapter of a day = `1 + dayIndex / 16`; unlocked chapters = `1 + min(levelsCompleted / 16, Count - 1)`.
A save that finished day 16 has `levelsCompleted = 16`, `currentLevel = 16`, so it opens on day 17. Add an int field
only if chapter 2 ever gets an unlock price (new field, never a rename; take the max with the derived value).
`Chapter.Art<T>(chapter, name)` loads `Resources/Chapter{n}/name`, null in chapter 1 = keep the scene's art.

| Thing | Where | Change |
| --- | --- | --- |
| Background | `LevelManager.ApplyVisualSettings` ~527 | `Canvas/bg` sprite = `Art("bg_day")` |
| Night | `DayNightTint.Begin` ~47 | if `Art("bg_night")`, fade a sibling Image by stage; skip chapter 1 moon/stars |
| Cloth | `TableLayer.Initialize` ~71 | swap the sprite before `SetupTableLayout` (same canvas, ppu, top edge as `knockout 1.PNG`) |
| Shop background | `ShopManager.Start` ~38 | swap the `bg` Image |
| Patience flowers | `PatienceMeter` ~21, 92-134 | cache per chapter; petal count from `petals.Length` (hibiscus 5, daisy 8) |
| Map | `MapScreen` | pages, see below |
| Kitchen tree | `KitchenScene` `bg/tree` (14 October: the apple tree is its own Image, `Kitchen/tree_apple.png`; the background `kitchen_bg_notree.png` has the window refilled) | swap the sprite for the coconut palm (owner: "instead of apple tree, use coconut tree in kitchen"); the apples object stays for Apple, a Coconut food would need its own pick object |
| Kitchen window (owner, 16 October) | `KitchenScene` `bg` | The window's view becomes its own object (an Image in a mask the size of the window glass), not part of the kitchen background: chapter 2 shows the same beach as the table scene but simpler and smaller. Resolution rule: the window object is anchored to the background Image's rect (same anchors and anchored position in the bg's local space), the view image uses `preserveAspect` inside a `RectMask2D`, so any aspect ratio crops the view at the glass instead of stretching it. Test at the four Game view sizes (`gv_*.cs`: iPhone SE, 1320 store, iPad, 886) before and after the swap. |
| Coconuts (owner, 16 October) | `KitchenScene`, like `apples` | The coconuts are the pick object, like the apples: their own sprites, one cluster (coconuts grow together), big and obvious at the apples' size and style (`Assets/Images/Kitchen/apples*`: reference size and outline), not drawn into the palm. The palm sprite is the tree only; the cluster sits in the crown with its own `PickItem` object and states (full, picked, regrowing) the way the apples work. |
| Finish text, day range, chapter end | `LevelManager` 82, 631, 684, 698 | done (step 2) |

## A new food (copy Cookies)

1. `Food/X.cs` (copy `Juice.cs`; the class name is found by `FindObjectOfType`).
2. `FoodInKitchen/XInKitchen.cs` (copy `CookiesInKitchen.cs`).
3. Order icon prefab `Assets/Prefabs/X.prefab` (from `Juice.prefab`).
4. GameSceneOne: duplicate the Cookies table object; add to `LevelManager.serveableItems`, `OrderSystem.availableFoods`,
   `ScoreManager.itemPoints` (or it scores 0).
5. KitchenScene: an Image in a free slot, layer `Kitchen`, kitchen script + `KitchenFoodGate`.
6. Shop: duplicate the Cookies card (itemName, price, itemType, info, icon).
7. Gating: `HeldBackFoods` becomes a food-to-chapter map {Cookies, IceCream, Coconut} = 2; `IsHeldBack(f)` =
   chapter of f > current chapter. Shop `BottomBoundary` 1800 in chapter 1, measured in chapter 2.
8. Scene edits through `execute_code` recipes; never edit a scene file while the editor has it open.

Ice cream is ONE food (`IceCream`); flavours are looks only (random sprites, like `RandomSprite.cs`). Coconut is a
second food built like Juice.

## Map

`Build(page)` under a rebuildable root; second star set `StarsCh2`; labels 17 to 32; `bg_ch{n}`, `title_ch{n}`;
truck offsets per page; the right arrow opens page 2 once unlocked (`arrow_right`), page 2 has a left arrow, its own
right arrow stays locked. After day 16, the map opens on page 2 with day 17 bouncing.

## Risks

- `Count = 2` before there are 32 `allLevels` ends the session (`LoadLevel` else branch calls `CompleteSession`,
  Continue stays disabled). Make the 8 day assets first; log an error when `allLevels.Length < TotalDays`.
- Very old (pre-map) saves that finished the last day have `isActive = false`: owner to decide whether to reactivate.
- Saved table positions are shared across chapters (`GamePhaseManager:286`); default positions must not overlap.
- Static caches (`PatienceMeter`, `DayNightTint.starSprite`) must reset when the chapter changes.

## Tests

Compile after each step; `driver.py` full chapter 1 compared with `run_2026-10-13_days.tsv`; save fixtures (fresh,
mid chapter 1, day 16 done, pre-map, inactive); a chapter 2 run from a "16 done" fixture (points not 0, refills,
hibiscus, night picture, "Chapter 2 complete!"); a chapter 1 replay after unlocking (daisies, no beach foods);
iPhone and iPad captures.

## Steps (each compiles and commits alone)

1. `Chapter.cs` with `Count = 1`. **Done 14 October.**
2. LevelManager day range, chapter end, finish text. **Done 14 October.**
3. Level25..32Data and `allLevels` to 32, plus the length check (tuning numbers, no art). **Done 16 October** (draft tuning continued from days 17 to 24; the length check still to add).
4. Food gating by chapter (`SessionManager.FoodChapter`, done 14 October, full-chapter driver run clean); the shop boundary still to do.
5. Map pages (waits for `bg_ch2`, `title_ch2`, arrows). **Done 16 October** (`MapScreen.page`, Cancer until the owner picks; `Store/chapter2/map/map_ideas.py`).
6. Beach background day and night. **Done 16 October** (`Resources/Chapter2/bg_day`, `bg_night`; the painted night fades in over the day picture).
7. Cloth swap. **Done 16 October** (the owner's stripes, `Resources/Chapter2/cloth`).
8. Shop background swap. **Done 16 October** (`Resources/Chapter2/shop_bg`).
9. Hibiscus flowers. **Done 16 October** (`PatienceMeter.Chapter2Kind`, three option sets; the owner picks A, B or C).
10. Cookies on in chapter 2.
11. Coconut (owner, 16 October night): a coconut with a straw, a tray of three on the table (`Coconut.cs`, a copy of Cookies), the pick cluster on the palm in the kitchen (`CoconutInKitchen.cs`), points 30, counter station. **A chapter 2 unlock, playable in any chapter**, like Cookies now: both sit in the shop with a lock until chapter 2 is started (`SessionManager.UnlockChapter`, `IsLockedInShop`; `ShopManager.ShowCardLock`). Scripts and the lock rule done 16 October; the table object, order prefab and shop card come from the recipe `/tmp/ap/add_coconut.cs` once the sprites are in `Assets/Images/coconut/n0..n3.png` and `icon.png` (Gemini strip of four tray states).
12. IceCream (owner's design, 16 October night): an **ice cream station** on the table (the chapter 2 fixed item, on the station tray at the bottom left like the coffee): a simple ice cream box with a few flavours and some cones beside it. Customers order a cone with scoops; the number of scoops and the flavours are random. The player taps the cone: it floats up; then each tap on a flavour in the box adds a scoop of that flavour to the floating cone; a red cross beside the cone being built clears it to start over; the cone is served when it matches (like the other items, a tap on the order). **Rules (owner, 16 October night):** a cone holds at most three scoops; each flavour section of the station serves three times, then its block goes down (empty) until the kitchen refills it. Art in `Store/chapter2/icecream_station`: `gemini_raw_3.jpg` (the four-section station, the owner's pick), `gemini_parts_raw.jpg` (empty cone, four scoops, a stacked example); `gemini_raw_4.jpg` and `gemini_raw_5.jpg` are the style retries (5 is the boldest, closest to the table items; the owner picks); still to draw: the section at three fill levels (full, two thirds, one third, empty), and the parts again in the picked style.
13. `Chapter.Count = 2`, full test pass.
14. Driver, doc 07, MANUAL section 6, TODO.
