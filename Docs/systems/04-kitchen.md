# 04 Kitchen

Maintenance notes for the kitchen: the additive `KitchenScene`, its ovens and taps, and the timers that keep running after it closes. Verified against the sources and scene YAML on 7 October 2026. Line numbers are 1-based. The per-item refill numbers (max counts, delays, points) are in `Docs/maintenance.md`, section "Refill system", and are not repeated here.

Files:

- `Assets/Scripts/Kitchen/KitchenSceneManager.cs`, `OpenKitchenButton.cs`, `CloseKitchenButton.cs`, `KitchenFoodGate.cs`, `KitchenButtonGlowController.cs`, `CloseKitchenButtonGlowController.cs`
- `Assets/Scripts/Food/FoodInKitchen/*.cs` (`KitchenItemWithTimer.cs` also holds `CooldownRegistry` and `KitchenTimerHelper`)
- `Assets/Scripts/Utils/ButtonGlowEffect.cs`
- `Assets/Scenes/KitchenScene.unity`, plus the kitchen objects in `Assets/Scenes/GameSceneOne.unity`

## 1. Opening and closing

### Objects in GameSceneOne

| Object (YAML line) | What it has |
| --- | --- |
| `KitchenSceneManager` (GO 6177, MB 6209) | `kitchenSceneName: KitchenScene`. Also a stale serialized field `cacheCollidersOnStart: 1` (6222) that no longer exists in code. Unity ignores it. |
| `Canvas/KitchenButton` (GO 386), layer 5 UI | `Button` with no persistent calls; `OpenKitchenButton` (508): `clickSound` = `Audio/squish.wav`, `levelManager` wired; `BalatroWobble`. |
| Main `Camera` (673) | depth 0, clear flags Skybox, culling mask everything, `PhysicsRaycaster`. |
| `Canvas` (6070) | Screen Space Camera on the main camera, sorting order 0. Holds the kitchen and pause buttons. |
| `PopupCanvas` (5023) | Screen Space **Overlay**, sorting order 1. Holds `PausePanel` (700 x 1000, partial) and `LevelCompletePanel` (full-stretch). |

### Objects in KitchenScene

| Object (line) | Notes |
| --- | --- |
| `Kitchen Camera` (994; Camera 1011) | GameObject on layer 0. Clear flags Depth only (3), **depth 1**, culling mask 64 (layer 6 only). No AudioListener. |
| `Canvas` (781; Canvas 840) | Layer 6, Screen Space Camera on the Kitchen Camera, sorting layer `KitchenBG`, order 1, `GraphicRaycaster` (800), `CanvasScaler` 1080 x 1920, match 0.5 (817). |
| `Canvas/bg` (1868) | Full-stretch Image, raycast target. It covers the whole screen, so it hides the game and blocks every tap on the game's Canvas, the pause button included. |
| `Canvas/bg/mat/backButton` (1554) | `Button` (1594), Sprite Swap transition, pressed sprite `kit2.png`; `CloseKitchenButton` (1688), `clickSound` = `squish.wav`; `BalatroWobble` (1702). |

There is no EventSystem in KitchenScene. The game scene's `StandaloneInputModule` serves both scenes. Layer 6 is named `Kitchen` in `ProjectSettings/TagManager.asset:14`.

### Open sequence (`KitchenSceneManager.OpenKitchen`, 39-71)

1. `Time.timeScale == 0` returns (41). The pause panel is only partial, so the kitchen button can still be tapped while paused. This guard is what blocks it.
2. `isKitchenOpen` returns (43-47). This is the double-tap guard. The flag is set at 68, in the same call, so a second tap in the same or the next frame is ignored.
3. An unload still in flight returns (49-53): `unloadOperation` was set by the last close.
4. The kitchen scene is already loaded: returns (55-59).
5. `DisableGameInteractions` (101-127): every `Collider2D` and `Collider` in every loaded scene (inactive objects included) that is enabled and not on the Kitchen layer gets disabled and remembered.
6. `SceneManager.LoadScene(..., Additive)` (66). The active scene stays GameSceneOne, so `RestartLevel`'s `GetActiveScene()` still works.

### Close sequence (`CloseKitchen`, 73-95)

