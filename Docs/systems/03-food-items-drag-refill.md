# 03 Food items, dragging, table and refill

Checked against the code, `GameSceneOne.unity`, `CoffeeController.controller` and the two brew clips on 7 October 2026. Line numbers are for that date. Nothing here was run on a device or in the editor. Behaviour comes from reading the code and the YAML, plus what Unity's `StandaloneInputModule` and `EventSystem` are documented to do.

The per-item refill numbers (max counts, kitchen sources, delays, sprite steps, points) are in the **Per-item catalogue** in `Docs/maintenance.md`. They are not repeated here.

Abbreviations: SI = `Assets/Scripts/ItemsAndOrders/ServeableItem.cs`, DF = `ItemsAndOrders/DraggableFood.cs`, ID = `ItemsAndOrders/IDraggable.cs`, RI = `Assets/Scripts/Refill/RefillableItem.cs`, RS = `Refill/RefillSystem.cs`, TL = `Assets/Scripts/Utils/TableLayer.cs`, GPM = `Utils/GamePhaseManager.cs`, LM = `LevelControl/LevelManager.cs`, CM = `Food/Coffee/CoffeeMachine.cs`, CO = `Food/Coffee/Coffee.cs`, BC = `Food/Coffee/BeanContainer.cs`, KSM = `Kitchen/KitchenSceneManager.cs`, SES = `Session/SessionManager.cs`. "Scene:N" is a line in `Assets/Scenes/GameSceneOne.unity`.

---

## 1. Input model

### What is in the scene

| Piece | Where | Value |
| --- | --- | --- |
| EventSystem | Scene:3593 | drag threshold 10 px |
| Input module | Scene:3573 | `StandaloneInputModule` (legacy Input Manager, not the Input System package) |
| PhysicsRaycaster | `Camera` (Scene:739) | event mask: everything. Max intersections 0 (no limit) |
| Camera | Scene:700 to 712 | orthographic, size 5, at z = -10 |
| Item colliders | one 3D `BoxCollider` on each food item, the machine and `BeanContainer` | all have **z size 0**. Sizes are listed in section 3 |
| `Canvas` (HUD) | Screen Space Camera, plane distance 100 | its GraphicRaycaster has Blocking Objects = All |
| `ArrangementUI` (Done button) | Screen Space Overlay, sort order 0 | |
| `PopupCanvas` (pause, level complete) | Screen Space Overlay, sort order 1 | GameObject is **active** in the scene. `LM.SetupLevelCompleteUI` (LM:155) turns it off at the end of `LM.Start` |

Food sprites sit near z = 0, about 10 units from the camera. The HUD canvas sits 100 units away, so physics hits always win over HUD graphics. The HUD raycaster's "Blocking Objects = All" also drops any HUD graphic behind a collider. As a result, **a food item placed over a HUD button covers that button's hit area**. The overlay canvases (Done, pause, level complete) always win over everything.

One case matters. The Kitchen button is anchored at the left edge, about 83 reference px above the screen's centre line. Its bottom edge is about 0.04 world units above the cloth top. Item centres can go up to y = -0.3 (section 4). So a tall item (Cake or Apple, half-height 0.6) pushed into the top-left corner can cover the bottom third of the Kitchen button. The top part stays tappable.

`TableBounds` has a 2D `BoxCollider2D`. No `Physics2DRaycaster` exists, so it never takes input. The class summary on SI:7 says "Physics2DRaycaster", which is out of date. The setup is 3D. Do not switch any item to 2D colliders, or its input stops without any error.

### Components that receive pointer events on a food item

- `ServeableItem`: `IPointerDownHandler`, `IPointerUpHandler`, `IPointerClickHandler` (SI:10).
- `DraggableFood`, added at runtime in the first arrangement phase (ID:31, SI:58): `IBeginDragHandler`, `IDragHandler`, `IEndDragHandler`, `IPointerDownHandler`, `IPointerUpHandler` (DF:5).

Unity sends each event to every component on the hit GameObject that implements the handler. Both scripts therefore see every down and up.

### Tap, during play

1. Finger down. `SI.OnPointerDown` (SI:132) records the owner pointer id, even when input is gated. If input is allowed, it calls `RI.OnPointerDown` to start hold detection. `DF.OnPointerDown` (DF:415) returns at once because dragging is off.
2. Finger moves less than 10 px. Nothing happens. If it moves more, `OnBeginDrag` and `OnDrag` go to DF, which ignores them (DF:449, DF:457). The tap stays eligible for a click because the press object and the drag object are the same GameObject.
3. Finger up. `SI.OnPointerUp` (SI:144) checks the owner, records `lastReleasedPointerId`, clears the owner, notes whether the press became a refill (`pressConsumedByRefill`), and calls `RI.OnPointerUp`.
4. Click. Unity sends it only if the finger is still over the same item. `SI.OnPointerClick` (SI:158) requires `pointerId == lastReleasedPointerId`, skips if the press was a refill, re-checks the gates, then serves or rejects (section 2).

