# 04 Kitchen

Last verified against the code: 26 November 2026. Paths are under `Assets/Scripts`. Times are quoted from the code
constants; the Mechanisms page is the player-facing list.
(The October 2026 note is in `Docs/history/systems_2026-10/`; it describes one oven per food, real-time timers that
ran through pause, and none of the flow, arranging, blender or clock below.)

## Opening and closing

`Kitchen/KitchenSceneManager.cs` (persistent, lives in GameSceneOne): `OpenKitchen` loads `KitchenScene` additively
and disables every enabled collider outside the `Kitchen` layer; `CloseKitchen` re-enables those and unloads the
scene. Open is refused while paused (`Time.timeScale == 0`), while already open and while the last unload is still
running. A single-mode scene load resets its state.
Callers: `Utils/PullHandle.cs` (the handle on the screen edge), `Kitchen/KitchenSwipe.cs` (swipe right opens, left
closes, with the slide; play phase only), `OpenKitchenButton` / `CloseKitchenButton`, `GamePhaseManager` (the two
Ready buttons) and `TutorialDirector`.

Because the scene is unloaded on every close, **anything that must survive a close lives outside it**: statics,
`KitchenTimerHelper` objects (DontDestroyOnLoad) or the `SessionManager` object.

## Kitchen clock and the day's reset (`Kitchen/KitchenClock.cs`)

- `KitchenClock.Now` is real time minus the time spent paused; `Delta` is the unscaled frame time, 0 while paused;
  `Wait(seconds)` waits in kitchen time. `LevelManager.PauseGame/ResumeGame` call `Pause()`/`Resume()` (so does
  `CandyGift` around its card). Every kitchen timer and animation uses this clock, so ovens keep baking while the
  kitchen is closed and stop while the game is paused.
- `KitchenDay.End()` stops and clears everything: `KitchenTimerHelper.CancelAll()`, `CooldownRegistry.ClearAll()`,
  `OvenKitchenBase.Recipes.Clear()`, apples and coconuts back to full, `KitchenFlow.Reset()`.
  Called from `LevelManager`: `OnLevelComplete`, `ShowLevelCompletePopup` (3 s later), `RestartLevel`,
  `LoadNextLevel` (day-end Next), `GoToMap` (pause Home), `GoToMainMenu`, `OnAllLevelsComplete`; and from
  `TutorialDirector` at Let's go and Replay. `KitchenArrange.BeginDay()` also calls `KitchenFlow.Reset()`.
  **New kitchen state must be cleared in `KitchenDay.End()`.**
- `SessionManager.StartNewSession` separately cancels timers, clears cooldowns and refills the coconuts.
- `Kitchen/KitchenSpeed.cs`: the star-shop item `StarKitchen` (15 stars). `Factor` is 0.75 once owned, else 1; ovens,
  fruit regrowth, `KitchenItemWithTimer` waits, the flow's mix and cut steps and the blender multiply by it.
  The table's waffle machine (`IceCreamStation.WaffleSeconds`) does not.

## Timers

`Food/FoodInKitchen/KitchenItemWithTimer.cs` holds three things:
- `CooldownRegistry` (static): pick time per key, on the kitchen clock.
- `KitchenTimerHelper` (persistent object per key, named `[OvenHelper] <key>` or `[TimerHelper] <key>`): one
  coroutine per key; a reloaded kitchen object re-attaches its callback to a running timer.
- `KitchenItemWithTimer` (base class for a thing that is tapped, hides and comes back after `respawnDelay`).
Callbacks can run on a destroyed kitchen object (the kitchen closed mid-wait); `OvenKitchenBase` caches its key and
guards with `this == null` for that reason.

## Ovens (`Food/FoodInKitchen/OvenKitchenBase.cs`, `OvenAssigner.cs`)

- Any oven bakes any recipe. A tap on an idle, owned oven opens `OvenPicker` with `OvenAssigner.RecipesOnTheMenu()`
  (the oven foods Bread, Choux, Cake, Cookies that are active today); with one recipe the bake starts at once.