1. `!isKitchenOpen` returns (75-79). This is the double-tap guard.
2. `EnableGameInteractions` (132-157) re-enables only the colliders in the remembered lists, skipping destroyed ones. Colliders that were already off stay off. No other script toggles `collider.enabled` (grep), so restoring is safe.
3. `UnloadSceneAsync` runs only if the scene is loaded (88-90).
4. `isKitchenOpen = false`.

There is **no pause guard on close**: the back button always works, even if `timeScale` is 0.

### Wiring

- Both buttons wire themselves in `Start` with `onClick.AddListener` (`OpenKitchenButton.cs:40`, `CloseKitchenButton.cs:32`) and remove the listener in `OnDestroy`.
- The back button also has a dead persistent call: target None, empty method (`KitchenScene.unity:1637-1640`). Unity skips it. It is on the "leave alone" list in `Docs/TODO.md`.
- `OpenKitchenButton.levelManager` is wired but unused.

### Persistence and reset

- `KitchenSceneManager` is a `DontDestroyOnLoad` singleton (25-37). The copy in each later GameSceneOne load destroys itself.
- On every **Single** scene load (`OnSceneLoaded`, 179-189), it resets `isKitchenOpen`, `unloadOperation` and both collider lists. Leaving the game scene by any route (Shop, Restart, Main Menu) therefore leaves the kitchen closed and clean. A Single load also removes the additive kitchen scene.

### Input routing while the kitchen is open

- Kitchen hits sort first: the event system's raycast comparer puts the higher camera depth first (Kitchen Camera 1 against main camera 0), and the full-screen `bg` is always hit.
- Customers spawned while the kitchen is open keep their colliders enabled (they were not in the list), but `bg` blocks them anyway.
- `PopupCanvas` is an Overlay canvas, so it draws and receives taps **above** the kitchen. The level-complete panel (full-stretch) and the pause panel both appear on top of the kitchen.

## 2. KitchenFoodGate

`KitchenFoodGate.Start` calls `ApplyVisibility` (19-37), which does `SetActive(IsFoodItemPurchased(trimmed type))`.

- It **fails open**, so everything stays visible, if there is no `SessionManager` or no active session (26-30).
- An empty string counts as purchased (`SessionManager.cs:190`).

| Kitchen object (gate line) | `associatedFoodType` | Role |
| --- | --- | --- |
| `bg/ovens/spot2/oven1` (346) | Choux | Choux oven |
| `bg/ovens/spot3/oven3` (1358) | Cake | Cake oven |
| `bg/cableft/juice` (454) | Juice | Juice tap |
| `bg/windowleft/apples` (731) | Apple | Apple tap |
| `bg/tableleft/cutboard` (1153) | Choux | Decoration only |
| `bg/tableright/cake` (201) | Cake | Decoration only |
| `bg/ovens/spot1/oven2` | none | Bread oven, always shown (Bread is owned from start) |
| `bg/shelf/coffee` | none | Coffee beans, always shown (Coffee is owned from start) |
| `bg/tablecenter/basket` | none | Decoration, no script |

- The strings match the Shop `itemName` values (Apple, Choux, Cake, Juice) and the defaults `Bread` and `Coffee` (`SessionManager.cs:52`).
- `LevelManager.ApplyServeableItemSettings` (402-438) uses the same purchased list for the table items, so the kitchen object and its table item are always shown or hidden together.
- A gated object deactivated by `KitchenItemWithTimer.Awake` (apple still on cooldown) runs its gate `Start` later, when it is re-activated. This is harmless, because only a purchased apple can have been tapped.

## 3. Timers

There are two base classes. Both record the start in `CooldownRegistry` and run the wait on a persistent `KitchenTimerHelper`, so closing the kitchen does not lose the timer.

| | `KitchenItemWithTimer` (apple) | `OvenKitchenBase` (three ovens) |
| --- | --- | --- |
| Effect on click | Refill **immediately**, then hide until `respawnDelay` passes (63-89) | Swap to the baking sprite; refill **when the bake ends** (75-89, 91-99) |
| Helper name | `"[TimerHelper] " + key` (32) | `"[OvenHelper] " + key` (44) |
| Key | `GetType().Name`, here `AppleInKitchen` (129-132) | `BreadOvenInKitchen`, `ChouxOvenInKitchen`, `CakeOvenInKitchen` (110-113) |
| Click guards | paused (65); raycast object must be this object (66); on cooldown (67) | same three (77-79) |
| On wake with time left | `SetActive(false)`, start or re-attach a timer for the remainder (48-54) | baking sprite, start or re-attach a timer for the remainder (59-64) |
| On wake after expiry | clear the pick time, show (55-60) | clear the pick time, **refill now**, idle sprite (65-72) |
| Completion | `OnRespawn` (106-115): clear the pick time, re-activate if the instance still exists | `OnBakeComplete` (91-99): clear the pick time, `OnRefill`, idle sprite, `bakeCompleteSound` |

