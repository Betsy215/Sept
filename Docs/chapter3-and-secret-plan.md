# Chapter 3 (winter) and the secret chapter (Day of the Dead): the owner's plan

Written 15 November 2026 from the owner's own words, so a fresh session knows the plan. Read this before any work on
either chapter. Update it as the owner adds detail; keep their words in quotes, my suggestions marked as mine.

## The owner's words (15 November 2026)

> "For chrismas chapter, it would be chapter 3, I won't make it too Christmas looking bc I want to keep it as a regular
> chapter, but it would be a winter chapter and has some chrismas element in it. We would need new backgrounds, new food
> items and all customers wearing winter clothes and so on… more to come but I only have a general idea. And I want to
> work on this for about month so it will get released around mid-december."

> "I also want to add a secret chapter, unlocks by stars, it is using the theme of day of the dead in Mexico. This needs
> some special treatment in our map since it is optional and only gets unlocks by stars."

> "I want to finish building both chapter before end of this year."

> Asked the same day: "From now till end of this year, I am going to release a Christmas chapter, this is something we
> keep working on but not getting released until done, while we might be during regular release when continue workin
> on this, how do we handle a hidden feature like this?"

## Chapter 3: winter, with some Christmas

- A regular chapter (number 3, after the beach), winter themed, a few Christmas touches, not a Christmas chapter.
- Needs: new backgrounds (table, kitchen, shop, map, day and night like the other chapters), new food items, every
  customer in winter clothes (a winter version of each customer sprite, the faces kept: see the sprite rules in memory).
  "More to come": the owner has only the general idea; ask for details before building pieces they have not described.
- Timing: about a month of work, release around **mid-December 2026**.

## Secret chapter: Day of the Dead (Mexico)

- Optional; unlocked by **stars**, not by finishing the chapter before it.
- The map needs special treatment for it: an optional, star-locked chapter (not one more stop on the road).
- To finish building before the end of 2026, like chapter 3. Release date not given.
- Not decided yet (ask the owner): how many stars open it, where it sits on the map, how many days, its foods and
  customers, whether it is played once or replayable, what it gives back.

## How hidden work is kept out of the regular releases (my answer, 15 November 2026)

The regular releases go on from `main` while these chapters are built, so the unfinished chapters must never show to
store players:

1. **One switch per chapter, off in store builds.** `Chapter.Count` already decides which chapters exist (it is 2;
   chapter 2 was switched on with it on 18 October). Chapter 3 is added to `Chapter.DaysPerChapter` but `Count` stays 2
   until the release that ships it. The secret chapter gets its own switch (it is not part of the road).
2. **Test builds can open them.** Like the "Chapter 2 test" button (editor, debug and TestFlight builds only,
   `AdsInitializer.IsTestFlightBuild()`), a "Chapter 3 test" and a "Secret chapter test" button set `Count` for that run
   only, so the owner can play the new chapters on TestFlight while the App Store copy never shows them.
3. **Everything new is guarded by the chapter number** (`SessionManager.CurrentChapter`, `Chapter.Art<T>(chapter, name)`),
   the same way chapter 2's beach art, foods (`FoodChapter`, `UnlockChapter`) and shop cards are. New shop cards for
   chapter 3 stay held back (`IsHeldBackCard`) until the switch is on.
4. **Work stays on `main`**, in small steps, not on a long branch: a month-long branch would drift from the releases.
   Each step is checked with the switch off (the store game unchanged) and on (the test buttons).
5. **App size:** art under `Assets/Resources` ships in every build even while hidden. Keep big chapter 3 art outside
   `Resources` (for example in `Store/`) until it is placed in the game, or accept a bigger download.
6. **Saves:** players' saves must survive the switch going on later: new fields get defaults, nothing is renamed.

## Status

- 15 November 2026: plan written; nothing built.
