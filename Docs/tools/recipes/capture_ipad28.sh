#!/bin/bash
# iPad 13-inch store set for 26.11.22 (15 November 2026, main editor, the yoga lady hired; from capture_ipad26.sh), 2064 x 2752, the iPhone set's six scenes: chapter 1 day, chapter 2 glasses,
# chapter 2 menu, chapter 2 kitchen, chapter 1 shop, chapter 2 map. Names ipad28_*, in Assets/Screenshots.
W=/Users/betzthebest/Desktop/Sept/FoodTruckCafe
cd $W
M="python3 Docs/tools/unity_mcp.py"; AP=Docs/tools/recipes/ap; RC=Docs/tools/recipes; R=Docs/tools/recipes/r26
rm -f Docs/tools/.mcp_session Assets/Screenshots/ipad28_*
S() { $M shot "$1" > /dev/null; sleep 1.5; echo "shot $1"; }
STEP() { for i in $(seq 1 $1); do $M exec $R/step_dish.cs > /dev/null; sleep 0.8; done; }
$M exec $AP/stop.cs; sleep 3
$M exec $RC/gv_iPad.cs; sleep 1
$M exec $AP/play.cs; sleep 12
$M exec $RC/r28/c1_session.cs; sleep 12
$M exec $R/done.cs; sleep 5
STEP 18; S ipad28_1_c1_day
$M exec $AP/clock_end.cs; STEP 16; sleep 2
$M exec $AP/toshop.cs; sleep 6; S ipad28_5_c1_shop
$M exec $RC/r28/c2_session.cs; sleep 12
$M exec $AP/menu_open.cs; sleep 2; S ipad28_3_c2_menu
$M exec $R/menu_close.cs; sleep 1
$M exec $R/done.cs; sleep 5
STEP 12; $M exec $R/glass_show.cs; sleep 1; S ipad28_2_c2_glass
$M exec $AP/openkitchen.cs; sleep 4; S ipad28_4_c2_kitchen
$M exec $RC/closekitchen.cs; sleep 2
$M exec $AP/clock_end.cs; STEP 18; sleep 2
$M exec $AP/toshop.cs; sleep 6
$M exec $AP/tomap.cs; sleep 6; S ipad28_6_c2_map
$M exec $AP/errs_dump.cs
$M exec $AP/stop.cs; sleep 3
$M exec $RC/gv_Store1320.cs
echo DONE
