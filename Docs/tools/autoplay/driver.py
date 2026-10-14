"""Full-chapter autoplay through the editor bridge: fresh session, tutorial, days 1-16, shop after each day.
Logs every action and the console's errors/warnings to /tmp/ap/run.log and a per-day table to /tmp/ap/days.tsv."""
import sys, json, time, re
sys.path.insert(0, "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools")
import unity_mcp as U
LOG = open("/tmp/ap/run.log", "a"); DAYS = open("/tmp/ap/days.tsv", "a")
def log(*a):
    s = " ".join(str(x) for x in a); print(s, flush=True); LOG.write(time.strftime("%H:%M:%S ") + s + "\n"); LOG.flush()
def res(text):
    m = re.search(r'"result":"(.*?)","compiler"', text)
    return m.group(1) if m else text[-200:]
def ex(code): return res(U.call("execute_code", {"action": "execute", "code": code}))
def exf(path, **kw):
    c = open(path).read()
    for k, v in kw.items(): c = c.replace(k, str(v))
    return ex(c)
def console(tag):
    out = U.call("read_console", {"action": "get", "types": ["error", "warning"], "count": 200, "include_stacktrace": False, "format": "plain"})
    try: data = json.loads(out).get("data") or []
    except Exception: data = [out]
    seen = {}
    for e in data:
        first = (e if isinstance(e, str) else json.dumps(e)).split("\n")[0][:220]
        seen[first] = seen.get(first, 0) + 1
    log(f"CONSOLE[{tag}] {len(data)} entries, {len(seen)} distinct")
    for k, v in seen.items(): log(f"    x{v} {k}")
    U.call("read_console", {"action": "clear"})
SPEED = float(sys.argv[1]) if len(sys.argv) > 1 else 1.5
STEP = float(sys.argv[2]) if len(sys.argv) > 2 else 1.0
U.call("manage_editor", {"action": "stop"}); time.sleep(2)
ex(open("/tmp/gv_Store1320.cs").read())
U.call("read_console", {"action": "clear"})
U.call("manage_editor", {"action": "play"}); time.sleep(6)
log("FRESH", exf("/tmp/ap/fresh.cs"))
console("menu")
# the tutorial (due on a fresh session) through the real flow: Play goes to GameSceneOne
log("===== TUTORIAL")
log(ex('var cp = UnityEngine.Object.FindObjectOfType<ClickPlay>(); if (cp == null) return "no ClickPlay"; var e = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current); cp.OnPointerDown(e); cp.OnPointerUp(e); return "play clicked on " + cp.name;'))
time.sleep(4)
if "GameSceneOne" not in ex(open("/tmp/ap/state.cs").read()):
    log(exf("/tmp/ap/goday.cs", DAY=0))
time.sleep(7)
t0 = time.time(); seen = {}
while time.time() - t0 < 180:
    r = ex(open("/tmp/ap/tutstep.cs").read()); seen[r] = seen.get(r, 0) + 1
    if seen[r] == 1: log("  tut:", r)
    if r in ("T|letsgo",) : break
    if r == "T|none" and time.time() - t0 > 20: break
    time.sleep(1.0)
time.sleep(5); log("after tutorial", ex(open("/tmp/ap/state.cs").read()))
U.call("manage_camera", {"action": "screenshot", "screenshot_file_name": "ap_tutorial_end", "properties": {"include_image": False}})
console("tutorial")
for day in range(0, 16):
    log(f"===== DAY index {day}")
    log(exf("/tmp/ap/goday.cs", DAY=day)); time.sleep(7)
    log(ex(open("/tmp/ap/state.cs").read()))
    log(ex(open("/tmp/startday.cs").read())); time.sleep(1)
    ex(f"UnityEngine.Time.timeScale = {SPEED}f; return \"speed\";")
    counts = {}; t0 = time.time(); last_state = 0
    while time.time() - t0 < 240:
        r = ex(open("/tmp/ap/step.cs").read())
        kind = r.split("|")[1] if "|" in r else r
        counts[kind] = counts.get(kind, 0) + 1
        if kind == "done": break
        if kind in ("noLM", "noOS", "missing") or not r.startswith("S|"): log("  step:", r)
        if time.time() - last_state > 15:
            log("  ", ex(open("/tmp/ap/state.cs").read()), counts); last_state = time.time()
        # the tutorial ends on its own board (Let's go): detect leaving the game scene
        if "S|noLM" in r: break
        time.sleep(STEP)
    ex("UnityEngine.Time.timeScale = 1f; return \"speed 1\";")
    time.sleep(1)
    st = ex(open("/tmp/ap/state.cs").read()); log("END", st, counts, f"{time.time() - t0:.0f}s")
    DAYS.write(f"{day}\t{st}\t{json.dumps(counts)}\n"); DAYS.flush()
    U.call("manage_camera", {"action": "screenshot", "screenshot_file_name": f"ap_day{day:02d}_end", "properties": {"include_image": False}})
    console(f"day {day}")
    if "panel True" in st:
        log(exf("/tmp/ap/next.cs")); time.sleep(5)
        log(ex(open("/tmp/ap/state.cs").read()))
    ex('SceneTransitionManager.Instance.TransitionToScene("Shop"); return "to shop";'); time.sleep(6)
    for _ in range(6):
        b = ex(open("/tmp/ap/shopbuy.cs").read()); log("  ", b)
        if not b.startswith("B|bought"): break
        time.sleep(1)
    console(f"shop after day {day}")
U.call("manage_editor", {"action": "stop"})
log("RUN COMPLETE")
