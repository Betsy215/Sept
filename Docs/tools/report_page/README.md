# The report page

Since 9 November 2026 the owner's report is an HTML artifact in the style of their bakery pages
(Nunito, cream page, pastel cards, numbered sections, questions answered by number):
https://claude.ai/artifact/VeYt7HSLpyotQEW5sTMvPP (slug `e7fe0ca8-59d9-4f3f-b8c6-7ed7a3495e06`).
The Claude Doc report (726347e1-dd8c-42fd-917b-be80474ff9ae) is history only.

`index.html` here is the published source. Pictures live in `Store/report/` (not in git).
Update: edit `index.html`, then republish the same slug with `publish_files` (read `get_artifact` for the
`base_version` first; `patch` mode keeps the pictures). Small text changes: `edit_version` with the slug.
Sections: Next (You / Me cards), questions Q1..Qn (the owner answers "Q1 all, Q3 a d" in the terminal; an
answered question is removed and the numbers start again), Answers (every question the owner typed, newest
first, removed when they say so), Done since (ticks + pictures), The next release,
Numbers (Unity and Apple), Things you can say, Your pages.