### `KitchenTimerHelper` (194-272)

- `GetOrCreate` (227-239) finds a GameObject by exact name with `GameObject.Find`, or creates one under `DontDestroyOnLoad`. There is one helper GameObject per key.
- `StartTimer` (251-258) always replaces the completion callback, but starts a coroutine only if none is running for that key. This is the "re-attach": a reopened kitchen's new oven instance receives the completion of the timer the old instance started, and a bake cannot double-fire.
- `Run` (260-271) waits with `WaitForSecondsRealtime` and invokes the callback once.

### Callbacks on a destroyed instance

If the kitchen was closed and never reopened, the stored callback belongs to the destroyed instance, and it still runs:

- `OnBakeComplete` uses only statics (`CooldownRegistry`, `FindObjectOfType`, `AudioManager`) and serialized fields. `SetSprite` is skipped because the destroyed `Image` compares equal to null. **So a bake completes and refills the table while the kitchen is closed, and the ding plays in the game scene.**
- `OnRespawn` clears the pick time and skips the activation (`this != null`, 110).

### `CooldownRegistry` (156-186)

- A static `Dictionary<string, float>` of pick times, using `Time.realtimeSinceStartup`.
- `IsOnCooldown` compares the time elapsed since the pick against the delay the caller passes in.

### What clears the timers

- Only `SessionManager.StartNewSession` (264-277) clears them: it calls `KitchenTimerHelper.CancelAll()` (270) and `CooldownRegistry.ClearAll()` (271). It runs from New Game (`ClickPlay.cs:135`, `ClickContinue.cs:108`).
- **Nothing else clears them.** Not `RestartLevel`, not `LoadNextLevel`, not `GoToMainMenu`, and not `ContinueSession` (279-292). A bake or apple cooldown therefore carries into the restarted level, the Shop, or a continued game.
- Both clocks are real time, so timers ignore `timeScale` (pause).

### Oven state machine

The ovens have **no text or countdown UI**: KitchenScene contains no Text or TMP component. Each oven's state is shown only by its sprite.

```
IDLE (defaultSprite)
  -- tap, not paused, tap hit the oven itself, not on cooldown -->
BAKING (bakingSprite); pick time recorded; helper timer started
  -- kitchen closed --> (scene gone; timer keeps running on helper)
       -- reopened before end --> BAKING (sprite restored, callback re-attached)
       -- reopened after end, timer already fired --> IDLE (refill already happened while closed)
  -- bakeTime elapses --> OnBakeComplete: refill to full, IDLE sprite, ding
  -- New Game --> registry and helper cleared; next load shows IDLE
```

Taps while BAKING are ignored (79). The apple has the same machine with hide/show in place of the sprite swap: READY → tap → refill, pop for 0.2 s (`GetHideDelay`, `AppleInKitchen.cs:30-33`), then HIDDEN → after 8 s, READY.

## 4. Per-object table (KitchenScene)

Delays and refill amounts agree with the catalogue in `Docs/maintenance.md`. Sprites are the `Image` sprite / `bakingSprite`.