Unity's order on release is PointerUp, Click, Drop, EndDrag. DF relies on this (DF:436).

### Hold, during play

Hold-to-refill applies only where `enableHoldToRefill` is on. In the scene that is only Melon, which is inactive. On every playable item a hold is just a slow tap. Lifting the finger over the item serves.

On a hold-enabled item: `RI.OnPointerDown` (RI:279) starts `HoldDetectionCoroutine` (RI:323). After 0.3 s of scaled time it calls `StartRefilling` (RI:342). `isHolding` stays true until the finger lifts, including after the item fills (RI:374). `SI.OnPointerUp` reads `IsRefilling` before calling `RI.OnPointerUp`, so the click that follows is swallowed (SI:153, SI:164).

### Gates

`SI.CanAcceptInput` (SI:124) and `CM.CanAcceptInput` (CM:72) refuse input when:

- serving or brewing is off (arrangement phase),
- `Time.timeScale == 0` (paused), or
- `popupCanvas.activeInHierarchy` (pause or level-complete popup is up).

`ServeableItem.popupCanvas` is None on every item in the scene. SI copies `LevelManager.popupCanvas` in `Start` (SI:88). `CoffeeMachine` copies the field from whichever `ServeableItem` `FindObjectOfType` returns, during its own `Start` (CM:46). If that item's `Start` has not run yet, the machine's copy is null and only the timeScale gate protects it (see Bugs, M-3).

DF only gates on timeScale, and only in `OnPointerDown` (DF:418). A drag already in progress keeps going if the game is paused with another finger.

While the kitchen is open, `KSM.DisableGameInteractions` (KSM:101) turns off every non-Kitchen-layer collider, so no table item can be hit. `KSM.EnableGameInteractions` (KSM:132) turns back on only the colliders it turned off.

During the 3 s between the last order and the level-complete popup (LM:557), nothing is gated. Food taps find no active order and do nothing. A coffee-machine tap still brews.

### Pointer ownership and two fingers

- **SI**: the first finger down owns the item (SI:134). Other fingers' downs and ups are ignored. A click is accepted only from `lastReleasedPointerId` (SI:162). The check compares against the last finger that *released* on this item, not the current owner. So a second finger whose id matches an earlier tap on the same item can still serve while the first finger holds. This is harmless: a tap is a tap.
- **DF**: one finger per item (DF:419, DF:450, DF:458, DF:507). Two fingers can drag two different items at the same time.
- **RI**: a second detection coroutine is refused while a hold is in progress (RI:294).
- **Lost release**: if Unity never delivers an up for the owner (rare on iOS, because cancelled touches count as releases), SI keeps a stale owner. The next tap with the same finger id fixes it, and that tap still serves, because the up and click paths run even though the down was ignored. `OnDisable` also clears ownership (SI:97, DF:572).

### Dragging and serving on the same object

`SetDraggingEnabled(true)` turns DF on and turns serving off (SI:54 to 65). `CoffeeMachine.SetDraggingEnabled` turns brewing off the same way (CM:60). During arrangement, a press-and-release on an item reaches `OnPointerClick`, which is refused by the gate. During play, DF stays attached but inert.

---

## 2. ServeableItem

### Fields and scene values

| Field | Scene | Note |
| --- | --- | --- |
| `foodType` | Bread, Coffee, Juice, Apple, Choux, Cake (Melon inactive) | Must match `OrderSystem.availableFoods`, `SessionData.purchasedFoodItems`, shop `itemName`, `KitchenFoodGate.associatedFoodType` and the ScoreManager table |
| `orderSystem` | assigned on all | `FindObjectOfType` fallback (SI:74) |
| `popupCanvas` | None on all | filled from LM (SI:88) |
| `useAudioManager` | **0 on Bread and Cake**, 1 on the rest | when 0, a wrong tap or an out-of-stock tap is silent (SI:257). See Bugs, m-1 |
| `shakeIntensity`, `shakeDuration`, `shakeCount` | 0.15, 0.4, 3 on all | |
| `serveSound` | Juice only | played in addition to OrderSystem's served sound (OS:370) |
| `enableDebugLogs` | 1 except Juice | `LogControl` filters logs in release builds, but the strings are still built |

Scene lines: Bread 5781, Coffee 1972, Juice 2982, Apple 4721, Choux 1555, Cake 2679.

### Serve path

`OnPointerClick` (SI:158):

