# 09 Ads, analytics and purchases

Last verified against the code: 26 November 2026. Paths are under `Assets/Scripts`. Dashboard ids and store facts
are in `MANUAL.md` section 1 and `Docs/ios-testing-and-release.md`.

## Starting Unity Ads (`Ads/AdsInitializer.cs`, on the MainMenu scene)

1. `Awake` stores the game id (`_iOSGameId`) and the test-mode flag in statics, then runs
   `RequestTrackingThenInitialize` on the persistent `InterstitialAdService` object.
2. On iOS, when tracking permission is undetermined: waits until the app is focused (up to 5 s) plus 0.5 s, asks,
   then polls for the answer for up to 15 s.
3. `Keep()` loops once a second until `Advertisement.isInitialized`: it calls `Advertisement.Initialize` when a try
   is due. A failed try is retried after 5, 15, 30, 60 s, then every 120 s (300 s when Unity reports an ad blocker);
   a try with no answer in 45 s counts as failed; while the phone reports no network the next try waits (at most a
   minute).
4. `Ensure()` (called by everything that wants an ad, and when the app returns from the background) brings a waiting
   try forward, at most one every 5 s.
5. On success `Announce()` runs once: `AnalyticsLog.AdsStarted(tries, seconds)`, `OnReady`, and
   `InterstitialAdService.Preload()`.

Test mode: the Inspector flag or `IsTestFlightBuild()` (a sandbox receipt, `Assets/Plugins/iOS/TestFlightCheck.mm`).

## The between-days ad (`Ads/InterstitialAdService.cs`, unit `Interstitial_iOS`)

- **Where it shows:** only from `MapScreen.PlayDay`, after Play on a day card and before GameSceneOne, when
  `day >= MapScreen.FirstAdDay` (2: the third day of the game is the first that starts with an ad). Nothing shows
  after a day ends or around the shop.
- `ShowAfterLevelThen(continuation)` runs the continuation at once, with no ad, when: the build is a TestFlight
  build; `RemoveAds.Owned`; fewer than `MinSecondsBetweenAds` (45 s) since the last ad; no ad is loaded (it starts a
  load); or one is already showing. Otherwise it shows the ad and runs the continuation when the ad completes, is
  skipped or fails.
- Watchdog: if `OnUnityAdsShowStart` has not arrived 12 s after `Show`, the continuation runs anyway. Once the ad has
  started there is no timeout (review item R6).
- Loading: `Preload()` after Unity Ads starts, after every ad and when the app comes back; a failed load is retried
  after 10, 20, 40, 80 s, then every 120 s; a load with no answer in 30 s counts as failed.

## Rewarded ads (unit `Rewarded_iOS`)

| Where | Script | Pays |
| --- | --- | --- |
| Shop coin button | `Ads/RewardedAdButton.cs` | `_coinsPerAd` (50) through `AddScoreImmediately`, only on `COMPLETED` and with an active session |
| Daily Treats "One more treat" (MainMenu) | `DailyTreats/DailyTreatsAd.cs` | 20 to 50 coins at random on `COMPLETED`, into the Daily Treats pending coins |

Remove Ads does not affect these. Daily Treats coins (`DailyTreats.CoinsFor(day)`: 30, 50, 70, then the gift 150 on the
fourth day of a row; since 26 Nov 2026) wait in PlayerPrefs `ftc.daily.pending` and are added to the wallet by `ContinueSession` or
`StartNewSession` (`DailyTreats.TakePending`).

## Analytics (`Analytics/AnalyticsLog.cs`, Unity Analytics)

Started before the first scene. Environment: `development` in the editor and on TestFlight, `production` in store
builds. Events recorded before the service is ready are queued (up to 50). Every event must be defined in the Unity
dashboard's Event Manager with the same parameter names, or Unity drops it. `Flush()` is called at the milestones
marked below and when the app loses focus.

| Event | Parameters | Sent from | Flush |
| --- | --- | --- | --- |
| `tutorialStep` | `stepNumber` (int), `stepText` (string) | `TutorialDirector`, each caption | no |
| `tutorialFinished` | `stepNumber` (captions shown) | `TutorialDirector`, chapter 1 tutorial's end | yes |
| `dayStarted` | `day`, `foodsOn` (enabled foods, comma-separated) | `GamePhaseManager.StartPlayPhase`, not in a tutorial | yes |
| `dayFinished` | `day`, `stars`, `dayEarnings` | `LevelManager.OnLevelComplete` | yes |
| `dayLeft` | `day`, `reason` (`restart` or `menu`) | `LevelManager.RestartLevel`, `GoToMainMenu`, only before the day has ended | yes |
| `shopPurchase` | `itemName`, `price` | `ShopManager.ConfirmPurchase` on success | yes |
| `adWatched` | `adType` (`interstitial`, `rewarded`, `dailyTreat`), `completed` (bool) | the three ad scripts' show-complete callbacks | no |
| `adsStarted` | `tries`, `seconds` | `AdsInitializer.Announce` | no |

`day` is the running day index + 1 (chapter 2's first day is 13), not the per-chapter number the player sees.

Known gaps, as the code stands: the pause menu's Home (`LevelManager.GoToMap`) records no `dayLeft`;
`dayEarnings` is `ScoreManager.GetCurrentScore()` before the replay factor; star-tab buys are logged with their star
price in `price`; Remove Ads and 15 Stars purchases are not logged; an ad that fails to show records nothing.

## Purchases (`Purchases/RemoveAds.cs`, Unity IAP)

- Products: `com.BetzzzGame.FoodTruckCafe.removeads` (non-consumable) and `...stars15` (consumable, `StarPack.Stars` 15).
- **Unity IAP starts only in the Shop scene:** `RemoveAds.Init()` is called from `ShopManager.EnsureRemoveAdsCard` and
  `EnsureStarPackCard`; it creates the persistent `RemoveAdsStore`, which waits up to 10 s for Unity Services and
  calls `UnityPurchasing.Initialize`.
- Remove Ads ownership is the PlayerPrefs flag `ftc_ads_removed` (`RemoveAds.Owned`), set by `ProcessPurchase` or, in
  `OnInitialized`, when the product has a receipt. After a reinstall the flag is therefore rebuilt only once a shop
  has been opened. This is safe only because no ad shows before the first shop visit; moving the first ad earlier
  would break it.
- 15 Stars: `ProcessPurchase` adds the stars (`SessionManager.AddBoughtStars`), then stores the transaction id in
  `ftc_starpack_done` (the last 20) so a repeated delivery is not paid twice; with no active session it returns
  `Pending` and the store delivers again later.
- The shop shows the $ tab, the cards and Restore only while `RemoveAds.Available` / `StarPack.Available`.
- Restore: `RemoveAds.Restore` → `IAppleExtensions.RestoreTransactions`.
