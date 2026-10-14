#!/bin/zsh
# Test-before-release series at 1320 x 2868: menu, map, day board, arrange, two orders, after a serve, receipt, dusk/night, finish, kitchen, shop, tutorial
cd /Users/betzthebest/Desktop/Sept/FoodTruckCafe
M() { python3 Docs/tools/unity_mcp.py "$@" 2>&1 | tail -c 60; }
rm -f Assets/Screenshots/rel_*.png
M exec /tmp/gv_Store1320.cs; sleep 1
M call manage_editor '{"action":"play"}'; sleep 6; M shot rel_01_menu; sleep 1
M exec /tmp/newgame.cs; sleep 7; M shot rel_04_arrange; sleep 1
M exec /tmp/tomap.cs; sleep 5; M shot rel_02_map; sleep 1; M exec /tmp/openday.cs; sleep 2; M shot rel_03_dayboard; sleep 1
M exec /tmp/newgame.cs; sleep 7; M exec /tmp/startday.cs
for i in $(seq 1 60); do sleep 0.4; r=$(python3 Docs/tools/unity_mcp.py exec /tmp/state.cs 2>&1 | grep -o "orders [0-9] customers [0-9]"); case "$r" in "orders 2 customers 2") break;; esac; done
M shot rel_05_two_orders; sleep 1; M exec /tmp/serve.cs; sleep 1.2; M shot rel_06_served; sleep 1; M exec /tmp/serve.cs; sleep 0.3; M exec /tmp/serve.cs; sleep 0.3; M exec /tmp/serve.cs; sleep 1.5; M shot rel_07_receipt; sleep 1
for i in $(seq 1 90); do sleep 1; r=$(python3 Docs/tools/unity_mcp.py exec /tmp/state.cs 2>&1 | grep -o "dayLeft [0-9.]*"); d=${r#dayLeft }; python3 -c "import sys; sys.exit(0 if float('$d') < 12 else 1)" 2>/dev/null && break; done
M shot rel_08_dusk; sleep 1
for i in $(seq 1 90); do sleep 1; r=$(python3 Docs/tools/unity_mcp.py exec /tmp/state.cs 2>&1 | grep -o "orders [0-9] customers [0-9] dayLeft [0-9.]*"); case "$r" in "orders 0 customers 0 dayLeft 0.0") break;; esac; python3 Docs/tools/unity_mcp.py exec /tmp/serve.cs >/dev/null 2>&1; done
sleep 4; M shot rel_09_finish; sleep 1
M exec /tmp/newgame.cs; sleep 7; M exec /tmp/kitchen.cs; sleep 2; M shot rel_10_kitchen; sleep 1
M exec /tmp/toshop.cs; sleep 5; M shot rel_11_shop; sleep 1
M exec /tmp/tut.cs; sleep 14; M shot rel_12_tutorial; sleep 2
M call manage_editor '{"action":"stop"}'
ls Assets/Screenshots/rel_*.png | wc -l
