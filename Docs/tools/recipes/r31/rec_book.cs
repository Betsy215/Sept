// r31 (20 Nov 2026, owner: "in the video, include some of opening the journal and briefly show the journal and cookies
// and staff pages"): in the shop, the book's button is tapped, the journal shows two pages, then the staff tab, then the
// cookies tab. The book animates on real time, so this part is recorded WITHOUT Time.captureFramerate: each frame's real
// time goes to times.txt and the cut picks frames by real time at 30 fps. Frames: Captures/rec_book/book/fNNNNN.png.
var shop = UnityEngine.Object.FindObjectOfType<ShopManager>(); if (shop == null) return "no shop";
var ROOT = System.IO.Path.Combine(Application.dataPath, "../Captures/rec_book");
var dir = System.IO.Path.Combine(ROOT, "book"); System.IO.Directory.CreateDirectory(dir);
var times = new System.Text.StringBuilder(); int frame = 0; bool recording = false; float t0 = 0f;
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
        ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(dir, "f" + frame.ToString("D5") + ".png"));
        times.Append(frame + " " + (Time.realtimeSinceStartup - t0).ToString("F4") + "\n"); frame++;
    }
}
System.Collections.IEnumerator Run()
{
    Time.captureFramerate = 0;
    shop.StartCoroutine(Rec());
    t0 = Time.realtimeSinceStartup; recording = true;
    yield return new WaitForSecondsRealtime(0.8f);
    GameObject.Find("CookieBookButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
    yield return new WaitForSecondsRealtime(2.0f);
    click("PrevPage"); yield return new WaitForSecondsRealtime(1.8f);
    click("Tab1"); yield return new WaitForSecondsRealtime(1.5f);
    click("Tab2"); yield return new WaitForSecondsRealtime(1.8f);
    recording = false; yield return null;
    System.IO.File.WriteAllText(System.IO.Path.Combine(ROOT, "times.txt"), times.ToString());
    System.IO.File.WriteAllText(System.IO.Path.Combine(ROOT, "done.txt"), "done " + frame);
}
shop.StartCoroutine(Run()); return "recording book";
