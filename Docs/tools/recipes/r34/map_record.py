"""Records map moments frame by frame (24 Nov 2026): day 9 reached, day 9 done, chapter 2's first look.
   python3 Docs/tools/recipes/r34/map_record.py"""
import sys, os, re, time, glob, json
sys.path.insert(0, "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools")
import unity_mcp as U
P = "/Users/betzthebest/Desktop/Sept/FoodTruckCafe"; RC = P + "/Docs/tools/recipes"; R = RC + "/r34"; SHOTS = P + "/Assets/Screenshots"
OUT = "/Users/betzthebest/Desktop/Sept/Store/day9_2026-11-24"; os.makedirs(OUT, exist_ok=True)
def res(t):
    m = re.search(r'"result":"(.*?)","compiler"', t); return m.group(1) if m else t[-300:]
def ex(code): return res(U.call("execute_code", {"action": "execute", "code": code}))
def log(*a): print(time.strftime("%H:%M:%S"), *a, flush=True)
log(ex(open(RC + "/ap/stop.cs").read())); time.sleep(3)
U.call("refresh_unity", {}); time.sleep(30)
if os.path.exists(P + "/Docs/tools/.mcp_session"): os.remove(P + "/Docs/tools/.mcp_session")
log(ex(open(RC + "/gv_Store1320.cs").read())); log(ex(open(RC + "/ap/play.cs").read())); time.sleep(12)
STEP = 'UnityEditor.EditorApplication.isPaused = true; Time.timeScale = 2f; UnityEditor.EditorApplication.Step(); return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "|" + Time.time.ToString("F3") + "|" + StarCelebration.SoundCount + "|" + StarCelebration.LastSound + "|" + StarCelebration.LastSoundAt.ToString("F3");'
clips = {}
for name, recipe, length in (("reach9", "day9_reach.cs", 10.5), ("done9", "day9_done.cs", 6.5), ("ch2first", "ch2_first.cs", 7.5)):
    ex('UnityEditor.EditorApplication.isPaused = false; Time.timeScale = 1f; return "go";')
    for f in glob.glob(SHOTS + f"/m9_{name}_*"): os.remove(f)
    log(name, ex(open(R + "/" + recipe).read()))
    t0 = None; seen = None; snd = []; i = 0
    while True:
        r = ex(STEP).split("|"); sc = r[0]; t = float(r[1]); cnt = int(r[2])
        if sc != "Map": continue
        if t0 is None: t0 = t; seen = cnt
        if cnt != seen: snd.append((r[3], float(r[4]) - t0)); seen = cnt
        U.call("manage_camera", {"action": "screenshot", "screenshot_file_name": f"m9_{name}_{i:03d}", "properties": {"include_image": False}})
        i += 1
        if t - t0 > length: break
    clips[name] = {"frames": i, "sounds": snd}; json.dump(clips, open(OUT + "/clips.json", "w"), indent=1)
    log(name, i, "frames", snd); time.sleep(1.5)
ex('UnityEditor.EditorApplication.isPaused = false; Time.timeScale = 1f; MapScreen.GiftAutoSeconds = 0f; return "ok";')
log(ex(open(RC + "/ap/stop.cs").read())); log("DONE")