1. Out-of-stock check: `RI.IsOutOfStock()` true leads to `OnItemRejected` (SI:172). This check runs before the order check, so an empty item shakes even when no order is active.
2. `OnItemClicked` (SI:184). If there is no active order, the tap is silently ignored (SI:192).
3. `OrderSystem.TryServeItem(foodType)` (OS:345). If the order needs this food: the item is marked, item points are awarded and OrderSystem plays its served sound. SI then plays `serveSound` (if set), calls `RefillSystem.OnItemServed(foodType, true)`, which takes one count off the first registered item with that food type (RS:96, RI:240), and sends `SendMessage("OnServedSuccessfully")`.
4. If the order does not need this food: `OnItemRejected` (SI:220) calls `OnItemServed(false)`, which changes no count. It then shakes and plays the wrong-item sound.

### Feedback

- Shake: `ShakeAnimation` (SI:237) moves `transform.position` right and left `shakeCount` times over `shakeDuration`, using `WaitForSeconds`, so pausing freezes it. A new rejection stops the running shake and snaps back to the rest position first (SI:227). `OnDisable` restores the position (SI:103).
- Wrong sound: `AudioManager.PlayWrongItemSFX`, only when `useAudioManager` is on.
- There is no tint, scale or particle feedback on a serve. The order bubble's effect lives in OrderSystem.

### SendMessage receivers

`OnServedSuccessfully` (SI:217) has **no receiver** anywhere in `Assets/Scripts`. It uses `DontRequireReceiver`, so it costs a reflection lookup per serve and does nothing. Nothing else in the project uses `SendMessage` or `BroadcastMessage`.

### IDraggable and IUpgradeable

- `IDraggable` (ID:3) has default methods. `ServeableItem` and `CoffeeMachine` both define their own `SetDraggingEnabled`, so the default at ID:6 is never used. `HasOverlap` (ID:18) and `InitializeDragging` (ID:24) are the defaults for both, and `InitializeDragging` is where `DraggableFood` is actually created.
- `IUpgradeable` (`IUpgradeable.cs:1`) has one method, `SetUpgradeLevel(int)`. It is implemented by `Coffee` and `CoffeeMachine` only. **Cake is not IUpgradeable** (Cake.cs:22, and the class line 24). `maintenance.md` lists Cake as an upgrade consumer, which is wrong. LM finds implementers with `FindObjectsOfType<MonoBehaviour>().OfType<IUpgradeable>()` (LM:442), so only active objects are found.
- `RandomSprite` (`RandomSprite.cs`) picks a random sprite in `Start`. It is meant for order-display prefabs, not table items.

---

## 3. DraggableFood

DF is in no scene or prefab. `IDraggable.InitializeDragging` adds it (ID:31) from `GPM.EnableArrangementMode` (GPM:183) for every *live* draggable: the six `LevelManager.serveableItems` plus every active `CoffeeMachine` (GPM:161). Inspector defaults therefore always apply: smoothness 0.8, drag Z offset -1, drag scale 1.05, touched tint grey 0.7, overlap tint (1, 0.5, 0.5), wiggle on, overlap prevention on, minimum gap 0, sprite bounds on.

### Lifecycle

- `Awake` (DF:86) captures `originalScale`, `originalZ`, the local position and rotation (wiggle rest pose) and `originalColor`, and computes a radius. It runs when the component is added, **after** `LoadSavedFoodPositions` (GPM:84 runs before GPM:85). The rest pose is therefore the restored position.
- `Initialize` (DF:111) stores the table, the GPM and the `allFoodItems` array, then recomputes the radius, this time through the RefillableItem.
- `SetDraggingEnabled(true)` (DF:169): an immediate overlap check, the 0.1 s overlap loop (DF:226) and the wiggle.
- `SetDraggingEnabled(false)`: finishes an in-progress drag (DF:189), clears touch and ownership, stops the loop and the wiggle (which snaps back to the rest pose), and restores the colour.
- `OnDisable` (DF:572) does the same clean-up without restarting the wiggle.

### Radius (the overlap shape)

`itemRadius = max(width, height) / 2` of the world-space AABB of the item's own `BoxCollider` (DF:158). For food this comes through `RI.GetBoundsWithPadding` (RI:441), which, despite its name, adds no padding (RI:447 and 448 are no-ops). The overlap shape is a **circle centred on `transform.position`**, not on the collider centre. Child colliders are not included.

