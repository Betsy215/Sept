# 05 Money, stars, the save and the shop

Last verified against the code: 26 November 2026. Paths are under `Assets/Scripts`. Prices and sale values are on
the report's Mechanisms page; the cards' prices live in `Assets/Scenes/Shop.unity`.
(The October 2026 deep note is in `Docs/history/systems_2026-10/`; it describes stars from money thresholds, a
nine-card shop, a far smaller save and a Shop that led straight to the next day.)

## Money (`Scoring/ScoreManager.cs`)

- Sale price per item: the points table on the `ScoreManager` in GameSceneOne (an unlisted food pays 0). Ice cream
  pays per scoop (`AwardItemPointsValue(food, scoops)`).
- Tip, when an order is completed: `item points × seconds left × timeBonusMultiplier / 100 × customer tip
  multiplier`, rounded to the nearest whole dollar when earned (`Money.Whole`, a half goes up; it was cents until
  26 November 2026); `timeBonusMultiplier` is 10 in the scene.
- The wallet (`SessionData.totalScore`) is paid per order, not per item: on completion `Whole(items × payFactor) +
  Whole(tip × payFactor)`, on expiry `Whole(items served × payFactor)`, through `SessionManager.AddScoreImmediately`.
  `payFactor` is 0.5 on a replayed day (`LevelManager.ReplayPayFactor`), so the sale and the tip are each rounded to
  a whole dollar by themselves (7.50 → 8). Example: a $45 order with an $18 tip pays 45 + 18 on a first play and
  23 + 9 on a replay.
- The same two numbers go to the receipt over the customer, to the wallet, and into `ScoreManager.PaidSaleToday` /
  `PaidTipsToday`; the day-end card's Order sale and Tips lines read those sums, so the card is the sum of the day's
  receipts. The level score (`GetCurrentScore`, analytics `dayEarnings`) keeps the full value on a replay.
- Three stars pay `LevelManager.ThreeStarBonus` (100) once per day (`SessionManager.RecordLevelStars`,
  `starBonusPaidDays`).
- Restart restores the wallet to the snapshot taken at the day's start (`SnapshotScoreBeforeLevel`, memory only) and
  re-adds a three-star bonus already paid. The pause menu's Home (`GoToMap`) does not restore it.
