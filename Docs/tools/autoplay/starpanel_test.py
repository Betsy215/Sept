"""Day-end card check (14 November 2026): play day 1, keep the lost count at LOST, and film the card's stars popping in.
Usage: starpanel_test.py tag LOST"""
import sys, time, re, glob, os
sys.path.insert(0, "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools")
import unity_mcp as U
A = "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools/autoplay/"
def res(t):
    m = re.search(r'"result":"(.*?)","compiler"', t); return m.group(1) if m else t[-300:]
def ex(code): return res(U.call("execute_code", {"action": "execute", "code": code}))
tag = sys.argv[1]; LOST = int(sys.argv[2])
for f in glob.glob("/tmp/ftc/popframes/%s_*.jpg" % tag): os.remove(f)
FORCE = '''var os = UnityEngine.Object.FindObjectOfType<OrderSystem>(); if (os == null) return "no os";
var F = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
typeof(OrderSystem).GetField("customersLost", F).SetValue(os, LOST); if (os.CustomersServed < 8) typeof(OrderSystem).GetField("customersServed", F).SetValue(os, 8); return "forced";'''.replace("LOST", str(LOST))
FILM = '''var lm = UnityEngine.Object.FindObjectOfType<LevelManager>(); if (lm == null) return "no lm";
System.Collections.IEnumerator Film() { float t0 = Time.time, last = -1f; int n = 0;
  while (Time.time - t0 < 7.5f) { yield return new WaitForEndOfFrame();
    if (Time.time - t0 >= 2.2f && Time.time - last >= 0.05f) { last = Time.time; var tex = ScreenCapture.CaptureScreenshotAsTexture();
      System.IO.File.WriteAllBytes("/tmp/ftc/popframes/TAG_" + n.ToString("D3") + "_" + ((Time.time - t0) * 1000f).ToString("F0") + ".jpg", tex.EncodeToJPG(80)); UnityEngine.Object.Destroy(tex); n++; } } }
lm.StartCoroutine(Film()); return "filming";'''.replace("TAG", tag)
U.call("manage_editor", {"action": "stop"}); time.sleep(2)
U.call("manage_editor", {"action": "play"}); time.sleep(9)
print(ex(open("/tmp/ftc/setup_day1.cs").read()))
print(ex(open(A + "goday.cs").read().replace("DAY", "0"))); time.sleep(7)
print(ex(open(A + "startday.cs").read())); time.sleep(1)
ex('UnityEngine.Time.timeScale = 2f; return "speed";')
t0 = time.time()
while time.time() - t0 < 200:
    ex(FORCE)
    r = ex(open(A + "step.cs").read()); kind = r.split("|")[1] if "|" in r else r
    e = ex('var lm = UnityEngine.Object.FindObjectOfType<LevelManager>(); return lm == null ? "?" : typeof(LevelManager).GetField("isLevelEnding", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(lm).ToString();')
    if e == "True" or kind == "done": break
    time.sleep(0.2)
print(ex('UnityEngine.Time.timeScale = 1f; ' + FILM)); time.sleep(10)
print(ex('var sc = UnityEngine.Object.FindObjectOfType<ScoreManager>(); return "stars " + sc.starProgressBar.GetEarnedStarsCount() + " saved " + SessionManager.Instance.GetLevelStars(0);'))
U.call("manage_camera", {"action": "screenshot", "screenshot_file_name": "starpanel_" + tag, "properties": {"include_image": False}}); time.sleep(1.5)
U.call("manage_editor", {"action": "stop"})
print(len(glob.glob("/tmp/ftc/popframes/%s_*.jpg" % tag)), "frames")
