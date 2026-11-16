"""The new choux platter on the real table (owner, 16 November 2026: the empty plate first). Chapter 1 day 6 with
everything bought; the Choux sprite is swapped in play mode for a picture on disk (same pixels per unit and pivot as
the current frames), and the table is snapped to /tmp/ftc/popframes/platter_<tag>.png. Nothing in the project changes.
  python3 platter_test.py <png> <tag> [chapter]"""
import sys, time, re
sys.path.insert(0, "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools")
import unity_mcp as U
A = "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools/autoplay/"
PNG, TAG = sys.argv[1], sys.argv[2]; DAY = 14 if len(sys.argv) > 3 and sys.argv[3] == "2" else 6
def res(t):
    m = re.search(r'"result":"(.*?)","compiler"', t, re.S); return m.group(1) if m else t[-900:]
def ex(code): return res(U.call("execute_code", {"action": "execute", "code": code}))
SETUP = open(A + "kitchen_full.py").read().split("SETUP = '''")[1].split("'''")[0]
SWAP = '''var c = UnityEngine.Object.FindObjectOfType<Choux>(); if (c == null) return "no choux";
var sr = c.GetComponent<SpriteRenderer>(); var old = sr.sprite;
var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false); tex.LoadImage(System.IO.File.ReadAllBytes("PNG"));
var piv = new Vector2(old.pivot.x / old.rect.width, old.pivot.y / old.rect.height);
sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), piv, old.pixelsPerUnit * tex.width / old.rect.width);
c.enabled = false;
return "swapped " + old.name + " ppu " + old.pixelsPerUnit + " size " + old.rect.size + " pivot " + piv;'''.replace("PNG", PNG)
U.call("manage_editor", {"action": "stop"}); time.sleep(2)
U.call("manage_editor", {"action": "play"}); time.sleep(10)
print(ex(SETUP.replace("DAY", str(DAY)))); time.sleep(10)
for _ in range(12):
    if ex(open(A + "tutstep.cs").read()) == "T|none": break
    time.sleep(1.0)
print(ex(open(A + "startday.cs").read())); time.sleep(4)
print(ex(SWAP)); time.sleep(1)
ex('Time.timeScale = 0f; return "paused";'); time.sleep(0.5)
ex('ScreenCapture.CaptureScreenshot("/tmp/ftc/popframes/platter_%s.png"); return "snap";' % TAG); time.sleep(1.5)
ex('Time.timeScale = 1f; return "on";')
U.call("manage_editor", {"action": "stop"})
