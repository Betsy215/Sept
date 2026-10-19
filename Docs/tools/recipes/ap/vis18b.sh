cd /Users/betzthebest/Desktop/Sept/FoodTruckCafe
M="python3 Docs/tools/unity_mcp.py"; STEP=Docs/tools/autoplay/step.cs
rm -f Docs/tools/.mcp_session
$M exec /tmp/ap/stop.cs; sleep 4
$M call read_console '{"action":"clear"}' > /dev/null
CH=1
$M exec /tmp/ap/play.cs; sleep 11
$M exec /tmp/ap/ch1_session.cs; sleep 12
$M shot v18_c1_arrange; sleep 1.5
$M exec /tmp/ap/menu_open.cs; sleep 2; $M shot v18_c1_menu; sleep 1.5
$M exec /tmp/ap/startday.cs; sleep 5
for i in $(seq 1 30); do $M exec $STEP > /dev/null; sleep 1; done
$M shot v18_c1_day; sleep 1.5
for i in $(seq 1 12); do $M exec $STEP > /dev/null; sleep 1; done
$M shot v18_c1_day2; sleep 1.5
$M exec /tmp/ap/openkitchen.cs; sleep 4; $M shot v18_c1_kitchen; sleep 1.5; $M exec /tmp/ap/closekitchen.cs; sleep 2
$M exec /tmp/ap/clock_end.cs; for i in $(seq 1 12); do $M exec $STEP > /dev/null; sleep 1; done
$M exec /tmp/ap/errs_dump.cs; $M shot v18_c1_finish; sleep 1.5
$M exec /tmp/ap/toshop.cs; sleep 6; $M shot v18_c1_shop; sleep 1.5
$M exec /tmp/ap/tomap.cs; sleep 6; $M shot v18_c1_map; sleep 1.5
$M exec /tmp/ap/tomenu.cs; sleep 5; $M shot v18_c1_mainmenu; sleep 1.5
$M call read_console '{"action":"get","types":["error"],"count":100,"include_stacktrace":false}' > /tmp/ap/console_c1.json
$M exec /tmp/ap/stop.cs; sleep 4
$M exec /tmp/ap/play.cs; sleep 11
$M exec /tmp/ap/ch2_full.cs; sleep 12
$M exec /tmp/ap/startday.cs; sleep 6
$M exec /tmp/ap/ice_build.cs; sleep 1.2; $M shot v18_c2_build; sleep 1.5
$M exec /tmp/ap/stop.cs
echo V18B_DONE
