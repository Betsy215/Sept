#!/bin/bash
# The shop scrolled to its bottom row in both chapters (the popup closed first), 19 October. Names s19_c*_shop_bottom.
cd /Users/betzthebest/Desktop/Sept/FoodTruckCafe
M="python3 Docs/tools/unity_mcp.py"; AP=Docs/tools/recipes/ap; RC=Docs/tools/recipes
S() { $M shot "$1" > /dev/null; sleep 1.5; }
X() { $M exec "$1" | grep -o '"result":"[^"]*"' | head -c 120; echo; }
rm -f Docs/tools/.mcp_session; rm -f Assets/Screenshots/s19_c*_shop_bottom*.png
X $AP/stop.cs; sleep 3; X $RC/gv_Store1320.cs; sleep 1
for CH in 1 2; do
  X $AP/play.cs; sleep 11
  if [ $CH = 1 ]; then X $AP/ch1_session.cs; else X $AP/ch2_full.cs; fi; sleep 12
  X $AP/toshop.cs; sleep 6; X $RC/scroll_1800.cs; sleep 1.5; S s19_c${CH}_shop_bottom
  X $AP/stop.cs; sleep 4
done
echo S19B_DONE
