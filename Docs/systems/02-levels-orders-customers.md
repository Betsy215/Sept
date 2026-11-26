# 02 Days, orders and customers

Last verified against the code: 26 November 2026. Paths are under `Assets/Scripts`. Per-day numbers (lengths,
beats, order sizes) are on the report's Mechanisms page, not here.
(The October 2026 deep note is in `Docs/history/systems_2026-10/`; it describes one customer at a time, days that
end on an order count and 14 level assets.)

## Day data

`LevelControl/LevelData.cs` (assets `Assets/Entity/Level<N>Data.asset`). `LevelManager.allLevels` in GameSceneOne holds
28 assets: Level1..12 (chapter 1), Level17..32 (chapter 2), indexed by the running day index (chapter 2's first day
is index 12). Level13..16 exist but are not in the list.

| Field | Read by |
| --- | --- |
| `dayDurationSeconds` | `LevelManager` day clock |
| `timeBetweenOrders` | `CustomerManager.SpawnLoop` (seconds between arrivals) |
| `orderDisplayTime` | patience per order, before the multipliers below |
| `minOrderItems`, `maxOrderItems` | order size |
| `ordersPerLevel` | only the no-customer fallback flow and `DayNightTint`'s fallback; it does not end a day |
| `starThreshold1..3` | nothing that decides stars (`StarRule` does) |

## The day (`LevelControl/LevelManager.cs`)

1. `LoadLevel` → `ApplyLevelSettings` → `StartLevel`: arrangement phase.
2. `Utils/GamePhaseManager.cs`: `StartArrangementPhase` enables dragging and disables `OrderSystem` and
   `CustomerManager`. Day index 0 skips it (`SkipsArrangement`). Otherwise `OnDoneButtonClicked` needs both places
   ready: Ready on the table (no overlapping items) opens the kitchen, Ready in the kitchen (no red piece,
   `KitchenArrange.AnyRed`) closes it; then food positions are saved, `KitchenArrange.Commit()`, `StartPlayPhase`.
3. `StartPlayPhase` logs `dayStarted`, enables the two systems and calls `LevelManager.StartGamePlay`: the clock
   starts at `dayDurationSeconds`, `YogaHire.StartDay`, the first customer is spawned.
4. `LevelManager.Update` counts the clock down (not while `OrderSystem.freezeTimer`). `TryEndDay` ends the day when
   the clock is at 0 and there is no open order and no customer on screen; it is called from the clock, from
   `OnOrderClosed` and from `OnCustomerLeft`.
5. `OnLevelComplete`: `SessionManager.OnLevelCompleted`, `StarProgressBar.CloseDay()`, `RecordLevelStars` (pays
   `ThreeStarBonus` 100 once per day), `AnalyticsLog.DayFinished`, `KitchenDay.End()`, both systems disabled; 3 s later
   `ShowLevelCompletePopup` shows the day-end card and counts sale and tips into "Earned".

`DayNightTint` reads the clock: dusk at 65 % of the day, night at 85 %.
A day that was already finished pays `ReplayPayFactor` 0.5 of its order money (`ScoreManager.payFactor`).

## Orders (`ItemsAndOrders/OrderSystem.cs`)

- `maxConcurrentOrders` is 2 in the scene: two slots, each with its own bubble set and `ActiveOrder` (items, timer,
  `displayTime`, `tipMultiplier`, `startedAt`).
- `StartOrderForCustomer`: the size is random between the customer's minimum (clamped) and `maxOrderItems`; each item
  is drawn from `activeFoodTypes` with repeats allowed. `forcedNextOrder` (tutorials) replaces the draw once.
  `activeFoodTypes` = the active table items that have an entry in `availableFoods` (rebuilt in `OnEnable`).
- The customer thinks first (`ThinkSeconds` 2 s, three dots): patience does not run and nothing can be served to
  that order. Not for forced orders.
- `TryServeItem(food, variant)`: goes to the oldest open order (by `startedAt`) that still needs the food; variant is
  only used by ice cream. A serve adds the item's points to the order, refills patience up to the next whole flower
  (`RefillPatience`, `PatienceMeter.OnItemServed`) and completes the order when everything is served.
- `CompleteOrder`: tip level from the time left (`SimpleScorePopup.LevelFor`), `ScoreManager.AwardOrderCompletionBonus`,
  `customersServed++`. `ExpireOrder` (timer at 0): the points of what was served are still paid, no tip,
  `customersLost++` (and `customersLostPartly` when something was served).
- Patience flowers: `Customer/PatienceMeter.cs`, `BaseFlowers` 3, `Chapter2Flowers` 4.

## Customers (`Customer/CustomerManager.cs`)

- `SpawnLoop`: waits for a free slot (two at most) with nobody still walking in, spawns one, waits
  `timeBetweenOrders`; stops when `LevelManager.DayOver`.
- `SelectCustomerForLevel`: random among the prefabs whose name (minus "Customer"/"Prefab") is in
  `purchasedCharacters`, never the one already standing in the other slot; the Girl while a tutorial runs.
- `OnCustomerReachedService` works out the order's values, then `HandleCustomerOrderDelay` waits the customer's
  `OrderDelay` and calls `StartOrderForCustomer`:
  - minimum items = `max(level minimum + ExtraOrderItems, MinOrderItems)`, clamped to the level maximum;
  - time = `orderDisplayTime` × `OrderTimeMultiplier` × decor bonus (`SessionManager.PatienceMultiplier`: +15 % per
    entry in `purchasedDecor`) × chapter bonus (4/3 in chapter 2, with the fourth flower).

| Customer | Class | Differs from the base |
| --- | --- | --- |
| Girl, Boy, Kid | `GirlCustomer`, `BoyCustomer`, `KidCustomer` | nothing (the Kid's minimum of 4 was removed on 25 November 2026) |
| Grandma | `GrandmaCustomer` | `TipMultiplier` 2 |
| Businessman | `BusinessmanCustomer` | `TipMultiplier` 3, `OrderTimeMultiplier` 0.75 |
| Yoga | `YogaCustomer` | `OrderTimeMultiplier` 1.5 |
| Chef | `ChefCustomer` | `MinOrderItems` 99, so always the day's maximum |

Girl, Boy and Kid are owned from the start (`SessionData` constructor); the others are shop cards.
The hired yoga lady in the background (`Customer/YogaHire.cs`, a star-shop item) is a different thing from the Yoga
customer.

## Gotchas

- `GamePhaseManager` toggles `OrderSystem.enabled`; its `OnEnable` rebuilds the food list, so the list reflects the
  table when play starts.
- A customer who leaves with part of the order served counts as lost for the stars but that part is paid.
- `OrderSystem.perfectTimeFraction` and `LevelData.ordersPerLevel` are still serialized; neither drives play.
