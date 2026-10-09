# 06 Audio, UI, scene transitions, ads, iOS build

Maintenance notes for the systems that sit around gameplay. Everything here was checked against the code, the scene and prefab YAML, the Unity package cache and the March 2026 Xcode export (`Desktop/Sept/Builds/iOS-26.03.29`) on 7 October 2026. Line numbers are for the files as of that date. Nothing was changed while writing this.

Rule above everything: the game is live and works. Items marked "bug" or "gotcha" are written up here so they can be decided on, not so they get fixed in passing.

---

## 1. Audio

### 1.1 Objects

`AudioManager` (`Assets/Scripts/Utils/AudioManager.cs`) is a `DontDestroyOnLoad` singleton (`Awake`, lines 52-67). The real instance is the prefab instance in **MainMenu.unity** (PrefabInstance at line 1442, overrides at lines 1450-1559). A second copy can be created by `LevelManager.EnsureAudioManagerExists` (`LevelManager.cs:106-133`) from `audioManagerPrefab` (`GameSceneOne.unity:3326`), but only when GameSceneOne is played without MainMenu (Editor only in practice).

Two `AudioSource`s are created at runtime as children, because the prefab leaves `musicSource` and `sfxSource` empty (`AudioManager.prefab:47-48`): music source `loop = true`, volume `musicVolume`; SFX source volume `sfxVolume` (`AudioManager.cs:90-117`). There is no AudioMixer.

### 1.2 Clip fields: prefab vs MainMenu override

`(missing)` means the field points at GUID `32d4b1811dda14a6cb9e059ee8ad17d2`, which no asset in the project has; Unity treats it as None at runtime.

| Field (`AudioManager.cs` line) | Prefab (`AudioManager.prefab` line) | MainMenu override (`MainMenu.unity` line) | Effective in game |
| --- | --- | --- | --- |
| `mainMenuMusic` (12) | `Audio/3-20. Inn.mp3` (49) | **None** (1463) | None. Owner's choice: silent menu. |
| `arrangementPhaseMusic` (13) | not serialized, None | `Audio/birds-nature-relax-sounds-110839.mp3` (1487) | birds |
| `gameplayPlaylist` (14) | not serialized, default `new AudioClip[2]` of nulls | size 3 (1503): `cute-music-26476.mp3`, `piano-country-music-61994.mp3`, `Sounds/edited-bird-music.wav` (1507-1515) | 3 tracks |
| `levelCompleteMusic` (15) | unknown GUID `0c008ee…`, None (51) | **None** (1479) | None |
| `shopMusic` (16) | not serialized | `ukulele-ukulele-cute-joyful-music-274858.mp3` (1451) | ukulele |
| `buttonClickSFX` (18) | (missing) (52) | none | None |
| `orderCompleteSFX` (19) | (missing) (53) | `cha-ching-7053.mp3` (1471) | cha-ching |
| `itemPickupSFX` (20) | (missing) (54) | none | None |
| `levelWinSFX` (21) | None (55) | none | None |
| `wrongItemSFX` (22) | not serialized | none | None |
| `customerWalkInSFX` (23) | not serialized | `door_EDITED.wav` (1475) | door |
| `defaultPerfectOrderSound` (26) | not serialized | `cha-ching-7053.mp3` (1499) | cha-ching |
| `defaultOrderDoneSound` (28) | not serialized | `cash.mp3` (1491) | cash |
| `moneyCountSound` (39) | not serialized | `coin.mp3` (1467) | coin |
| `moneyCompleteSound` (40) | not serialized | `cha-ching-7053.mp3` (1483) | cha-ching |
| `musicVolume` (31) | 0.7 (56) | **0.4** (1455) | 0.4 |
| `sfxVolume` (33) | 0.8 (57) | none | 0.8 |
| `autoStartMainMenuMusic` (35) | 1 (58) | 1 (1495) | on, but the clip is None |
| `gameplayMusic` | prefab line 50, override line 1459 | | Stale field. It no longer exists in the script and is ignored. |

Consequences:

- **GameSceneOne played directly in the Editor is not silent.** The fallback `AudioManager` comes from the prefab, so arrangement falls back to `PlayMainMenuMusic` (`AudioManager.cs:158-163`) and the playlist falls back the same way (`185-188`). The prefab's `mainMenuMusic` is Inn.mp3, so you hear Inn in both phases. `Docs/TODO.md` says "plays no music"; that is not quite right.
- "Apply All" on the MainMenu instance (suggested in TODO.md) would also write `mainMenuMusic = None` and `levelCompleteMusic = None` into the prefab. That is fine, but know that it happens.

### 1.3 Music state machine

State is `currentMusicType` (`AudioManager.cs:50`) plus `isPlaylistActive` and `playlistCoroutine` (47-49).

