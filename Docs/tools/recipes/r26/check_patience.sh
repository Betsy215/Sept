#!/bin/bash
# chapter 2 patience bonus (26 October 2026, night): order times in a chapter 1 and a chapter 2 day; the last number
# in brackets is the chapter factor (1.00 in chapter 1, 1.20 in chapter 2)
cd /Users/betzthebest/Desktop/Sept/FoodTruckCafe/.claude/worktrees/icecream-polish-25oct
export UNITY_INSTANCE=icecream-polish-25oct@5bf70c628db81bfd
M="python3 Docs/tools/unity_mcp.py"; R=Docs/tools/recipes/r26; AP=Docs/tools/recipes/ap
rm -f Docs/tools/.mcp_session
$M exec $AP/stop.cs; sleep 3
$M exec $AP/play.cs; sleep 12
$M exec $R/c1_session.cs; sleep 12
$M exec $R/done.cs; sleep 12
$M exec $R/order_times.cs
$M exec $R/c2_session.cs; sleep 12
$M exec $R/done.cs; sleep 12
$M exec $R/order_times.cs
$M exec $AP/errs_dump.cs
$M exec $AP/stop.cs
