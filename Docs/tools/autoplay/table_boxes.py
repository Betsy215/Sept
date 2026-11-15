"""Shows where food may stand on the table (owner, 15 November 2026: "can you show me the gap, or the collision box of
ice cream station for both default and leveled up one"). Opens a chapter 2 day in the arrangement phase, puts the ice
cream station as low as the game allows, and writes a screenshot with the boxes drawn on it:
yellow = the area food may stand in, red = the station's own box (what must stay inside the yellow), blue = the other
foods' boxes, white lines = the cloth's edge and the lowest point of the fixed items.
Usage: table_boxes.py <tag> <ice cream level 1|2> [chapter 1|2]     -> /tmp/ftc/popframes/boxes_<tag>.png and .json"""
import sys, time, re, json
sys.path.insert(0, "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools")
import unity_mcp as U
A = "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools/autoplay/"
def res(t):
    m = re.search(r'"result":"(.*?)","compiler"', t, re.S); return m.group(1) if m else t[-900:]
def ex(code): return res(U.call("execute_code", {"action": "execute", "code": code}))
tag, level = sys.argv[1], sys.argv[2]; chapter = sys.argv[3] if len(sys.argv) > 3 else "2"
U.call("manage_editor", {"action": "stop"}); time.sleep(2)
U.call("manage_editor", {"action": "play"}); time.sleep(9)
print(ex(open("/tmp/ftc/setup_day1.cs").read()))
print(ex('var sm = SessionManager.Instance; var s = sm.GetCurrentSession(); if (%s) { s.levelsCompleted = 12; s.tutorial2Seen = true; } if (s.foodUpgradeLevels == null) s.foodUpgradeLevels = new System.Collections.Generic.Dictionary<string, int>(); s.foodUpgradeLevels["IceCream"] = %s; return "ice cream level " + sm.GetFoodUpgradeLevel("IceCream");' % ("true" if chapter == "2" else "false", level)))
print(ex(open(A + "goday.cs").read().replace("DAY", "12" if chapter == "2" else "0"))); time.sleep(9)
for _ in range(12):
    r = ex(open(A + "tutstep.cs").read())
    if r == "T|none": break
    time.sleep(1.0)
DUMP = '''var tl = UnityEngine.Object.FindObjectOfType<TableLayer>(); var tb = tl.InteractiveBounds; var cloth = tl.tableClothSprite.bounds; var cam = Camera.main;
var F = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var sb = new System.Text.StringBuilder(); System.Func<float, string> n = v => v.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);
sb.Append("{'cam':[" + n(cam.orthographicSize) + "," + n(cam.aspect) + "],'area':[" + n(tb.min.x) + "," + n(tb.min.y) + "," + n(tb.max.x) + "," + n(tb.max.y) + "],'cloth':[" + n(cloth.min.y) + "," + n(cloth.max.y) + "],'reserve':" + n(tl.topReserve) + ",'pad':" + n(tl.tableBoundsPadding) + ",'foods':[");
foreach (var d in UnityEngine.Object.FindObjectsOfType<DraggableFood>()) {
  var half = d.HalfSize; var off = (Vector2)typeof(DraggableFood).GetField("centreOffset", F).GetValue(d); var p = d.transform.position;
  
  sb.Append("['" + d.name + "'," + n(p.x + off.x - half.x) + "," + n(p.y + off.y - half.y) + "," + n(p.x + off.x + half.x) + "," + n(p.y + off.y + half.y) + "],"); }
sb.Append("],'fixed':[");
foreach (var name in new[] { "MoneyFlipClock", "TipJar", "FlowerPots", "KitchenButton" }) { var go = GameObject.Find(name); if (go == null) continue; var lo = float.MaxValue; foreach (var r in go.GetComponentsInChildren<SpriteRenderer>()) if (r.enabled && r.sprite != null && r.color.a > 0.5f) lo = Mathf.Min(lo, r.bounds.min.y); if (lo < float.MaxValue) sb.Append("['" + name + "'," + n(lo) + "],"); }
sb.Append("]}"); return sb.ToString();'''
print(ex(DUMP.replace("MOVE", "true"))[:80]); time.sleep(0.8)
raw = ex(DUMP.replace("MOVE", "false")); data = json.loads(raw.replace("'", '"').replace(",]", "]"))
json.dump(data, open("/tmp/ftc/popframes/boxes_%s.json" % tag, "w"), indent=1)
ex('ScreenCapture.CaptureScreenshot("/tmp/ftc/popframes/boxes_%s.png"); return "snap";' % tag); time.sleep(1.2)
print(json.dumps(data)[:700])
U.call("manage_editor", {"action": "stop"})