| Method | Lines | Sets type | Behaviour |
| --- | --- | --- | --- |
| `PlayMainMenuMusic` | 254-259 | `mainmenu` | Stops the playlist, then plays `mainMenuMusic`. With None it logs and returns **without stopping the source**, so whatever was playing carries on. |
| `PlayArrangementMusic` | 149-164 | `arrangement` | Loops the birds track; falls back to main menu music if unassigned. |
| `PlayGameplayPlaylist` / `PlayGameplayMusic` | 172-199, 261-264 | `gameplay` | Needs `[0]` and `[1]` non-null; picks a random start index across all 3; `PlaylistCoroutine` (201-224) plays each track with `loop=false` and waits `WaitForSeconds(track.length)` (scaled time), cycling `% Length`. |
| `PlayShopMusic` | 266-271 | `shop` | Loops ukulele. |
| `PlayLevelCompleteMusic` | 273-278 | `levelcomplete` | Clip is None, so this **only stops the playlist coroutine**. The gameplay track that is already playing runs to its end (loop is false), then the game goes quiet until the player leaves the popup. |
| `StopMusic` | 241-251 | `""` | Stops the playlist and the source. |
| `SetMusicEnabled(false)` | 323-336 | keeps type | Stops the playlist and the source; ignores no-op calls (326). |
| `SetMusicEnabled(true)` | 337-364 | | Replays according to the type. With `""` it falls into `default` (356-361) and calls `musicSource.Play()` on **whatever clip was last loaded**. See bug B4. |

The playlist waits in scaled time, but the game only sets `timeScale = 0` in `PauseGame`, and that also calls `StopMusic`, so the wait cannot drift. Backgrounding the app pauses Unity's clock and the audio together.

### 1.4 Call sites

Music:

| Caller | File:line | Call |
| --- | --- | --- |
| AudioManager on MainMenu load | `AudioManager.cs:75-88` | `PlayMainMenuMusic` (Single loads only; additive Kitchen ignored) |
| Play button | `ClickPlay.cs:137` | `StopMusic` before the transition |
| Continue button | `ClickContinue.cs:112` | `StopMusic` |
| Level start | `LevelManager.cs:490-502` | `PlayArrangementMusic` |
| Gameplay start | `LevelManager.cs:504-512` | `PlayGameplayMusic` |
| Pause | `LevelManager.cs:263-264` | `StopMusic` |
| Resume | `LevelManager.cs:283-289` | Gameplay or arrangement music again, from the start of a track |
| Restart | `LevelManager.cs:328-329` | `StopMusic` |
| Level complete popup, after 3 s | `LevelManager.cs:562-563` | `PlayLevelCompleteMusic` (stops the playlist only) |
| Next Level | `LevelManager.cs:663-664` | `StopMusic`, then the interstitial |
| Main Menu (pause or end) | `LevelManager.cs:688-689` | `StopMusic` |
| Shop start | `ShopManager.cs:44-45` | `PlayShopMusic` |
| Shop Next Level | `ShopManager.cs:330` | `PlayGameplayMusic`, no null check; GameSceneOne replaces it with arrangement music about 0.5 s later. See B6. |
| Shop Main Menu | `ShopManager.cs:336` | `StopMusic` |
| Settings | `SettingsButtonsController.cs:102, 166` | `SetMusicEnabled` |

SFX, all through `PlaySFX` (`AudioManager.cs:135-146`) on the shared SFX source unless noted:

| Caller | File:line | Clip | Plays? |
| --- | --- | --- | --- |
| `PlayButtonClick` from settings toggles | `SettingsButtonsController.cs:136, 149, 171` | `buttonClickSFX` | No, (missing) |
| `PlayOrderComplete` (volume scale `sfxVolume*0.7`, line 289) | `OrderSystem.cs:568`, `ScoreManager.cs:356` fallback | cha-ching | Yes |
| `PlayItemPickup` | `OrderSystem.cs:563`, `ScoreManager.cs:349` fallbacks | `itemPickupSFX` | No, (missing) |
| `PlayWrongItemSFX` | `ServeableItem.cs:257-258` when `useAudioManager` is true | `wrongItemSFX` | No, None |
| `PlayLevelWin` | `LevelManager.cs:535-536` | `levelWinSFX` | No, None |
| `PlayCustomerWalkIn` | `CustomerManager.cs:335-336` | door | Yes |
| `PlayMoneyCount` | `LevelManager.cs:779-780` (once a second while counting) | coin | Yes |
| `PlayMoneyTransferComplete` | `LevelManager.cs:796-797` | cha-ching | Yes |
| `PlayPurchaseSound` | `ShopManager.cs:188-189` | `orderCompleteSFX` | Yes |
| Order done / perfect | `CustomerController.cs:77-83` | per-customer clip, else defaults | Yes |
| Item serve | `ServeableItem.cs:212-213` | `serveSound` (set on one item, `GameSceneOne.unity:3001`) | That item only |
| Kitchen | `JuiceInKitchen.cs:30-32`, `CoffeeInKitchen.cs:29-31`, `OvenKitchenBase.cs:83-84, 97-98`, `KitchenItemWithTimer.cs:71-72`, `OpenKitchenButton.cs:46-47`, `CloseKitchenButton.cs:38-39` | scene clips | Yes where assigned |
| Rewarded ad | `RewardedAdButton.cs:178-179` | cash | Yes |

