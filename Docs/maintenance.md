# Maintenance notes

Notes for whoever maintains this project next, including Claude in a future session. Facts verified against the code and scenes on 7 October 2026. The rule above everything else: **the game is live and works; change code only for real bugs.**

## Scene wiring facts you cannot see from the code

- Input in GameSceneOne is a 3D `PhysicsRaycaster` on the camera, and every food item and the coffee machine has a 3D `BoxCollider`. `IPointer*` handlers work because of that pairing. Switching anything to 2D colliders silently stops its input.
- Every `ServeableItem` in the scene has `popupCanvas` unassigned; the script now copies it from `LevelManager.popupCanvas` at start.
- Every `RefillableItem` has `refillBar`, `fill`, `bar` unassigned and no such children exist; the refill bar never shows. `enableHoldToRefill` is on for Melon only. Do not "fix" this without first reading the refill section below and testing on device.
- `AudioManager` clips are mostly scene overrides on the MainMenu instance, not on the prefab. Main Menu Music and Level Complete Music are deliberately None.
- Shop buttons Purchase, Cancel and Next Level have Inspector `onClick` calls. `ShopManager.Wire` therefore skips adding runtime listeners when a persistent call exists. Do not add listeners to those three again.
- `PopupCanvas` in GameSceneOne is Screen Space Overlay, sorting order 1, above the camera-space `Canvas`. The pause panel does not cover the whole screen; the pause button and kitchen button remain tappable while paused, which is handled in code (toggle and guard).
- `DraggableFood` exists in no scene or prefab; it is added at runtime by `ServeableItem.SetDraggingEnabled(true)`.
- `KitchenScene` loads additively over GameSceneOne. It has no EventSystem and no AudioListener on purpose. Its camera is depth 1, culling only layer 6 (Kitchen).
- Level data assets 1 to 14 are wired in order on `LevelManager.allLevels` in GameSceneOne.

## Scoring

Per order: sum of item points, plus a tip of `itemPoints x secondsLeft x 10 / 100 x customerTipMultiplier`, rounded to cents (`ScoreManager.AwardOrderCompletionBonus`; the 10 is `timeBonusMultiplier` in the scene, and the customer multiplier is 2 for Grandma, 1 for everyone else). Example: a 50-point order finished with 3 s left from a normal customer tips 15.00. Item points (ScoreManager in GameSceneOne): Coffee 10, Bread 15, Apple 15, Juice 20, Melon 25, Cake 35, Choux 35. The table also holds Pie 30, Mont Blanc 35 and Log Cake 35, which match no item in the game and look like the old names for Cake and Choux; Cake and Choux had no entry at all until 7 October, so they scored 0. The fallback for an unlisted food is 0 points, so any new food must be added to this table. Expired orders still pay item points but no tip. Stars come from `LevelData` thresholds against the level score. Money goes to `SessionManager` at order completion or expiry; Restart rolls money back to the level-start snapshot.

Level tuning after 7 October (orders, display seconds, items, stars):

| Day | Orders | Timer | Items | Stars |
| --- | --- | --- | --- | --- |
| 1 | 5 | 5 | 1 to 2 | 50 / 100 / 150 |
| 2 | 5 | 5 | 1 to 3 | 100 / 200 / 300 |
| 3 | 6 | 5 | 2 to 3 | 150 / 250 / 350 |
| 4 | 6 | 5 | 2 to 4 | 150 / 250 / 350 |
| 5 | 7 | 5 | 2 to 4 | 300 / 400 / 500 |
| 6 to 8 | 8 | 4 | 2 to 4 | 350 / 450 / 550 |
| 9 | 9 | 4 | 2 to 4 | 400 / 500 / 600 |
| 10 | 9 | 4 | 2 to 4 | 450 / 550 / 650 |
| 11 | 9 | 4 | 2 to 4 | 500 / 600 / 700 |
| 12 | 10 | 4 | 2 to 4 | 550 / 650 / 750 |
| 13 | 10 | 4 | 2 to 4 | 600 / 700 / 800 |
| 14 | 11 | 4 | 2 to 4 | 650 / 750 / 850 |

