// Scripted play of the glass-dish ice cream flow on the live chapter 2 day (26 October 2026), for the owner's video,
// before the branch is merged: the editor runs main's cone flow, so this starts the day, builds the dish row, moves
// the machine and drives the taps itself from the worktree's Resources/IceCream pictures, serving the real orders
// through OrderSystem.TryServeItem, and redraws the bubbles' cones as dishes. Frames go to Captures/flow/f_XXXX.png
// at 10 a second (ffmpeg makes the video). Run right after ch2_full.cs (the day is 70 s). FOLDER = the worktree's
// Assets/Resources/IceCream. Layout values = IceCreamStation's defaults (dishWidth 0.15, boxBottom 0.12, liftRise 0.1).
var g = UnityEngine.Object.FindObjectOfType<GamePhaseManager>(); if (g == null) return "no GamePhaseManager yet";
g.OnDoneButtonClicked();
var F = "FOLDER"; var OUTDIR = System.IO.Path.Combine(Application.dataPath, "../Captures/flow"); System.IO.Directory.CreateDirectory(OUTDIR);
var st = UnityEngine.Object.FindObjectOfType<IceCreamStation>(); if (st == null) return "no station";
var os = UnityEngine.Object.FindObjectOfType<OrderSystem>(); if (os == null) return "no order system";
System.Func<string, Sprite> load = (name) => {
    var p = System.IO.Path.Combine(F, name + ".png"); if (!System.IO.File.Exists(p)) return null;
    var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false); tex.LoadImage(System.IO.File.ReadAllBytes(p)); tex.wrapMode = TextureWrapMode.Clamp; tex.filterMode = FilterMode.Bilinear;
    return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
};
var dishBack = load("dish_back"); var frontEmpty = load("dish_front_empty"); var frontBowl = load("dish_front_bowl"); var bowlS = load("bowl"); var trashS = load("trash");
var scoopS = new Sprite[4]; var letters = "vscm"; for (var i = 0; i < 4; i++) scoopS[i] = load("scoop_small_" + letters[i]);
if (dishBack == null || bowlS == null) return "no dish pictures in " + F;
var box = st.GetComponent<SpriteRenderer>(); var b = box.sprite.bounds; var layer = box.sortingLayerName; var order = box.sortingOrder;
var FL = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var sections = (SpriteRenderer[])typeof(IceCreamStation).GetField("sections", FL).GetValue(st);
var count = IceCreamStation.ActiveFlavours; var rects = count < 4 ? st.smallSectionRects : st.sectionRects;
System.Func<Vector2, Vector3> At = (frac) => new Vector3(b.min.x + frac.x * b.size.x, b.min.y + frac.y * b.size.y, 0f);
var holder = st.transform.Find("Holder"); if (holder != null) holder.gameObject.SetActive(false);
var build = st.transform.Find("Build"); if (build != null) build.gameObject.SetActive(false);
var rowLeft = rects[0].x; var rowRight = rects[count - 1].xMax + rects[0].x; var rowCentre = (rowLeft + rowRight) * 0.5f;
var waffle = st.transform.Find("Waffle"); var wr = waffle != null ? waffle.GetComponent<SpriteRenderer>() : null;
if (waffle != null && wr != null && wr.sprite != null) { var wh = 0.66f; var ww = wh * wr.sprite.rect.width / wr.sprite.rect.height * b.size.y / b.size.x; waffle.localPosition = At(new Vector2(rowRight + 0.02f + ww * 0.5f, rects[0].center.y)) + Vector3.back * 0.02f; }
var waffleClosed = wr != null ? wr.sprite : null; var waffleOpen = Resources.Load<Sprite>("IceCream/waffle_open");
var n = IceCreamStation.ActiveFlavours >= 4 ? 6 : 4; var dishWidth = 0.15f; var dw = dishWidth * b.size.x; var ks = dw / dishBack.bounds.size.x; var dh = dishBack.bounds.size.y * ks;
var top = b.min.y + 0.12f * b.size.y; var cy = top - dh * 0.5f;
var roots = new Transform[n]; var backs = new SpriteRenderer[n]; var bowls = new SpriteRenderer[n]; var fronts = new SpriteRenderer[n]; var trashes = new GameObject[n]; var scoops = new SpriteRenderer[n, 3]; var rest = new Vector3[n];
var hasBowl = new bool[n]; var flav = new System.Collections.Generic.List<int>[n];
System.Func<Transform, string, int, SpriteRenderer> child = (parent, name, so) => { var go = new GameObject(name, typeof(SpriteRenderer)); go.transform.SetParent(parent, false); go.transform.localPosition = new Vector3(0f, 0f, -0.001f * (so - order)); var sr = go.GetComponent<SpriteRenderer>(); sr.sortingLayerName = layer; sr.sortingOrder = so; sr.enabled = false; return sr; };
for (var i = 0; i < n; i++)
{
    flav[i] = new System.Collections.Generic.List<int>();
    var go = new GameObject("DemoDish" + i, typeof(SpriteRenderer)); go.transform.SetParent(st.transform, false);
    rest[i] = new Vector3(b.min.x + (rowCentre + (i - (n - 1) * 0.5f) * dishWidth) * b.size.x, cy, -0.03f - 0.001f * i);
    go.transform.localPosition = rest[i]; go.transform.localScale = new Vector3(ks, ks, 1f); roots[i] = go.transform;
    backs[i] = go.GetComponent<SpriteRenderer>(); backs[i].sortingLayerName = layer; backs[i].sortingOrder = order + 3; backs[i].sprite = dishBack;
    bowls[i] = child(go.transform, "Bowl", order + 4); bowls[i].sprite = bowlS;
    for (var s = 0; s < 3; s++) scoops[i, s] = child(go.transform, "Scoop" + s, order + 5 + s);
    fronts[i] = child(go.transform, "Front", order + 9); fronts[i].sprite = frontEmpty; fronts[i].enabled = true;
    var t = new GameObject("Trash", typeof(SpriteRenderer)); t.transform.SetParent(go.transform, false); var tr = t.GetComponent<SpriteRenderer>(); tr.sortingLayerName = layer; tr.sortingOrder = order + 10; tr.sprite = trashS;
    t.transform.localPosition = new Vector3(dishBack.bounds.extents.x + (trashS != null ? trashS.bounds.size.x : 0.5f) * 0.7f, dishBack.bounds.size.y * 0.1f, -0.01f); t.SetActive(false); trashes[i] = t;
}
System.Action<int> refresh = (i) => {
    bowls[i].enabled = hasBowl[i]; fronts[i].sprite = hasBowl[i] ? frontBowl : frontEmpty;
    var m = flav[i].Count;
    for (var s = 0; s < 3; s++) { var r = scoops[i, s]; r.enabled = s < m; if (s >= m) continue; r.sprite = scoopS[flav[i][s]]; var sw = r.sprite.bounds.size.x; r.transform.localPosition = new Vector3((s - (m - 1) * 0.5f) * sw * 0.72f, dishBack.bounds.min.y + 0.643f * dishBack.bounds.size.y - (s % 2 == 1 ? 0.014f * dishBack.bounds.size.y : 0f), -0.001f * (3 + s)); }
};
System.Action<int, int> setOrder = (i, so) => { backs[i].sortingOrder = so; bowls[i].sortingOrder = so + 1; for (var s = 0; s < 3; s++) scoops[i, s].sortingOrder = so + 2 + s; fronts[i].sortingOrder = so + 6; trashes[i].GetComponent<SpriteRenderer>().sortingOrder = so + 7; };
var selected = -1;
System.Action<int> select = (i) => { if (selected >= 0) { var o = selected; roots[o].localScale = new Vector3(ks, ks, 1f); roots[o].localPosition = rest[o]; setOrder(o, order + 3); trashes[o].SetActive(false); } selected = i; if (i < 0) return; roots[i].localScale = new Vector3(ks * 1.5f, ks * 1.5f, 1f); roots[i].localPosition = rest[i] + Vector3.up * (dh * 0.1f) + Vector3.back * 0.05f; setOrder(i, order + 23); trashes[i].SetActive(true); };
// the order bubbles: the dish look instead of the cone, found by name (Parts/Cone, Parts/ScoopN under an IceCream item; the branch's IceCreamOrderIcon does this for real)
System.Action reskin = () => {
    foreach (var parts in UnityEngine.Object.FindObjectsOfType<Transform>())
    {
        if (parts.name != "Parts" || parts.parent == null || !parts.parent.name.StartsWith("IceCream")) continue;
        var coneT = parts.Find("Cone"); if (coneT == null) continue; var cone = coneT.GetComponent<SpriteRenderer>(); if (cone == null || cone.sprite == dishBack) continue;
        var v = ""; var sc = new System.Collections.Generic.List<SpriteRenderer>();
        for (var s = 0; s < 3; s++) { var t = parts.Find("Scoop" + s); if (t == null) continue; var r = t.GetComponent<SpriteRenderer>(); if (r == null) continue; sc.Add(r); if (r.enabled && r.sprite != null) v += r.sprite.name.EndsWith("s") ? "s" : r.sprite.name.EndsWith("c") ? "c" : r.sprite.name.EndsWith("m") ? "m" : "v"; }
        var wantBowl = v.Length >= 2;
        parts.localScale = new Vector3(0.85f, 0.85f, 1f); parts.localPosition = Vector3.zero; cone.sprite = dishBack; coneT.localPosition = Vector3.zero;
        var bw = child(parts, "Bowl", cone.sortingOrder + 1); bw.sprite = bowlS; bw.enabled = wantBowl;
        var fr = child(parts, "Front", cone.sortingOrder + 6); fr.sprite = wantBowl ? frontBowl : frontEmpty; fr.enabled = true;
        var m = v.Length;
        for (var s = 0; s < sc.Count; s++) { var r = sc[s]; if (s >= m) { r.enabled = false; continue; } r.sprite = scoopS[Mathf.Max(0, letters.IndexOf(v[s]))]; r.sortingOrder = cone.sortingOrder + 2 + s; var sw = r.sprite.bounds.size.x; r.transform.localPosition = new Vector3((s - (m - 1) * 0.5f) * sw * 0.72f, dishBack.bounds.min.y + 0.643f * dishBack.bounds.size.y - (s % 2 == 1 ? 0.014f * dishBack.bounds.size.y : 0f), -0.001f * (s + 2)); }
    }
};
System.Collections.IEnumerator Pop(Transform t) { var s0 = t.localScale; var e = 0f; while (e < 0.16f) { e += Time.deltaTime; var k = Mathf.Sin(Mathf.Clamp01(e / 0.16f) * Mathf.PI); t.localScale = s0 * (1f + 0.25f * k); yield return null; } t.localScale = s0; }
System.Collections.IEnumerator Fly(Transform t, Vector3 fromWorld) { var to = t.localPosition; var from = t.parent.InverseTransformPoint(fromWorld); from.z = to.z; var e = 0f; while (e < 0.22f) { e += Time.deltaTime; var k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(e / 0.22f)); t.localPosition = Vector3.Lerp(from, to, k) + Vector3.up * (0.35f * Mathf.Sin(k * Mathf.PI)); yield return null; } t.localPosition = to; st.StartCoroutine(Pop(t)); }
System.Collections.IEnumerator Capture() { for (var k = 0; k < 700; k++) { ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(OUTDIR, "f_" + k.ToString("D4") + ".png")); yield return new WaitForSeconds(0.1f); } }
System.Collections.IEnumerator Reskin() { for (var k = 0; k < 300; k++) { reskin(); yield return new WaitForSeconds(0.25f); } }
System.Collections.IEnumerator AddScoop(int dish, int f) { flav[dish].Add(f); var sec = sections[f]; if (sec != null) st.StartCoroutine(Pop(sec.transform)); refresh(dish); var r = scoops[dish, flav[dish].Count - 1]; if (sec != null) yield return Fly(r.transform, sec.transform.position); yield return new WaitForSeconds(0.7f); }
System.Collections.IEnumerator Bake() { if (wr != null) st.StartCoroutine(Pop(waffle)); var t = 0f; while (t < 4f) { t += Time.deltaTime; if (wr != null) wr.color = Color.Lerp(Color.white, new Color(1f, 0.92f, 0.82f), 0.5f + 0.5f * Mathf.Sin(t * 5f)); yield return null; } if (wr != null) { wr.color = Color.white; if (waffleOpen != null) wr.sprite = waffleOpen; } yield return new WaitForSeconds(0.4f); var given = 0; for (var i = 0; i < n && given < 2; i++) { if (hasBowl[i] || flav[i].Count > 0) continue; hasBowl[i] = true; given++; refresh(i); st.StartCoroutine(Fly(bowls[i].transform, waffle.position)); yield return new WaitForSeconds(0.5f); } yield return new WaitForSeconds(1.2f); if (wr != null && waffleClosed != null) wr.sprite = waffleClosed; }
System.Func<string> firstOrder = () => { foreach (var tv in os.GetCurrentOrderTypesWithVariants()) if (tv.StartsWith("IceCream:")) return tv.Substring(9); return null; };
System.Collections.IEnumerator Demo()
{
    yield return new WaitForSeconds(1.5f);
    st.StartCoroutine(Capture()); st.StartCoroutine(Reskin());
    yield return new WaitForSeconds(1.2f);
    yield return Bake();
    yield return new WaitForSeconds(0.8f);
    for (var round = 0; round < 2; round++)
    {
        var want = firstOrder(); var waited = 0f;
        while (want == null && waited < (round == 0 ? 15f : 10f)) { yield return new WaitForSeconds(0.5f); waited += 0.5f; want = firstOrder(); }
        if (want == null) break;
        var wantBowl = want.Length >= 2; var dish = -1;
        for (var i = 0; i < n; i++) if (hasBowl[i] == wantBowl && flav[i].Count == 0) { dish = i; break; }
        if (dish < 0) dish = round;
        select(dish); st.StartCoroutine(Pop(roots[dish])); yield return new WaitForSeconds(0.9f);
        foreach (var ch in want) yield return AddScoop(dish, letters.IndexOf(ch));
        yield return new WaitForSeconds(0.8f);
        var ok = os.TryServeItem("IceCream", want);
        if (ok) { hasBowl[dish] = false; flav[dish].Clear(); refresh(dish); }
        select(-1); yield return new WaitForSeconds(1.5f);
        if (round == 0)
        {
            var d2 = -1; for (var i = 0; i < n; i++) if (flav[i].Count == 0 && i != dish) { d2 = i; break; }
            if (d2 >= 0)
            {
                select(d2); yield return new WaitForSeconds(0.9f); yield return AddScoop(d2, 1); yield return new WaitForSeconds(0.6f);
                st.StartCoroutine(Pop(trashes[d2].transform)); yield return new WaitForSeconds(0.3f); hasBowl[d2] = false; flav[d2].Clear(); refresh(d2); yield return new WaitForSeconds(0.5f); select(-1); yield return new WaitForSeconds(1.2f);
                select(d2); yield return new WaitForSeconds(0.8f); yield return AddScoop(d2, 0); yield return new WaitForSeconds(0.6f); select(-1); yield return new WaitForSeconds(1.5f);
            }
        }
    }
    yield return new WaitForSeconds(1.5f);
    st.StopAllCoroutines();
}
st.StartCoroutine(Demo());
return "day started and demo running: " + n + " dishes, " + count + " flavours, t=" + Time.time.ToString("F1");
