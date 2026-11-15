"""Demo: a layer in front of the yoga lady and behind the customers (15 November 2026). She is shown at a given mat spot,
and a front layer (a full-canvas picture from Store/yoga_front_layer_2026-11-15/out) is stretched over the screen like
the background, between her and the customers. Nothing is saved in the project.
Usage: yoga_front_demo.py <chapter> tag:layer|none:matX:matY:pose ..."""
import sys, time, re
sys.path.insert(0, "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools")
import unity_mcp as U
A = "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools/autoplay/"
O = "/Users/betzthebest/Desktop/Sept/Store/yoga_front_layer_2026-11-15/out/"
def res(t):
    m = re.search(r'"result":"(.*?)","compiler"', t, re.S); return m.group(1) if m else t[-700:]
def ex(code): return res(U.call("execute_code", {"action": "execute", "code": code}))
ch = sys.argv[1]
U.call("manage_editor", {"action": "stop"}); time.sleep(2)
U.call("manage_editor", {"action": "play"}); time.sleep(9)
print(ex(open("/tmp/ftc/setup_day1.cs").read()))
print(ex('var sm = SessionManager.Instance; var s = sm.GetCurrentSession(); s.levelStars = new System.Collections.Generic.List<int> { 3, 3, 3, 3, 3 }; ' + ('s.levelsCompleted = 12; s.tutorial2Seen = true; ' if ch == "2" else '') + 'return "hired " + sm.BuyWithStars(YogaHire.ItemName, YogaHire.StarPrice);'))
print(ex(open(A + "goday.cs").read().replace("DAY", "12" if ch == "2" else "0"))); time.sleep(8)
for _ in range(12):
    r = ex(open(A + "tutstep.cs").read())
    if r == "T|none": break
    time.sleep(1.0)
print(ex(open(A + "startday.cs").read())); time.sleep(6.5)
ex('Time.timeScale = 0.02f; return "slow";')
SHOW = '''var old = GameObject.Find("YogaFrontDemo"); if (old != null) UnityEngine.Object.DestroyImmediate(old);
var y = UnityEngine.Object.FindObjectOfType<YogaHire>(); if (y == null) return "no yoga lady";
y.StopAllCoroutines(); var F = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var scale = y.transform.localScale.x; var pos = new Vector3(MXf, MYf + 0.715f * scale, 0.5f);
y.transform.position = pos; y.transform.localRotation = Quaternion.identity; typeof(YogaHire).GetField("home", F).SetValue(y, pos);
var fr = y.transform.Find("Front").GetComponent<SpriteRenderer>(); fr.sprite = Resources.Load<Sprite>("Yoga/sit_POSE"); fr.color = Color.white;
var layer = "LAYER"; if (layer == "none") return "shown, no front layer";
var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false); tex.LoadImage(System.IO.File.ReadAllBytes("PATH")); tex.wrapMode = TextureWrapMode.Clamp;
var sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.height / 10f);
var cam = Camera.main; var go = new GameObject("YogaFrontDemo"); go.transform.position = new Vector3(0f, 0f, 0.25f);
go.transform.localScale = new Vector3(cam.orthographicSize * 2f * cam.aspect / (tex.width / (tex.height / 10f)), 1f, 1f);
var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sp; sr.sortingLayerName = "Default"; sr.sortingOrder = 0;
return "shown with " + layer;'''
for a in sys.argv[2:]:
    tag, layer, mx, my, pose = a.split(":")
    print(tag, ex(SHOW.replace("MX", mx).replace("MY", my).replace("POSE", pose).replace("LAYER", layer).replace("PATH", O + layer + ".png"))); time.sleep(0.7)
    ex('ScreenCapture.CaptureScreenshot("/tmp/ftc/popframes/yf_ch%s_%s.png"); return "snap";' % (ch, tag)); time.sleep(1.2)
ex('Time.timeScale = 1f; return "speed";')
U.call("manage_editor", {"action": "stop"})
