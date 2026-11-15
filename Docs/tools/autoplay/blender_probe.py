import sys, time, re
sys.path.insert(0, "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools")
import unity_mcp as U
A = "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools/autoplay/"
def res(t):
    m = re.search(r'"result":"(.*?)","compiler"', t, re.S); return m.group(1) if m else t[-700:]
def ex(code): return res(U.call("execute_code", {"action": "execute", "code": code}))
tag = sys.argv[1]
U.call("manage_editor", {"action": "stop"}); time.sleep(2)
U.call("manage_editor", {"action": "play"}); time.sleep(9)
print(ex(open("/tmp/ftc/setup_day1.cs").read()))
print(ex('var s = SessionManager.Instance.GetCurrentSession(); s.levelsCompleted = 12; s.tutorial2Seen = true; return "ch2";'))
print(ex(open(A + "goday.cs").read().replace("DAY", "12"))); time.sleep(8)
for _ in range(12):
    r = ex(open(A + "tutstep.cs").read())
    if r == "T|none": break
    time.sleep(1.0)
print(ex(open(A + "startday.cs").read())); time.sleep(1.5)
print(ex('KitchenSceneManager.Instance.OpenKitchen(); return "open";')); time.sleep(2.0)
print(ex('''var sb = new System.Text.StringBuilder(); var b = GameObject.Find("Blender"); if (b == null) return "no blender";
var rt = (RectTransform)b.transform; var img = b.GetComponent<UnityEngine.UI.Image>(); var c = new Vector3[4]; rt.GetWorldCorners(c);
var cam = b.GetComponentInParent<Canvas>().worldCamera; Vector2 p0 = RectTransformUtility.WorldToScreenPoint(cam, c[0]), p2 = RectTransformUtility.WorldToScreenPoint(cam, c[2]);
sb.Append("blender sprite " + img.sprite.name + " " + img.sprite.rect.size + " tex " + img.sprite.texture.width + "x" + img.sprite.texture.height + " format " + img.sprite.texture.format + " filter " + img.sprite.texture.filterMode + " screen box " + p0.ToString("F1") + " .. " + p2.ToString("F1") + " screen " + Screen.width + "x" + Screen.height + " | ");
var bg = b.transform.parent; var bi = bg.GetComponent<UnityEngine.UI.Image>(); sb.Append("bg sprite " + (bi != null && bi.sprite != null ? bi.sprite.name + " " + bi.sprite.rect.size + " preserve " + bi.preserveAspect + " format " + bi.sprite.texture.format : "none") + " bg rect " + ((RectTransform)bg).rect.size + " | ");
foreach (Transform t in b.transform) { var ti = t.GetComponent<UnityEngine.UI.Image>(); sb.Append(t.name + (ti != null && ti.sprite != null ? " " + ti.sprite.name + " color " + ti.color : "") + "; "); }
return sb.ToString();'''))
ex('ScreenCapture.CaptureScreenshot("/tmp/ftc/popframes/blender_%s.png"); return "snap";' % tag); time.sleep(1.5)
print(ex('var b = GameObject.Find("Blender"); b.GetComponent<UnityEngine.UI.Image>().enabled = false; foreach (Transform t in b.transform) t.gameObject.SetActive(false); return "overlay hidden";')); time.sleep(0.5)
ex('ScreenCapture.CaptureScreenshot("/tmp/ftc/popframes/blender_%s_bgonly.png"); return "snap";' % tag); time.sleep(1.5)
U.call("manage_editor", {"action": "stop"})
