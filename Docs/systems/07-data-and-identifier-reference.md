# 07 Data and identifier reference

Verified against the working tree on 7 October 2026 (commit `2824c0c`). Scene line numbers are YAML lines in `Assets/Scenes/*.unity`; code lines are in `Assets/Scripts/...`. Nothing in this pass changed code, scenes, prefabs or settings.

Read this before renaming any string, serialized field, scene, layer, animation event or asset. The game matches food, character and upgrade names by exact string across code, scene YAML and the save file, and a mismatch fails silently.

---

## 1. Food type strings

Every place a food identifier is written or compared. "OK" means the string matches the canonical name exactly.

| Canonical | ServeableItem.foodType (GameSceneOne) | OrderSystem.availableFoods (GameSceneOne) | ScoreManager.itemPoints (GameSceneOne) | SessionData default | Shop item (Shop.unity) | KitchenFoodGate (KitchenScene) | Kitchen refill script | Sprite script | Upgrade key |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Bread | `Bread` :5793, in `serveableItems` | `Bread` :6404, display `Bread.prefab` | 15 :4604 | owned (`SessionManager.cs:52`) | none (owned) | none (oven2 has no gate, correct) | `BreadOvenInKitchen` finds `Bread` component | `Bread.cs` (4 sprites, 3 per step) | none |
| Coffee | `Coffee` :1984, in list | `Coffee` :6410, `Coffee.prefab` | 10 :4602 | owned, upgrade level 1 | `CoffeeUpgrade` item, `upgradeFoodType: Coffee` :4147 | none | beans via `CoffeeInKitchen` -> `BeanContainer` | `Coffee.cs` (3 / 4 sprites) | `Coffee` (via ServeableItem foodType, `LevelManager.cs:460`) |
| (machine) | n/a | n/a | n/a | upgrade level 1 | `CoffeeMachineUpgrade`, `upgradeFoodType: CoffeeMachine` :1081 | n/a | n/a | n/a | `CoffeeMachine` (literal, `LevelManager.cs:464`) |
| Juice | `Juice` :2994, in list | `Juice` :6402, `Juice.prefab` | 20 :4608 | no | `Juice` $150 :3452 | `Juice` :466 (cableft/juice) | `JuiceInKitchen` finds `Juice` | `Juice.cs` (5, 1 per step) | none |
| Apple | `Apple` :4733, in list | `Apple` :6406, `Apple.prefab` | 15 :4606 | no | `Apple` $350 :326 | `Apple` :743 (windowleft/apples) | `AppleInKitchen` finds `Apple` | `Apple.cs` (5, 2 per step) | none |
| Choux | `Choux` :1567, in list | `Choux` :6400, `Choux.prefab` | 35 :4620 | no | `Choux` $750 :1327 | `Choux` :358 (oven1) and :1165 (cutboard, decor) | `ChouxOvenInKitchen` finds `Choux` | `Choux.cs` (5) | none |
| Cake | `Cake` :2691, in list | `Cake` :6408, display **`Log.prefab`** | 35 :4618 | no | `Cake` $750 :2917 | `Cake` :1370 (oven3) and :213 (tableright/cake, decor) | `CakeOvenInKitchen` finds `Cake` | `Cake.cs` (7) | none (Cake is not IUpgradeable) |
| Melon | `Melon` :2863, **GameObject inactive, not in `serveableItems`** | absent | 25 :4610 | no | absent | absent | none | none | none |
| Pie | absent | absent | **30 :4612 orphan** | no | absent | absent | none | none | none |
| Mont Blanc | absent | absent | **35 :4614 orphan** | no | absent | absent | none | none | none |
| Log Cake | absent | absent | **35 :4616 orphan** | no | absent | absent | none | none | none |

Mismatches and oddities:

