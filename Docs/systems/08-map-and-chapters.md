# 08 Map and chapters

Last verified against the code: 26 November 2026. Paths are under `Assets/Scripts`.

## Chapters (`Map/Chapter.cs`)

- `DaysPerChapter = { 12, 16 }`, `Count = 2`. `Count` is a field, so hidden work can stay behind it.
- There is no chapter field in the save. Everything derives from the **running day index** (0-based; chapter 2's
  first day is index 12): `Of(dayIndex)`, `FirstDay(chapter)`, `DaysIn(chapter)`, `TotalDays`, `IsLastDay(dayIndex)`.
  The save, `levelStars` and analytics use the running index.
- What the player sees restarts per chapter: `DayInChapter(dayIndex)` (the day card, the in-game label).
- `Art<T>(chapter, name)` loads `Resources/Chapter<n>/<name>` and returns null in chapter 1 (the scene's art stays).
- `LevelManager.allLevels` must hold the days in the same order (Level1..12, then Level17..32).
- Never write 12, 16 or 28 in code; go through `Chapter`.

## Which chapter is open (`Session/SessionManager.cs`)

| Member | Meaning |
| --- | --- |
| `CurrentChapter` | Chapter of the day being played (`Chapter.Of(GetCurrentLevelIndex())`) |
| `DaysDoneIn(chapter)` | Days finished in that chapter: the larger of `chapterDone[chapter-1]` and what `levelsCompleted` implies |
| `Chapter2OpensAfterDays` = 9 | Chapter 2 opens after chapter 1's day 9 |
| `Day9StarDue` | Day 9 is reached (8 days done), its star not given yet |
| `Chapter2CeremonyDue` | Day 9 is done and `chapter2Claimed` is false |
| `ChaptersOpen` | 2 once `chapter2Claimed` is saved, or once chapter 2 has a finished day or chapter 1 is complete (`ChaptersStarted`) |

The chapters run side by side after that: each open chapter has its own next day (`NextDayIn`), and chapter 1's days
10 to 12 stay playable. `OnLevelCompleted` moves `currentLevel` to the next day, or at a chapter's last day to the next
chapter's first unfinished day.

## The map (`Map/MapScreen.cs`, scene `Map`)

Built in code in `Start` → `Build()` on its own canvas (1080 × 1920, match mode Expand); pictures in
`Resources/Map`. One page per chapter (`page`), star positions in `Stars` (12) and `StarsCh2` (16).

- **Opening page:** the chapter of the player's current day, limited to `ChaptersOpen`; chapter 1's page once per run
  while `Chapter2CeremonyDue`.
- **Days:** a played day or today can be tapped (`TapDay`): the truck drives there, then `OpenDay` shows the day card
  (day number in its chapter, stars, Play, Cancel). Today's card opens by itself once the map is up
  (`OpenTodaySoon`), after any pending day 9 moment.
- **Play** (`PlayDay`): `SessionManager.SetCurrentLevel(day)`, music stops, then the between-days ad when
  `day >= FirstAdDay` (2, a running index: no ad before the first two days of the game), then GameSceneOne. The
  `CameFromDay` branch above it is dead: nothing sets `CameFromDay` to true.
- **Day 9** (`SpecialDay` = 8, chapter 1 only): `Day9Moments` runs from `Start` when something is pending:
  `StarGiftCard` when `Day9StarDue` (→ `GiveDay9Star`: one star in `starsGranted`, `day9StarGiven`), then
  `Chapter2Ceremony` when day 9 is done (→ `ClaimChapter2`: `chapter2Claimed`). While either is pending, taps and
  page arrows are refused.
- **Chapter 2's first visit:** `Chapter2Welcome` when `Chapter2WelcomeDue` (→ `chapter2MapSeen`).
- **Bonus days** (`BonusDays` = 3): a chapter's last three days are not drawn until the day before them is reached;
  then `RevealBonus` plays once and `MarkBonusShown(chapter)` saves it (`SessionData.bonusShown`).
- **Arrows:** `ShowPage` rebuilds the canvas for another chapter; the next page is open when `ChaptersOpen` allows.
- **Home:** `GoHome` → MainMenu.
- The truck's last spot per page is static (`truckStoodAt`); `ForgetTruck()` is called by `StartNewSession`.

## Chapter-specific behaviour elsewhere

- Foods: `SessionManager.FoodChapter`, `UnlockChapter`, `FixedFood` (`systems/05`, `systems/03`).
- Patience: four flowers and a third more time in chapter 2 (`PatienceMeter.Flowers`, `CustomerManager`).
- Art swaps through `Chapter.Art`: `shop_bg` (`ShopManager.Start`), `cloth` and `cloth_front` (`TableLayer`), `bg_day`
  (`LevelManager.ApplyVisualSettings`), `bg_night` (`DayNightTint`), the beach, palm and coconuts in the kitchen window
  (`Kitchen/KitchenWindow.cs`).
- Chapter 2's first day: `TutorialDirector.Begin2` (`systems/01`).

## Adding a chapter (nothing for chapter 3 exists in code yet)

A third entry in `DaysPerChapter` and its Level assets appended to `allLevels`; `Resources/Chapter3` art; a third
star layout in `MapScreen` (`StarsOf`) and its background (`Resources/Map/bg_ch<page>`); an opening rule like `Chapter2OpensAfterDays`;
`FoodChapter` / `FixedFood` entries. Keep `Chapter.Count` at 2 until release (`Docs/chapter3-and-secret-plan.md`).
