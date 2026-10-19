cd /Users/betzthebest/Desktop/Sept/FoodTruckCafe
M="python3 Docs/tools/unity_mcp.py"; STEP=Docs/tools/autoplay/step.cs
rm -f Docs/tools/.mcp_session
$M exec /tmp/ap/refresh.cs; n=0; until [ Library/ScriptAssemblies/Assembly-CSharp.dll -nt Assets/Scripts/Shop/ShopManager.cs ] || [ $n -ge 90 ]; do sleep 2; n=$((n+1)); done; echo "dll ready after $n"; sleep 8; rm -f Docs/tools/.mcp_session
$M call read_console '{"action":"clear"}' > /dev/null
for CH in 1 2; do
  $M exec /tmp/ap/play.cs; sleep 11
  if [ $CH = 1 ]; then $M exec /tmp/ap/ch1_session.cs; else $M exec /tmp/ap/ch2_full.cs; fi; sleep 12
  $M shot v18_c${CH}_arrange; sleep 1.5
  $M exec /tmp/ap/menu_open.cs; sleep 2; $M shot v18_c${CH}_menu; sleep 1.5
  $M exec /tmp/ap/startday.cs; sleep 5
  for i in $(seq 1 30); do $M exec $STEP > /dev/null; sleep 1; done
  $M shot v18_c${CH}_day; sleep 1.5
  for i in $(seq 1 12); do $M exec $STEP > /dev/null; sleep 1; done
  $M shot v18_c${CH}_day2; sleep 1.5
  $M exec /tmp/ap/openkitchen.cs; sleep 4; $M shot v18_c${CH}_kitchen; sleep 1.5; $M exec /tmp/ap/closekitchen.cs; sleep 2
  $M exec /tmp/ap/clock_end.cs; for i in $(seq 1 12); do $M exec $STEP > /dev/null; sleep 1; done
  $M exec /tmp/ap/errs_dump.cs; $M shot v18_c${CH}_finish; sleep 1.5
  $M exec /tmp/ap/toshop.cs; sleep 6; $M shot v18_c${CH}_shop; sleep 1.5
  $M exec /tmp/ap/tomap.cs; sleep 6; $M shot v18_c${CH}_map; sleep 1.5
  $M exec /tmp/ap/tomenu.cs; sleep 5; $M shot v18_c${CH}_mainmenu; sleep 1.5
  $M call read_console '{"action":"get","types":["error"],"count":100,"include_stacktrace":false}' > /tmp/ap/console_c${CH}.json
  $M exec /tmp/ap/stop.cs; sleep 4
done
echo V18_DONE