- **Pie, Mont Blanc, Log Cake**: orphan points entries; no item uses those names. Owner decision: leave (Docs/TODO.md, Leave alone). `Cake.cs:18` still says foodType may be "Cake" or "Log Cake"; only "Cake" is real.
- **Melon**: dead item (inactive, unlisted, unsold) that still has a points entry and the only `enableHoldToRefill: 1` (:2900). Owner decision: leave.
- **Cake order icon** is `Log.prefab`, named after the old "Log Cake". Correct by reference; do not rename the prefab without re-pointing :6408.
- **Shop upgrade items** both have `itemName: Upgrade` (:1056, :4122). Only `upgradeFoodType` is used for upgrades, so this is harmless, but the success label reads "Purchased Upgrade!".
- Purchases of food are stored by `ShopItemController.itemName` (`ShopManager.cs:170`), read back by `LevelManager.ApplyServeableItemSettings` (`LevelManager.cs:427`, exact `Contains`), `KitchenFoodGate.ApplyVisibility` (trimmed exact match), and `ShopManager.UpdatePurchasedItemsUI`. All six sellable/owned names match across all four. `RefillSystem.OnItemServed` compares case-insensitively and trimmed (`RefillSystem.cs:101`); `OrderSystem` and `ScoreManager` compare exactly.
- Kitchen refill scripts do not use strings; they call `FindObjectOfType<Bread|Apple|Juice|Choux|Cake|BeanContainer>()`. The class name is the identifier there.
- Cooldown / timer keys are class names (`OvenKitchenBase.GetCooldownKey`, `KitchenItemWithTimer.GetCooldownKey` return `GetType().Name`): `BreadOvenInKitchen`, `ChouxOvenInKitchen`, `CakeOvenInKitchen`, `AppleInKitchen`. Helper GameObjects are named `[OvenHelper] <key>` and `[TimerHelper] <key>` (found by `GameObject.Find`). Juice and Coffee kitchen taps have no cooldown. Renaming a kitchen class orphans any running timer for one session only.
- Saved food positions (`SessionData.savedFoodPositions`) are keyed by foodType, read by `GamePhaseManager.cs:276` and `SessionManager.ApplySavedPositions`.
- `*PreferredFoods` on every customer prefab is `Burger, Fries` (template leftovers). Nothing reads `PreferredFoods`; harmless.

## 2. Character names

| Name | SessionData | Shop item (Shop.unity) | GameObject name | CustomerManager prefab (GameSceneOne :2351) | Prefab file | Script | Per-type behaviour |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Girl | default owned (`SessionManager.cs:58`, also legacy fallback :449) | none | n/a | index 0 | `Girl.prefab` | `GirlCustomer` | baseline |
| Boy | | `Boy` $50 :2649, itemType 1 | `Boy` | index 1 | `Boy.prefab` | `BoyCustomer` | baseline |
| Grandma | | `Grandma` $300 :3192 | **`Granma`** | index 3 | `Grandma.prefab` | `GrandmaCustomer` (logs say "Toad") | `TipMultiplier => 2` |
| Kid | | `Kid` $500 :717 | `Kid` | index 2 | `Kid.prefab` | `KidCustomer` (copy of Girl, logs say "Girl") | `MinOrderItems => 4`, clamped to level max (`CustomerManager.cs:185-190`) |

`CustomerManager.SelectCustomerForLevel` (`CustomerManager.cs:294`) derives the name from the prefab asset name with `Replace("Customer","").Replace("Prefab","")`, then requires an exact match in `purchasedCharacters`. Prefab names equal shop `itemName`s, so every purchasable character spawns. The "Granma" GameObject name is cosmetic (only `itemName` is stored). Renaming a customer prefab file breaks spawning for that character silently (falls back to the others; if none match, `LogError` and no customer, which soft-locks the level).

Customer prefab serialized fields differ: Boy and Girl prefabs still carry stale `walkInAnimationState` etc. (removed from code, ignored). Girl has `sadSpriteRenderer` / `happySpriteRenderer` assigned; Boy, Grandma, Kid have them empty and auto-find (`CustomerController.cs:43-51`: own SpriteRenderer and child `HappySprite`, present on all four).

