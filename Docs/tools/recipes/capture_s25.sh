#!/bin/bash
# Every screen of both chapters at 1320 x 2868, plus iPad and iPhone SE samples, after the 25 October merge (main = build 10).
# Like capture_s21.sh plus: chapter 2 waffle session (closed, open), the cone row after cones were taken, the station at
# level 2 (four flavours), the oven recipe picker in both kitchens. Names s25_*. Extra recipes in $T/ap (job tmp). Run with bash; the editor must be idle.
cd /Users/betzthebest/Desktop/Sept/FoodTruckCafe
T=${T:-/Users/betzthebest/.claude/jobs/dff58236/tmp}; mkdir -p $T/ap
M="python3 Docs/tools/unity_mcp.py"; AP=Docs/tools/recipes/ap; RC=Docs/tools/recipes; STEP=Docs/tools/autoplay/step.cs
S() { $M shot "$1" > /dev/null; sleep 1.5; }
X() { $M exec "$1" | grep -o '"result":"[^"]*"' | head -c 160; echo; }
rm -f Docs/tools/.mcp_session; rm -f Assets/Screenshots/s25_*.png
X $AP/stop.cs; sleep 3
$M call read_console '{"action":"clear"}' > /dev/null
X $RC/gv_Store1320.cs; sleep 1
for CH in 1 2; do
  X $AP/play.cs; sleep 11
  if [ $CH = 1 ]; then X $AP/ch1_session.cs; else X $AP/ch2_full.cs; fi; sleep 12
  S s25_c${CH}_arrange
  X $AP/menu_open.cs; sleep 2; S s25_c${CH}_menu
  X $AP/startday.cs; sleep 5
  if [ $CH = 2 ]; then
    X $AP/ice_build.cs; sleep 1.2; S s25_c2_build; sed "s/KIND/Cross/; s/IDX/0/" $AP/ice_tap.cs > $T/ap/tap_x.cs; X $T/ap/tap_x.cs; sleep 1
    X $T/ap/cones_down.cs; sleep 1; S s25_c2_cones3
    X $T/ap/waffle_tap.cs; sleep 2.5; S s25_c2_waffle_closed; sleep 4.3; S s25_c2_waffle_open; sleep 3; S s25_c2_cones_back
  fi
  for i in $(seq 1 30); do $M exec $STEP > /dev/null; sleep 1; done
  S s25_c${CH}_day
  for i in $(seq 1 12); do $M exec $STEP > /dev/null; sleep 1; done
  S s25_c${CH}_day2
  X $AP/openkitchen.cs; sleep 4; S s25_c${CH}_kitchen
  X $T/ap/oven_picker.cs; sleep 1.5; S s25_c${CH}_oven_picker
  if [ $CH = 2 ]; then X $AP/blend_tap.cs; sleep 1.0; S s25_c2_blend; sleep 3; fi
  X $RC/closekitchen.cs; sleep 2
  X $RC/ff_dusk.cs; for i in $(seq 1 6); do $M exec $STEP > /dev/null; sleep 1; done; S s25_c${CH}_dusk
  X $RC/ff_night.cs; for i in $(seq 1 6); do $M exec $STEP > /dev/null; sleep 1; done; S s25_c${CH}_night
  X $AP/clock_end.cs; for i in $(seq 1 14); do $M exec $STEP > /dev/null; sleep 1; done
  X $AP/errs_dump.cs; S s25_c${CH}_finish
  X $AP/toshop.cs; sleep 6; S s25_c${CH}_shop
  sed "s/INDEX/0/g" $AP/popup_n.cs > $T/ap/popup_x.cs; X $T/ap/popup_x.cs; sleep 1.2; S s25_c${CH}_popup
  X $AP/popup_cancel.cs; sleep 0.8; X $RC/scroll_1800.cs; sleep 1.5; S s25_c${CH}_shop_bottom
  X $AP/tomap.cs; sleep 6; S s25_c${CH}_map
  X $AP/tomenu.cs; sleep 5; S s25_c${CH}_mainmenu
  $M call read_console '{"action":"get","types":["error"],"count":100,"include_stacktrace":false}' > $T/s25_console_c${CH}.json
  X $AP/stop.cs; sleep 4
done
# chapter 2 at station level 2 (four flavours), a cone being built
X $AP/play.cs; sleep 11; X $T/ap/ch2_l2.cs; sleep 12; S s25_c2_l2_arrange; X $AP/startday.cs; sleep 5
X $AP/ice_build.cs; sleep 1.2; S s25_c2_l2_build; X $T/ap/tap_x.cs; sleep 1
for i in $(seq 1 20); do $M exec $STEP > /dev/null; sleep 1; done; S s25_c2_l2_day; X $AP/stop.cs; sleep 4
# the tutorial (Day 0)
X $AP/play.cs; sleep 11; X $RC/tut.cs; sleep 14; S s25_tutorial; X $AP/stop.cs; sleep 4
# other screen sizes: iPad and iPhone SE, the arrangement and a day in each chapter
for SZ in iPad iPhoneSE; do
  X $RC/gv_$SZ.cs; sleep 1
  for CH in 1 2; do
    X $AP/play.cs; sleep 11
    if [ $CH = 1 ]; then X $AP/ch1_session.cs; else X $AP/ch2_full.cs; fi; sleep 12
    S s25_${SZ}_c${CH}_arrange
    X $AP/startday.cs; sleep 5; for i in $(seq 1 20); do $M exec $STEP > /dev/null; sleep 1; done; S s25_${SZ}_c${CH}_day
    X $AP/stop.cs; sleep 4
  done
done
X $RC/gv_Store1320.cs
ls Assets/Screenshots/s25_*.png | wc -l
echo S25_DONE
