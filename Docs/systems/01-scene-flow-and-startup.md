# 01 Scene flow and startup

Last verified against the code: 26 November 2026. Paths are under `Assets/Scripts`.
(The October 2026 deep note with startup timelines is in `Docs/history/systems_2026-10/`; it describes four scenes
and the old flow.)

## Scenes

Build order (`ProjectSettings/EditorBuildSettings.asset`): MainMenu, GameSceneOne, Shop, KitchenScene, Map.

| From | Action | Code | To |
| --- | --- | --- | --- |
| MainMenu | Play | `Buttons/ClickPlay.cs` `WaitForDelay`: `StartNewSession()`, then GameSceneOne when `TutorialDirector.IsDue` (true for a new session), else its `_sceneName` (Map in the scene) | GameSceneOne (tutorial) |
| MainMenu | Continue | `Buttons/ClickContinue.cs`: `ContinueSession()`, `_sceneName` (Map in the scene) | Map |
| GameSceneOne | Tutorial's Let's go | `TutorialDirector` (chapter 1): coins restored, `MarkTutorialSeen`, `KitchenDay.End()` | Map |
| Map | Play on a day card | `MapScreen.PlayDay`: `SetCurrentLevel(day)`, the ad from day index 2 on (`FirstAdDay`) | GameSceneOne |
| Map | Home | `MapScreen.GoHome` | MainMenu |
| GameSceneOne | Day-end card, Next | `LevelManager.LoadNextLevel` | Shop |
| GameSceneOne | Day-end card or pause, Restart | `LevelManager.RestartLevel` (coins back to the day-start snapshot, the three-star bonus kept) | GameSceneOne |
| GameSceneOne | Pause, Home | `LevelManager.GoToMap` | Map |
| Shop | Next | `ShopManager.LoadNextGameLevel` (called from the scene's button; refused while the purchase popup is open) | Map |
| GameSceneOne | Pull handle, swipe right, kitchen button | `KitchenSceneManager.OpenKitchen` (through `Utils/PullHandle.cs`, `Kitchen/KitchenSwipe.cs`, `Kitchen/OpenKitchenButton.cs`) | KitchenScene, additive |

`LevelManager.GoToMainMenu` (wired to the day-end card's main-menu button field) and `ShopManager.OnMainMenuClicked`
also exist and open MainMenu.

All loads go through `SceneTransition/SceneTransitionManager.cs` `TransitionToScene`: a fade on its own canvas
(sorting order 9999) in unscaled time; a second request while one is running is ignored.

## Startup

- Before the first scene (`RuntimeInitializeOnLoadMethod`, BeforeSceneLoad): `Utils/LogControl.cs` (log filter),
  `Utils/FrameRate.cs` (60 fps), `Analytics/AnalyticsLog.cs` (starts Unity Services and analytics).
- After the first scene: `Utils/UpdateGate.cs` (iPhone only: asks Apple's lookup once per launch; shows the UPDATE
  card on MainMenu, Map or Shop, never over a day), `Utils/RatingPrompt.cs` (listens for the Shop scene),
  `Utils/Chapter2TestButton.cs` (editor, development and TestFlight builds only), `Buttons/CookiePress.cs`,
  `Kitchen/KitchenSwipe.cs`.
- MainMenu: `SessionManager.Awake` loads the save (`FoodTruckSession`); `AdsInitializer.Awake` asks for tracking
  permission and starts Unity Ads (`systems/09`); `AudioManager` starts menu music on every single-mode load of
  MainMenu when `autoStartMainMenuMusic` is set; `DailyTreats` shows its card (it needs a session that has seen the
  tutorial or finished a day).
- GameSceneOne, `LevelManager.Start`: registers with `SessionManager`, `LoadLevel(saved day)`, sets up the buttons,
  `CandyJar.Ensure()`, then adds at most one tutorial: `TutorialDirector.Begin` when day index 0 and `IsDue`
  (session active, `tutorialSeen` false, no day finished), else `Begin2` when `IsDue2` (chapter 2, no chapter 2 day
  finished, `tutorial2Seen` false). On other days it adds `MenuBook`. It always adds `DayNightTint`.
- `LoadLevel` → `ApplyLevelSettings` (order settings from `LevelData`, which foods are on the table, hen board,
  customers) → `StartLevel` (`SnapshotScoreBeforeLevel`, `GamePhaseManager.StartArrangementPhase`).

## Pause and time scale

`LevelManager.PauseGame` sets `Time.timeScale = 0`, calls `KitchenClock.Pause()` and stops the music; it is refused
once the day is ending. `ResumeGame` reverses it. Every exit from the day (`RestartLevel`, `LoadNextLevel`, `GoToMap`,
`GoToMainMenu`) sets the time scale to 1, resumes the kitchen clock and calls `KitchenDay.End()`. `MapScreen.Start`
also sets the time scale to 1.

## Tutorials

- Chapter 1 ("day 0"): `Tutorial/TutorialDirector.cs` `Begin`. Ends on the Map; `tutorialSeen` is saved.
- Chapter 2's first day: `Begin2` (one customer at a time: `maxConcurrentOrders = 1`). At its end the coins of the demo
  are restored, `tutorial2Seen` is saved and the scene reloads into the real day.
- The first arrangement (day 2): the hint card and moving hand (`GamePhaseManager.HintDue`, `Utils/ArrangeLesson.cs`),
  saved as `arrangeTutorialSeen`.
- The first menu (day 2, when the menu book first opens): the banner "You can remove or add items to today's menu" under
  the paper and a hand that peels one sticker off to the strip and puts it back (pictures only, the save is untouched;
  `MenuBook.Lesson`). Due by `SessionManager.MenuTutorialDue`: `menuTutorialSeen` false, at most day 1 finished and the
  menu never changed, so players who already used the menu skip it. A touch after the demo, Done or Ready removes the banner.
- Every tutorial banner (TutorialDirector's caption, the menu lesson, the arrangement hint on the table and in the
  kitchen) is the taped note (`Tutorial/TapedNote.cs`, owner 27 November 2026): `Resources/Tutorial/note_paper`
  (9-sliced, 200 ppu) with `note_tape` centred on top, Delius bold. It slides in on a tilt and settles with a little
  flutter (about 0.7 s) when it newly shows; new words on a note already up just change.
- Day 3: the candy jar's gift when play begins (`Utils/CandyGift.cs`, `candyGiftSeen`).
- While a `TutorialDirector` exists: no `dayStarted` analytics, no day clock (`dayClockDisabled`), the customer is
  the Girl (`CustomerManager.SelectCustomerForLevel`).
