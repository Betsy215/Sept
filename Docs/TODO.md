# TODO

Open items as of 24 November 2026. The report's Next and MVP hold the owner's current asks; this file holds what is
left over in the code and the store. Done items are removed (git history and `Docs/history/` keep the record).

## Now

- [ ] **2026.11.24 (build 1) in review** (AFTER_APPROVAL): watch `asc_release.py 2026.11.24 status`; once live, the next
  test build takes the next day's date. Update MANUAL section 1.
- [ ] **C chapter (chapter 3) art brainstorm**: read the ticks in the report's Current, then the ChatGPT restyles in
  `Store/cchapter_2026-11-24c/prompts.md` (MANUAL section 10, memory note `cchapter-indoor-cafe`). Nothing in code yet.
- [ ] **Privacy label**: confirm with the owner that App Privacy lists Usage Data / Product Interaction (not linked, not
  tracking) for Unity Analytics (in the game since 16 October 2026; no API, the owner sets it in App Store Connect).

## Ideas, not started (owner's backlog)

- [ ] Shop item: a clock on the table's back band that shows the day's time left (owner, 17 October 2026).
- [ ] Roll cake (owner's spec of 15 November 2026: the oven bakes a roll, a tap cuts a slice, serve cut slices; memory
  note `roll-cake-feature`); art waits on the owner.
- [ ] Locked star cards Chef (20) and Dining table (25) say "Coming soon": what they do is the owner's to decide.
- [ ] Marketing: keywords, an Apple Search Ads test, short-video clips, cozy-game communities, a featuring request;
  iPhone Duo screenshots before April 2027 (memory note `app-store-page-aso`).
- [ ] Merge Unity's SKAdNetwork id list into `Assets/Editor/IOSPostBuild.cs` (not done; Unity Ads may already add its own).

## Known issues, not fixed (owner's call)

- **Keep the minimum iOS at 15.0 while 2026.11.23 copies are around** (23 November 2026). The update card in 2026.11.23
  does not check the store version's minimum iOS (fixed from 2026.11.24): if a later release needs a newer iOS than a
  player's phone has, a 2026.11.23 copy would show the blocking card with an update the phone cannot install. Same if the
  game leaves a country's store (the lookup asks the US store).
- The chapter 2 shop still sells the coffee machine upgrade (650) though coffee is a chapter 1 food (its
  `upgradeFoodType` CoffeeMachine is not in `SessionManager.FoodChapter`). Fix: `{ "CoffeeMachine", 1 }` there, after
  checking nothing else reads it through `IsFoodItemPurchased`. Most players buy it in chapter 1, where it leaves the shop.
- `Level13Data..Level16Data.asset` are unused (chapter 1 shrank to 12 days); `LevelData.starThreshold1..3` and
  `ordersPerLevel` are no longer read for stars or the day's end. Harmless.
- From the 7 to 8 October review and the 13 October play test, not rechecked since:
  - Leaving a day through Pause, Home keeps the coins earned so far and the day replays; Restart rolls them back.
  - Players from 26.03.29 lost bought coffee upgrades in that build (it never saved them); nothing to recover.
  - The item-served sound plays twice per correct serve; several clips are unassigned (button click, item pickup,
    wrong item, level win).
  - Bread and Cake reject a wrong tap silently; other foods buzz.
  - Play and Continue clicks ignore Sound Off.
  - Rewarded ad status labels are unassigned in the Shop scene, so they never show.
  - `drag.anim` has an animation event with an empty function name.
  - Settings panel animator runs on scaled time, so it freezes while paused.
  - The ringer switch mutes the game (Ambient audio session). Standard for casual games.
  - Editor only: "AudioManager: Cannot play music. Clip: False" once at menu start; Unity Ads shows a Placeholder ad.

## Leave alone (owner's decision, 7 October 2026)

The game is live and working. These were reviewed and the owner chose not to change them. Do not "fix" them.

- Main menu music set to None in the MainMenu scene (silent menu is accepted).
- Unused packages (Visual Scripting, XR Management, Mobile feature set, 2D feature set extras, Timeline): keep.
- `Assets/Plugins/iOS/NSUserTrackingUsageDescription.plist`: leave (never reaches the export; harmless).
- Choux sprite cropping: needs a device test; stays under Someday.
- MP3 sound effects re-encoded to Vorbis: fine as is.
- Debug flag defaults: leave.
- Buttons under the notch and home indicator: leave for now. `SafeAreaFitter` exists if this changes.
- AudioManager prefab vs scene overrides: fine, not every clip is meant to be wired.
- Hold-to-refill only on Melon (an inactive object): leave. Details in `Docs/history/maintenance_2026-10-07.md`
  (section "Refill system").
- Orphan entries Pie, Mont Blanc and Log Cake in the ScoreManager points table: harmless.
- Kitchen bakes continuing through pause and into the Shop (since 20 to 23 November 2026 the kitchen clock stops on
  pause and `KitchenDay.End` clears the kitchen at the day's end, Restart, Home and the shop's Next).
- Small scene leftovers (missing script on the MainMenu Canvas, dead Inspector call on the kitchen back button,
  silent pause button, unreferenced scripts): leave unless they break something.
- Upgrade shop items are one-shot by design.

## Clean-up ideas (not to do without the owner)

- `StartArrangementPhase` runs twice per level load (harmless, guarded).
- Money state lives in both `ScoreManager` and `SessionManager`; new money features go through SessionManager.
- String identifiers for foods, characters, upgrades and cards; a shared constants class would make renames safe.
- Git history is large (Library was committed early on); only worth rewriting on a fresh clone.

## Someday (needs device testing)

- [ ] Crop the Choux source art (`Untitled_Artwork 5` to `9`) so it can sit at 512 px.

## Test checklist for a release build

- [ ] Fresh install: tracking prompt once; the tutorial plays (apple tree tap, kitchen swipe); Let's go opens the map.
- [ ] Day 1 has no arrangement; day 2 shows the arranging lesson; no ad before day 3, one after Play from day 3.
- [ ] Two customers, thinking dots, flowers wilt and refill on each serve; receipt tip levels right.
- [ ] Day ends after the clock and the last customer; dusk and night on the way; stars by customers served.
- [ ] Kitchen: ovens and the picker, choux and cake flow (mixer, board), arranging (red blocks Ready); stops at day end.
- [ ] Day 3 candy gift once; candies in the shop after day 3.
- [ ] Day 9: the star card, chapter 2 opens on finishing it; bonus days revealed once.
- [ ] Chapter 2: gift flower, ice cream wells, cones and waffle machine, blender refill, coconuts.
- [ ] Shop: tabs, NEW tags, eaten cookie, book (journal, staff, cookies); $ tab and Restore on device.
- [ ] iPad and iPhone SE: hen, handle, pots and jar in line, nothing off screen.
- [ ] Daily Treats pays on the next Continue; kill the app mid-chapter, reopen, Continue: map and stars right.