## Shop

Nine items in Shop.unity, all instances of `ShopItemPrefab` with overrides:

| Item | Type | Price | Note |
| --- | --- | --- | --- |
| Boy | Character | 50 | |
| Juice | Food | 150 | |
| Grandma | Character | 300 | GameObject is named "Granma" |
| Apple | Food | 350 | |
| Coffee upgrade | Upgrade | 350 | `upgradeFoodType` Coffee, one-shot |
| Kid | Character | 500 | |
| Coffee Machine upgrade | Upgrade | 600 | `upgradeFoodType` CoffeeMachine, one-shot |
| Choux | Food | 750 | |
| Cake | Food | 750 | |

Bread and Coffee are owned from the start (`SessionData` constructor). Upgrades are one-shot by design: `UpdatePurchasedItemsUI` marks an upgrade item owned once its level is above 1. The session stores upgrade levels up to 3, but nothing in the shop sells a second step. Purchases pay first, then record; a failed record refunds.

Upgrade consumers: `Coffee` (cup sprite set per level), `CoffeeMachine` (brews one or two cups), `Cake` (IUpgradeable, check the script before relying on it). Food and character identifiers are plain strings matched across `SessionData`, `ShopItemController.itemName`, `ServeableItem.foodType`, `KitchenFoodGate.associatedFoodType`, and the `ScoreManager` points table. A rename must hit every one.

## Refill system (read before touching)

Each table food has a `RefillableItem` with a `currentCount` and `maxCount`. `RefillSystem` registers all items at start (and rescans after 0.5 s), tells them when gameplay begins, and relays serves. A correct serve decrements the count; at zero the item is out of stock and a tap on it shakes instead of serving.

Two refill paths exist:

1. **Hold-to-refill** on the table: press for 0.3 s and keep holding; a count is added every `refillTimePerCount` seconds. Only enabled on Melon in the scene. A hold that reaches the maximum stops refilling but still counts as a hold, so lifting the finger does not serve.
2. **Kitchen**: ovens and machines in KitchenScene (`OvenKitchenBase`, `KitchenItemWithTimer` subclasses) run a bake or respawn timer on a persistent `KitchenTimerHelper` keyed by the item, so closing the kitchen does not lose the timer. Completion calls the matching table item's refill. `CooldownRegistry` is static and keyed the same way. A new game clears both. The coffee machine consumes beans from `BeanContainer` and brews one or two cups depending on its upgrade level.

### Per-item catalogue (verified 7 October 2026)

Effective values: `customMaxCount` 0 means the `RefillSystem` default of 5; `customRefillTime` 0 or -1 means 1 second; `customStartingCount` -1 means start full.

| Item | Unlock | Max | Hold? | Refill source | Delay | Sprites vs max | Points |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Bread | owned from start | 9 | no | kitchen `oven2` bake, refills to full | 8 s | 4 sprites, 1 per 3 counts, exact | 15 |
| Coffee | owned from start | 2, or 3 once upgraded | no | table coffee machine, 1 bean per brew, 1 cup (2 when upgraded) | animation | 3 sprites, 4 when upgraded, exact | 10 |
| Juice | shop $150 | 4 | no | kitchen `juice` tap, refills to full | instant, no cooldown | 5 sprites, 1 per count, exact | 20 |
| Apple | shop $350 | 8 | no | kitchen `apples` tap, refills to full | instant, 8 s before re-tap | 5 sprites, 1 per 2 counts | 15 |
| Choux | shop $750 | 4 | no | kitchen `oven1` bake, refills to full | 10 s | 5 sprites, exact | 35 (was 0, fixed) |
| Cake | shop $750 | 6 | no | kitchen `oven3` bake, refills to full | 15 s | 7 sprites, exact | 35 (was 0, fixed) |
| Melon | not in the shop | 3 | **yes** | hold only, no kitchen object | 1.5 s per count | no sprite script | 25 |

