cd /Users/betzthebest/Desktop/Sept/FoodTruckCafe
M="python3 Docs/tools/unity_mcp.py"
rm -f Docs/tools/.mcp_session
$M exec /tmp/ap/refresh.cs > /dev/null; sleep 15; rm -f Docs/tools/.mcp_session
$M exec /tmp/ap/play.cs | grep -o '"result":"[^"]*"'; sleep 9; rm -f Docs/tools/.mcp_session
$M exec /tmp/ap/toshop.cs | grep -o '"result":"[^"]*"'; sleep 6
for i in $(seq 0 12); do sed "s/INDEX/$i/g" /tmp/ap/popup_n.cs > /tmp/ap/popup_x.cs; R=$($M exec /tmp/ap/popup_x.cs); echo "$R" | grep -o '"result":"[^"]*"'; echo "$R" | grep -q "END" && break; sleep 1.2; $M shot pp24_$i > /dev/null; sleep 1.2; done
$M exec /tmp/ap/stop.cs | grep -o '"result":"[^"]*"'
echo V20_DONE
