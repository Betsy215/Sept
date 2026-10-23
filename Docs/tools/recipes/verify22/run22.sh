#!/bin/bash
# Verification of the cone refill (22 October), after the merge of worktree-waffle-machine: about 2 minutes. Names wm22_*.
cd /Users/betzthebest/Desktop/Sept/FoodTruckCafe
M="python3 Docs/tools/unity_mcp.py"; AP=Docs/tools/recipes/ap; RC=Docs/tools/recipes; V=Docs/tools/recipes/verify22
S() { $M shot "$1" > /dev/null; sleep 1.5; }
X() { $M exec "$1" | grep -o '"result":"[^"]*"' | head -c 300; echo; }
rm -f Docs/tools/.mcp_session; rm -f Assets/Screenshots/wm22_*.png
X $AP/stop.cs; sleep 3; X $RC/gv_Store1320.cs; sleep 1
X $AP/play.cs; sleep 11
X $AP/ch2_full.cs; sleep 12
X $AP/startday.cs; sleep 5
X $AP/openkitchen.cs; sleep 4; X $V/waffle_where.cs; S wm22_kitchen
X $V/waffle_check.cs; sleep 0.6; S wm22_bake
sleep 3; X $RC/closekitchen.cs; sleep 2; S wm22_table_full
$M call read_console '{"action":"get","types":["error"],"count":50,"include_stacktrace":false,"filter_text":"Exception"}' | head -c 1500; echo
X $AP/stop.cs; sleep 3
echo WM22_DONE
