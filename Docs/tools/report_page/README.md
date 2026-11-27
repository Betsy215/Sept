> **27 November 2026:** Q&A is back (removed by mistake 26 Nov; the owner meant clear its read content). Sections: Next, Q&A, Current, MVP, Backlog, Analysis, Marketing (numbered 1 to 7). Answers to the owner's questions go in Q&A; cleared once read. In tick lists write `<span><strong>Title</strong> rest</span>`, never `<b>` (it is styled as a 22 px letter column).

# The report: how to write it and how to use it

The owner reads the report, not the terminal. It is the HTML artifact
https://claude.ai/artifact/VeYt7HSLpyotQEW5sTMvPP (slug `e7fe0ca8-59d9-4f3f-b8c6-7ed7a3495e06`), built from the files in
this folder. Style: the owner's bakery pages (Nunito, cream page, pastel cards, numbered circle headings, sticky section
bar). Chat replies stay short (a few lines); the substance goes in the page. The old Claude Doc
726347e1-dd8c-42fd-917b-be80474ff9ae is history only (everything before 9 November 2026).

## Pages and sections (fixed; never add, rename, remove or reorder unless the owner asks)

`index.html`, the report, seven sections (Q&A back since 27 November 2026; an answer to
the owner's question goes in Q&A):

1. **Next**: two cards, **You** (what the owner should look at or decide, my questions with tick boxes) and **Me** (what
   I do next). The owner reads it first: rewrite both at the start and end of every batch. Two or three lines each.
2. **Current**: the work in progress and the answers to the owner's questions, newest first: a card per piece of work
   with a short bold lead, pictures, one line per option, then tick boxes for the owner's choice. Since 24 November 2026
   the C chapter work lives here too.
3. **MVP**: what goes into the next release, an ordered list. Approved Current cards move here (one line each).
4. **Backlog**: one check list; items leave only when the owner says so.
5. **Analysis**: store and ads numbers only (App Store Connect, Unity Analytics/Ads), UTC; each refresh replaces the
   last. Exclude the owner's own phone from player counts (production USER_ID `ec14c6a5031c84057b578cfdccaa95c1`).
6. **Marketing** (always present): the App Store page, ratings, keywords, screenshots, video, events, header.

Two more pages that "clear/clean up the report" never touches:
- `visuals.html` **Visuals**: real captures of the live build (store page, menu and maps, each chapter's day,
  tutorial, chapter change, screen sizes, shop, journal).
- `mechanisms.html` **Mechanisms** (owner, 23 Nov 2026: "keep it forever ... game data and information"): the game in
  numbers (sale prices, kitchen times, days and chapters, customers per day, day lengths and money). Keep it current
  whenever a number changes.

The **C chapter** page existed 19 to 24 November 2026 and was removed by the owner ("this becomes the main focus now,
let's put updates on the report itself").

## Writing rules (the owner's, collected)

- **Fewer words, always.** No process narration, no hedging, no restating the ask, no instruction text ("tick the box
  to..."). One picture beats a paragraph.
- **Real pictures only**: captures from the editor or device, or real mocks at the right size. Game-scene mocks are
  phone-size (1320 x 2868) and laid out like the game (the background scaled to the screen height and centre-cropped,
  the cloth from the screen middle, the drop at the cloth's edge row). One place per picture (table, kitchen, shop or UI;
  never a shop icon in a kitchen sheet); the caption names it.
- **Comparisons side by side**: e.g. a new shop page next to chapter 1's and chapter 2's.
- **Every option gets its own tick**; add "More ideas" when brainstorming.
- **Approved → MVP**: when the owner ticks a choice or says "good", drop the card from Current and add a line to MVP.
  Questions answered or "Seen" are removed.
- Dates are absolute ("24 November"), never "today".
- Alt text on every image describes what it shows (the owner may read it; it is also how a future session knows).

## The tick routine (every exchange that touches the report)

1. `db_get(slug, "ticks/state")`: `{"<data-id>": true|false, "<data-id>@": "<iso time>"}`. Also `read_comments(slug)`.
2. Act on the ticks before any other write: delete Seen/approved entries, carry out ticked choices (or say in the card
   why one cannot be done), move approved work to MVP.
3. Make the batch's changes, refresh Next, publish, commit this folder, push.

Tick ids are unique and dated: `<section>.<topic+date>.<choice>`, e.g. `cur.c24.shop.desert`,
`next.day16.no` (ids starting `qa.` are from the removed Q&A section). Old ids stay in the db; the page reads only ids it still has.

The page's markup: `<div class="card">`, `<figure><img ...><figcaption>`, `<ul class="checks"><li><label><input
type="checkbox" data-id="..."> Label</label></li></ul>`. The page script saves ticks itself (it declares
`{db:{}, user:{}}`).

## Publishing

- Pictures go in a dated subfolder here (e.g. `c24/`, `bonus/`, `stars/`, `visuals/`), JPEG, sheets about 900 px tall.
- Before `publish_files` on the existing artifact, call `list_files` (or use the last publish result) and pass
  `base_version` = the live version. Default patch mode keeps files not named; list deleted pictures in `remove`.
- Small text changes: `edit_version` (exact unique `old_text`).
- A new section id needs a chip in `nav.toc` (only when the owner asks for a section).
- After every publish: `git add Docs/tools/report_page && git commit && git push origin main`.

## "Clean up the report" means

Remove everything answered, approved or seen; Current and Backlog may end as "Nothing open."; MVP keeps the next
release's list (or "nothing yet"); Next is rewritten; Visuals and Mechanisms stay untouched.
