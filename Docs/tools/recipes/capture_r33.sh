#!/bin/bash
# Store captures for release 2026.11.23 (23 November 2026, owner: "do screenshots for release and for visual tabs"): capture_r31.sh with r33 names.
# the six store scenes of the r29 set on the iPhone 6.9-inch (1320 x 2868) and the iPad 13-inch (2064 x 2752), r29 sessions
# (yoga lady hired, no arranging hint), r31/journal_quiet.cs before each shop so no journal page covers it.
# Names r33_* and ipad33_* in Assets/Screenshots. Log: Captures/r33_log.txt
cd /Users/betzthebest/Desktop/Sept/FoodTruckCafe
M="python3 Docs/tools/unity_mcp.py"; AP=Docs/tools/recipes/ap; RC=Docs/tools/recipes; R=Docs/tools/recipes/r26; Q=Docs/tools/recipes/r31/journal_quiet.cs
LOG=Captures/r33_log.txt; mkdir -p Captures; : > $LOG
S() { $M shot "$1" > /dev/null; sleep 1.5; echo "shot $1" >> $LOG; }
X() { local r; r=$($M exec "$1" | grep -o '"result":"[^"]*"' | head -c 200); echo "$(basename $1): $r" >> $LOG; }
STEP() { for i in $(seq 1 $1); do $M exec $R/step_dish.cs > /dev/null; sleep 0.8; done; }
rm -f Docs/tools/.mcp_session Assets/Screenshots/r33_* Assets/Screenshots/ipad33_*
for SIZE in Store1320 iPad; do
  P=r33; [ $SIZE = iPad ] && P=ipad33
  X $AP/stop.cs; sleep 3
  X $RC/gv_$SIZE.cs; sleep 1
  X $AP/play.cs; sleep 12
  X $RC/r29/c1_session.cs; sleep 12
  X $RC/r31/journal_set.cs
  X $RC/r29/done.cs; sleep 5
  STEP 18; S ${P}_1_c1_day
  X $AP/clock_end.cs; STEP 16; sleep 2; S ${P}_x_c1_finish
  X $Q; X $AP/toshop.cs; sleep 6; X $Q; sleep 1; S ${P}_5_c1_shop
  X $RC/r31/book_open.cs; sleep 2.5; S ${P}_7_journal_a
  X $RC/r31/book_next.cs; sleep 2.5; S ${P}_7_journal_b
  X $RC/r31/book_next.cs; sleep 2.5; S ${P}_7_journal_c
  X $RC/r31/book_next.cs; sleep 2.5; S ${P}_7_journal_d
  X $RC/r29/c2_session.cs; sleep 12
  X $AP/menu_open.cs; sleep 2; S ${P}_3_c2_menu
  X $R/menu_close.cs; sleep 1
  X $RC/r29/done.cs; sleep 5
  STEP 12; X $R/glass_show.cs; sleep 1; S ${P}_2_c2_glass
  X $AP/openkitchen.cs; sleep 4; S ${P}_4_c2_kitchen
  X $RC/closekitchen.cs; sleep 2
  X $AP/clock_end.cs; STEP 18; sleep 2
  X $Q; X $AP/toshop.cs; sleep 6; X $Q; sleep 1
  X $AP/tomap.cs; sleep 6; X $RC/r33/map_cancel.cs; sleep 1.5; S ${P}_6_c2_map   # the day card opens by itself now: closed for the store picture
done
X $AP/errs_dump.cs
X $AP/stop.cs; sleep 3
X $RC/gv_Store1320.cs
echo DONE >> $LOG
