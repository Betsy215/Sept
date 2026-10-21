#!/bin/bash
# Every screen of both chapters at 1320 x 2868, plus iPad and iPhone SE samples (19 October 2026). Names s21_*.
# Recipes: Docs/tools/recipes/ap (the 18 October set) and Docs/tools/recipes. Run with bash; the editor must be idle.
cd /Users/betzthebest/Desktop/Sept/FoodTruckCafe
M="python3 Docs/tools/unity_mcp.py"; AP=Docs/tools/recipes/ap; RC=Docs/tools/recipes; STEP=Docs/tools/autoplay/step.cs
S() { $M shot "$1" > /dev/null; sleep 1.5; }
X() { $M exec "$1" | grep -o '"result":"[^"]*"' | head -c 120; echo; }
rm -f Docs/tools/.mcp_session; rm -f Assets/Screenshots/s21_*.png
X $AP/stop.cs; sleep 3
$M call read_console '{"action":"clear"}' > /dev/null
X $RC/gv_Store1320.cs; sleep 1
for CH in 1 2; do
  X $AP/play.cs; sleep 11
  if [ $CH = 1 ]; then X $AP/ch1_session.cs; else X $AP/ch2_full.cs; fi; sleep 12
  S s21_c${CH}_arrange
  X $AP/menu_open.cs; sleep 2; S s21_c${CH}_menu
  X $AP/startday.cs; sleep 5
  if [ $CH = 2 ]; then X $AP/ice_build.cs; sleep 1.2; S s21_c2_build; sed "s/KIND/Cross/; s/IDX/0/" $AP/ice_tap.cs > /tmp/ap/tap_x.cs; X /tmp/ap/tap_x.cs; sleep 1; fi
  for i in $(seq 1 30); do $M exec $STEP > /dev/null; sleep 1; done
  S s21_c${CH}_day
  for i in $(seq 1 12); do $M exec $STEP > /dev/null; sleep 1; done
  S s21_c${CH}_day2
  X $AP/openkitchen.cs; sleep 4; S s21_c${CH}_kitchen
  if [ $CH = 2 ]; then X $AP/blend_tap.cs; sleep 1.0; S s21_c2_blend; sleep 3; fi
  X $RC/closekitchen.cs; sleep 2
  X $RC/ff_dusk.cs; for i in $(seq 1 6); do $M exec $STEP > /dev/null; sleep 1; done; S s21_c${CH}_dusk
  X $RC/ff_night.cs; for i in $(seq 1 6); do $M exec $STEP > /dev/null; sleep 1; done; S s21_c${CH}_night
  X $AP/clock_end.cs; for i in $(seq 1 14); do $M exec $STEP > /dev/null; sleep 1; done
  X $AP/errs_dump.cs; S s21_c${CH}_finish
  X $AP/toshop.cs; sleep 6; S s21_c${CH}_shop
  sed "s/INDEX/0/g" $AP/popup_n.cs > /tmp/ap/popup_x.cs; X /tmp/ap/popup_x.cs; sleep 1.2; S s21_c${CH}_popup
  X $AP/popup_cancel.cs; sleep 0.8; X $RC/scroll_1800.cs; sleep 1.5; S s21_c${CH}_shop_bottom
  X $AP/tomap.cs; sleep 6; S s21_c${CH}_map
  X $AP/tomenu.cs; sleep 5; S s21_c${CH}_mainmenu
  $M call read_console '{"action":"get","types":["error"],"count":100,"include_stacktrace":false}' > /tmp/ap/s21_console_c${CH}.json
  X $AP/stop.cs; sleep 4
done
# the tutorial (Day 0)
X $AP/play.cs; sleep 11; X $RC/tut.cs; sleep 14; S s21_tutorial; X $AP/stop.cs; sleep 4
# other screen sizes: iPad and iPhone SE, the arrangement and a day in each chapter
for SZ in iPad iPhoneSE; do
  X $RC/gv_$SZ.cs; sleep 1
  for CH in 1 2; do
    X $AP/play.cs; sleep 11
    if [ $CH = 1 ]; then X $AP/ch1_session.cs; else X $AP/ch2_full.cs; fi; sleep 12
    S s21_${SZ}_c${CH}_arrange
    X $AP/startday.cs; sleep 5; for i in $(seq 1 20); do $M exec $STEP > /dev/null; sleep 1; done; S s21_${SZ}_c${CH}_day
    X $AP/stop.cs; sleep 4
  done
done
X $RC/gv_Store1320.cs
ls Assets/Screenshots/s21_*.png | wc -l
echo S21_DONE
