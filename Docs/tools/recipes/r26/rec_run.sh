#!/bin/bash
# Records the chapter 1 then the chapter 2 preview frames (rec_c1.cs, rec_c2.cs) at 886 x 1920 in the branch editor, then waits for done.txt.
cd /Users/betzthebest/Desktop/Sept/FoodTruckCafe/.claude/worktrees/icecream-polish-25oct
export UNITY_INSTANCE=icecream-polish-25oct@5bf70c628db81bfd
M="python3 Docs/tools/unity_mcp.py"; R=Docs/tools/recipes/r26; AP=Docs/tools/recipes/ap; RC=Docs/tools/recipes
rm -f Docs/tools/.mcp_session; rm -rf Captures/rec_c2 Captures/rec_c1
$M exec $AP/stop.cs; sleep 3
$M exec $RC/gv_Preview886.cs; sleep 1
$M exec $AP/play.cs; sleep 12
$M exec $R/c1_session.cs; sleep 12
$M exec $R/done.cs; sleep 4
$M exec $R/rec_c1.cs
for i in $(seq 1 60); do [ -f Captures/rec_c1/done.txt ] && break; sleep 5; done
for s in serve candy kitchen night; do echo "c1 $s $(ls Captures/rec_c1/$s 2>/dev/null | wc -l)"; done
$M exec $R/c2_session.cs; sleep 12
$M exec $R/done.cs; sleep 4
$M exec $R/rec_c2.cs
for i in $(seq 1 120); do [ -f Captures/rec_c2/done.txt ] && break; sleep 5; done
ls Captures/rec_c2
for s in serve waffle candy kitchen night; do echo "$s $(ls Captures/rec_c2/$s 2>/dev/null | wc -l)"; done
$M exec $AP/stop.cs; sleep 3
$M exec $RC/gv_Store1320.cs
