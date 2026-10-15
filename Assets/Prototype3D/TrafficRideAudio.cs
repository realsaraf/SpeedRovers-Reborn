using UnityEngine;
using System.Collections;
using System.Text;
using System.Runtime.InteropServices;

public partial class TrafficRidePrototype
{
    AudioSource outgoingMusic, idleEngine, nitroAudio;
    AudioClip menuMusicClip, raceMusicClip, shiftSound;
    bool rideAudioReady, rideAudioSuspended;
    float audioRecoveryAt, musicDuckUntil, engineLoad;
    bool engineAudioProbe;
    float engineProbeSpeed;
    int engineProbeGear=1;
    bool engineProbeThrottle;

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")] static extern void SRActivateGameAudio();
#endif

    AudioSource RideAudioSource(string label, bool loop, int priority)
    {
        var node=new GameObject(label);node.transform.SetParent(transform,false);
        var source=node.AddComponent<AudioSource>();source.playOnAwake=false;
        source.loop=loop;source.spatialBlend=0;source.dopplerLevel=0;
        source.priority=priority;source.volume=0;return source;
    }

    void InitializeRideAudio()
    {
        if(rideAudioReady)return;
        effects=RideAudioSource("Gameplay feedback",false,16);effects.volume=.55f;
        music=RideAudioSource("Music A",true,0);
        outgoingMusic=RideAudioSource("Music B",true,0);
        wind=RideAudioSource("Road airflow",true,80);
        idleEngine=RideAudioSource("Recorded engine idle",true,32);
        nitroAudio=RideAudioSource("Nitro combustion",true,48);
        // The engine is a separate bus like the other layers, with no legacy
        // procedural generator/filter attached to the game controller object.
        engine=RideAudioSource("Recorded engine under load",true,8);
        engine.Stop();engine.loop=true;engine.spatialBlend=0;engine.dopplerLevel=0;engine.priority=8;engine.volume=0;
        engine.clip=Resources.Load<AudioClip>("GrandTour/Audio/Engine");
        idleEngine.clip=Resources.Load<AudioClip>("GrandTour/Audio/EngineIdle");
        if(engine.clip!=null)engine.clip.LoadAudioData();
        if(idleEngine.clip!=null)idleEngine.clip.LoadAudioData();
        wind.clip=Resources.Load<AudioClip>("GrandTour/Audio/Wind");
        nitroAudio.clip=Resources.Load<AudioClip>("GrandTour/Audio/BoostLoop");
        menuMusicClip=Resources.Load<AudioClip>("GrandTour/Audio/MenuScore");
        raceMusicClip=Resources.Load<AudioClip>("GrandTour/Audio/RaceScore");
        clickSound=Resources.Load<AudioClip>("GrandTour/Audio/UI");
        shiftSound=Resources.Load<AudioClip>("GrandTour/Audio/Shift");
        coinSound=Resources.Load<AudioClip>("GrandTour/Audio/Coin");
        fuelSound=Resources.Load<AudioClip>("GrandTour/Audio/Fuel");
        boostSound=Resources.Load<AudioClip>("GrandTour/Audio/Boost");
        jumpSound=Resources.Load<AudioClip>("GrandTour/Audio/Jump");
        hitSound=Resources.Load<AudioClip>("GrandTour/Audio/Impact");
        passSound=Resources.Load<AudioClip>("GrandTour/Audio/Pass");
        rideAudioReady=true;
        AudioSettings.OnAudioConfigurationChanged+=RideAudioConfigurationChanged;
        ResumeRideAudio();SetRideAudioPage();
        Debug.Log("Ride audio: menu="+(menuMusicClip!=null?menuMusicClip.length.ToString("F1")+"s":"MISSING")+
            ", race="+(raceMusicClip!=null?raceMusicClip.length.ToString("F1")+"s":"MISSING")+
            ", recorded engine="+(engine.clip!=null&&idleEngine.clip!=null));
    }

    void SetRideAudioPage()
    {
        if(!rideAudioReady)return;
        AudioClip desired=page==Page.Riding||page==Page.Pause?raceMusicClip:menuMusicClip;
        if(desired==null){Debug.LogError("Required ride music resource is missing.");return;}
        if(music.clip!=desired)
        {
            // The previous track fades out while the new one fades in. No silent
            // Stop/Load/Play gap when opening the race or returning to the garage.
            var previous=music;music=outgoingMusic;outgoingMusic=previous;
            music.Stop();music.clip=desired;music.volume=0;
            music.mute=!musicOn||engineAudioProbe;
            if(!rideAudioSuspended)music.Play();
        }
        audioRecoveryAt=0;
        // Do not wait for the periodic recovery tick to hear the motor at GO.
        if(page==Page.Riding&&!rideAudioSuspended)
        {
            KeepRideLoopPlaying(engine,soundOn);
            KeepRideLoopPlaying(idleEngine,soundOn);
        }
    }

