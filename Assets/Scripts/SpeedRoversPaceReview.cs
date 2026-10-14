using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

namespace SpeedRovers.Reborn
{
    public sealed partial class SpeedRoversGame
    {
        IEnumerator CapturePaceReview(string directory)
        {
            verificationRun=captureHold=true;
            Directory.CreateDirectory(directory);
            yield return new WaitForSeconds(1);
            StartRun();things.Clear();spawnDistance=100000;nextWork=nextPolice=100000;
            distance=100;checkpoints=0;bannerUntil=0;timeLeft=120;
            double started=Time.realtimeSinceStartupAsDouble,last=started;
            float origin=distance;int frames=0;
            var csv=new StringBuilder("wall_seconds,route_metres,speed_kmh,gear,rpm,frame_seconds,event\n");
            bool bumped=false;int nextCapture=0;
            while(Time.realtimeSinceStartupAsDouble-started<34)
            {
                yield return new WaitForEndOfFrame();
                double now=Time.realtimeSinceStartupAsDouble;
                float dt=(float)(now-last);last=now;
                string action="";
                if(!bumped && now-started>=16)
                {
                    things.Add(new Thing{kind=ThingKind.Rival,distance=distance+2,lane=0,variant=1,travelSpeed=speed/3.6f});
                    bumped=true;action="bike contact";
                }
                AdvanceElapsedTime(dt);frames++;
                csv.AppendLine((now-started).ToString("F5")+","+(distance-origin).ToString("F3")+","+speed.ToString("F2")+","+gear+","+engineRpm.ToString("F1")+","+dt.ToString("F5")+","+action);
                if(now-started>=nextCapture)
                {
                    ScreenCapture.CaptureScreenshot(Path.Combine(directory,"drive-"+nextCapture+".png"));
                    nextCapture+=4;
                }
            }
            double elapsed=Time.realtimeSinceStartupAsDouble-started;
            File.WriteAllText(Path.Combine(directory,"pace.csv"),csv.ToString());
            File.WriteAllText(Path.Combine(directory,"summary.txt"),"Visible native player, real elapsed time, scenery rendering with traffic spawning disabled.\nSeconds="+elapsed+"\nFrames="+frames+"\nMean FPS="+(frames/elapsed)+"\nRoute metres="+(distance-origin)+"\nRoute metres per real second="+((distance-origin)/elapsed));
            Debug.Log("PACE REVIEW: "+frames+" frames in "+elapsed+" real seconds; "+(distance-origin)+" route metres.");
            mode=ScreenMode.Running;
            for(int variant=0;variant<12;variant++)
            {
                distance=variant%2==0?100:800;animationTime=10;things.Clear();
                for(int side=-1;side<=1;side++)things.Add(new Thing{kind=ThingKind.Traffic,distance=distance+50+Mathf.Abs(side)*40,lane=side,variant=variant,travelSpeed=30});
                yield return new WaitForSeconds(.1f);
                ScreenCapture.CaptureScreenshot(Path.Combine(directory,"traffic-"+variant+".png"));
                yield return new WaitForSeconds(.1f);
            }
            yield return new WaitForSeconds(.3f);
            Application.Quit();
        }
    }
}
