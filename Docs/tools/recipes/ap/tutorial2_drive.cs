// Drives the chapter 2 tutorial in the editor for a capture (26 October 2026): run on the chapter 2 day 1 scene after
// ch2 session setup with levelsCompleted = 12 and tutorial2Seen false. Presses Done, then whenever the director waits
// for a tap, waits 1.4 s (the arrow and caption show), saves a frame to Captures/tut/, and plays the tap. Frames every
// 0.7 s as well. Stops after 75 s or when the director is gone.
var dir = UnityEngine.Object.FindObjectOfType<TutorialDirector>(); if (dir == null) return "no TutorialDirector on this day";
var g = UnityEngine.Object.FindObjectOfType<GamePhaseManager>(); var lm = UnityEngine.Object.FindObjectOfType<LevelManager>();
var OUT = System.IO.Path.Combine(Application.dataPath, "../Captures/tut"); System.IO.Directory.CreateDirectory(OUT);
var FL = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var fHit = typeof(TutorialDirector).GetField("awaitingHit", FL); var fTapped = typeof(TutorialDirector).GetField("tapped", FL);
System.Collections.IEnumerator Drive()
{
    yield return new WaitForSeconds(1.0f);
    if (g != null && g.GetCurrentPhase() == GamePhase.ARRANGEMENT) g.OnDoneButtonClicked();
    var k = 0; var t = 0f; var next = 0f;
    while (t < 75f && dir != null)
    {
        t += Time.deltaTime;
        if (t >= next) { ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(OUT, "f_" + (k++).ToString("D3") + ".png")); next = t + 0.7f; }
        if (fHit.GetValue(dir) != null)
        {
            yield return new WaitForSeconds(1.4f); t += 1.4f;
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(OUT, "tap_" + (k++).ToString("D3") + ".png"));
            yield return null;
            if (dir != null && fHit.GetValue(dir) != null) { fHit.SetValue(dir, null); fTapped.SetValue(dir, true); }
        }
        yield return null;
    }
}
lm.StartCoroutine(Drive()); return "driving the tutorial; frames in " + OUT;
