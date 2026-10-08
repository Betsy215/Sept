# System notes index

Deep notes per system, written 7 to 8 October 2026 from the code and scene YAML (file:line references throughout). Read the one for the area you are about to touch before changing it. Line numbers drift as code changes; the method names stay valid.

| File | Read before touching |
| --- | --- |
| [01-scene-flow-and-startup.md](01-scene-flow-and-startup.md) | Scene loads, the persistent singletons, startup order, button flows, events, pause and time scale |
| [02-levels-orders-customers.md](02-levels-orders-customers.md) | LevelManager, the level-complete popup and money count-up, GamePhaseManager, OrderSystem, customers and their animation events, the 14 level assets |
| [03-food-items-drag-refill.md](03-food-items-drag-refill.md) | Tap and drag input, ServeableItem, DraggableFood, TableLayer, RefillSystem, coffee machine and beans |
| [04-kitchen.md](04-kitchen.md) | The additive kitchen scene, food gates, bake and respawn timers, CooldownRegistry |
| [05-scoring-session-shop.md](05-scoring-session-shop.md) | Points and tips, the save file (SessionData) and its JSON, Play and Continue, the Shop purchase flow, adding a shop item |
| [06-audio-ui-ads-ios.md](06-audio-ui-ads-ios.md) | AudioManager, canvases and popups, scene fades, Unity Ads (rewarded, interstitial, tracking prompt), the iOS build pipeline |
| [07-data-and-identifier-reference.md](07-data-and-identifier-reference.md) | Every string key, PlayerPrefs key, layer, animation event, tunable number, and Inspector-wired field |

Higher-level docs: `../architecture.md` (overview), `../maintenance.md` (key facts and the refill catalogue), `../TODO.md` (open items, leave-alone list, known issues), `../ios-testing-and-release.md` (build and release).

Facts the notes were written before, now changed: final-day completion is saved when the last order ends (commit 2865ea7); oven taps on the sticker work (2865ea7); GamePhaseManager fills its food list in Awake (d9ccdee); the tracking prompt waits for the app to be active (90b1b51).
