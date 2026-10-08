# 05: Scoring, session (save file) and Shop

Verified against the code and scene YAML on 7 October 2026. Line numbers are for commit `2865ea7` (which moved the final-day session completion into `OnLevelComplete`). The game is live: change code only for real bugs (see `CLAUDE.md`).

Paths are relative to `Assets/`. Shorthand: SM = `Scripts/Session/SessionManager.cs`, ScM = `Scripts/Scoring/ScoreManager.cs`, LM = `Scripts/LevelControl/LevelManager.cs`, ShM = `Scripts/Shop/ShopManager.cs`.

---

## 1. Scoring

### 1.1 Money words

The code says "score" and "points" everywhere, but there is only one currency: dollars, shown as `$ 12.50`. Three numbers exist:

| Number | Where it lives | Lifetime | What it is |
| --- | --- | --- | --- |
| `currentScore` | ScM:41, private | one level (reset in `ResetScore`, ScM:120) | item points plus tips earned this level. Drives the stars and the "Today Sale" split. Never saved. |
| `totalTipsEarned` | ScM:43, private | one level in practice (new `ScoreManager` per scene load; **not** reset by `ResetScore`) | sum of tips this level. Read by the level-complete popup. |
| `currentOrderItemPoints` | ScM:42, public | one order | item points of the order in progress, held back until the order ends |
| `SessionData.totalScore` | SM:9 | the whole save | the player's wallet. Every dollar earned, spent, refunded or granted by an ad goes through it. This is the only number saved. |

### 1.2 Points table (GameSceneOne, `ScoreManager` component at scene line 4579, table from line 4601)

| itemType | Points | Notes |
| --- | --- | --- |
| Coffee | 10 | |
| Bread | 15 | |
| Apple | 15 | |
| Juice | 20 | |
| Melon | 25 | Melon is unreachable in play (see maintenance notes) |
| Pie | 30 | orphan, no item uses this name |
| Mont Blanc | 35 | orphan |
| Log Cake | 35 | orphan |
| Cake | 35 | **added 7 Oct 2026** (scene line 4618); scored 0 before |
| Choux | 35 | **added 7 Oct 2026** (scene line 4620); scored 0 before |

`pointsPerItem` is **0** in the scene (line 4599): the fallback in `GetPointsForItem` (ScM:295-304) returns it for any name not in the table, so a new food that is not added here earns nothing. Matching is exact and case-sensitive against `ServeableItem.foodType`.

### 1.3 Tip formula

`OrderSystem.CompleteOrder` (`Scripts/ItemsAndOrders/OrderSystem.cs:430-436`) passes the seconds left on the order timer and `currentOrderItemPoints`. Then ScM:152-156:

```
timeBonusPoints = remainingSeconds * timeBonusMultiplier      // timeBonusMultiplier = 10 in the scene (line 4600; code default 5)
tip = Round( itemPoints * timeBonusPoints / 100 * tipMultiplier , 2 decimals, MidpointRounding.AwayFromZero )
```

So **tip = item points x remaining seconds x 10% x customer multiplier**, rounded to cents. (Docs/maintenance.md says "remainingSeconds x 10"; that omits the item points and the /100, the formula above is what runs.)

- `tipMultiplier`: set per customer by `CustomerManager` (`Scripts/Customer/CustomerManager.cs:200`), reset to 1 at :330. `CustomerController.TipMultiplier` is 1 (`CustomerController.cs:34`); `GrandmaCustomer` overrides it to 2 (`GrandmaCustomer.cs:16`). Girl, Boy and Kid tip at 1x.
- Example: Day 1, order Coffee + Bread = 25 points, served with 3.0 s left, normal customer: tip = 25 x 30 / 100 = 7.50. Same order for Grandma: 15.00. Order total banked: 32.50 (or 40.00).
- The order timer starts at `LevelData.orderDisplayTime` (5 s on Days 1-5, 4 s after) and counts down in scaled time (`OrderSystem.cs:236`). Maximum tip is therefore 50% (Days 1-5) or 40% (Days 6-14) of the item points, doubled for Grandma.
- "Perfect order" sound: `orderTimer >= orderDisplayTime * perfectTimeFraction` (0.5), `OrderSystem.cs:441`. It does not change money.

### 1.4 When money reaches the session

