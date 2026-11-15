"""Demo of a picture layer between the customers and the table (15 November 2026): strips from Store/bg_layer_2026-11-15/out
are shown on the sorting layer between Default (customers) and Table, in a running day, and captured. Nothing is saved
in the project. Usage: bglayer_demo.py <chapter 1|2> name[:yOffset] ..."""
import sys, time, re
sys.path.insert(0, "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools")
import unity_mcp as U
A = "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools/autoplay/"
O = "/Users/betzthebest/Desktop/Sept/Store/bg_layer_2026-11-15/out/"
def res(t):
    m = re.search(r'"result":"(.*?)","compiler"', t, re.S); return m.group(1) if m else t[-600:]
def ex(code): return res(U.call("execute_code", {"action": "execute", "code": code}))
ch = sys.argv[1]
U.call("manage_editor", {"action": "stop"}); time.sleep(2)
U.call("manage_editor", {"action": "play"}); time.sleep(9)
print(ex(open("/tmp/ftc/setup_day1.cs").read()))
if ch == "2": print(ex('var s = SessionManager.Instance.GetCurrentSession(); s.levelsCompleted = 12; s.tutorial2Seen = true; return "ch2";'))
print(ex(open(A + "goday.cs").read().replace("DAY", "12" if ch == "2" else "0"))); time.sleep(8)
for _ in range(12):
    r = ex(open(A + "tutstep.cs").read())
    if r == "T|none": break
    time.sleep(1.0)
print(ex(open(A + "startday.cs").read())); time.sleep(6.5)
ex('Time.timeScale = 0.02f; return "slow";')
ex('ScreenCapture.CaptureScreenshot("/tmp/ftc/popframes/bgl_ch%s_none.png"); return "snap";' % ch); time.sleep(1.2)
SHOW = '''var old = GameObject.Find("BgLayerDemo"); if (old != null) UnityEngine.Object.DestroyImmediate(old);
var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false); tex.LoadImage(System.IO.File.ReadAllBytes("PATH")); tex.wrapMode = TextureWrapMode.Clamp;
var sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0f), tex.width / 4.6f);
var go = new GameObject("BgLayerDemo"); go.transform.position = new Vector3(0f, YOFFf, 0f);
var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sp; sr.sortingLayerName = "Customoers"; sr.sortingOrder = 0;
return "shown " + tex.width + "x" + tex.height + " layer " + sr.sortingLayerName + " customers " + UnityEngine.Object.FindObjectsOfType<CustomerController>().Length;'''
for a in sys.argv[2:]:
    name, _, yo = a.partition(":"); yo = yo or "-0.06"
    print(name, ex(SHOW.replace("PATH", O + name + ".png").replace("YOFF", yo))); time.sleep(0.6)
    ex('ScreenCapture.CaptureScreenshot("/tmp/ftc/popframes/bgl_ch%s_%s.png"); return "snap";' % (ch, name)); time.sleep(1.2)
ex('Time.timeScale = 1f; return "speed";')
U.call("manage_editor", {"action": "stop"})
