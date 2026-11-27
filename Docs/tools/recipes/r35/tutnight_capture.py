"""The tutorials at night (owner, 27 Nov 2026): day-1 tutorial steps on iPhone (tn_step_NN), its first step on iPad
(tn_ipad), and a real day 2 afterwards (tn_day2, must be day). Uses r35/tutorial_session.cs and r26/tut_tap.cs."""
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
def errs():
    r = U.call("read_console", {"action": "get", "types": ["error"], "count": 10, "include_stacktrace": False})
    return [m[:160] for m in re.findall(r'"([^"]*?)\\n #0', r) if "Adaptive" not in m]
STAGE = 'return "stage " + DayNightTint.CurrentStage.ToString("F2");'
CAP = 'var d = UnityEngine.Object.FindObjectOfType<TutorialDirector>(); if (d == null) return "no director"; var c = typeof(TutorialDirector).GetField("caption", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(d) as TMPro.TextMeshProUGUI; return c != null ? (c.transform.parent.gameObject.activeSelf + " " + c.text) : "-";'
def start(gv, session, wait):
    log(exf(RC + "/ap/stop.cs")); time.sleep(3)
    log(exf(RC + f"/{gv}.cs")); log(exf(RC + "/ap/play.cs")); time.sleep(12); clear_sess()
    log(exf(RC + session)); time.sleep(wait)
for f in glob.glob(SHOTS + "/tn_*"): os.remove(f)
clear_sess()
if "refresh" in os.environ.get("STAGE", "refresh"):
    log(exf(RC + "/ap/stop.cs")); time.sleep(3); log(U.call("refresh_unity", {"mode": "force"})[:120]); time.sleep(20)
    for i in range(40):
        clear_sess()
        try:
            r = ex('return typeof(TapedNote).Name;')
            if r == "TapedNote": log("ready"); break
        except Exception as e: pass
        time.sleep(5)
start("gv_Store1320", "/r35/tutorial_session.cs", 6)
for i in range(12):
    log("step", i, ex(CAP), ex(STAGE)); shot(f"tn_step_{i:02d}"); time.sleep(1)
    if ex(CAP).startswith("no director"): break
    exf(RC + "/r26/tut_tap.cs"); time.sleep(3.5)
log("errors", errs())
start("gv_iPad", "/r35/tutorial_session.cs", 6); log(ex(CAP), ex(STAGE)); shot("tn_ipad"); time.sleep(1.5)
start("gv_Store1320", "/r35/day2_session.cs", 6); log("day 2", ex(STAGE)); shot("tn_day2"); time.sleep(1.5)
log("errors", errs())
clear_sess(); log(exf(RC + "/ap/stop.cs")); log("DONE")
