# TODO

Current open work, as of 13 October 2026 (night). Done items are removed; the dated record is in `Docs/history/`
(the 9 to 13 October TODO is `history/TODO_2026-10-13_before_rewrite.md`).

## Waiting

- [ ] **26.10.13 build 4 in App Store review** (submitted 13 October 21:41, manual release). Answer anything Apple
  raises; the owner presses Release.
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
- [ ] **Economy tune** (owner's call, see below).
- [ ] Fold the 13 October night fixes (commit 4cad84d) into the next build.

## Economy findings (13 October play test, not acted on)

Full chapter played by `Docs/tools/autoplay/driver.py` at 1.5x, one action a second, never letting an order expire
(a fast, perfect player). Per-day numbers: `Docs/tools/autoplay/run_2026-10-13_days.tsv`.

- Daily earnings 163, 231, 328, 388, 474, 1013, 848, 832, 665, 866, 1029, 885, 1110, 1127, 1072, 935; about $11,960
  in all. Stars 1 to 2 every day; **three stars on no day**. Earnings were 56 to 99 % of the three-star line, median
  about 75 %.
- Why: days are timed and a customer sets off every 5 s, so income is capped by arrivals, not by skill. The
  thresholds came from the older order-count model at 60 % patience left.
- **The whole shop was bought by day 9.** Days 10 to 16 have nothing to buy; the wallet ended at $7,214.
- Suggestion: three stars at about 0.8x today's line, two at 0.6x, one at 0.4x; add a late money sink (decorations,
  Cookies, chapter 2 unlock price). Then re-run the driver to check that a good player gets three stars on most days.
- `Docs/economy_sim.py` is out of date (still has Cookies in the shop, Vase/Lantern/Radio and no day clock). Update
  it before tuning, or tune from the driver.

## Later

- [ ] Rating prompt (`SKStoreReviewController` after a three-star day or day 5): not built (owner, 14 October: backlog).

- [ ] Marketing: keyword work, an Apple Search Ads test, short-video clips, cozy-game communities, a featuring
  request. The TikTok account is the owner's to create.
- [ ] Re-read downloads, retention and conversion in November, then decide on content.
- [ ] Country breakdown of downloads and ads into the report.
- [ ] Unity Ads payout profile; merge Unity's SKAdNetwork id list into `IOSPostBuild.cs`.
- [ ] Paid Apps Agreement, bank and tax (only before a purchase); Remove Ads (later); interstitial frequency once
  retention data exists.
- [ ] Confirm the `Interstitial_iOS` ad unit on the Unity Ads dashboard (14 October: cloud.unity.com rendered blank from the automated browser, a script error after the cookie banner; retry or the owner looks) and test the ad on a device (offline the
  Shop must open with no delay).

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
