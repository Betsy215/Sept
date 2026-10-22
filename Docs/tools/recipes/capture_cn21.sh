#!/bin/bash
# Chapter 2 kitchen by day and at night (the palm under its navy copy, coconuts bright), 1320 x 2868. Names cn21_*.
# Run with bash from the main checkout; the editor must be idle.
cd /Users/betzthebest/Desktop/Sept/FoodTruckCafe || exit 1
M="python3 Docs/tools/unity_mcp.py"; AP=Docs/tools/recipes/ap; RC=Docs/tools/recipes
S() { $M shot "$1" > /dev/null; sleep 1.5; }
X() { echo -n "$(basename $1): "; $M exec "$1" | grep -o '"result":"[^"]*"' | head -c 200; echo; }
rm -f Docs/tools/.mcp_session
X $AP/stop.cs; sleep 3
$M call read_console '{"action":"clear"}' > /dev/null
X $RC/gv_Store1320.cs; sleep 1
X $AP/play.cs; sleep 11
X $AP/ch2_full.cs; sleep 12
X $AP/startday.cs; sleep 5
X $AP/openkitchen.cs; sleep 4
S cn21_day
X $RC/closekitchen.cs; sleep 2
X $RC/night_hold.cs; sleep 8
X $AP/openkitchen.cs; sleep 4
S cn21_night
$M call read_console '{"action":"get","types":["error"],"count":50,"include_stacktrace":false}' | head -c 600; echo
X $AP/stop.cs; sleep 3
ls -la Assets/Screenshots/cn21_*.png
echo CN21_DONE