| Object (GO line, script line) | Script | Refills | Amount | Delay | Tap sound | End sound | Sprites |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `ovens/spot1/oven2` (1167, 1244) | `BreadOvenInKitchen` | `Bread` | to full | bake 8 s | none (`interactSound` None) | `Audio/ovensound_EDITED.wav` | idle `ovens/IMG_4464 2.png`, baking `ovens/IMG_4464.png`; child `sticker2` = `sticker6.png` |
| `ovens/spot2/oven1` (250, 328) | `ChouxOvenInKitchen` | `Choux` | to full | bake 10 s | none | `ovensound_EDITED.wav` | idle `IMG_4461 3.png`, baking `IMG_4461 2.png`; child `sticker1` = `sticker5.png` |
| `ovens/spot3/oven3` (1262, 1340) | `CakeOvenInKitchen` | `Cake` | to full | bake 15 s | none | `ovensound_EDITED.wav` | idle `IMG_4462 2.png`, baking `IMG_4462.png`; child `sticker3` = `sticker3.png` |
| `windowleft/apples` (636, 713) | `AppleInKitchen` | `Apple` | to full, instantly | hidden 8 s after a tap | none | none | `IMG_4467.png`; pop 1.3x over 0.12 s + 0.08 s |
| `cableft/juice` (360, 437) | `JuiceInKitchen` | `Juice` | to full, instantly | none, spammable | `pour_EDITED.wav` | n/a | `IMG_4470 1.png`; pop 1.3x |
| `shelf/coffee` (543, 619) | `CoffeeInKitchen` | `BeanContainer` | beans to 6 | none, spammable | `...putting-beans-in-the-grinder..._EDITED.wav` | n/a | `IMG_4475.png`; pop 1.3x |

- All sprite paths are under `Assets/Images/Kitchen/`.
- Each refill target is found with `FindObjectOfType<T>()`, which skips inactive objects. If the table item is missing, the script logs `"ERROR: <X> not found"` through `Debug.Log`, **not** `Debug.LogError`.
- The stickers (`sticker1` at 468, `sticker2` at 883, `sticker3` at 1793) are 100 x 100 (90 x 90 for cake) Images centred on each oven, with Raycast Target on (lines 519, 934, 1844).

**Known issue:** a tap on a sticker reaches the oven through event bubbling, but `OvenKitchenBase.cs:78` rejects it because the raycast object is the sticker, not the oven. The centre of every oven therefore ignores taps. The minimal fix is to untick Raycast Target on the three sticker Images. This is a scene-only change. It is not applied yet: it needs the owner's go-ahead and a device check.

## 5. Coffee beans: kitchen vs table

- **Kitchen `CoffeeInKitchen`** (`CoffeeInKitchen.cs:23-44`) only refills beans. Each tap sets `BeanContainer.currentBeanLevel` to 6 (`BeanContainer.cs:23-29`). It does not touch the Coffee cup count, has no cooldown and no gate, and checks only pause.
- **Table `CoffeeMachine`** (`Food/Coffee/CoffeeMachine.cs`) brews. A tap needs at least 1 bean and space in the Coffee `RefillableItem` (92-94), consumes 1 bean (102), and the animation events add 1 cup, or 2 at upgrade level 2 (147).
- The `BeanContainer` is a child of the machine (38). It shows 6 sprites, and none at 0 beans.
- The loop is: serve Coffee until empty → tap the machine (uses a bean) → beans run out → kitchen `coffee` → back to the table.
- `BeanContainer` starts at 6 every time the scene loads.

## 6. Glow controllers (all unused)

I searched for each script GUID across every `.unity` and `.prefab`, and grepped the code for `AddComponent`. None of these three is used:

| Script | GUID | Intended trigger |
| --- | --- | --- |
| `ButtonGlowEffect` | `052b5a7e3cda45ac98eaf8f88912d21d` | The shine sweep itself. `EnableGlow(n)` runs n sweeps 2.5 s apart. It uses scaled time, so it freezes while paused. `OnDisable` resets it (52-57). |
| `KitchenButtonGlowController` | `7ea2671a69ee46cd88c0ec856135dc75` | On the game's KitchenButton: glow 3 times when any `RefillableItem` reaches 0 (55-62), or when a food is bought (38-42). |
| `CloseKitchenButtonGlowController` | `2be82575d301415bb15616fa31c0b5f9` | On the kitchen back button: glow 3 times when `CustomerManager.OnCustomerSpawned` fires (35-39), "nudging the player to go back". |

If anyone wires them up:

- Each controller needs a `ButtonGlowEffect` on the same object, with a `shineImage` child.
- The "food purchased" trigger can never fire in practice. Purchases happen in the Shop scene, and the kitchen button's `Start` subscribes only after GameSceneOne has loaded.
- The close-button controller finds `CustomerManager` across scenes. This works because the kitchen loads additively.

## 7. Walkthroughs

### Bread from zero to baked to refilled

