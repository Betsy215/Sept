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

Per order: sum of item points, plus a tip of `remainingSeconds x 10` rounded to cents. Item points (ScoreManager in GameSceneOne): Coffee 10, Bread 15, Apple 15, Juice 20, Melon 25, Pie 30, Mont Blanc 35, Log Cake 35. Expired orders still pay item points but no tip. Stars come from `LevelData` thresholds against the level score. Money goes to `SessionManager` at order completion or expiry; Restart rolls money back to the level-start snapshot.

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

Per-item behaviour (counts and timers are scene values on each `RefillableItem` and kitchen object, not in code) has not yet been catalogued item by item. That review is the open task before changing hold-to-refill settings.

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