Sounds that do **not** go through AudioManager, so they ignore the Sound toggle:

- `ClickPlay` (`ClickPlay.cs:36, 43`) and `ClickContinue` (`ClickContinue.cs:71, 82`) play on their own AudioSources (`MainMenu.unity:302` and `:838`, volume 1). See B2.
- `OrderSystem.audioSource` and `ScoreManager.audioSource` exist but are unassigned in the scene (`GameSceneOne.unity:6418`, `:4628`), so their sounds fall back to AudioManager.
- `PauseButton` has no AudioSource and no clips (`GameSceneOne.unity:6052`), so it is silent (owner: leave).

### 1.5 Settings persistence and delayed apply

- Keys `GlobalAudioState` and `GlobalMusicState` (`SettingsButtonsController.cs:31-32`), default 1. They are read in `Start` (74-81) and written with `PlayerPrefs.Save()` on every toggle (83-91).
- `ApplySettingsAfterDelay` (45-50) waits one frame, then pushes both values into AudioManager (93-104), so AudioManager has already run `Start` by then.
- The Settings prefab is placed only in **MainMenu** (`MainMenu.unity:1310`) and **GameSceneOne** (`GameSceneOne.unity:5366`). Shop and Kitchen have none; they rely on the persistent AudioManager keeping its flags. Across scenes this is consistent: AudioManager outlives every scene, and each scene with a Settings panel re-applies the same saved values (no-ops by design, `AudioManager.cs:326`).
- The SFX flag only gates `PlaySFX` and `PlayOrderComplete`. Nothing uses `AudioListener.volume` or `AudioListener.pause`.
- The buttons are wired at runtime (`SettingsButtonsController.cs:108-127`); the prefab buttons have no persistent `onClick` calls, so nothing fires twice (matches `CLAUDE.md`: `ToggleAudio`/`ToggleMusic` are wired from code).

### 1.6 Volumes

Music source 0.4. SFX source 0.8; `PlayOneShot` multiplies by the source volume, so most SFX play at 0.8 and order-complete at 0.8 x 0.56 = 0.45. Menu button clicks play at 1.0 on their own sources.

Player Settings `muteOtherAudioSources: 0` (`ProjectSettings.asset:84`) gives the Ambient audio session: **the iPhone ringer/silent switch mutes the game**, and other apps' music keeps playing under it. If a player reports "no sound", check the switch first.

### 1.7 Known silent spots

