cd /Users/betzthebest/Desktop/Sept/FoodTruckCafe
M="python3 Docs/tools/unity_mcp.py"; STEP=Docs/tools/autoplay/step.cs
rm -f Docs/tools/.mcp_session
$M exec /tmp/ap/stop.cs > /dev/null; sleep 3; rm -f Docs/tools/.mcp_session
$M exec /tmp/ap/refresh.cs > /dev/null; sleep 20; rm -f Docs/tools/.mcp_session
$M call read_console '{"action":"clear"}' > /dev/null
$M exec /tmp/ap/play.cs | grep -o '"result":"[^"]*"'; sleep 11; rm -f Docs/tools/.mcp_session
$M exec /tmp/ap/ch2_full.cs | grep -o '"result":"[^"]*"'; sleep 12
$M shot v19_c2_arrange > /dev/null; sleep 1.5
$M exec /tmp/ap/menu_open.cs | grep -o '"result":"[^"]*"'; sleep 2; $M shot v19_c2_menu > /dev/null; sleep 1.5
$M exec /tmp/ap/startday.cs | grep -o '"result":"[^"]*"'; sleep 6
$M shot v19_c2_day0 > /dev/null; sleep 1.5
$M exec /tmp/ap/ice_build.cs | grep -o '"result":"[^"]*"'; sleep 1.2; $M shot v19_c2_build > /dev/null; sleep 1.5
sed "s/KIND/Cross/; s/IDX/0/" /tmp/ap/ice_tap.cs > /tmp/ap/tap_x.cs; $M exec /tmp/ap/tap_x.cs > /dev/null; sleep 1
for i in $(seq 1 25); do $M exec $STEP > /dev/null; sleep 1; done
$M shot v19_c2_day > /dev/null; sleep 1.5
$M exec /tmp/ap/toshop.cs | grep -o '"result":"[^"]*"'; sleep 6
for i in $(seq 0 12); do sed "s/INDEX/$i/g" /tmp/ap/popup_n.cs > /tmp/ap/popup_x.cs; R=$($M exec /tmp/ap/popup_x.cs); echo "$R" | grep -o '"result":"[^"]*"'; echo "$R" | grep -q "END" && break; sleep 1.2; $M shot pp22_$i > /dev/null; sleep 1.2; done
$M call read_console '{"action":"get","types":["error"],"count":60,"include_stacktrace":false}' > /tmp/ap/console_v19.json
$M exec /tmp/ap/stop.cs | grep -o '"result":"[^"]*"'
echo V19_DONE
