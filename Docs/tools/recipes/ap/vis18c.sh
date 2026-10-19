cd /Users/betzthebest/Desktop/Sept/FoodTruckCafe
M="python3 Docs/tools/unity_mcp.py"
n=0; until grep -q SPOT_DONE /tmp/ap/spot_test.log 2>/dev/null || [ $n -ge 60 ]; do sleep 3; n=$((n+1)); done
rm -f Docs/tools/.mcp_session; $M exec /tmp/ap/stop.cs; sleep 3
cp Assets/Resources/Chapter2/cloth.png /tmp/ap/cloth_keep.png
for T in A B C D; do
  cp /Users/betzthebest/Desktop/Sept/Store/chapter2/table/cloth/texture_$T.png Assets/Resources/Chapter2/cloth.png
  $M exec /tmp/ap/refresh.cs; sleep 6; rm -f Docs/tools/.mcp_session
  $M exec /tmp/ap/play.cs; sleep 10
  $M exec /tmp/ap/ch2_full.cs; sleep 11
  $M exec /tmp/ap/startday.cs; sleep 5
  $M shot cloth_tex_$T; sleep 1.5
  $M exec /tmp/ap/stop.cs; sleep 4
done
cp /tmp/ap/cloth_keep.png Assets/Resources/Chapter2/cloth.png; $M exec /tmp/ap/refresh.cs; sleep 6; rm -f Docs/tools/.mcp_session
# the ice cream build, step by step, and the shop popups
$M exec /tmp/ap/play.cs; sleep 10
$M exec /tmp/ap/ch2_full.cs; sleep 11
$M exec /tmp/ap/startday.cs; sleep 5
$M shot icb_0; sleep 1.2
sed "s/KIND/Holder/; s/IDX/0/" /tmp/ap/ice_tap.cs > /tmp/ap/tap_x.cs; $M exec /tmp/ap/tap_x.cs; sleep 1; $M shot icb_1; sleep 1.2
sed "s/KIND/Section/; s/IDX/1/" /tmp/ap/ice_tap.cs > /tmp/ap/tap_x.cs; $M exec /tmp/ap/tap_x.cs; sleep 0.8; $M shot icb_2; sleep 1.2
sed "s/KIND/Section/; s/IDX/2/" /tmp/ap/ice_tap.cs > /tmp/ap/tap_x.cs; $M exec /tmp/ap/tap_x.cs; sleep 0.5
sed "s/KIND/Section/; s/IDX/3/" /tmp/ap/ice_tap.cs > /tmp/ap/tap_x.cs; $M exec /tmp/ap/tap_x.cs; sleep 0.8; $M shot icb_3; sleep 1.2
sed "s/KIND/Cross/; s/IDX/0/" /tmp/ap/ice_tap.cs > /tmp/ap/tap_x.cs; $M exec /tmp/ap/tap_x.cs; sleep 0.8; $M shot icb_4; sleep 1.2
$M exec /tmp/ap/toshop.cs; sleep 6
for i in $(seq 0 15); do sed "s/INDEX/$i/" /tmp/ap/popup_n.cs > /tmp/ap/popup_x.cs; R=$($M exec /tmp/ap/popup_x.cs); echo "$R" | grep -o '"result":"[^"]*"'; echo "$R" | grep -q "END" && break; sleep 1.2; $M shot pp18_$i; sleep 1.2; done
$M exec /tmp/ap/stop.cs
echo V18C_DONE
