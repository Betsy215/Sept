#!/bin/bash
# iPad captures for the 26.10.18 store set (plan_26.10.18.md scenes 2 to 6; scene 1 is s25_iPad_c1_day from capture_s25.sh).
# Run after capture_s25.sh, editor idle. Names s25_iPad_*.
cd /Users/betzthebest/Desktop/Sept/FoodTruckCafe
T=${T:-/Users/betzthebest/.claude/jobs/dff58236/tmp}
M="python3 Docs/tools/unity_mcp.py"; AP=Docs/tools/recipes/ap; RC=Docs/tools/recipes; STEP=Docs/tools/autoplay/step.cs
S() { $M shot "$1" > /dev/null; sleep 1.5; }
X() { $M exec "$1" | grep -o '"result":"[^"]*"' | head -c 160; echo; }
X $AP/stop.cs; sleep 3; X $RC/gv_iPad.cs; sleep 1
# chapter 1: the shop
X $AP/play.cs; sleep 11; X $AP/ch1_session.cs; sleep 12; X $AP/startday.cs; sleep 5
X $AP/clock_end.cs; for i in $(seq 1 14); do $M exec $STEP > /dev/null; sleep 1; done
X $AP/toshop.cs; sleep 6; S s25_iPad_c1_shop; X $AP/stop.cs; sleep 4
# chapter 2: menu, a cone, the kitchen, the map
X $AP/play.cs; sleep 11; X $AP/ch2_full.cs; sleep 12
X $AP/menu_open.cs; sleep 2; S s25_iPad_c2_menu; X $AP/startday.cs; sleep 5
X $AP/ice_build.cs; sleep 1.2; S s25_iPad_c2_build; X $T/ap/tap_x.cs; sleep 1
for i in $(seq 1 10); do $M exec $STEP > /dev/null; sleep 1; done
X $AP/openkitchen.cs; sleep 4; S s25_iPad_c2_kitchen; X $RC/closekitchen.cs; sleep 2
X $AP/clock_end.cs; for i in $(seq 1 14); do $M exec $STEP > /dev/null; sleep 1; done
X $AP/toshop.cs; sleep 6; X $AP/tomap.cs; sleep 6; S s25_iPad_c2_map; X $AP/stop.cs; sleep 4
X $RC/gv_Store1320.cs
ls Assets/Screenshots/s25_iPad_*.png | wc -l; echo IPAD_DONE
# the recipe picker at phone size: chapter 2 kitchen with four oven foods on the menu
X $AP/play.cs; sleep 11; X $AP/ch2_full.cs; sleep 12; X $T/ap/oven_foods_on.cs; X $AP/startday.cs; sleep 5
X $AP/openkitchen.cs; sleep 4; X $T/ap/oven_picker.cs; sleep 1.5; S s25_c2_oven_picker2; X $AP/stop.cs; sleep 4
echo PICKER_DONE
