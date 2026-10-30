using System.Collections.Generic;
using UnityEngine;

// The ten existing participants leave through the real rear opening. No
// replacement crowd, teleport, timed disappearance or hidden completion count.
public sealed partial class Chapter3SchoolAmbient
{
    readonly Vector3[][] withdrawalRoutes=new Vector3[AmbusherCount][];
    readonly int[] withdrawalLeg=new int[AmbusherCount];
    readonly Chapter3PathMotor[] withdrawalMotors=new Chapter3PathMotor[AmbusherCount];
    readonly Vector3[] withdrawalHomes=new Vector3[AmbusherCount];
    readonly Quaternion[] withdrawalRotations=new Quaternion[AmbusherCount];
    // A route coordinate, independent of camera yaw, keeps the player ahead
    // through the two right-angle corners as well as on straight ground.
    static readonly Vector3[] withdrawalSpine={new Vector3(13,0,33),new Vector3(17,0,34),new Vector3(18,0,44.8f),new Vector3(24.2f,0,45.4f),new Vector3(24.2f,0,61),new Vector3(22,0,66),new Vector3(12,0,70),new Vector3(0,0,76)};
    public static float WithdrawalProgress(Vector3 point)
    {
        float best=float.PositiveInfinity,result=0,length=0;
        for(int i=1;i<withdrawalSpine.Length;i++)
        {
            Vector3 a=withdrawalSpine[i-1],d=withdrawalSpine[i]-a;point.y=0;
            float t=Mathf.Clamp01(Vector3.Dot(point-a,d)/d.sqrMagnitude);
            float distance=(point-a-d*t).sqrMagnitude;
            if(distance<best){best=distance;result=length+d.magnitude*t;}
            length+=d.magnitude;
        }
        return result;
    }
    static Vector3 WithdrawalSample(float progress)
    {
        for(int i=1;i<withdrawalSpine.Length;i++)
        {
            float length=Horizontal(withdrawalSpine[i-1],withdrawalSpine[i]);
            if(progress<=length)return Vector3.Lerp(withdrawalSpine[i-1],withdrawalSpine[i],Mathf.Clamp01(progress/length));
            progress-=length;
        }
        return withdrawalSpine[withdrawalSpine.Length-1];
    }
    public bool PlayerAtWithdrawalAssembly=>withdrawalPlayer&&Horizontal(withdrawalPlayer.position,new Vector3(0,0,78))<4;
    public bool CanFollowWithdrawal(Vector3 current,ref Vector3 target,float gap)
    {
        if(PlayerAtWithdrawalAssembly)return true;
        float limit=WithdrawalProgress(withdrawalPlayer.position)-gap;
        float progress=WithdrawalProgress(current);
        if(limit<=0||progress>=limit-.15f)return false;
        if(WithdrawalProgress(target)>limit)target=WithdrawalSample(limit);
        return true;
    }
    Transform withdrawalPlayer;
    public bool Withdrawing {get;private set;}
    public int WithdrawalSignalFrame {get;private set;}=-1;
    public Vector3 WithdrawalDestination(int index)=>new Vector3(-4.4f+(index%5)*2.2f,0,72.5f+(index/5)*2.2f);
    public int WithdrawalArrivedCount
    {
        get
        {
            int count=0;if(!Withdrawing)return count;
            for(int i=0;i<AmbusherCount;i++)
                if(assailants[i]&&assailants[i].gameObject.activeInHierarchy&&withdrawalRoutes[i]!=null
                    &&withdrawalLeg[i]>=withdrawalRoutes[i].Length&&Horizontal(assailants[i].transform.position,WithdrawalDestination(i))<.6f)count++;
            return count;
        }
    }
    public bool WithdrawalArrived=>Withdrawing&&WithdrawalArrivedCount==AmbusherCount;
    public bool WithdrawalNeedsCatchup(Vector3 leader)
    {
        if(!Withdrawing)return false;
        foreach(var actor in assailants)
            if(actor&&actor.transform.position.z<68&&Horizontal(actor.transform.position,leader)>22)return true;
        return false;
    }
    public bool BeginWithdrawal(Transform player)
    {
        if(!Panicking||Withdrawing||!player)return false;
        foreach(var actor in assailants)if(!actor||!actor.gameObject.activeInHierarchy)return false;
        EndSkirmish();HideStraggler();withdrawalPlayer=player;Withdrawing=true;WithdrawalSignalFrame=Time.frameCount;
        for(int i=0;i<AmbusherCount;i++)
        {
            var actor=assailants[i];withdrawalHomes[i]=actor.transform.position;withdrawalRotations[i]=actor.transform.rotation;
            actor.FollowingWithdrawal=true;
            if(withdrawalMotors[i]==null)withdrawalMotors[i]=new Chapter3PathMotor();
            withdrawalMotors[i].Reset();
            actor.HoldAtNeck(null,false);actor.SetStrike(0);actor.SetMeleePose(0,0);actor.SetStartled(0);actor.SetAfraid(false);actor.SetWaiting(false);
            withdrawalRoutes[i]=BuildWithdrawalRoute(actor.transform.position,i);withdrawalLeg[i]=0;
        }
        // Releasing the two holds must not leave their former partners frozen
        // in a restraint pose. Any unfinished encounter also becomes flight.
        for(int i=0;i<crowd.Length;i++)if(crowd[i]&&crowd[i].gameObject.activeInHierarchy&&fallBegan[i]<0)
        {crowdPhases[i]=CrowdPhase.Fleeing;crowd[i].SetAfraid(true);crowd[i].SetStartled(0);if(i<14)escapeLeg[i]=0;}
        return true;
    }
    static Vector3[] BuildWithdrawalRoute(Vector3 start,int index)
    {
        var route=new List<Vector3>();
        if(start.x<4)
        {
            if(start.z<19)route.Add(new Vector3(-10,0,20));
            if(start.z<27)route.Add(new Vector3(-7,0,28));
            route.Add(new Vector3(-6,0,35));route.Add(new Vector3(2,0,35));route.Add(new Vector3(8,0,33));
        }
        else
        {
            if(start.z<18)route.Add(new Vector3(12,0,20));
            if(start.z<26)route.Add(new Vector3(14,0,27));
        }
        // Queue on the verified open centre line at the two narrow corners.
        // Side-by-side offsets put the outside lane against the solid fence.
        route.Add(new Vector3(17,0,34));route.Add(new Vector3(18,0,44.8f));
        route.Add(new Vector3(24.2f,0,45.4f));route.Add(new Vector3(24.2f,0,61));
        route.Add(new Vector3(22,0,66));route.Add(new Vector3(12,0,70));
        float x=-4.4f+(index%5)*2.2f;
        route.Add(new Vector3(x,0,69.5f));route.Add(new Vector3(x,0,72.5f+(index/5)*2.2f));
        return route.ToArray();
    }
    public void CancelWithdrawal(bool restorePositions=false)
    {
        if(Withdrawing&&restorePositions)
            for(int i=0;i<AmbusherCount;i++)if(assailants[i])
            {
                assailants[i].ResetPose();assailants[i].transform.SetPositionAndRotation(withdrawalHomes[i],withdrawalRotations[i]);
                assailants[i].SetAnimated(true);
            }
        Withdrawing=false;WithdrawalSignalFrame=-1;withdrawalPlayer=null;
        for(int i=0;i<AmbusherCount;i++){if(assailants[i])assailants[i].FollowingWithdrawal=false;withdrawalMotors[i]?.Reset();withdrawalRoutes[i]=null;withdrawalLeg[i]=0;}
    }
    void UpdateWithdrawal()
    {
        if(!Withdrawing||!withdrawalPlayer)return;
        var school=GetComponentInParent<Chapter3SchoolController>();
        if(school&&school.Battle&&school.Battle.CurrentStage==Chapter3SchoolBattle.Stage.Down)return;
        for(int i=0;i<AmbusherCount;i++)
        {
            var actor=assailants[i];var route=withdrawalRoutes[i];if(!actor||route==null)continue;
            if(withdrawalLeg[i]>=route.Length)
            {actor.SetWaiting(false);actor.Face(withdrawalPlayer.position,65);continue;}
            Vector3 current=actor.transform.position,target=route[withdrawalLeg[i]],toPlayer=withdrawalPlayer.position-current;
            // A fleeing guest can stop on an authored corner. A* cannot
            // solve an occupied goal, even when the aisle beside it is open.
            // Pick a nearby clear corner and retain it in this actor's route.
            // The last assembly slot is never moved or counted early.
            if(withdrawalLeg[i]<route.Length-1)
            {
                Vector3 corner=OpenWithdrawalCorner(target,route[withdrawalLeg[i]+1]);
                if((corner-target).sqrMagnitude>.001f)
                {target=route[withdrawalLeg[i]]=corner;withdrawalMotors[i].Reset();}
            }
            // Consume an intermediate corner before yielding to neighbours:
            // nobody needs to stand on its exact centre to turn through it.
            // The longer campus battle brings both flanks here together.
            // Consume the open gathering point before two adjacent arrivals
            // yield to each other while trying to stand on its exact centre.
            // Tight fence corners and final slots keep their original tolerance.
            bool fieldMerge=Horizontal(target,new Vector3(17,0,34))<.01f;
            float arrival=withdrawalLeg[i]==route.Length-1?.2f:fieldMerge?1.2f:.5f;
            // A fighter approaching from the east can already be beyond the
            // open merge point. Do not send him south into the northbound queue.
            bool passedMerge=fieldMerge&&current.z>=34&&current.x>=15&&current.x<=19.5f;
            if(Horizontal(current,target)<arrival||passedMerge)
            {
                withdrawalLeg[i]++;
                // A settled participant is now a standing obstacle. Later
                // arrivals must plan around them to reach the rear row.
                if(withdrawalLeg[i]>=route.Length)actor.FollowingWithdrawal=false;
                continue;
            }
            // Join the exit from the authored safe aisle, then follow behind
            // the player. Never wait for a leader who is waiting for this crowd.
            bool onSpine=target.x>=17||target.z>=44;
            if(onSpine&&!CanFollowWithdrawal(current,ref target,3.4f)){actor.SetWaiting(false);continue;}
            actor.SetWaiting(false);
            withdrawalMotors[i].Move(actor,target,3.2f);
        }
    }
    public static Vector3 OpenWithdrawalCorner(Vector3 target,Vector3 next)
    {
        if(Chapter3PathMotor.Clear(target,target))return target;
        Vector3 forward=Vector3.ProjectOnPlane(next-target,Vector3.up).normalized;
        foreach(float radius in new[]{.8f,1.25f,1.75f})
            foreach(float angle in new[]{0f,45f,-45f,90f,-90f,135f,-135f,180f})
            {
                Vector3 alternative=target+Quaternion.Euler(0,angle,0)*forward*radius;
                if(Chapter3PathMotor.Clear(alternative,alternative)&&Chapter3PathMotor.Clear(alternative,next))return alternative;
            }
        return target;
    }
    static float Horizontal(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
}
