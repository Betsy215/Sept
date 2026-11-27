"""Tutorial banner drafts in the game (26 November 2026): day 2, today's banner (0) and drafts 1-4 over the menu lesson and the
   table's arrange hint, runtime only (banner_preview.cs.tmpl builds an overlay canvas; nothing in the game changes).
   Art: Store/ui/tutorial_banner_2026-11-26 (designs.py). Shots: Assets/Screenshots/bn<d>_<menu|table>.png. DES=0,1,2,3,4"""
import sys, os, re, time, glob
sys.path.insert(0, "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools")
import unity_mcp as U
P = "/Users/betzthebest/Desktop/Sept/FoodTruckCafe"; RC = P + "/Docs/tools/recipes"; SHOTS = P + "/Assets/Screenshots"
HERE = os.path.dirname(os.path.abspath(__file__))
def res(t):
    m = re.search(r'"result":"(.*?)","compiler"', t); return m.group(1) if m else t[-900:]
def ex(code): return res(U.call("execute_code", {"action": "execute", "code": code}))
def exf(path): return ex(open(path).read())
def log(*a): print(time.strftime("%H:%M:%S"), *a, flush=True)
def shot(name): return U.call("manage_camera", {"action": "screenshot", "screenshot_file_name": name, "properties": {"include_image": False}})
def clear_sess():
    if os.path.exists(P + "/Docs/tools/.mcp_session"): os.remove(P + "/Docs/tools/.mcp_session")
TM = open(HERE + "/banner_preview.cs.tmpl").read()
def prev(d, ctx, text):
    for k in range(3):
        clear_sess()
        try:
            r = prev1(d, ctx, text)
            if r.startswith("design") or r.startswith("today"): return r
            log("retry", r[:200])
        except Exception as e: log("retry", e)
        time.sleep(2)
    return "FAILED"
def prev1(d, ctx, text): return ex(TM.replace("__ART__", "/Users/betzthebest/Desktop/Sept/Store/ui/tutorial_banner_2026-11-26/art").replace("__D__", str(d)).replace("__CTX__", ctx).replace("__TEXT__", text))
DES = [int(x) for x in os.environ.get("DES", "0,1,2,3,4").split(",")]
clear_sess()
log(exf(RC + "/ap/stop.cs")); time.sleep(3)
log(exf(RC + "/gv_Store1320.cs")); log(exf(RC + "/ap/play.cs")); time.sleep(12); clear_sess()
log(exf(RC + "/r35/day2_session.cs")); time.sleep(14)
for d in DES:
    log(prev(d, "menu", "You can remove or add items to today's menu")); time.sleep(1.5); shot(f"bn{d}_menu"); time.sleep(1.5)
log(ex('var o = GameObject.Find("BannerPreviewCanvas"); if (o != null) UnityEngine.Object.DestroyImmediate(o); UnityEngine.Object.FindObjectOfType<MenuBook>().Close(); return "closed";'))
time.sleep(8)
for d in DES:
    log(prev(d, "table", "Drag to arrange your table")); time.sleep(1.5); shot(f"bn{d}_table"); time.sleep(1.5)
clear_sess(); log(exf(RC + "/ap/stop.cs")); log("DONE")
