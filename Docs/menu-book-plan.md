# Menu book: plan

Owner, 14 October 2026: chapter 2 is postponed again; first a menu where the player switches owned foods on and off,
so new chapters can add foods without crowding the table and kitchen. Customers stay as they are (once bought, always
there). Functions first; a simple cute placeholder UI; the real design later.

## Rule

A food is in play when it is **bought** (`purchasedFoodItems`, as today) **and switched on** (`enabledFoodItems`, new
list in `SessionData`; a new field, never a rename). `SessionManager.IsFoodItemActive(f)` = bought and on and not of
a later chapter. Old saves (list null) get every bought food switched on in `LoadSession`.

## Who reads it

| Place | Today | Change |
| --- | --- | --- |
| Table | `LevelManager.ApplyServeableItemSettings` ~467: `purchasedItems.Contains && !IsHeldBack` | `IsFoodItemActive` |
| Kitchen | `KitchenFoodGate.ApplyVisibility`: `IsFoodItemPurchased` | `IsFoodItemActive`; re-run when the book changes |
| Orders | `OrderSystem.UpdateActiveFoodTypes`: active table items | nothing (follows the table); re-run after a change |
| Tutorial Day 0 | forced Bread + Coffee | hide the book on Day 0 |
| Shop | buying adds to bought | also switch on |
| Shop upgrades of a food | `ShopManager` ~262 | unchanged (ownership) |

## Stations and caps

`FoodStation` map in `SessionManager`: oven = Bread, Cake, Choux, Cookies (cap 3); machine = Coffee (cap 1);
counter = Juice, Apple (cap 2). `CanEnable(f)`: the station's cap is not reached. At least two foods on. The kitchen
objects stay one per food for now (hide/show); when a station has more foods than kitchen spots (chapter 2), the
spot shows the food that is on.

## Book

Opens from a button beside Ready during the arrangement phase (`GamePhaseManager`, `ArrangementUI`), built in code
like `MapScreen` (no scene edits): a cream card, one row per bought food (icon from the order prefab, name, on/off),
the station's count ("Oven 3/3"), greyed rows when the cap is reached. A switch applies at once: table
(`ApplyServeableItemSettings`, saved positions keep), kitchen gates, orders. Done keeps its overlap check.

## Steps

1. Save list, `IsFoodItemActive`, migration, purchase switches on. **Done 14 October.**
2. Gates: table, kitchen, orders (follow the table). **Done 14 October.**
3. Stations and caps. **Done 14 October.**
4. The book panel (placeholder art, `Assets/Scripts/Utils/MenuBook.cs`, button `Resources/UI/menu_book.png`). **Done 14 October.**
5. Cookies: a chapter 1 shop buy again (600); off when bought while the oven is full. **Done 14 October.** (Owner's first idea was "owned by everyone"; kept as a buy so the shop still has something to sell.)
6. Driver run (14 October, running), old-save fixture, iPad captures, and the Day 0 tutorial still skips the book. The coffee machine goes with Coffee (owner, 14 October): off in the book, off the table.

## Owner, 14 October afternoon

- Oven stickers: each baked food's sticky note on its oven shows when the food is on and hides when it is off
  (`KitchenFoodGate` on sticker1/2/3; Cookies get a fourth note, three designs in `Store/ui/stickers`, A installed as
  `Resources/UI/stickers/Cookies.png` until the owner picks).
- The book is a grid (three tiles across), the baked foods' icons are their stickers (`Resources/UI/stickers/<food>`,
  copies of the owner's notes), the others keep their table picture.
- Level complete panel: design A (the order pad with the gingham band, `Store/ui/day_receipt/idea_A.png`) replaces
  `Assets/Images/paper.png` (original kept as `paper_original.png`), to match the order receipt.
