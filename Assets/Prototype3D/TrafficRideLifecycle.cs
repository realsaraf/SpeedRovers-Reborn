using UnityEngine;

public partial class TrafficRidePrototype
{
    bool mobileSuspended;
    bool applicationPaused;
    bool applicationFocused=true;
    void SetMobileSuspended(bool suspended)
    {
        if(!Application.isMobilePlatform||qaMode||mobileSuspended==suspended)return;
        mobileSuspended=suspended;
        if(suspended){
            if(page==Page.Riding)SetPage(Page.Pause);
            SuspendRideAudio();SuspendMotionFeedback();
        }else{
            ResumeRideAudio();RecenterTilt();PrepareMotionFeedback();
        }
    }
    void OnApplicationPause(bool pausedNow)
    {
        applicationPaused=pausedNow;
        if(pausedNow&&page==Page.Riding&&!qaMode)SetPage(Page.Pause);
        SetMobileSuspended(applicationPaused||!applicationFocused);
    }
    void OnApplicationFocus(bool focused)
    {
        applicationFocused=focused;
        if(!focused&&page==Page.Riding&&!qaMode)SetPage(Page.Pause);
        SetMobileSuspended(applicationPaused||!applicationFocused);
    }
}