Coffee beans come from the kitchen `coffee` object: tapping refills beans to 6 instantly with no cooldown. `BeanContainer` resets each level.

**No playable item can run out permanently.** Every one has an always-available source, and each level starts its items full.

**Melon is a dead object.** It is inactive in the scene, absent from `LevelManager.serveableItems`, absent from `OrderSystem.availableFoods`, and not sold in the shop, yet it is the only item with `enableHoldToRefill` on. So hold-to-refill ships in the game but no player can reach it. The code default tutorial message "Hold on item to refill" is overridden in the scene by two messages that never mention holding, so nothing tells players about it either.

**Recommendation: do not enable hold-to-refill more widely.** It is mechanically safe (counts clamp, a pending bake cannot double-fill) but it would be redundant with the kitchen on Bread, Apple, Juice, Choux and Cake, and on Coffee it would bypass the bean and machine loop entirely. Enabling it would also need: a `RefillBar` child with `Fill` and `Bar` sprites on each item (none exist, so holds give no visual feedback), a deliberate `customRefillTime` per item (the 1 second default outpaces an 8 to 15 second bake), a tutorial message restored in the scene, and a decision on whether Melon becomes a real item or is deleted.

**Kitchen timers run on real time** (`CooldownRegistry` uses `realtimeSinceStartup`), so bakes continue while the game is paused and while the player is in the Shop. A bake that completes in the Shop looks for its table item, finds nothing, and writes a normal log line containing the word ERROR (not a real error); the refill is lost, and the oven ding plays in the Shop. This has no player impact because every level starts its items full.

## Ads

- `AdsInitializer` (MainMenu) requests tracking permission, initialises Unity Ads with game ID 6074412, and preloads the interstitial.
- `RewardedAdButton` (Shop) loads `Rewarded_iOS` with retry, shows on tap, credits 5 coins only when the ad completed and a session is active.
- `InterstitialAdService` (created on first use, persistent) loads `Interstitial_iOS` and is called from `LevelManager.LoadNextLevel`, between the level-complete popup and the Shop. If no ad is loaded, the Shop opens immediately. `ShowEveryNLevels` (default 1) and `MinSecondsBetweenAds` (default 45) are static fields. A 12 s watchdog guarantees the Shop opens even if the SDK never calls back.
- The ad unit `Interstitial_iOS` must exist on the Unity Ads dashboard under Monetization, Ad Units. Until it does, loads fail quietly every 10 s and no interstitial shows.
- Impressions: an interstitial or rewarded ad counts one impression each time it is displayed; rewarded revenue mostly depends on completion. A banner counts one impression every time it is drawn or refreshed while visible (the SDK refreshes roughly every 30 s), and nothing while hidden.
- Info.plist keys for tracking and export compliance are added by `Assets/Editor/IOSPostBuild.cs` on every export.

## Persistence

PlayerPrefs key `FoodTruckSession` holds the JSON session; `FoodTruckHighScore` the high score; `GlobalAudioState` and `GlobalMusicState` the settings toggles. Upgrade levels are stored as two parallel lists because JsonUtility drops dictionaries. Old saves without the lists get the defaults (Coffee 1, CoffeeMachine 1).

## Verifying a change

1. Compile with the csc command in `CLAUDE.md` (Unity batchmode cannot run while the editor is open).
2. If Unity is open, click into it so it refreshes, then read `~/Library/Logs/Unity/Editor.log` for `error CS` and runtime exceptions.
3. For anything touching input, timers, or the save file, run the test checklist in `Docs/TODO.md` on a real phone.
4. Commit with a message that says what was broken, not just what changed. Push with `git push origin main` (a write token is stored in the keychain).

## History

- March 2026: version 26.03.29 shipped on the App Store.
- 7 October 2026: project moved to `Desktop/Sept/FoodTruckCafe`; 186 never-committed files pushed; two review passes fixed the bugs listed under Done in `Docs/TODO.md`; interstitial ads added between level and shop.
