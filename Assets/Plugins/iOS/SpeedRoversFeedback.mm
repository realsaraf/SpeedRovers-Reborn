#import <UIKit/UIKit.h>
#import <AudioToolbox/AudioToolbox.h>
#import <CoreHaptics/CoreHaptics.h>
#include <math.h>

// UIKit and engine state stay on the main queue. Prepare and reuse the generators.
static UIImpactFeedbackGenerator *sLightFeedback;
static UIImpactFeedbackGenerator *sCollisionFeedback;
static CHHapticEngine *sHapticEngine;
static BOOL sEngineRunning = NO;
static BOOL sReportedFailure = NO;

static void SRMain(dispatch_block_t action)
{
    if ([NSThread isMainThread]) action();
    else dispatch_async(dispatch_get_main_queue(), action);
}

static BOOL SRHasHapticHardware()
{
    return [CHHapticEngine capabilitiesForHardware].supportsHaptics;
}

static void SRPrepareGenerators()
{
    if (!sLightFeedback) sLightFeedback = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleLight];
    if (!sCollisionFeedback) sCollisionFeedback = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleMedium];
    [sLightFeedback prepare];
    [sCollisionFeedback prepare];
}

static void SRReportFailure(NSError *error)
{
    if (!sReportedFailure)
    {
        NSLog(@"Speed Rovers: custom haptics unavailable (%@); using impact feedback.", error.localizedDescription ?: @"engine not ready");
        sReportedFailure = YES;
    }
}

static BOOL SREnsureEngine()
{
    if (!SRHasHapticHardware()) return NO;
    NSError *error = nil;
    if (!sHapticEngine)
    {
        sHapticEngine = [[CHHapticEngine alloc] initAndReturnError:&error];
        if (!sHapticEngine || error) { SRReportFailure(error); return NO; }
        // The haptics engine never changes the Unity soundtrack's audio session category.
        sHapticEngine.playsHapticsOnly = YES;
        sHapticEngine.autoShutdownEnabled = YES;
        sHapticEngine.stoppedHandler = ^(CHHapticEngineStoppedReason reason) {
            SRMain(^{ sEngineRunning = NO; });
        };
        sHapticEngine.resetHandler = ^{
            SRMain(^{
                sEngineRunning = NO;
                // Create players per pulse; restart after reset without retaining stale players.
                if ([UIApplication sharedApplication].applicationState == UIApplicationStateActive) SREnsureEngine();
            });
        };
    }
    if (!sEngineRunning)
    {
        sEngineRunning = [sHapticEngine startAndReturnError:&error];
        if (!sEngineRunning || error) { SRReportFailure(error); return NO; }
    }
    return YES;
}

static void SRFallbackImpact(float severity)
{
    if (SRHasHapticHardware())
    {
        SRPrepareGenerators();
        [sCollisionFeedback impactOccurredWithIntensity:0.65 + severity * 0.35];
        [sCollisionFeedback prepare];
    }
    else if (UI_USER_INTERFACE_IDIOM() == UIUserInterfaceIdiomPhone)
    {
        // Phones with a vibration motor but no Core Haptics still receive impact feedback.
        AudioServicesPlaySystemSound(kSystemSoundID_Vibrate);
    }
}

extern "C" void SRPrepareFeedback()
{
    SRMain(^{
        if ([UIApplication sharedApplication].applicationState != UIApplicationStateActive) return;
        SRPrepareGenerators();
        SREnsureEngine();
    });
}

extern "C" void SRSuspendFeedback()
{
    SRMain(^{
        sEngineRunning = NO;
        [sHapticEngine stopWithCompletionHandler:nil];
    });
}

extern "C" void SRSoftImpact()
{
    SRMain(^{
        if ([UIApplication sharedApplication].applicationState != UIApplicationStateActive) return;
        SRPrepareGenerators();
        [sLightFeedback impactOccurredWithIntensity:0.7];
        [sLightFeedback prepare];
    });
}

extern "C" void SRCollisionImpact(float severity)
{
    SRMain(^{
        if ([UIApplication sharedApplication].applicationState != UIApplicationStateActive) return;
        float strength = fminf(1.0f, fmaxf(0.0f, severity));
        if (!SREnsureEngine()) { SRFallbackImpact(strength); return; }
        NSError *error = nil;
        float intensity = 0.55f + strength * 0.4f;
        CHHapticEventParameter *level = [[CHHapticEventParameter alloc] initWithParameterID:CHHapticEventParameterIDHapticIntensity value:intensity];
        CHHapticEventParameter *softness = [[CHHapticEventParameter alloc] initWithParameterID:CHHapticEventParameterIDHapticSharpness value:0.22f];
        CHHapticEventParameter *rumbleLevel = [[CHHapticEventParameter alloc] initWithParameterID:CHHapticEventParameterIDHapticIntensity value:intensity * 0.65f];
        CHHapticEvent *tap = [[CHHapticEvent alloc] initWithEventType:CHHapticEventTypeHapticTransient parameters:@[level, softness] relativeTime:0];
        CHHapticEvent *body = [[CHHapticEvent alloc] initWithEventType:CHHapticEventTypeHapticContinuous parameters:@[rumbleLevel, softness] relativeTime:0.015 duration:0.07 + strength * 0.06];
        CHHapticPattern *pattern = [[CHHapticPattern alloc] initWithEvents:@[tap, body] parameters:@[] error:&error];
        id<CHHapticPatternPlayer> player = pattern ? [sHapticEngine createPlayerWithPattern:pattern error:&error] : nil;
        if (!player || error || ![player startAtTime:CHHapticTimeImmediate error:&error])
        {
            SRReportFailure(error);
            SRFallbackImpact(strength);
        }
        else sReportedFailure = NO;
    });
}
