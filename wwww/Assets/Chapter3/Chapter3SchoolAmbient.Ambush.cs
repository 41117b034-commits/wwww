using UnityEngine;

// Fictional background staging shared by all choices; no score or player damage.
public sealed partial class Chapter3SchoolAmbient
{
    readonly int[] assaultVictim=new int[AmbusherCount];
    readonly float[] strikeBegan=new float[AmbusherCount];
    float[] fallBegan;
    bool skirmishActive;
    float enteredGroundAt=-1;
    public int AmbushSignalFrame {get;private set;}=-1;
    public bool SkirmishActive=>Panicking&&skirmishActive;
    public Vector3 AmbushStart(int index)=>assaultStarts[index];
    public Transform Ambusher(int index)=>assailants[index]?assailants[index].transform:null;
    public int SkirmishPairsReady {get;private set;}
    static Vector3 Pocket(int pair)=>new Vector3(pair%2==0?-10:10,0,8+(pair/2)*7);
    static Vector3 AttackSpot(int victim)
    {
        int pair=victim%AmbusherCount;
        return Pocket(pair)+(victim>=AmbusherCount?Vector3.right*(pair%2==0?-2.7f:2.7f):Vector3.zero);
    }
    void ConfigureSkirmish(){fallBegan=new float[crowd.Length];ResetSkirmish();}
    void ResetSkirmish()
    {
        skirmishActive=false;AmbushSignalFrame=-1;SkirmishPairsReady=0;enteredGroundAt=-1;
        for(int i=0;i<AmbusherCount;i++){assaultVictim[i]=i;strikeBegan[i]=-1;}
        if(fallBegan!=null)for(int i=0;i<fallBegan.Length;i++)fallBegan[i]=-1;
    }
    public void EndSkirmish()
    {
        skirmishActive=false;SkirmishPairsReady=0;
        foreach(var actor in assailants)if(actor)actor.SetStrike(0);
    }
    void UpdateAmbush(float elapsed)
    {
        SkirmishPairsReady=0;
        var school=GetComponentInParent<Chapter3SchoolController>();
        if(enteredGroundAt<0&&school&&school.player.transform.position.z>3)enteredGroundAt=elapsed;
        for(int pair=0;pair<AmbusherCount;pair++)
        {
            var actor=assailants[pair];if(!actor||!skirmishActive)continue;
            int index=assaultVictim[pair];
            if(index>=crowd.Length){actor.SetStrike(0);continue;}
            var guest=crowd[index];if(!guest)continue;
            if(guest.IsFallen)
            {assaultVictim[pair]+=AmbusherCount;strikeBegan[pair]=-1;actor.SetStrike(0);continue;}
            bool second=index>=AmbusherCount;
            // The second exchanges begin as the player approaches, keeping the
            // scene readable from the outer routes and during Tado's entry.
            float readyAt=second?(enteredGroundAt<0?float.PositiveInfinity:enteredGroundAt+18+pair*3):3.5f+pair*.55f;
            bool arrived=actor.MoveTowards(AttackSpot(index),second?2.5f:3.5f);
            if(arrived)actor.Face(guest.transform.position,150);
            bool near=Vector3.ProjectOnPlane(guest.transform.position-actor.transform.position,Vector3.up).sqrMagnitude<2.8f;
            if(!arrived||!near){actor.SetStrike(0);continue;}
            SkirmishPairsReady++;
            if(elapsed<readyAt)
            {
                // Defensive parries, with roots planted, precede the final cut.
                float beat=Mathf.Repeat(elapsed+pair*.37f,3.4f);
                actor.SetStrike(beat<1.1f?Mathf.Clamp(beat/1.1f,.01f,.99f):0);
                continue;
            }
            if(strikeBegan[pair]<0)strikeBegan[pair]=Time.time;
            float progress=(Time.time-strikeBegan[pair])/1.15f;
            actor.SetStrike(progress<1?Mathf.Clamp(progress,.01f,.99f):0);
            // Contact precedes the complete collapse, even after a blocked path.
            if(progress>=.72f&&fallBegan[index]<0)fallBegan[index]=Time.time;
        }
    }
    bool UpdateSkirmishGuest(int index)
    {
        var guest=crowd[index];
        // Finish a collapse even when the mission hands off to regroup.
        if(fallBegan[index]>=0)
        {
            guest.SetStartled(0);guest.SetAfraid(false);
            guest.SetFall((Time.time-fallBegan[index])/1.45f);
            crowdPhases[index]=guest.IsFallen?CrowdPhase.Fallen:CrowdPhase.Falling;
            return true;
        }
        if(!skirmishActive)return false;
        int pair=index%AmbusherCount;
        Vector3 refuge=AttackSpot(index)+Vector3.forward*1.35f;
        if(guest.MoveTowards(refuge,1.9f))
        {
            guest.Face(assailants[pair].transform.position,100);
            guest.SetStartled(.3f+.25f*Mathf.Sin(AssaultElapsed*2+index));
        }
        return true;
    }
}
