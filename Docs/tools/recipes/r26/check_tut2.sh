#!/bin/bash
# chapter 2 demo end to end in the branch editor: the banner hides on the way back from the kitchen, the glass serves
cd /Users/betzthebest/Desktop/Sept/FoodTruckCafe/.claude/worktrees/icecream-polish-25oct
export UNITY_INSTANCE=icecream-polish-25oct@5bf70c628db81bfd
M="python3 Docs/tools/unity_mcp.py"; R=Docs/tools/recipes/r26; AP=Docs/tools/recipes/ap; RC=Docs/tools/recipes
rm -f Docs/tools/.mcp_session
$M exec $AP/stop.cs; sleep 3
$M exec $RC/gv_Store1320.cs; sleep 1
$M exec $AP/play.cs; sleep 12
$M exec $R/c2_tut.cs; sleep 18
$M exec $R/tut_tap.cs; sleep 7.5
echo "back at the table:"; $M exec $R/tut2_state.cs
$M shot r26_tut_back > /dev/null; sleep 6
for i in 1 2 3 4; do $M exec $R/tut_tap.cs; sleep 2.5; done
echo "after serve:"; $M exec $R/tut2_state.cs
$M shot r26_tut_served > /dev/null; sleep 1.5
$M exec $AP/errs_dump.cs
$M exec $AP/stop.cs
