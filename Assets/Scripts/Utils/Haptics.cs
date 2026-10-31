using System.Collections;
using UnityEngine;

/// <summary>
/// Taptic Engine taps on iPhone (owner, 31 October 2026): a light tap when an item is served, two taps when an
/// order is completed with time to spare. Goes through Assets/Plugins/iOS/Haptics.mm (UIImpactFeedbackGenerator),
/// which follows the phone's System Haptics switch; nothing in the editor or on other platforms.
/// No buzz on a wrong tap: the item's shake already says it.
/// </summary>
public static class Haptics
{
#if UNITY_IOS && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("__Internal")] private static extern void FTC_HapticImpact(int style);
#endif

    private const int Light = 0, Medium = 1;
    private const float DoubleGap = 0.09f;   // seconds between the two taps of a perfect order

    /// One light tap: an item served.
    public static void Tap() => Impact(Light);

    /// Two taps, light then medium: an order completed fast. The host runs the short wait.
    public static void DoubleTap(MonoBehaviour host)
    {
        Impact(Light);
        if (host != null && host.isActiveAndEnabled) host.StartCoroutine(Second());
    }

    private static IEnumerator Second()
    {
        yield return new WaitForSecondsRealtime(DoubleGap);
        Impact(Medium);
    }

    private static void Impact(int style)
    {
#if UNITY_IOS && !UNITY_EDITOR
        try { FTC_HapticImpact(style); } catch (System.Exception) { }
#endif
    }
}
