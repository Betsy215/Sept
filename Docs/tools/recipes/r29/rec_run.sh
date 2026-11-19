#!/bin/bash
# Records the chapter 1 then the chapter 2 preview frames for 26.11.22 (r28/rec_c1.cs, r26/rec_c2.cs) at 886 x 1920 in the main editor,
# with the yoga lady hired (r28 sessions), then waits for done.txt. From r26/rec_run.sh. Frames: Captures/rec_c1, Captures/rec_c2.
cd /Users/betzthebest/Desktop/Sept/FoodTruckCafe
M="python3 Docs/tools/unity_mcp.py"; R=Docs/tools/recipes/r26; N=Docs/tools/recipes/r29; AP=Docs/tools/recipes/ap; RC=Docs/tools/recipes
rm -f Docs/tools/.mcp_session; rm -rf Captures/rec_c2 Captures/rec_c1
$M exec $AP/stop.cs; sleep 3
$M exec $RC/gv_Preview886.cs; sleep 1
$M exec $AP/play.cs; sleep 12
$M exec $N/c1_session.cs; sleep 12
$M exec $N/done.cs; sleep 4
$M exec $N/rec_c1.cs
for i in $(seq 1 60); do [ -f Captures/rec_c1/done.txt ] && break; sleep 5; done
for s in serve candy kitchen night; do echo "c1 $s $(ls Captures/rec_c1/$s 2>/dev/null | wc -l)"; done
$M exec $N/c2_session.cs; sleep 12
$M exec $N/done.cs; sleep 4
$M exec $R/rec_c2.cs
for i in $(seq 1 120); do [ -f Captures/rec_c2/done.txt ] && break; sleep 5; done
for s in serve waffle candy kitchen night; do echo "c2 $s $(ls Captures/rec_c2/$s 2>/dev/null | wc -l)"; done
$M exec $AP/stop.cs; sleep 3
$M exec $RC/gv_Store1320.cs
