"""Tutorial swipe steps check (14 November 2026): fresh session, the real tutorial; taps are answered like the autoplay,
the two swipe steps are filmed (arrow sliding, then the swipe, then the kitchen's slide). Usage: tut_swipe_test.py"""
import sys, time, re, glob, os
sys.path.insert(0, "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools")
import unity_mcp as U
A = "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools/autoplay/"
def res(t):
    m = re.search(r'"result":"(.*?)","compiler"', t); return m.group(1) if m else t[-600:]
def ex(code): return res(U.call("execute_code", {"action": "execute", "code": code}))
for f in glob.glob("/tmp/ftc/popframes/tut*_*.jpg"): os.remove(f)
STEP = '''var td = UnityEngine.Object.FindObjectOfType<TutorialDirector>(); if (td == null) return "T|none";
var F = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance; var t = typeof(TutorialDirector);
var end = (GameObject)t.GetField("endCard", F).GetValue(td);
if (end != null && end.activeInHierarchy) return "T|endcard";
var cap = (TMPro.TextMeshProUGUI)t.GetField("caption", F).GetValue(td);
var sw = (int)t.GetField("awaitingSwipe", F).GetValue(td);
if (sw != 0) return "T|swipe|" + sw + "|" + cap.text;
var hit = t.GetField("awaitingHit", F);
if (hit.GetValue(td) != null) { hit.SetValue(td, null); t.GetField("tapped", F).SetValue(td, true); return "T|tapped|" + cap.text; }
return "T|wait|" + (cap != null ? cap.text : "");'''
FILM = '''var td = UnityEngine.Object.FindObjectOfType<TutorialDirector>(); if (td == null) return "no td";
System.Collections.IEnumerator Film() { float t0 = Time.unscaledTime, last = -1f; int n = 0; bool done = false;
  while (Time.unscaledTime - t0 < 3.0f) { yield return new WaitForEndOfFrame(); float t = Time.unscaledTime - t0;
    if (t - last >= 0.045f) { last = t; var tex = ScreenCapture.CaptureScreenshotAsTexture();
      System.IO.File.WriteAllBytes("/tmp/ftc/popframes/TAG_" + n.ToString("D3") + "_" + (t * 1000f).ToString("F0") + ".jpg", tex.EncodeToJPG(70)); UnityEngine.Object.Destroy(tex); n++; }
    if (!done && t > 1.9f) { done = true; if (td != null) td.OnSwipe(DIR); } } }
KitchenSceneManager.Instance.StartCoroutine(Film()); return "filming";'''
U.call("manage_editor", {"action": "stop"}); time.sleep(2)
U.call("manage_editor", {"action": "play"}); time.sleep(9)
print(ex(open(A + "fresh.cs").read()))
print(ex('var cp = UnityEngine.Object.FindObjectOfType<ClickPlay>(); if (cp == null) return "no ClickPlay"; var e = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current); cp.OnPointerDown(e); cp.OnPointerUp(e); return "play clicked";'))
time.sleep(10)
t0 = time.time(); seen = set(); n = 0
while time.time() - t0 < 200:
    r = ex(STEP)
    if r not in seen: print("  ", r, flush=True); seen.add(r)
    if r.startswith("T|swipe"):
        n += 1; d = r.split("|")[2]
        print("   wrong-way swipe first:", ex('UnityEngine.Object.FindObjectOfType<TutorialDirector>().OnSwipe(%d); return "sent";' % -int(d)), ex(STEP)[:30])
        print("  ", ex(FILM.replace("TAG", "tut%d" % n).replace("DIR", d))); time.sleep(4.5)
        print("   kitchen open:", ex('return KitchenSceneManager.Instance.IsKitchenOpen.ToString();'))
        seen.discard(r)
    if r in ("T|endcard", "T|none"): break
    time.sleep(0.8)
U.call("manage_camera", {"action": "screenshot", "screenshot_file_name": "tut_swipe_end", "properties": {"include_image": False}}); time.sleep(1.5)
U.call("manage_editor", {"action": "stop"})
print(len(glob.glob("/tmp/ftc/popframes/tut1_*.jpg")), len(glob.glob("/tmp/ftc/popframes/tut2_*.jpg")), "frames")