    void TickRideAudio(float dt)
    {
        if(!rideAudioReady||rideAudioSuspended)return;
        music.mute=outgoingMusic.mute=!musicOn||engineAudioProbe;
        engine.mute=idleEngine.mute=!soundOn;
        effects.mute=wind.mute=nitroAudio.mute=!soundOn||engineAudioProbe;
        bool riding=page==Page.Riding;
        float motorSpeed=engineAudioProbe?engineProbeSpeed:speed;
        int motorGear=engineAudioProbe?engineProbeGear:gear;
        bool motorThrottle=engineAudioProbe?engineProbeThrottle:throttle&&!brake;
        float speed01=Mathf.Clamp01(motorSpeed/TopSpeed);
        float musicTarget=page==Page.Pause?.28f:riding?.36f:.44f;
        if(Time.unscaledTime<musicDuckUntil)musicTarget*=.76f;
        music.volume=Mathf.MoveTowards(music.volume,musicTarget,dt*.65f);
        outgoingMusic.volume=Mathf.MoveTowards(outgoingMusic.volume,0,dt*.65f);
        if(outgoingMusic.volume<=0&&outgoingMusic.isPlaying)outgoingMusic.Stop();
        engineLoad=Mathf.MoveTowards(engineLoad,riding&&motorThrottle?1:.2f,dt*4);
        float gearProgress=Mathf.Clamp01((motorSpeed-(motorGear-1)*85)/85);
        float rpm=.9f+gearProgress*.7f+engineLoad*.1f;
        if(shiftDelay>0&&!engineAudioProbe)rpm*=.78f;
        engine.pitch=Mathf.Lerp(engine.pitch,rpm,1-Mathf.Exp(-dt*9));
        engine.volume=riding?Mathf.Lerp(.18f,.48f,engineLoad)*Mathf.Lerp(.65f,1,speed01):0;
        idleEngine.pitch=.85f+speed01*.3f;
        idleEngine.volume=riding?.28f*(1-speed01*.9f):0;
        wind.volume=riding?speed01*speed01*.105f:0;
        nitroAudio.volume=Mathf.MoveTowards(nitroAudio.volume,riding&&boosting?.16f:0,dt*.8f);
        // Short engine clips must recover on this frame if a mobile route reset
        // stopped them. The quieter streaming layers only need periodic checks.
        KeepRideLoopPlaying(engine,soundOn&&riding);
        KeepRideLoopPlaying(idleEngine,soundOn&&riding);
        // iOS interruptions / output route changes can stop a streaming source.
        // Retry only once per second rather than restarting an in-flight load.
        if(Time.unscaledTime>=audioRecoveryAt)
        {
            audioRecoveryAt=Time.unscaledTime+1;
            KeepRideLoopPlaying(music,musicOn);
            KeepRideLoopPlaying(wind,soundOn&&riding);
            KeepRideLoopPlaying(nitroAudio,soundOn&&riding&&boosting);
        }
    }

    void KeepRideLoopPlaying(AudioSource source,bool wanted)
    {
        if(source==null||source.clip==null||!wanted||source.isPlaying)return;
        if(!source.enabled)source.enabled=true;
        if(source.clip.loadState==AudioDataLoadState.Unloaded)source.clip.LoadAudioData();
        if(source.clip.loadState==AudioDataLoadState.Loaded)source.Play();
    }

    void PlayRideSound(AudioClip clip)
    {
        if(!soundOn||clip==null||effects==null||rideAudioSuspended||engineAudioProbe)return;
        float volume=clip==clickSound?.48f:clip==hitSound?.95f:clip==passSound?.6f:.8f;
        effects.PlayOneShot(clip,volume);
        if(clip==hitSound)musicDuckUntil=Time.unscaledTime+.25f;
    }

    void SuspendRideAudio()
    {
        rideAudioSuspended=true;
        if(!rideAudioReady)return;
        foreach(var source in new[]{music,outgoingMusic,engine,idleEngine,wind,nitroAudio,effects})if(source!=null)source.Pause();
    }

