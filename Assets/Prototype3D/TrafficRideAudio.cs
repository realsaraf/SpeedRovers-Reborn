using UnityEngine;
using System.Runtime.InteropServices;

public partial class TrafficRidePrototype
{
    AudioSource outgoingMusic, idleEngine, nitroAudio;
    AudioClip menuMusicClip, raceMusicClip, shiftSound;
    bool rideAudioReady, rideAudioSuspended;
    float audioRecoveryAt, musicDuckUntil, engineLoad;

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
        if(engine==null)engine=RideAudioSource("Recorded engine under load",true,32);
        engine.Stop();engine.loop=true;engine.spatialBlend=0;engine.dopplerLevel=0;engine.priority=32;engine.volume=0;
        engine.clip=Resources.Load<AudioClip>("GrandTour/Audio/Engine");
        idleEngine.clip=Resources.Load<AudioClip>("GrandTour/Audio/EngineIdle");
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
            music.mute=!musicOn;
            if(!rideAudioSuspended)music.Play();
        }
        audioRecoveryAt=0;
    }

    void TickRideAudio(float dt)
    {
        if(!rideAudioReady||rideAudioSuspended)return;
        music.mute=outgoingMusic.mute=!musicOn;
        effects.mute=engine.mute=idleEngine.mute=wind.mute=nitroAudio.mute=!soundOn;
        bool riding=page==Page.Riding;
        float speed01=Mathf.Clamp01(speed/TopSpeed);
        float musicTarget=page==Page.Pause?.28f:riding?.48f:.44f;
        if(Time.unscaledTime<musicDuckUntil)musicTarget*=.76f;
        music.volume=Mathf.MoveTowards(music.volume,musicTarget,dt*.65f);
        outgoingMusic.volume=Mathf.MoveTowards(outgoingMusic.volume,0,dt*.65f);
        if(outgoingMusic.volume<=0&&outgoingMusic.isPlaying)outgoingMusic.Stop();
        engineLoad=Mathf.MoveTowards(engineLoad,riding&&throttle&&!brake?1:.2f,dt*4);
        float gearProgress=Mathf.Clamp01((speed-(gear-1)*85)/85);
        float rpm=.9f+gearProgress*.7f+engineLoad*.1f;
        if(shiftDelay>0)rpm*=.78f;
        engine.pitch=Mathf.Lerp(engine.pitch,rpm,1-Mathf.Exp(-dt*9));
        engine.volume=riding?Mathf.Lerp(.06f,.24f,engineLoad)*Mathf.Lerp(.3f,1,speed01):0;
        idleEngine.pitch=.85f+speed01*.3f;
        idleEngine.volume=riding?.14f*(1-speed01*.9f):0;
        wind.volume=riding?speed01*speed01*.105f:0;
        nitroAudio.volume=Mathf.MoveTowards(nitroAudio.volume,riding&&boosting?.16f:0,dt*.8f);
        // iOS interruptions / output route changes can stop a streaming source.
        // Retry only once per second rather than restarting an in-flight load.
        if(Time.unscaledTime>=audioRecoveryAt)
        {
            audioRecoveryAt=Time.unscaledTime+1;
            KeepRideLoopPlaying(music,musicOn);
            KeepRideLoopPlaying(engine,soundOn&&riding);
            KeepRideLoopPlaying(idleEngine,soundOn&&riding);
            KeepRideLoopPlaying(wind,soundOn&&riding);
            KeepRideLoopPlaying(nitroAudio,soundOn&&riding&&boosting);
        }
    }

    void KeepRideLoopPlaying(AudioSource source,bool wanted)
    {
        if(source==null||source.clip==null||!wanted||source.isPlaying)return;
        if(source.clip.loadState==AudioDataLoadState.Unloaded)source.clip.LoadAudioData();
        if(source.clip.loadState!=AudioDataLoadState.Failed)source.Play();
    }

    void PlayRideSound(AudioClip clip)
    {
        if(!soundOn||clip==null||effects==null||rideAudioSuspended)return;
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
}
