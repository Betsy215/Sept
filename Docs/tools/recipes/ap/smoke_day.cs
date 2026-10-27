// Release smoke test (26 October 2026): plays the current day by itself through the real tap handlers, the glass-dish ice
// cream flow included, refills like a kitchen trip would, and counts every error and exception the game logs. Writes a
// one-line summary to Captures/smoke_<day>.txt when the level-complete panel shows (or after 240 s). Run on a started day.
var lm = UnityEngine.Object.FindObjectOfType<LevelManager>(); if (lm == null) return "no LevelManager";
var os = UnityEngine.Object.FindObjectOfType<OrderSystem>(); var st = UnityEngine.Object.FindObjectOfType<IceCreamStation>();
var FL = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public;
var errors = new System.Collections.Generic.List<string>(); var served = 0; var actions = 0;
Application.LogCallback cb = (msg, trace, type) => { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) { if (errors.Count < 30) errors.Add(type + ": " + msg.Split('\n')[0]); } };
Application.logMessageReceived += cb;
var ev = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
System.Collections.IList dishes = st != null ? (System.Collections.IList)typeof(IceCreamStation).GetField("dishes", FL).GetValue(st) : null;
System.Func<object, bool> hasBowl = d => (bool)d.GetType().GetField("waffle").GetValue(d);
System.Func<object, int> scoopsIn = d => ((System.Collections.ICollection)d.GetType().GetField("flavours").GetValue(d)).Count;
var day = SessionManager.Instance.GetCurrentSession().levelsCompleted + 1;
System.Collections.IEnumerator IceCream(string want)
{
    var bowl = want.IndexOf(IceCreamStation.WaffleLetter) >= 0; var letters = want.Replace(IceCreamStation.WaffleLetter.ToString(), "");
    foreach (var ch in letters) if (st.Left(IceCreamStation.Letters.IndexOf(ch)) <= 0) { st.RefillAll(); break; }   // the blender
    var pick = -1;
    for (var i = 0; i < dishes.Count; i++) if (scoopsIn(dishes[i]) == 0 && hasBowl(dishes[i]) == bowl) { pick = i; break; }
    if (pick < 0 && bowl)
    {
        for (var i = 0; i < dishes.Count && pick < 0; i++) if (scoopsIn(dishes[i]) == 0 && !hasBowl(dishes[i])) pick = -2;
        if (pick == -2) { if (!st.IsBakingWaffles) st.OnTap(IceCreamTap.Kind.Waffle, 0); var w = 0f; while (st.IsBakingWaffles && w < 15f) { w += Time.deltaTime; yield return null; } pick = -1; for (var i = 0; i < dishes.Count; i++) if (scoopsIn(dishes[i]) == 0 && hasBowl(dishes[i])) { pick = i; break; } }
    }
    if (pick < 0)
    {
        // no fitting glass: empty one with the trash can (lift, trash)
        st.OnTap(IceCreamTap.Kind.Dish, 0); yield return null; st.OnTap(IceCreamTap.Kind.Trash, 0); yield return null;
        if (bowl) { if (!st.IsBakingWaffles) st.OnTap(IceCreamTap.Kind.Waffle, 0); var w = 0f; while (st.IsBakingWaffles && w < 15f) { w += Time.deltaTime; yield return null; } }
        pick = -1; for (var i = 0; i < dishes.Count; i++) if (scoopsIn(dishes[i]) == 0 && hasBowl(dishes[i]) == bowl) { pick = i; break; }
        if (pick < 0) yield break;
    }
    st.OnTap(IceCreamTap.Kind.Dish, pick); yield return new WaitForSeconds(0.15f);
    foreach (var ch in letters) { st.OnTap(IceCreamTap.Kind.Section, IceCreamStation.Letters.IndexOf(ch)); yield return new WaitForSeconds(0.12f); }
    st.OnTap(IceCreamTap.Kind.Dish, pick); served++;
}
System.Collections.IEnumerator Play()
{
    var t = 0f;
    while (t < 240f && !(lm.levelCompletePanel != null && lm.levelCompletePanel.activeSelf))
    {
        t += 0.35f; yield return new WaitForSeconds(0.35f); actions++;
        var tv = os.GetCurrentOrderTypesWithVariants(); if (tv.Count == 0) continue;
        var first = tv[0];
        if (first.StartsWith("IceCream:") && st != null) { yield return IceCream(first.Substring(9)); continue; }
        foreach (var it in UnityEngine.Object.FindObjectsOfType<ServeableItem>())
        {
            if (it.GetFoodType() != first || !it.gameObject.activeInHierarchy) continue;
            var ri = it.GetComponent<RefillableItem>();
            if (ri != null) { var f = typeof(RefillableItem).GetField("currentCount", FL); var n = (int)f.GetValue(ri); if (n <= 0) { if (first == "Coffee") { var cm = UnityEngine.Object.FindObjectOfType<CoffeeMachine>(); if (cm != null) cm.OnPointerClick(ev); } else ri.IncreaseCount(99); break; } }
            it.OnPointerDown(ev); it.OnPointerUp(ev); it.OnPointerClick(ev); served++; break;
        }
    }
    Application.logMessageReceived -= cb;
    var stars = lm.levelCompletePanel != null && lm.levelCompletePanel.activeSelf ? "day ended" : "timed out";
    var line = "day " + day + " chapter " + SessionManager.CurrentChapter + ": " + stars + " after " + t.ToString("F0") + " s, served " + served + ", errors " + errors.Count + (errors.Count > 0 ? " | " + string.Join(" | ", errors) : "");
    System.IO.File.WriteAllText(System.IO.Path.Combine(Application.dataPath, "../Captures/smoke_" + day + ".txt"), line);
}
lm.StartCoroutine(Play()); return "smoke test running on day " + day;