| Item | BoxCollider (local) | Scale | World box | Radius | Scene default position |
| --- | --- | --- | --- | --- | --- |
| Bread | 3 x 3 | 0.25 | 0.75 x 0.75 | 0.375 | (1.44, -3.98) |
| Coffee | 2.5 x 3.5 | 0.354 x 0.328 | 0.88 x 1.15 | 0.575 | (-0.11, -4.19) |
| Juice | 5 x 5 | 0.18 x 0.2 | 0.9 x 1.0 | 0.5 | (0, -2) |
| Apple | 3 x 3 | 0.4 | 1.2 x 1.2 | 0.6 | (1, -1.11) |
| Choux | 10 x 8, centre (0, -1.3) | 0.1 | 1.0 x 0.8, centre 0.13 below pivot | 0.5 | (-1.3, -0.9) |
| Cake | 3 x 4 | 0.3 | 0.9 x 1.2 | 0.6 | (1.5, -2.4) |
| CoffeeMachine | 3 x 2.5 | 0.3 | 0.9 x 0.75 | 0.45 | (-1.39, -3.76), not saved |
| BeanContainer (child of machine) | 1.5 x 1, centre (-0.8, 2) | inherits 0.3 | 0.45 x 0.3, centre (-1.63, -3.16) | not used for overlap | |
| Melon (inactive) | 11 x 10 | 0.3 | 3.3 x 3.0 | 1.65 | (-1.56, -2.32) |

With every item at its scene default position, no pair overlaps. The tightest pairs are Apple and Cake (0.19 to spare) and Juice and Apple (0.24).

### Overlap detection and the Done button

- `CheckOverlapAtPosition` (DF:328) loops over `allFoodItems`, which is `LevelManager.serveableItems`: the six food items, **not the coffee machine**. It skips self and inactive items, and reports overlap when the 2D centre distance is less than the sum of the radii.
- Effect: the machine's DF checks itself against every food item, but food never checks against the machine. Drag Bread onto the machine and Bread stays its normal colour, while the machine turns red within 0.1 s. Done still greys out, because GPM asks the machine too (see Bugs, m-3).
- State is repainted only when it changes (DF:242). The loop runs every 0.1 s (DF:76). During a drag, `OnDrag` evaluates the *target* position and repaints at once (DF:470).
- Every state change, and every drag end, calls `GPM.OnItemOverlapChanged` (DF:255, DF:502, DF:549). That recomputes `AnyItemsOverlapping` from scratch over all live draggables using `HasOverlap()` at the current positions (GPM:203), and sets `doneButton.interactable` (GPM:225). Because it recomputes from scratch, Done cannot get stuck greyed by a stale flag.
- The first check after phase start is delayed 0.2 s (GPM:133). Until then Done is interactable even if items overlap.
- The wiggle moves items by up to about 0.014 units. Two items resting within that distance of touching can flicker between overlapping and clear every 0.1 s, toggling Done each time.
- Movement is never blocked. Overlap only tints and disables Done (DF:492).

### Drag

- Down (DF:415): owner, touched tint, `offset = position - finger world point` (z forced to the rest Z).
- Begin drag (after 10 px, DF:447): scale x 1.05 and the wiggle pauses (DF:515).
- Drag (DF:455): target = finger + offset, clamped by `TableLayer.ClampToTableBounds` (centre only), then `Lerp(current, target, 0.8)`. Z is set to `originalZ - 1`, so the item draws over items with the same sorting order and is hit first by the raycaster. Each `OnDrag` moves 80 % of the remaining way. The event fires only when the finger moves, so the item ends a drag a fraction of the last step behind the finger.
- End (DF:505, DF:526): scale restored, snap to the last drag position at the rest Z, new wiggle rest pose, wiggle resumes, overlap re-evaluated and repainted, GPM notified.
- Sorting: food sprites use sorting order 2 on layer 3. The machine uses order 0 and the bean container order 1. The drag Z only reorders within the same sorting order, so a dragged machine still draws under the food.

### Wiggle

`WiggleCoroutine` (DF:307) sets the local rotation to ±1° and the local position to ±0.01 every frame from `Time.time`, so it freezes when paused. It writes `localPosition` every frame. Any other code that moves an item while the wiggle runs is overwritten on the next frame. Nothing does this today: the shake is off in arrangement, and saved positions load before DF exists.

### Clamping

Only the item centre is clamped (TL:290). Up to `radius - 0.3` of an item can hang past the table edge: Apple and Cake can hang 0.3 units off the side of the screen. They stay tappable.

### Saved positions

- **Save**: Done calls `SessionManager.UpdateFoodPositions(allFoodItems)` (GPM:249, SES:195). It clears the list, then stores x and y for each *active* `ServeableItem`, then writes PlayerPrefs. The positions are read before `StartPlayPhase` stops the wiggle, so they include the wiggle offset (at most 0.014 units, harmless). **The coffee machine is never saved**. It is not a ServeableItem, so it starts at (-1.39, -3.76) every day.
- **Load**: `GPM.LoadSavedFoodPositions` (GPM:261), for each active item with a saved entry for its food type, sets x and y (keeping the current Z), clamped to the current table bounds (GPM:286). An item with **no saved entry** (just bought) keeps its scene default position.
- `SessionManager.ApplySavedPositions` (SES:215) is unused, and it does not clamp.

### Phase switch

