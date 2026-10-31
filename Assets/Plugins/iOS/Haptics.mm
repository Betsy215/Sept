// Taptic Engine taps for the game's serves (owner, 31 October 2026): a light tap on a serve, two on a perfect order.
// UIImpactFeedbackGenerator drives the same engine as the system's own taps; it follows the phone's
// Settings > Sounds & Haptics > System Haptics switch and needs no permission. style: 0 light, 1 medium, 2 heavy,
// 3 soft, 4 rigid (UIImpactFeedbackStyle). Called from Assets/Scripts/Utils/Haptics.cs.
#import <UIKit/UIKit.h>

extern "C" void FTC_HapticImpact(int style)
{
    if (style < 0 || style > 4) style = 0;
    UIImpactFeedbackGenerator *generator = [[UIImpactFeedbackGenerator alloc] initWithStyle:(UIImpactFeedbackStyle)style];
    [generator prepare];
    [generator impactOccurred];
}
