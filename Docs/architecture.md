# Architecture

Last verified against the code: 26 November 2026.

How the game is put together. The state, the owner's rules and the features in plain words are in `MANUAL.md`
(section 6); numbers players feel are on the report's Mechanisms page. This file and `systems/*.md` say where the
code for each thing is. Paths are under `Assets/Scripts` unless they start with `Assets/`.

## Scenes and flow

Five scenes in the build (`ProjectSettings/EditorBuildSettings.asset`): MainMenu, GameSceneOne, Shop, KitchenScene, Map.

```
MainMenu ──Play (always a new game)──▶ GameSceneOne as the tutorial ──Let's go──▶ Map
MainMenu ──Continue──▶ Map
Map ──tap a day, Play on its card──▶ (between-days ad, from the third day on) ──▶ GameSceneOne
GameSceneOne ──day-end card, Next──▶ Shop ──Next──▶ Map
GameSceneOne ──pause, Home──▶ Map            Map ──Home──▶ MainMenu
GameSceneOne ⇄ KitchenScene (loaded additively over the table, unloaded on close)
```

- **MainMenu** holds `SessionManager`, the `AudioManager` prefab, `AdsInitializer`, `DailyTreats` and `TransitionSetup`
  (which creates `SceneTransitionManager`). Play (`Buttons/ClickPlay.cs`) acts on pointer-up, waits 2 s, calls
  `StartNewSession()` (the old save is replaced, no confirm) and opens GameSceneOne, where the tutorial runs.
  Continue (`Buttons/ClickContinue.cs`) is enabled once the tutorial was seen or a day is finished; it opens Map.
  Both act only when the finger lifts over the button, and share one guard (`ClickPlay.MenuLaunchAccepted`): once
  either is accepted the other ignores taps until the scene has changed (review R3, 26 November 2026).
- **Map** (`Map/MapScreen.cs`) is built in code: one page per chapter, a day card, Play. Details: `systems/08`.
- **GameSceneOne** is the only gameplay scene; the day comes from the save (`SessionManager.GetCurrentLevelIndex`).
  A day has two phases (`Utils/GamePhaseManager.cs`): arrangement (drag things, the menu book; skipped on day 1 and in
  the tutorials) and play.
- **KitchenScene** has no EventSystem and no AudioListener of its own; `Kitchen/KitchenSceneManager.cs` loads it
  additively and switches off every enabled collider outside the `Kitchen` layer while it is open.
- **Shop** (`Shop/ShopManager.cs`) comes after every finished day; its Next opens the Map.

Every scene change goes through `SceneTransitionManager.TransitionToScene` (its own fade canvas, unscaled time).

## Objects that outlive a scene

| Object | Where it comes from | Holds |
| --- | --- | --- |
| `SessionManager` (`Session/SessionManager.cs`) | MainMenu scene; `SessionInitializer` creates one in MainMenu, GameSceneOne or Shop if it is missing | The save: coins, days done, stars, purchases, menu, positions. Also hosts `KitchenFlow`'s coroutines |
| `AudioManager` (`Utils/AudioManager.cs`) | Prefab instance in MainMenu; `LevelManager.EnsureAudioManagerExists` as a fallback | Music and sound effects, mute state |
| `SceneTransitionManager` | `TransitionSetup` in MainMenu | The fade and scene loading |
| `KitchenSceneManager` | GameSceneOne scene | Open/close of the kitchen |
| `[InterstitialAdService]` | Created on first use | The between-days ad; also runs `AdsInitializer`'s coroutines |
| `RemoveAdsStore` | Created by `RemoveAds.Init()`, first called from the Shop | Unity IAP listener |
| `KitchenTimerHelper` objects | Created per oven / fruit | Kitchen timers that survive the kitchen closing |
| `[UpdateGate]`, `CookiePress`, `KitchenSwipe` objects | `RuntimeInitializeOnLoadMethod` | Update card, button crunch, swipe listener |

State also lives in statics that no scene load clears: `KitchenClock`, `CooldownRegistry`, `OvenKitchenBase.Recipes`,
`KitchenFlow`, `AppleInKitchen.Count`, `CoconutInKitchen.Count`. `KitchenDay.End()` (`Kitchen/KitchenClock.cs`) resets
them; new kitchen state must be cleared there too.

## Systems, by script

