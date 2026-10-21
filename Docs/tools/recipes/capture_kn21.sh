#!/bin/bash
# Chapter 1 kitchen at night, before and after the overlay-order fix (runtime copy). Names kn21_*.
cd /Users/betzthebest/Desktop/Sept/FoodTruckCafe/.claude/worktrees/kitchen-night-tree || exit 1
M="python3 Docs/tools/unity_mcp.py"; AP=Docs/tools/recipes/ap; RC=Docs/tools/recipes; T=/Users/betzthebest/.claude/jobs/40887d5c/tmp
S() { $M shot "$1" > /dev/null; sleep 1.5; }
X() { echo -n "$1: "; $M exec "$1" | grep -o '"result":"[^"]*"' | head -c 160; echo; }
X $AP/stop.cs; sleep 3
$M call read_console '{"action":"clear"}' > /dev/null
X $RC/gv_Store1320.cs; sleep 1
X $AP/play.cs; sleep 11
X $AP/ch1_session.cs; sleep 12
X $AP/startday.cs; sleep 5
X $T/night_hold.cs; sleep 8
X $AP/openkitchen.cs; sleep 4
S kn21_before
X $T/fix_window.cs; sleep 1.5
S kn21_after
X $RC/closekitchen.cs; sleep 2
S kn21_table_night
$M call read_console '{"action":"get","types":["error"],"count":50,"include_stacktrace":false}' | head -c 600; echo
X $AP/stop.cs; sleep 3
ls -la /Users/betzthebest/Desktop/Sept/FoodTruckCafe/Assets/Screenshots/kn21_*.png
echo KN21_DONE