## 3. Scenes

| Index | Scene | Loaded by (string literal) |
| --- | --- | --- |
| 0 | `MainMenu` | `ShopManager.cs:337` `"MainMenu"`; `LevelManager.mainMenuSceneName` (code default `"MainMenu"`, scene GameSceneOne :3287); `AudioManager.mainMenuSceneName` (compared, not loaded) |
| 1 | `GameSceneOne` | `ShopManager.cs:331` `"GameSceneOne"`; `ClickPlay._sceneName` and `ClickContinue._sceneName` serialized `GameSceneOne` (MainMenu :301, :990; code default empty); `LevelManager.RestartLevel` reloads the active scene by name (:331) |
| 2 | `Shop` | `LevelManager.cs:670` `"Shop"` (inside the interstitial continuation) |
| 3 | `KitchenScene` (additive) | `KitchenSceneManager.kitchenSceneName` (code default and GameSceneOne :6221 `KitchenScene`), `LoadScene(..., Additive)` :66, `UnloadSceneAsync` :90 |

All loads except the kitchen go through `SceneTransitionManager.TransitionToScene` (ignores requests while a fade is running). No code uses build indices.

## 4. PlayerPrefs keys

| Key | Type | Default | Writer | Reader |
| --- | --- | --- | --- | --- |
| `FoodTruckSession` | string (JsonUtility of `SessionData`) | absent = no session | `SessionManager.SaveSession` :431 (every purchase, level change, score change, app pause/unfocus) | `SessionManager.LoadSession` :440 |
| `FoodTruckHighScore` | float | 0 | `SessionManager.SaveHighScore` :307, only from `CheckAndSaveHighScore`, only from `LevelManager.ShowFinalCompletionMessage` | `SessionManager.GetHighScore` :297 |
| `GlobalAudioState` | int 1/0 | 1 | `SettingsButtonsController.cs:86` | `SettingsButtonsController.cs:77` |
| `GlobalMusicState` | int 1/0 | 1 | `SettingsButtonsController.cs:87` | `SettingsButtonsController.cs:78` |

`SessionData` JSON fields (renaming any loses saved data on update): `totalScore`, `currentLevel`, `levelsCompleted`, `isActive`, `purchasedFoodItems`, `purchasedCharacters`, `foodUpgradeKeys`, `foodUpgradeValues`, `savedFoodPositions[].foodType/x/y`. `sessionStartTime` (DateTime) is not serialized by JsonUtility.

## 5. Ads

| Item | Value | Where |
| --- | --- | --- |
| Unity Ads iOS game ID | `6074412` | `AdsInitializer._iOSGameId` code default and MainMenu :752 |
| Test mode | `false` | `AdsInitializer._testMode`, MainMenu :753 |
| Rewarded unit | `Rewarded_iOS` | `RewardedAdButton._adUnitId`, Shop :4327 |
| Interstitial unit | `Interstitial_iOS` | `InterstitialAdService.AdUnitId` const :21 (must exist on the dashboard) |
| UnityConnectSettings game IDs | empty | `ProjectSettings/UnityConnectSettings.asset:31-34` (unused; IDs are in code) |

## 6. Layers, tags, sorting layers (`ProjectSettings/TagManager.asset`)