- `BakeTimes`: Bread 5 s, Choux 3, Cake 3, Cookies 3 (× `KitchenSpeed.Factor`). State: the pick time in
  `CooldownRegistry` and the recipe in the static `Recipes`, both under the key `"Oven:<parent name>/<object name>"`.
- Owned ovens: `SessionManager.OvensOwned` (the `Oven` upgrade level, 1 at the start; the shop's `OvenUpgrade` card
  sells the second and third). `OvenAssigner` orders the ovens by spot and locks the rest.
- At the end of a bake: Bread and Cookies refill the table (`OnRefill`; Cookies through `Cookies.AddBake`); Choux and
  Cake go to `KitchenFlow.Baked`. A recipe handled by the flow cannot be started while its plate is busy or another
  oven bakes it.
- The three subclasses (`BreadOvenInKitchen`, `ChouxOvenInKitchen`, `CakeOvenInKitchen`) are empty; the scene points
  at them.

## The flow for choux and cake (`Kitchen/KitchenFlow.cs`)

Stages per dessert: `Empty → Baked → Creamed → Done`. The mixer step (`MixSeconds` 2.5) and the board step
(`CutSeconds` 2) each take one dessert at a time, the longest-waiting first. Steps start by themselves
(`AutoWaitSeconds` 0); `TapMixer` / `TapBoard` start a waiting one at once. After `Done` the picture shows
(`ShowSeconds` 0.9), leaves by `DoneStyle` (1 = sparkles) and `Deliver` refills the table item and earns the journal
page. Coroutines run on the `SessionManager` object; a `generation` counter drops running steps on `Reset()`.

## Other kitchen things

| Thing | Script | What a tap does |
| --- | --- | --- |
| Apples on the tree | `Food/FoodInKitchen/AppleInKitchen.cs` | Takes one of 4 (`Full`) to the basket; all four grow back `respawnDelay` s after the last one |
| Coconuts on the palm (chapter 2) | `CoconutInKitchen.cs`, built by `Kitchen/KitchenWindow.cs` | Takes one of 4 to the tray when it has room; regrow like the apples |
| Juice | `JuiceInKitchen.cs` | Refills the table's juice |
| Coffee bags | `CoffeeInKitchen.cs` | Refills the bean container |
| Blender | `Kitchen/KitchenBlender.cs` | Refills every ice cream well (`IceCreamStation.RefillAll`); `StepSeconds` 0.6 per flavour |
| Kitchen waffle machine | `Kitchen/KitchenWaffleMachine.cs` | Not built: `OnTheTable = true` (the machine is on the table, `IceCreamStation`) |

`Kitchen/KitchenFoodGate.cs` shows a kitchen object only when its `associatedFoodType` is active
(`SessionManager.IsFoodItemActive`). `Kitchen/KitchenGlow.cs` and `Utils/GlowFx.cs` draw the tap-me glows.
`OvenKitchenBase.Awake` is where the per-load builders run: `OvenAssigner.EnsureAssigned`, `KitchenWindow.Apply`,
`KitchenBlender.Apply`, `KitchenWaffleMachine.Apply`, `KitchenArrange.Apply`, `KitchenGlow.Apply`.

## Arranging the kitchen (`Kitchen/KitchenArrange.cs`)

Nine movable pieces (`Blender`, `cutboard`, `whisk`, `basket`, `cake`, `coffee`, `chouxplate`, `mixer`, `juice`); the
ovens stay. Pieces can be dragged only while the table is being arranged (`Arranging()`: arrangement phase, not day 1,
no tutorial). A place is the piece's foot as a share of the kitchen picture. The day's arrangement is kept in the
static `working` and written to the save at Ready (`Commit` → `SessionData.kitchenPlaces`, `kitchenLayout` =
`LayoutVersion`). A piece that overlaps or does not fit turns red; `AnyRed` keeps the day from starting.
