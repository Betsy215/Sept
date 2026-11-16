"""The blender's glow painted with glow_brush.py (owner, 15 November 2026: make it like the coffee bags' and the
tree fruit's). Chapter 2 kitchen with everything bought, snapped to /tmp/ftc/popframes/kglow_<tag>.png.
  python3 glow_brush_test.py <tag>      (copy the glow picture to test into Resources/Kitchen/blender_glow.png first)"""
import sys, time, re
sys.path.insert(0, "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools")
import unity_mcp as U
A = "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools/autoplay/"
TAG = sys.argv[1]
def res(t):
    m = re.search(r'"result":"(.*?)","compiler"', t, re.S); return m.group(1) if m else t[-900:]
def ex(code): return res(U.call("execute_code", {"action": "execute", "code": code}))
SETUP = open(A + "kitchen_full.py").read().split("SETUP = '''")[1].split("'''")[0]
U.call("manage_editor", {"action": "stop"}); time.sleep(2)
print(ex('UnityEditor.AssetDatabase.ImportAsset("Assets/Resources/Kitchen/blender_glow.png", UnityEditor.ImportAssetOptions.ForceUpdate); return "imported";')); time.sleep(2)
U.call("manage_editor", {"action": "play"}); time.sleep(10)
print(ex(SETUP.replace("DAY", "14"))); time.sleep(10)
for _ in range(12):
    if ex(open(A + "tutstep.cs").read()) == "T|none": break
    time.sleep(1.0)
print(ex(open(A + "startday.cs").read())); time.sleep(5)
print(ex('KitchenSceneManager.Instance.OpenKitchen(); return "kitchen";')); time.sleep(3)
ex('ScreenCapture.CaptureScreenshot("/tmp/ftc/popframes/kglow_%s.png"); return "snap";' % TAG); time.sleep(1.5)
print(ex('var g = GameObject.Find("Blender"); return g == null ? "no blender" : "blender at " + g.transform.position;'))
U.call("manage_editor", {"action": "stop"})
