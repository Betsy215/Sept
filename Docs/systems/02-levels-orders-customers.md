# 02 Levels, orders and customers

Verified against the code, `GameSceneOne.unity`, the four customer prefabs, `CustomerAnimatorController` and the clips on 7 October 2026. Line numbers are for that date. Nothing here was run on a device; behaviour comes from reading the code and the YAML. Abbreviations: LM = `Assets/Scripts/LevelControl/LevelManager.cs`, GPM = `Assets/Scripts/Utils/GamePhaseManager.cs`, OS = `Assets/Scripts/ItemsAndOrders/OrderSystem.cs`, CM = `Assets/Scripts/Customer/CustomerManager.cs`, CC = `Assets/Scripts/Customer/CustomerController.cs`, SM = `Assets/Scripts/Scoring/ScoreManager.cs`, SES = `Assets/Scripts/Session/SessionManager.cs`.

## 1. LevelManager

### Fields and their GameSceneOne values (scene lines 3247 to 3330)

| Field | Scene value | Used? |
| --- | --- | --- |
| `allLevels` | Level1Data to Level14Data, in order (GUIDs checked against the `.meta` files) | yes |
| `orderSystem`, `scoreManager`, `customerManager`, `gamePhaseManager` | the scene singletons | yes |
| `serveableItems` | Apple, Choux, Juice, Bread, Cake, Coffee (six; Melon is not in the list) | yes |
| `backgroundRenderer` | None | `ApplyVisualSettings` (LM:477) does nothing; every `LevelData.backgroundSprite` is None too |
| `mainCamera` | Main Camera | never read |
| `levelInfoText` | Canvas/Board/LevelInfoText, shows `levelName` ("Day N") | yes |
| `mainMenuSceneName` | `MainMenu` | yes |
| `popupCanvas` | PopupCanvas (overlay, sort order 1) | yes; also copied into every `ServeableItem` |
| `levelCompletePanel` | PopupCanvas/LevelCompletePanel (inactive) | yes |
| `totalEarned`, `todaySale`, `todayTip`, `unlockedItemsText` | texts inside LevelCompletePanel | yes (see Gotchas: ScoreManager also writes two of them) |
| `nextLevelButton`, `levelCompleteRestartButton`, `levelCompleteMainMenuButton` | inside LevelCompletePanel, all three inactive in the scene, no Inspector `onClick` | listeners added in code |
| `pausePanel`, `resumeButton` (CloseButton), `pauseRestartButton`, `pauseMainMenuButton` | PopupCanvas/PausePanel (inactive), 700 x 1000 centred | listeners added in code |
| `scoreTransferDuration` 2, `scoreTransferCurve` ease-in-out | | never read; the count-up uses its own duration and curve |
| `audioManagerPrefab` | AudioManager prefab | only when GameSceneOne is started without MainMenu |
| `closeButton` | None | never read |
| `debugMode` 0, `debugStartLevel` 13 | | see Debug below |

### Start (LM:60)

1. `EnsureAudioManagerExists` (LM:106) creates an AudioManager from the prefab if MainMenu did not.
2. `debugMode` branch (LM:66): see Debug.
3. Registers with `SessionManager`, then `LoadLevel(session.currentLevel)` if the session is active, else `LoadLevel(0)` (LM:82 to 92). No SessionManager: logs an error and loads nothing.
4. `SetupLevelCompleteUI` (LM:153): hides PopupCanvas, `RemoveAllListeners` then adds `LoadNextLevel` / `RestartLevel` / `GoToMainMenu` to the three popup buttons.
5. `SetupPauseUI` (LM:191): Resume, Restart, Main Menu on the pause panel; hides it.
6. `SetupSessionEvents` (LM:135): subscribes `OnSessionCompleted` (only logs).

### LoadLevel, ApplyLevelSettings, StartLevel

