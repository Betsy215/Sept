// r31 (20 Nov 2026): the book part again, fast: rec_book.cs saved PNGs on the main thread and ran at about 10 fps, so
// the book's real-time animations were choppy. Here each frame goes into a RenderTexture, comes back by
// AsyncGPUReadback and is written raw (RGBA32, w x h) on a worker thread; times.txt keeps each frame's real time
// (when it was rendered). The cut (Store/video/cut_r31.py) turns the raw files into pictures (they may be upside down).
var shop = UnityEngine.Object.FindObjectOfType<ShopManager>(); if (shop == null) return "no shop";
var ROOT = System.IO.Path.Combine(Application.dataPath, "../Captures/rec_book2"); var dir = System.IO.Path.Combine(ROOT, "book");
System.IO.Directory.CreateDirectory(dir);
int w = 0, h = 0; RenderTexture rt = null;   // the game view's size is only known inside the game loop
var times = new System.Text.StringBuilder(); int frame = 0, pending = 0; bool recording = false; float t0 = 0f;
var writes = new System.Collections.Generic.List<System.Threading.Tasks.Task>();
System.Func<string, bool> click = name => {
    var b = GameObject.Find("CookieBook"); if (b == null) return false;
    foreach (var x in b.GetComponentsInChildren<UnityEngine.UI.Button>(true)) if (x.name == name && x.gameObject.activeInHierarchy) { x.onClick.Invoke(); return true; }
    return false; };
System.Collections.IEnumerator Rec()
{
    while (shop != null)
    {
        yield return new WaitForEndOfFrame();
        if (!recording) continue;
        ScreenCapture.CaptureScreenshotIntoRenderTexture(rt);
        var n = frame++; var tt = Time.realtimeSinceStartup - t0; times.Append(n + " " + tt.ToString("F4") + "\n"); pending++;
        UnityEngine.Rendering.AsyncGPUReadback.Request(rt, 0, TextureFormat.RGBA32, req => {
            pending--; if (req.hasError) return;
            var bytes = req.GetData<byte>().ToArray(); var path = System.IO.Path.Combine(dir, "f" + n.ToString("D5") + ".raw");
            lock (writes) writes.Add(System.Threading.Tasks.Task.Run(() => System.IO.File.WriteAllBytes(path, bytes)));
        });
    }
}
System.Collections.IEnumerator Run()
{
    Time.captureFramerate = 0;
    yield return new WaitForEndOfFrame(); w = Screen.width; h = Screen.height; rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32);
    shop.StartCoroutine(Rec());
    t0 = Time.realtimeSinceStartup; recording = true;
    yield return new WaitForSecondsRealtime(0.8f);
    GameObject.Find("CookieBookButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
    yield return new WaitForSecondsRealtime(2.0f);
    click("PrevPage"); yield return new WaitForSecondsRealtime(1.8f);
    click("Tab1"); yield return new WaitForSecondsRealtime(1.5f);
    click("Tab2"); yield return new WaitForSecondsRealtime(0.8f);
    click("PrevPage"); yield return new WaitForSecondsRealtime(1.6f);   // the cookies open on their last page (one cookie); back to the full first page
    recording = false;
    while (pending > 0) yield return null;
    System.Threading.Tasks.Task[] all; lock (writes) all = writes.ToArray();
    System.Threading.Tasks.Task.WaitAll(all);
    System.IO.File.WriteAllText(System.IO.Path.Combine(ROOT, "times.txt"), times.ToString());
    System.IO.File.WriteAllText(System.IO.Path.Combine(ROOT, "done.txt"), "done " + frame + " " + w + " " + h);
    rt.Release();
}
shop.StartCoroutine(Run()); return "recording book (fast)";
