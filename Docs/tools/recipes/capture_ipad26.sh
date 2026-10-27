#!/bin/bash
# iPad 13-inch store set for 26.10.26 (2064 x 2752), the iPhone set's six scenes: chapter 1 day, chapter 2 glasses,
# chapter 2 menu, chapter 2 kitchen, chapter 1 shop, chapter 2 map. Names ipad26_*, in Assets/Screenshots.
W=/Users/betzthebest/Desktop/Sept/FoodTruckCafe/.claude/worktrees/icecream-polish-25oct
cd $W
export UNITY_INSTANCE=icecream-polish-25oct@5bf70c628db81bfd
M="python3 Docs/tools/unity_mcp.py"; AP=Docs/tools/recipes/ap; RC=Docs/tools/recipes; R=Docs/tools/recipes/r26
rm -f Docs/tools/.mcp_session Assets/Screenshots/ipad26_*
S() { $M shot "$1" > /dev/null; sleep 1.5; echo "shot $1"; }
STEP() { for i in $(seq 1 $1); do $M exec $R/step_dish.cs > /dev/null; sleep 0.8; done; }
$M exec $AP/stop.cs; sleep 3
$M exec $RC/gv_iPad.cs; sleep 1
$M exec $AP/play.cs; sleep 12
$M exec $R/c1_session.cs; sleep 12
$M exec $R/done.cs; sleep 5
STEP 18; S ipad26_1_c1_day
$M exec $AP/clock_end.cs; STEP 16; sleep 2
$M exec $AP/toshop.cs; sleep 6; S ipad26_5_c1_shop
$M exec $R/c2_session.cs; sleep 12
$M exec $AP/menu_open.cs; sleep 2; S ipad26_3_c2_menu
$M exec $R/menu_close.cs; sleep 1
$M exec $R/done.cs; sleep 5
STEP 12; $M exec $R/glass_show.cs; sleep 1; S ipad26_2_c2_glass
$M exec $AP/openkitchen.cs; sleep 4; S ipad26_4_c2_kitchen
$M exec $RC/closekitchen.cs; sleep 2
$M exec $AP/clock_end.cs; STEP 18; sleep 2
$M exec $AP/toshop.cs; sleep 6
$M exec $AP/tomap.cs; sleep 6; S ipad26_6_c2_map
$M exec $AP/errs_dump.cs
$M exec $AP/stop.cs; sleep 3
$M exec $RC/gv_Store1320.cs
echo DONE