- Main menu: no music (owner's choice, leave alone).
- Level complete popup: the last gameplay track finishes, then silence (`levelCompleteMusic` None).
- Level win, item pickup, wrong item and settings click SFX: clips are None or missing (the owner accepts that not every clip is wired).
- While paused: music is stopped on purpose (`LevelManager.cs:263-264`).

### 1.8 Ads, backgrounding and audio

- Unity Ads pauses the whole player while an ad is on screen: `UnityPause(1)` on native show start and `UnityPause(0)` on complete or failure (`Library/PackageCache/com.unity.ads@4.17.0/Plugins/iOS/UnityAdsShowListener.mm:22-46`). Unity audio pauses and resumes with it, so Shop music continues after a rewarded ad.
- The interstitial runs after `StopMusic` (`LevelManager.cs:663`), so there is nothing to resume; the Shop then starts its own music (`ShopManager.cs:44-45`).
- On backgrounding, `UnityAppController.mm:534-550` in the export pauses Unity, and `applicationDidBecomeActive` (446-462) resumes it and re-routes audio after 0.1 s. When an ad had already paused Unity, `_wasPausedExternal` keeps it paused until the ad finishes, which is correct. In that case `OnApplicationPause` is **not** delivered, so `SessionManager` (`SessionManager.cs:567-575`) does not save; nothing is lost, because level completion already saved (`SessionManager.cs:364-366`) and rewarded coins save on credit (`SessionManager.cs:324-332`).
- AudioManager has no `OnApplicationPause`/`AudioSettings` code and needs none for the cases above. Phone-call or Siri interruptions are handled by Unity's FMOD layer; test them on a device (release checklist item).

---

## 2. UI

### 2.1 Canvases and cameras per scene

All CanvasScalers: Scale With Screen Size, 1080 x 1920, match 0.5.

| Scene | Object (line) | Mode | Sort | Notes |
| --- | --- | --- | --- | --- |
| MainMenu | Main Camera (477) | ortho 5, depth -1 | | |
| MainMenu | Main Camera/Canvas (673) | Overlay | 0 | Child of the camera but Overlay. Play, Continue, scrolling bg, Settings. Holds the disabled missing-script component (leave). |
| GameSceneOne | Camera (673) | ortho 5, depth 0 | | Has the PhysicsRaycaster (see maintenance.md). |
| GameSceneOne | Canvas (6129) | Screen Space Camera, Camera | 0 | HUD: pause button, Kitchen button, Settings. |
| GameSceneOne | ArrangementUI (601) | Overlay | 0 | |
| GameSceneOne | PopupCanvas (5103) | Overlay | 1, sorting layer **Popup** (ID -262186767 = 4032780529) | Pause panel, LevelCompletePanel (nested Canvas 3937 with its own GraphicRaycaster, blocking All). |
| Shop | Main Camera (1415) | ortho 5, depth -1 | | |
| Shop | Main Camera/Canvas (3898) | Screen Space Camera | 0 | Everything, including Get5Coins. |
| KitchenScene (additive) | Kitchen Camera (1011) | ortho 5, depth 1, clear depth only, culls layer 6 | | No EventSystem and no AudioListener on purpose. |
| KitchenScene | Canvas (840) | Screen Space Camera, Kitchen Camera | 1, layer KitchenBG | |
| Runtime | `TransitionCanvas` (`SceneTransitionManager.cs:70-106`) | Overlay | 9999, Default layer | Created under the persistent manager. |

### 2.2 Popup blocking

- Pause: `PauseGame` (`LevelManager.cs:241-267`) refuses while the level-complete panel is up (248-252), sets `timeScale = 0`, and shows `PopupCanvas` and the pause panel. The panel does not cover the whole screen; gameplay taps are blocked in code by `Time.timeScale == 0f` checks (`ServeableItem.cs:127`, `DraggableFood.cs:418`, `CoffeeMachine.cs:75`, the kitchen scripts, `KitchenSceneManager.cs:41`).
- Level complete: the full panel on `PopupCanvas` (sort 1) sits above the camera-space HUD. Its buttons are wired at runtime (`LevelManager.cs:153-189`); Next Level has no persistent call, so it fires once per tap, but it has no re-entry guard (B5).
- Shop: `ShopManager.IsPurchasePopupOpen` (`ShopManager.cs:85`) blocks Next Level (329) and the rewarded button (`RewardedAdButton.cs:129`).
- Transition: the fade image turns `raycastTarget` on during a transition (`SceneTransitionManager.cs:147-150`); at sort 9999 it wins over every scene raycaster.

### 2.3 Button scripts (`Assets/Scripts/Buttons`)

| Script | Where | What it does |
| --- | --- | --- |
| `ClickPlay` | MainMenu `Canvas/play` (`MainMenu.unity:283`) | Press and release sprites and clicks; then `WaitForSeconds(2)` (**scaled**), `StartNewSession`, `StopMusic`, transition to `GameSceneOne`. `launching` blocks repeats. One tap starts over even with saved progress: the two-tap confirm notice was removed on 9 October at the owner's request. |
| `ClickContinue` | MainMenu `ContinueButton` (972) | Interactable only if `levelsCompleted >= 1` (29-61, re-checked 0.1 s after `OnEnable`); release waits 2 s scaled (90), then `ContinueSession` or `StartNewSession`, `StopMusic`, transition. Its compress clip is (missing). |
| `PauseButton` | GameSceneOne `Canvas/PauseButton` (6052) | Darken plus Z press; release calls `TogglePause` after 0.1 s realtime (156-174); slide-off cancels (140-154). Silent: no source. |
| `SettingButton` | Added component on the Settings prefab's gear in both scenes (MainMenu 1295, GameSceneOne 5573) | Darkens on press only. |
| `SettingsButtonsController` | Settings prefab root (`Settings.prefab:64`) | Toggles and persistence, section 1.5. |

The two 2 s delays use scaled time. Every path into MainMenu sets `timeScale = 1` first (`LevelManager.cs:692`, Shop never pauses), so this is safe today, but a future path that reaches MainMenu while paused would make Play and Continue hang.

### 2.4 Wobble and rhythm

- `BalatroWobble` (`Utils/BalatroWobble.cs`): every frame writes `anchoredPosition`, `localEulerAngles` and `localScale` around the values captured in `Awake` (40-51, 53-87), in unscaled time. It stops and resets while the Button is not interactable (55-65) and on disable (90-93). Used on GameSceneOne `Canvas/KitchenButton` (523), Kitchen `backButton` (1702), and Shop `Get5Coins` (4334).
- `BalatroRhythm` (`Utils/Balatrorhythm.cs`, class name `BalatroRhythm`): heartbeat scale pop at `_bpm` plus press sink, unscaled. Only on Shop `Get5Coins` (4353, bpm 60).
- Gotcha: Get5Coins has both, and both write `localScale` (`BalatroWobble.cs:86`, `Balatrorhythm.cs:101`). Whichever `Update` runs last wins each frame, so the beat and press sink are partly overwritten. Cosmetic.
- Gotcha: wobble captures its rest position in `Awake`. Anything else that moves the RectTransform later (a layout group, or re-parenting under a `SafeAreaFitter` panel) is overwritten every frame. Re-parent in the Editor, not at runtime.

### 2.5 Settings animator

- Settings prefab: Animator with `Animation/Controllers/Settings.controller`, **Update Mode Normal** (`Settings.prefab:58`, `m_UpdateMode: 0`). Not overridden in either scene.
- The gear button's persistent `onClick` is `Animator.SetTrigger("Show")` (`Settings.prefab:443-453`; GameSceneOne re-targets it to its own stripped Animator, `GameSceneOne.unity:5398-5410`, `5563`).
- States: `Start` (no motion) to `setting` on Show, to `hidesetting` (same clip, speed -1) on Show, back to `Start` on exit time. The clip `Animation/Clips/setting.anim` slides the `audio` and `music` buttons out from x = -500 over 0.58 s.
- Because the Animator uses scaled time, the panel cannot open or close while the game is paused; the trigger stays set and plays on resume (B3).

### 2.6 Safe area

- `SafeAreaFitter` (`Utils/SafeAreaFitter.cs`) is **not used in any scene or prefab** (GUID `ba5bd907…` appears nowhere). It anchors its RectTransform to `Screen.safeArea` and re-applies when the safe area or the resolution changes (33-61).
- The tooltips at lines 12 and 15 say "Ignore the top/bottom inset", but `true` means the inset is **applied**. The code is right; the wording is backwards.
- The only live safe-area code is `TableLayer.cs:195-234` (drag bounds).
- Buttons under the notch and home indicator: owner said leave for now. The recipe is in `Docs/TODO.md`.

---

## 3. Scene transitions

`SceneTransitionManager` (`Assets/Scripts/SceneTransition/SceneTransitionManager.cs`) is a persistent singleton, created by `TransitionSetup` (`TransitionSetup.cs:12-32`), which lives only in **MainMenu** (`MainMenu.unity:1076`, duration 1, black).

- **Timing:** `transitionDuration` 1 s is split into a 0.5 s fade out (186-202), a synchronous `SceneManager.LoadScene` (157), and a 0.5 s fade in (204-220).
- **Unscaled time:** both fades use `Time.unscaledDeltaTime` (195, 213), so they run while paused. Side effect: the first frame after the synchronous load carries the whole load time in its unscaled delta, so on a slow load the fade in can finish in one frame (the scene pops in). Cosmetic.
- **Blocking:** the canvas is activated and `raycastTarget` turned on for the whole transition (141-150).
- **Guards:** a call during a transition is ignored with a warning (110-114); an empty name is rejected (116-120); if the manager is inactive it loads without a fade (122-128).
- **Abort and reset:** the `finally` block (164-183) always clears alpha, raycast, canvas and flag, even when the coroutine is stopped. `HideTransition` (239-261) stops a running transition and resets. `OnDestroy` clears the static (64-68).
- `TransitionSetup` sets `fadeColor` after `AddComponent`, so after `Awake` already built the image (`TransitionSetup.cs:21-25`). Harmless while the colour is black.
- `ClickPlay.cs:139`, `ClickContinue.cs:114`, `LevelManager.cs:331, 695` and `ShopManager.cs:331, 337` call `Instance` without a null check. In normal play it always exists (MainMenu is first in the build list, `EditorBuildSettings.asset:8-19`). Pressing Play from a scene other than MainMenu breaks these; see CLAUDE.md.
- Unverified gotcha: `PopupCanvas` is on the **Popup** sorting layer while the fade canvas is on Default. If Unity orders Overlay canvases by sorting layer before sort order, the level-complete or pause popup stays visible above the black during the fade out. Check on a device when leaving a level.

---

## 4. Ads

Packages: `com.unity.ads` 4.17.0 and `com.unity.ads.ios-support` 1.0.1 (`Packages/manifest.json`). The Unity Services "Ads" toggle is **off** (`UnityConnectSettings.asset`, `UnityAdsSettings.m_Enabled: 0`); that is correct, because the package is initialised from code. Turning the service toggle on would add a second, legacy initialisation path.

### 4.1 AdsInitializer (MainMenu `AdManager`, `MainMenu.unity:740`)

- Game ID `6074412`, `_testMode: 0` in the scene (the serialized values win over the code defaults at `AdsInitializer.cs:10-11`).
- `Awake` (13-21): if ATT status is `NOT_DETERMINED`, call `RequestAuthorizationTracking()`, then **immediately** call `InitializeAds()` (23-27). The ATT request is fire-and-forget (ios-support 1.0.1 has no callback), so Unity Ads initialises and the first interstitial preloads before the player answers.
- `OnInitializationComplete` (29-35) calls `InterstitialAdService.Instance.Preload()`. A failure only logs (37-40); there is no retry here, but every return to MainMenu runs `Awake` again and retries if not initialised.
- Native side: `TrackingAuthorizationManager.m` (`Libraries/com.unity.ads.ios-support/...` in the export) does nothing unless `NSUserTrackingUsageDescription` is in Info.plist (`isAvailable`). That is why the March build never showed a prompt and did not crash.
- **Timing risk:** in a release build `startUnity` runs synchronously inside `didFinishLaunching` (`UnityAppController.mm:353, 415`), so the first scene's `Awake` may run before the app is active. iOS only shows the ATT prompt to an active app. See bug B1. Because `Awake` re-runs whenever MainMenu reloads, the prompt would then first appear when the player returns to the menu.

### 4.2 RewardedAdButton (Shop `Get5Coins`, `Shop.unity:4315`)

- Unit `Rewarded_iOS`, 5 coins, reward SFX `cash.mp3`; UI references are auto-found (39-52).
- Waits up to 15 s realtime for initialisation (73-93), then loads. A failed load retries with backoff from 3 s doubling to 30 s (104-120).
- Tap (126-133): ignored if not ready or the purchase popup is open; disables itself and shows the ad.
- Complete (135-150): grants only on `COMPLETED` via `SessionManager.AddScoreImmediately` (`SessionManager.cs:324-332`, saves at once). Skip or fail updates the label. Both reload.
- The button is a per-scene object. If the Shop is left before callbacks arrive, the Unity-null checks protect the UI, but `LoadAd()` then runs `StartCoroutine` on a destroyed component and logs an exception. Harmless.

### 4.3 InterstitialAdService (runtime singleton, `Assets/Scripts/Ads/InterstitialAdService.cs`)

- Created on first `Instance` access as a `DontDestroyOnLoad` GameObject `[InterstitialAdService]` (34-47). Unit `Interstitial_iOS` (21).
- **Preload** (59-84): waits up to 20 s realtime for init, then `Advertisement.Load`. A failed load retries every 10 s, forever (94-107).
- **Due and spacing** (117-138): `levelsSinceLastAd++` on every call. It shows only if due (`>= ShowEveryNLevels`, default 1), spaced (`MinSecondsBetweenAds` 45 s of `realtimeSinceStartup`), ready, and no ad is already pending. Otherwise it runs the continuation at once (and preloads if due but not ready).
- **Continuation:** from `LevelManager.LoadNextLevel` (`LevelManager.cs:657-672`). It sets `timeScale = 1`, `StopMusic`, then continues to `TransitionToScene("Shop")`. `Finish` (175-189) runs it exactly once (it nulls `pendingContinuation` first) and queues the next load.
- **Watchdog** (140-145): 12 s realtime. What happens on a device: native show start calls `UnityPause(1)` before the C# `OnUnityAdsShowStart` is delivered (C# callbacks are posted to the main-thread dispatcher, `Runtime/Advertisement/Dispatchers/UnityAdsShowListenerMainDispatch.cs:20-22`). So the watchdog is never cancelled "while the ad is on screen" as the comment at line 149 claims; it is frozen with the player. On the first frame after an ad longer than 12 s it fires, logs "No show callback in time", and runs `Finish`. The real callbacks then find nothing pending. The result is correct; only the log message misleads.
- **timeScale and fades:** an interstitial cannot start while paused (`LoadNextLevel` resets `timeScale` first, and pause is refused during the popup) or during a fade (no transition is running at the level-complete popup). If the watchdog fires before a slow ad appears, the Shop fade starts and the ad then covers it; the fade resumes after the ad.
- **Backgrounding during the ad:** Unity stays paused until the ad reports completion (section 1.8), then continues as normal.
- **Shop never opening:** every path ends in the continuation (ready, not ready, failure, watchdog). The only hang is the SDK never reporting completion after native show start, but in that case the native `UnityPause(1)` freezes the whole player anyway and no C# code could recover.

### 4.4 Dashboard requirements and testing

- Both `Rewarded_iOS` and `Interstitial_iOS` must exist and be enabled on the Unity dashboard (Monetization, Ad Units) for game 6074412. They are Unity's default unit names for a new iOS project, but confirm they are there. A missing unit makes loads fail quietly, with a warning every 10 s (warnings survive `LogControl`).
- Test ads on a device without shipping test mode: add the phone as a test device on the dashboard (Monetization, Testing), or force test mode for the game there. Flipping `_testMode` on the MainMenu `AdManager` also works, but must be reverted before an App Store build. The Editor shows Unity's placeholder ads.
- The App Store "advertising identifier: Yes" answer and the App Privacy tracking disclosure need to match the ATT prompt now being present (see `ios-testing-and-release.md` section 5).

### 4.5 iOS pods and support package

- `com.unity.ads` declares the pod `UnityAds ~> 4.17.0` (`Library/PackageCache/com.unity.ads@4.17.0/Editor/Source/Dependencies.xml`). The export's `Podfile` adds it to target `UnityFramework`, platform iOS 14.0, `use_frameworks! :linkage => :static`; `Podfile.lock` resolves 4.17.0 (CocoaPods 1.16.2).
- Both packages' `PostProcessBuildPlist` (`IPostprocessBuildWithReport`, order 0) add the SKAdNetwork IDs: 77 in the March export.
- Privacy manifests in the export: `UnityFramework/PrivacyInfo.xcprivacy` and the one inside `UnityAds.framework`.

---

## 5. iOS build pipeline

### 5.1 Player Settings that matter (`ProjectSettings/ProjectSettings.asset`)

| Setting | Value | Line |
| --- | --- | --- |
| Bundle ID (iPhone) | `com.BetzzzGame.FoodTruckCafe` | 169 |
| Version | 26.03.29 | 143 |
| Build (iPhone) | 1 (the March export says 0) | 174 |
| Target device | 2 = iPhone and iPad | 12 |
| Orientation | 0 = Portrait (autorotate flags ignored) | 11, 61-64 |
| Min iOS | 14.0 | 194 |
| SDK | 988 = Device SDK | 193 |
| Status bar hidden, requires full screen | 1, 1 | 202-203 |
| Mute other audio | 0 (Ambient, the silent switch mutes) | 84 |
| Background modes | 0 | 237 |
| Team | XY39924J9T, automatic signing on | 245, 252 |
| Unity splash | shown | 20 |
| Strip engine code | on | 182 |
| Metal API validation | on (dev builds only) | 241 |
| Scripting backend and architecture | not pinned (IL2CPP, ARM64 on iOS) | 734-736 |

Mobile Notifications: `RequestAuthorizationOnAppLaunch` is now False (`Assets/Editor/com.unity.mobile.notifications/NotificationSettings.asset`). The March export still had `UnityNotificationRequestAuthorizationOnAppLaunch => true`, so the March build asked for notification permission at launch.

### 5.2 IOSPostBuild (`Assets/Editor/IOSPostBuild.cs`)

`[PostProcessBuild(100)]` (16) runs after the order-0 SKAdNetwork processors and rewrites `Info.plist`:

- `NSUserTrackingUsageDescription` = "This lets us show ads that are more relevant to you and keeps the game free." (13-14, 25)
- `ITSAppUsesNonExemptEncryption` = false (29). This is correct for a game whose only encryption is the OS's HTTPS (Unity Ads), so it is exempt.

The March export has neither key; both appear from the next export on.

The string states the purpose, which is what review checks. "Keeps the game free" can be read as nudging the player to consent; Apple's own example wording is plainer ("Your data will be used to deliver personalized ads to you"). Optional reword, not a blocker.

### 5.3 The loose plist (`Assets/Plugins/iOS/NSUserTrackingUsageDescription.plist`)

It is imported with `DefaultImporter` (its `.meta`), not as a plugin, so Unity **does not copy it into the export at all**. The file existed on disk at 16:02 on 29 March, and the 16:34 export contains no copy of it. Its tracking text ("We use your advertising ID…") never ships. The owner said leave it. `Docs/TODO.md` says it is "copied as a resource"; that is wrong, it is simply ignored.

### 5.4 Mobile Dependency Resolver

`Assets/MobileDependencyResolver` (1.2.185, Unity's fork of the External Dependency Manager). There are no `IosResolverSettings` in ProjectSettings, so it uses the defaults: generate the Podfile, run `pod install`, create `Unity-iPhone.xcworkspace`, link statically. If CocoaPods is missing on the Mac, the export still succeeds but has no workspace; run `pod install` in the export folder.

### 5.5 Xcode targets and signing (March export `project.pbxproj`)

| Target | Product | Release signing | Other configs |
| --- | --- | --- | --- |
| Unity-iPhone | application, `com.BetzzzGame.FoodTruckCafe`, Info.plist | team XY39924J9T, "iPhone Developer", Automatic (TargetAttributes) | team set in all four |
| UnityFramework | framework, `com.unity3d.framework` | team XY39924J9T, "Apple Development", Automatic | **ReleaseForProfiling, ReleaseForRunning, Debug: team ""** |
| Unity-iPhone Tests | unit test bundle | team XY39924J9T | |
| GameAssembly | static library | Automatic, no team (none needed) | |

The scheme uses ReleaseForRunning for Run, Release for Archive. So Archive works, but **Run on device can fail with "Signing for UnityFramework requires a development team"** until the team is chosen on UnityFramework for all configurations. This is the recurring "signing issue" in `ios-testing-and-release.md` section 3.

### 5.6 Export folder contents (`Builds/iOS-26.03.29`)

`Classes/` (trampoline, `UnityAppController.mm`), `Data/` (level0-3 and assets), `Il2CppOutputProject/`, `Libraries/` (`com.unity.ads`, `com.unity.ads.ios-support`, `com.unity.mobile.notifications` native code, libil2cpp, baselib), `MainApp/`, `UnityFramework/` (Info.plist, PrivacyInfo), `Unity-iPhone/` (asset catalog), `Unity-iPhone Tests/`, `Unity-iPhone.xcodeproj`, `Unity-iPhone.xcworkspace` (project plus `Pods/Pods.xcodeproj`), `Podfile`, `Podfile.lock`, `Pods/UnityAds`, launch storyboards and PNGs, `Info.plist`, `Dummy.swift`, `process_symbols.sh`. `Pods/` was last touched 3 April.

### 5.7 Append vs Replace

- **Replace** deletes the folder contents and regenerates everything: signing chosen in Xcode is lost (except what Unity writes from Player Settings, which is the main target's team), and `pod install` runs again.
- **Append** keeps the Xcode project and your edits to it, and regenerates Data, Classes, Libraries and the Podfile.
- `IOSPostBuild` and the SKAdNetwork processors run in both modes, so the Info.plist keys come back either way.

---

## 6. Logging

- `LogControl` (`Utils/LogControl.cs:10-15`): before the first scene loads, release (non-development) device builds set `Debug.unityLogger.filterLogType = LogType.Warning`, which keeps Error, Assert, Warning and Exception and drops `Log`. The Editor and development builds keep everything.
- The message strings are still built, because the interpolation runs before the filter. That costs a little CPU, not NSLog time.
- Warnings that stay in release: the interstitial load-failure retry (every 10 s while the unit is missing), the watchdog message after most ads, rewarded load failures, `AudioManager: No arrangement phase music assigned` (only if unassigned), and the settings "not found" warnings.
- Per-script debug flags (`PauseButton.enableDebugLogs`, `SettingButton.showDebugMessages`, and others) are on in the scenes; owner said leave the defaults.
- Device logs: Xcode, Devices and Simulators, Open Console, filter on the process `FoodTruckCafe`. Unity also prints the `-> applicationDidBecomeActive()` lifecycle lines there.

---

## 7. Gotchas (quick list)

1. The real AudioManager clips are MainMenu overrides; the prefab still has Inn.mp3 as menu music and three references to a deleted clip.
2. `SetMusicEnabled(true)` after a `StopMusic` replays the last clip (`AudioManager.cs:356-361`).
3. The Settings Animator runs in scaled time, so it is dead while paused.
4. Menu Play and Continue clicks ignore the Sound toggle.
5. Two scripts fight over Get5Coins' `localScale`.
6. `SafeAreaFitter` is unused and its tooltips read backwards.
7. The scene transition depends on `TransitionSetup` in MainMenu; Play from MainMenu.
8. The ATT request runs in `Awake` of the first scene and does not wait for an answer before ads initialise.
9. The interstitial watchdog normally fires after the ad; the log line is expected.
10. Turn on Unity Services Ads? No. The package initialises from code.
11. UnityFramework is signed only in Release; set the team for all configs before Run.
12. The loose plist in `Plugins/iOS` never ships; Info.plist keys come only from `IOSPostBuild.cs`.
13. The ringer switch mutes the game (Ambient session).
14. A background during an ad delivers no `OnApplicationPause`; nothing relies on it today.

---

## 8. Bugs referenced above (none fixed; for the owner to decide)

| ID | Severity | Where | Summary |
| --- | --- | --- | --- |
| B1 | MAJOR if confirmed on device (App Review risk) | `AdsInitializer.cs:13-21` | The ATT request runs in the first scene's `Awake`, possibly before the app is active, so the prompt may not appear on first launch; it then shows only after a return to MainMenu. Ads also initialise before the answer. |
| B2 | MINOR | `ClickPlay.cs:36,43`, `ClickContinue.cs:71,82` | Menu button clicks ignore Sound Off. |
| B3 | MINOR | `Settings.prefab:58` (Animator Update Mode Normal) | The Settings panel cannot open or close while paused; the queued trigger plays on resume. |
| B4 | MINOR | `AudioManager.cs:356-361` | Music Off then On while paused replays the stopped gameplay track under the pause menu. |
| B5 | MINOR | `LevelManager.cs:657-672`, `InterstitialAdService.cs:124-129` | A double tap on Next Level with an ad loaded starts the Shop fade under the ad and miscounts `levelsSinceLastAd`. |
| B6 | MINOR | `ShopManager.cs:330` | Shop Next Level starts a random gameplay track for about 0.5 to 1 s before GameSceneOne switches to arrangement music. |
