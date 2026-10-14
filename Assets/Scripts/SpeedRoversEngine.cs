using UnityEngine;

namespace SpeedRovers.Reborn
{
    public sealed partial class SpeedRoversGame
    {
        AudioSource engineAudio, coastAudio, windAudio, sirenAudio, passAudio, impactAudio;
        AudioClip engineClip, windClip;
        float engineRpm=1200, shiftRemaining, wheelTurns, impactRecovery;
        int gear=1;
        bool braking, audioReview;
        static readonly float[] GearTop={0,90,150,217.5f,292.5f,367.5f,480};
        static readonly float[] GearAcceleration={0,60,49,41,35,29,23};

        void AdvanceDrive(float dt)
        {
            impactRecovery=Mathf.Max(0,impactRecovery-dt);
            float target=braking?45:(easyMode?337.5f:427.5f)+Mathf.Min(checkpoints*3,37.5f);
            float acceleration=braking?180:GearAcceleration[gear]*(shiftRemaining>0?.18f:1)*(impactRecovery>0?.28f:1);
            speed=Mathf.MoveTowards(speed,target,dt*acceleration);
        }

        void ImpactDownshift()
        {
            while(gear>1 && speed<GearTop[gear-1]*.95f)gear--;
            shiftRemaining=.34f;
            engineRpm=Mathf.Min(engineRpm,RpmForSpeed(speed,gear)*.72f);
            impactRecovery=.85f;
        }

        AudioSource Loop(string resource)
        {
            var source=gameObject.AddComponent<AudioSource>();
            source.clip=Resources.Load<AudioClip>("Audio/Rush/"+resource);
            source.loop=true;source.playOnAwake=false;source.volume=0;
            return source;
        }
        void InitRushAudio()
        {
            engineAudio=Loop("engine-load");coastAudio=Loop("engine-coast");
            windAudio=Loop("wind");sirenAudio=Loop("police-wail");
            passAudio=Loop("pass-by");passAudio.loop=false;
            impactAudio=Loop("impact");impactAudio.loop=false;impactAudio.volume=.45f;
            engineClip=engineAudio.clip;windClip=windAudio.clip;
        }
        void ResetEngine(float initialSpeed)
        {
            gear=1;
            while(gear<6 && initialSpeed>GearTop[gear])gear++;
            engineRpm=RpmForSpeed(initialSpeed,gear);shiftRemaining=0;braking=false;wheelTurns=0;
        }
        static float RpmForSpeed(float kmh,int currentGear) => Mathf.Clamp(1800+kmh/GearTop[currentGear]*8600,1100,10800);
        void AdvanceEngine(float dt,bool running)
        {
            shiftRemaining=Mathf.Max(0,shiftRemaining-dt);
            if(running)
            {
                if(gear<6 && speed>GearTop[gear] && shiftRemaining<=0) {gear++;shiftRemaining=.30f;}
                else if(gear>1 && speed<GearTop[gear-1]*.82f && shiftRemaining<=0) {gear--;shiftRemaining=.24f;}
            }
            float wanted=running?RpmForSpeed(speed,gear):1000;
            if(shiftRemaining>0)wanted*=.77f;
            engineRpm=Mathf.MoveTowards(engineRpm,wanted,dt*(running?19000:15000));
        }
        void UpdateRushAudio()
        {
            if(engineAudio==null)return;
            bool racing=mode==ScreenMode.Running;
            bool tail=mode==ScreenMode.Results && engineRpm>1100;
            if(!soundEnabled || (verificationRun && !audioReview) || (!racing && !tail))
            {
                engineAudio.Pause();coastAudio.Pause();windAudio.Pause();sirenAudio.Pause();
                if(mode==ScreenMode.Paused || !soundEnabled) {passAudio.Pause();impactAudio.Pause();}
                return;
            }
            if(tail)AdvanceEngine(Time.deltaTime,false);
            foreach(var source in new[]{engineAudio,coastAudio,windAudio,sirenAudio})
                if(!source.isPlaying) {source.UnPause();if(!source.isPlaying)source.Play();}
            float throttle=racing&&!braking?1:0;
            float shiftCut=shiftRemaining>0?.38f:1;
            float tailGain=tail?Mathf.InverseLerp(1000,9000,engineRpm):1;
            engineAudio.pitch=coastAudio.pitch=engineRpm/4500;
            engineAudio.volume=Mathf.Lerp(engineAudio.volume,(.08f+throttle*.15f)*shiftCut*tailGain,1-Mathf.Exp(-Time.deltaTime*18));
            coastAudio.volume=(.05f+(1-throttle)*.09f)*tailGain;
            windAudio.volume=Mathf.InverseLerp(60,300,speed)*.20f;
            Thing nearest=null;float near=180;
            foreach(Thing t in things)
            {
                float z=t.distance-distance;
                if(t.police && Mathf.Abs(z)<near) {near=Mathf.Abs(z);nearest=t;}
            }
            float loud=nearest==null?0:Mathf.Pow(1-near/180,2);
            sirenAudio.volume=Mathf.MoveTowards(sirenAudio.volume,loud*.32f,Time.deltaTime*2);
            if(nearest!=null)
            {
                sirenAudio.panStereo=Mathf.Clamp((nearest.lane-lane)*.7f,-.85f,.85f);
                sirenAudio.pitch=nearest.distance>distance?1.13f:.80f;
            }
        }
        void PlayPass(Thing vehicle)
        {
            if(verificationRun || !soundEnabled || passAudio==null)return;
            passAudio.panStereo=Mathf.Clamp((vehicle.lane+vehicle.lateral-lane)*.8f,-1,1);
            passAudio.pitch=vehicle.kind==ThingKind.Oncoming?1.25f:.85f;
            passAudio.volume=vehicle.kind==ThingKind.Rival?.12f:.26f;
            passAudio.Play();
        }
        void PlayCrash()
        {
            if((!verificationRun || audioReview) && soundEnabled && impactAudio!=null)impactAudio.Play();
        }