    void ResumeRideAudio()
    {
        rideAudioSuspended=false;audioRecoveryAt=0;
#if UNITY_IOS && !UNITY_EDITOR
        SRActivateGameAudio();
#endif
        if(!rideAudioReady)return;
        foreach(var source in new[]{music,outgoingMusic,engine,idleEngine,wind,nitroAudio,effects})if(source!=null)source.UnPause();
        // Mute preferences are never reset by recovery or focus changes.
        TickRideAudio(0);
    }

    void RideAudioConfigurationChanged(bool deviceWasChanged){audioRecoveryAt=0;}
    void OnDestroy(){AudioSettings.OnAudioConfigurationChanged-=RideAudioConfigurationChanged;}

    // Explicit QA measures listener output with all non-engine buses muted.
    // Playing flags alone cannot prove audible output on a shipped player.
    IEnumerator CheckEngineAudioOutput()
    {
        bool previousSound=soundOn,previousMusic=musicOn;
        var report=new StringBuilder();
        engineAudioProbe=true;soundOn=true;
        StartRace();countdown=30;hitCooldown=30;
        engineProbeSpeed=0;engineProbeGear=1;engineProbeThrottle=false;
        effects.Stop();TickRideAudio(0);
        var captureAudio=view.gameObject.AddComponent<SpeedRovers.Reborn.RaceAudioCapture>();
        captureAudio.Begin(20);
        try
        {
            yield return MeasureEngineOutput("countdown-idle",1.5f,true,report);
            engineProbeSpeed=60;engineProbeThrottle=true;
            yield return MeasureEngineOutput("throttle-low-speed",2,true,report);
            engineProbeSpeed=TopSpeed;engineProbeGear=6;
            yield return MeasureEngineOutput("top-speed",2,true,report);
            engine.Stop();idleEngine.Stop();
            yield return MeasureEngineOutput("stopped-channels-recover",1,true,report);
            SetPage(Page.Pause);
            yield return MeasureEngineOutput("pause-silent",.8f,false,report);
            SetPage(Page.Riding);SuspendRideAudio();ResumeRideAudio();
            yield return MeasureEngineOutput("resume-motor",1.5f,true,report);
        }
        finally
        {
            captureAudio.Save(System.IO.Path.Combine(captureDir,"engine-only.wav"));
            Destroy(captureAudio);
            engineAudioProbe=false;soundOn=previousSound;musicOn=previousMusic;
            TickRideAudio(.05f);
            System.IO.File.WriteAllText(System.IO.Path.Combine(captureDir,"engine-output.txt"),report.ToString());
            SetPage(Page.Settings);
        }
    }

    IEnumerator MeasureEngineOutput(string name,float seconds,bool expectSound,StringBuilder report)
    {
        yield return new WaitForSeconds(.3f);
        float until=Time.unscaledTime+seconds;
        double energy=0;float peak=0;int count=0;var buffer=new float[1024];
        float engineRms=0,idleRms=0;
        while(Time.unscaledTime<until)
        {
            AudioListener.GetOutputData(buffer,0);
            foreach(float sample in buffer){energy+=sample*sample;peak=Mathf.Max(peak,Mathf.Abs(sample));count++;}
            engine.GetOutputData(buffer,0);float v=0;foreach(float s in buffer)v+=s*s;engineRms=Mathf.Max(engineRms,Mathf.Sqrt(v/buffer.Length));
            idleEngine.GetOutputData(buffer,0);v=0;foreach(float s in buffer)v+=s*s;idleRms=Mathf.Max(idleRms,Mathf.Sqrt(v/buffer.Length));
            yield return null;
        }
        float rms=count>0?Mathf.Sqrt((float)(energy/count)):0;
        string line=name+" listener_rms="+rms.ToString("F5")+" peak="+peak.ToString("F5")+
            " engine_rms_max="+engineRms.ToString("F5")+" idle_rms_max="+idleRms.ToString("F5")+
            " loaded="+engine.clip.loadState+" engine_playing="+engine.isPlaying+" idle_playing="+idleEngine.isPlaying;
        report.AppendLine(line);Debug.Log("ENGINE QA: "+line);
        Check(expectSound?rms>.012f&&peak>.035f:rms<.002f,name+" actual listener output "+(expectSound?"contains engine audio":"is silent"));
        Check(peak<.98f,name+" listener output retains clipping headroom");
    }
}
