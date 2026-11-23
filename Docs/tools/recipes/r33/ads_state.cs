// r33 (23 Nov 2026): the ad start's state (AdsInitializer keeps Unity Ads started) and the interstitial's
var F = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static;
var t = typeof(AdsInitializer); System.Func<string, object> g = n => t.GetField(n, F).GetValue(null);
var I = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance; var svc = InterstitialAdService.Instance;
return "initialized " + UnityEngine.Advertisements.Advertisement.isInitialized + " supported " + UnityEngine.Advertisements.Advertisement.isSupported
  + " | tries " + g("tries") + " failures " + g("failures") + " trying " + g("trying") + " keeping " + g("keeping") + " announced " + g("announced") + " firstTry " + g("firstTryMade")
  + " | interstitial ready " + typeof(InterstitialAdService).GetField("adReady", I).GetValue(svc) + " loading " + typeof(InterstitialAdService).GetField("loading", I).GetValue(svc);
