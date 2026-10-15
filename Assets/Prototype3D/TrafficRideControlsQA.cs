using System.Collections;
using UnityEngine;

public partial class TrafficRidePrototype
{
    Vector2 ControlScreenPoint(Vector2 p)
    {
        var safe=Screen.safeArea;float scale=Mathf.Min(safe.width/1280f,safe.height/720f);
        float left=safe.x+(safe.width-1280*scale)*.5f,top=Screen.height-safe.yMax+(safe.height-720*scale)*.5f;
        return new Vector2(left+p.x*scale,Screen.height-(top+p.y*scale));
    }
    IEnumerator CheckRideControlsAndAudio()
    {
        StartRace();countdown=1;speed=100;hitCooldown=20;nitro=.8f;
        bool previousTilt=tiltControl;tiltControl=true;steerInput=.4f;throttle=brake=false;
        Vector2 gas=ControlScreenPoint(ThrottleControl.center),boost=ControlScreenPoint(NitroControl.center),hop=ControlScreenPoint(JumpControl.center);
        ProcessRideTouch(90,gas,TouchPhase.Began);countdown=0;throttle=false;
        ProcessRideTouch(90,gas,TouchPhase.Stationary);
        Check(throttle,"throttle held before GO stays engaged through countdown");
        ProcessRideTouch(91,boost,TouchPhase.Began);
        Check(throttle&&boosting&&Mathf.Abs(steerInput-.4f)<.001f,"second finger activates nitro while throttle and tilt remain active");
        ProcessRideTouch(91,boost,TouchPhase.Ended);ProcessRideTouch(92,hop,TouchPhase.Began);
        Check(throttle&&jumpVelocity>0,"second finger jump works while holding throttle");
        ProcessRideTouch(92,hop,TouchPhase.Canceled);ProcessRideTouch(90,gas,TouchPhase.Ended);
        Check(touchStarts.Count==0,"ended and canceled ability touches release cleanly");
        Check(ControlAt(ControlScreenPoint(new Vector2(1080,530)))==1&&ControlAt(ControlScreenPoint(new Vector2(180,650)))==-1,"large pedal targets match visible lower-corner controls");
        Check(ControlAt(boost)==2&&ControlAt(hop)==3,"ability hit zones do not become throttle or brake");
        RecenterTilt();for(int i=0;i<20;i++)SampleTiltGravity(new Vector3(0,-.7071f,-.7071f),.02f);
        Check(Mathf.Abs(TiltSteering)<.001f,"tilt calibrates a comfortable neutral phone angle");
        for(int i=0;i<30;i++)SampleTiltGravity(new Vector3(.342f,-.6645f,-.6645f),.02f);
        Check(TiltSteering>.94f,"right gravity roll produces smooth right steering");
        for(int i=0;i<45;i++)SampleTiltGravity(new Vector3(-.342f,-.6645f,-.6645f),.02f);
        Check(TiltSteering<-.94f,"left gravity roll produces smooth left steering");
        float stableTilt=TiltSteering;SampleTiltGravity(Vector3.zero,.02f);
        Check(Mathf.Abs(stableTilt-TiltSteering)<.001f&&MapTiltSteering(1,0)==0,"tilt rejects zero sensor samples and neutral hand jitter");
        tiltControl=previousTilt;boosting=false;nitro=.8f;jump=jumpVelocity=jumpCooldown=0;
        yield return new WaitForSeconds(1.2f);
        Check(musicOn&&music.isPlaying&&music.clip!=null&&music.volume>.3f,"race background music is loaded playing and mixed audibly");
        yield return Shot("08-new-controls");
        // Exercise a stopped source as after an interrupted mobile audio device.
        music.Stop();yield return new WaitForSeconds(1.2f);
        Check(music.isPlaying,"music recovers after interrupted playback");
        musicOn=false;SuspendRideAudio();ResumeRideAudio();TickRideAudio(.1f);
        Check(music.mute,"audio recovery preserves the music mute preference");musicOn=true;
        ProcessRideTouch(94,ControlScreenPoint(PauseControl.center),TouchPhase.Began);
        Check(page==Page.Pause&&!throttle&&touchStarts.Count==0,"touch pause clears held controls");
        SetPage(Page.Settings);yield return Shot("09-control-settings");
    }
}