- Tags: none custom. `MainCamera` is set on the GameSceneOne `Camera`, MainMenu and Shop `Main Camera`; required by `Camera.main` in `DraggableFood.cs:89` and `TableLayer.cs:54`. The KitchenScene camera is Untagged on purpose (keeps `Camera.main` pointing at the game camera).
- Layers: 0 Default, 1 TransparentFX, 2 Ignore Raycast, 4 Water, 5 UI, **6 Kitchen**. Every KitchenScene object is on layer 6; the kitchen camera culls only layer 6 (mask 64, KitchenScene :1050). `KitchenSceneManager.DisableGameInteractions` (:108) disables every collider not on `"Kitchen"` while the kitchen is open. Renaming the layer breaks that lookup (NameToLayer returns -1 and every collider, kitchen ones included, gets disabled).
- Sorting layers: Default, `Customoers` (typo, unused), `Table` (TableCloth), `FoodItems` (9 renderers in GameSceneOne), `GameUI`, `Popup`, `KitchenBG` (kitchen Canvas), `KitchenObjects`, `KitchenUI`. GameSceneOne `PopupCanvas` (:5123) references sorting layer ID `-262186767`, which does not exist (deleted layer); Unity treats it as Default. It is a Screen Space Overlay canvas ordered by sorting order 1, so nothing visible depends on it. Code sets only `transitionCanvas.sortingOrder = 9999` (`SceneTransitionManager.cs:80`).

## 7. Animators and animation events

| Controller | Used by | Parameters | States -> clips |
| --- | --- | --- | --- |
| `CustomerAnimatorController` | Boy, Girl, Grandma, Kid prefabs | `CustomerState` int, `IsHappy` bool, `TriggerReaction` trigger | Sad_Toad_Walking, Perfect_Order_Toad, happy_toad_walking, Sad_Toad_Walk_Out |
| `CoffeeController` | GameSceneOne CoffeeMachine | `StartBrewing` trigger, `BrewingLevel` int (default 1) | Idle, Brewing_1Cup (`brew`), Brewing_2Cup (`brew 1`) |
| `Settings.controller` | Settings.prefab (MainMenu, GameSceneOne) | `Show` trigger | setting, hidesetting (both `setting.anim`) |
| `TutorialPanel.controller` (in `Assets/Images`) | GameSceneOne TutorialPanel | none | dragAnimation (`drag.anim`) |
| `truckImg.controller` | MainMenu truck | none | `truck.anim` |

Parameter writers: `CustomerController.cs:127-238` and each `*Customer.PlayWalkInAnimation` (`SetInteger("CustomerState")`, `SetBool("IsHappy")`, `SetTrigger("TriggerReaction")`); `CoffeeMachine.cs:116-117`; Settings button persistent call `SetTrigger("Show")` (Settings.prefab :445-452, GameSceneOne override :5411) and `SettingsButtonsController.showTrigger = "Show"`. `CustomerState` is written but no transition reads it (harmless).

Animation events:

| Clip | Time | functionName | Receiver |
| --- | --- | --- | --- |
| `Sad_Toad_Walking.anim:170` | 2.017 | `OnReachedServicePoint` | `CustomerController.OnReachedServicePoint` (:246), on all four customer prefabs |
| `brew.anim:92` | 4.967 | `OnFinish1Cup` | `CoffeeMachine.OnFinish1Cup` (:131) |
| `brew.anim` | 5.0 | `CompleteBrewing` | `CoffeeMachine.CompleteBrewing` (:169, private; events reach private methods) |
| `brew 1.anim:98` | 5.333 | `OnFinish1Cup` | as above |
| `brew 1.anim` | 6.667 | `OnFinish2Cup` | `CoffeeMachine.OnFinish2Cup` (:151) |
| `brew 1.anim` | end | `CompleteBrewing` | as above |
| **`drag.anim:500`** | 4.0 (clip end, looping) | **empty** | none: Unity logs "AnimationEvent has no function name specified" every loop while the tutorial hand plays |

No event names a method that no longer exists. Unused receivers kept for future clips: `OnWalkInComplete`, `OnWalkOutComplete`, `OnReactionComplete`, `OnAnimationMidpoint`, `OnCustomerReady` (`CustomerController.cs:255-280`). `ServeableItem.cs:217` sends `OnServedSuccessfully` with DontRequireReceiver; nothing implements it.

