# TODO

Current open work, as of 13 October 2026 (night). Done items are removed; the dated record is in `Docs/history/`
(the 9 to 13 October TODO is `history/TODO_2026-10-13_before_rewrite.md`).

## Release 26.10.17 (owner, 16 October evening: "today or this weekend")

- [ ] **Owner decides:** ship without chapter 2 (my pick) or hold for it; re-tune the star lines; release note text; privacy
  label entry (Usage Data / Product Interaction); the store description's "two customers" wording.
- [ ] **TestFlight first** (26.10.17 build 2, uploaded 16 October 17:58; build 1 before it): the one-more-treat ad button, test ads, analytics events, the menu on iPad, shared ovens, the shop layout and popup, the day-end note, the longer days.
- [ ] **Store screenshots:** redo for the menu, prices and background; the banner must not say "16 days", use
  "earn stars along the trip" (owner, 16 October). `Store/screenshots/caption.py`.
- [ ] **Next release after this one:** stars as a second currency (earned by play, a replay never pays the same star twice;
  what they buy is open), chapter 2 fully built (`Docs/chapter2-plan.md` steps 3 to 14).
- [ ] **Backlog (owner, 16 October):** a serve sound for every item (only Juice has one; the menu's placed sound uses it).
- [ ] Coffee station: its moved spot is not saved between days (`FixedStation.Place` puts it bottom left every arrangement); save the machine's position if the owner wants it kept.
- [ ] Ice cream: build the cone mechanic from Gemini's four-section station (`Store/chapter2/icecream_station/gemini_raw_3.jpg`): cut the station, the cone, scoops per flavour; tap the cone to float it, flavour taps add scoops, a red cross clears, serve on match (owner, 16 October).
- [ ] **ChatGPT list (owner, 16 October):** the chapter 2 beach background's right palm (its trunk leaves the frame; a painted trunk down to the bushes, `Resources/Chapter2/bg_day` and `bg_night`; my paint try was jagged and reverted); the chapter 2 stripe cloth, a texture pass so it reads as cloth (`Store/chapter2/table/cloth/stripes.png`, in the game as `Resources/Chapter2/cloth`); the apple basket, a new style as well as the 40-degree angle (Gemini's angle-only redraw in `Store/redesign/ai/apple` is not used); the menu sticker paper (a nicer pass on the notes); then the other foods at the angle (coffee machine, juice jug, bread basket, cookie plate, choux board) and their state strips.
- [ ] **Cookies art is the owner's:** every related sprite is in `Store/handoff/cookies` with a README; the game holds the owner's original plates and my heart cut-outs until they replace them.
- [ ] Shop descriptions and special notes: the owner tunes the list (report, Current). The popup layout is fixed places (price and two description lines in one label, a one-line note), so any new text must fit two lines at 68 pt bold on 1,000 px and the note one line at 56 pt on 940 px.
- [ ] **Balance (16 October late run):** a perfect chapter 1 earns about $12,800 against the $8,740 shop; the owner's rule (most but not all) needs prices about 60% higher or more items; owner decides.

## Waiting