1. The player serves Bread 9 times, and the count reaches 0. A tap on the empty bread now shakes it. Nothing glows (the glow scripts are unused).
2. The player taps the KitchenButton. The squish sound plays, `OpenKitchen` disables the game colliders, and KitchenScene loads.
3. `BreadOvenInKitchen.Awake`: the helper `[OvenHelper] BreadOvenInKitchen` is created or found, no pick time is recorded, and the oven shows IDLE.
4. The player taps the oven, away from the sticker. There is no sound. The baking sprite shows, the pick time is recorded, and the 8 s timer starts.
5. After 8 s, `OnBakeComplete` runs: `Bread.RefillToFull`, then `RefillableItem.RefillToFull` (`RefillableItem.cs:526-534`), which sets the count to 9, clears out-of-stock and fires `OnCountChanged`, so the bread sprite updates. The oven goes back to IDLE and the oven sound plays.
6. The player taps back. `CloseKitchen` re-enables the colliders and unloads the kitchen.

The order and customer timers ran the whole time. `OrderSystem.Update` uses `Time.deltaTime` (`OrderSystem.cs:229-236`).

### Variants

- **Kitchen closed mid-bake.** The timer keeps running on the helper. At 8 s the old instance's callback refills Bread and the ding plays in the game scene. If the player reopens the kitchen before then, the oven shows BAKING, and a tap does nothing until the bake ends.
- **Paused mid-bake.** The player cannot pause from inside the kitchen, because the pause button sits under the kitchen `bg`. They can close the kitchen and then pause. The bake runs on real time and completes during the pause (refill plus ding). This is on the owner's leave-alone list.
- **In the Shop when the bake completes.** `FindObjectOfType<Bread>()` returns null, a `Debug.Log` ERROR line is written, and **the oven ding still plays in the Shop**. The refill is lost, which has no impact because each level starts full.
- **Restart mid-bake** (the pause panel's Restart, or the level-complete panel's). The Single load removes the kitchen. The timer survives, and the bake completes in the restarted level: a no-op refill of the full Bread, plus a ding during arrangement. If the player reopens the kitchen before it ends, the oven still shows BAKING.
- **Return to the main menu mid-bake.** The timer survives. **Continue** within the bake time: the bake completes in the new GameSceneOne, as for Restart. **New Game**: `StartNewSession` cancels it.
- **Level ends while the kitchen is open.** The last order expires (`OrderSystem.cs:516-521`, or 466-471 for a serve), and `OnLevelComplete` runs: the win sound plays, customers and orders are disabled, and 3 s later the full-screen `LevelCompletePanel` appears on the Overlay `PopupCanvas` above the kitchen. Next Level, Restart and Main Menu all do a Single load, which removes the kitchen and resets the manager. Nothing gets stuck.
- **Kitchen opened during arrangement.** It works. Dragging is impossible while it is open, because the colliders are off. Ovens bake, but the refill lands on an already-full item. An apple tap starts its 8 s cooldown for nothing.
- **Kitchen opened after level complete.** This is only possible in the 3 s before the popup, which then covers it.

## 8. Gotchas

- Never rename the kitchen subclasses. The class name is the cooldown key and part of the helper GameObject's name. A rename orphans any running timer until the next New Game. A second instance of the same oven class would also share one key; override `GetCooldownKey` for that.
- `GameObject.Find` in `GetOrCreate` only finds active objects. Never deactivate a helper GameObject.
- Changing `bakeTime` or `respawnDelay` changes how remaining time is computed on wake for timers already running (`remaining = delay - elapsed`), but not the running coroutine.
- Keep the kitchen camera depth above the main camera's, and keep `bg` full-screen and a raycast target. Together they are what blocks taps from reaching the game while the kitchen is open; the disabled colliders cover only the 3D and 2D physics objects, not the game's UI buttons.
- Any new Image child on an oven or apple must have Raycast Target off. Otherwise the `pointerCurrentRaycast.gameObject != gameObject` guard swallows taps on it.
- `FindObjectsOfType` in `DisableGameInteractions` covers every loaded scene, `DontDestroyOnLoad` included. A future persistent object with a collider would be disabled and restored too.
- `PopupCanvas` is an Overlay canvas, so anything put on it appears above the kitchen. Anything put on the game's camera `Canvas` is hidden and blocked by the kitchen.
- All kitchen scripts have `enableDebugLogs` on. The owner has said to leave the debug defaults alone.