| System | Scripts | Notes | Detail |
| --- | --- | --- | --- |
| Day | `LevelControl/LevelManager.cs`, `LevelData.cs` | Loads the day's `LevelData`, runs the day clock, pause, restart, the day-end card and its money count-up | `systems/02` |
| Phase | `Utils/GamePhaseManager.cs` | Arrangement, then play; both the table's and the kitchen's Ready are needed | `systems/02` |
| Orders | `ItemsAndOrders/OrderSystem.cs`, `Customer/PatienceMeter.cs` | Up to two open orders, a thinking pause, patience as flowers | `systems/02` |
| Customers | `Customer/CustomerManager.cs`, `CustomerController.cs`, `*Customer.cs` | One arrival per beat while the clock runs, two slots | `systems/02` |
| Food on the table | `ItemsAndOrders/ServeableItem.cs`, `DraggableFood.cs`, `Refill/*`, `Utils/TableLayer.cs`, `Utils/FixedStation.cs`, `Food/*` | Tap serves, drag only while arranging | `systems/03` |
| Kitchen | `Kitchen/*`, `Food/FoodInKitchen/*` | Ovens with a recipe picker, the choux and cake flow, fruit, blender, arranging, the kitchen clock | `systems/04` |
| Money and stars | `Scoring/ScoreManager.cs`, `StarRule.cs`, `StarProgressBar.cs`, `SimpleScorePopup.cs` | Sale + tip per order; stars from customers served | `systems/05` |
| Save | `Session/SessionManager.cs` (`SessionData`) | One JSON blob in PlayerPrefs | `systems/05` |
| Shop | `Shop/ShopManager.cs`, `ShopCookies.cs`, `ShopGlass.cs`, `ShopFlourish.cs` | Tabs Stars / Coins / $, cookie cards, the book | `systems/05` |
| Map and chapters | `Map/MapScreen.cs`, `Map/Chapter.cs` | Days per chapter, day 9, bonus days | `systems/08` |
| Ads, purchases, analytics | `Ads/*`, `Purchases/RemoveAds.cs`, `Analytics/AnalyticsLog.cs`, `DailyTreats/*` | | `systems/09` |
| Audio, UI helpers | `Utils/AudioManager.cs`, `Buttons/*`, `SceneTransition/*` | | `systems/06` |
| Tutorials | `Tutorial/TutorialDirector.cs`, `Utils/ArrangeLesson.cs`, `Utils/CandyGift.cs` | Added by `LevelManager.Start` when due | `systems/01` |

## Saving

`SessionManager.SaveSession` writes the whole `SessionData` as JSON to the PlayerPrefs key `FoodTruckSession` and calls
`PlayerPrefs.Save()` after every change. Other keys (settings, Remove Ads, Daily Treats, shop NEW tags, rating prompt,
high score) are listed in `systems/07`. Add fields to `SessionData`; never rename one (old saves would lose it).

## Where to change things

| Want to | Go to |
| --- | --- |
| Tune a day (length, beat, order size, patience) | `Assets/Entity/Level<N>Data.asset`; the scene's `LevelManager.allLevels` holds Level1..12 then Level17..32. Update the Mechanisms page |
| Change days per chapter or add a chapter | `Map/Chapter.cs` (`DaysPerChapter`, `Count`), the assets in `allLevels`, `Resources/Chapter<n>` |
| Change a sale price | The `ScoreManager` points table in GameSceneOne (an unlisted food pays 0) |
| Change the star rule | `Scoring/StarRule.cs` |
| Change bake or kitchen times | `OvenKitchenBase.BakeTimes`, `KitchenFlow.MixSeconds/CutSeconds`, `KitchenBlender.StepSeconds`, `IceCreamStation.WaffleSeconds`, `KitchenSpeed.Faster` |
| Add a shop card | Shop.unity (a `ShopItemPrefab` instance), or a runtime card in `ShopManager.Start` (`Ensure...Card`) |
| Change when the first ad shows | `MapScreen.FirstAdDay`; pacing in `InterstitialAdService` |
| Change the rewarded coin amount | `RewardedAdButton` in Shop.unity (`_coinsPerAd`, 50 in the scene and in code) |
| Add an analytics event | `AnalyticsLog` and the Unity dashboard's Event Manager (undefined events are dropped) |
| Version and build number | `Docs/ios-testing-and-release.md` |

## Conventions and gotchas

- Foods, characters, upgrades and shop cards are plain strings matched across scripts and scenes (`systems/07`).
- Input is EventSystem-based: a `PhysicsRaycaster` on the GameSceneOne camera and 3D `BoxCollider`s on the items.
  No `OnMouse*`; no 2D colliders on tappable things.
- Pause sets `Time.timeScale = 0` and `KitchenClock.Pause()`. Anything that must move while paused uses unscaled time;
  anything in the kitchen uses `KitchenClock`.
- Only three methods are called from scene files (Shop.unity buttons): `ShopManager.LoadNextGameLevel`,
  `ConfirmPurchase`, `CancelPurchase`. `ShopManager.Wire` adds no runtime listener to a button that has one.
- Most newer UI (map, cards, tabs, book, tutorials, gift cards) is built at runtime; look for the builder class.
- `Utils/LogControl.cs` filters `Debug.Log` to warnings and errors outside the editor and development builds.
- `Utils/FrameRate.cs` sets 60 fps.
- Compile check without Unity: the csc command in `CLAUDE.md`.
