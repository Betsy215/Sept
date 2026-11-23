// r33: a failed start as Unity would report it (the static listener's callback), then Ensure: the failure is counted,
// the next try is scheduled, and nothing throws. In the editor Unity Ads is already started, so no real retry follows.
var F = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static;
var t = typeof(AdsInitializer);
var l = (UnityEngine.Advertisements.IUnityAdsInitializationListener)t.GetField("listener", F).GetValue(null);
l.OnInitializationFailed(UnityEngine.Advertisements.UnityAdsInitializationError.INTERNAL_ERROR, "simulated");
AdsInitializer.Ensure();
return "failures " + t.GetField("failures", F).GetValue(null) + " next try in " + ((float)t.GetField("nextTryAt", F).GetValue(null) - Time.realtimeSinceStartup).ToString("F1") + " s";
