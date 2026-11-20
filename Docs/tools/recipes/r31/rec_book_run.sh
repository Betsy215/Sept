#!/bin/bash
# Only the book part of rec_run.sh, recorded with rec_book2.cs (fast). Frames: Captures/rec_book2/book/fNNNNN.raw
cd /Users/betzthebest/Desktop/Sept/FoodTruckCafe
M="python3 Docs/tools/unity_mcp.py"; R=Docs/tools/recipes/r26; AP=Docs/tools/recipes/ap; RC=Docs/tools/recipes; Q=$RC/r31
rm -f Docs/tools/.mcp_session; rm -rf Captures/rec_book2
$M exec $AP/stop.cs; sleep 3
$M exec $RC/gv_Preview886.cs; sleep 1
$M exec $AP/play.cs; sleep 12
$M exec $RC/r29/c1_session.cs; sleep 12
$M exec $Q/journal_set.cs
$M exec $RC/r29/done.cs; sleep 4
$M exec $AP/clock_end.cs; for i in $(seq 1 16); do $M exec $R/step_dish.cs > /dev/null; sleep 0.8; done; sleep 2
$M exec $Q/journal_quiet.cs; $M exec $AP/toshop.cs; sleep 7; $M exec $Q/journal_quiet.cs; sleep 2
$M exec $Q/rec_book2.cs
for i in $(seq 1 40); do [ -f Captures/rec_book2/done.txt ] && break; sleep 3; done
echo "book $(cat Captures/rec_book2/done.txt 2>/dev/null)"
$M exec $AP/stop.cs; sleep 3
$M exec $RC/gv_Store1320.cs
echo DONE