| Method | Called from | What it does to money |
| --- | --- | --- |
| `AwardItemPoints(itemType)` ScM:140 | `OrderSystem.cs:367`, each correct serve | adds points to `currentScore` and `currentOrderItemPoints`, updates stars, plays pickup sound. **Nothing reaches the session yet.** |
| `AwardOrderCompletionBonus(remaining, basepoints)` ScM:152 | `OrderSystem.cs:436`, last item of an order served | computes the tip, adds it to `currentScore` and `totalTipsEarned`, then `SessionManager.AddScoreImmediately(itemPoints + tip)` (ScM:162-164), refreshes labels and stars, shows the popup after 0.2 s, clears `currentOrderItemPoints`. |
| `ApplyOrderExpiredPenalty()` ScM:191 | `OrderSystem.cs:498`, timer hit 0 | despite the name, **no penalty**: banks the item points already served (`AddScoreImmediately`, ScM:197-198), no tip, popup with item points only, clears `currentOrderItemPoints`. The "Order Expired!" text goes to `feedbackText`, which is unassigned in the scene, so it is only logged; `penaltySound` is unassigned too. |
| `AddScore(points)` ScM:306 | nobody | dead private method |

`SessionManager.AddScoreImmediately` (SM:324-332) returns false and adds nothing when there is no active session; otherwise it rounds the new total to cents (SM:335-338), saves to PlayerPrefs and fires `OnTotalScoreChanged`. So the save file is written once per finished or expired order.

### 1.5 Stars

`StarProgressBar` (`Scripts/Scoring/StarProgressBar.cs`, scene line 4537). `LevelManager.LoadLevel` calls `Initialize(levelData)` (LM:367); every `AwardItemPoints` and `AwardOrderCompletionBonus` calls `UpdateDisplay(currentScore)`. A star is filled when `currentScore >= starThresholdN` (`LevelData.HasEarnedStar`, `LevelData.cs:65`). Stars are purely visual: they are not saved, not shown on the level-complete popup, and gate nothing. The three optional texts (`currentScoreText`, `nextStarText`, `performanceText`) are unassigned in the scene. Thresholds per day are in `Docs/maintenance.md`.

### 1.6 Popups

`SimpleScorePopup.CreateCombinedPopupWithFont` (`Scripts/Scoring/SimpleScorePopup.cs:207`) is spawned under `ScoreManager.gameCanvas` 0.2 s after an order ends (ScM:182-189). Text: `" $15.00\n Tips: $ 7.50"` when a tip was earned, `"$15.00"` otherwise (SimpleScorePopup.cs:73-82). Only one popup exists at a time (static `currentPopup`), it stays 4 s then fades. `ClearScorePopup` runs at level start, level end, and Restart.

### 1.7 Which text shows which number (GameSceneOne)

| Text object | Wired as | Written by | Shows |
| --- | --- | --- | --- |
| `ScoreText` (HUD) | `ScoreManager.inGameScoreText` | `UpdateScoreUI` ScM:214-230, at Start, `ResetScore`, each order end | `"$ 123.45"`: the **session wallet**, not the level score |
| `TotalEarned` (level-complete panel) | `ScoreManager.finalScoreText` and `LevelManager.totalEarned` | ScM writes `"Level Score: x"` during play (hidden); LM:605 and the count-up overwrite it | `"Earned: $ x"` counting up to the wallet; on Day 14 `"New Record! $x"` or `"Final Score: $x"` |
| `TodaySale` (panel) | `ScoreManager.totalScoreText` and `LevelManager.todaySale` | ScM writes `"Total Score: x"` on every `OnTotalScoreChanged` (hidden); LM:603 overwrites | `"Today Sale: $ x"` = `currentScore - totalTipsEarned`; Day 14: "Congrats! You finished all levels!" |
| `TodayTip` (panel) | `LevelManager.todayTip` | LM:601 | `"Tips Earned: $ x"` |
| `unlockedItemsText` (panel) | `LevelManager.unlockedItemsText` | LM:643-659 | `"Unlocked Food: n\nUnlocked Customers: m"` where n and m are counts **still to buy**; blank on Day 14 because the session is already completed when the popup opens |
| `overlayScoreText`, `feedbackText` | unassigned | | |

Level-complete count-up (LM:577-622): wait 3 s, show panel, set the three labels, wait 1 s, animate Today Sale into Earned, wait 1 s, animate Tips into Earned. Each animation lasts `amount / 100` seconds clamped to 0.5-10 s (LM:752-753). The money was already in the wallet; the animation is display only.

