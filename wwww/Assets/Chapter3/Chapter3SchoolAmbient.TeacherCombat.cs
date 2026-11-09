using UnityEngine;

// The four original male teachers form two groups; every one has a native
// opponent. Only these combatants accept player damage, never the pupils.
public sealed partial class Chapter3SchoolAmbient
{
    public const int RequiredTeacherSupportCount=4;
    static readonly int[] supportTeacherSlots={3,0,6,9};
    readonly int[] teacherHealth={100,100,100,100},teacherPlayerHits=new int[4];
    readonly float[] teacherHitUntil=new float[4];
    readonly float[] duelBegan={-1,-1,-1,-1};
    readonly int[] duelContactCycle={-1,-1,-1,-1},duelResponseCycle={-1,-1,-1,-1};
    readonly int[] duelContacts=new int[4],duelResponses=new int[4];
    static readonly float[] approachAngles={0,45,-45,90,-90,135,-135,180};
    static Vector3 OpenDuelApproach(Chapter3SchoolCrowdActor actor,Chapter3SchoolCrowdActor opponent,Vector3 away,float reach)
    {
        Vector3 target=opponent.transform.position-away*reach;
        foreach(float angle in approachAngles)
        {
            Vector3 candidate=opponent.transform.position-Quaternion.Euler(0,angle,0)*away*reach;
            if(Chapter3PathMotor.ClearCrowdPoint(candidate,actor))return candidate;
        }
        return target;
    }
    public Chapter3SchoolCrowdActor SupportTeacher(int index)=>index>=0&&index<RequiredTeacherSupportCount&&crowd!=null?crowd[supportTeacherSlots[index]]:null;
    public int SupportTeacherHealth(int index)=>index>=0&&index<RequiredTeacherSupportCount?teacherHealth[index]:0;
    public int SupportTeacherPlayerHits(int index)=>index>=0&&index<RequiredTeacherSupportCount?teacherPlayerHits[index]:0;
    public int CivilianDuelContacts(int index)=>index>=0&&index<4?duelContacts[index]:0;
    public int CivilianDuelResponses(int index)=>index>=0&&index<4?duelResponses[index]:0;
    public bool CivilianDuelStarted(int index)=>index>=0&&index<4&&duelBegan[index]>=0;
    public int TeacherSupportDefeatedCount
    {get{int count=0;for(int i=0;i<RequiredTeacherSupportCount;i++)if(teacherHealth[i]<=0&&teacherPlayerHits[i]>0&&SupportTeacher(i)&&SupportTeacher(i).IsFallen)count++;return count;}}
    public bool TeacherSupportComplete=>TeacherSupportDefeatedCount==RequiredTeacherSupportCount;
    public Vector3 PendingTeacherSupportPosition
    {get{for(int i=0;i<RequiredTeacherSupportCount;i++)if(SupportTeacher(i)&&!SupportTeacher(i).IsFallen)return SupportTeacher(i).transform.position;return new Vector3(12,0,32);}}
    public bool IsLivingSupportTeacher(Chapter3SchoolCrowdActor actor)
    {
        for(int i=0;i<RequiredTeacherSupportCount;i++)if(actor&&actor==SupportTeacher(i))return teacherHealth[i]>0&&actor.gameObject.activeInHierarchy;
        return false;
    }
    public bool HitSupportTeacher(Chapter3SchoolCrowdActor actor,int damage,Vector3 source,out bool defeated)
    {
        defeated=false;
        var school=GetComponentInParent<Chapter3SchoolController>();
        if(!Panicking||!skirmishActive||Withdrawing||damage<=0||!school||!school.Battle||!school.Battle.Fighting)return false;
        for(int i=0;i<RequiredTeacherSupportCount;i++)if(actor&&actor==SupportTeacher(i)&&teacherHealth[i]>0&&actor.gameObject.activeInHierarchy)
        {
            teacherHealth[i]=Mathf.Max(0,teacherHealth[i]-damage);teacherPlayerHits[i]++;teacherHitUntil[i]=Time.time+.4f;
            actor.SetStartled(.75f);actor.Face(source,160);defeated=teacherHealth[i]==0;
            if(defeated)
            {
                int guest=supportTeacherSlots[i];fallBegan[guest]=Time.time;StagedCivilianFalls++;
                actor.SetStrike(0);actor.SetMeleePose(0,0);actor.SetWaiting(false);
            }
            return true;
        }
        return false;
    }
    void ResetTeacherCombat()
    {
        for(int i=0;i<RequiredTeacherSupportCount;i++){teacherHealth[i]=100;teacherPlayerHits[i]=0;teacherHitUntil[i]=0;}
        for(int i=0;i<4;i++){duelBegan[i]=-1;duelContactCycle[i]=duelResponseCycle[i]=-1;duelContacts[i]=duelResponses[i]=0;}
    }
    bool UpdateCivilianDuel(int encounter,Chapter3SchoolCrowdActor actor,Chapter3SchoolCrowdActor guest,float elapsed)
    {
        int index=civilianVictims[encounter];
        var school=GetComponentInParent<Chapter3SchoolController>();
        if(fallBegan[index]>=0||!school||!school.Battle||!school.Battle.Fighting)
        {actor.SetStrike(0);actor.SetStartled(0);guest.SetStrike(0);return true;}
        Vector3 away=Vector3.ProjectOnPlane(guest.transform.position-actor.transform.position,Vector3.up).normalized;
        if(away.sqrMagnitude<.1f)away=Vector3.forward;
        float distance=Horizontal(actor.transform.position,guest.transform.position);
        actor.SetAfraid(false);actor.SetWaiting(false);actor.SetCowering(false);actor.Face(guest.transform.position,180);
        guest.SetAfraid(false);guest.SetWaiting(false);guest.SetCowering(false);guest.Face(actor.transform.position,150);
        if(duelBegan[encounter]<0)
        {
            civilianPanicMotors[index].Patrol(guest,refuges[index],2.1f,.25f);
            civilianAssaultMotors[encounter].Patrol(actor,OpenDuelApproach(actor,guest,away,1.18f),distance>2?3.1f:1.15f,.1f);
            actor.SetMeleePose(.55f,0);actor.SetStrike(0);guest.SetMeleePose(.8f,0);guest.SetStrike(0);
            if(Horizontal(guest.transform.position,refuges[index])<.45f&&distance<=1.5f&&ClearCivilianContact(actor,guest)&&
                elapsed>=5+encounter*.7f&&CivilianEncounterVisible(guest))duelBegan[encounter]=Time.time;
            return true;
        }
        float clock=Time.time-duelBegan[encounter];int cycle=Mathf.FloorToInt(clock/3.1f);float phase=clock-cycle*3.1f;
        bool nativeSwing=phase<.85f,counter=phase>=1.4f&&phase<2.2f;
        bool flinching=Time.time<teacherHitUntil[encounter];
        Vector3 side=Vector3.Cross(Vector3.up,away)*(cycle%2==0?1:-1);
        Vector3 dodge=refuges[index]+side*(nativeSwing?.32f:-.18f)+away*(nativeSwing?.22f:0);
        if(!flinching)civilianPanicMotors[index].Patrol(guest,dodge,.85f,.08f);
        civilianAssaultMotors[encounter].Patrol(actor,guest.transform.position-away*(counter?1.55f:1.18f),1.25f,.10f);
        // Patrol turns toward its walking step. Restore the live opponent
        // facing after that step so a dodge remains a facing sidestep.
        actor.Face(guest.transform.position,360);guest.Face(actor.transform.position,360);
        actor.SetMeleePose(counter?.95f:.5f,cycle%3);actor.SetStartled(counter&&phase>1.8f?.35f:0);
        actor.SetStrike(nativeSwing&&distance<=1.9f?Mathf.Clamp(phase/.85f,.01f,.99f):0);
        guest.SetMeleePose(nativeSwing?.9f:.6f,cycle%2);guest.SetStartled(flinching?.75f:nativeSwing&&phase>.45f?.3f:0);
        guest.SetStrike(counter&&!flinching?Mathf.Clamp((phase-1.4f)/.8f,.01f,.99f):0);
        if(nativeSwing&&phase>=.48f&&duelContactCycle[encounter]!=cycle&&distance<=1.5f&&ClearCivilianContact(actor,guest))
        {
            duelContactCycle[encounter]=cycle;duelContacts[encounter]++;
            civilianContactDistance[encounter]=distance;civilianContactElapsed[encounter]=elapsed;
            // Repeated visible contact does not finish the player's support
            // objective. All four male teachers need actual weapon hits.
        }
        bool defendedStep=nativeSwing&&phase>=.48f&&Horizontal(guest.transform.position,refuges[index])>.12f;
        bool counterShove=counter&&!flinching&&phase>=1.65f;
        if(duelResponseCycle[encounter]!=cycle&&(defendedStep||counterShove))
        {duelResponseCycle[encounter]=cycle;duelResponses[encounter]++;}
        return true;
    }
}
