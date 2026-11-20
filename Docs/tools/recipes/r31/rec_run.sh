#!/bin/bash
# Preview footage for the release after 2026.11.18 (20 November 2026): chapter 1 (r28/rec_c1.cs) and chapter 2
# (r26/rec_c2.cs) as for 26.11.22, now with the r29 sessions (no arranging hint), then the book in the shop
# (r31/rec_book.cs; owner: "include some of opening the journal and briefly show the journal and cookies and staff pages").
# 886 x 1920, main editor. Frames: Captures/rec_c1, rec_c2, rec_book.
cd /Users/betzthebest/Desktop/Sept/FoodTruckCafe
M="python3 Docs/tools/unity_mcp.py"; R=Docs/tools/recipes/r26; N=Docs/tools/recipes/r28; AP=Docs/tools/recipes/ap; RC=Docs/tools/recipes; Q=$RC/r31
rm -f Docs/tools/.mcp_session; rm -rf Captures/rec_c2 Captures/rec_c1 Captures/rec_book
$M exec $AP/stop.cs; sleep 3
$M exec $RC/gv_Preview886.cs; sleep 1
$M exec $AP/play.cs; sleep 12
$M exec $RC/r29/c1_session.cs; sleep 12
$M exec $RC/r29/done.cs; sleep 4
$M exec $N/rec_c1.cs
for i in $(seq 1 60); do [ -f Captures/rec_c1/done.txt ] && break; sleep 5; done
for s in serve candy kitchen night; do echo "c1 $s $(ls Captures/rec_c1/$s 2>/dev/null | wc -l)"; done
$M exec $RC/r29/c2_session.cs; sleep 12
$M exec $RC/r29/done.cs; sleep 4
$M exec $R/rec_c2.cs
for i in $(seq 1 120); do [ -f Captures/rec_c2/done.txt ] && break; sleep 5; done
for s in serve waffle candy kitchen night; do echo "c2 $s $(ls Captures/rec_c2/$s 2>/dev/null | wc -l)"; done
# the book: a chapter 1 day ends, the shop opens (no journal page flying in), the book is opened
$M exec $RC/r29/c1_session.cs; sleep 12
$M exec $Q/journal_set.cs
$M exec $RC/r29/done.cs; sleep 4
$M exec $AP/clock_end.cs; for i in $(seq 1 16); do $M exec $R/step_dish.cs > /dev/null; sleep 0.8; done; sleep 2
$M exec $Q/journal_quiet.cs; $M exec $AP/toshop.cs; sleep 7; $M exec $Q/journal_quiet.cs; sleep 2
$M exec $Q/rec_book.cs
for i in $(seq 1 40); do [ -f Captures/rec_book/done.txt ] && break; sleep 3; done
echo "book $(cat Captures/rec_book/done.txt 2>/dev/null)"
$M exec $AP/stop.cs; sleep 3
$M exec $RC/gv_Store1320.cs
echo DONE
