# Architecture

How the game is put together, for someone opening the project after a break.

## Scenes and flow

```
MainMenu ──Play / Continue──▶ GameSceneOne ──level complete──▶ Shop ──Next level──▶ GameSceneOne
   ▲                              │  ▲                            │
   └────────── Main Menu ─────────┘  └── KitchenScene (additive) ─┘  └── Main Menu ──▶ MainMenu
```

- **MainMenu** creates the persistent managers (below), starts menu music, initialises Unity Ads, and offers Play (new game, asks for a second tap if a saved game exists) and Continue (enabled once a level has been completed).
- **GameSceneOne** is the only gameplay scene. Every level runs here; the level number comes from the saved session. It has two phases: *arrangement* (drag food onto the table, no timer) and *playing* (customers and orders).
- **KitchenScene** loads additively on top of GameSceneOne when the kitchen button is tapped. It is where baking and brewing refill stock. While it is open, the game scene's colliders are disabled.
- **Shop** runs between levels. Buys foods, characters, and upgrades with coins, and hosts the rewarded ad button.

Scene changes go through `SceneTransitionManager.TransitionToScene`, which fades to black on its own canvas and uses unscaled time so it works while paused.

## Level 0 tutorial (branch feature/tutorial-level)

`Tutorial/TutorialDirector.cs` is added to the LevelManager object at start when the session has not seen the tutorial (`SessionData.tutorialSeen`) or `TutorialDirector.ReplayRequested` is set. It builds its own overlay canvas (blocker, caption, hand, end card), skips arrangement, forces a coffee-and-bread order (`OrderSystem.forcedNextOrder`), freezes the order timer (`OrderSystem.freezeTimer`), and drives the real tap handlers for bread, coffee, the coffee machine, the kitchen button and the bread oven. Let's go! restores the coin snapshot, marks the tutorial seen and reloads into Day 1; Replay reloads with the flag set. Sprites live in `Resources/Tutorial`.

## Persistent objects

Created in MainMenu, marked DontDestroyOnLoad, and deduplicated when a scene that also contains them is reloaded.

| Object | Script | Owns |
| --- | --- | --- |
| SessionManager | `Session/SessionManager.cs` | Coins, current level, purchases, upgrade levels, saved food positions. Saves to PlayerPrefs on every change. |
| AudioManager | `Utils/AudioManager.cs` | Music per scene or phase, sound effects, mute state. Starts menu music whenever MainMenu loads. |
| SceneTransitionManager | `SceneTransition/SceneTransitionManager.cs` | Fade canvas and scene loading. |
| KitchenSceneManager | `Kitchen/KitchenSceneManager.cs` | Opening and closing the kitchen; becomes persistent from the first game scene. |

Everything else is per scene.

## Gameplay systems in GameSceneOne

| System | Script | Role |
| --- | --- | --- |
| Level | `LevelControl/LevelManager.cs` | Loads `LevelData` for the current level, applies it to the other systems, handles pause, restart, level complete popup, and the money count-up. |
| Phase | `Utils/GamePhaseManager.cs` | Switches between arrangement and playing; enables dragging or serving on each food item. |
| Table | `Utils/TableLayer.cs` | Sizes the table to the camera and safe area; defines drag bounds. |
| Customers | `Customer/CustomerManager.cs`, `CustomerController.cs`, `*Customer.cs` | Spawns one customer at a time from the purchased characters, plays walk-in and walk-out, and asks OrderSystem for an order. Each customer type can tweak order size. |
| Orders | `ItemsAndOrders/OrderSystem.cs` | Builds an order of 1 to 4 purchased foods, shows it in a speech bubble with a countdown, accepts served items, completes or expires the order. |
| Food items | `ItemsAndOrders/ServeableItem.cs`, `DraggableFood.cs`, `Refill/RefillableItem.cs`, `Refill/RefillSystem.cs` | A tap serves; dragging only in arrangement. A 0.3 s hold refills, but only Melon has that enabled and Melon is inactive, so in practice stock comes from the kitchen. See Docs/maintenance.md for the per-item catalogue. Stock counts and out-of-stock state. |
| Scoring | `Scoring/ScoreManager.cs`, `StarProgressBar.cs`, `SimpleScorePopup.cs` | Points per item plus a time-based tip per order, star thresholds per level, popups. Money goes to SessionManager when an order completes or expires. |
| Kitchen | `Kitchen/*`, `Food/FoodInKitchen/*` | Ovens and machines with timers that refill stock; timers survive closing the kitchen via a persistent helper. |

Level end: when completed plus expired orders reach `ordersPerLevel`, LevelManager records the level in the session, stops customers, waits 3 s, and shows the level-complete popup with Next Level (to Shop), Restart (reloads the scene and restores coins to the level-start snapshot), and Main Menu.

## Saving

One JSON blob under the PlayerPrefs key `FoodTruckSession`, written synchronously after every change and on app pause. Upgrade levels are stored as two parallel lists because Unity's JsonUtility cannot serialise a Dictionary. A high score lives under `FoodTruckHighScore`. Settings (music and SFX on or off) are separate PlayerPrefs ints.

## Ads

`Ads/AdsInitializer.cs` (MainMenu) requests App Tracking Transparency, initialises Unity Ads with the iOS game ID, and preloads the interstitial. `Ads/RewardedAdButton.cs` (Shop) loads `Rewarded_iOS`, retries with backoff, shows on tap, and adds coins only when the ad completed and a session is active. `Ads/InterstitialAdService.cs` is a persistent object created on first use; `LevelManager.LoadNextLevel` asks it to show `Interstitial_iOS` and then opens the Shop, or opens the Shop at once when no ad is ready. `Assets/Editor/IOSPostBuild.cs` writes the tracking usage string and the export-compliance key into Info.plist on every export.

Banners were considered and parked: they earn roughly a hundredth of an interstitial per impression and would cover the table or the order bubble in GameSceneOne. If ever added, show them only in Shop and MainMenu and hide them in `SceneTransitionManager.TransitionToScene`.

## Where to change things

| Want to | Go to |
| --- | --- |
| Tune a level (orders, timer, order size, star thresholds) | `Assets/Entity/Level<N>Data.asset` |
| Add a level | Duplicate a LevelData asset, add it to the `allLevels` array on LevelManager in GameSceneOne |
| Add a shop item | Shop scene, duplicate a ShopItemPrefab under the item container, set type, name, price |
| Add a food | Prefab under `Assets/Prefabs`, a `ServeableItem` + `RefillableItem` on it, matching `foodType` string in OrderSystem's list, shop item to unlock it |
| Change music or SFX | AudioManager prefab fields |
| Change ad reward amount | RewardedAdButton in the Shop scene, Coins Per Ad |
| Change version or build number | Player Settings, see `Docs/ios-testing-and-release.md` |

## Conventions and gotchas

- Food types, character names, and upgrade keys are plain strings compared across scripts (`"Bread"`, `"Coffee"`, `"Girl"`, `"CoffeeMachine"`). Rename in every place at once or purchases silently stop matching.
- Input is EventSystem based (`IPointerDown/Up/Click`, `IDrag`). Do not add `OnMouse*` handlers; they ignore pause and misbehave with multi-touch.
- Pause sets `Time.timeScale = 0`. Anything that must move while paused uses unscaled time.
- Debug logging is verbose; `Utils/LogControl.cs` filters it to warnings and errors in release builds.
- Scripts can be compiled outside Unity for a quick check: see `CLAUDE.md`.
