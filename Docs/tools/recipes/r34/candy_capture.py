"""Day 3's candy gift, three styles, recorded frame by frame (24 Nov 2026), like stars_capture.py.   python3 Docs/tools/recipes/r34/candy_capture.py"""
import sys, os, re, time, glob, json
sys.path.insert(0, "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools")
import unity_mcp as U
P = "/Users/betzthebest/Desktop/Sept/FoodTruckCafe"; RC = P + "/Docs/tools/recipes"; R = RC + "/r34"; SHOTS = P + "/Assets/Screenshots"
OUT = "/Users/betzthebest/Desktop/Sept/Store/candy_gift_2026-11-24"; os.makedirs(OUT, exist_ok=True)
def res(t):
    m = re.search(r'"result":"(.*?)","compiler"', t); return m.group(1) if m else t[-300:]
def ex(code): return res(U.call("execute_code", {"action": "execute", "code": code}))
def exf(path, **kw):
    c = open(path).read()
    for k, v in kw.items(): c = c.replace(k, str(v))
    return ex(c)
def log(*a): print(time.strftime("%H:%M:%S"), *a, flush=True)
log(ex(open(RC + "/ap/stop.cs").read())); time.sleep(3)
U.call("refresh_unity", {}); time.sleep(30)
if os.path.exists(P + "/Docs/tools/.mcp_session"): os.remove(P + "/Docs/tools/.mcp_session")
log(ex(open(RC + "/gv_Store1320.cs").read())); log(ex(open(RC + "/ap/play.cs").read())); time.sleep(12)
STEP = 'UnityEditor.EditorApplication.isPaused = true; Time.timeScale = 2f; UnityEditor.EditorApplication.Step(); return Time.time.ToString("F3") + "|" + StarCelebration.SoundCount + "|" + StarCelebration.LastSound + "|" + StarCelebration.LastSoundAt.ToString("F3");'
clips = {}
for style in [int(x) for x in os.environ.get("STYLES", "1,2,3").split(",")]:
    ex('UnityEditor.EditorApplication.isPaused = false; Time.timeScale = 1f; return "go";')
    for f in glob.glob(SHOTS + f"/cg{style}_*"): os.remove(f)
    log(exf(R + "/candy_day3.cs", __STYLE__=style)); time.sleep(10)
    log(ex(open(RC + "/r26/menu_close.cs").read())); time.sleep(1.5)
    ex('UnityEditor.EditorApplication.isPaused = true; return "p";')
    log(ex(open(RC + "/r29/done.cs").read()))
    t0 = None; seen = None; snd = []; i = 0
    while True:
        r = ex(STEP).split("|"); t = float(r[0]); cnt = int(r[1])
        if t0 is None: t0 = t; seen = cnt
        if cnt != seen: snd.append((r[2], float(r[3]) - t0)); seen = cnt
        U.call("manage_camera", {"action": "screenshot", "screenshot_file_name": f"cg{style}_{i:03d}", "properties": {"include_image": False}})
        i += 1
        if t - t0 > 6.4: break
    clips[f"s{style}"] = {"frames": i, "sounds": snd}; json.dump(clips, open(OUT + "/clips.json", "w"), indent=1)
    log("style", style, i, "frames", snd); time.sleep(1.5)
ex('UnityEditor.EditorApplication.isPaused = false; Time.timeScale = 1f; CandyGift.PreviewMode = false; return "ok";')
log(ex(open(RC + "/ap/stop.cs").read())); log("DONE")
