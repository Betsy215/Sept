// r32 (21 Nov 2026): the kitchen flow in motion, for the report: rec_book2.cs's fast recorder (RenderTexture +
// AsyncGPUReadback, raw RGBA frames, times.txt) while a choux and a cake are baked and the mixer and the board work.
// Run in an open kitchen. Frames: Captures/rec_kitchen/f*.raw; done.txt holds "done <frames> <w> <h>".
var host = SessionManager.Instance; if (host == null) return "no session";
var ROOT = System.IO.Path.Combine(Application.dataPath, "../Captures/rec_kitchen"); System.IO.Directory.CreateDirectory(ROOT);
int w = 0, h = 0; RenderTexture rt = null;
var times = new System.Text.StringBuilder(); int frame = 0, pending = 0; bool recording = false; float t0 = 0f; bool running = true;
var writes = new System.Collections.Generic.List<System.Threading.Tasks.Task>();
System.Collections.IEnumerator Rec()
{
    while (running)
    {
        yield return new WaitForEndOfFrame();
        if (!recording) continue;
        ScreenCapture.CaptureScreenshotIntoRenderTexture(rt);
        var n = frame++; var tt = Time.realtimeSinceStartup - t0; times.Append(n + " " + tt.ToString("F4") + "\n"); pending++;
        UnityEngine.Rendering.AsyncGPUReadback.Request(rt, 0, TextureFormat.RGBA32, req => {
            pending--; if (req.hasError) return;
            var bytes = req.GetData<byte>().ToArray(); var path = System.IO.Path.Combine(ROOT, "f" + n.ToString("D5") + ".raw");
            lock (writes) writes.Add(System.Threading.Tasks.Task.Run(() => System.IO.File.WriteAllBytes(path, bytes)));
        });
    }
}
System.Collections.IEnumerator Run()
{
    yield return new WaitForEndOfFrame(); w = Screen.width; h = Screen.height; rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32);
    host.StartCoroutine(Rec());
    t0 = Time.realtimeSinceStartup; recording = true;
    yield return new WaitForSecondsRealtime(0.3f);
    KitchenFlow.Baked("Choux"); KitchenFlow.Baked("Cake");
    yield return new WaitForSecondsRealtime(10.5f);
    recording = false; running = false;
    while (pending > 0) yield return null;
    System.Threading.Tasks.Task[] all; lock (writes) all = writes.ToArray();
    System.Threading.Tasks.Task.WaitAll(all);
    System.IO.File.WriteAllText(System.IO.Path.Combine(ROOT, "times.txt"), times.ToString());
    System.IO.File.WriteAllText(System.IO.Path.Combine(ROOT, "done.txt"), "done " + frame + " " + w + " " + h);
    rt.Release();
}
host.StartCoroutine(Run()); return "recording the kitchen";