        System.Collections.IEnumerator CaptureAudioReview(string directory)
        {
            System.IO.Directory.CreateDirectory(directory);
            verificationRun=captureHold=audioReview=true;
            soundEnabled=musicEnabled=true;
            var recorder=GetComponentInChildren<AudioListener>().gameObject.AddComponent<RaceAudioCapture>();
            yield return new WaitForSeconds(.5f);
            StartRun();things.Clear();speed=65;ResetEngine(65);bannerUntil=0;
            recorder.Begin(24);
            float elapsed=0;
            var telemetry=new System.Text.StringBuilder("seconds,speed,gear,rpm,siren\n");
            var police=new Thing{distance=500,lane=-1,kind=ThingKind.Oncoming,variant=4,police=true,travelSpeed=-28};
            things.Add(police);
            while(elapsed<18)
            {
                float dt=Time.deltaTime;
                elapsed+=dt;animationTime+=dt;
                braking=elapsed>8 && elapsed<10;
                AdvanceDrive(dt);
                AdvanceEngine(dt,true);
                distance+=speed/3.6f*dt;wheelTurns+=speed/3.6f/2.05f*dt;
                police.distance=distance+(6-elapsed)*70;
                telemetry.AppendLine(elapsed.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+","+speed+","+gear+","+engineRpm+","+sirenAudio.volume);
                yield return null;
            }
            PlayCrash();mode=ScreenMode.Results;speed=0;
            yield return new WaitForSeconds(1.5f);
            mode=ScreenMode.Paused;
            yield return new WaitForSeconds(.6f);
            recorder.Save(System.IO.Path.Combine(directory,"race-mix.wav"));
            System.IO.File.WriteAllText(System.IO.Path.Combine(directory,"audio-telemetry.csv"),telemetry.ToString());
            Application.Quit(0);
        }
    }
}
