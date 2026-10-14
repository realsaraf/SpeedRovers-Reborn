#import <UIKit/UIKit.h>
extern "C" void SRSoftImpact()
{
    dispatch_async(dispatch_get_main_queue(), ^{
        static UIImpactFeedbackGenerator *feedback;
        if (!feedback) feedback = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleSoft];
        [feedback impactOccurredWithIntensity:0.4];
        [feedback prepare];
    });
}
