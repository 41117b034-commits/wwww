using UnityEngine;

// Fictional background staging shared by all choices; no score or player damage.
public sealed partial class Chapter3SchoolAmbient
{
    readonly int[] assaultVictim=new int[AmbusherCount];
    readonly float[] strikeBegan=new float[AmbusherCount];
    float[] fallBegan;
    int[] escapeLeg;
    bool skirmishActive;
    public int AmbushSignalFrame {get;private set;}=-1;
    public bool SkirmishActive=>Panicking&&skirmishActive;
    public Vector3 AmbushStart(int index)=>assaultStarts[index];
    public Transform Ambusher(int index)=>assailants[index]?assailants[index].transform:null;
    public int SkirmishPairsReady {get;private set;}
    public int RestrainedSpectators=>CountPhase(CrowdPhase.Restrained);
    public int EscapedSpectators
    {get{int count=0;if(crowdPhases!=null)foreach(var p in crowdPhases)if(p==CrowdPhase.Escaped)count++;return count;}}

    // Irregular, separate locations; neither ranks nor mirrored pairs.
    static readonly Vector3[] guestStarts={
        new Vector3(-11,0,15),new Vector3(14,0,27),new Vector3(-12,0,8),new Vector3(10,0,12),
        new Vector3(-8,0,27),new Vector3(12,0,34),new Vector3(-13,0,36),new Vector3(5.7f,0,5.8f),
        new Vector3(8.8f,0,26),new Vector3(-15,0,24),new Vector3(-9,0,13),new Vector3(14.5f,0,17),
        new Vector3(-11,0,31),new Vector3(8.5f,0,34),new Vector3(-3,0,17),new Vector3(3.5f,0,10),
        new Vector3(-8,0,22),new Vector3(14,0,23),new Vector3(-4,0,14),new Vector3(-2.4f,0,32)};
    static readonly Vector3[] refuges={
        new Vector3(-12.7f,0,18.5f),new Vector3(12.6f,0,30.8f),new Vector3(-13.9f,0,10.9f),new Vector3(8.6f,0,15.2f),
        new Vector3(-7.8f,0,30.5f),new Vector3(14,0,35),new Vector3(-10,0,37.4f),new Vector3(4,0,7),
        new Vector3(10.8f,0,27.4f),new Vector3(-13.8f,0,23.4f),new Vector3(-12,0,14.9f),new Vector3(12.8f,0,19.3f),
        new Vector3(-13.6f,0,33.2f),new Vector3(6.4f,0,34.9f)};
    static readonly Vector3[] concealment={
        new Vector3(-14,0,16),new Vector3(15,0,29),new Vector3(-10,0,6),new Vector3(13,0,10),
        new Vector3(-11,0,28),new Vector3(15.5f,0,34),new Vector3(-14,0,39),new Vector3(8,0,6.2f),
        new Vector3(13,0,24),new Vector3(-14,0,21)};
    // Each survivor crosses open ground on a different path. The final lane
    // uses the existing rear opening, not a target beyond a solid fence.
    static readonly Vector3[][] escapeRoutes={
        new[]{new Vector3(-3,0,25),new Vector3(-6,0,34),new Vector3(-1,0,38),new Vector3(8,0,34),new Vector3(17,0,34),new Vector3(18,0,44.8f),new Vector3(24.2f,0,45.4f),new Vector3(26,0,51)},
        new[]{new Vector3(2,0,17),new Vector3(4,0,21),new Vector3(9,0,25),new Vector3(16,0,30),new Vector3(18,0,38),new Vector3(18,0,44.8f),new Vector3(24.2f,0,45.4f),new Vector3(27,0,54)},
        new[]{new Vector3(-7,0,29),new Vector3(-11,0,34),new Vector3(-4,0,38),new Vector3(4,0,36),new Vector3(9,0,34),new Vector3(18,0,35),new Vector3(18,0,44.8f),new Vector3(24.2f,0,45.4f),new Vector3(28,0,52)},
        new[]{new Vector3(12,0,25),new Vector3(15,0,31),new Vector3(18,0,38),new Vector3(18,0,44.8f),new Vector3(24.2f,0,45.4f),new Vector3(29,0,56)},
        new[]{new Vector3(-2,0,24),new Vector3(3,0,29),new Vector3(8,0,33),new Vector3(16,0,34),new Vector3(18,0,42),new Vector3(18,0,44.8f),new Vector3(24.2f,0,45.4f),new Vector3(30,0,54)},
        new[]{new Vector3(-5,0,34),new Vector3(-2,0,38),new Vector3(5,0,35),new Vector3(11,0,33),new Vector3(18,0,36),new Vector3(18,0,44.8f),new Vector3(24.2f,0,45.4f),new Vector3(27,0,57)}};
    static Vector3 GuestStart(int index)=>guestStarts[index];
    static Vector3 AmbusherStart(int index)=>concealment[index];
    static Vector3 HoldForward(int index)=>Quaternion.Euler(0,index==0?55:-80,0)*Vector3.forward;
    void ConfigureSkirmish(){fallBegan=new float[crowd.Length];escapeLeg=new int[crowd.Length];ResetSkirmish();}
    void ResetSkirmish()
    {
        skirmishActive=false;AmbushSignalFrame=-1;SkirmishPairsReady=0;
        for(int i=0;i<AmbusherCount;i++){assaultVictim[i]=i;strikeBegan[i]=-1;}
        if(fallBegan!=null)for(int i=0;i<fallBegan.Length;i++){fallBegan[i]=-1;escapeLeg[i]=0;}
    }
    public void EndSkirmish()
    {
        skirmishActive=false;SkirmishPairsReady=0;
        foreach(var actor in assailants)if(actor)actor.SetStrike(0);
        // Retain the two established holds until the school is suspended;
        // the remaining survivors continue fleeing while the squad regroups.
    }
    void UpdateAmbush(float elapsed)
    {
        SkirmishPairsReady=0;
        for(int attacker=0;attacker<AmbusherCount;attacker++)
        {
            var actor=assailants[attacker];if(!actor||!skirmishActive)continue;
            int index=assaultVictim[attacker];
            if(index<0)
            {
                actor.SetStrike(0);
                // Disperse instead of repeatedly swinging at a body or forming
                // ten identical hostage-like pairs.
                Vector3 watch=concealment[attacker]+new Vector3(attacker%2==0?2:-2,0,attacker%3-1);
                if(actor.MoveTowards(watch,1.6f))actor.Face(new Vector3(0,0,29),60);
                continue;
            }
            var guest=crowd[index];if(!guest)continue;
            if(attacker<2)
            {
                Vector3 forward=HoldForward(index),right=Vector3.Cross(Vector3.up,forward);
                actor.HoldAtNeck(guest,false);
                Vector3 behind=guest.transform.position-forward*.48f-right*.15f;
                bool arrived=actor.MoveTowards(behind,2.4f,true,.035f);
                bool guestReady=Vector3.ProjectOnPlane(guest.transform.position-refuges[index],Vector3.up).sqrMagnitude<.12f;
                if(arrived&&guestReady)
                {
                    actor.Face(actor.transform.position+forward,150);
                    actor.HoldAtNeck(guest,true);actor.SetStrike(0);
                    crowdPhases[index]=CrowdPhase.Restrained;SkirmishPairsReady++;
                }
                continue;
            }
            if(guest.IsFallen)
            {
                // Four actors make a second encounter. Six pupils flee and
                // are never recruited into another static pair.
                assaultVictim[attacker]=index<10&&attacker<6?attacker+8:-1;
                strikeBegan[attacker]=-1;actor.SetStrike(0);continue;
            }
            Vector3 direction=Vector3.ProjectOnPlane(guest.transform.position-actor.transform.position,Vector3.up).normalized;
            actor.MoveTowards(guest.transform.position-direction*1.05f,3.1f);
            float distance=Vector3.ProjectOnPlane(guest.transform.position-actor.transform.position,Vector3.up).magnitude;
            bool guestReadyToFall=Vector3.ProjectOnPlane(guest.transform.position-refuges[index],Vector3.up).sqrMagnitude<.12f;
            if(distance>1.5f||!guestReadyToFall||elapsed<3+attacker*.6f){actor.SetStrike(0);continue;}
            actor.Face(guest.transform.position,180);SkirmishPairsReady++;
            if(strikeBegan[attacker]<0)strikeBegan[attacker]=Time.time;
            float progress=(Time.time-strikeBegan[attacker])/1.15f;
            actor.SetStrike(progress<1?Mathf.Clamp(progress,.01f,.99f):0);
            if(progress>=.72f&&fallBegan[index]<0)fallBegan[index]=Time.time;
        }
    }
    bool UpdateSkirmishGuest(int index)
    {
        var guest=crowd[index];
        if(fallBegan[index]>=0)
        {
            guest.SetStartled(0);guest.SetAfraid(false);
            guest.SetFall((Time.time-fallBegan[index])/1.45f);
            crowdPhases[index]=guest.IsFallen?CrowdPhase.Fallen:CrowdPhase.Falling;
            return true;
        }
        if(index>=14||Withdrawing)
        {
            var route=escapeRoutes[index>=14?index-14:index%escapeRoutes.Length];
            if(escapeLeg[index]<route.Length)
            {
                crowdPhases[index]=CrowdPhase.Fleeing;
                if(guest.MoveTowards(route[escapeLeg[index]],2.2f+(index%3)*.23f))escapeLeg[index]++;
            }
            else
            {
                // Only hide after physically reaching the rear exit.
                crowdPhases[index]=CrowdPhase.Escaped;guest.gameObject.SetActive(false);
            }
            return true;
        }
        if(index<2&&crowdPhases[index]==CrowdPhase.Restrained)
        {
            guest.Face(guest.transform.position+HoldForward(index),150);
            guest.SetAfraid(true);guest.SetStartled(.2f);return true;
        }
        if(!skirmishActive)return false;
        if(guest.MoveTowards(refuges[index],1.8f+(index%3)*.2f)&&index<2)
            guest.Face(guest.transform.position+HoldForward(index),150);
        return true;
    }
}
