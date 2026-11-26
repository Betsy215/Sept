# System notes index

Short notes per system: where the code is and how it works today. Every file carries the date it was last verified
against the code; when you change a system, correct its note in the same commit and move the date. State, rules and
features in plain words are in `../MANUAL.md` (section 6); numbers players feel are on the report's Mechanisms page.

| File | Read before touching |
| --- | --- |
| [01-scene-flow-and-startup.md](01-scene-flow-and-startup.md) | Scene loads, who goes where, startup order, pause and time scale, tutorials |
| [02-levels-orders-customers.md](02-levels-orders-customers.md) | The day clock and the day's end, the arrangement and play phases, orders, patience, customers |
| [03-food-items-drag-refill.md](03-food-items-drag-refill.md) | Tap and drag input, table items, the fixed station, stock, coffee, ice cream, adding a food |
| [04-kitchen.md](04-kitchen.md) | Opening the kitchen, the kitchen clock and the day's reset, ovens, the choux and cake flow, arranging |
| [05-scoring-session-shop.md](05-scoring-session-shop.md) | Sale and tips, the star rule, stars as currency, the save's fields, the shop and its tabs |
| [06-audio-ui-ads-ios.md](06-audio-ui-ads-ios.md) | AudioManager, runtime UI builders, scene fades, logging, iOS settings the code depends on |
| [07-data-and-identifier-reference.md](07-data-and-identifier-reference.md) | Every string that is matched across code, scenes and the save; PlayerPrefs keys; external ids |
| [08-map-and-chapters.md](08-map-and-chapters.md) | `Chapter`, which chapter is open, the map, day 9, bonus days, adding a chapter |
| [09-ads-analytics-purchases.md](09-ads-analytics-purchases.md) | Starting Unity Ads and its retries, where the between-days ad shows, rewarded ads, analytics events and parameters, Unity IAP |

Overview: `../architecture.md`. Open problems: `../review-2026-11-25.md` and `../TODO.md`.

The deep notes of 7 to 8 October 2026 that these replaced on 26 November 2026 are kept in
`../history/systems_2026-10/` (with `../history/maintenance_2026-10-07.md`). They describe the October game: four
scenes, one customer at a time, days ending on an order count, stars from money, one oven per food. Use them only for
history, or as a starting point for low-level detail of scripts that have changed little since (`ServeableItem`,
`RefillableItem`, `RefillSystem`, `AudioManager`, `SceneTransitionManager`, the coffee animator), and check the code.
