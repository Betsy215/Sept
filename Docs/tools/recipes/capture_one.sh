#!/bin/zsh
cd /Users/betzthebest/Desktop/Sept/FoodTruckCafe
M() { python3 Docs/tools/unity_mcp.py "$@" 2>&1 | tail -c 80; }
size=$1
M exec /tmp/gv_$size.cs; sleep 2
M call manage_editor '{"action":"play"}'; sleep 5; M exec /tmp/newgame.cs; sleep 8; M shot v11_${size}_table; sleep 1; M exec /tmp/startday.cs
for i in $(seq 1 60); do sleep 0.4; r=$(python3 Docs/tools/unity_mcp.py exec /tmp/state.cs 2>&1 | grep -o "orders [0-9] customers [0-9]"); case "$r" in "orders 2 customers 2") break;; esac; done; echo "$size $r"
M exec /tmp/lay/freeze.cs; M shot v11_${size}_two; sleep 2; M exec /tmp/lay/unfreeze.cs; M call manage_editor '{"action":"stop"}'; sleep 2
