#!/bin/bash
# play a chapter 2 day in the branch editor, tap the glasses fast, report each glass's scale ratio (1.00 = resting)
cd /Users/betzthebest/Desktop/Sept/FoodTruckCafe/.claude/worktrees/icecream-polish-25oct
export UNITY_INSTANCE=icecream-polish-25oct@5bf70c628db81bfd
M="python3 Docs/tools/unity_mcp.py"; R=Docs/tools/recipes/r26; AP=Docs/tools/recipes/ap
rm -f Docs/tools/.mcp_session
$M exec $AP/play.cs; sleep 12
$M exec $R/c2_session.cs; sleep 12
$M exec $R/done.cs; sleep 5
for i in 1 2 3; do $M exec $R/rapid_taps.cs; sleep 3; $M exec $R/dish_scales.cs; done
$M shot r26_lift_check > /dev/null; sleep 1.5
$M exec $AP/errs_dump.cs
$M exec $AP/stop.cs
