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
            actor.HoldAtNeck(null,false);actor.SetStrike(0);actor.SetStartled(0);actor.SetAfraid(false);actor.SetWaiting(false);
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
            // Consume an intermediate corner before yielding to neighbours:
            // nobody needs to stand on its exact centre to turn through it.
            float arrival=withdrawalLeg[i]==route.Length-1?.2f:.5f;
            if(Horizontal(current,target)<arrival)
            {
                withdrawalLeg[i]++;
                // A settled participant is now a standing obstacle. Later
                // arrivals must plan around them to reach the rear row.
                if(withdrawalLeg[i]>=route.Length)actor.FollowingWithdrawal=false;
                continue;
            }
            toPlayer.y=0;Vector3 direction=target-current;direction.y=0;
            // The withdrawal progresses toward the north/rear. A lateral
            // corner must not misclassify a player already at the rear as
            // "behind" merely because they are left of this short waypoint.
            bool playerBehind=withdrawalPlayer.position.z<current.z-1.5f;
            if((toPlayer.magnitude>10&&playerBehind)||YieldToWithdrawal(i,direction.normalized))
            {actor.SetWaiting(false);continue;}
            actor.SetWaiting(false);
            withdrawalMotors[i].Move(actor,target,3.2f);
        }
    }
    bool YieldToWithdrawal(int index,Vector3 direction)
    {
        Vector3 current=assailants[index].transform.position;
        for(int other=0;other<AmbusherCount;other++)if(other!=index&&assailants[other])
        {
            if(withdrawalRoutes[other]==null||withdrawalLeg[other]>=withdrawalRoutes[other].Length)continue;
            Vector3 delta=assailants[other].transform.position-current;delta.y=0;
            Vector3 otherDirection=withdrawalRoutes[other][withdrawalLeg[other]]-assailants[other].transform.position;otherDirection.y=0;
            if(Vector3.Dot(direction,otherDirection.normalized)<.4f&&index<other)continue;
            float along=Vector3.Dot(delta,direction);
            if(delta.sqrMagnitude<1.05f*1.05f&&along>.15f&&Vector3.Cross(delta,direction).magnitude<.8f)return true;
        }
        return false;
    }
    static float Horizontal(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
}
