using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class TrafficRidePrototype
{
    float soakLane=1.85f;
    float SoakSteer()
    {
        float best=float.NegativeInfinity;
        foreach(float lane in new[]{1.85f,5.4f,-1.85f}){
            float score=lane==1.85f?4:lane==5.4f?2:-3;
            if(Mathf.Abs(lane-soakLane)<.1f)score+=2;
            foreach(var p in pickups)if(!p.taken&&p.at-distance>0&&p.at-distance<250&&Mathf.Abs(p.x-lane)<1&&fuel<.8f)score+=12;
            foreach(var c in cars)if(c.position.z> -6&&c.position.z<95&&Mathf.Abs(c.position.x-lane)<1.8f)score-=20;
            foreach(var h in hazards)if(h.position.z> -4&&h.position.z<90&&Mathf.Abs(h.position.x-lane)<1.5f)score-=20;
            foreach(var r in racers)if(Mathf.Abs(r.progress-distance)<20&&Mathf.Abs(r.x-lane)<.8f)score-=10;
            if(score>best){best=score;soakLane=lane;}
        }
        return Mathf.Clamp((soakLane-PlayerX)*1.6f,-1,1);
    }
    IEnumerator FullRaceSoak()
    {
        System.IO.Directory.CreateDirectory(captureDir);yield return new WaitForSeconds(2);
        StartRace();countdown=0;float start=Time.realtimeSinceStartup,maxSpeed=0,minHealth=1,minFuel=1,nearFrames=0;
        int frames=0,slowFrames=0;var sections=new HashSet<string>();
        while(page==Page.Riding&&Time.realtimeSinceStartup-start<420){
            frames++;if(Time.unscaledDeltaTime>.0334f)slowFrames++;
            maxSpeed=Mathf.Max(maxSpeed,speed);minHealth=Mathf.Min(minHealth,health);minFuel=Mathf.Min(minFuel,fuel);sections.Add(district);
            if(racers.Exists(r=>Mathf.Abs(r.progress-distance)<220))nearFrames++;
            yield return null;
        }
        float wall=Time.realtimeSinceStartup-start;
        string result="Outcome: "+resultHeading+"\nDistance: "+distance+" / "+finishDistance+" m\nRace seconds: "+elapsed+"\nWall seconds: "+wall+
            "\nAverage FPS: "+frames/Mathf.Max(1,wall)+"\nFrames over 33.4 ms: "+slowFrames+" / "+frames+"\nPeak km/h: "+maxSpeed+
            "\nMinimum bike health: "+minHealth+"\nMinimum fuel: "+minFuel+"\nFinish fuel: "+fuel+"\nFuel stops collected: "+fuelStopsTaken+"\nSections visited: "+sections.Count+"\nSection names: "+string.Join(", ",sections)+"\nNearby rival fraction: "+nearFrames/Mathf.Max(1,frames);
        bool success=distance>=finishDistance&&sections.Count==8&&elapsed>=180&&elapsed<=300&&maxSpeed>=TopSpeed-.1f;
        result+="\nSOAK "+(success?"PASS":"FAIL");Debug.Log(result);
        System.IO.File.WriteAllText(System.IO.Path.Combine(captureDir,"soak.txt"),result);yield return Shot("race-finish");Application.Quit(success?0:1);
    }
}
