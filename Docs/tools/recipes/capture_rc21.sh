#!/bin/bash
# The owner's repainted red cookie (21 October 2026, 11:59): the chapter 2 table with Cookies on, the arrangement and twice
# during a day, 1320 x 2868. Names rc21_*. Run with bash; the editor must be idle.
cd /Users/betzthebest/Desktop/Sept/FoodTruckCafe
M="python3 Docs/tools/unity_mcp.py"; AP=Docs/tools/recipes/ap; RC=Docs/tools/recipes; STEP=Docs/tools/autoplay/step.cs
S() { $M shot "$1" > /dev/null; sleep 1.5; }
X() { $M exec "$1" | grep -o '"result":"[^"]*"' | head -c 120; echo; }
rm -f Docs/tools/.mcp_session; rm -f Assets/Screenshots/rc21_*.png
X $AP/stop.cs; sleep 3
X $RC/gv_Store1320.cs; sleep 1
X $AP/play.cs; sleep 11
X $AP/ch2_cookies.cs; sleep 12
S rc21_arrange
X $AP/startday.cs; sleep 5
for i in $(seq 1 18); do $M exec $STEP > /dev/null; sleep 1; done
S rc21_day
for i in $(seq 1 22); do $M exec $STEP > /dev/null; sleep 1; done
S rc21_day2
X $RC/state.cs
X $AP/stop.cs; sleep 3
ls -la Assets/Screenshots/rc21_*.png
echo RC21_DONE
