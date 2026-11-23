# The report page

The owner's report is the HTML artifact https://claude.ai/artifact/VeYt7HSLpyotQEW5sTMvPP
(slug `e7fe0ca8-59d9-4f3f-b8c6-7ed7a3495e06`), two pages: `index.html` (the report) and
`visuals.html` (captures). Style: the owner's bakery pages (Nunito, cream page, pastel cards,
numbered circle headings, sticky section bar). The Claude Doc 726347e1-dd8c-42fd-917b-be80474ff9ae
is history only.

## Fixed structure: eight sections (never add, rename, remove or reorder a section unless the owner asks)

1. **Next**: You / Me cards. The owner reads this first; rewrite both at the start and the end of a batch.
   My questions to the owner go in You, with check boxes.
2. **Q&A**: the owner's questions only, newest first, numbered Q1..Qn as shown: bold question + date,
   short answer, picture if it helps, then a check list.
3. **Current**: today's work, one entry per batch, newest first: bold dated lead, ticks with commits,
   pictures, then a check list.
4. **MVP**: the next release's ordered list.
5. **Backlog**: one check list; items leave only when the owner says so.
6. **Analysis**: store and ads numbers only (App Store Connect, Unity Ads), UTC; each refresh replaces the last.
7. **Marketing** (owner, 18 November 2026: "move the app store page section to it's own section in the report, call it
   marketing. i am not sure what to do on this yet but we would need to continue working on this ... keep this section
   always there"): the App Store page, ratings, keywords, screenshots, video, in-app events, custom pages, anything that
   brings players. Cards with check lists like Current. Always present, even when it holds only "Nothing open".
8. **C chapter** (owner, 18 November 2026: "Put a section on report for c chapter (Christmas chapter), I will call it c
   chapter for short"): the Christmas chapter's plan, starting from the owner's ideas (pink cloth and white snow, winter
   clothes, cookies, hot drinks). Always present while the chapter is being made.
Mechanisms (`mechanisms.html`; owner, 23 November 2026: "add this tab and keep it forever, call it mechanisms, this is the
tab we keep game data and information etc."): game data in numbers (sale prices, kitchen times, ...). Never cleared with the
report; keep its numbers current.
Visuals (`visuals.html`): real captures of the live build in the Claude Doc's order
(Store page, Menu and maps, Chapter 1 day, Chapter 2 tutorial, Chapter 2 day, Chapter change, Screen sizes).

## Check boxes (the owner's action buttons)

Every Q&A and Current entry ends with `<ul class="checks">`: first `Seen, remove this`, then the choices.
Each box has a unique `data-id` (`qa.plan.all`, `cur.oct31.keep`, `backlog.a`, `next.day16.yes`).
Ticks are saved by the page script into the artifact's db, doc `ticks/state`
(`{"<data-id>": true|false, "<data-id>@": "<iso time>"}`); the page declares `{db:{}, user:{}}`.

Every session that touches the report first reads the ticks:
`db_get(slug, path "ticks/state")`. Then, before any other write: delete every entry whose `.seen`
is true, act on ticked choices, and reword an entry whose choice cannot be carried out here.
A removed entry's ids stay in the db; that is fine (the page only reads ids it still has).

## Republish

Edit here, then `publish_files` with the slug (`get_artifact` for `base_version`; default patch mode keeps
the pictures). Small text: `edit_version`. Pictures: `Store/report/` (not in git) and `visuals/` here.
A new section id needs a chip in `nav.toc`. Commit here after every publish.
