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
            if(!t.trafficReady) {t.trafficReady=true;t.cruiseSpeed=t.travelSpeed;}
            float direction=t.cruiseSpeed<0?-1:1;
            float actualLane=t.lane+t.lateral;
            float targetSpeed=Mathf.Abs(t.cruiseSpeed);
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
                    targetSpeed=Mathf.Min(targetSpeed,Mathf.Max(0,(gap-15)/RoadProjection.TravelScale/1.2f));
            }
            float weave=t.kind==ThingKind.Rival?.04f*Mathf.Sin(animationTime*.8f+t.variant):0;
            t.lateral=Mathf.MoveTowards(t.lateral,weave,dt*.85f);
            t.lean=Mathf.Lerp(t.lean,(t.lane+t.lateral-actualLane)/Mathf.Max(.001f,dt)*.7f,1-Mathf.Exp(-dt*9));
            t.travelSpeed=direction*Mathf.MoveTowards(Mathf.Abs(t.travelSpeed),targetSpeed,dt*(targetSpeed<Mathf.Abs(t.travelSpeed)?45:9));
            float next=t.distance+t.travelSpeed*RoadProjection.TravelScale*dt;
            if(direction>0 && t.distance<=stopAt && next>stopAt) {next=stopAt;t.travelSpeed=0;}
            if(direction<0 && t.distance>=stopAt && next<stopAt) {next=stopAt;t.travelSpeed=0;}
            t.distance=next;
        }
    }
}
