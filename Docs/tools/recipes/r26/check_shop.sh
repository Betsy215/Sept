#!/bin/bash
# shop check (26 October 2026, night): the Gemini candies icon, and the Candies card greyed out once the jar holds nine
cd /Users/betzthebest/Desktop/Sept/FoodTruckCafe/.claude/worktrees/icecream-polish-25oct
export UNITY_INSTANCE=icecream-polish-25oct@5bf70c628db81bfd
M="python3 Docs/tools/unity_mcp.py"; R=Docs/tools/recipes/r26; AP=Docs/tools/recipes/ap; RC=Docs/tools/recipes
rm -f Docs/tools/.mcp_session
$M exec $AP/stop.cs; sleep 3
$M exec $RC/gv_Store1320.cs; sleep 1
$M exec $AP/play.cs; sleep 12
$M exec $R/c2_session.cs; sleep 12
$M exec $R/done.cs; sleep 4
$M exec $R/glass_show.cs; sleep 2
$M shot r26n_glass > /dev/null; sleep 1.5
$M exec $AP/toshop.cs; sleep 7
$M shot r26n_shop_open > /dev/null; sleep 1.5
$M exec $R/jar_full.cs; sleep 1.5
$M shot r26n_shop_full > /dev/null; sleep 1.5
$M exec $AP/errs_dump.cs
$M exec $AP/stop.cs
