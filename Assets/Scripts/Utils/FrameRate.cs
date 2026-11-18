using UnityEngine;

/// 60 frames a second (owner, 18 November 2026: the menu's cookies and truck looked "lagging not very smooth").
/// Without a target, Unity on iPhone renders 30 frames a second, so slow steady motion (the scrolling cookie
/// background, the driving truck, every tween) moves in visible steps. vSync count is ignored on iOS; 0 lets the
/// target rule. The phone still drops to 30 by itself in Low Power Mode.
public static class FrameRate
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Apply()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
    }
}
