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
3. Level25..32Data and `allLevels` to 32, plus the length check (tuning numbers, no art).
4. Food gating by chapter (`SessionManager.FoodChapter`, done 14 October, full-chapter driver run clean); the shop boundary still to do.
5. Map pages (waits for `bg_ch2`, `title_ch2`, arrows).
6. Beach background day and night (art ready: `Store/chapter2/beach_bg/final_day.png`, `final_night.png`).
7. Cloth swap (owner's table pick).
8. Shop background swap (owner's pick).
9. Hibiscus flowers (petal and centre sprites).
10. Cookies on in chapter 2.
11. Coconut (table states, order icon, kitchen and shop icons).
12. IceCream (freezer-well steps, cone icons, kitchen tub).
13. `Chapter.Count = 2`, full test pass.
14. Driver, doc 07, MANUAL section 6, TODO.
