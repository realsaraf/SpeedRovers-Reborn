using UnityEngine;

namespace SpeedRovers.Reborn
{
    public sealed partial class SpeedRoversGame
    {
        static bool IsVehicle(Thing t) => t.kind==ThingKind.Rival || t.kind==ThingKind.Traffic || t.kind==ThingKind.Oncoming;

        bool MergeGap(Thing moving,int candidate)
        {
            if(Mathf.Abs(distance-moving.distance)<38 && Mathf.Abs(lane-candidate)<.7f)return false;
            foreach(Thing other in things)
            {
                if(other==moving || other.used || !IsVehicle(other))continue;
                float clearance=other.travelSpeed*moving.travelSpeed<0?110:45;
                if(Mathf.Abs(other.distance-moving.distance)<clearance && Mathf.Abs(other.lane+other.lateral-candidate)<.8f)return false;
            }
            return true;
        }

        void AdvanceTraffic(Thing t,float dt)
        {
            if(!IsVehicle(t))return;
            if(t.kind==ThingKind.Rival && t.finishTime>=0)return;
            if(!t.trafficReady) {t.trafficReady=true;t.cruiseSpeed=t.travelSpeed;}
            float direction=t.cruiseSpeed<0?-1:1;
            float actualLane=t.lane+t.lateral;
            float targetSpeed=Mathf.Abs(t.cruiseSpeed);
            if(t.kind==ThingKind.Rival)
            {
                float top=(easyMode?337.5f:427.5f)+Mathf.Min(checkpoints*3,37.5f);
                float gap=t.distance-distance;
                float temperament=.955f+(t.variant%3)*.025f;
                float pace=temperament+Mathf.Clamp(-gap/3500,-.075f,.03f)+.008f*Mathf.Sin(animationTime*.25f+t.variant);
                targetSpeed=top*pace/3.6f;
                if(animationTime>=t.nextRaceDecision)
                {
                    bool passing=gap<0 && gap> -90 && Mathf.Abs(actualLane-lane)<.65f;
                    foreach(Thing other in things)
                        if(other!=t && !other.used && IsVehicle(other) && other.distance>t.distance && other.distance<t.distance+150 && Mathf.Abs(other.lane+other.lateral-actualLane)<.65f && other.travelSpeed<t.travelSpeed-2)passing=true;
                    if(passing)
                    {
                        for(int offset=-1;offset<=1;offset+=2)
                        {
                            int candidate=t.lane+offset;
                            if(candidate< -1 || candidate>1 || !MergeGap(t,candidate))continue;
                            int work=Mathf.Max(0,RoadRoute.WorkIndex(t.distance+150));
                            if(RoadRoute.WorkLane(work)==candidate && RoadRoute.WorkStart(work)-t.distance<220)continue;
                            t.lateral+=t.lane-candidate;t.lane=candidate;break;
                        }
                    }
                    t.nextRaceDecision=animationTime+1.2f;
                }
            }
            float lookahead=Mathf.Max(150,targetSpeed*RoadProjection.TravelScale*3.5f);
            float stopAt=direction>0?float.PositiveInfinity:float.NegativeInfinity;
            int first=Mathf.Max(0,RoadRoute.WorkIndex(t.distance-lookahead)-1);
            int last=RoadRoute.WorkIndex(t.distance+lookahead)+1;
            for(int work=first;work<=last;work++)
            {
                float start=RoadRoute.WorkStart(work)-35,end=RoadRoute.WorkStart(work)+44;
                float gap=(direction>0?start-t.distance:t.distance-end);
                if(gap< -79 || gap>lookahead)continue;
                int blocked=RoadRoute.WorkLane(work);
                if(t.lane==blocked)
                {
                    int chosen=t.lane;
                    // Merge toward the centre first; never jump across a lane.
                    int candidate=t.lane==0?(blocked==1?-1:1):0;
                    if(candidate!=blocked && MergeGap(t,candidate))chosen=candidate;
                    if(chosen!=t.lane) {t.lateral+=t.lane-chosen;t.lane=chosen;}
                }
                if(Mathf.Abs(actualLane-blocked)<.70f)
                {
                    targetSpeed=Mathf.Min(targetSpeed,Mathf.Max(0,gap-8)/RoadProjection.TravelScale/1.4f);
                    if(direction>0)stopAt=Mathf.Min(stopAt,start-5);
                    else stopAt=Mathf.Max(stopAt,end+5);
                }
            }
            // Following cars yield instead of passing through one another.
            foreach(Thing other in things)
            {
                if(other==t || other.used || !IsVehicle(other))continue;
                float gap=(other.distance-t.distance)*direction;
                if(gap>0 && gap<65 && Mathf.Abs(other.lane+other.lateral-actualLane)<.65f)
                    targetSpeed=Mathf.Min(targetSpeed,Mathf.Max(0,other.travelSpeed*direction+(gap-18)/RoadProjection.TravelScale/1.2f));
            }
            float weave=t.kind==ThingKind.Rival?.04f*Mathf.Sin(animationTime*.8f+t.variant):0;
            t.lateral=Mathf.MoveTowards(t.lateral,weave,dt*.85f);
            t.lean=Mathf.Lerp(t.lean,(t.lane+t.lateral-actualLane)/Mathf.Max(.001f,dt)*.7f,1-Mathf.Exp(-dt*9));
            float acceleration=9;
            if(t.kind==ThingKind.Rival)
            {
                int rivalGear=1;while(rivalGear<6 && t.travelSpeed*3.6f>GearTop[rivalGear])rivalGear++;
                acceleration=GearAcceleration[rivalGear]/3.6f*(animationTime<t.contactAfter?.35f:1);
            }
            t.travelSpeed=direction*Mathf.MoveTowards(Mathf.Abs(t.travelSpeed),targetSpeed,dt*(targetSpeed<Mathf.Abs(t.travelSpeed)?45:acceleration));
            float next=t.distance+t.travelSpeed*RoadProjection.TravelScale*dt;
            if(direction>0 && t.distance<=stopAt && next>stopAt) {next=stopAt;t.travelSpeed=0;}
            if(direction<0 && t.distance>=stopAt && next<stopAt) {next=stopAt;t.travelSpeed=0;}
            if(t.kind==ThingKind.Rival && next>=RoadRoute.Length)
            {
                t.finishTime=animationTime-dt+dt*Mathf.Clamp01((RoadRoute.Length-t.distance)/Mathf.Max(.0001f,next-t.distance));
                next=RoadRoute.Length;
                t.travelSpeed=0;
            }
            t.rollingPhase+=Mathf.Abs(next-t.distance)/RoadProjection.TravelScale/2.05f;
            t.distance=next;
        }
    }
}