`OnDoneButtonClicked` (GPM:243): guard against a second call, hide the tutorial, save positions, then `StartPlayPhase`. That tells RefillSystem it is PLAYING, then `DisableArrangementMode` calls `SetDraggingEnabled(false)` on every cached draggable, including inactive ones. For food this turns serving back on (SI:64), and for the machine it turns brewing on.

`StartArrangementPhase` runs twice per day: from `LM.StartLevel` (LM:492) and from `GPM.Start` (GPM:67). The second run reuses the DF components and is harmless. The order between the two `Start` methods is not fixed. See Bugs, M-1.

---

## 4. TableLayer

`TableLayer.Awake` (TL:45) runs before every `Start`, so the bounds exist when saved positions are clamped. Scene values (Scene:5912): `tableBoundsPadding` 0.3, `screenCoveragePercent` 0.5, `TableLayer` at the origin with scale 1, `TableCloth` and `TableBounds` as its children.

### Cloth sizing (TL:93)

- Screen height in world units = 2 x orthographic size = 10. Screen width = 10 x aspect.
- The cloth is scaled to exactly the screen width and half the screen height. Its bottom edge sits on the screen bottom (y = -5), so its top is at y = 0. It does **not** respect the safe area (TODO already lists this).
- The code assumes the parent scale is 1, which is true in the scene.

### Drag bounds (TL:176)

```
safe rect (world) = screen rect inset by Screen.safeArea, each edge by its own inset
minX = max(clothMinX, safeMinX) + 0.3      maxX = min(clothMaxX, safeMaxX) - 0.3
minY = max(clothMinY, safeMinY) + 0.3      maxY = min(clothMaxY, safeMaxY) - 0.3
```

A negative span collapses to the centre line (TL:215). The result is cached as `cachedTableBounds` and also written into the `TableBounds` collider for gizmos.

Worked values for item centres:

| Device | Aspect | X range | Y range |
| --- | --- | --- | --- |
| iPhone 14 (1170 x 2532, home-indicator inset 102 px) | 0.462 | -2.01 to 2.01 | -4.30 to -0.30 |
| iPhone SE (750 x 1334, no insets) | 0.562 | -2.51 to 2.51 | -4.70 to -0.30 |
| iPad 3:4 (no insets) | 0.75 | -3.45 to 3.45 | -4.70 to -0.30 |

`targetDevice` is 2 (iPhone and iPad). Every scene default position lies inside the narrowest range. Coffee's y of -4.19 is the closest to an edge. Positions saved on a wider device (an iPad) are pulled in by the load-time clamp. Two items clamped into the same corner can then overlap, which greys Done until the player drags one of them.

The bounds are computed once, in `Awake`. Portrait upside-down is allowed in Player Settings. If an iPad is turned upside down mid-level, the insets swap and the bounds are not recomputed. `RecalculateScaling` (TL:305) exists but nothing calls it, and the same is true of `IsPositionOnTable`, `AddFoodItem` and `RemoveFoodItem`. `initialItemScales` is only used by gizmos.

---

## 5. RefillSystem and RefillableItem

### RefillSystem (Scene:1501: default max 5, refill time 1, logs on)

- `Start` registers every active `RefillableItem` (RS:44), then rescans after 0.5 s (RS:32). `RegisterRefillableItem` (RS:61) refuses duplicates, calls `item.Initialize(this)`, then pushes the current phase (RS:71), so a late registrant still learns the phase.
- `OnGamePhaseChanged` (RS:85) sets `isGameplayMode` on every item. GPM calls it through `NotifyPhaseChange` at the start of each phase (GPM:93, GPM:147).
- `OnItemServed` (RS:96) is ignored outside gameplay. Otherwise it matches the first registered item by trimmed, case-insensitive food type.
- `UnregisterRefillableItem` and the three context-menu methods are never called from code.

### RefillableItem (mechanisms)

