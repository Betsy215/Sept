#!/bin/bash
# Verification of the 21 October fixes in the editor (about 4 minutes). Names v21_*.
cd /Users/betzthebest/Desktop/Sept/FoodTruckCafe
M="python3 Docs/tools/unity_mcp.py"; AP=Docs/tools/recipes/ap; RC=Docs/tools/recipes; V=Docs/tools/recipes/verify21
S() { $M shot "$1" > /dev/null; sleep 1.5; }
X() { $M exec "$1" | grep -o '"result":"[^"]*"' | head -c 300; echo; }
rm -f Docs/tools/.mcp_session; rm -f Assets/Screenshots/v21_*.png
X $AP/stop.cs; sleep 3; X $RC/gv_Store1320.cs; sleep 1
X $AP/play.cs; sleep 11
X $V/fresh_map.cs; sleep 6; X $V/map_check.cs; S v21_map_fresh
X $AP/tomenu.cs; sleep 5; X $V/continue_check.cs; S v21_menu_fresh
X $V/ch1_coconut_session.cs; sleep 12; X $V/drag_check.cs; sleep 1; S v21_c1_arrange
X $AP/stop.cs; sleep 4
X $AP/play.cs; sleep 11
X $AP/ch2_full.cs; sleep 12
X $AP/menu_open.cs; sleep 2; X $V/menu_count.cs; S v21_c2_menu
X $AP/startday.cs; sleep 5
X $AP/openkitchen.cs; sleep 4; X $V/blender_check.cs; sleep 0.5; S v21_c2_blend
sleep 4; X $RC/closekitchen.cs; sleep 2
X $AP/openkitchen.cs; sleep 4; X $V/coconut_check.cs; S v21_c2_kitchen2
X $RC/closekitchen.cs; sleep 2
$M call read_console '{"action":"get","types":["error"],"count":50,"include_stacktrace":false,"filter_text":"Exception"}' | head -c 1500; echo
X $AP/stop.cs; sleep 3
echo V21_DONE