- `LoadLevel(i)` (LM:352): hides the popup; for a valid index sets `currentLevelIndex`, `currentLevelData`, initialises the star bar, then `ApplyLevelSettings` and `StartLevel`. An index past the end calls `OnAllLevelsComplete` (LM:674) and leaves `currentLevelIndex` at its default 0.
- `ApplyLevelSettings` (LM:376): copies `ordersPerLevel`, `orderDisplayTime`, `timeBetweenOrders`, `minOrderItems`, `maxOrderItems` into OrderSystem; `ApplyServeableItemSettings`; `customerManager.OnLevelLoaded(index)` (which captures the level minimum, LM:394 / CM:157); background; `levelInfoText`.
- `ApplyServeableItemSettings` (LM:402): with an active session, each of the six items is `SetActive(purchasedFoodItems.Contains(foodType))`. Without one, all six are activated and every `IUpgradeable` set to level 1.
- Upgrade application `ApplyUpgradeLevels` (LM:440): every `IUpgradeable` MonoBehaviour in the scene gets a level from `GetUpgradeLevelForObject` (LM:456): a `ServeableItem` by its food type, a `CoffeeMachine` by the key `CoffeeMachine`, anything else by its GameObject name. `FindObjectsOfType` only sees active objects, so unpurchased items are skipped.
- `StartLevel` (LM:490): `SnapshotScoreBeforeLevel` (money at level start, kept in memory only, SES:153), `gamePhaseManager.StartArrangementPhase`, `scoreManager.ResetScore`, arrangement music.

### Arrangement to playing

The player arranges items and taps Done. `GPM.OnDoneButtonClicked` (GPM:243) saves item positions to the session, then `StartPlayPhase` (GPM:141) turns off dragging and calls `EnableGameplaySystems` (GPM:236): `orderSystem.enabled = true` (its `OnEnable` resets `ordersCompleted` and rebuilds the food list), `customerManager.enabled = true`, then `LM.StartGamePlay` (LM:510): gameplay playlist, `orderSystem.InitializeForCustomerFlow` (a no-op after `OnEnable`), `customerManager.SpawnCustomerForCurrentLevel`. The `else` branch (`StartOrderCycle`, the original timed flow) is dead because `customerManager` is always assigned.

### Pause, resume, restart, next, main menu

- `TogglePause` (LM:233) is called by `PauseButton` (`Assets/Scripts/Buttons/PauseButton.cs:156`, realtime 0.1 s after release).
- `PauseGame` (LM:241): ignored while LevelCompletePanel is active. Otherwise `timeScale = 0`, PopupCanvas and PausePanel on, music stopped. The pause panel covers the Done button, so Done cannot be pressed while paused.
- `ResumeGame` (LM:269): `timeScale = 1`, hides the panel, hides PopupCanvas unless the level-complete panel is up, restarts gameplay or arrangement music according to the phase.
- `RestartLevel` (LM:301): `timeScale = 1`, hides popups, with an active session `RestoreScoreToSnapshot` (money back to level start, saved) and `SetCurrentLevel(currentLevelIndex)`, then reloads the active scene.
- `LoadNextLevel` (LM:657): `timeScale = 1`, stops music, `InterstitialAdService.ShowAfterLevelThen`, then Shop. The Shop's Next Level loads GameSceneOne, and `Start` loads `session.currentLevel`.
- `GoToMainMenu` (LM:684): stops music, `timeScale = 1`, loads MainMenu. It does not touch the session. Money earned so far in the level stays, and the level index is whatever was last saved.

### OnLevelComplete (LM:531) step by step

Called synchronously from inside `OS.CompleteOrder` or `OS.ExpireOrder` once `ordersCompleted >= ordersPerLevel`.

1. `PlayLevelWin` (the `levelWinSFX` clip is None in the prefab and the MainMenu override, so this is silent).
2. `scoreManager.OnLevelEnd` clears the score popup. The last order's popup is started 0.2 s later by `DelayedCombinedPopup`, so it still shows.
3. `SessionManager.OnLevelCompleted(index)` (SES:360): `currentLevel = index + 1`, `levelsCompleted = index + 1`, **saved now**. From this moment Continue on the main menu goes to the next day.
4. `customerManager.enabled = false`, `orderSystem.enabled = false`. The current customer keeps walking out (CM:62).
5. `StartCoroutine(ShowLevelCompletePopup())`.

