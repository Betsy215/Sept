# 03 Food items, dragging, table and stock

Last verified against the code: 26 November 2026. Paths are under `Assets/Scripts`.
(The October 2026 deep note is in `Docs/history/systems_2026-10/`. Its line-by-line account of `ServeableItem`,
`DraggableFood`, `RefillableItem` and the coffee animator was written for code that has changed little since, but it
was not re-verified; its food list, kitchen sources and scene line numbers are out of date.)

## Input

- GameSceneOne's camera carries a `PhysicsRaycaster`; table items have 3D `BoxCollider`s (the candy jar's is added at
  runtime). Handlers are the EventSystem interfaces (`IPointerDown/Up/Click`, drag interfaces). No `OnMouse*`.
- While the kitchen is open, `KitchenSceneManager.DisableGameInteractions` switches off every enabled collider
  outside the `Kitchen` layer and switches the same ones back on at close.

## A food on the table: `ItemsAndOrders/ServeableItem.cs`

- `foodType` is the string everything matches on (`systems/07`).
- A tap serves: `OnPointerClick` → out of stock (`RefillableItem.IsOutOfStock`) shakes; else
  `OrderSystem.TryServeItem(foodType)`; success → `RefillSystem.OnItemServed(foodType, true)` takes one from the
  stock; a food no open order needs shakes and plays the wrong-item sound (only when `useAudioManager` is on).
- Input is refused (`CanAcceptInput`) while serving is off (arrangement), while `Time.timeScale == 0`, while the popup
  canvas is active, or when `inputHandledElsewhere` is set (the ice cream station handles its own taps).
- One finger owns an item at a time; a click counts only from the finger that last released on it.
- `popupCanvas` is copied from `LevelManager.popupCanvas` in `Start` when it is not assigned.

Which foods are on the table: `LevelManager.ApplyServeableItemSettings` shows an item when
`SessionManager.IsFoodItemActive(food)` (bought, switched on in the menu book, in season for the chapter);
`MenuBook` changes re-run it through `ReapplyServeableItems`.

## Dragging: `ItemsAndOrders/DraggableFood.cs`

- Added at runtime by `ServeableItem.SetDraggingEnabled(true)` (`IDraggable`); it is in no scene or prefab. Dragging
  is on only in the arrangement phase, and switching it on switches serving off.
- Items wiggle while arranging; an item overlapping another turns red and keeps Ready off
  (`GamePhaseManager.AnyItemsOverlapping`, `UpdateDoneButtonState`).
- Positions are saved per food type in `SessionData.savedFoodPositions` when the day starts
  (`SessionManager.UpdateFoodPositions`) and applied at the next arrangement (`LoadSavedFoodPositions`);
  `KeepRectOnTable` then pulls each item's whole box inside the drag area.

## The table: `Utils/TableLayer.cs`

Sizes the cloth to the camera and safe area once at start, defines the drag bounds (`InteractiveBounds`,
`ClampToTableBounds`) and draws the cloth's front drop (`UpdateClothFront`). Set the Game view size before Play.

## The fixed station: `Utils/FixedStation.cs`, `Utils/StationGroup.cs`

`SessionManager.FixedFood` names each chapter's fixed food: Coffee (chapter 1), IceCream (chapter 2).
`GamePhaseManager.StartArrangementPhase` calls `FixedStation.Place` (machine and cups in a row on a tray, bottom
left) and `PushOthersOff`. The station moves as one piece: the handle (`FixedStation.IsHandle`: the coffee machine,
or the ice cream station) is dragged and `StationGroup` moves the others with it.

## Stock: `Refill/RefillableItem.cs`, `Refill/RefillSystem.cs`

- Each food has a count and a maximum (`customMaxCount`, 0 = the system default; `customStartingCount` -1 = start
  full). A correct serve takes one; at zero the item is out of stock.
- Stock comes back from the kitchen (`RefillToFull`, `IncreaseCount`, called by the kitchen scripts in `systems/04`)
  or, for coffee, from the table's coffee machine.
- Hold-to-refill code exists (`enableHoldToRefill`) but is on for one object in GameSceneOne (the unused Melon item).
  Owner's decision: leave it (TODO, Leave alone).

## Coffee: `Food/Coffee/*`

`CoffeeMachine` (on the table, `IDraggable`, `IUpgradeable`): a tap brews when `BeanContainer` has beans; the brew
animation's events (`OnFinish1Cup`, `OnFinish2Cup`, then `CompleteBrewing`) add cups to the `Coffee` item. Beans (max 6) are refilled by the
coffee bags in the kitchen (`Food/FoodInKitchen/CoffeeInKitchen.cs`). Upgrade levels `Coffee` and `CoffeeMachine`
come from the save (`SessionManager.GetFoodUpgradeLevel`) and are applied by `LevelManager.ApplyUpgradeLevels`
through `IUpgradeable.SetUpgradeLevel`.

## Chapter 2 foods

- `Food/IceCreamStation.cs` (on the table, foodType `IceCream`): wells per flavour (`Flavours` 4, `BaseFlavours` 2
  until the `IceCream` upgrade level 2), `PerSection` 4 scoops a well, dishes under the box (`DishesBase` 4,
  `DishesFull` 6), up to `MaxScoops` 3 scoops a dish. The table's waffle machine bakes `WafflesPerSession` 2 bowls in
  `WaffleSeconds` 6 s; this wait is not multiplied by `KitchenSpeed.Factor`. Orders carry a variant string (sorted
  flavour letters from `"vscm"`, plus `w` for a waffle bowl); sale points are per scoop
  (`ScoreManager.AwardItemPointsValue(food, scoops)`). The kitchen blender refills every well.
- `Food/Coconut.cs` with `Food/FoodInKitchen/CoconutInKitchen.cs` (the palm in the chapter 2 kitchen window).

## Adding a food

Table object with `ServeableItem` + `RefillableItem` in `LevelManager.serveableItems`; an entry in
`OrderSystem.availableFoods`; its price in the `ScoreManager` points table; `SessionManager.FoodStation` (and
`FoodChapter` / `UnlockChapter` if chapter-bound); a kitchen source; a shop card; a menu sticker
(`Resources/UI/stickers/<food>`); a cookie picture (`Resources/Cookies/<card name>`).
