"""The pull handle's bounce as two small films for the report (owner, 15 November 2026: "show me the bounce affect,
show me kitchen too"). Plays day 1, then steps the handle through its own bounce with PullHandle.ShowAt (the method
the game's bounce runs on), one screenshot every 1/30 s, on the table and in the kitchen.
Writes /tmp/ftc/popframes/phg_table_NN.png, phg_kitchen_NN.png; pull_handle_gif_make.py turns them into GIFs."""
import sys, time, re
sys.path.insert(0, "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools")
import unity_mcp as U
A = "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools/autoplay/"
def res(t):
    m = re.search(r'"result":"(.*?)","compiler"', t, re.S); return m.group(1) if m else t[-700:]
def ex(code): return res(U.call("execute_code", {"action": "execute", "code": code}))
U.call("manage_editor", {"action": "stop"}); time.sleep(2)
U.call("manage_editor", {"action": "play"}); time.sleep(9)
print(ex(open("/tmp/ftc/setup_day1.cs").read()))
print(ex(open(A + "goday.cs").read().replace("DAY", "0"))); time.sleep(8)
for _ in range(12):
    r = ex(open(A + "tutstep.cs").read())
    if r == "T|none": break
    time.sleep(1.0)
print(ex(open(A + "startday.cs").read())); time.sleep(9)
print(ex('var b = GameObject.Find("KitchenButton"); var rt = b.GetComponent<RectTransform>(); var hen = GameObject.Find("MoneyFlipClock"); var st = GameObject.Find("HandleStick"); return "table handle pos " + rt.anchoredPosition + " size " + rt.sizeDelta + " stick " + ((RectTransform)st.transform).sizeDelta + " hen x " + hen.transform.position.x.ToString("0.00") + " sprite " + b.GetComponent<UnityEngine.UI.Image>().sprite.name + " seconds " + PullHandle.BounceSeconds;'))
STEPS = 14
def film(tag, find):
    ex('Time.timeScale = 0.02f; return "slow";')
    for i in range(STEPS + 1):
        t = i / 30.0
        ex(find + ' h.ShowAt(%sf); ScreenCapture.CaptureScreenshot("/tmp/ftc/popframes/phg_%s_%02d.png"); return "ok";' % (repr(round(t, 4)), tag, i)); time.sleep(0.9)
    ex(find + ' h.ShowAt(0f); Time.timeScale = 1f; return "rest";')
film("table", 'var h = GameObject.Find("KitchenButton").GetComponent<PullHandle>();')
print(ex('GameObject.Find("KitchenButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); return "tapped";')); time.sleep(2.0)
KB = 'var cb = UnityEngine.Object.FindObjectOfType<CloseKitchenButton>(); var h = cb.GetComponent<PullHandle>();'
print(ex(KB + ' var rt = (RectTransform)cb.transform; return "kitchen open " + KitchenSceneManager.Instance.IsKitchenOpen + " handle pos " + rt.anchoredPosition + " size " + rt.sizeDelta;'))
film("kitchen", KB)
print(ex(KB + ' cb.GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); return "tapped back";')); time.sleep(1.6)
print(ex('return "kitchen open " + KitchenSceneManager.Instance.IsKitchenOpen;'))
U.call("manage_editor", {"action": "stop"})
