using UnityEngine;

// The original withdrawal party clears a view down the forest path on foot.
// Reception slots do not replace either the campus arrival or follower order.
public sealed partial class Chapter3SchoolAmbient
{
    readonly Vector3[] receptionTargets=new Vector3[AmbusherCount];
    readonly bool[] receptionAssigned=new bool[AmbusherCount];
    Vector3 receptionObserver,receptionForward,receptionSide;
    Vector3[] receptionSightline;
    public bool ReceptionClearing {get;private set;}
    public int ReceptionArrivedCount
    {
        get
        {
            int count=0;if(!ReceptionClearing)return count;
            for(int i=0;i<AmbusherCount;i++)
                if(assailants[i]&&assailants[i].gameObject.activeInHierarchy&&withdrawalReached[i]
                    &&forestOrder[i]>=0&&receptionAssigned[i]
                    &&Horizontal(assailants[i].transform.position,receptionTargets[i])<.3f)count++;
            return count;
        }
    }
    public bool ReceptionReady=>ReceptionClearing&&ReceptionArrivedCount==AmbusherCount;
    public bool BeginReceptionClearing(Vector3 observer,Vector3 lookToward)
    {
        if(!Withdrawing||!forestFollowing)return false;
        CancelReceptionClearing();receptionObserver=observer;receptionObserver.y=0;
        receptionForward=Vector3.ProjectOnPlane(lookToward-observer,Vector3.up).normalized;
        if(receptionForward.sqrMagnitude<.01f)receptionForward=Vector3.back;
        receptionSide=Vector3.Cross(Vector3.up,receptionForward);
        // Clear the whole bend, not only the final straight line to Pihu.
        receptionSightline=new[]{new Vector3(8,0,103),new Vector3(0,0,98),
            new Vector3(-1,0,94),new Vector3(-1.75f,0,92),lookToward};
        ReceptionClearing=true;
        for(int i=0;i<AmbusherCount;i++)withdrawalMotors[i]?.Reset();
        return true;
    }
    public void CancelReceptionClearing()
    {
        if(ReceptionClearing)for(int i=0;i<AmbusherCount;i++)withdrawalMotors[i]?.Reset();
        ReceptionClearing=false;receptionSightline=null;
        for(int i=0;i<AmbusherCount;i++)receptionAssigned[i]=false;
    }
    void UpdateReceptionWalker(int index,Chapter3SchoolCrowdActor actor)
    {
        if(!receptionAssigned[index])
        {
            if(!TryReceptionSlot(index,actor,out var destination))return;
            receptionTargets[index]=destination;receptionAssigned[index]=true;
            withdrawalMotors[index].Reset();
        }
        actor.FollowingWithdrawal=true;actor.SetWaiting(false);
        if(Horizontal(actor.transform.position,receptionTargets[index])<.2f)
        {actor.Face(receptionObserver,65);return;}
        withdrawalMotors[index].Move(actor,receptionTargets[index],Chapter3Player.WalkSpeed,.12f);
    }
    bool TryReceptionSlot(int index,Chapter3SchoolCrowdActor actor,out Vector3 target)
    {
        int rank=forestOrder[index],row=rank/2;
        float gap=3.4f+row*1.8f,side=rank%2==0?-1:1;
        // Keep each row near its existing position, widening the two flanks.
        // All slots remain south of Mona's entrance behind the shelter.
        for(int wider=0;wider<6;wider++)
            for(int depth=0;depth<3;depth++)
            {
                float offset=depth==0?0:depth==1?.6f:-.6f;
                Vector3 candidate=receptionObserver+receptionForward*(gap+offset)
                    +receptionSide*side*(2.6f+row*.4f+wider*.75f);
                if(!ClearReceptionSightlines(candidate)||!Chapter3PathMotor.ClearCrowdPoint(candidate,actor))continue;
                bool occupied=false;
                for(int other=0;other<AmbusherCount;other++)
                    if(other!=index&&receptionAssigned[other]&&Horizontal(candidate,receptionTargets[other])<.85f)
                    {occupied=true;break;}
                if(occupied)continue;
                target=candidate;return true;
            }
        target=actor.transform.position;return false;
    }
    bool ClearReceptionSightlines(Vector3 candidate)
    {
        if(receptionSightline==null)return false;
        Vector3 previous=Vector3.zero;
        foreach(var point in receptionSightline)
        {
            Vector3 segment=Vector3.ProjectOnPlane(point-receptionObserver,Vector3.up);
            float t=segment.sqrMagnitude>.01f?Mathf.Clamp01(Vector3.Dot(candidate-receptionObserver,segment)/segment.sqrMagnitude):0;
            if(Horizontal(candidate,receptionObserver+segment*t)<1.5f)return false;
            float area=Vector3.Cross(previous,segment).y;
            if(Mathf.Abs(area)>.001f)
            {
                Vector3 relative=Vector3.ProjectOnPlane(candidate-receptionObserver,Vector3.up);
                float a=Vector3.Cross(relative,segment).y/area,b=Vector3.Cross(previous,relative).y/area;
                if(a>=0&&b>=0&&a+b<=1)return false;
            }
            previous=segment;
        }
        return true;
    }
}