- `Awake` caches the `ServeableItem` and `SpriteRenderer` (RI:64). `Start` auto-finds `RefillBar/Fill/Bar` children, which do not exist, then registers itself. If RefillSystem is missing it retries every 0.1 s (RI:126).
- `Initialize` (RI:130): `maxCount = customMaxCount > 0 ? custom : 5`, unless an upgrade override has already happened (RI:136). Refill time is `customRefillTime > 0 ? custom : 1`. `currentCount` is full when `customStartingCount == -1`, otherwise the custom value clamped to the range. It raises **no** `OnCountChanged` event.
- Served (RI:240): ignored when refill is off, outside gameplay or out of stock. A correct serve takes one off and raises the event, and at zero sets `isOutOfStock`.
- Refills: `IncreaseCount(n)` (RI:258, used by the coffee machine) and `RefillToFull` (RI:526, used by the kitchen through each sprite script's `RefillToFull`). Both clear `isOutOfStock` and raise the event only if the count changed.
- `HasSpace(n)` (RI:273) is used only by the coffee machine.
- Hold-to-refill (RI:279 to 411): see section 1. The hold coroutines use scaled time. `SetGameplayMode(false)` and `OnDisable` reset all hold state (RI:166, RI:176). The hold refill loop increments `currentCount` directly and never clears `isOutOfStock` (RI:396). See Bugs, N-1. This only matters if hold-to-refill is ever enabled.
- Bar UI (`SetStatusBarVisible`, `SetStatusBarFill`, `AnimateStatusBar`) does nothing, because `refillBar` and `fill` are null on every item.

### OverrideMaxCount (RI:536)

It is called only by `Coffee.ApplyUpgradeLevel` (CO:61). It sets `maxCount`, sets `hasMaxCountOverride`, and with `customStartingCount == -1` (Coffee's value) sets the count to full. In every case it raises `OnCountChanged`. If `Initialize` runs later it keeps the override (RI:136). So all orders of `LM.Start`, `RI.Start` and `RS.Start` end with Coffee full at 2 (or 3 when upgraded).

### Events

`OnCountChanged(int current, int max)` (RI:35) is a plain `System.Action` field, raised by serve, `IncreaseCount`, hold refill, `RefillToFull` and `OverrideMaxCount`. It is **not** raised by `Initialize`. That is why each sprite script reads the count once on its own.

### Sprite scripts and their subscription lifecycle

| Script | Subscribe | Unsubscribe | First paint | Sprite rule |
| --- | --- | --- | --- | --- |
| Bread, Apple, Juice | `OnEnable`, with -= then += | `OnDisable` | one frame after `OnEnable` | `index = len - 1 - (max - count) / countPerSpriteChange`, clamped |
| Choux | same | same | same | `index = count`, clamped |
| Cake | same | same | same, plus a settings check | `index = count` clamped to 0 to 6. The `Start` check only logs |
| Coffee | `Start`, with -= then += | `OnDestroy` only | in `Start` and on every `SetUpgradeLevel` | `index = count` within the active level's array |

The `OnEnable`/`OnDisable` pattern survives the object being turned off and on. Coffee is subscribed for the whole life of the object. That is fine because Coffee is never turned off while owned.

Coffee's `Start` can paint with counts that are not ready yet (0 of 0, the empty-cup sprite) if it runs before the RefillableItem is initialised. `LM.ApplyUpgradeLevels` always runs and repaints correctly (CO:66), so the wrong sprite lasts at most one frame. The Bread, Apple and Juice formulas need a non-empty array, otherwise they throw. The scene has 4, 5 and 5 sprites. Bread logs a line on every count change (Bread.cs:54), and so does Choux (Choux.cs:41 and 47).

---

## 6. Coffee

### Coffee.cs (Scene:2060)

- `SetUpgradeLevel(level)` (CO:48): level 1 gives max 2 and `level1CoffeeSprites` (3 sprites). Level 2 or more gives max 3 and `level2CoffeeSprites` (4 sprites). It calls `OverrideMaxCount` and then repaints.
- The upgrade key is `"Coffee"` (LM:460, through `ServeableItem.foodType`).
- Coffee's own `RefillableItem` (Scene:1929): custom max 2 (overridden every day by `SetUpgradeLevel`), start full, refill time 0.5 (unused because hold is off).

### CoffeeMachine.cs (Scene:299)

References: `beanContainer`, the child BeanContainer. `coffeeRefillableItem` is Coffee's RefillableItem. `animator` is on the same GameObject. `level2Sprite` is `1-1.png`. The scene sprite is `emptymachine 13.png`. The upgrade key is `"CoffeeMachine"` (LM:464).

States:

| State | `isBrewing` | `brewingEnabled` | Animator |
| --- | --- | --- | --- |
| Arrangement | false | false (CM:62) | disabled in `Start` (CM:52) |
| Idle, play | false | true | disabled |
| Brewing | true | true | enabled, rebound, `BrewingLevel` set and `StartBrewing` triggered (CM:112) |

`OnPointerClick` (CM:81) passes the gates, then `CanBrew` (CM:89): not brewing, at least 1 bean, and room for at least 1 cup.

`StartBrewing` (CM:97) consumes **one bean** for either brew size. It brews 2 cups only when the machine is level 2 or more **and** Coffee has room for 2. Otherwise it brews 1 (CM:105). With no Animator, the cups are delivered immediately (CM:120).

A **tap during brewing** is ignored with no feedback and no queue (CM:91). A tap with no beans or a full Coffee is also silent. While the Coffee item is served, the brew carries on, and the cups land when the events fire.

### Animator (`Assets/Animation/Controllers/CoffeeController.controller`)

- Parameters: `StartBrewing` (trigger) and `BrewingLevel` (int).
- `Idle` (default, no motion) goes to `Brewing_1Cup` (`brew.anim`) when BrewingLevel equals 1 and the trigger is set. It goes to `Brewing_2Cup` (`brew 1.anim`) when BrewingLevel equals 2 and the trigger is set.
- Both transitions have **Has Exit Time at 0.75**. As far as I know, Unity gives an empty state a length of 1 s, so a brew should start about 0.75 s after the tap. This was not measured.
- Both brew states return to Idle at exit time 1.
- The Animator is on Normal update mode (pauses with timeScale 0) and Always Animate culling.

Animation events (all on the machine's GameObject; `CompleteBrewing` is private, which animation events can still call):

| Clip | Length | Events |
| --- | --- | --- |
| `brew.anim` (1 cup) | 5.0 s, loops | 4.967 `OnFinish1Cup`, 5.0 `CompleteBrewing` |
| `brew 1.anim` (2 cups) | 6.917 s, loops | 5.333 `OnFinish1Cup`, 6.667 `OnFinish2Cup`, 6.833 `CompleteBrewing` |

- `OnFinish1Cup` (CM:131) adds one cup. If the brew started as level 1, it completes the brew there.
- `OnFinish2Cup` (CM:151) adds the second cup and completes.
- `CompleteBrewing` (CM:169) clears `isBrewing`, disables the Animator (freezing the last sampled sprite), and on level 2 restores `level2Sprite`.
- Every handler returns early when not brewing, so a looping clip cannot add free cups.
- `brew.anim` ends on the level-1 idle sprite. `brew 1.anim` has a final key at 6.9 s with **no sprite** (fileID 0). Brewing completes at 6.667 s, before that key, so it is never shown in practice.

No path was found that leaves `isBrewing` stuck. The events are redundant (cup event plus complete event), `BrewingLevel` is always 1 or 2, and the machine is never turned off mid-level.

### BeanContainer.cs (Scene:959)

- Starts at 6 beans every scene load (BC:12). There is no save, so beans reset each day.
- 6 sprites; level n shows `sprites[n-1]`. At 0 beans the sprite is set to None, so the container disappears (BC:48).
- The SpriteRenderer colour alpha is 0.5 in the scene.
- `RefillToFull` is called from the kitchen's `CoffeeInKitchen` (CoffeeInKitchen.cs:35), which finds the container with `FindObjectOfType`.
- Its collider sits above the machine. A tap on it reaches the machine through parent bubbling, for both click and drag.

### Upgrades

| Key | Shop | Level 1 | Level 2 | Level 3 (stored, never sold) |
| --- | --- | --- | --- | --- |
| `Coffee` | $350 | max 2, 3 sprites | max 3, 4 sprites | same as 2 |
| `CoffeeMachine` | $600 | 1 cup per bean, scene sprite | 2 cups per bean when there is room, `level2Sprite` | brews 2, but `SetUpgradeLevel` and `RestoreUpgradeSprite` have no case 3, so it keeps the level-1 sprite (CM:187, CM:210) |

---

## 7. Gotchas

1. The colliders are 3D with z = 0 and the raycaster is a PhysicsRaycaster. Do not "fix" either to 2D.
2. Items beat HUD buttons on the camera-space Canvas. An item dragged over the Kitchen button covers part of it.
3. `PopupCanvas` is active in the scene file, and only `LM.Start` turns it off. Anything that stops `LM.Start` before LM:99 leaves every food item refusing taps (Bugs, M-1).
4. `DraggableFood` is runtime-only. Its Inspector values cannot be tuned in the scene. Change the defaults in DF.
5. The overlap shape is a circle (half the longer side of the collider box) centred on the pivot. The machine is checked against food, but food is not checked against the machine.
6. The coffee machine's position is not saved. It resets every day.
7. A newly bought item appears at its scene default position, which may sit under an item the player moved there. The overlap tint and the greyed Done tell the player to move one. There is no text explaining this.
8. `RI.GetBoundsWithPadding` adds no padding.
9. `OnCountChanged` is not raised by `Initialize`. Any new sprite script must read the count itself, a frame after `OnEnable`.
10. Melon is the only hold-to-refill item and it is inactive. Hold-to-refill has never shipped in a reachable form.
11. Bread and Cake have `useAudioManager` off, so a wrong tap on them is silent.
12. `IUpgradeable` implementers are found with `FindObjectsOfType`, so active objects only. An upgradeable thing that starts inactive never gets its level.
13. Choux has `customStartingCount` 4 rather than -1. It equals its max today, but if the max is raised, Choux will start part-empty.
14. Bread logs on every count change and Choux on every change and refill. These calls are filtered in release builds but the strings are still built.

---

## Bugs and risks found in this pass

Severity: **MAJOR** = player-visible broken behaviour, crash, soft-lock or lost progress or money. **MINOR** = visible but with a workaround. **NOTE** = latent or cosmetic.

**M-1 (MAJOR, conditional): null `allFoodItems` when `LevelManager.Start` runs first.** GPM:272 loops over `allFoodItems`, which is only set in `GPM.Start` (GPM:57). `LM.Start` reaches it through `LoadLevel`, then `StartLevel`, then `StartArrangementPhase` (LM:492). On any day with saved positions (day 2 onwards, or a restart after Done), it throws if `LM.Start` runs before `GPM.Start`. The exception aborts the rest of `LM.Start`: `SetupLevelCompleteUI`, `SetupPauseUI`, the session events and the arrangement music. Since the 7 October change copies `popupCanvas` into every item (SI:88), the PopupCanvas would stay active, so **every food item would refuse every tap for the whole day**. No script execution order is set. The shipped build appears to run GPM first, because otherwise the March build's pause buttons and music would have been visibly broken. Unity does not guarantee that order, and it can change when objects are added to the scene or the Unity version changes. Fix (one line): set `allFoodItems` in `GamePhaseManager.Awake`, or fall back to `levelManager.serveableItems` at GPM:272 when it is null. Verify on device: start day 2 and look for `NullReferenceException ... GamePhaseManager.LoadSavedFoodPositions` in the Xcode console. Doc 02 records the same risk.

**m-1 (MINOR): Bread and Cake reject silently.** `useAudioManager` is 0 on Bread (Scene:5788) and Cake (Scene:2686), and SI:257 is the only caller of `PlayWrongItemSFX`. Repro: tap Bread when the order has no Bread. It shakes with no sound, while Juice, Apple, Choux and Coffee buzz. Fix: tick Use Audio Manager on both in the Inspector. Check with the owner first in case it is deliberate.

**m-2 (MINOR): the coffee machine's position is not kept between days.** It is not a ServeableItem, so `UpdateFoodPositions` and `LoadSavedFoodPositions` skip it (SES:195, GPM:272). Repro: day 1, drag the machine to the right and put Bread at (-1.4, -3.8), then Done. Day 2: the machine is back at (-1.39, -3.76), on top of Bread. It shows red and Done is greyed until something is moved. Fix: save the machine under a reserved key such as `"CoffeeMachine"`, or leave it and document it.

**m-3 (MINOR): uneven overlap feedback.** Food checks only food (DF:332). Dragging food onto the machine leaves the dragged item untinted, while the machine turns red and Done greys. The player may not see why. Fix: also check the cached draggables, or pass the machine in with the food list.

**N-1 (NOTE): hold-refill never clears out-of-stock.** RI:396 increments the count without `isOutOfStock = false`. An item emptied and then held back to full still shakes as out of stock, and `OnItemServed` ignores it. This only matters if hold-to-refill is ever turned on (only Melon has it today). Fix: set `isOutOfStock = false` after RI:396.

**N-2 (NOTE): the machine's popup gate depends on Start order.** CM:46 copies `popupCanvas` from an arbitrary ServeableItem, whose own copy may not exist yet. During the 3 s before the level-complete popup and while the popup is up, a machine tap may brew. The only cost is a bean that resets the next day anyway. Fix: read `FindObjectOfType<LevelManager>().popupCanvas` like SI does.

**N-3 (NOTE): Done can flicker.** The wiggle moves items by up to 0.014 units and the overlap loop re-tests every 0.1 s (DF:231), so two items resting almost touching can flip Done on and off.

**N-4 (NOTE): taps on a busy machine give no feedback.** A tap while brewing, out of beans or with Coffee full does nothing (CM:89). The empty bean container simply disappears.

**N-5 (NOTE): smaller dead or stale bits.** `SendMessage("OnServedSuccessfully")` has no receiver (SI:217). The SI summary says Physics2DRaycaster (SI:7). Machine level 3 has no sprite case (CM:187, CM:210). The 2-cup clip has a sprite-None key at 6.9 s that is reached only if both completion events are skipped. `maintenance.md` calls Cake IUpgradeable.

Checked and found **not** to be bugs:

- A newly bought item with no saved position keeps its scene default position. It may overlap a moved item, but both are draggable and Done re-enables as soon as they separate.
- Overlap cannot hold Done greyed for good. `AnyItemsOverlapping` recomputes from positions on every change. All six items and the machine fit easily in the narrowest drag area, 4.0 x 4.0 world units on a notched iPhone. Done is on an overlay canvas that no item can cover.
- Items cannot end up off screen on another device. Saved positions are clamped at load (GPM:286), and every scene default lies inside the narrowest bounds.
- No path was found that leaves an item untappable for the rest of a day. SI ownership heals on the next tap. DF drag state is cleared on Done and `OnDisable`. Hold state is cleared on phase change and `OnDisable`. Kitchen colliders are restored on close or on the next scene load (KSM:178). The machine's `isBrewing` always clears.
