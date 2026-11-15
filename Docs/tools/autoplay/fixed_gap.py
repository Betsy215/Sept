"""The gap between the fixed items at the back of the table (hen, candy jar, flower pots) and the food (owner, 15
November 2026: "show me how much the gap between fixed item and other, the collision box for fixed items, i need to he
minumised"). Chapter 1, day 1 running, every fixed item owned, the apple basket pushed as high as the game allows.
Writes /tmp/ftc/popframes/fixedgap_<tag>.png and .json (same format as table_boxes.py, drawn by table_boxes_draw.py:
green = each fixed item's picture box, white line = its lowest solid pixel).  Usage: fixed_gap.py <tag>"""
import sys, time, re, json
sys.path.insert(0, "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools")
import unity_mcp as U
A = "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools/autoplay/"
def res(t):
    m = re.search(r'"result":"(.*?)","compiler"', t, re.S); return m.group(1) if m else t[-900:]
def ex(code): return res(U.call("execute_code", {"action": "execute", "code": code}))
tag = sys.argv[1]
U.call("manage_editor", {"action": "stop"}); time.sleep(2)
U.call("manage_editor", {"action": "play"}); time.sleep(9)
print(ex(open("/tmp/ftc/setup_day1.cs").read()))
print(ex('var sm = SessionManager.Instance; return "pots " + sm.PurchaseDecor("PotFlowers") + " " + sm.PurchaseDecor("PotGreen") + " candies " + sm.PurchaseDecor("Candies");'))
print(ex(open(A + "goday.cs").read().replace("DAY", "0"))); time.sleep(9)
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
  if (MOVE && d.name.Contains("Apple")) { p.y = tb.max.y - off.y - half.y; d.SetRestPosition(p); }
  sb.Append("['" + d.name + "'," + n(p.x + off.x - half.x) + "," + n(p.y + off.y - half.y) + "," + n(p.x + off.x + half.x) + "," + n(p.y + off.y + half.y) + "],"); }
sb.Append("],'fixed':[],'fixedboxes':[");
var list = new System.Collections.Generic.List<SpriteRenderer>(); var decor = GameObject.Find("TableDecor");
if (decor != null) foreach (var nm in new[] { "PotFlowers", "PotGreen" }) { var t = decor.transform.Find(nm); if (t != null && t.gameObject.activeInHierarchy) list.Add(t.GetComponent<SpriteRenderer>()); }
var jar = UnityEngine.Object.FindObjectOfType<CandyJar>(); if (jar != null && jar.gameObject.activeInHierarchy) list.Add(jar.GetComponent<SpriteRenderer>());
var clock = UnityEngine.Object.FindObjectOfType<MoneyFlipClock>(); if (clock != null) list.Add(clock.transform.Find("Hen").GetComponent<SpriteRenderer>());
foreach (var sr in list) if (sr != null && sr.sprite != null) sb.Append("['" + sr.name + "'," + n(sr.bounds.min.x) + "," + n(sr.bounds.min.y) + "," + n(sr.bounds.max.x) + "," + n(sr.bounds.max.y) + "," + n(TableBackRowAlign.VisibleBottom(sr)) + "],");
sb.Append("]}"); return sb.ToString();'''
print(ex(DUMP.replace("MOVE", "true"))[:60]); time.sleep(1.0)
print(ex(open(A + "startday.cs").read())); time.sleep(4)
raw = ex(DUMP.replace("MOVE", "false")); data = json.loads(raw.replace("'", '"').replace(",]", "]"))
json.dump(data, open("/tmp/ftc/popframes/boxes_fixedgap_%s.json" % tag, "w"), indent=1)
ex('ScreenCapture.CaptureScreenshot("/tmp/ftc/popframes/boxes_fixedgap_%s.png"); return "snap";' % tag); time.sleep(1.2)
print(json.dumps(data)[:900])
U.call("manage_editor", {"action": "stop"})