### 1.8 High score

- Key `FoodTruckHighScore` (float), SM:110, read SM:295, written SM:305-310.
- Only `OnLevelComplete` on the final day calls `CheckAndSaveHighScore` (LM:563-567, immediately after the last order, before `CompleteSession`): it saves the **wallet balance at that moment** if it beats the stored value. So the "score" is money left after every Shop purchase, plus rewarded-ad coins. Until commit `2865ea7` this happened at the end of the count-up (LM `ShowFinalCompletionMessage`), so leaving the popup early skipped it.
- It is never shown anywhere except as "New Record!" vs "Final Score" on the Day 14 popup (`ShowFinalCompletionMessage`, LM:624-641, reads the stored `finalRunIsNewRecord`). It survives Play (new game); nothing resets it.
- The legacy path (a save already pointing past Day 14, see 4) completes the session but never calls `CheckAndSaveHighScore`, so that run's total is not recorded.

---

## 2. SessionManager (SM)

One instance, created by the `SessionManager` object in MainMenu (scene line 435: `totalFoodItems: 8`, `totalCharacters: 4`), `DontDestroyOnLoad` (SM:122-126). `SessionInitializer` exists in all three scenes and creates a bare one if missing (code defaults 4 and 2); that only happens when a scene is played directly in the editor. Awake loads the save (SM:129, 251-262).

### 2.1 SessionData, field by field (SM:6-90)

| Field | Type | Saved? | Meaning |
| --- | --- | --- | --- |
| `totalScore` | float | yes | wallet, whole cents |
| `currentLevel` | int | yes | 0-based index of the next day to play (advanced at level complete) |
| `levelsCompleted` | int | yes | days completed; set to `index+1` at each completion, never lowered. Gates Continue (>= 1). |
| `sessionStartTime` | DateTime | **no** | JsonUtility cannot serialize `DateTime`; it is silently skipped. Unused anyway. |
| `isActive` | bool | yes | false once Day 14 is finished (`CompleteSession`). An inactive session accepts no money and no purchases. |
| `purchasedFoodItems` | List<string> | yes | starts `["Bread","Coffee"]` |
| `purchasedCharacters` | List<string> | yes | starts `["Girl"]` |
| `foodUpgradeLevels` | Dictionary<string,int> | no (`[NonSerialized]`) | runtime view; starts Coffee 1, CoffeeMachine 1 |
| `foodUpgradeKeys` / `foodUpgradeValues` | List<string> / List<int> | yes | parallel lists mirroring the dictionary (`SyncUpgradesToLists` SM:64 before every save, `SyncUpgradesFromLists` SM:77 after load) |
| `savedFoodPositions` | List<FoodItemPosition{foodType,x,y}> | yes | table layout from the arrangement phase, written when Done is tapped (`GamePhaseManager.cs:249` -> SM:195-213), applied by `GamePhaseManager.LoadSavedFoodPositions` (:261) |

### 2.2 Example save, exactly as stored

PlayerPrefs key `FoodTruckSession`, one line, field order = declaration order. A player who finished Days 1 and 2, bought Juice, Boy and the Coffee upgrade (position values illustrative):

```json
{"totalScore":412.5,"currentLevel":2,"levelsCompleted":2,"isActive":true,"purchasedFoodItems":["Bread","Coffee","Juice"],"purchasedCharacters":["Girl","Boy"],"foodUpgradeKeys":["Coffee","CoffeeMachine"],"foodUpgradeValues":[2,1],"savedFoodPositions":[{"foodType":"Bread","x":-2.5,"y":-3.25},{"foodType":"Coffee","x":0.75,"y":-3.0},{"foodType":"Juice","x":2.25,"y":-3.5}]}
```

A save from shipped version **26.03.29** has the same shape **without** `foodUpgradeKeys` and `foodUpgradeValues` (the old code had a public Dictionary, which JsonUtility drops; checked with `git show b8084c7:Assets/Scripts/Session/SessionManager.cs`).

### 2.3 Every SaveSession call site (SaveSession is SM:425-436)

