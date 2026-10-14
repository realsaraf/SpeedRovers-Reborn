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
            distance=700;speed=285;ResetEngine(speed);checkpoints=1;bannerUntil=0;
            double started=Time.realtimeSinceStartupAsDouble,last=started;
            float origin=distance;int frames=0;
            var csv=new StringBuilder("wall_seconds,route_metres,speed_kmh,frame_seconds\n");
            while(Time.realtimeSinceStartupAsDouble-started<12)
            {
                yield return new WaitForEndOfFrame();
                double now=Time.realtimeSinceStartupAsDouble;
                float dt=(float)(now-last);last=now;
                AdvanceElapsedTime(dt);frames++;
                csv.AppendLine((now-started).ToString("F5")+","+(distance-origin).ToString("F3")+","+speed.ToString("F2")+","+dt.ToString("F5"));
            }
            double elapsed=Time.realtimeSinceStartupAsDouble-started;
            File.WriteAllText(Path.Combine(directory,"pace.csv"),csv.ToString());
            File.WriteAllText(Path.Combine(directory,"summary.txt"),"Visible native player, real elapsed time, scenery rendering with traffic spawning disabled.\nSeconds="+elapsed+"\nFrames="+frames+"\nMean FPS="+(frames/elapsed)+"\nRoute metres="+(distance-origin)+"\nRoute metres per real second="+((distance-origin)/elapsed));
            Debug.Log("PACE REVIEW: "+frames+" frames in "+elapsed+" real seconds; "+(distance-origin)+" route metres.");
            Application.Quit();
        }
    }
}
