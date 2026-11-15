import sys, time, re
sys.path.insert(0, "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools")
import unity_mcp as U
A = "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools/autoplay/"
def res(t):
    m = re.search(r'"result":"(.*?)","compiler"', t); return m.group(1) if m else t[-600:]
def ex(code): return res(U.call("execute_code", {"action": "execute", "code": code}))
ST = 'var lm = UnityEngine.Object.FindObjectOfType<LevelManager>(); var pm = UnityEngine.Object.FindObjectOfType<GamePhaseManager>(); var mb = UnityEngine.Object.FindObjectOfType<MenuBook>(); return "dayStarted=" + (lm != null && lm.DayStarted) + " phase=" + (pm != null ? pm.GetCurrentPhase().ToString() : "none") + " book=" + (mb != null ? mb.IsOpen.ToString() : "none") + " kitchen=" + KitchenSceneManager.Instance.IsKitchenOpen;'
U.call("manage_editor", {"action": "stop"}); time.sleep(2)
U.call("manage_editor", {"action": "play"}); time.sleep(9)
print(ex(open("/tmp/ftc/setup_day1.cs").read()))
print(ex('var sm = SessionManager.Instance; sm.OnLevelCompleted(0); return "day 1 marked done, now day " + (sm.GetCurrentLevelIndex() + 1);'))
print(ex(open(A + "goday.cs").read().replace("DAY", "1"))); time.sleep(7)
print(ex(ST)); print("swipe right before the day starts:", ex('return KitchenSwipe.Swipe(1);')); time.sleep(0.6); print(ex(ST))
print(ex(open(A + "startday.cs").read())); time.sleep(1.2)
print(ex(ST)); print("swipe right in play:", ex('return KitchenSwipe.Swipe(1);')); time.sleep(0.8); print(ex(ST))
print("swipe left:", ex('return KitchenSwipe.Swipe(-1);')); time.sleep(0.8); print(ex(ST))
U.call("manage_editor", {"action": "stop"})
