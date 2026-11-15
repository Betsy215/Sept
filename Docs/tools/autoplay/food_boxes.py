"""Every food's drag box on the table, drawn on a screenshot (15 November 2026, owner: "Cookie collision box seems big,
show me and fix it"). Chapter 1 day 2 in the arrangement phase with all chapter 1 foods and the cookies on the menu.
Writes /tmp/ftc/popframes/boxes_<tag>.png/.json, drawn by table_boxes_draw.py (red = the cookies, blue = the others).
Usage: food_boxes.py <tag>"""
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
print(ex('''var sm = SessionManager.Instance; var s = sm.GetCurrentSession(); s.levelsCompleted = 1; s.levelStars = new System.Collections.Generic.List<int> { 3 };
foreach (var f in new[] { "Juice", "Cake", "Choux", "Cookies" }) if (!s.purchasedFoodItems.Contains(f)) s.purchasedFoodItems.Add(f);
foreach (var f in new[] { "Juice", "Cake", "Choux", "Cookies" }) sm.SetFoodEnabled(f, true); return "on the menu: " + string.Join(",", s.enabledFoodItems);'''))
print(ex(open(A + "goday.cs").read().replace("DAY", "1"))); time.sleep(9)
for _ in range(12):
    r = ex(open(A + "tutstep.cs").read())
    if r == "T|none": break
    time.sleep(1.0)
ex('var b = UnityEngine.Object.FindObjectOfType<MenuBook>(); if (b != null && b.IsOpen) b.SendMessage("Close", SendMessageOptions.DontRequireReceiver); return "menu";'); time.sleep(1)
DUMP = '''var tl = UnityEngine.Object.FindObjectOfType<TableLayer>(); var tb = tl.InteractiveBounds; var cloth = tl.tableClothSprite.bounds; var cam = Camera.main;
var F = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var sb = new System.Text.StringBuilder(); System.Func<float, string> n = v => v.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);
sb.Append("{'cam':[" + n(cam.orthographicSize) + "," + n(cam.aspect) + "],'area':[" + n(tb.min.x) + "," + n(tb.min.y) + "," + n(tb.max.x) + "," + n(tb.max.y) + "],'cloth':[" + n(cloth.min.y) + "," + n(cloth.max.y) + "],'foods':[");
foreach (var d in UnityEngine.Object.FindObjectsOfType<DraggableFood>()) {
  var half = d.HalfSize; var off = (Vector2)typeof(DraggableFood).GetField("centreOffset", F).GetValue(d); var p = d.transform.position;
  var sr = d.GetComponent<SpriteRenderer>(); var sz = sr != null ? sr.bounds.size : Vector3.zero;
  sb.Append("['" + (d.name == "Cookies" ? "IceCream" : d.name) + "'," + n(p.x + off.x - half.x) + "," + n(p.y + off.y - half.y) + "," + n(p.x + off.x + half.x) + "," + n(p.y + off.y + half.y) + "],"); }
sb.Append("],'fixed':[]}"); return sb.ToString();'''
raw = ex(DUMP); data = json.loads(raw.replace("'", '"').replace(",]", "]"))
json.dump(data, open("/tmp/ftc/popframes/boxes_%s.json" % tag, "w"), indent=1)
print(ex('var sb = new System.Text.StringBuilder(); var F = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public; foreach (var d in UnityEngine.Object.FindObjectsOfType<DraggableFood>()) sb.Append(d.name + " half " + d.HalfSize + " gap " + typeof(DraggableFood).GetField("minDistanceBetweenItems", F).GetValue(d) + "; "); return sb.ToString();'))
ex('ScreenCapture.CaptureScreenshot("/tmp/ftc/popframes/boxes_%s.png"); return "snap";' % tag); time.sleep(1.2)
print(json.dumps(data)[:600])
U.call("manage_editor", {"action": "stop"})
