# 07 Identifier reference

Last verified against the code: 26 November 2026. Strings that are matched across scripts, scenes and the save.
Grep the whole project (scripts **and** `Assets/Scenes/*.unity`) before changing any of them. Tunable numbers are on
the report's Mechanisms page.
(The October 2026 version, with scene line numbers and animator tables, is in `Docs/history/systems_2026-10/`.)

## Foods

`ServeableItem.foodType` on the table, `OrderSystem.availableFoods`, the `ScoreManager` points table,
`SessionData.purchasedFoodItems` / `enabledFoodItems`, shop card `itemName`, `KitchenFoodGate.associatedFoodType`,
`SessionManager.FoodStation` / `FoodChapter` / `UnlockChapter`, sticker and cookie picture names.

| String | Station (`FoodStation`) | Chapter rule | Kitchen source |
| --- | --- | --- | --- |
| `Bread` | Oven | all; owned from the start | oven |
| `Coffee` | Machine | chapter 1 only (`FoodChapter`); owned from the start; chapter 1's fixed food | table machine, beans from the kitchen bags |
| `Apple` | Counter | chapter 1 only; owned from the start | apple tree |
| `Juice` | Counter | all | juice in the kitchen |
| `Choux`, `Cake` | Oven | all | oven, then `KitchenFlow` |
| `Cookies` | Oven | card locked until chapter 2 is open (`UnlockChapter`), then plays anywhere | oven |
| `Coconut` | Counter | chapter 2 only; card locked until chapter 2 is open | palm in the chapter 2 kitchen |
| `IceCream` | Machine | chapter 2 only; chapter 2's fixed food | table station, wells refilled by the blender |

The points table also holds `Melon`, `Pie`, `Mont Blanc` and `Log Cake`, which no playable item uses; a `Melon`
object exists in GameSceneOne.

## Customers

`SessionData.purchasedCharacters` against the prefab name with "Customer" and "Prefab" removed:
`Girl`, `Boy`, `Kid` (owned from the start), `Grandma`, `Chef`, `Yoga`, `Businessman`.

## Upgrades, decor, star items

| Kind | Strings | Where kept |
| --- | --- | --- |
| Upgrade levels (`upgradeFoodType`) | `Coffee`, `CoffeeMachine`, `Oven` (ovens owned, 1 to 3), `IceCream` (2 = four flavours) | `foodUpgradeKeys` / `foodUpgradeValues` |
| Decor | `PotGreen`, `PotFlowers` (`DecorGate.decorName`) | `purchasedDecor` |
| Candies | card `Candies` (type Decor, never recorded as decor) | `candies` |
| Star items | `StarYoga` (`YogaHire.ItemName`), `StarKitchen` (`KitchenSpeed.ItemName`), `StarChef`, `StarTable` (both locked) | `starItems` |

## Shop cards

Scene cards (GameObject name; `itemName` where it differs): `Apple`, `Juice`, `Choux`, `Cake`, `Cookies`, `Coconut`,
`Grandma`, `Chef`, `Yoga`, `Businessman`, `PotGreen`, `PotFlowers`, `Candies`, `CoffeeUpgrade` and
`CoffeeMachineUpgrade` (both `itemName` `Upgrade`), `OvenUpgrade`, `IceCreamUpgrade`.
Runtime cards (`ShopManager`): `RemoveAds`, `DollarStars`, `CoinStar`, and the star items above.
A name that starts with `Star` puts a card on the star tab. NEW-tag and book keys are
`<ItemType>:<itemName or upgradeFoodType>` (`ShopManager.KeyOf`), e.g. `Food:Juice`, `Upgrade:Oven`.
Cookie pictures: `Resources/Cookies/<GameObject name>` (falling back to `itemName`).

## Journal pages (`SessionManager.Journal*`)

`open`, `star3`, `icecream`, `cake`, `choux`, `machine`, `kitchen`; stored as `id|day`.

## PlayerPrefs keys

| Key | Owner | Holds |
| --- | --- | --- |
| `FoodTruckSession` | `SessionManager` | The save (JSON `SessionData`) |
| `FoodTruckHighScore` | `SessionManager` | High score (float) |
| `GlobalAudioState`, `GlobalMusicState` | `SettingsButtonsController` | Sound and music switches |
| `ftc_ads_removed` | `RemoveAds` | Remove Ads owned |
| `ftc_starpack_done` | `RemoveAdsStore` | Last 20 paid 15 Stars transaction ids, `|`-separated |
| `ftc.daily.last`, `ftc.daily.streak`, `ftc.daily.pending` | `DailyTreats` | Last collect date, row day, coins waiting for Continue / new game |
| `ftc_shop_seen_<card>`, `ftc_shop_seen_init` | `ShopFlourish` | NEW tags already seen |
| `ftc.rating.asked` | `RatingPrompt` | The rating popup was asked for |

A new game replaces `FoodTruckSession` only; every other key survives it.

## Scenes, layers, external ids

- Scenes: `MainMenu`, `Map` (`MapScreen.SceneName`), `GameSceneOne` (`TutorialDirector.GameScene`), `Shop`,
  `KitchenScene` (`KitchenSceneManager.kitchenSceneName`). Scene names are passed as strings.
- Layer `Kitchen`: everything tappable in the kitchen; other colliders are disabled while it is open.
- Unity Ads: game id `6074412` (`AdsInitializer._iOSGameId`), units `Interstitial_iOS`, `Rewarded_iOS`.
- IAP products: `com.BetzzzGame.FoodTruckCafe.removeads`, `com.BetzzzGame.FoodTruckCafe.stars15`.
- App Store id `6755611355` (`UpdateGate.AppId`).
- Analytics event and parameter names: `systems/09`.
- Oven state keys: `"Oven:<parent name>/<object name>"` (`OvenKitchenBase`).

## Methods called from scene files

Shop.unity buttons: `ShopManager.LoadNextGameLevel`, `ConfirmPurchase`, `CancelPurchase`. Animation events in
`Assets/Animation/Clips/*.anim` (methods found by name): `CoffeeMachine.OnFinish1Cup`, `OnFinish2Cup`,
`CompleteBrewing` (the brew clips) and `CustomerController.OnReachedServicePoint` (a walk clip). Serialized field
names on MonoBehaviours are wired in scenes and prefabs: add fields, never rename.
