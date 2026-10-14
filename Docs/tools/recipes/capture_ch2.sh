#!/bin/zsh
# Chapter 2 previews in the real game (14 October 2026). Usage: capture_ch2.sh <name> "<bg|cloth|receipt|night>"
cd /Users/betzthebest/Desktop/Sept/FoodTruckCafe
M() { python3 Docs/tools/unity_mcp.py "$@" 2>&1 | tail -c 120; echo; }
name=$1; echo "$2" > /tmp/ap/ch2_args.txt
M exec /tmp/gv_Store1320.cs; sleep 2
M call manage_editor '{"action":"play"}'; sleep 6; M exec /tmp/newgame.cs; sleep 9
M exec /tmp/ap/ch2_swap.cs; sleep 1
M exec /tmp/startday.cs
for i in $(seq 1 60); do sleep 0.4; r=$(python3 Docs/tools/unity_mcp.py exec /tmp/state.cs 2>&1 | grep -o "orders [0-9] customers [0-9]"); case "$r" in "orders 2 customers 2") break;; esac; done; echo "$r"
case "$2" in *"|1") M exec /tmp/ff_night.cs; sleep 1; M exec /tmp/ap/ch2_swap.cs;; esac
for i in 1 2 3 4 5 6; do M exec /tmp/serve_once.cs; sleep 0.35; done
sleep 1.2; M exec /tmp/lay/freeze.cs; M shot $name; sleep 1; M exec /tmp/lay/unfreeze.cs; M call manage_editor '{"action":"stop"}'; sleep 3