## 8. Tunable numbers

"Scene" means a serialized value in the scene overrides the code default; change it in the Inspector, not in code.

| Tunable | Effective value | Code default | Location | Effect |
| --- | --- | --- | --- | --- |
| Order timer | 5 s (Days 1-5), 4 s (6-14) | 5 | `LevelData.orderDisplayTime`, written to `OrderSystem` by `LevelManager.ApplyLevelSettings` | seconds to fill an order |
| Orders per level, min/max items, gap | see Docs/maintenance.md; gap 3 s | 3 / 1 / 4 / 2 | `Assets/Entity/Level1-14Data.asset` | level length |
| Star thresholds | 50/100/150 ... 650/750/850 | 10/20/30 | LevelData `starThreshold1-3` | stars |
| Perfect fraction | 0.5 | 0.5 | `OrderSystem.perfectTimeFraction` (not serialized in scene yet, code default applies) | perfect-order sound |
| Points per item | table above; fallback 0 | 10 | ScoreManager :4599 `pointsPerItem: 0` | score and money |
| Tip | `itemPoints x remainingSeconds x 10 / 100 x customerMultiplier`, rounded to cents | `timeBonusMultiplier` 5 | ScoreManager :4600 = 10; `ScoreManager.cs:152-157` | tip per order. (Docs/maintenance.md says "remainingSeconds x 10"; the real formula also scales by the order's item points / 100.) |
| Customer tip multiplier | Grandma 2, others 1 | 1 | `GrandmaCustomer.TipMultiplier` | tip |
| Kid min items | 4, clamped to level max | 1 | `KidCustomer.MinOrderItems` | order size |
| Next customer delay | 2 s | 2 | CustomerManager :2360 | gap after a customer leaves |
| Customer timings | walk-in fallback 2 s, service delay 1 s, walk-out pause 0.3 s, sad exit 2 s | same | `CustomerController.cs:6-8,115` (Awake re-sets 2 / 1) | |
| Level-complete popup delay | 3 s | | `LevelManager.cs:560` | |
| Count-up | amount / 100 s, clamped 0.5-10 s; tick sound each 1 s; 1 s pauses between steps | | `LevelManager.cs:589-599,739-781` | Today Sale / Tips transfer |
| `scoreTransferDuration` / curve | 2 / linear | | GameSceneOne LevelManager | unused fields |
| Hold threshold | 0.3 s | | `RefillableItem.cs:326` | hold-to-refill start |
| Refill per count | per item `customRefillTime` > 0, else `RefillSystem.refillTimePerCount` 1 s | 1 | GameSceneOne :1514 | hold refill speed |
| Default max count | 5 | 5 | RefillSystem :1513 | items with `customMaxCount` 0 |
| Item max counts | Bread 9, Coffee 2 (3 upgraded, set in `Coffee.cs`), Juice 4, Apple 8, Choux 4, Cake 6, Melon 3 | 0 | RefillableItem per item | stock |
| Shake | 0.15 units, 0.4 s, 3 cycles | same | ServeableItem (all items) | out-of-stock tap |
| Drag | smoothness 0.8, z offset -1, scale 1.05, min distance 0 | same | `DraggableFood.cs:9-30` (added at runtime) | arrangement drag |
| Wiggle | ±1°, speed 20, 0.01 units | const | `DraggableFood.cs:41-43` | arrangement wiggle |
| Table bounds | padding 0.3, coverage 0.5 | same | TableLayer | drag limits |
| Bake / respawn | Bread oven 8 s, Choux oven 10 s, Cake oven 15 s, Apple re-tap 8 s | 10 / 5 | KitchenScene :1260, :344, :1356, :726 | kitchen refill |
| Kitchen pop | 1.3x, 0.15 + 0.1 s (Apple 0.12 + 0.08) | same | kitchen tap scripts | |
| Bean max | 6 | const | `BeanContainer.cs:11` | coffee brews per refill |
| Coins per rewarded ad | 5 | 5 | Shop :4329 | |
| Rewarded retry | 3 s doubling to 30 s | same | `RewardedAdButton.cs:19-21` | |
| Interstitial spacing | every 1 level, min 45 s apart, 12 s watchdog, 10 s load retry, 20 s init wait | static/const | `InterstitialAdService.cs:24-30,72` | |
| Fade | 1 s total (0.5 out + 0.5 in) | 1 | MainMenu TransitionSetup :1088 | scene transitions |
| Shop slide-in | 3 s | 1 | Shop :3788 | |
| Shop scroll | 800 px over 1 s, clamp -38..2000 | same | ShopManager | |
| Play / Continue delay | 2 s (no start-over confirm since 9 October) | | `ClickPlay.cs:26,53`, `ClickContinue` | |
| Pause press | 0.1 s realtime | | `PauseButton.cs:130` | |
| Score popup | 1 s anim, stays 4 s, fade 0.5 s, 0.2 s delay | | SimpleScorePopup, `ScoreManager.cs:184` | |
| Feedback text hide | 2 s | | `ScoreManager.cs:329` | |
| Volumes | music 0.4 (MainMenu override), SFX 0.8; order-complete SFX at 0.7 x SFX | 0.7 / 0.8 | AudioManager | |
| Wobble | KitchenButton/backButton pos 5/1, freq 1, rot 1/1, scale 0.01/1; Get5Coins 2/1, 15, 1.5/8, 0.02/3; rhythm 60 bpm | 15 / 8 / 3 | BalatroWobble, Balatrorhythm | |

## 9. Serialized fields with non-default values (do not rename)

Per script, the fields whose scene or prefab value differs from the code default. Renaming any of these resets it to the code default.

- `OrderSystem` (GameSceneOne): `ordersPerLevel` 1, `timeBetweenOrders` 5, `systemInitializationDelay` 0, `availableFoods[]` (6 entries), `itemSpacing` 1, `singleColumnStartPosition` (-0.55, 1.3), `twoColumnStartPosition` (-1.15, 1.3), `speechBubble1/2/4` (3 intentionally empty), all references. (Per-level values are overwritten from LevelData at load.)
- `ScoreManager`: `pointsPerItem` 0, `timeBonusMultiplier` 10, `itemPoints[]`, `popupFont` beachday SDF, `popupBackgroundSprite` paper 4, references.
- `RefillSystem`: references only (values equal defaults).
- `RefillableItem` x7: `customMaxCount`, `customStartingCount` (Choux 4), `customRefillTime` (Coffee 0.5, Apple 1, Melon 1.5, Choux/Juice -1), `enableHoldToRefill` (0 on all but Melon; code default is true), `serveableItem` (set on Coffee, Apple, Choux).
- `ServeableItem` x7: `foodType`, `orderSystem`, `useAudioManager` (0 on Cake and Bread), `serveSound` (Juice), `enableDebugLogs` (0 on Juice).
- Sprite scripts: `Bread.breadSprites` + `countPerSpriteChange` 3 (default 2); `Apple.appleSprites` + 2; `Juice.juiceSprites` + 1; `Choux.chouxSprites` (has `[FormerlySerializedAs("breadSprites")]`, keep it); `Cake.cakeSprites`; `Coffee.level1CoffeeSprites`, `level2CoffeeSprites`.
- `CoffeeMachine`: `beanContainer`, `coffeeRefillableItem`, `animator`, `level2Sprite`. `BeanContainer.beanLevelSprites`, `spriteRenderer`.
- `LevelManager`: `allLevels[14]`, `serveableItems[6]`, all UI refs, `audioManagerPrefab`, `debugMode` 0, `debugStartLevel` 13, `scoreTransferDuration` 2.
- `CustomerManager`: `customerPrefabs[4]`, `spawnPoint`, refs.
- `GamePhaseManager`: `arrangmentUI` (misspelt, keep), `levelTutorialMessages` (2 scene messages replace the 2 code defaults), refs.
- `TableLayer`: refs; `showDebugInfo` 1.
- `StarProgressBar`: star objects, images, `starFilledSprite`, `starUnfilledSprite`.
- `KitchenSceneManager`: `kitchenSceneName`.
- `OpenKitchenButton`, `CloseKitchenButton`: `clickSound` squish, `levelManager`.
- Kitchen: `OvenKitchenBase.defaultSprite`, `bakingSprite`, `bakeCompleteSound`, `bakeTime`; `AppleInKitchen.respawnDelay` 8, pop values; `JuiceInKitchen` / `CoffeeInKitchen.refillSound`; `KitchenFoodGate.associatedFoodType` x6.
- `ShopManager` (Shop): `slideAnimationDuration` 3, all refs. `ShopItemController` x9 overrides: `itemName`, `price`, `itemType`, `upgradeFoodType`, `popupInfoText`, `purchaseButtonText`. The instances also carry stale overrides `itemIcon`, `iconImage`, `itemPrefab`, `isAvailable` for fields that no longer exist; ignored by Unity.
- `RewardedAdButton`: `_adUnitId`, `_rewardSFX` cash, `_coinsPerAd` 5.
- `AdsInitializer`: `_iOSGameId`, `_testMode`.
- `SessionManager` (MainMenu): `totalFoodItems` 8, `totalCharacters` 4 (defaults 4 / 2; see bug B2).
- `ClickPlay`, `ClickContinue`: `_img`, `_default`, `_pressed`, `_source`, `_sceneName`; `_compressClip` / `_uncompressClip` point at deleted clips (null).
- `TransitionSetup`: `transitionDuration` 1, `transitionColor` black.
- `AudioManager` (MainMenu instance overrides): `shopMusic`, `musicVolume` 0.4, `gameplayMusic`, `mainMenuMusic` None, `levelCompleteMusic` None, `moneyCountSound`, `orderCompleteSFX`, `customerWalkInSFX`, `moneyCompleteSound`, `arrangementPhaseMusic`, `defaultOrderDoneSound`, `defaultPerfectOrderSound`, `gameplayPlaylist[3]`. Prefab-level `buttonClickSFX` and `itemPickupSFX` point at a deleted clip (null); `wrongItemSFX`, `levelWinSFX` are None.
- `SettingsButtonsController` (Settings.prefab): `settingsAnimator`, `audioButton`, `musicButton`.
- `BalatroWobble` / `Balatrorhythm`: underscore-prefixed fields above.

## 10. Asset inventory

- Scenes (4): `MainMenu`, `GameSceneOne`, `Shop`, `KitchenScene`.
- Prefabs (14): order icons `Apple`, `Bread`, `Choux`, `Coffee`, `Juice`, `Log` (Cake icon); customers `Girl`, `Boy`, `Kid`, `Grandma`; `AudioManager`, `Settings`, `ShopItemPrefab`; **`Mb`** is used by nothing live (only the stale `itemPrefab` override on three shop items; its own sprite reference is missing).
- ScriptableObjects: `Assets/Entity/Level1Data` to `Level14Data` (LevelData). Package settings assets under `Assets/Adaptive Performance`, `Assets/XR`, `Assets/Editor/com.unity.mobile.notifications`.
- Animator controllers (5): see section 7. Clips (9) in `Assets/Animation/Clips`.
- Fonts: `beachday.otf` -> `TextMesh Pro/Resources/Fonts & Materials/beachday SDF.asset` (game UI and score popups); LiberationSans SDF (TMP default).
- Key audio: music `ukulele...274858.mp3` (shop), `birds-nature-relax...110839.mp3` (arrangement), playlist `cute-music-26476.mp3`, `piano-country-music-61994.mp3`, `Sounds/edited-bird-music.wav`; SFX `cash.mp3`, `cha-ching-7053.mp3`, `coin.mp3`, `door_EDITED.wav`, `squish.wav`, kitchen `ovensound_EDITED.wav`, `pour_EDITED.wav`, beans wav, `voicebosch-ice-cubes...wav` (Juice serve). `3-20. Inn.mp3` is referenced only by the prefab and overridden to None in the scene.
- Files over 1 MB: `Sounds/edited-bird-music.wav` 8.6 MB, `Audio/birds-nature-relax-sounds-110839.mp3` 8.2 MB, `Audio/ukulele...274858.mp3` 2.6 MB, `Images/bg.png` 2.4 MB, `Images/knockout 1.PNG` 2.0 MB, `Images/Kitchen/未命名作品.png` 1.9 MB (kitchen background), `Images/truckbg.png` 1.5 MB, `Untitled_Artwork 5-9.png` 1.0-1.3 MB each (Choux), `cute-music` 1.3 MB, `piano-country-music` 1.3 MB, `Images/icon.png` 1.0 MB.
- App icon PNGs `Images/20.png` to `180.png` and `icon.png` are referenced from ProjectSettings, not scenes.
- Missing references (asset deleted before the March 2026 release, so they shipped this way): `ClickPlay` and `ClickContinue` press clips (MainMenu :298-299, :987-988), AudioManager prefab `buttonClickSFX` / `itemPickupSFX` / prefab-level `gameplayMusic` / `levelCompleteMusic` (the last two overridden in the scene), `Bread.prefab` and `Mb.prefab` sprite (Bread's is replaced at runtime by `RandomSprite`), stale shop `itemIcon` overrides, a missing script on MainMenu `Canvas` (:705). None came from the 7 October asset trim.

## 11. External identifiers (no secrets)

| Item | Value | Source |
| --- | --- | --- |
| iOS bundle ID | `com.BetzzzGame.FoodTruckCafe` | `ProjectSettings/ProjectSettings.asset:169` |
| Version / build | `26.03.29` / iPhone build 1 | :143, :174 |
| Company / product | `Betzzz` / `FoodTruckCafe` | :15-16 |
| Apple team ID | `XY39924J9T`, automatic signing | :245, :252 |
| Unity Ads game ID | `6074412` | section 5 |
| Ad units | `Rewarded_iOS`, `Interstitial_iOS` | section 5 |
| GitHub | `https://github.com/Betsy215/Sept.git`, repo root is the FoodTruckCafe folder | `git remote -v` |
| Info.plist keys added at export | `NSUserTrackingUsageDescription`, `ITSAppUsesNonExemptEncryption = false` | `Assets/Editor/IOSPostBuild.cs:25,29` |

## 12. Known mismatch findings from this pass

Detail and repro steps are in the session report; summary here so a future reader knows they were looked at.

- B1 (MINOR): finishing Day 14 and tapping Main Menu before the count-up ends skips `CompleteSession` and the high-score save; Continue then replays an empty "level complete" popup with no session-safe path. `LevelManager.cs:560-625,352-368`.
- B2 (MINOR): the level-complete "Unlocked Food" count is computed from `totalFoodItems` 8 (MainMenu :449), but only 6 foods exist, so it is always 2 too high and never reaches 0.
- B3 (NOTE): `drag.anim` has an event with no function name (log spam each loop).
- B4 (NOTE): several SFX are silent because their clips were deleted before release (section 10). Owner decision on AudioManager clips: leave.
- B5 (NOTE): double-tapping Next Level while an interstitial is loading opens the Shop immediately and reloads it again after the ad.
- Checked and fine: every shop `itemName` matches a ServeableItem foodType or a customer prefab; every KitchenFoodGate name is purchasable; every animation event has a receiver; every purchasable character can spawn; `upgradeFoodType` keys match the IUpgradeable lookups.
