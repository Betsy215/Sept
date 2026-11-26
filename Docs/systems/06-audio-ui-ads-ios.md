# 06 Audio, UI helpers, scene fades, iOS settings

Last verified against the code: 26 November 2026. Paths are under `Assets/Scripts`.
Ads moved to `09-ads-analytics-purchases.md`; building and releasing is `Docs/ios-testing-and-release.md`.
(The October 2026 deep note is in `Docs/history/systems_2026-10/`. Its audio clip tables and canvas tables were written
for `AudioManager` and `SceneTransitionManager` code that is unchanged since, but were not re-verified; its ads, Xcode
and version sections are out of date.)

## Audio (`Utils/AudioManager.cs`)

- A persistent singleton with a music source and an SFX source (created in `InitializeAudioManager` when not
  assigned). Clip fields: `mainMenuMusic`, `arrangementPhaseMusic`, `gameplayPlaylist`, `levelCompleteMusic`,
  `shopMusic`, and the SFX clips (`buttonClickSFX`, `orderCompleteSFX`, `itemPickupSFX`, `levelWinSFX`, `wrongItemSFX`,
  `customerWalkInSFX`, the order and money sounds).
- Who starts music: `AudioManager.OnSceneLoaded` (menu music on a single-mode load of `mainMenuSceneName` when
  `autoStartMainMenuMusic`), `LevelManager` (`PlayArrangementMusic`, `PlayGameplayMusic`, `PlayLevelCompleteMusic`;
  pause stops the music and resume restarts the phase's music), `ShopManager.Start` (`PlayShopMusic`). Scene exits
  call `StopMusic()`.
- One-shot sounds go through `PlaySFX(clip)`; newer features load their clips by name from `Resources/Audio`,
  `Resources/Kitchen` and `Resources/UI`.
- The sound and music switches are saved by `Buttons/SettingsButtonsController.cs` in PlayerPrefs `GlobalAudioState`
  and `GlobalMusicState` (1 = on) and applied through `SetSFXEnabled` / `SetMusicEnabled`.
- Known open items (played twice, unassigned clips, silent menu) are listed in `Docs/TODO.md`.

## UI

- Scene canvases use a 1080 × 1920 reference. Most newer UI is built at runtime by its own class: `MapScreen` (map,
  day card, gift cards), `ShopManager` + `ShopCookies` + `ShopGlass` (tabs, cookie cards, the book), `MenuBook`,
  `TutorialDirector`, `DailyTreats`, `MoneyFlipClock` (the hen board), `StarCelebration`, `UpdateGate`, `CandyGift`.
  Look for the builder class before searching a scene file.
- Player-facing text is hard-coded English in those builders; there is no localisation layer.
- Fonts are loaded by name from `Resources/Fonts & Materials`: `beachday SDF` (map, shop EARNED line) and
  `Delius SDF` (`ShopManager.HandFontPath`, the hand-written panels).
- Buttons: `Buttons/CookiePress.cs` gives the cookie buttons their press and crunch; `ClickPlay`, `ClickContinue`,
  `PauseButton`, `SettingButton` are the older scene buttons. `Utils/Haptics.cs` (with `Assets/Plugins/iOS/Haptics.mm`)
  does the taps on a serve.
- `Utils/SafeAreaFitter.cs` exists for safe-area layouts; `Utils/TableLayer.cs` handles the table's own safe area.

## Scene fades (`SceneTransition/SceneTransitionManager.cs`)

`TransitionToScene(name)`: fade out, load, fade in, on its own overlay canvas (sorting order 9999), timed with
unscaled time so it works while paused. A request while a transition is running is ignored; an empty name is
refused; when the manager is inactive the scene loads without a fade. `IsTransitioning()` lets other code wait
(the map's bonus-day reveal does). Created by `SceneTransition/TransitionSetup.cs` in MainMenu.

## Logging and frame rate

`Utils/LogControl.cs`: outside the editor and development builds only warnings and errors are logged.
`Utils/FrameRate.cs`: `targetFrameRate` 60, vSync off.

## iOS project settings that code depends on

- `Assets/Editor/IOSPostBuild.cs` writes `NSUserTrackingUsageDescription` and `ITSAppUsesNonExemptEncryption = false`
  into Info.plist on every export. `Assets/Editor/CommandLineBuild.cs` is the batchmode export entry.
- `Assets/Plugins/iOS/TestFlightCheck.mm` backs `AdsInitializer.IsTestFlightBuild()` (test ads, the test button, the
  `development` analytics environment, no between-days ad).
- Notifications: `ProjectSettings/NotificationsSettings.asset` has
  `UnityNotificationRequestAuthorizationOnAppLaunch` = `False` (since 26 November 2026; this is the file the package
  reads, not the copy under `Assets/Editor/com.unity.mobile.notifications/`). The game sends no notifications.
- The version (`bundleVersion`) and build number are set through the recipes in the release guide, never by hand
  here.
