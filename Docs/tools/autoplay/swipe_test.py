"""Kitchen swipe try-out check (14 November 2026): day 1 in play, swipe right (kitchen slides in), swipe left (slides out),
filmed; then the cases where it must not react. Usage: swipe_test.py tag"""
import sys, time, re, glob, os
sys.path.insert(0, "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools")
import unity_mcp as U
A = "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools/autoplay/"
def res(t):
    m = re.search(r'"result":"(.*?)","compiler"', t); return m.group(1) if m else t[-600:]
def ex(code): return res(U.call("execute_code", {"action": "execute", "code": code}))
tag = sys.argv[1]
for f in glob.glob("/tmp/ftc/popframes/%s_*.jpg" % tag): os.remove(f)
FILM = '''var lm = UnityEngine.Object.FindObjectOfType<LevelManager>(); if (lm == null) return "no lm";
System.Collections.IEnumerator Film() { float t0 = Time.unscaledTime, last = -1f; int n = 0; bool opened = false, closed = false;
  while (Time.unscaledTime - t0 < 2.6f) { yield return new WaitForEndOfFrame(); float t = Time.unscaledTime - t0;
    if (t - last >= 0.035f) { last = t; var tex = ScreenCapture.CaptureScreenshotAsTexture();
      System.IO.File.WriteAllBytes("/tmp/ftc/popframes/TAG_" + n.ToString("D3") + "_" + (t * 1000f).ToString("F0") + ".jpg", tex.EncodeToJPG(70)); UnityEngine.Object.Destroy(tex); n++; }
    if (!opened && t > 0.25f) { opened = true; Debug.Log("SWIPE open: " + KitchenSwipe.Swipe(1)); }
    if (!closed && t > 1.5f) { closed = true; Debug.Log("SWIPE close: " + KitchenSwipe.Swipe(-1)); } } }
lm.StartCoroutine(Film()); return "filming";'''.replace("TAG", tag)
STATE = 'return "open=" + KitchenSceneManager.Instance.IsKitchenOpen + " loaded=" + UnityEngine.SceneManagement.SceneManager.GetSceneByName("KitchenScene").isLoaded;'
U.call("manage_editor", {"action": "stop"}); time.sleep(2)
U.call("manage_editor", {"action": "play"}); time.sleep(9)
print(ex(open("/tmp/ftc/setup_day1.cs").read()))
print(ex(open(A + "goday.cs").read().replace("DAY", "0"))); time.sleep(7)
print("arranging, swipe right:", ex('return KitchenSwipe.Swipe(1);'), ex(STATE))
print(ex(open(A + "startday.cs").read())); time.sleep(1)
ex('var cmg = UnityEngine.Object.FindObjectOfType<CustomerManager>(); if (cmg != null) cmg.enabled = false; return "no customers";')
print("playing, swipe left with kitchen closed:", ex('return KitchenSwipe.Swipe(-1);'), ex(STATE))
print(ex(FILM)); time.sleep(4.5)
print("after film:", ex(STATE))
print("swipe right:", ex('return KitchenSwipe.Swipe(1);')); time.sleep(1.0); print("  ", ex(STATE))
print("swipe right again:", ex('return KitchenSwipe.Swipe(1);'), ex(STATE))
print("close by the kitchen's own button path:", ex('KitchenSceneManager.Instance.CloseKitchen(); return "closed";')); time.sleep(0.8); print("  ", ex(STATE))
print("paused, swipe right:", ex('Time.timeScale = 0f; var r = KitchenSwipe.Swipe(1); Time.timeScale = 1f; return r;'), ex(STATE))
print("table items tappable again:", ex('int on = 0, all = 0; foreach (var c in UnityEngine.Object.FindObjectsOfType<Collider2D>(true)) { all++; if (c.enabled) on++; } return on + " of " + all + " colliders on";'))
U.call("manage_editor", {"action": "stop"})
print(len(glob.glob("/tmp/ftc/popframes/%s_*.jpg" % tag)), "frames")
