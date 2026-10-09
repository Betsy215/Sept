# 01 Scene flow and startup

Verified against the code and scene YAML on 7 October 2026. Paths are relative to the project root. Line numbers refer to the files as they were on that date. Scene facts came from parsing the `.unity` files (each MonoBehaviour's `m_Script` guid matched to its `.cs.meta`), not from opening Unity.

The rule still applies: **the game is live and works.** This page describes how it works so a change does not break startup by accident.

---

## 1. The four scenes

Build Settings (`ProjectSettings/EditorBuildSettings.asset`) list, all enabled:

| Index | Scene | Role |
| --- | --- | --- |
| 0 | `Assets/Scenes/MainMenu.unity` | Boot scene. Creates the persistent managers. Play and Continue. |
| 1 | `Assets/Scenes/GameSceneOne.unity` | The only gameplay scene. Every level runs here. |
| 2 | `Assets/Scenes/Shop.unity` | Between levels. Purchases and the rewarded ad. |
| 3 | `Assets/Scenes/KitchenScene.unity` | Loaded **additively** over GameSceneOne. |

Index 0 is the scene the app boots into. It is the only scene that creates `AudioManager` and `SceneTransitionManager`, so a session that does not start in MainMenu has no fades and (except through `LevelManager`'s fallback) no audio. Every scene change goes through the name string, never the build index.

### MainMenu

| GameObject | Scripts (and notes) |
| --- | --- |
| `SessionManager` (root) | `SessionManager`. Scene values `totalFoodItems: 8`, `totalCharacters: 4` (`MainMenu.unity:449-450`). |
| `SessionInitializer` (root) | `SessionInitializer` |
| `TransitionSetup` (root) | `TransitionSetup` (`transitionDuration 1`, colour black) |
| `AdManager` (root) | `AdsInitializer` (`_iOSGameId 6074412`, `_testMode 0`, `MainMenu.unity:752-753`) |
| `AudioManager` (root, prefab instance of `Assets/Prefabs/AudioManager.prefab`) | `AudioManager`. Most clips are scene overrides here, not on the prefab. |
| `Main Camera` | Camera, AudioListener |
| `Main Camera/EventSystem` | EventSystem, input module |
| `Main Camera/Canvas` (Screen Space Overlay) | Has one missing script (known, leave alone) |
| `Main Camera/Canvas/play` | `ClickPlay` (`_sceneName: GameSceneOne`), AudioSource |
| `Main Camera/Canvas/ContinueButton` | `ClickContinue` (`_sceneName: GameSceneOne`), Button, AudioSource |
| `Main Camera/Canvas/scrolling bg` | `ScrollingBg` (unscaled time) |
| `Main Camera/Canvas/Settings` (prefab instance of `Settings.prefab`) | `SettingsButtonsController`, Animator, plus a scene-added `SettingButton`. The `setting` child's Button calls `Animator.SetTrigger` (the only persistent call in the prefab). |

Ways in: cold launch; `LevelManager.GoToMainMenu` (`Assets/Scripts/LevelControl/LevelManager.cs:684-696`), reachable from the pause panel at any time and from the level-complete panel on the last level only. `ShopManager.OnMainMenuClicked` (`Assets/Scripts/Shop/ShopManager.cs:334-338`) exists, but **the Shop scene has no Main Menu button** (see below), so that path is never used.

Ways out: `ClickPlay.WaitForDelay` -> `TransitionToScene("GameSceneOne")` (`Assets/Scripts/Buttons/ClickPlay.cs:139`); `ClickContinue.WaitForDelay` -> `TransitionToScene("GameSceneOne")` (`Assets/Scripts/Buttons/ClickContinue.cs:114`).

### GameSceneOne

| GameObject | Scripts (and notes) |
| --- | --- |
| `SessionInitializer` | `SessionInitializer` (creates a SessionManager only when the scene is played directly) |
| `LevelManager` | `LevelManager`. `allLevels` 1 to 14, `serveableItems` = Apple, Choux, Juice, Bread, Cake, Coffee (Melon is not listed), `mainMenuSceneName: MainMenu`, `audioManagerPrefab` = AudioManager prefab, `debugMode 0`. |
| `GamePhaseManager` | `GamePhaseManager` (references LevelManager, OrderSystem, CustomerManager, TableLayer, RefillSystem, `Canvas/TutorialPanel`, `ArrangementUI/Text`) |
| `OrderSystem` (+ `OrderContainer`) | `OrderSystem` |
| `CustomerManager`, `SpawnPoint` | `CustomerManager` (customer prefabs Boy, Girl, Grandma, Kid) |
| `ScoreManager` | `ScoreManager` |
| `RefillSystem` | `RefillSystem` |
| `TableLayer` (+ `TableBounds` BoxCollider2D, `TableCloth`) | `TableLayer` |
| `KitchenSceneManager` | `KitchenSceneManager` (`kitchenSceneName: KitchenScene`) |
| `Bread`, `Coffee`, `Juice`, `Apple`, `Choux`, `Cake` | `ServeableItem`, `RefillableItem`, the matching food script, SpriteRenderer, BoxCollider (3D) |
| `Melon` (inactive) | `ServeableItem`, `RefillableItem` only |
| `CoffeeMachine` (+ `BeanContainer`) | `CoffeeMachine`, Animator, BoxCollider; `BeanContainer` |
| `Camera` | Camera (depth 0, culls everything), AudioListener, two built-in components (the PhysicsRaycaster that makes 3D-collider input work, see maintenance.md) |
| `EventSystem` | EventSystem, input module. The only EventSystem while the kitchen is open. |
| `Canvas` (Screen Space Camera) | `Board` (`LevelInfoText`, `ScoreText`, `StarProgressBar`), `KitchenButton` (`OpenKitchenButton`, `BalatroWobble`, Button), `PauseButton` (`PauseButton`), `TutorialPanel`, `bg`, `Settings` prefab instance |
| `ArrangementUI` (Overlay, Default layer, order 0) | `DoneButton`, `Text` |
| `PopupCanvas` (Overlay, sorting layer Popup, order 1) | `PausePanel` (inactive; Close, Restart, MainMenu buttons), `LevelCompletePanel` (inactive, full-stretch, raycast target; NextLevel, Restart, MainMenu buttons all inactive in the scene; text fields) |
| `Bubble1`, `Bubble2`, `Bubble4` (inactive) | Speech bubbles used by OrderSystem |

**No button in GameSceneOne has a persistent (Inspector) onClick call** except the Settings prefab's `SetTrigger` override. Every gameplay button is wired in code: level-complete and pause buttons in `LevelManager.SetupLevelCompleteUI` / `SetupPauseUI` (`LevelManager.cs:153-229`), Done in `GamePhaseManager.InitializeGamePhase` (`Assets/Scripts/Utils/GamePhaseManager.cs:64`), Kitchen in `OpenKitchenButton.Start` (`Assets/Scripts/Kitchen/OpenKitchenButton.cs:40`), Pause through `IPointerUp` in `PauseButton` (`Assets/Scripts/Buttons/PauseButton.cs:105-174`).

Ways in: Play or Continue from MainMenu; `ShopManager.LoadNextGameLevel` -> `TransitionToScene("GameSceneOne")` (`ShopManager.cs:331`); `LevelManager.RestartLevel` reloads the active scene (`LevelManager.cs:331`).

Ways out: `RestartLevel` (itself, `LevelManager.cs:331`); `LoadNextLevel` -> interstitial -> `TransitionToScene("Shop")` (`LevelManager.cs:667-671`); `GoToMainMenu` -> `TransitionToScene("MainMenu")` (`LevelManager.cs:695`); `KitchenSceneManager.OpenKitchen` -> `SceneManager.LoadScene("KitchenScene", LoadSceneMode.Additive)` (`Assets/Scripts/Kitchen/KitchenSceneManager.cs:66`).

### Shop

| GameObject | Scripts (and notes) |
| --- | --- |
| `SessionInitializer` | `SessionInitializer` |
| `Shopmanager` | `ShopManager`. `nextLevelButton` = `NextLevelButton`; **`mainMenuButton` is None**; `slideAnimationDuration 3`. |
| `Main Camera` | Camera, AudioListener |
| `EventSystem` | EventSystem, input module |
| `Main Camera/Canvas` (Screen Space Camera) | `ShopPanel` (`ItemContainer` with nine `ShopItemPrefab` instances, `ScrollUpButton`, `ScrollDownButton`), `Score`, `NextLevelButton`, `Get5Coins` (`RewardedAdButton`, `BalatroWobble`, `Balatrorhythm`), `PurchaseConfirmationPopup` (inactive; `Purchase`, `Cancel`, icon, info), `bg` |

Persistent onClick calls (must keep these method names): `NextLevelButton` -> `ShopManager.LoadNextGameLevel` (`Shop.unity:2132`), `Purchase` -> `ConfirmPurchase` (`Shop.unity:2000`), `Cancel` -> `CancelPurchase` (`Shop.unity:472`). `ShopManager.Wire` therefore adds no runtime listener to those three (`ShopManager.cs:75-81`).

There is no Shop scene object for SessionManager, AudioManager, SceneTransitionManager or the Settings prefab. **There is no Main Menu button in the Shop.** The whole Shop hierarchy was listed and no such object exists, and `ShopManager.mainMenuButton` is unassigned. From the Shop the only way to the menu is Next Level, then pause in GameSceneOne, then Main Menu.

Ways in: `LevelManager.LoadNextLevel` only. Way out: `LoadNextGameLevel` -> `TransitionToScene("GameSceneOne")` (`ShopManager.cs:327-332`).

### KitchenScene

| GameObject | Scripts (and notes) |
| --- | --- |
| `Kitchen Camera` | Camera, depth 1, culling mask 64 (layer 6 Kitchen only). No AudioListener. |
| `Canvas` (Screen Space Camera on Kitchen Camera) | `bg` (full image, raycast target) |
| `Canvas/bg/ovens/spot1/oven2` | `BreadOvenInKitchen` (bakeTime 8) |
| `Canvas/bg/ovens/spot2/oven1` | `ChouxOvenInKitchen` (bakeTime 10), `KitchenFoodGate` Choux |
| `Canvas/bg/ovens/spot3/oven3` | `CakeOvenInKitchen` (bakeTime 15), `KitchenFoodGate` Cake |
| `Canvas/bg/windowleft/apples` | `AppleInKitchen` (respawnDelay 8), `KitchenFoodGate` Apple |
| `Canvas/bg/cableft/juice` | `JuiceInKitchen`, `KitchenFoodGate` Juice |
| `Canvas/bg/shelf/coffee` | `CoffeeInKitchen` |
| `Canvas/bg/tableleft/cutboard` | `KitchenFoodGate` Choux |
| `Canvas/bg/tableright/cake` | `KitchenFoodGate` Cake |
| `Canvas/bg/mat/backButton` | `CloseKitchenButton`, `BalatroWobble`, Button (one dead persistent call with target None, known) |

No EventSystem and no AudioListener on purpose; it uses GameSceneOne's. Because its camera is depth 1, the full-screen `bg` image wins raycasts over GameSceneOne's camera-space `Canvas`, so the pause and kitchen buttons cannot be pressed while the kitchen is open. The two Overlay canvases (`ArrangementUI`, `PopupCanvas`) still sit above it, so the level-complete popup appears and works over an open kitchen.

Way in: `OpenKitchen` (additive). Ways out: `CloseKitchen` -> `SceneManager.UnloadSceneAsync` (`KitchenSceneManager.cs:88-90`), or any Single scene load (Restart, Next Level, Main Menu), which removes the kitchen implicitly. `KitchenSceneManager.OnSceneLoaded` then resets its state (`KitchenSceneManager.cs:179-189`).

### Complete list of scene loads

| Call | File:line | Target | Mode |
| --- | --- | --- | --- |
| `ClickPlay.WaitForDelay` | `Assets/Scripts/Buttons/ClickPlay.cs:139` | `_sceneName` = GameSceneOne | fade, Single |
| `ClickContinue.WaitForDelay` | `Assets/Scripts/Buttons/ClickContinue.cs:114` | `_sceneName` = GameSceneOne | fade, Single |
| `LevelManager.RestartLevel` | `Assets/Scripts/LevelControl/LevelManager.cs:331` | active scene (GameSceneOne) | fade, Single |
| `LevelManager.LoadNextLevel` continuation | `LevelManager.cs:670` | Shop | fade, Single (after the interstitial) |
| `LevelManager.GoToMainMenu` | `LevelManager.cs:695` | `mainMenuSceneName` = MainMenu | fade, Single |
| `ShopManager.LoadNextGameLevel` | `Assets/Scripts/Shop/ShopManager.cs:331` | GameSceneOne | fade, Single |
| `ShopManager.OnMainMenuClicked` | `ShopManager.cs:337` | MainMenu | fade, Single. **Unreachable: no button.** |
| `KitchenSceneManager.OpenKitchen` | `Assets/Scripts/Kitchen/KitchenSceneManager.cs:66` | KitchenScene | Additive, no fade |
| `KitchenSceneManager.CloseKitchen` | `KitchenSceneManager.cs:90` | KitchenScene | UnloadSceneAsync |
| `SceneTransitionManager.TransitionToScene` fallback | `Assets/Scripts/SceneTransition/SceneTransitionManager.cs:126` | any | direct `LoadScene` when the manager is inactive |
| `SceneTransitionManager.TransitionCoroutine` | `SceneTransitionManager.cs:157` | any | the real load |

---

## 2. Persistent objects

| Object | Born | Singleton | On re-entering its home scene | What survives | What resets it |
| --- | --- | --- | --- | --- | --- |
| **SessionManager** | MainMenu scene object, or `new GameObject` from `SessionInitializer.Awake` (`Assets/Scripts/Session/SessionInitializer.cs:5-13`) in any scene when `Instance` is null. Also fallbacks in `ClickPlay.cs:129-133` and `ClickContinue.cs:93-97` (never hit in practice). | `Awake`: first one sets `Instance` and `DontDestroyOnLoad`, later ones `Destroy(gameObject)` (`Assets/Scripts/Session/SessionManager.cs:117-143`). No `OnDestroy` clearing `Instance` (never needed, it is never destroyed). | MainMenu's copy is destroyed in its own Awake. | The whole `SessionData` (also saved to PlayerPrefs `FoodTruckSession` on every change), the in-memory `scoreAtLevelStart` snapshot (not saved), cached `levelManager`/`scoreManager` references (which become Unity-null after each scene unload). | `StartNewSession` (Play) replaces the data, cancels kitchen timers and cooldowns (`SessionManager.cs:264-277`). `CompleteSession` sets `isActive = false` (`SessionManager.cs:383-395`). `Start` (and therefore `FindGameReferences`) runs only once, in MainMenu. `LevelManager.Start` re-registers itself each level (`LevelManager.cs:76-79`). |
| **AudioManager** | MainMenu prefab instance. Fallback: `LevelManager.EnsureAudioManagerExists` instantiates the prefab when none exists (`LevelManager.cs:106-133`); that prefab copy lacks the MainMenu overrides. | `Awake` singleton + `DontDestroyOnLoad`; duplicates destroyed (`Assets/Scripts/Utils/AudioManager.cs:52-67`). `Start` returns early on a non-instance and subscribes `SceneManager.sceneLoaded` once (`AudioManager.cs:69-77`). | Duplicate destroyed; `OnSceneLoaded` calls `PlayMainMenuMusic` for every Single load of "MainMenu" (`AudioManager.cs:84-88`). Main menu music is None, so the menu is silent but `currentMusicType` becomes "mainmenu". | Music and SFX sources, `musicEnabled`/`sfxEnabled`, current music type, playlist coroutine. | Nothing resets it. Mute flags are re-applied from PlayerPrefs one frame after Start by every `SettingsButtonsController` (MainMenu and GameSceneOne only, `Assets/Scripts/Buttons/SettingsButtonsController.cs:34-50, 93-104`). |
| **SceneTransitionManager** | Only by `TransitionSetup.Awake` in MainMenu (`Assets/Scripts/SceneTransition/TransitionSetup.cs:12-32`) via `AddComponent`. GameSceneOne and Shop have no TransitionSetup. | `Awake` singleton + `DontDestroyOnLoad`, duplicates destroyed, `OnDestroy` clears `Instance` (`SceneTransitionManager.cs:31-68`). Builds its own overlay canvas (sorting order 9999) in `CreateTransitionUI`. | TransitionSetup sees `Instance` and does nothing. | Fade canvas, `isTransitioning`. | The `finally` block of `TransitionCoroutine` always clears alpha, raycast blocking and `isTransitioning` (`SceneTransitionManager.cs:164-183`). `HideTransition` stops a running fade (`:239-261`). |
| **KitchenSceneManager** | GameSceneOne scene object, first time GameSceneOne loads (not MainMenu). | `Awake` singleton + `DontDestroyOnLoad`, duplicates destroyed, `OnDestroy` clears `Instance` (`KitchenSceneManager.cs:25-37, 164-167`). | Each new GameSceneOne copy is destroyed; the persistent one keeps working. | `isKitchenOpen`, `unloadOperation`, list of colliders it disabled. | `OnSceneLoaded` resets all of that on every Single load (`KitchenSceneManager.cs:179-189`), so leaving with the kitchen open is safe. |
| **InterstitialAdService** | Lazy: first read of `InterstitialAdService.Instance` creates `[InterstitialAdService]` with `DontDestroyOnLoad` (`Assets/Scripts/Ads/InterstitialAdService.cs:34-47`). First caller is normally `AdsInitializer.OnInitializationComplete` (`Assets/Scripts/Ads/AdsInitializer.cs:29-35`), otherwise `LevelManager.LoadNextLevel` (`LevelManager.cs:667`). | Static `instance` field; no Awake dedup (nothing else creates one). | n/a | `adReady`, `levelsSinceLastAd`, `lastAdShownAt`, any pending continuation. | Nothing. Static `ShowEveryNLevels`/`MinSecondsBetweenAds` are code defaults. |
| **KitchenTimerHelper** GOs `[TimerHelper] AppleInKitchen`, `[OvenHelper] BreadOvenInKitchen`, `[OvenHelper] ChouxOvenInKitchen`, `[OvenHelper] CakeOvenInKitchen` | `KitchenTimerHelper.GetOrCreate` from `KitchenItemWithTimer.Awake` / `OvenKitchenBase.Awake` the first time the kitchen opens (`Assets/Scripts/Food/FoodInKitchen/KitchenItemWithTimer.cs:28-34, 227-239`, `Assets/Scripts/Food/FoodInKitchen/OvenKitchenBase.cs:35-46`). Juice and Coffee kitchen items are plain MonoBehaviours and have no helper. | Found by name with `GameObject.Find`, created with `DontDestroyOnLoad` otherwise. Static `instances` list (`KitchenItemWithTimer.cs:200-210`). | n/a | Running realtime timers and their callbacks; static `CooldownRegistry` pick times. | Only `SessionManager.StartNewSession` (`KitchenTimerHelper.CancelAll` + `CooldownRegistry.ClearAll`, `SessionManager.cs:269-271`). Restart, Main Menu and Continue do **not** reset them, so a bake started before a restart still finishes in the new level. |

`AdsInitializer` is **not** persistent: it runs `Awake` every time MainMenu loads, re-requests tracking only while the status is NOT_DETERMINED, and calls `Advertisement.Initialize` only if not yet initialized (`AdsInitializer.cs:13-27`), so an offline first launch retries on each return to the menu.

---

## 3. Startup timeline per scene

There is **no Script Execution Order** for any project script: no `.cs.meta` under `Assets` has a non-zero `executionOrder`, and no script uses `[DefaultExecutionOrder]`. Within one phase, the order between different objects is undefined by Unity. Everything below that lists several scripts in one phase can run in any order.

Known facts about Unity's behaviour that this game relies on:
- `AddComponent` runs the new component's `Awake` (and `OnEnable`) synchronously, before `AddComponent` returns.
- `Start` of a component that is disabled before its first frame does not run until it is enabled again.
- `Destroy(gameObject)` in `Awake` takes effect at the end of the frame; `OnEnable`/`OnDisable` of the doomed copy still run, but its `Start` does not.

### MainMenu (cold launch)

1. **Awake** (any order): `SessionManager.Awake` (loads the save); `SessionInitializer.Awake` (creates a SessionManager if none exists *yet*); `TransitionSetup.Awake` -> `AddComponent<SceneTransitionManager>` -> its Awake builds the fade canvas, *then* TransitionSetup copies duration and colour; `AdsInitializer.Awake` (ATT request + `Advertisement.Initialize`); `AudioManager.Awake` (singleton, creates sources only if unassigned).
2. **OnEnable**: `ClickContinue.OnEnable` -> `Invoke("UpdateButtonState", 0.1f)` (`ClickContinue.cs:23-27`).
3. **Start** (any order): `AudioManager.Start` subscribes `sceneLoaded` and runs `OnSceneLoaded` for MainMenu (silent); `ClickContinue.Start` -> `UpdateButtonState` (`ClickContinue.cs:18-21`); `SettingsButtonsController.Start` (reads PlayerPrefs, wires toggles, starts a 1-frame coroutine); `SettingButton.Start`; `SessionManager.Start` -> `FindGameReferences` (finds nothing in MainMenu).
4. **Frame +1**: `SettingsButtonsController.ApplySettingsAfterDelay` -> `AudioManager.SetSFXEnabled/SetMusicEnabled` (`SettingsButtonsController.cs:45-50`).
5. **+0.1 s**: the Invoke re-runs `UpdateButtonState`.
6. **Async**: `AdsInitializer.OnInitializationComplete` -> `InterstitialAdService.Instance.Preload()` -> coroutine waits for init, then `Advertisement.Load("Interstitial_iOS")`.

Order dependencies:
- **SessionInitializer vs SessionManager Awake.** Whichever runs first decides which SessionManager survives. If SessionInitializer runs first, the survivor is a code-made one with `totalFoodItems = 4`, `totalCharacters = 2` (code defaults, `SessionManager.cs:102-105`) instead of the scene's 8 and 4. Only `LevelManager.UpdateUnlockedItemsDisplay` reads those fields. Everything else is identical.
- `ClickContinue.Start` and `ClickPlay.HasSavedProgress` need `SessionManager.Instance`, which every Awake ordering guarantees before Start.
- `SettingsButtonsController` waits one frame for AudioManager; AudioManager is already alive by then.
- `TransitionSetup` writes `transitionDuration`/`fadeColor` after the manager's Awake has already coloured the fade image with the default `fadeColor`. Duration still applies (read per fade); a non-black `transitionColor` would **not** reach the image.

### GameSceneOne (every entry: Play, Continue, Shop Next Level, Restart)

1. **Awake** (any order): `SessionInitializer`; `KitchenSceneManager` (the scene copy dies when one already persists); `TableLayer.Awake` (sizes the table and caches drag bounds, deliberately Awake, `Assets/Scripts/Utils/TableLayer.cs:43-48`); `StarProgressBar.Awake` (stores star scales, deliberately Awake, `Assets/Scripts/Scoring/StarProgressBar.cs:39-46`); `RefillableItem.Awake` x6; `BalatroWobble.Awake`.
2. **OnEnable**: `OrderSystem.OnEnable` (resets counters, runs `InitializeOrderSystem` once per enable cycle, `Assets/Scripts/ItemsAndOrders/OrderSystem.cs:114-121`); `Bread/Apple/Juice/Choux/Cake.OnEnable` subscribe to their `RefillableItem.OnCountChanged` and start a 1-frame sprite refresh; `KitchenSceneManager.OnEnable` (subscribes `sceneLoaded`).
3. **Start** (any order). The ones that matter:
   - `LevelManager.Start` (`LevelManager.cs:60-104`): ensure AudioManager -> register with SessionManager -> `LoadLevel(savedLevel or 0)`. `LoadLevel` (`:352-369`) hides the popup, `starProgressBar.Initialize`, `ApplyLevelSettings` (copies LevelData to OrderSystem, activates purchased items and deactivates the rest, applies upgrade levels to every `IUpgradeable` found with `FindObjectsOfType`, `customerManager.OnLevelLoaded`, level name), then `StartLevel` (`:490-496`): `SnapshotScoreBeforeLevel`, `gamePhaseManager.StartArrangementPhase()`, `scoreManager.ResetScore()`, arrangement music. Only then `SetupLevelCompleteUI`, `SetupPauseUI`, `SetupSessionEvents`.
   - `GamePhaseManager.Start` -> `InitializeGamePhase` (`GamePhaseManager.cs:48-70`): caches `levelManager.serveableItems` as `allFoodItems`, wires Done, calls `StartArrangementPhase()` (the second call per load, see TODO). `StartArrangementPhase` refreshes the draggable cache, shows the tutorial, applies saved positions, enables dragging, **disables OrderSystem and CustomerManager**, shows ArrangementUI, starts a 0.2 s coroutine for the Done button state, tells RefillSystem the phase.
   - `RefillSystem.Start` registers every active `RefillableItem` and `Invoke("RescanForItems", 0.5f)` (`Assets/Scripts/Refill/RefillSystem.cs:27-33`). `RefillableItem.Start` also registers itself, retrying every 0.1 s if RefillSystem is not found (`Assets/Scripts/Refill/RefillableItem.cs:71-128`).
   - `ServeableItem.Start` finds OrderSystem, RefillSystem, and copies `LevelManager.popupCanvas` (`Assets/Scripts/ItemsAndOrders/ServeableItem.cs:67-95`).
   - `ScoreManager.Start` subscribes `OnTotalScoreChanged`; `StarProgressBar.Start` resets star visuals; `Coffee.Start`, `CoffeeMachine.Start`, `BeanContainer.Start`, `Cake.Start`; `PauseButton.Start`, `OpenKitchenButton.Start`; `SettingsButtonsController.Start` (+1 frame); `SettingButton.Start`.
   - `OrderSystem.Start` and `CustomerManager.Start` run only if they get their turn before either `StartArrangementPhase` disables them; otherwise they are deferred until Done re-enables them.
4. **Coroutines/Invokes**: +1 frame food sprite refresh and settings apply; +0.2 s Done button state; +0.5 s RefillSystem rescan.
5. **Done tap** -> `OnDoneButtonClicked` -> save food positions -> `StartPlayPhase` -> `EnableGameplaySystems`: `orderSystem.enabled = true` (OnEnable re-initialises), `customerManager.enabled = true`, `levelManager.StartGamePlay()` (gameplay music, `orderSystem.InitializeForCustomerFlow()`, `customerManager.SpawnCustomerForCurrentLevel()`) (`GamePhaseManager.cs:236-251`, `LevelManager.cs:510-529`). The deferred `CustomerManager.Start` runs later that frame, after the first spawn.

Order dependencies (all relied on, none enforced):
- **LevelManager.Start vs GamePhaseManager.Start.** `LevelManager.StartLevel` calls `gamePhaseManager.StartArrangementPhase()`. If that happens before `GamePhaseManager.Start`, `allFoodItems` is still null and `LoadSavedFoodPositions` does `foreach (var item in allFoodItems)` (`GamePhaseManager.cs:272`). Its early return only covers "no saved positions", and positions are saved on every Done tap, so from the second level on this would throw a NullReferenceException. The exception would abort `LevelManager.Start` before `SetupLevelCompleteUI`/`SetupPauseUI` wire the pause and level-complete buttons. The shipped build evidently runs GamePhaseManager first. **Treat this as the most fragile point in startup.**
- `ApplyLevelSettings` deactivates unpurchased items. If GamePhaseManager ran first, its `EnableArrangementMode` briefly also prepares unpurchased items for dragging (adds `DraggableFood`) before LevelManager hides them. Harmless.
- `ApplyUpgradeLevels` may reach `Coffee.SetUpgradeLevel` before `RefillableItem.Initialize`; that is why `RefillableItem` keeps an upgrade max-count override (`RefillableItem.cs:135-137`) and `Coffee` looks its components up lazily (`Assets/Scripts/Food/Coffee/Coffee.cs:40-46`).
- `StarProgressBar` stores scales in Awake because `LevelManager.Start` can animate stars before `StarProgressBar.Start`.
- `TableLayer` computes bounds in Awake because `LoadSavedFoodPositions` clamps to them in Start.
- `CustomerManager.currentLevelIndex` is set by `OnLevelLoaded` from LevelManager and again by its own `Start` from the session; both give the same value only because `LevelManager` loads exactly the session's level (or sets the session first in debug mode).
- `OrderSystem.InitializeOrderSystem` is idempotent per enable cycle (`OrderSystem.cs:129-138`); the disable in `StartArrangementPhase` is what makes the re-enable at Done rebuild the food list from the items LevelManager activated.
- `ServeableItem` and `CoffeeMachine` copy `popupCanvas` at Start; `CoffeeMachine` takes it from whichever `ServeableItem` `FindObjectOfType` returns first, which has it only if that item's Start already ran (`Assets/Scripts/Food/Coffee/CoffeeMachine.cs:46-50`).

### Shop

1. **Awake**: `SessionInitializer`; `ShopItemController.Awake` x9 (trim names); `BalatroWobble`, `Balatrorhythm`.
2. **Start** (any order): `ShopManager.Start` (wire buttons, shop music, `DelayedInitializeShop` coroutine, `ShopManager.cs:38-60`); `ShopItemController.Start` x9 (finds ShopManager, wires its icon, shows unaffordable); `RewardedAdButton.Start` (wires itself, begins load, `Assets/Scripts/Ads/RewardedAdButton.cs:39-52`).
3. **End of first frame**: `InitializeShop` (score text, affordability, owned marks, slide-in over 3 s), then another end-of-frame scroll-button update.

Order dependency: `ShopManager` waits `WaitForEndOfFrame` so every `ShopItemController.Start` (which sets the "unaffordable" look) has run before it applies the real state.

### KitchenScene (each open)

1. **Awake**: `AppleInKitchen` -> `KitchenItemWithTimer.Awake` (helper, then may hide itself and re-attach to a running cooldown, `KitchenItemWithTimer.cs:28-61`); three ovens -> `OvenKitchenBase.Awake` (helper, then restore the baking sprite or refill at once if the bake ended while closed, `OvenKitchenBase.cs:35-73`); `CoffeeInKitchen`, `JuiceInKitchen` (store scale); `BalatroWobble`.
2. **Start**: `KitchenFoodGate.Start` x6 (hide objects for unpurchased foods, `Assets/Scripts/Kitchen/KitchenFoodGate.cs:19-37`); `CloseKitchenButton.Start` (wire).

Order dependency: an item hidden in Awake (cooldown running) skips its gate's Start until it reappears. Harmless, since only purchased items can have a cooldown.

---

## 4. Button flows

### Play (MainMenu)
1. Pointer down: pressed sprite and sound. Pointer up (`ClickPlay.cs:39-54`): if a session with progress exists (`levelsCompleted >= 1` or `totalScore > 0`) and the button is not armed, show the red warning label for 4 s (real time) and stop.
2. A second release within the window (or the first, with no progress) sets `launching` and starts `WaitForDelay(2)` (scaled seconds).
3. After 2 s: `StartNewSession()` (new SessionData saved, kitchen timers and cooldowns cleared, `OnTotalScoreChanged` fired), `StopMusic`, `TransitionToScene("GameSceneOne")` (`ClickPlay.cs:125-140`).
4. Fade out 0.5 s (unscaled), Single load, fade in 0.5 s. GameSceneOne starts level index 0.

### Continue (MainMenu)
1. Interactable only when an active session has `levelsCompleted >= 1` (`ClickContinue.cs:29-61`), checked at Start and 0.1 s after enable.
2. Pointer up: `launching = true`, `WaitForDelay(2)`.
3. After 2 s: `ContinueSession()` (the cached `levelManager` is dead, so it only fires `OnTotalScoreChanged`), `StopMusic`, `TransitionToScene("GameSceneOne")` (`ClickContinue.cs:88-115`). `LevelManager.Start` loads `currentLevel` from the session.

Play and Continue have separate `launching` flags. With saved progress, tapping Continue and then Play twice within 2 s runs both coroutines; the later one's transition is ignored, but if Play's runs it erases the save first. Since 9 October Play starts over on a single tap (the confirm notice was removed at the owner's request), so this is now one mis-tap away; left as is because the owner asked for the one-tap Play.

### Restart (GameSceneOne: pause panel any time, level-complete panel on levels 1 to 13)
`RestartLevel` (`LevelManager.cs:301-332`): `timeScale = 1`, clear the score popup, hide every panel, `RestoreScoreToSnapshot()` (money back to the value at `StartLevel`), `SetCurrentLevel(currentLevelIndex)` (undoes the advance `OnLevelCompleted` made, but `levelsCompleted` stays advanced), `StopMusic`, `TransitionToScene(active scene)`. Kitchen timers are not reset.

### Next Level (GameSceneOne level-complete panel, levels 1 to 13)
1. The panel appears 3 s (scaled) after the last order, with Next Level and Restart activated before the money count-up (`LevelManager.cs:557-602`). By then `OnLevelComplete` has already saved the level as completed (`:548`).
2. `LoadNextLevel` (`LevelManager.cs:657-672`): `timeScale = 1`, `StopMusic`, `InterstitialAdService.Instance.ShowAfterLevelThen(continuation)`.
3. `ShowAfterLevelThen` (`InterstitialAdService.cs:117-138`) increments `levelsSinceLastAd`. If the ad is not due, not spaced 45 s from the last one, not loaded, or another continuation is already pending, it runs the continuation at once (and preloads if due but not loaded). Otherwise it stores the continuation, starts a 12 s real-time watchdog, and calls `Advertisement.Show`.
4. `OnUnityAdsShowStart` stops the watchdog. `ShowComplete` or `ShowFailure` -> `Finish` (`:175-189`): preload the next ad, run the continuation once.
5. Continuation: `TransitionToScene("Shop")`.
If the SDK reports ShowStart but never ShowComplete, the player sits on the level-complete panel. A second tap on Next Level recovers, because a pending continuation makes `ShowAfterLevelThen` run the new one immediately.

### Main Menu (GameSceneOne: pause panel any time; level-complete panel on the last level only)
`GoToMainMenu` (`LevelManager.cs:684-696`): `StopMusic`, `timeScale = 1`, `TransitionToScene("MainMenu")`. No money rollback: coins earned so far in an unfinished level are kept (Restart rolls them back). AudioManager runs `PlayMainMenuMusic` on load (silent clip).

### Next Level (Shop)
`LoadNextGameLevel` (persistent call, `ShopManager.cs:327-332`): ignored while the purchase popup is open, `AudioManager.Instance.PlayGameplayMusic()` (no null check), `TransitionToScene("GameSceneOne")`. GameSceneOne then switches to arrangement music in `LevelManager.StartLevel`, so the gameplay track plays only during the fade.

### Main Menu (Shop)
**Does not exist.** `OnMainMenuClicked` is wired by `Wire(mainMenuButton, ...)` (`ShopManager.cs:69`), but `mainMenuButton` is None and the Shop has no such GameObject. The TODO checklist item "Return to main menu from the shop: shop music stops" cannot be tested as written.

---

## 5. Events and delegates

All are public `System.Action` fields (not `event`), so any script could also assign or invoke them.

| Delegate | Publisher (invoke sites) | Subscribers | Unsubscribe |
| --- | --- | --- | --- |
| `SessionManager.OnTotalScoreChanged` (`Action<float>`) | `SessionManager.cs:165, 276, 287, 330, 520` (restore, new session, continue, add, deduct) | `ScoreManager.UpdateTotalScoreDisplay` (`Assets/Scripts/Scoring/ScoreManager.cs:111-113`, remove-then-add) | `ScoreManager.OnDestroy` (`ScoreManager.cs:365-368`) |
| `SessionManager.OnSessionCompleted` (`Action`) | `SessionManager.cs:393` | `LevelManager.OnSessionCompleted` (log only, `LevelManager.cs:135-143`) | `LevelManager.OnDestroy` (`LevelManager.cs:145-150`) |
| `SessionManager.OnFoodItemPurchased` (`Action<string>`) | `SessionManager.cs:483` | `KitchenButtonGlowController` only (`Assets/Scripts/Kitchen/KitchenButtonGlowController.cs:35`), which is in no scene | `KitchenButtonGlowController.cs:52` |
| `RefillableItem.OnCountChanged` (`Action<int,int>`) | `RefillableItem.cs:247, 268, 397, 533, 547, 555` | `Bread`, `Apple`, `Juice`, `Choux` (`OnEnable`, lines 21-22 in each), `Cake` (`Assets/Scripts/Food/Cake.cs:61-62`), `Coffee` (`Coffee.cs:22-23`, Start), `KitchenButtonGlowController` (`:29`, unused) | Food scripts in `OnDisable` (line 29; `Cake.cs:69`); `Coffee.OnDestroy` (`Coffee.cs:104-108`); glow controller `:48` |
| `CustomerManager.OnCustomerSpawned` (`Action<CustomerController>`) | `Assets/Scripts/Customer/CustomerManager.cs:339` | `CloseKitchenButtonGlowController` only (`Assets/Scripts/Kitchen/CloseKitchenButtonGlowController.cs:26`), in no scene | `:44` |
| `CustomerManager.OnCustomerCompleted` (`Action<CustomerController>`) | `CustomerManager.cs:217` | none | n/a |
| `SceneManager.sceneLoaded` (Unity) | Unity | `AudioManager.OnSceneLoaded` (`AudioManager.cs:75`), `KitchenSceneManager.OnSceneLoaded` (`KitchenSceneManager.cs:171`) | `AudioManager.OnDestroy` (`:81`, instance only), `KitchenSceneManager.OnDisable` (`:176`) |
| `InterstitialAdService.pendingContinuation` (`Action`) | `Finish` / immediate path (`InterstitialAdService.cs:127, 188`) | the lambda from `LoadNextLevel` | cleared in `Finish` |
| `KitchenTimerHelper.callbacks[key]` (`Action`) | `KitchenTimerHelper.Run` (`KitchenItemWithTimer.cs:266-270`) | `OnRespawn` / `OnBakeComplete` of the latest kitchen instance (closures over possibly destroyed objects; guarded) | removed when fired; `CancelAll` |
| `OrderSystem.ServeItemWithEffect` `onFinished` | `OrderSystem.cs:400` | local lambda | n/a |

Unity Ads listeners (`IUnityAdsLoadListener`/`IUnityAdsShowListener`) are passed per call: `InterstitialAdService` (persistent, safe) and `RewardedAdButton` (Shop object; a load callback that arrives after leaving the Shop lands on a destroyed object).

---

## 6. Time.timeScale

**Who sets it.** Only `LevelManager`: `PauseGame` 0 (`LevelManager.cs:254`); `ResumeGame` 1 (`:274`); `RestartLevel` 1 (`:306`); `LoadNextLevel` 1 (`:660`); `GoToMainMenu` 1 (`:692`). Every exit from GameSceneOne resets it to 1, and pause is refused while the level-complete panel is showing (`:247-251`), so a 0 cannot leak into another scene through the buttons.

**Uses unscaled or real time (keeps running while paused):** scene fades (`SceneTransitionManager.cs:195, 213`); `PauseButton` 0.1 s delay (`PauseButton.cs:158`); interstitial and rewarded load/retry/watchdog timers; kitchen timers and cooldowns (`KitchenItemWithTimer.cs:93, 162-173, 262`, `OvenKitchenBase.cs:57`); kitchen pop effects (`JuiceInKitchen`, `AppleInKitchen`, `CoffeeInKitchen`); `ScrollingBg`, `BalatroWobble`, `Balatrorhythm` animation.

**Uses scaled time (freezes while paused):** order and customer timers, `WaitForSeconds` in `ShowLevelCompletePopup` (the 3 s delay before the panel) and the money count-up, `ClickPlay`/`ClickContinue` 2 s launch delay, `ClickContinue` Invoke, RefillSystem rescan, AudioManager playlist track waits, Shop slide and scroll, Animators in Normal update mode (CoffeeMachine, Settings).

**Still takes input while paused** (the pause panel is 700 x 1000 centred, not full screen):
- PauseButton (toggles resume, by design).
- Pause panel buttons (Close, Restart, Main Menu).
- KitchenButton: `OpenKitchen` returns when `timeScale == 0` (`KitchenSceneManager.cs:41`).
- Food items: `ServeableItem.CanAcceptInput` rejects at timeScale 0 or with PopupCanvas active (`ServeableItem.cs:124-130`); `DraggableFood` rejects drags (`DraggableFood.cs:418`); `CoffeeMachine` rejects brews (`CoffeeMachine.cs:75`).
- Kitchen items reject taps at timeScale 0 (`KitchenItemWithTimer.cs:65`, `OvenKitchenBase.cs:77`, `JuiceInKitchen.cs:25`, `CoffeeInKitchen.cs:25`). They cannot normally be reached while paused anyway: the kitchen cannot open while paused, and pause cannot be pressed while the kitchen is open.
- The Settings toggles, wherever they are not covered; their show animation uses a Normal-mode Animator, so it waits for resume.
- The Done button is covered by the pause panel (centre of the screen), so arrangement cannot be finished while paused.

---

## 7. Gotchas

Intentional oddities
- `StartArrangementPhase` runs twice per level load (LevelManager and GamePhaseManager). It is idempotent; the second call is what makes the start order above survivable.
- GameSceneOne's OrderSystem and CustomerManager start enabled in the scene and are switched off during startup, then on at Done. Do not untick them in the scene: `EnableGameplaySystems` relies on the OnEnable re-initialisation.
- The level is saved as completed the moment the last order ends (`LevelManager.cs:548`), before the panel. Killing the app on the panel skips the Shop; Continue goes to the next level.
- `scoreAtLevelStart` lives only in memory. It is retaken at every level start, so it is always valid in normal play.
- Main Menu keeps the coins earned in an unfinished level; Restart removes them.
- After a level, the level-complete panel offers Main Menu only on the last level. On other levels the route to the menu is Next Level, Shop, Next Level, pause, Main Menu.

Fragile points
- **Start order in GameSceneOne** (section 3): adding, removing or re-ordering root objects in GameSceneOne, or re-saving it from another Unity version, can change which Start runs first. If LevelManager ever runs before GamePhaseManager, levels after the first throw in `LoadSavedFoodPositions` and the pause and level-complete buttons stay unwired. Check the Editor log for a NullReferenceException at `GamePhaseManager.LoadSavedFoodPositions` after any scene edit.
- **Play from MainMenu only.** GameSceneOne and Shop have no TransitionSetup, so `SceneTransitionManager.Instance` is null and Restart, Main Menu and Shop Next Level throw. Shop Next Level also dereferences `AudioManager.Instance` without a check.
- `SessionInitializer` and the code fallbacks create a SessionManager with the code defaults (4 and 2), not the MainMenu scene values (8 and 4).
- `LevelManager.Start` does all its button wiring at the end. Any exception earlier in it (missing LevelData, null `scoreManager.starProgressBar`, `gamePhaseManager` unassigned) leaves the pause and level-complete buttons dead.
- `TransitionToScene` ignores calls while a fade runs, and the fade image blocks raycasts. That is the only double-tap protection for Restart, Main Menu and both Next Level buttons. `LoadNextLevel` itself is not guarded.
- `TransitionSetup.transitionColor` does not change the fade colour (see section 3). Change `SceneTransitionManager.fadeColor` handling if a non-black fade is ever wanted.
- Scene names are hard-coded strings: "GameSceneOne" and "Shop" in code, "MainMenu" in code and on `LevelManager`, `_sceneName` on both MainMenu buttons, `mainMenuSceneName` on AudioManager, `kitchenSceneName` on KitchenSceneManager. Renaming a scene needs every one updated, plus Build Settings.
- `KitchenSceneManager` lives in GameSceneOne, not MainMenu. Before the first GameSceneOne it does not exist; nothing outside GameSceneOne needs it.
- `CLAUDE.md`'s list of "public method names used by scene buttons" does not match the scenes. The only persistent calls in any scene or prefab are `ShopManager.LoadNextGameLevel`, `ConfirmPurchase`, `CancelPurchase` (Shop) and `Animator.SetTrigger` (Settings prefab). The listed names (`LoadNextLevel`, `RestartLevel`, `GoToMainMenu`, `TogglePause`, `OnNextLevelClicked`, `OnMainMenuClicked`, `PurchaseItem`, `ToggleAudio`, `ToggleMusic`) are wired from code. Keep them stable anyway, and keep `LoadNextGameLevel` too.

Likely ways a change breaks startup
- Moving work from `Awake` to `Start` in `TableLayer`, `StarProgressBar`, `SessionManager`, `SceneTransitionManager` or `AudioManager`.
- Adding a second EventSystem or AudioListener to KitchenScene.
- Giving KitchenScene's camera a culling mask beyond layer 6, or changing its depth (raycast priority over the game Canvas).
- Removing the `DontDestroyOnLoad` objects from MainMenu or putting a second `TransitionSetup`/`AudioManager` into another scene with different settings: the first one alive wins and the new scene's copy is silently destroyed.
- Calling `SceneManager.LoadScene` directly instead of `TransitionToScene`: fine functionally, but it skips the fade and the raycast block that protects against double taps.
