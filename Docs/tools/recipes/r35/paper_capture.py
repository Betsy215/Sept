"""Taped-note paper variants 0-4 in the game (27 Nov 2026), runtime only: the day-1 tutorial's first step, the day-2 menu
lesson and the arrange hint. Shots: Assets/Screenshots/pv<v>_<tut|menu|arrange>.png"""
import sys, os, re, time, glob
sys.path.insert(0, "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools")
import unity_mcp as U
P = "/Users/betzthebest/Desktop/Sept/FoodTruckCafe"; RC = P + "/Docs/tools/recipes"; SHOTS = P + "/Assets/Screenshots"
ART = "/Users/betzthebest/Desktop/Sept/Store/ui/tutorial_banner_2026-11-26/art"
def res(t):
    m = re.search(r'"result":"(.*?)","compiler"', t); return m.group(1) if m else t[-600:]
def ex(code): return res(U.call("execute_code", {"action": "execute", "code": code}))
def exf(path): return ex(open(path).read())
def log(*a): print(time.strftime("%H:%M:%S"), *a, flush=True)
def shot(name): return U.call("manage_camera", {"action": "screenshot", "screenshot_file_name": name, "properties": {"include_image": False}})
def clear_sess():
    if os.path.exists(P + "/Docs/tools/.mcp_session"): os.remove(P + "/Docs/tools/.mcp_session")
TM = open(RC + "/r35/paper_swap.cs.tmpl").read()
def each(ctx):
    for v in range(5):
        log(ex(TM.replace("__ART__", ART).replace("__V__", str(v)))); time.sleep(1.2); shot(f"pv{v}_{ctx}"); time.sleep(1.2)
def start(session, wait):
    log(exf(RC + "/ap/stop.cs")); time.sleep(3)
    log(exf(RC + "/gv_Store1320.cs")); log(exf(RC + "/ap/play.cs")); time.sleep(12); clear_sess()
    log(exf(RC + session)); time.sleep(wait)
for f in glob.glob(SHOTS + "/pv*_*"): os.remove(f)
clear_sess()
start("/r35/tutorial_session.cs", 9); each("tut")
start("/r35/day2_session.cs", 14); each("menu")
log(ex('UnityEngine.Object.FindObjectOfType<MenuBook>().Close(); return "closed";')); time.sleep(8); each("arrange")
clear_sess(); log(exf(RC + "/ap/stop.cs")); log("DONE")
