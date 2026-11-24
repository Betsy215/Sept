"""Day-end star celebrations, recorded frame by frame (24 Nov 2026): for each style (1 Chimes, 2 Cookie, 3 Fireworks) and
star count 1-3, replay the card's stars with the editor paused and stepped (0.04 s of game time per frame = 25 fps), one
screenshot per frame, the sounds logged by game time; then an mp4 with its soundtrack per clip.
   python3 Docs/tools/recipes/r34/stars_capture.py      (iPhone 6.9-inch game view; ~12 minutes)"""
import sys, os, re, json, time, glob, shutil, subprocess, wave
sys.path.insert(0, "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools")
import unity_mcp as U
P = "/Users/betzthebest/Desktop/Sept/FoodTruckCafe"; RC = P + "/Docs/tools/recipes"; OUT = "/Users/betzthebest/Desktop/Sept/Store/star_fx_2026-11-24"
SHOTS = P + "/Assets/Screenshots"; os.makedirs(OUT + "/frames", exist_ok=True)
def res(t):
    m = re.search(r'"result":"(.*?)","compiler"', t); return m.group(1) if m else t[-300:]
def ex(code): return res(U.call("execute_code", {"action": "execute", "code": code}))
def exf(path, **kw):
    c = open(path).read()
    for k, v in kw.items(): c = c.replace(k, str(v))
    return ex(c)
def log(*a): print(time.strftime("%H:%M:%S"), *a, flush=True)
# to the day-end card
log(ex(open(RC + "/ap/stop.cs").read())); time.sleep(3)
U.call("refresh_unity", {}); time.sleep(30)
if os.path.exists(P + "/Docs/tools/.mcp_session"): os.remove(P + "/Docs/tools/.mcp_session")
log(ex(open(RC + "/gv_Store1320.cs").read())); log(ex(open(RC + "/ap/play.cs").read())); time.sleep(12)
log(ex(open(RC + "/r29/c1_session.cs").read())); time.sleep(13)
log(ex(open(RC + "/r26/menu_close.cs").read())); time.sleep(1)
log(ex(open(RC + "/r29/done.cs").read())); time.sleep(5)
log(ex(open(RC + "/ap/clock_end.cs").read()))
for i in range(30):
    r = ex(open(RC + "/r26/step_dish.cs").read())
    if "done" in r: break
    time.sleep(0.8)
time.sleep(4); log("card:", ex('var r = GameObject.Find("StarsRow"); return r == null ? "none" : "ok";'))
STEP = 'UnityEditor.EditorApplication.isPaused = true; Time.timeScale = 2f; UnityEditor.EditorApplication.Step(); return Time.time.ToString("F3") + "|" + StarCelebration.SoundCount + "|" + StarCelebration.LastSound + "|" + StarCelebration.LastSoundAt.ToString("F3");'
LEN = {1: 2.6, 2: 3.6, 3: 5.0}
clips = {}
for style in (1, 2, 3):
    for n in (1, 2, 3):
        name = f"s{style}_{n}"
        for f in glob.glob(SHOTS + f"/fx_{name}_*"): os.remove(f)
        ex('UnityEditor.EditorApplication.isPaused = true; Time.timeScale = 2f; return "p";')
        log(name, exf(P + "/Docs/tools/recipes/r34/stars_replay.cs", __STYLE__=style, __N__=n))
        t0 = None; seen = None; sounds = []; i = 0
        while True:
            r = ex(STEP).split("|")
            t = float(r[0]); cnt = int(r[1])
            if t0 is None: t0 = t; seen = cnt
            if cnt != seen: sounds.append((r[2], float(r[3]) - t0)); seen = cnt
            U.call("manage_camera", {"action": "screenshot", "screenshot_file_name": f"fx_{name}_{i:03d}", "properties": {"include_image": False}})
            i += 1
            if t - t0 > LEN[n]: break
        time.sleep(1.5)
        clips[name] = {"frames": i, "sounds": sounds}; log(name, i, "frames", sounds)
        json.dump(clips, open(OUT + "/clips.json", "w"), indent=1)
ex('UnityEditor.EditorApplication.isPaused = false; Time.timeScale = 1f; Time.captureDeltaTime = 0f; return "ok";')
log(ex(open(RC + "/ap/stop.cs").read()))
log("DONE")