| Line | Caller | Trigger |
| --- | --- | --- |
| SM:164 | `RestoreScoreToSnapshot` | Restart |
| SM:211 | `UpdateFoodPositions` | Done tapped in arrangement phase |
| SM:267 | `StartNewSession` | Play |
| SM:329 | `AddScoreImmediately` | order complete/expired, rewarded ad, shop refund |
| SM:366 | `OnLevelCompleted` | last order of a day ends (LM:558) |
| SM:378 | `SetCurrentLevel` | Restart (LM:330), debug start (LM:77) |
| SM:388 | `CompleteSession` | final day's last order (LM:566) or `OnAllLevelsComplete` (LM:692) |
| SM:482 / 500 | `PurchaseFoodItem` / `PurchaseCharacter` | Shop |
| SM:519 | `DeductScore` | Shop |
| SM:542 | `UpgradeFood` | Shop |
| SM:569 / 574 | `OnApplicationPause(true)` / `OnApplicationFocus(false)` | app backgrounded |

Every save calls `PlayerPrefs.Save()` immediately.

### 2.4 LoadSession and legacy handling (SM:438-469)

- No key: `currentSession = null` (Continue disabled, Play creates one).
- Parse error: logged, `currentSession = null`; the bad string stays until the next save overwrites it.
- `purchasedCharacters` null -> `["Girl"]`.
- `SyncUpgradesFromLists`: rebuilds the dictionary, then forces Coffee and CoffeeMachine to 1 if missing. This covers 26.03.29 saves; whether or not JsonUtility runs the constructor, the result is Coffee 1, CoffeeMachine 1.
- `savedFoodPositions` null -> empty list.
- `purchasedFoodItems` null is **not** handled; no known save produces it.

### 2.5 StartNewSession vs ContinueSession

- `StartNewSession` (SM:264-277): brand-new `SessionData` (wallet 0, Day 1, Bread/Coffee/Girl, upgrades 1), saved, kitchen timers and cooldowns cleared, event fired. High score untouched.
- `ContinueSession` (SM:279-293): if active, `levelManager.SetCurrentLevel(...)` (only if a LevelManager is alive, which it never is in MainMenu) and fires the event. In practice it does nothing; `LevelManager.Start` reads `GetCurrentLevelIndex()` itself (LM:89-93). If no active session it calls `StartNewSession`.

### 2.6 Snapshot and restore on Restart

- `SnapshotScoreBeforeLevel` (SM:153-157) from `LevelManager.StartLevel` (LM:502), i.e. every time GameSceneOne loads a day. Held in memory only (`scoreAtLevelStart`, SM:151).
- `RestoreScoreToSnapshot` (SM:159-168) from `RestartLevel` (LM:329), followed by `SetCurrentLevel(currentLevelIndex)` (LM:330) and a scene reload. Used by the pause Restart and the level-complete Restart.
- The snapshot is taken after the Shop, so Shop purchases and rewarded-ad coins are never rolled back by a Restart.
- Main Menu from the pause panel does **not** restore: money earned so far in the day is kept, and the day is replayed on Continue (see Money edge cases).

### 2.7 CompleteSession (SM:383-395)

