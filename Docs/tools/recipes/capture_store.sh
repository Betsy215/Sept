#!/bin/zsh
# Store screenshots at one game-view size: 01 two orders, 02 kitchen during play, 03 receipt, 04 night, 05 shop (fresh session)
cd /Users/betzthebest/Desktop/Sept/FoodTruckCafe
M() { python3 Docs/tools/unity_mcp.py "$@" 2>&1 | tail -c 60; }
size=$1; rm -f Assets/Screenshots/st_${size}_*.png
M exec /tmp/gv_$size.cs; sleep 1
M call manage_editor '{"action":"play"}'; sleep 5; M exec /tmp/newgame.cs; sleep 8; M exec /tmp/startday.cs
for i in $(seq 1 60); do sleep 0.4; r=$(python3 Docs/tools/unity_mcp.py exec /tmp/state.cs 2>&1 | grep -o "orders [0-9] customers [0-9]"); case "$r" in "orders 2 customers 2") break;; esac; done
M shot st_${size}_01; sleep 1
M exec /tmp/kitchen.cs; sleep 2; M shot st_${size}_02; sleep 1; M exec /tmp/closekitchen.cs; sleep 1
M exec /tmp/serve.cs; sleep 0.3; M exec /tmp/serve.cs; sleep 0.3; M exec /tmp/serve.cs; sleep 0.3; M exec /tmp/serve.cs; sleep 1.5; M shot st_${size}_03; sleep 1
M exec /tmp/ff_night.cs; sleep 6; M shot st_${size}_04; sleep 1
M exec /tmp/freshshop.cs; sleep 6; M shot st_${size}_05; sleep 1
M call manage_editor '{"action":"stop"}'; ls Assets/Screenshots/st_${size}_*.png | wc -l
