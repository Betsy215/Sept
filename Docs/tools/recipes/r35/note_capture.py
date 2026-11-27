"""Taped note (owner's pick, 27 Nov 2026): show-up animations a-d recorded frame by frame (pose set per frame, editor stepped), plus stills."""
import sys, os, re, time, glob
sys.path.insert(0, "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools")
import unity_mcp as U
P = "/Users/betzthebest/Desktop/Sept/FoodTruckCafe"; RC = P + "/Docs/tools/recipes"; SHOTS = P + "/Assets/Screenshots"
HERE = os.path.dirname(os.path.abspath(__file__))
ART = os.environ.get("ART", "/Users/betzthebest/Desktop/Sept/Store/ui/tutorial_banner_2026-11-26/art")
def res(t):
    m = re.search(r'"result":"(.*?)","compiler"', t); return m.group(1) if m else t[-900:]
def ex(code): return res(U.call("execute_code", {"action": "execute", "code": code}))
def exf(path): return ex(open(path).read())
def log(*a): print(time.strftime("%H:%M:%S"), *a, flush=True)
def shot(name): return U.call("manage_camera", {"action": "screenshot", "screenshot_file_name": name, "properties": {"include_image": False}})
def clear_sess():
    if os.path.exists(P + "/Docs/tools/.mcp_session"): os.remove(P + "/Docs/tools/.mcp_session")
TM = open(HERE + "/note.cs.tmpl").read()
MENU = "You can remove or add items to today's menu"; TABLE = "Drag to arrange your table"
def pose(style, ctx, t, text, bold=True, step=False):
    code = TM.replace("__ART__", ART).replace("__STYLE__", style).replace("__CTX__", ctx).replace("__T__", f"{t:.4f}").replace("__TEXT__", text).replace("__BOLD__", "true" if bold else "false")
    if step: code = code.replace('return "ok " + style', 'UnityEditor.EditorApplication.isPaused = true; Time.captureDeltaTime = 1f / 30f; UnityEditor.EditorApplication.Step(); return "ok " + style')
    for k in range(3):
        try:
            r = ex(code)
            if r.startswith("ok"): return r
            log("retry", r[:300])
        except Exception as e: log("retry", e)
        clear_sess(); time.sleep(1)
    return "FAILED"
for f in glob.glob(SHOTS + "/na_*") + glob.glob(SHOTS + "/ns_*"): os.remove(f)
clear_sess()
log(exf(RC + "/ap/stop.cs")); time.sleep(3)
log(exf(RC + "/gv_Store1320.cs")); log(exf(RC + "/ap/play.cs")); time.sleep(12); clear_sess()
log(exf(RC + "/r35/day2_session.cs")); time.sleep(14)
for bold in (False, True):
    log(pose("s", "menu", 2.0, MENU, bold)); time.sleep(1.2); shot(f"ns_menu_{'b' if bold else 'r'}"); time.sleep(1.2)
for st in os.environ.get("STYLES", "a,b,c,d").split(","):
    n = 0; t = -0.1
    while t <= 1.0 + 1e-6:
        r = pose(st, "menu", t, MENU, True, step=True); shot(f"na_{st}_{n:03d}"); n += 1; t += 1 / 30
    log(st, n, "frames", r)
log(pose("s", "menu", 2.0, MENU, True, step=True)); time.sleep(1); shot("ns_final_menu"); time.sleep(1.2)
ex('UnityEditor.EditorApplication.isPaused = false; Time.captureDeltaTime = 0f; foreach (var o in UnityEngine.Object.FindObjectsOfType<Canvas>()) if (o.name.StartsWith("NotePreview_")) UnityEngine.Object.DestroyImmediate(o.gameObject); UnityEngine.Object.FindObjectOfType<MenuBook>().Close(); return "closed";')
time.sleep(8)
log(pose("s", "table", 2.0, TABLE, True)); time.sleep(1.2); shot("ns_final_table"); time.sleep(1.2)
log(pose("s", "table", 2.0, TABLE, False)); time.sleep(1.2); shot("ns_table_r"); time.sleep(1.2)
clear_sess(); log(exf(RC + "/ap/stop.cs")); log("DONE")
