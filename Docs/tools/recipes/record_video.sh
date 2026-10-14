#!/bin/zsh
cd /Users/betzthebest/Desktop/Sept/FoodTruckCafe
M() { python3 Docs/tools/unity_mcp.py "$@" 2>&1 | tail -c 70; }
waitframes() { for i in $(seq 1 400); do n=$(python3 Docs/tools/unity_mcp.py exec /tmp/rec_state.cs 2>&1 | grep -o "frame [0-9]*" | grep -o "[0-9]*"); [ -n "$n" ] && [ "$n" -ge "$1" ] && break; sleep 0.5; done; echo "frames $n"; }
R=/Users/betzthebest/Desktop/Sept/Store/video/frames2; rm -rf $R; mkdir -p $R
# 1. store shot 01 again with two different people (phone and iPad)
for size in Store1320 iPad; do
  M exec /tmp/gv_$size.cs; sleep 1; M call manage_editor '{"action":"play"}'; sleep 5; M exec /tmp/newgame.cs; sleep 8; M exec /tmp/startday.cs
  for i in $(seq 1 60); do sleep 0.4; r=$(python3 Docs/tools/unity_mcp.py exec /tmp/state.cs 2>&1 | grep -o "orders [0-9] customers [0-9]"); case "$r" in "orders 2 customers 2") break;; esac; done
  M shot st_${size}_01b; sleep 1; M call manage_editor '{"action":"stop"}'; sleep 2
done
# 2. preview video footage at 886 x 1920
M exec /tmp/gv_Preview886.cs; sleep 1; M call manage_editor '{"action":"play"}'; sleep 5
M exec /tmp/newgame.cs; sleep 8
M exec /tmp/rec_08_arrange.cs; M exec /tmp/arrange_demo.cs; waitframes 230; M exec /tmp/rec_stop.cs
M exec /tmp/startday.cs; M exec /tmp/rec_02_day_walkin.cs; waitframes 150; M exec /tmp/serve.cs; waitframes 170; M exec /tmp/serve.cs; waitframes 190; M exec /tmp/serve.cs; waitframes 210; M exec /tmp/serve.cs; waitframes 360; M exec /tmp/rec_stop.cs
M exec /tmp/rec_03_coffee_machine.cs; M exec /tmp/brew.cs; waitframes 240; M exec /tmp/rec_stop.cs
M exec /tmp/kitchen.cs; sleep 2; M exec /tmp/rec_04_kitchen.cs; waitframes 60; M exec /tmp/bake.cs; waitframes 200; M exec /tmp/apples_tap.cs; waitframes 260; M exec /tmp/juice_tap.cs; waitframes 330; M exec /tmp/rec_stop.cs; M exec /tmp/closekitchen.cs; sleep 1
M exec /tmp/ff_dusk.cs; M exec /tmp/rec_06_dusk_night.cs; waitframes 120; M exec /tmp/serve.cs; waitframes 200; M exec /tmp/ff_night.cs; waitframes 300; M exec /tmp/serve.cs; waitframes 400; M exec /tmp/rec_stop.cs
M exec /tmp/kitchen.cs; sleep 2; M exec /tmp/rec_07_chef_juice_window.cs; waitframes 60; M exec /tmp/bake.cs; waitframes 240; M exec /tmp/rec_stop.cs; M exec /tmp/closekitchen.cs; sleep 1
M exec /tmp/freshshop.cs; sleep 6; M exec /tmp/rec_05_shop.cs; waitframes 60; M exec /tmp/scroll_900.cs; waitframes 130; M exec /tmp/scroll_1800.cs; M exec /tmp/scrollbtn.cs; waitframes 200; M exec /tmp/rec_stop.cs
M call manage_editor '{"action":"stop"}'; M exec /tmp/gv_Store1320.cs
for d in $R/*; do echo "$(basename $d) $(ls $d | wc -l)"; done