- [ ] **Star lines after the price change:** the 16 October check run gave 3 stars on 8 days, 2 on 7, 1 on 1 (owner's call to re-tune with `economy_tune.cs`).
- [ ] **Privacy label:** with the next submission add "Usage Data / Product Interaction, not linked to you" for Unity Analytics (owner approves).

- [x] **26.10.13 build 4 released** (by 16 October).
- [ ] **Owner to decide:** the description and promo text still say "serve two customers at a time" / "keep two
  customers happy at once". The owner said not to say that in a caption. Rewording the description means pulling the
  submission back and resubmitting (MANUAL section 9), or it waits for the next version.

## Next version (MVP list in the report)

- [ ] **Menu book (owner, 14 October afternoon: the next version's main feature; plan in `Docs/menu-book-plan.md`).**
  Switch owned foods on and off with station caps, chosen in the arrangement phase; placeholder UI.
- [ ] **Chapter 2, the beach (parked again 14 October, after the menu book).** Art three ways (ChatGPT, Gemini, my
  kitchen-style painting), the owner picks. Parts: beach background day/night (picked); table and Shop background
  (owner's pick, sheets in `Store/chapter2/table`, `shop_bg`); ice cream cones/cups and a freezer well, coconut drink
  (brief `Store/chapter2/icecream`; Gemini draft in, ChatGPT pending); Cookies; hibiscus patience flowers; a beach
  map page (`MapScreen`, the locked arrow after day 16); days 17 to 32 (`Level17..24Data` exist, 8 more to make);
  chapter number in the save; "Chapter 1 complete!" (`LevelManager` line ~698) leads on to chapter 2.

- [ ] **Table redesign at the 40-degree camera** (approved 13 October): each item keeps its art and is repainted at
  the angle by ChatGPT. Briefs ready in `Store/redesign/ai/<item>/`: juice, choux, cookies, coffee cups, machine, cake
  redo (flat napkin, no pink cut face). About three pictures a day on the free plan.
- [ ] **Choux board stretch** to the 40-degree look (`Store/artkit/choux_board5.py`, K = 1.52, parked).
- [ ] **Cookies** back in (MANUAL section 6, "Held back").
- [ ] Fold the 13 October night fixes (commit 4cad84d) into the next build.

## Economy (tuned 14 October 2026)

The 13 October play test (a fast, perfect player through `Docs/tools/autoplay/driver.py`) reached three stars on no
day and bought out the shop by day 9 (numbers in `Docs/tools/autoplay/run_2026-10-13_days.tsv`). On 14 October the
star lines of all 16 days were reset from that run's earnings, smoothed over three days and never falling: 1 star at
30 %, 2 at 55 %, 3 at 80 % of a perfect player's day (`Docs/tools/recipes/economy_tune.cs`; days 1 and 5 eased by
hand). Check run (14 October): three stars on 14 of 16 days, two on days 1 and 5 before the easing. Cookies (600,
fourth shop row, back since 14 October) are the late money sink for now; `Docs/economy_sim.py` is still the old
model (Vase/Lantern/Radio, no day clock) and only worth updating if the tune needs more work.

## Later

- [ ] Rating prompt (`SKStoreReviewController` after a three-star day or day 5): not built (owner, 14 October: backlog).

- [ ] Marketing: keyword work, an Apple Search Ads test, short-video clips, cozy-game communities, a featuring
  request. The TikTok account is the owner's to create.
- [ ] Re-read downloads, retention and conversion in November, then decide on content.
- [ ] Country breakdown of downloads and ads into the report.
- [ ] Unity Ads payout profile; merge Unity's SKAdNetwork id list into `IOSPostBuild.cs`.
- [ ] Paid Apps Agreement, bank and tax (only before a purchase); Remove Ads (later); interstitial frequency once
  retention data exists.

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
- Hold-to-refill only on Melon (an inactive object): leave. Details in `Docs/maintenance.md`.
- Orphan entries Pie, Mont Blanc and Log Cake in the ScoreManager points table: harmless.
- Kitchen bakes continuing through pause and into the Shop: logs an error, no player impact.
- Small scene leftovers (missing script on the MainMenu Canvas, dead Inspector call on the kitchen back button,
  silent pause button, four unreferenced scripts): leave unless they break something.
- Upgrade shop items are one-shot by design.

## Known issues (minor, not fixed)

From the 7 to 8 October review (details in `Docs/systems`) and the 13 October play test.

- Double-tapping Next Level while an interstitial is pending can run the Shop transition twice.
- Leaving a day through Pause, Home keeps the coins earned so far and the day replays; Restart rolls them back.
- Players from 26.03.29 lost bought coffee upgrades in that build (it never saved them); nothing to recover.
- The item-served sound plays twice per correct serve.
- Bread and Cake reject a wrong tap silently; other foods buzz.
- The coffee machine's table position is not saved; food is not overlap-checked against the machine.
- Shop Next Level briefly plays gameplay music before arrangement music.
- Play and Continue clicks ignore Sound Off.
- Kitchen bake timers carry over through Restart and Home; the oven ding can play in the Shop.
- Rewarded ad status labels are unassigned in the Shop scene, so they never show.
- `drag.anim` has an animation event with an empty function name (logs while the old hand animation plays).
- Several clips are unassigned (button click, item pickup, wrong item, level win).
- Settings panel animator runs on scaled time, so it freezes while paused.
- The ringer switch mutes the game (Ambient audio session). Standard for casual games.
- Editor only: "AudioManager: Cannot play music. Clip: False" once at menu start; Unity Ads shows a Placeholder ad.

## Clean-up ideas (not to do without the owner)

- `StartArrangementPhase` runs twice per level load (harmless, idempotent).
- Money state lives in both `ScoreManager` and `SessionManager`; new money features go through SessionManager.
- String identifiers for foods, characters and upgrades; a shared constants class would make renames safe.
- Git history is large (Library was committed early on); only worth rewriting on a fresh clone.
- Unused packages: see `history/TODO_2026-10-13_before_rewrite.md` for the table of what each would save.

## Someday (needs device testing)

- [ ] Crop the Choux source art (`Untitled_Artwork 5` to `9`) so it can sit at 512 px.

## Test checklist for the next build

- [ ] Fresh install: tracking prompt once, no notification prompt; tutorial plays; Let's go opens the map.
- [ ] Two customers: bubbles over their heads, margins even, never the same person twice at once.
- [ ] Daisies wilt in turn and come back on each serve; an expired order leaves without paying.
- [ ] The day ends after the clock and the last customer; dusk and night tint on the way.
- [ ] Finish card: sale, tips and stars right; a replay pays half and shows the half; three-star bonus once.
- [ ] Shop: the lock arrow at the bottom; prices; buy with too little money shows the message.
- [ ] Kitchen: tap each oven; bake completes once after closing mid-bake.
- [ ] iPad: hen board beside the truck, pots on the cloth, nothing off screen.
- [ ] Daily Treats: pays on the next Continue; day 5 bonus.
- [ ] Kill the app mid-chapter, reopen, Continue: the map shows the right day and stars.

## Ice cream (17 October)
- The empty section wells are a flat steel paint (`cut_station.py` interior()); a drawn well would read better. Gemini's stainless redraw (`Store/chapter2/icecream_station/gemini_metal_raw.png`) is the alternative box; cutting it needs new section polygons.
- Scoops drawn in the ice cream squash with the lowered levels (sec*_l2, l1).
- The autoplay driver cannot build cones: read `OrderSystem.GetCurrentOrderTypesWithVariants()` and call `IceCreamStation.OnTap` (holder, sections by letter, cone).
- Ice cream pays 35 flat (`ScoreManager.itemPoints`); per-scoop pay is the fairer option. No own scoop/serve sound yet.
- The owner's "use it in the kitchen" could mean the station stands in the kitchen; built on the table (the 16 October design), to confirm.
