#!/bin/bash
cd /Users/betzthebest/Desktop/Sept/FoodTruckCafe
M="python3 Docs/tools/unity_mcp.py"; AP=Docs/tools/recipes/ap; RC=Docs/tools/recipes; V=Docs/tools/recipes/verify21
S() { $M shot "$1" > /dev/null; sleep 1.5; }
X() { $M exec "$1" | grep -o '"result":"[^"]*"' | head -c 300; echo; }
rm -f Docs/tools/.mcp_session
X $AP/play.cs; sleep 11
X $AP/ch2_full.cs; sleep 12
X $AP/startday.cs; sleep 5
X $AP/openkitchen.cs; sleep 4; X $V/coconut_take.cs; sleep 0.5; S v21_c2_kitchen_took2
X $RC/closekitchen.cs; sleep 2
X $AP/openkitchen.cs; sleep 4; X $V/coconut_check.cs; S v21_c2_kitchen_reopened
X $RC/closekitchen.cs; sleep 2
X $AP/stop.cs; sleep 3
echo V21C_DONE
