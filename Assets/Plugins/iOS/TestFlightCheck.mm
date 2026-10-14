// Is this a TestFlight (or Xcode) build? Apple gives those a sandbox receipt; App Store builds get "receipt".
// Used by AdsInitializer to turn Unity Ads test mode on for testers (owner, 14 October 2026), so our own test
// sessions never count as real impressions on the Unity dashboard.
#import <Foundation/Foundation.h>
extern "C" bool FTC_IsTestFlight()
{
    NSURL *url = [[NSBundle mainBundle] appStoreReceiptURL];
    return url != nil && [[url lastPathComponent] isEqualToString:@"sandboxReceipt"];
}