- No cents at all (owner, 26 November 2026): every amount that reaches the wallet is whole dollars (item prices,
  rounded tips and replay pay, the 100 bonus, Daily Treats, the coin ad's 50, shop prices). A save from before can
  hold cents: `LoadSession` floors `totalScore` once. The wallet is still a float that goes through
  `SessionManager.RoundToCents`; compare with `CanAfford`.
- Every money text goes through `Money.Format` (`Utils/Money.cs`): "$ 1,814" (dollar sign, space, thousands comma;
  a part of a dollar is cut off, which only the day-end count-up has). Used by the hen (`MoneyFlipClock`), the
  receipt (`SimpleScorePopup`), the day-end card (`LevelManager`), the shop's EARNED line, its "Not enough points"
  message and the CoinStar card (`ShopManager`), and the map pill (`MapScreen`). Prices typed in `Shop.unity`
  ("$ 300", "$ 1400") and the painted menu stickers are not formatted by code.
- The receipt's tip level (`Scoring/SimpleScorePopup.cs` `LevelFor`): Perfect at 70 % or more of the order's time
  left, Nice at 40 % or more, else OK.

## Stars (`Scoring/StarRule.cs`, `Scoring/StarProgressBar.cs`)

Stars come from customers, not money. A customer counts when the order closes: served whole (`CustomersServed`) or
lost (`CustomersLost`, including those served in part).

- `StarRule.Stars(served, lost)`: nobody lost = 3; served ≥ `Needed(customers, TwoStarPercent)` = 2, with
  `TwoStarPercent` 80 and `Needed = (customers × percent + 49) / 100` in whole numbers; served more than half = 1;
  else 0. Before any order closes the day stands at three stars.
- During the day the hen board shows the projected result (`StarsSoFar` with `StillToCome` from the pace so far).
  `StarProgressBar.CloseDay()` switches to the exact count; `LevelManager.OnLevelComplete` calls it before reading
  `GetEarnedStarsCount()`.
- `SessionData.levelStars[dayIndex]` keeps the best result per day.
- Stars are also a currency: `SessionManager.StarsAvailable` = sum of `levelStars` + `starsBought` (the 15 Stars
  purchase) + `starsGranted` (the day 9 gift, the test button) + `starsFromCoins` (the `CoinStar` card) − `starsSpent`.
  A new game keeps only `starsBought` (`StartNewSession`).

## The save (`Session/SessionManager.cs`)

One `SessionData` object, JSON in the PlayerPrefs key `FoodTruckSession`, written by `SaveSession()` after every
change. Fields, by group:

| Group | Fields |
| --- | --- |
| Progress | `totalScore`, `currentLevel` (running day index), `levelsCompleted` (unbroken run from day 1), `chapterDone` (days done per chapter), `levelStars`, `starBonusPaidDays`, `isActive`, `sessionStartTime` |
| Owned | `purchasedFoodItems` (starts Bread, Coffee, Apple), `purchasedCharacters` (starts Girl, Boy, Kid), `purchasedDecor`, `foodUpgradeKeys` + `foodUpgradeValues` (the upgrade dictionary: Coffee, CoffeeMachine, Oven, IceCream), `candies` |
| Menu and places | `enabledFoodItems`, `savedFoodPositions`, `menuStickerPositions`, `kitchenPlaces`, `kitchenLayout`, `handleTableY/KitchenY`, `handleTableSet/KitchenSet`, `kitchenAutoPlaced` |
| Stars as currency | `starItems`, `starsSpent`, `starsBought`, `starsGranted`, `starsFromCoins` |
| Seen flags | `tutorialSeen`, `tutorial2Seen`, `chapter2GiftSeen`, `arrangeTutorialSeen`, `menuTutorialSeen`, `candyGiftSeen`, `chapter2Claimed`, `day9StarGiven`, `chapter2MapSeen`, `bonusShown` |
| NEW tags and journal | `newThings`, `newInBook`, `journal`, `journalNew`, `journalFilled`, `journalShown` |

- Old saves: `LoadSession` fills missing lists, switches every bought food on when `enabledFoodItems` is missing and
  adds Apple; `OvensOwned` derives a missing `Oven` level. New fields must tolerate being absent (null or 0).
- Progress helpers: `DaysDoneIn(chapter)`, `IsDayDone(dayIndex)`, `NextDayIn(chapter)`, `OnLevelCompleted`
  (a replay never moves progress back).
- Foods in play: `IsFoodItemActive` = bought and enabled and in season (`FoodChapter`: Apple and Coffee chapter 1,
  IceCream and Coconut chapter 2). `UnlockChapter` (Cookies, Coconut: 2) locks shop cards until
  `ChaptersOpen` reaches the chapter. At least `MinEnabledFoods` 2 stay on.
- High score: `FoodTruckHighScore`, checked at a chapter's last day.

## The shop (`Shop/ShopManager.cs`)

- Opened by the day-end card's Next; its Next (`LoadNextGameLevel`) opens the Map.
- Cards are `ShopItemController`s (`itemName`, `price`, `itemType` Food / Character / Upgrade / Decor,
  `upgradeFoodType`). Scene cards are `ShopItemPrefab` instances; `ShopManager.Start` also builds runtime cards by
  cloning the Candies card: Remove Ads, 15 Stars, the star cards (yoga lady, Chef and Dining table locked, Kitchen
  upgrade) and the coin-to-star card (`CoinStar`, `CoinStarPrice` 999).
- Tabs (`TabOf`): `RemoveAds` and `DollarStars` → $; a card whose `itemName` or GameObject name starts with `Star` →
  Stars (paid with `BuyWithStars`); everything else → Coins. The $ tab shows only while the App Store offers a
  product (`DollarTabOffered`).
- `TryPurchaseItem` for coins: checks (owned, upgrade maxed, locked, candy jar full, affordable), then pays first
  (`DeductScore`) and records (`PurchaseFoodItem`, `PurchaseCharacter`, `UpgradeFood`, `BuyCandies`,
  `BuyStarWithCoins`, `PurchaseDecor`); a failed record refunds. `ConfirmPurchase` logs `shopPurchase` on success.
- Owned things leave the shop for the book (`IsOwned`, `LeavesShop`, `ShopCookies`); candies, the star pack and the
  coin-star card stay, and the oven card stays until three ovens are owned (`OvenStillSells`).
- Unity IAP starts here: `EnsureRemoveAdsCard` / `EnsureStarPackCard` call `RemoveAds.Init()` (`systems/09`).
- `Utils/RatingPrompt.cs` asks for Apple's rating popup once, when the Shop loads with three or more days in the
  unbroken run finished.
- `Shop/ShopFlourish.cs` keeps the NEW tags (PlayerPrefs `ftc_shop_seen_*`).

## Gotchas

- `PatienceMultiplier` counts every entry in `purchasedDecor`, so only the two flower pots may go through
  `PurchaseDecor`.
- The two coffee upgrade cards both have `itemName` `Upgrade` (they differ by `upgradeFoodType` and object name).
- Three `ShopManager` methods are called from Shop.unity: `LoadNextGameLevel`, `ConfirmPurchase`, `CancelPurchase`.
