#import <AVFoundation/AVFoundation.h>
#import <UIKit/UIKit.h>

// Music/effects toggles remain in Unity. No microphone or background-audio mode
// is requested. Playback keeps intentional game audio audible with Silent Mode;
// MixWithOthers lets a player's own music continue if they mute our soundtrack.
static void SRConfigurePlaybackSession(void)
{
    AVAudioSession *session=AVAudioSession.sharedInstance;
    NSError *error=nil;
    if(![session setCategory:AVAudioSessionCategoryPlayback
                       mode:AVAudioSessionModeDefault
                    options:AVAudioSessionCategoryOptionMixWithOthers error:&error])
        NSLog(@"Speed Rovers: audio category error: %@", error);
    error=nil;
    if(![session setActive:YES error:&error])
        NSLog(@"Speed Rovers: audio activation error: %@", error);
}

extern "C" void SRActivateGameAudio(void)
{
    dispatch_async(dispatch_get_main_queue(), ^{
        static BOOL observing=NO;
        if(!observing)
        {
            observing=YES;
            [NSNotificationCenter.defaultCenter addObserverForName:AVAudioSessionInterruptionNotification
                object:AVAudioSession.sharedInstance queue:NSOperationQueue.mainQueue
                usingBlock:^(NSNotification *note){
                    if([note.userInfo[AVAudioSessionInterruptionTypeKey] unsignedIntegerValue]==AVAudioSessionInterruptionTypeEnded &&
                       UIApplication.sharedApplication.applicationState==UIApplicationStateActive)
                        SRConfigurePlaybackSession();
                }];
            [NSNotificationCenter.defaultCenter addObserverForName:UIApplicationDidBecomeActiveNotification
                object:nil queue:NSOperationQueue.mainQueue usingBlock:^(NSNotification *note){SRConfigurePlaybackSession();}];
        }
        SRConfigurePlaybackSession();
    });
}
