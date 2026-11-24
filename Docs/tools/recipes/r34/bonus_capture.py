"""Bonus days appearing on the map (24 Nov 2026), recorded frame by frame like stars_capture.py: a still of the map one
day before (bonus days hidden), then chapter 1 at day 9 and chapter 2 at day 13 with the reveal stepped at 0.04 s.
   python3 Docs/tools/recipes/r34/bonus_capture.py"""
import sys, os, re, time, glob, subprocess, shutil
sys.path.insert(0, "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools")
import unity_mcp as U
P = "/Users/betzthebest/Desktop/Sept/FoodTruckCafe"; RC = P + "/Docs/tools/recipes"; R = RC + "/r34"; SHOTS = P + "/Assets/Screenshots"
def res(t):
    m = re.search(r'"result":"(.*?)","compiler"', t); return m.group(1) if m else t[-300:]
def ex(code): return res(U.call("execute_code", {"action": "execute", "code": code}))
def exf(path, **kw):
    c = open(path).read()
    for k, v in kw.items(): c = c.replace(k, str(v))
    return ex(c)
def shot(n): U.call("manage_camera", {"action": "screenshot", "screenshot_file_name": n, "properties": {"include_image": False}})
def log(*a): print(time.strftime("%H:%M:%S"), *a, flush=True)
log(ex(open(RC + "/ap/stop.cs").read())); time.sleep(3)
U.call("refresh_unity", {}); time.sleep(30)
if os.path.exists(P + "/Docs/tools/.mcp_session"): os.remove(P + "/Docs/tools/.mcp_session")
log(ex(open(RC + "/gv_Store1320.cs").read())); log(ex(open(RC + "/ap/play.cs").read())); time.sleep(12)
for f in glob.glob(SHOTS + "/bx_*"): os.remove(f)
log(ex(open(R + "/bonus_before.cs").read())); time.sleep(4)
log(ex(open(RC + "/r33/map_cancel.cs").read())); time.sleep(1.5); shot("bx_before"); time.sleep(2)
STEP = 'UnityEditor.EditorApplication.isPaused = true; Time.timeScale = 2f; UnityEditor.EditorApplication.Step(); return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "|" + Time.time.ToString("F3") + "|" + StarCelebration.SoundCount + "|" + StarCelebration.LastSound + "|" + StarCelebration.LastSoundAt.ToString("F3");'
import json
SOUNDS = {}
for ch in (1, 2):
    ex('UnityEditor.EditorApplication.isPaused = false; Time.timeScale = 1f; return "go";')
    log(exf(R + "/bonus_map.cs", __CH__=ch))
    t0 = None; i = 0; seen = None; snd = []
    while True:
        r = ex(STEP).split("|"); sc = r[0]; t = float(r[1]); cnt = int(r[2])
        if sc != "Map": continue
        if t0 is None: t0 = t; seen = cnt
        if cnt != seen: snd.append((r[3], float(r[4]) - t0)); seen = cnt
        shot(f"bx_c{ch}_{i:03d}"); i += 1
        if t - t0 > 7.6: break
    SOUNDS[f"c{ch}"] = {"frames": i, "sounds": snd}; json.dump(SOUNDS, open("/Users/betzthebest/Desktop/Sept/Store/bonus_days_2026-11-24/clips.json", "w"), indent=1)
    log("chapter", ch, i, "frames", snd); time.sleep(1.5)
ex('UnityEditor.EditorApplication.isPaused = false; Time.timeScale = 1f; return "ok";')
log(ex(open(RC + "/ap/stop.cs").read())); log("DONE")
