#!/bin/bash
# Visuals captures of 21 November 2026 (owner: "update visual tab screenshots with latest"): the r30 script with r32 names,
# plus the new parts: the chapter 1 kitchen with the baking tray, the cake plate and the mixer (each stage of the flow), the
# thinking dots, the day 9 star on the map and its chapter 2 scene, the chapter 2 gift before its tutorial, the hand pointer.
# Run with bash while the editor is open and idle (play mode off). Log: Captures/r32_log.txt. Then Store/report/visuals_r32.py.
W=/Users/betzthebest/Desktop/Sept/FoodTruckCafe
cd $W
M="python3 Docs/tools/unity_mcp.py"; export M; AP=Docs/tools/recipes/ap; RC=Docs/tools/recipes; R=Docs/tools/recipes/r26; N=Docs/tools/recipes/r32
LOG=Captures/r32_log.txt; mkdir -p Captures; : > $LOG
S() { $M shot "$1" > /dev/null; sleep 1.5; echo "shot $1" >> $LOG; }
X() { local r; r=$($M exec "$1" | grep -o '"result":"[^"]*"' | head -c 200); echo "$(basename $1): $r" >> $LOG; }
STEP() { for i in $(seq 1 $1); do $M exec $R/step_dish.cs > /dev/null; sleep 0.8; done; }
rm -f Docs/tools/.mcp_session; rm -f Captures/r32_*.png Assets/Screenshots/r32_*   # Unity saves a repeat as name-1; clear first
X $AP/stop.cs; sleep 3
X $RC/gv_Store1320.cs; sleep 1
X $AP/play.cs; sleep 12
S r32_00_menu
# chapter 1, choux and cake on the menu
X $N/c1_choux.cs; sleep 12
S r32_c1_arrange
X $AP/menu_open.cs; sleep 2; S r32_c1_menu
X $R/menu_close.cs; sleep 1
X $RC/r29/done.cs; sleep 2
bash $N/wait_dots.sh 30; S r32_c1_think
STEP 16; S r32_c1_day
STEP 10; S r32_c1_day2
X $R/candy_tap.cs; sleep 0.5; S r32_c1_candy
X $AP/openkitchen.cs; sleep 4; S r32_c1_kitchen
X $N/bake_both.cs; sleep 0.6; S r32_k1_baked
sleep 0.9; S r32_k2_mixing
sleep 2.0; S r32_k3_creamed
sleep 1.2; S r32_k4_cutting
sleep 1.8; S r32_k5_done
sleep 4; S r32_k6_after
X $RC/closekitchen.cs; sleep 2
X $RC/ff_dusk.cs; STEP 6; S r32_c1_dusk
X $RC/ff_night.cs; STEP 6; S r32_c1_night
X $AP/clock_end.cs; STEP 16; sleep 2; S r32_c1_finish
X $AP/toshop.cs; sleep 6; S r32_c1_shop
X $R/tab_stars.cs; sleep 1.5; S r32_c1_shop_stars
X $AP/tomap.cs; sleep 6; S r32_c1_map
# day 9 on the chapter 1 map, then its scene
X $N/day9_map.cs; sleep 6; S r32_d9_map
X $N/day9_tap.cs; sleep 0.7; S r32_d9_star; sleep 1.3; S r32_d9_truck; sleep 2.5; S r32_d9_open
# chapter 2 tutorial on its first day: the gift first
X $AP/stop.cs; sleep 3; X $AP/play.cs; sleep 12
X $R/c2_tut.cs
for i in $(seq 1 20); do sleep 1; r=$($M exec $N/gift_state.cs | grep -o '"result":"[^"]*"'); case "$r" in *"order 5"*|*"order 6"*|*"card True"*) break;; esac; done
S r32_t0_three
for i in $(seq 1 10); do r=$($M exec $N/gift_state.cs | grep -o '"result":"[^"]*"'); case "$r" in *"card True"*) break;; esac; sleep 0.5; done
sleep 1.6; S r32_t0_gift
X $N/gift_go.cs; sleep 0.3; S r32_t0_fly; sleep 1.2; S r32_t0_four
sleep 3; S r32_t2_waffle
X $R/tut_tap.cs; sleep 3.5; S r32_t3_kitchen
sleep 6; S r32_t4_back
for i in 1 2 3 4; do sleep 2.5; X $R/tut_tap.cs; done
sleep 1.5; S r32_t5_serve
X $R/tut_tap.cs; sleep 4; S r32_t6_endcard
# chapter 2, four flavours, six glasses, six candies
X $AP/stop.cs; sleep 3; X $AP/play.cs; sleep 12
X $RC/r29/c2_session.cs; sleep 12
S r32_c2_arrange
X $AP/menu_open.cs; sleep 2; S r32_c2_menu
X $R/menu_close.cs; sleep 1
X $RC/r29/done.cs; sleep 5
STEP 16; S r32_c2_day
X $R/waffle_tap.cs; sleep 2.5; S r32_c2_waffle
sleep 7; S r32_c2_bowls
X $R/glass_show.cs; sleep 1; S r32_c2_glass
X $R/candy_tap.cs; sleep 0.5; S r32_c2_candy
STEP 12; S r32_c2_day2
X $AP/openkitchen.cs; sleep 4; S r32_c2_kitchen
X $AP/blend_tap.cs; sleep 1; S r32_c2_blend
sleep 3; X $RC/closekitchen.cs; sleep 2
X $RC/ff_dusk.cs; STEP 6; S r32_c2_dusk
X $RC/ff_night.cs; STEP 6; S r32_c2_night
X $AP/clock_end.cs; STEP 18; sleep 2; S r32_c2_finish
X $AP/toshop.cs; sleep 6; S r32_c2_shop
X $AP/tomap.cs; sleep 6; S r32_c2_map
X $R/page2.cs; sleep 2; S r32_c2_map2
X $AP/stop.cs; sleep 4
# iPad and iPhone SE samples, chapter 2 day
for G in iPad iPhoneSE; do
  X $RC/gv_$G.cs; sleep 1; X $AP/play.cs; sleep 12
  X $R/c2_session.cs; sleep 12; X $RC/r29/done.cs; sleep 4; STEP 12; X $R/glass_show.cs; sleep 1; S r32_$G
  X $AP/stop.cs; sleep 4
done
X $RC/gv_Store1320.cs
echo "DONE" >> $LOG
