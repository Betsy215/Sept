// Records a chapter 1 day for the store preview (26 October 2026, owner: "for the video, we should have both chapter 1 and chapter 2"); made from rec_c2.cs: 30 fps fixed game time (Time.captureFramerate), one
// PNG per frame into Captures/rec_c2/<section>/fNNNNN.png. Sections: serve (orders served through the real taps, glasses,
// scoops, bowls), waffle (a bake with its steam), candy (a candy to a customer), kitchen (the blender), night.
// Run after c2_session.cs + done.cs at the Preview886 game view size.
var lm = UnityEngine.Object.FindObjectOfType<LevelManager>(); if (lm == null) return "no LevelManager";
var os = UnityEngine.Object.FindObjectOfType<OrderSystem>(); var st = UnityEngine.Object.FindObjectOfType<IceCreamStation>(); var jar = UnityEngine.Object.FindObjectOfType<CandyJar>();
var FL = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public;
var ROOT = System.IO.Path.Combine(Application.dataPath, "../Captures/rec_c1");   // the run script clears the folder (the bridge blocks deletes)
var dishes = st != null ? (System.Collections.IList)typeof(IceCreamStation).GetField("dishes", FL).GetValue(st) : new System.Collections.ArrayList();
System.Func<object, bool> bw = d => (bool)d.GetType().GetField("waffle").GetValue(d);
System.Func<object, int> sc = d => ((System.Collections.ICollection)d.GetType().GetField("flavours").GetValue(d)).Count;
var ev = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
string section = "serve"; int frame = 0; bool recording = false;
System.Collections.IEnumerator Rec()
{
    while (lm != null)
    {
        yield return new WaitForEndOfFrame();
        if (!recording) continue;
        var dir = System.IO.Path.Combine(ROOT, section); System.IO.Directory.CreateDirectory(dir);
        ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(dir, "f" + (frame++).ToString("D5") + ".png"));
    }
}
System.Collections.IEnumerator Serve(string want)
{
    var bowl = want.IndexOf(IceCreamStation.WaffleLetter) >= 0; var letters = want.Replace(IceCreamStation.WaffleLetter.ToString(), "");
    foreach (var ch in letters) if (st.Left(IceCreamStation.Letters.IndexOf(ch)) <= 0) { st.RefillAll(); break; }
    var pick = -1; for (var i = 0; i < dishes.Count; i++) if (sc(dishes[i]) == 0 && bw(dishes[i]) == bowl) { pick = i; break; }
    if (pick < 0 && bowl) { if (!st.IsBakingWaffles) st.OnTap(IceCreamTap.Kind.Waffle, 0); var w = 0f; while (st.IsBakingWaffles && w < 12f) { w += Time.deltaTime; yield return null; } for (var i = 0; i < dishes.Count; i++) if (sc(dishes[i]) == 0 && bw(dishes[i])) { pick = i; break; } }
    if (pick < 0) { for (var i = 0; i < dishes.Count; i++) if (sc(dishes[i]) > 0 || bw(dishes[i]) != bowl) { st.OnTap(IceCreamTap.Kind.Dish, i); yield return new WaitForSeconds(0.3f); st.OnTap(IceCreamTap.Kind.Trash, i); yield return new WaitForSeconds(0.3f); break; } yield break; }
    st.OnTap(IceCreamTap.Kind.Dish, pick); yield return new WaitForSeconds(0.45f);
    foreach (var ch in letters) { st.OnTap(IceCreamTap.Kind.Section, IceCreamStation.Letters.IndexOf(ch)); yield return new WaitForSeconds(0.4f); }
    yield return new WaitForSeconds(0.3f); st.OnTap(IceCreamTap.Kind.Dish, pick); yield return new WaitForSeconds(0.5f);
}
System.Collections.IEnumerator Play(float seconds)
{
    var t = 0f;
    while (t < seconds)
    {
        var tv = os.GetCurrentOrderTypesWithVariants();
        if (tv.Count > 0)
        {
            var first = tv[0];
            if (st != null && first.StartsWith("IceCream:")) { var t0 = Time.time; yield return Serve(first.Substring(9)); t += Time.time - t0; continue; }
            foreach (var it in UnityEngine.Object.FindObjectsOfType<ServeableItem>())
            {
                if (it.GetFoodType() != first || !it.gameObject.activeInHierarchy) continue;
                var ri = it.GetComponent<RefillableItem>();
                if (ri != null) { var n = (int)typeof(RefillableItem).GetField("currentCount", FL).GetValue(ri); if (n <= 0) { ri.IncreaseCount(99); break; } }
                it.OnPointerDown(ev); it.OnPointerUp(ev); it.OnPointerClick(ev); break;
            }
        }
        yield return new WaitForSeconds(0.7f); t += 0.7f;
    }
}
System.Collections.IEnumerator Run()
{
    Time.captureFramerate = 30;
    lm.StartCoroutine(Rec());
    section = "serve"; frame = 0; recording = true; yield return Play(12f); recording = false;
    section = "candy"; frame = 0; recording = true; yield return new WaitForSeconds(0.6f); if (jar != null) jar.Tap(); yield return new WaitForSeconds(1.6f); recording = false;
    KitchenSceneManager.Instance.OpenKitchen(); yield return new WaitForSeconds(1.0f);
    section = "kitchen"; frame = 0; recording = true; yield return new WaitForSeconds(3.5f); recording = false;
    KitchenSceneManager.Instance.CloseKitchen(); yield return new WaitForSeconds(0.8f);
    var ft = typeof(LevelManager).GetField("dayTimeLeft", FL); ft.SetValue(lm, lm.DayDuration * 0.1f);
    section = "night"; frame = 0; recording = true; yield return Play(5f); recording = false;
    Time.captureFramerate = 0;
    System.IO.File.WriteAllText(System.IO.Path.Combine(ROOT, "done.txt"), "done");
}
lm.StartCoroutine(Run()); return "recording";
