"""Kitchen arrangement test (16 November 2026): chapter 1 day 6 (or chapter 2 day 14 with "2") with everything bought,
left in the arrangement phase. Opens the kitchen, snaps the game's own places, then drives KitchenArrange's drag calls
with made-up pointer events: board onto the cake (red, Ready off), the table with the red handle, board into the big
cabinet (snap), blender into the shelf (too big, red), blender back, day start, kitchen during the day (no grab sheets),
and the places in the save. Snaps in /tmp/ftc/popframes/karr_<n>_<what>.png.
  python3 kitchen_arrange_test.py [2]"""
import sys, time, re
sys.path.insert(0, "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools")
import unity_mcp as U
A = "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools/autoplay/"
DAY = 14 if len(sys.argv) > 1 and sys.argv[1] == "2" else 6; TAG = "c2" if DAY == 14 else "c1"
def res(t):
    m = re.search(r'"result":"(.*?)","compiler"', t, re.S); return m.group(1) if m else t[-1200:]
def ex(code): return res(U.call("execute_code", {"action": "execute", "code": code}))
n = [0]
def snap(what):
    n[0] += 1; ex('ScreenCapture.CaptureScreenshot("/tmp/ftc/popframes/karr_%s_%d_%s.png"); return "snap";' % (TAG, n[0], what)); time.sleep(1.3)
SETUP = open(A + "kitchen_full.py").read().split("SETUP = '''")[1].split("'''")[0]
# drag a piece so its foot lands at (u, v) of the kitchen picture
MOVE = '''var area = GameObject.Find("Arrange").GetComponent<RectTransform>(); var cam = area.GetComponentInParent<Canvas>().rootCanvas.worldCamera;
var piece = area.Find("NAME") as RectTransform; if (piece == null) return "no NAME";
System.Func<Vector2, Vector2> scr = share => { var r = area.rect; var w = area.TransformPoint(new Vector3(r.xMin + share.x * r.width, r.yMin + share.y * r.height, 0f)); return RectTransformUtility.WorldToScreenPoint(cam, w); };
var t = typeof(KitchenArrange); var working = (System.Collections.Generic.Dictionary<string, Vector2>)t.GetField("working", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).GetValue(null);
var from = working["NAME"]; var e = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
e.position = scr(from); KitchenArrange.BeginDrag("NAME", e);
e.position = scr(Vector2.Lerp(from, new Vector2(TOU, TOV), 0.5f)); KitchenArrange.Drag("NAME", e);
e.position = scr(new Vector2(TOU, TOV)); KitchenArrange.Drag("NAME", e); var mid = piece.GetComponent<UnityEngine.UI.Image>().color;
if (HOLD) return "held at " + working["NAME"] + " colour " + mid;
KitchenArrange.EndDrag("NAME");
var done = UnityEngine.Object.FindObjectOfType<GamePhaseManager>().doneButton;
return "NAME " + from + " -> " + working["NAME"] + " red " + (piece.GetComponent<UnityEngine.UI.Image>().color.g < 0.9f) + " anyRed " + KitchenArrange.AnyRed + " ready " + done.interactable;'''
def move(name, u, v, hold=False):
    return ex(MOVE.replace("NAME", name).replace("TOU", "%sf" % u).replace("TOV", "%sf" % v).replace("HOLD", "true" if hold else "false"))
STATE = '''var area = GameObject.Find("Arrange"); if (area == null) return "no Arrange"; var sb = new System.Text.StringBuilder();
foreach (Transform c in area.transform) { var rt = (RectTransform)c; sb.Append(c.name + (c.gameObject.activeSelf ? "" : "(off)") + " grab " + (c.Find("Grab") != null) + " size " + rt.rect.size + " | "); }
sb.Append("area " + ((RectTransform)area.transform).rect.size + " anyRed " + KitchenArrange.AnyRed); return sb.ToString();'''
U.call("manage_editor", {"action": "stop"}); time.sleep(2)
print(ex('UnityEditor.AssetDatabase.Refresh(); return "refreshed";')); time.sleep(6)
U.call("manage_editor", {"action": "play"}); time.sleep(11)
print(ex(SETUP.replace("DAY", str(DAY)))); time.sleep(10)
for _ in range(12):
    if ex(open(A + "tutstep.cs").read()) == "T|none": break
    time.sleep(1.0)
print(ex('var b = UnityEngine.Object.FindObjectOfType<MenuBook>(); if (b != null && b.IsOpen) { b.Close(); return "menu closed"; } return "menu was closed";')); time.sleep(1.5)
snap("table_arranging")
print(ex('KitchenSceneManager.Instance.OpenKitchen(); return "kitchen";')); time.sleep(3)
print(ex(STATE)); snap("kitchen_default")
print(move("cutboard", 0.80, 0.44)); snap("board_on_cake_red")
print(ex('KitchenSceneManager.Instance.CloseKitchen(); return "closed";')); time.sleep(1.5); snap("table_handle_red")
print(ex('KitchenSceneManager.Instance.OpenKitchen(); return "kitchen";')); time.sleep(2.5); print(ex(STATE)); snap("kitchen_reopened_red")
print(move("cutboard", 0.20, 0.33)); snap("board_in_big_cabinet")
print(move("whisk", 0.42, 0.19)); print(move("basket", 0.40, 0.33)); snap("whisk_small_basket_big")
print(move("basket", 0.53, 0.50))
print(move("Blender", 0.27, 0.72)); snap("blender_in_shelf_red")
print(move("Blender", 0.30, 0.90)); snap("blender_dropped_nowhere")
print(move("Blender", 0.20, 0.545)); print(move("cake", 0.78, 0.50)); snap("blender_and_cake_moved")
print(move("juice", 0.61, 0.43, hold=True)); snap("juice_held_over_table"); print(ex('KitchenArrange.EndDrag("juice"); return "dropped anyRed " + KitchenArrange.AnyRed;'))
print(ex('KitchenSceneManager.Instance.CloseKitchen(); return "closed";')); time.sleep(1.5)
print(ex(open(A + "startday.cs").read())); time.sleep(4)
print(ex('var s = SessionManager.Instance.GetCurrentSession(); var sb = new System.Text.StringBuilder(); if (s.kitchenPlaces != null) foreach (var p in s.kitchenPlaces) sb.Append(p.foodType + " " + p.x.ToString("F3") + "," + p.y.ToString("F3") + " | "); return "saved " + sb;'))
print(ex('KitchenSceneManager.Instance.OpenKitchen(); return "kitchen";')); time.sleep(3); print(ex(STATE)); snap("kitchen_in_play")
print(ex('KitchenSceneManager.Instance.CloseKitchen(); return "closed";')); time.sleep(1.5); snap("table_in_play")
U.call("manage_editor", {"action": "stop"})
