using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// Apple's own rating popup (owner, 19 November 2026: "add rating prompt after player finished day 3"). Asked once, the
/// first time the shop opens after day 3 is finished: a calm moment, after the day's board and its ad, before the next
/// day. Apple decides whether the popup really shows (at most three times a year per player) and never shows it in
/// TestFlight builds; nothing happens in the editor or on other platforms.
public static class RatingPrompt
{
    private const string AskedKey = "ftc.rating.asked";
    private const int AfterDay = 3;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot() { SceneManager.sceneLoaded += OnSceneLoaded; }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Shop" || PlayerPrefs.GetInt(AskedKey, 0) == 1) return;
        var sm = SessionManager.Instance;
        if (sm == null || sm.GetLevelsCompleted() < AfterDay) return;
        PlayerPrefs.SetInt(AskedKey, 1); PlayerPrefs.Save();
        var host = new GameObject("RatingPrompt").AddComponent<Host>();
        host.StartCoroutine(host.AskSoon());
    }

    private class Host : MonoBehaviour
    {
        public IEnumerator AskSoon()
        {
            yield return new WaitForSecondsRealtime(1.2f);   // the shop is on screen first
#if UNITY_IOS && !UNITY_EDITOR
            UnityEngine.iOS.Device.RequestStoreReview();
#else
            Debug.Log("[RatingPrompt] would ask for a rating now (iOS only)");
#endif
            Destroy(gameObject);
        }
    }
}
