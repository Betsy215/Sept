#!/bin/bash
# Release captures, 26 October 2026: every screen of both chapters at 1320 x 2868 on the release code (the branch editor),
# the chapter 2 tutorial, the glass-dish ice cream, the candy jar, plus iPad and iPhone SE samples. Names r26_*, in Captures/.
# Also a second round of checks: chapter 1's last day into chapter 2's first day (the tutorial must start), day labels.
# Run with bash while the branch editor is open and idle. Log: Captures/r26_log.txt
W=/Users/betzthebest/Desktop/Sept/FoodTruckCafe/.claude/worktrees/icecream-polish-25oct
cd $W
export UNITY_INSTANCE=icecream-polish-25oct@5bf70c628db81bfd
M="python3 Docs/tools/unity_mcp.py"; AP=Docs/tools/recipes/ap; RC=Docs/tools/recipes; R=Docs/tools/recipes/r26
LOG=Captures/r26_log.txt; mkdir -p Captures; : > $LOG
S() { $M shot "$1" > /dev/null; sleep 1.5; echo "shot $1" >> $LOG; }
X() { local r; r=$($M exec "$1" | grep -o '"result":"[^"]*"' | head -c 200); echo "$(basename $1): $r" >> $LOG; }
STEP() { for i in $(seq 1 $1); do $M exec $R/step_dish.cs > /dev/null; sleep 0.8; done; }
rm -f Docs/tools/.mcp_session; rm -f Captures/r26_*.png
X $AP/stop.cs; sleep 3
X $RC/gv_Store1320.cs; sleep 1
X $AP/play.cs; sleep 12
S r26_00_menu
# chapter 1
X $R/c1_session.cs; sleep 12
S r26_c1_arrange
X $AP/menu_open.cs; sleep 2; S r26_c1_menu
X $R/menu_close.cs; sleep 1
X $R/done.cs; sleep 5
STEP 18; S r26_c1_day
STEP 10; S r26_c1_day2
X $R/candy_tap.cs; sleep 0.5; S r26_c1_candy
X $AP/openkitchen.cs; sleep 4; S r26_c1_kitchen
X $RC/closekitchen.cs; sleep 2
X $RC/ff_dusk.cs; STEP 6; S r26_c1_dusk
X $RC/ff_night.cs; STEP 6; S r26_c1_night
X $AP/clock_end.cs; STEP 16; sleep 2; S r26_c1_finish
X $AP/toshop.cs; sleep 6; S r26_c1_shop
X $AP/tomap.cs; sleep 6; S r26_c1_map
# chapter 2 tutorial on its first day
X $R/c2_tut.cs; sleep 12; X $R/state.cs
sleep 6; S r26_t1_customer
sleep 3; S r26_t2_waffle
X $R/tut_tap.cs; sleep 3.5; S r26_t3_kitchen
sleep 6; S r26_t4_back
for i in 1 2 3 4; do sleep 2.5; X $R/tut_tap.cs; done
sleep 1.5; S r26_t5_serve
X $R/tut_tap.cs; sleep 4; S r26_t6_endcard
# chapter 2, four flavours, six glasses, six candies
X $R/c2_session.cs; sleep 12
S r26_c2_arrange
X $AP/menu_open.cs; sleep 2; S r26_c2_menu
X $R/menu_close.cs; sleep 1
X $R/done.cs; sleep 5
STEP 16; S r26_c2_day
X $R/waffle_tap.cs; sleep 2.5; S r26_c2_waffle
sleep 7; S r26_c2_bowls
X $R/glass_show.cs; sleep 1; S r26_c2_glass
X $R/candy_tap.cs; sleep 0.5; S r26_c2_candy
STEP 12; S r26_c2_day2
X $AP/openkitchen.cs; sleep 4; S r26_c2_kitchen
X $AP/blend_tap.cs; sleep 1; S r26_c2_blend
sleep 3; X $RC/closekitchen.cs; sleep 2
X $RC/ff_dusk.cs; STEP 6; S r26_c2_dusk
X $RC/ff_night.cs; STEP 6; S r26_c2_night
X $AP/clock_end.cs; STEP 18; sleep 2; S r26_c2_finish
X $AP/toshop.cs; sleep 6; S r26_c2_shop
X $AP/tomap.cs; sleep 6; S r26_c2_map
X $R/page2.cs; sleep 2; S r26_c2_map2
# check: chapter 1's last day leads into chapter 2's first day with its tutorial
X $R/c1_session.cs; sleep 3
$M exec /dev/stdin > /dev/null <<'EOF'
var sm = SessionManager.Instance; var s = sm.GetCurrentSession(); s.levelsCompleted = 11; sm.SetCurrentLevel(11); SceneTransitionManager.Instance.TransitionToScene("GameSceneOne"); return "day 12";
EOF
sleep 12; X $R/state.cs; X $R/done.cs; sleep 3; X $AP/clock_end.cs; STEP 20; sleep 3; S r26_x_day12_finish
X $R/next_day.cs; sleep 12; X $R/state.cs; S r26_x_day13_start
X $AP/stop.cs; sleep 4
# iPad and iPhone SE samples, chapter 2 day
for G in iPad iPhoneSE; do
  X $RC/gv_$G.cs; sleep 1; X $AP/play.cs; sleep 12
  X $R/c2_session.cs; sleep 12; X $R/done.cs; sleep 4; STEP 12; X $R/glass_show.cs; sleep 1; S r26_$G
  X $AP/stop.cs; sleep 4
done
X $RC/gv_Store1320.cs
echo "DONE" >> $LOG