Sets `isActive = false`, saves, fires `OnSessionCompleted` (LevelManager's handler only logs). Afterwards: Continue is greyed out, `AddScoreImmediately` refuses money, Shop purchases fail with "No active session!", `ApplyServeableItemSettings` falls back to "all items active" (LM:412-424).

### 2.8 Public API and callers

| Member | Callers |
| --- | --- |
| `Instance` | everywhere |
| `OnTotalScoreChanged` | ScoreManager subscribes (ScM:111-113, :367) |
| `OnSessionCompleted` | LevelManager (LM:142-157) |
| `OnFoodItemPurchased` | `KitchenButtonGlowController` (unused in scenes) |
| `SnapshotScoreBeforeLevel` / `RestoreScoreToSnapshot` | LM:502 / LM:329 |
| `FindGameReferences`, `RegisterLevelManager` | LM:85-86 |
| `IsFoodItemPurchased` | `KitchenFoodGate.cs:34` |
| `UpdateFoodPositions`, `HasSavedPositions` | `GamePhaseManager.cs:249`, :263 |
| `StartNewSession` | `ClickPlay.cs:135`, `ClickContinue.cs:108`, `ContinueSession` |
| `ContinueSession` | `ClickContinue.cs:103` |
| `CheckAndSaveHighScore` | LM:565 |
| `IsNewHighScore`, `SaveHighScore` | only internally |
| `GetHighScore` | only internally (no UI reads it) |
| `AddScoreImmediately` | ScM:164, ScM:198, `RewardedAdButton.cs:174`, ShM:174 (refund) |
| `CanAfford` | ShM:104, ShM:146, `DeductScore` |
| `GetUnpurchasedItemsCount` | LM:654 |
| `OnLevelCompleted` | LM:558 |
| `SetCurrentLevel` | LM:77, LM:330 |
| `CompleteSession` | LM:566, LM:692 |
| `HasActiveSession`, `GetTotalScore`, `GetCurrentLevelIndex`, `GetCurrentSession` | many (ClickPlay, ClickContinue, LevelManager, CustomerManager, GamePhaseManager, ShopManager, KitchenFoodGate) |
| `PurchaseFoodItem`, `PurchaseCharacter`, `UpgradeFood`, `DeductScore`, `CanUpgradeFood` | ShopManager only |
| `GetFoodUpgradeLevel` | ShM:273, LM:466-478 |
| `GetAllFoodUpgradeLevels` | nobody |

### 2.9 All PlayerPrefs keys in the project

| Key | Type | Owner |
| --- | --- | --- |
| `FoodTruckSession` | string (JSON above) | SM:109 |
| `FoodTruckHighScore` | float | SM:110 |
| `GlobalAudioState` | int 1/0, default 1 | `Scripts/Buttons/SettingsButtonsController.cs:31` |
| `GlobalMusicState` | int 1/0, default 1 | `SettingsButtonsController.cs:32` |

No other script under `Assets` touches PlayerPrefs.

---

## 3. Shop

### 3.1 Layout (Shop.unity)

- `ShopPanel` slides in from x = -1284 over `slideAnimationDuration` = **3 s** (ShM:406-427; scene value). Buttons work during the slide.
- `ItemContainer` (800 x 4000, anchored y = -38) holds the nine items. It also carries a `ScrollRect` whose content is the container itself with no viewport, so finger drags cannot move it; scrolling is only by the arrow buttons.
- `ScrollUpButton` / `ScrollDownButton` move the container by `scrollAmount` 800 over 1 s, clamped to y in [-38, 2000] (hard-coded at ShM:365-366 and :396-397). From the top it takes three taps down (-38, 762, 1562, 2000).
- `Score` label (`playerScoreText`): `"EARNED: $ 412.50"` (ShM:292); also used for 2-second green/red messages.
- `PurchaseConfirmationPopup`: `PopupItemIcon`, `PopupItemInfo`, `Purchase` (Inspector call `ConfirmPurchase`, scene line 2000) and `Cancel` (`CancelPurchase`, line 472).
- `NextLevelButton` (Inspector call `LoadNextGameLevel`, line 2132).
- `Get5Coins` (`RewardedAdButton`).
- There is **no Main Menu button** in the Shop (`mainMenuButton` is unassigned and no such object exists).

### 3.2 Item table (prefab overrides in `m_Modifications`)

All are instances of `Prefabs/ShopItemPrefab.prefab`. Prefab defaults: `itemName` Bread, `price` 50, `itemType` Food, popup text about bread. Structure: root (Image + `ShopItemController`), child `ItemIcon` (Image + Button, the tap target), child `PriceText` (TMP).

| Scene object (instance line) | itemName | itemType | upgradeFoodType | price | PriceText | purchaseButtonText | popupInfoText | Container pos (x, y) |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Juice (3259) | Juice | Food | | 150 | $ 150 | $150 | $ 150 / Orange Juice | -300, 477 |
| CoffeeUpgrade (3921) | Upgrade | Upgrade | Coffee | 350 | $ 350 | 350 | $ 350 / Add one more plate | 59, 468 |
| Boy (2448) | Boy | Character | | 50 | $ 50 | $50 | $ 50 / New Customer! | -9, -69 |
| Apple (125) | Apple | Food | | 350 | $ 350 | 350 | $ 350 / Fresh Apples! | -259, -151 |
| Granma (2979) | Grandma | Character | | 300 | $ 300 | $300 | $ 300 / She pays more tips! | -397, -734 |
| CoffeeMachineUpgrade (859) | Upgrade | Upgrade | CoffeeMachine | 600 | $ 600 | $ 600 | $ 600 / Brew one more cup | -250, -790 |
| Kid (520) | Kid | Character | | 500 | $ 500 | $500 | $ 500 / She orders a lot! | 33, -1390 |
| Choux (1126) | Choux | Food | | 750 | $ 750 | $750 | $ 750 / Choux with fresh blueberries | -400, -1488 |
| Cake (2716) | Cake | Food | | 750 | $ 750 | $750 | $ 750 / Classic Strawberry Cake | -384, -2065 |

The price is typed in four independent places per item (`price`, `PriceText`, `purchaseButtonText`, the first line of `popupInfoText`); only `price` is charged.

Owned from the start: Bread, Coffee, Girl. Not sold: Melon. Character names must match the customer prefab name with "Customer"/"Prefab" stripped (`CustomerManager.cs:294`); the prefabs are `Girl`, `Boy`, `Kid`, `Grandma`.

### 3.3 Start-up

`Start` (ShM:38-49) wires buttons through `Wire` (ShM:75-81: adds a runtime listener only when the button has no Inspector call, so Purchase, Cancel and Next Level are not doubled), starts shop music, then after one frame `InitializeShop` (ShM:298-314): label, affordability (`RefreshShopDisplay`, ShM:94-107), owned state (`UpdatePurchasedItemsUI`, ShM:252-278), scroll buttons, slide.

Item visuals (`ShopItemController.UpdateVisualState`, `Scripts/Shop/ShopItemController.cs:68-96`): owned = green, not tappable; affordable = white, tappable; too expensive = black at 80% alpha, not tappable. Tapping the icon opens the popup (`OnItemClicked`, :51-54) unless owned.

### 3.4 Purchase flow (`TryPurchaseItem`, ShM:117-203)

1. No active session -> "No active session!".
2. `isPurchased` -> "Already purchased!".
3. Upgrade and `CanUpgradeFood` false (level >= 3) -> mark owned, "Max level reached!".
4. `CanAfford(price)` false -> "Not enough points!\nNeed: 350 | Have: 120.00".
5. **Pay first**: `DeductScore(price)` (SM:512-527; saves). If it fails -> "Not enough points!".
6. **Record**: Food -> `PurchaseFoodItem(itemName)`, Character -> `PurchaseCharacter(itemName)`, Upgrade -> `UpgradeFood(upgradeFoodType)` (no cost passed, so it does not charge twice). Each saves.
7. Record failed (e.g. the name is already in the list) -> **refund** with `AddScoreImmediately(price)`, "Purchase failed!".
8. Success: label update, mark owned, purchase sound, refresh affordability, close popup, green "Purchased {itemName}!" in the score label for 2 s.

Pay-then-record runs in one frame, so a kill can at worst lose the item after paying, never hand it out free; a crash between the two saves is the only window.

### 3.5 Feedback messages

Failures go into `PopupItemInfo` in red while the popup is open (the popup stays open, the player taps Cancel), otherwise into the score label in red for 2 s (ShM:206-219). Success always goes to the score label in green (ShM:221-224). A serial number (ShM:35, 234, 246, 294) stops an older message's timer from overwriting a newer one; the restore re-reads the live wallet.

### 3.6 One-shot upgrades

`UpgradeFood` raises the level by one (SM:531-546). `UpdatePurchasedItemsUI` marks an upgrade item owned once its level is above 1 (ShM:271-277), and the item never unmarks, so each upgrade item sells exactly once (level 2). The cap of 3 exists in `CanUpgradeFood` but no item sells the second step. Consumers: `Coffee.SetUpgradeLevel` (`Scripts/Food/Coffee/Coffee.cs:48`, max cups 2 -> 3), `CoffeeMachine.SetUpgradeLevel` (`Scripts/Food/Coffee/CoffeeMachine.cs:199`, brews 2 cups and swaps sprite). Keys are resolved in `LevelManager.GetUpgradeLevelForObject` (LM:466-478): ServeableItem food type, "CoffeeMachine" for the machine, otherwise the GameObject name.

### 3.7 Exits

- Next Level -> `LoadNextGameLevel` (ShM:327-332): ignored while the popup is open, plays gameplay music, transitions to GameSceneOne, which loads `currentLevel` (already advanced at level complete).
- No Main Menu. The only other way out is closing the app; Continue then resumes at the next day, skipping the Shop.

### 3.8 Rewarded ad ("Get5Coins", `Scripts/Ads/RewardedAdButton.cs`)

- Lives only in Shop.unity (the script GUID `b18abd06...` appears in no other scene or prefab).
- Waits up to 15 s for Unity Ads to initialise, loads `Rewarded_iOS`, retries failed loads after 3 s doubling to 30 s. Button is non-interactable until loaded.
- Tap ignored while the purchase popup is open (:129). On `COMPLETED` -> `AddScoreImmediately(5)` (:174, saved immediately), reward sound, `ShopManager.RefreshScoreDisplay` so newly affordable items light up. Skipped or failed shows nothing extra.
- `_buttonLabel` and `_infoLabel` are unassigned in the scene and the button has no text child, so none of the status texts ("Loading ad...", "+$5 coins added!", "Watch the whole ad to earn coins.") are ever visible. The only feedback is the button greying out, the sound, and the wallet label.
- Interplay with Restart: ad coins arrive before the next day's snapshot (LM:492), so a Restart can never take them back, and an ad cannot be watched mid-level.

### 3.9 Adding a new shop item, end to end

**Food**
1. Shop.unity: duplicate an existing Food instance under `ItemContainer`; set `itemName` to the exact food string, `price`, `itemType` Food, the `ItemIcon` sprite, `PriceText`, `purchaseButtonText`, `popupInfoText`; place it inside y in roughly [-2100, 500] (taller content needs the clamp constants at ShM:365-366 and :396-397 changed).
2. GameSceneOne: a `ServeableItem` with the same `foodType`, listed in `LevelManager.serveableItems` (purchase-gated in `ApplyServeableItemSettings`, LM:412) and `OrderSystem.availableFoods`; a `RefillableItem`; an entry in the `ScoreManager.itemPoints` table (otherwise 0 points).
3. KitchenScene: a refill source with `KitchenFoodGate.associatedFoodType` set to the same string.
4. MainMenu: raise `SessionManager.totalFoodItems` if you want the level-complete counter to stay right.

**Character**: a customer prefab named `<Name>` or `Customer<Name>` in `CustomerManager.customerPrefabs`, a Shop item with `itemType` Character and `itemName` `<Name>`, and `totalCharacters` in MainMenu.

**Upgrade**: a Shop item with `itemType` Upgrade and `upgradeFoodType` = key; a component implementing `IUpgradeable` whose key resolves in `GetUpgradeLevelForObject`. Unknown keys default to level 1 (`GetFoodUpgradeLevel`, SM:548-553), so no save migration is needed. It will be one-shot.

---

## 4. Play and Continue

### Play (`Scripts/Buttons/ClickPlay.cs`, MainMenu object `play`)

1. Pointer up. If there is saved progress (active session with `levelsCompleted >= 1` or wallet > 0, :56-62) and the button is not armed, show "This erases your saved game. Tap PLAY again to start over." for 4 s (real time) and stop.
2. Otherwise (no session, a finished session, or a second tap within 4 s): lock, wait 2 s, `StartNewSession`, stop music, transition to GameSceneOne (:125-140).
3. GameSceneOne `LevelManager.Start` loads day index 0.

A finished game (`isActive` false) counts as no progress, so Play starts at once without the warning.

### Continue (`Scripts/Buttons/ClickContinue.cs`, `ContinueButton`)

1. Interactable only if the session is active and `levelsCompleted >= 1` (:29-61), re-checked 0.1 s after enable.
2. Tap: lock, wait 2 s, `ContinueSession` (or `StartNewSession` if the session vanished), stop music, transition to GameSceneOne.
3. `LevelManager.Start` loads `currentLevel` (LM:89-93).

**After Day 14.** The last order of Day 14 calls `OnLevelComplete`, which saves the high score and calls `CompleteSession` at once (LM:563-567). The popup then offers only Main Menu (`SetupLevelCompleteButtons`, LM:661-668); Restart and Next Level are hidden and pausing is blocked from the last order onward (`isLevelEnding`, LM:62, 254, 569). Back in the menu Continue is grey (session inactive), Play starts a fresh game with no warning, and the high score stays. Nothing is soft-locked.

**Saves left pointing past Day 14.** Before commit `2865ea7`, completion happened only at the end of the 7 to 12 s count-up, so tapping Main Menu during it or killing the app left `isActive: true, currentLevel: 14` (the shipped 26.03.29 build has the same flow, so live players can have such a save). Continue then loads index 14: `LoadLevel` now sets `currentLevelIndex = 13` (LM:376) and calls `OnAllLevelsComplete` (LM:687-695), which completes the session and shows the final popup with Main Menu only and "Final Score" (no high-score save; see section 7).

---

## 5. Money edge cases

- **Rounding.** Item points are integers; tips are rounded to cents (ScM:156); the wallet is rounded to cents on every add and subtract (SM:328, 518). `currentScore` is not rounded but only ever sums cent values. `CanAfford` allows 0.005 slack (SM:342).
- **Expired orders** keep the item points served, no tip. An order with nothing served banks nothing.
- **App killed mid-level.** Every finished or expired order was already saved. On relaunch, Continue (or Play's warning, on Day 1) restarts the same day from scratch with the earlier money kept; the new snapshot includes it. Item points of the order in progress at the kill are lost (they were never banked).
- **Pause, Main Menu mid-level.** Same as a kill: the money stays, the day replays on Continue. Only Restart rolls back.
- **Restart from the level-complete popup** rolls back the whole day's money and replays the day; `levelsCompleted` stays.
- **The 3 s gap** between the last order and the popup: pausing is blocked since commit `2865ea7` (LM:254, 569), so the gap can no longer be used to skip the Shop.
- **Shop after the last day** cannot be reached: Next Level is hidden on Day 14.
- **Inactive session**: no money can be earned or spent; the Shop shows nothing owned and refuses purchases.
- **Shop refund** uses `AddScoreImmediately`, which needs an active session; it always has one there because step 1 checked it.
- **Ads** add exactly 5.00 and only on a completed view.

---

## 6. Gotchas

- String identifiers (`Bread`, `Coffee`, `CoffeeMachine`, `Grandma`...) are matched across the Shop items, `SessionData`, `ServeableItem.foodType`, `KitchenFoodGate`, the points table and customer prefab names. The two upgrade items both have `itemName` "Upgrade"; only `upgradeFoodType` matters.
- `ScoreManager.finalScoreText` and `totalScoreText` point at the level-complete `TotalEarned` and `TodaySale` labels. ScoreManager writes "Level Score:" and "Total Score:" into them during play; LevelManager overwrites them when the popup opens. Anything that fires `OnTotalScoreChanged` while the popup is showing would overwrite "Today Sale" with "Total Score: x".
- `totalTipsEarned` is not cleared by `ResetScore`; it is correct only because each day reloads the scene.
- `SnapshotScoreBeforeLevel` dereferences `currentSession` without a null check (SM:155). With no session (GameSceneOne played directly in the editor with no save) it throws and the level start is cut short. Always press Play from MainMenu.
- `ContinueSession` does nothing useful; LevelManager reads the level itself.
- `RewardedAdButton` status labels are not wired; do not rely on them for testing (the checklist item "button shows Loading" cannot be seen).
- The Shop has no Main Menu exit; the checklist item "Return to main menu from the shop" cannot be performed.
- `ShopManager.LoadNextGameLevel` calls `AudioManager.Instance` and `SceneTransitionManager.Instance` without null checks (ShM:330-331); both exist in normal play.
- `LevelManager.debugMode` is 0 in the scene with `debugStartLevel` 13; turning it on also skips `SetupLevelCompleteUI`, `SetupPauseUI` and session registration (LM:73-80).
- `ShopItemPrefab` still serializes a `priceText` field (prefab line 103) that the script no longer has; harmless.

---

## 7. Open issues found while writing this (not fixed; code untouched)

- Legacy past-Day-14 saves: `OnAllLevelsComplete` (LM:687-695) never calls `CheckAndSaveHighScore`, and it does not set `isLevelEnding`, so during the 3 s before its popup the player can pause and tap Restart, which reloads into a session-less Day 1 (all foods free, no money counted, Shop refuses purchases) until they return to the menu.
- "Unlocked Food" on the level-complete popup counts against `totalFoodItems` = 8 in MainMenu, but only 6 foods exist (2 owned + 4 sold); after buying every food it still says 2. The label also shows items still to buy, not items unlocked. Since `2865ea7` the label is blank on Day 14.
- Money earned mid-day is kept when the app is killed or the player goes Pause, Main Menu, while the day replays from the start on Continue. Restart is the only path that rolls back. Repeating part of a day this way farms money.
- Players coming from 26.03.29 have every upgrade at level 1: that build never saved upgrade levels, so there is nothing to restore. Anyone who paid for the Coffee or Coffee Machine upgrade will see it for sale again.
- Rewarded-ad status texts are unwired (3.8). Shop has no Main Menu exit (3.7). Both upgrade items say "Purchased Upgrade!". Apple and Coffee upgrade popup buttons read "350" without a "$".
