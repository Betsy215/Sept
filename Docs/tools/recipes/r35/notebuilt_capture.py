"""The taped note built into the game (27 Nov 2026): day-1 tutorial steps, the day-2 menu lesson, the arrange hint
appearing (frame by frame), the kitchen hint, and the menu lesson on iPad. STAGE=refresh,tutorial,day2,ipad"""
import sys, os, re, time, glob
sys.path.insert(0, "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools")
import unity_mcp as U
P = "/Users/betzthebest/Desktop/Sept/FoodTruckCafe"; RC = P + "/Docs/tools/recipes"; SHOTS = P + "/Assets/Screenshots"
def res(t):
    m = re.search(r'"result":"(.*?)","compiler"', t); return m.group(1) if m else t[-600:]
def ex(code): return res(U.call("execute_code", {"action": "execute", "code": code}))
def exf(path): return ex(open(path).read())
def log(*a): print(time.strftime("%H:%M:%S"), *a, flush=True)
def shot(name): return U.call("manage_camera", {"action": "screenshot", "screenshot_file_name": name, "properties": {"include_image": False}})
def clear_sess():
    if os.path.exists(P + "/Docs/tools/.mcp_session"): os.remove(P + "/Docs/tools/.mcp_session")
def errors(): return U.call("read_console", {"action": "get", "types": ["error"], "count": 6, "include_stacktrace": False})[:1200]
stages = os.environ.get("STAGE", "refresh,tutorial,day2,ipad").split(",")
for f in glob.glob(SHOTS + "/nb_*"): os.remove(f)
clear_sess()
if "refresh" in stages:
    log(exf(RC + "/ap/stop.cs")); time.sleep(3)
    log(U.call("refresh_unity", {"mode": "force"})[:150]); time.sleep(20)
    for i in range(40):
        clear_sess()
        try:
            r = ex('return typeof(TapedNote).Name;')
            if r == "TapedNote": log("ready"); break
            log("wait", r[:100])
        except Exception as e: log("wait", e)
        time.sleep(5)
    log(exf(RC + "/r35/note_import.cs"))
    log(U.call("read_console", {"action": "get", "types": ["error"], "count": 5, "include_stacktrace": False, "filter_text": "error CS"})[:300])
def start(gv, session, wait):
    log(exf(RC + "/ap/stop.cs")); time.sleep(3)
    log(exf(RC + f"/{gv}.cs")); log(exf(RC + "/ap/play.cs")); time.sleep(12); clear_sess()
    log(exf(RC + session)); time.sleep(wait)
CAP = 'var d = UnityEngine.Object.FindObjectOfType<TutorialDirector>(); if (d == null) return "no director"; var c = typeof(TutorialDirector).GetField("caption", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(d) as TMPro.TextMeshProUGUI; return c != null ? (c.transform.parent.gameObject.activeSelf + " " + c.text) : "-";'
if "tutorial" in stages:
    start("gv_Store1320", "/r35/tutorial_session.cs", 8)
    last = None
    for i in range(30):
        cap = ex(CAP); log("step", i, cap); shot(f"nb_tut_{i:02d}")
        if cap.startswith("no director"): break
        r = exf(RC + "/r26/tut_tap.cs"); log(" ", r[:100]); time.sleep(3.5)
    log("errors", errors())
STEP = 'UnityEditor.EditorApplication.isPaused = true; Time.captureDeltaTime = 1f / 30f; UnityEditor.EditorApplication.Step(); return "s";'
if "day2" in stages:
    start("gv_Store1320", "/r35/day2_session.cs", 14)
    shot("nb_menu"); time.sleep(1.5)
    log(ex('UnityEditor.EditorApplication.isPaused = true; Time.captureDeltaTime = 1f / 30f; UnityEngine.Object.FindObjectOfType<MenuBook>().Close(); return "closing";'))
    for n in range(48): ex(STEP); shot(f"nb_gif_{n:03d}")
    ex('UnityEditor.EditorApplication.isPaused = false; Time.captureDeltaTime = 0f; return "ok";')
    time.sleep(7); shot("nb_arrange"); time.sleep(1.5)
    log(exf(RC + "/ap/openkitchen.cs")); time.sleep(4); shot("nb_kitchen"); time.sleep(1.5)
    log("errors", errors())
if "ipad" in stages:
    start("gv_iPad", "/r35/day2_session.cs", 14)
    shot("nb_ipad"); time.sleep(1.5)
    log("errors", errors())
clear_sess(); log(exf(RC + "/ap/stop.cs")); log(exf(RC + "/gv_Store1320.cs")); log("DONE")