### ShowLevelCompletePopup (LM:557)

1. `WaitForSeconds(3)` (scaled time, so it is frozen while paused). The pause button still works during these 3 s, because the panel is not up yet.
2. `PlayLevelCompleteMusic` (clip None by the owner's choice: the playlist coroutine stops but the current track plays on to its end).
3. PopupCanvas and LevelCompletePanel on.
4. Maths: `tips = totalTipsEarned`, `todayScore = currentScore` (item points of every order including expired ones, plus tips), `orderSale = todayScore - tips`, `totalScore = session total` (already includes everything from this level), `earnedBeforeLevel = totalScore - todayScore`, `earnedAfterSale = totalScore - tips`.
5. Texts: "Tips Earned: $ t", "Today Sale: $ s", "Earned: $ before", formatted F2.
6. `SetupLevelCompleteButtons` (LM:648): not the last level, so Next Level and Restart are shown. On the last level only Main Menu is shown. It only ever activates.
7. `UpdateUnlockedItemsDisplay` (LM:630): "Unlocked Food: n / Unlocked Customers: m". These are the *remaining* counts (total minus purchased), despite the label.
8. 1 s, then `AnimateFullTransfer(todaySale -> totalEarned, orderSale, earnedAfterSale)`, 1 s, then `AnimateFullTransfer(todayTip -> totalEarned, tips, totalScore)`.
9. On the last level: 0.5 s, then `ShowFinalCompletionMessage`.

`AnimateFullTransfer` (LM:730): duration = `amount / 100`, clamped to 0.5 to 10 s. The source counts from `amount` down to 0, and the target counts from `final - amount` up to `final` on an ease-in-out curve, with unscaled-by-pause `Time.deltaTime` (pause is blocked while the panel is up). The target is red while counting. A money-count tick plays every second and the complete sound plays at the end. It writes with the prefixes "Order Sale: $ " / "Tips: $ " / "Earned: $ " and format **F0** (whole dollars), unlike the F2 used in step 5. Each amount is added to Earned exactly once: before → before + sale → before + sale + tips = total.

Example: total before the level 200.00, sale 120, tips 18.50 (session total 338.50). Earned shows "200.00" (F2), then counts "200" to "320" over 1.2 s while Order Sale counts 120 to 0, then "320" to "338" or "339" (F0 of 338.5) over 0.5 s while Tips counts "18" or "19" to 0. The cents never show again.

### Last level (Day 14, index 13)

`OnLevelComplete` saves `currentLevel = 14` with the session still active. Only after 3 s + 1 s + sale transfer + 1 s + tips transfer + 0.5 s (about 7 to 12 s) does `ShowFinalCompletionMessage` (LM:604) run. It checks and saves the high score, writes "Congrats! You finished all levels!" and "New Record! $x" or "Final Score: $x", and calls `CompleteSession` (session inactive, saved). After that, Continue on the main menu is greyed out (`ClickContinue.UpdateButtonState`, `Assets/Scripts/Buttons/ClickContinue.cs:29`), and Play starts a new session with no warning (`ClickPlay.HasSavedProgress` needs an active session). The Main Menu button is visible from the moment the popup appears, so it can be tapped before `CompleteSession` runs. See Gotcha 1.

`OnAllLevelsComplete` (LM:674) runs only when `LoadLevel` gets an index of 14 or more, which only happens in that interrupted case.

### Debug

`debugMode` (off in the scene) jumps to `debugStartLevel` (13 = Day 14) and **returns before** `SetupLevelCompleteUI`, `SetupPauseUI` and `SetupSessionEvents` (LM:66 to 73). In debug mode the popup and pause buttons have no listeners, so they do nothing, and without a SessionManager `StartLevel` throws at LM:492. The owner has said to leave the debug defaults alone.

## 2. GamePhaseManager

- Phases: `ARRANGEMENT`, `PLAYING` (GPM:8).
- Scene values: `levelTutorialMessages` is overridden to two strings: "Drag to arrange your table" and "The faster you serve, the more tips you earn!". The code default (GPM:31) has a third, "Hold on item to refill", which never shows. The index is `(day - 1) % 2`, with the day taken from `SessionManager.currentLevel + 1` (GPM:124), so odd days show the drag hint and even days the tips hint. The message goes through `string.Format(msg, day)`, so a `{` in a future message would throw.
- `Start` (GPM:48) caches `levelManager.serveableItems`, finds RefillSystem, adds the Done listener and calls `StartArrangementPhase`. `LevelManager.StartLevel` calls it as well, so it runs twice per load (known, harmless).
- `StartArrangementPhase` (GPM:72): rebuilds the draggable cache, shows the tutorial panel and message, restores saved positions clamped to the table (GPM:261), enables dragging on live items, disables OrderSystem and CustomerManager, shows ArrangementUI, updates the Done button after 0.2 s, and tells RefillSystem.
- Done flow: see section 1. Done is disabled while any live draggable overlaps another (`UpdateDoneButtonState`, GPM:220, re-run from `OnItemOverlapChanged`). There is a double-tap guard (GPM:246).
- Draggable cache (GPM:161): all six `serveableItems` (including inactive ones, which `IsLive` filters out) plus every active `CoffeeMachine`. It is rebuilt only in `StartArrangementPhase`.

## 3. OrderSystem

Scene values (scene line 6386): `ordersPerLevel` 1, `orderDisplayTime` 5, `timeBetweenOrders` 5, min 1, max 4 (all overwritten by LevelData), `systemInitializationDelay` 0, `perfectTimeFraction` **not serialized, so the code default 0.5 applies**, `orderProgressText` / `orderTitleText` / `orderTimerText` None (no progress or timer text anywhere; the timer is invisible), `itemSpacing` 1, single-column start (-0.55, 1.3), two-column start (-1.15, 1.3), `audioSource` and `itemServedSound` None, `speechBubble1` Bubble1, `speechBubble2` Bubble2, `speechBubble3` None, `speechBubble4` Bubble4.

- **Available foods** (`availableFoods`): Choux, Juice, Bread, Apple, Cake, Coffee, each with a display prefab. `UpdateActiveFoodTypes` (OS:194) keeps those whose table item is active (that is, purchased). If none match, it falls back to all six.
- **Generation** `GenerateNewOrder` (OS:245): size `Random.Range(clamp(min,1,max), max+1)`. Each slot is a random active food, and duplicates are allowed. Display objects are instantiated under OrderContainer: one or two items in a column, three or four in a 2 x 2 grid (OS:303).
- **Bubbles** (OS:164): 1 item uses Bubble1, 2 items Bubble2, 3 or 4 items Bubble4. Bubble3 is unused.
- **Timer**: `orderTimer = orderDisplayTime` (OS:340), counted down with `Time.deltaTime` in `Update` (OS:229), so it is frozen while paused and while the component is disabled. It keeps running while the kitchen is open.
- **TryServeItem** (OS:345): with no active order it returns false. It marks the first unserved slot of that type, starts the pop and fade effect (0.2 s pop + 0.3 s fade, `ServedItemVisual`, OS:690), `AwardItemPoints`, plays the item sound, and checks completion. A wrong item returns false; `ServeableItem` then shakes and plays the wrong-item sound.
- **CompleteOrder** (OS:427): `AwardOrderCompletionBonus(orderTimer, currentOrderItemPoints)`; `isPerfect = orderTimer >= orderDisplayTime * 0.5` (2.5 s left on 5 s days, 2 s on 4 s days); `ordersCompleted++`; `customerManager.HandleOrderServed(isPerfect)`; bubbles off; level end check.
- **ExpireOrder** (OS:484): clears the display, bubbles off, `ApplyOrderExpiredPenalty` (item points already earned are still paid, with no tip and no money taken away), `ordersCompleted++`, `HandleOrderExpired`, level end check.
- **Level end condition**: `ordersCompleted >= ordersPerLevel`, counting served and expired orders alike (OS:466, OS:516). A level therefore always ends after exactly `ordersPerLevel` customers, and the next day unlocks whatever the stars. Expired orders end the level correctly: the last one calls `OnLevelComplete` the same way.

### Money (ScoreManager, scene line 4588)

`pointsPerItem` 0 (fallback for unlisted foods), `timeBonusMultiplier` 10. Points: Coffee 10, Bread 15, Apple 15, Juice 20, Melon 25, Pie 30, Mont Blanc 35, Log Cake 35, Cake 35, Choux 35.

- An item served adds points to `currentScore` and `currentOrderItemPoints` (SM:140). No money moves yet.
- Order complete (SM:152): `tip = round2(itemPoints x secondsLeft x 10 / 100 x tipMultiplier)`, which is **itemPoints x secondsLeft x 0.1 x multiplier** (Grandma 2, everyone else 1). Example: Bread + Coffee (25 points) with 3.2 s left gives a tip of 8.00, and 33.00 goes to the session at once (`AddScoreImmediately`, saved, rounded to cents). `Docs/maintenance.md` describes the tip as "remainingSeconds x 10", which is not what the code does.
- Order expired (SM:191): the items already served are paid, the tip is 0, and "Order Expired!" goes to `feedbackText` (None in the scene, so it is only logged).
- Stars: `LevelData.GetStarsEarned(currentScore)`, shown by the star progress bar only. Stars gate nothing.

## 4. Customers

### CustomerManager (scene line 2348)

`customerPrefabs`: Girl, Boy, Kid, Grandma (`Assets/Prefabs`); `spawnPoint` SpawnPoint at (-4, 4, 0); `nextCustomerSpawnDelay` 2.

Spawn cycle:

1. `SpawnCustomerForCurrentLevel` (CM:115). If a customer is already being processed, it does nothing.
2. `SelectCustomerForLevel` (CM:275) picks at random among prefabs whose name minus "Customer" or "Prefab" (here the bare prefab name) is in `purchasedCharacters`. A new game owns Girl only. With no active session it uses `customerPrefabs[0]` (Girl).
3. `SpawnCustomer` (CM:314) restores the level minimum order size, sets the tip multiplier to 1, instantiates at the spawn point, and plays the walk-in SFX.
4. Animation event `OnReachedServicePoint` leads to `OnCustomerReachedService` (CM:181): `minOrderItems = clamp(max(levelMin, customer.MinOrderItems), 1, levelMax)`, the tip multiplier is set, and `HandleCustomerOrderDelay` (CM:342) runs.
5. After `OrderDelay` (1 s for all four), `orderSystem.StartOrderCycleForCustomer` generates the order and `customer.OnOrderGenerated` is called.
6. Served or expired: `HandleOrderServed` / `HandleOrderExpired` hand off to the customer.
7. `OnCustomerExited` (CM:208) clears the state. `CheckForNextCustomer` (CM:230) does nothing when the manager is disabled (level over). Otherwise, after 2 s, `SpawnCustomerForCurrentLevel`.

`OnDisable` (CM:62) stops the spawn and order-delay coroutines but lets the current customer finish its walk-out. `OnDestroy` destroys the customer.

### CustomerController states (CC)

Flags: `hasReachedServicePoint`, `isWaitingForOrder`, `isWalkingIn` (never set true), `isWalkingOut`, `hasProcessedOrder` (guard against a second serve or expiry).

- Awake (CC:37): finds the Animator and CustomerManager, uses the root SpriteRenderer as the sad sprite and child `HappySprite` as the happy sprite (Girl has both wired; the others rely on the auto-find), and starts sad.
- Served, `OnOrderServed` (CC:67): plays `perfectOrderSound` or `orderDoneSound` (both unset on all four prefabs, so they fall back to AudioManager's `defaultPerfectOrderSound` / `defaultOrderDoneSound`, both set on the MainMenu instance), switches to the happy sprite, `IsHappy = true` + `TriggerReaction`, then `HappyWalkOut`: 0.3 s, `CustomerState = 2`, 2.0 s, `OnReachedExit`. That is 2.3 s from serve to destroy.
- Expired, `OnOrderExpired` (CC:97): sad sprite, `IsHappy = false` + `TriggerReaction`, `DestroyAfterAnimation`: 2.0 s, `OnReachedExit`.
- `OnReachedExit` (CC:217) tells the manager and destroys the object.
- `SadWalkOut` (CC:177) is never called.

### Animator and events

`CustomerAnimatorController` (all four prefabs, culling Always Animate, Normal update, so it is frozen by pause):

| State | Clip | Speed | Length (real time) | Movement (local position) | Events |
| --- | --- | --- | --- | --- | --- |
| **Sad_Toad_Walking** (default) | Sad_Toad_Walking, no loop | 0.5 | 2.017 s clip, 4.03 s real | x -3 to 1 by 1.517 (3.03 s real), then holds | `OnReachedServicePoint` at 2.0167 (the last frame), 4.03 s after spawn |
| Perfect_Order_Toad | loop | 1 | 0.833 s | hops on y 1 to 1.6 | none |
| happy_toad_walking | **loop** | 1 | 2 s | x 1 to 4, then wraps back to 1 | none |
| Sad_Toad_Walk_Out | no loop | 1 | 1.5 s | x 1 to 5 | none |

Transitions: from Sad_Toad_Walking, `TriggerReaction` with `IsHappy` true goes to Perfect_Order_Toad, and with `IsHappy` false goes to Sad_Toad_Walk_Out (no exit time, 0.25 s blend). Perfect_Order_Toad goes to happy_toad_walking at exit time 1. The `CustomerState` int parameter is set by code but used by no transition.

Animation event handlers in CC and where they fire:

| Handler | Fired by |
| --- | --- |
| `OnReachedServicePoint` (CC:246) | Sad_Toad_Walking end. **The only trigger for an order.** If this event is lost, the customer stands forever and the level never ends. |
| `OnWalkInComplete` (CC:255) | nothing |
| `OnWalkOutComplete` (CC:261) | nothing (exit is timer driven) |
| `OnReactionComplete`, `OnAnimationMidpoint`, `OnCustomerReady` (CC:268 to 284) | nothing |

Other clips: `brew` fires `OnFinish1Cup`, `CompleteBrewing`; `brew 1` fires `OnFinish1Cup`, `OnFinish2Cup`, `CompleteBrewing` (coffee machine, see the refill doc). `drag.anim` has one event at 4 s with an **empty function name** (Unity logs "AnimationEvent has no function name specified" each time it fires on whatever uses it). `truck`, `setting` have no events.

The animation curves drive absolute local position, so the spawn point's position is overwritten on the first frame: customers always walk from x -3 to x 1 at y 1. The happy walk-out is cut by the 2.3 s timer about 1.2 s into happy_toad_walking (around x 2.4). On a 16:9 screen (ortho size 5, half-width 2.81) part of the customer may still be visible when it disappears.

### Per-type overrides

| Prefab | Script | OrderDelay | MinOrderItems | TipMultiplier | Notes |
| --- | --- | --- | --- | --- | --- |
| Girl | `GirlCustomer` | 1 | 1 | 1 | owned at start |
| Boy | `BoyCustomer` | 1 | 1 | 1 | shop 50 |
| Kid | `KidCustomer` | 1 | **4**, clamped to the level max (2 on Day 1, 3 on Days 2 and 3) | 1 | shop 500; uses `girl*` field names |
| Grandma | `GrandmaCustomer` | 1 | 1 | **2** | shop 300; code calls it Toad |

`PatienceLevel` (5) and `PreferredFoods` ("Burger", "Fries") are read by nothing. `fallbackWalkInDuration`, `servicePointDelay` and the extra serialized fields on Girl (`walkInAnimationState`, `sadWalkDistance` and others) are leftovers.

### Sounds in this loop

| Moment | Sound |
| --- | --- |
| Spawn | `customerWalkInSFX` (CM:336) |
| Correct item | `itemPickupSFX` **twice** (SM:345 `PlayPointsSound` and OS:559 `PlayItemServedSound`, both fall back to `PlayItemPickup`), plus Juice's own `serveSound` |
| Wrong item | `wrongItemSFX` |
| Order complete | `orderCompleteSFX` at 0.7 volume (SM:352) plus the customer's perfect or done sound. On a perfect order both are the same clip (guid 8f9fcb...) |
| Expired | `penaltySound` (None) |
| Level end | `levelWinSFX` (None), level-complete music (None), money tick and complete during the count-up |

## 5. The 14 LevelData assets (`Assets/Entity`)

All have `timeBetweenOrders` 3 (unused in the customer flow), `backgroundSprite` None, `useSpecificFoodTypes` 0, `difficultyMultiplier` 1 (both unused). `maxPossibleScore` is 0 on Days 1 and 2 and 50 elsewhere (unused).

| Asset | Name | Orders | Timer s | Items | Stars 1 / 2 / 3 |
| --- | --- | --- | --- | --- | --- |
| Level1Data | Day 1 | 7 | 5 | 1 to 2 | 70 / 140 / 210 |
| Level2Data | Day 2 | 7 | 5 | 1 to 3 | 140 / 280 / 420 |
| Level3Data | Day 3 | 8 | 5 | 2 to 3 | 200 / 330 / 470 |
| Level4Data | Day 4 | 8 | 5 | 2 to 4 | 200 / 330 / 470 |
| Level5Data | Day 5 | 9 | 5 | 2 to 4 | 390 / 510 / 640 |
| Level6Data | Day 6 | 10 | 4 | 2 to 4 | 440 / 560 / 690 |
| Level7Data | Day 7 | 10 | 4 | 2 to 4 | 440 / 560 / 690 |
| Level8Data | Day 8 | 10 | 4 | 2 to 4 | 440 / 560 / 690 |
| Level9Data | Day 9 | 11 | 4 | 2 to 4 | 490 / 610 / 730 |
| Level10Data | Day 10 | 11 | 4 | 2 to 4 | 550 / 670 / 790 |
| Level11Data | Day 11 | 11 | 4 | 2 to 4 | 610 / 730 / 860 |
| Level12Data | Day 12 | 12 | 4 | 2 to 4 | 660 / 780 / 900 |
| Level13Data | Day 13 | 12 | 4 | 2 to 4 | 720 / 840 / 960 |
| Level14Data | Day 14 | 13 | 4 | 2 to 4 | 770 / 890 / 1000 |

## 6. One order, start to finish (Day 1, Girl, Bread + Coffee)

| t (s) | What happens | Method chain |
| --- | --- | --- |
| 0 | Done tapped | `GPM.OnDoneButtonClicked` → `StartPlayPhase` → `EnableGameplaySystems` → `OS.OnEnable` (food list = Bread, Coffee) → `LM.StartGamePlay` → `CM.SpawnCustomerForCurrentLevel` → `SelectCustomerForLevel` → `SpawnCustomer` (walk-in SFX) |
| 0 to 3.03 | Girl walks x -3 to 1 | Animator state Sad_Toad_Walking at speed 0.5 |
| 4.03 | Last frame of the walk clip | event → `CC.OnReachedServicePoint` → `CM.OnCustomerReachedService` (min 1, tip x1) → `HandleCustomerOrderDelay` |
| 5.03 | Order appears, timer 5 s | `OS.StartOrderCycleForCustomer` → `GenerateCustomerOrder` → `GenerateNewOrder` → `DisplayOrder` (Bubble2); `CC.OnOrderGenerated` |
| 6.2 | Tap Bread | `ServeableItem.OnPointerClick` (ServeableItem.cs:158) → `OnItemClicked` (184) → `OS.TryServeItem` → `SM.AwardItemPoints` (+15) → effect; `OnItemServedSuccessfully` (210) → `RefillSystem.OnItemServed` (count −1) |
| 6.8 | Tap Coffee, 3.23 s left | `TryServeItem` → `CheckOrderCompletion` → `CompleteOrder` → `SM.AwardOrderCompletionBonus` (tip 25 x 3.23 x 0.1 = 8.08, session +33.08, saved; popup at +0.2 s) → `isPerfect` (3.23 ≥ 2.5) → `CM.HandleOrderServed(true)` → `CC.OnOrderServed` (perfect sound, happy reaction) |
| 6.8 to 9.1 | Hop 0.83 s, then happy walk | Perfect_Order_Toad → happy_toad_walking |
| 9.1 | Customer destroyed | `HappyWalkOut` → `OnReachedExit` → `CM.OnCustomerExited` → `CheckForNextCustomer` |
| 11.1 | Next customer spawns | `DelayedNextCustomerSpawn` → `SpawnCustomerForCurrentLevel` |
| 16.1 | Next order appears | the same as 4.03 to 5.03 |

Dead time between one order ending and the next appearing: 9.3 s after a serve (2.3 + 2 + 4.03 + 1), 9.0 s after an expiry. A five-order Day 1 lasts about 5 x (5 + timer used) + 4 x 9.3, roughly 60 to 75 s. If the order in the table were the last one, `OnLevelComplete` would run at 6.8 s and the popup would appear at 9.8 s.

## 7. Gotchas

1. **Last day is not finished until about 10 s after the popup appears.** `CompleteSession` and the high score save happen at the end of the count-up, not in `OnLevelComplete`. Leaving early (popup Main Menu during the count-up, pause then Main Menu during the 3 s delay, or the app being killed) leaves an active session at index 14. Continue then hits `OnAllLevelsComplete` with `currentLevelIndex` 0. Recommended fix: run the high-score save and `CompleteSession` inside `OnLevelComplete` when it is the last level.
2. Main Menu during the 3 s delay on any other day skips the Shop and the interstitial (progress is already saved as the next day).
3. Quitting mid-level keeps the money already earned (it is added per order and saved), while Restart rolls it back. Quitting and continuing can be repeated.
4. `ScoreManager.finalScoreText` is the popup's TotalEarned and `totalScoreText` is the popup's TodaySale (scene line 4600). `UpdateScoreUI` writes "Level Score: x" into TotalEarned on every order, and `OnTotalScoreChanged` writes "Total Score: x" into TodaySale. Today the popup overwrites both before it shows, but anything that changes the session total while the popup is up (for example a future rewarded ad in this scene) would garble it.
5. Start order between `LevelManager` and `GamePhaseManager` is not fixed (no execution order is set). If `LevelManager.Start` ran first on a day with saved positions, `GPM.LoadSavedFoodPositions` would iterate a null `allFoodItems` (GPM:272) and abort the rest of `LevelManager.Start`, including the button wiring. The shipped build evidently runs GamePhaseManager first; do not add objects or reorder the scene casually.
6. The walk-in clip's `OnReachedServicePoint` event is the single point of failure for every order. Do not change that clip's length, speed or loop flag without moving the event to the new last frame.
7. `CustomerState`, `SadWalkOut`, `OnWalkOutComplete` and the other event handlers look like a state machine but do nothing. Exits are timers (2.3 s happy, 2.0 s sad).
8. `perfectTimeFraction` is not in the scene, so changing the code default changes the game.
9. In the Editor, unassigned `AudioClip` fields are Unity "fake null", so `perfectOrderSound ?? default` (CC:77, CC:82) does not fall back and the customer's order sound is silent in Play mode. On device the fallback works. Do not "fix" this from Editor testing alone.
10. Expired orders count towards the level, and stars gate nothing: a player who serves nothing still advances.
11. Unused tuning knobs: `timeBetweenOrders`, `maxPossibleScore`, `useSpecificFoodTypes`, `difficultyMultiplier`, `backgroundSprite`, `scoreTransferDuration`/`Curve`, `PatienceLevel`, `PreferredFoods`. Editing them changes nothing.
12. The count-up uses F0 and different labels ("Order Sale", "Tips") from the initial F2 texts ("Today Sale", "Tips Earned").
