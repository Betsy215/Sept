#!/bin/bash
# ChatGPT's fills in play: full wells, then 4-3-2-1, plus the two-flavour station
cd /Users/betzthebest/Desktop/Sept/FoodTruckCafe/.claude/worktrees/icecream-polish-25oct
export UNITY_INSTANCE=icecream-polish-25oct@5bf70c628db81bfd
M="python3 Docs/tools/unity_mcp.py"; R=Docs/tools/recipes/r26; AP=Docs/tools/recipes/ap; RC=Docs/tools/recipes
rm -f Docs/tools/.mcp_session
$M exec $AP/stop.cs; sleep 3
$M exec $RC/gv_Store1320.cs; sleep 1
$M exec $AP/play.cs; sleep 12
$M exec $R/c2_session.cs; sleep 12
$M exec $R/done.cs; sleep 4
$M shot r26f_full > /dev/null; sleep 1.5
$M exec $R/wells_levels.cs; sleep 1
$M shot r26f_levels > /dev/null; sleep 1.5
$M exec $R/c2_tut.cs; sleep 14
$M shot r26f_two > /dev/null; sleep 1.5
$M exec $AP/errs_dump.cs
$M exec $AP/stop.cs
