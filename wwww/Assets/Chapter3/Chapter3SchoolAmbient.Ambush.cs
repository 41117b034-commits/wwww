using UnityEngine;

// Fictional background staging shared by all choices; no score or player damage.
public sealed partial class Chapter3SchoolAmbient
{
    readonly int[] assaultVictim=new int[AmbusherCount];
    float[] fallBegan;
    int[] escapeLeg;
    bool skirmishActive;
    // Two separate civilian encounters: two teachers, and two pupils.
    // These five participants never become extra police fighting groups.
    static readonly int[] civilianAttackers={3,7,4,5};
    static readonly int[] civilianVictims={3,1,12,13};
    readonly bool[] civilianEncounterDone=new bool[AmbusherCount];
    readonly Chapter3PathMotor[] civilianAssaultMotors={new Chapter3PathMotor(),new Chapter3PathMotor(),new Chapter3PathMotor(),new Chapter3PathMotor(),new Chapter3PathMotor()};
    readonly RaycastHit[] civilianContactHits=new RaycastHit[32];
    public int StagedCivilianFalls {get;private set;}
    readonly float[] civilianContactDistance=new float[4],civilianContactElapsed=new float[4];
    public int CivilianEncounterCount=>civilianVictims.Length;
    public bool CivilianEncounterComplete=>civilianEncounterDone[3]&&civilianEncounterDone[7]&&civilianEncounterDone[4]&&civilianEncounterDone[5];
    public float CivilianContactDistance(int encounter)=>encounter>=0&&encounter<civilianVictims.Length?civilianContactDistance[encounter]:-1;
    public float CivilianContactElapsed(int encounter)=>encounter>=0&&encounter<civilianVictims.Length?civilianContactElapsed[encounter]:-1;
    public int CivilianEncounterVictim(int encounter)=>encounter>=0&&encounter<civilianVictims.Length?civilianVictims[encounter]:-1;
    public int CivilianEncounterAttacker(int encounter)=>encounter>=0&&encounter<civilianAttackers.Length?civilianAttackers[encounter]:-1;
    bool HasCivilianEncounter(int actor)=>System.Array.IndexOf(civilianAttackers,actor)>=0&&!civilianEncounterDone[actor];
    bool IsStagedCivilian(int guest)=>System.Array.IndexOf(civilianVictims,guest)>=0;
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
        ClearPoliceOpposition();
        skirmishActive=false;AmbushSignalFrame=-1;SkirmishPairsReady=0;
        System.Array.Clear(sheltered,0,sheltered.Length);
        ResetCivilianShelters();
        ResetTeacherCombat();
        foreach(var motor in watchMotors)motor?.Reset();
        StagedCivilianFalls=0;
        for(int i=0;i<civilianVictims.Length;i++){civilianContactDistance[i]=-1;civilianContactElapsed[i]=-1;}
        for(int i=0;i<AmbusherCount;i++)
        {int encounter=System.Array.IndexOf(civilianAttackers,i);assaultVictim[i]=encounter>=0?civilianVictims[encounter]:-1;civilianEncounterDone[i]=false;}
        foreach(var motor in civilianAssaultMotors)motor.Reset();
        if(fallBegan!=null)for(int i=0;i<fallBegan.Length;i++){fallBegan[i]=-1;escapeLeg[i]=0;}
    }
    public void EndSkirmish()
    {
        skirmishActive=false;SkirmishPairsReady=0;
        foreach(var actor in assailants)if(actor){actor.SetStrike(0);actor.SetMeleePose(0,0);}
        if(crowd!=null)foreach(var guest in crowd)if(guest){guest.SetStrike(0);guest.SetMeleePose(0,0);guest.SetStartled(0);}
        // Surviving guests retain their own escape/pursuit state. Only the
        // original ten participants join the player's departing party.
    }
    void UpdateAmbush(float elapsed)
    {
        UpdatePoliceMovement();SkirmishPairsReady=0;
        for(int attacker=0;attacker<AmbusherCount;attacker++)
        {
            var actor=assailants[attacker];if(!actor||!skirmishActive)continue;
            if(attacker<3){ShelterNativeChild(attacker,actor);continue;}
            if(HasCivilianEncounter(attacker)&&UpdateCivilianEncounter(attacker,actor,elapsed))continue;
            if(attacker==8&&!civilianEncounterDone[5])
            {
                var pupil=crowd[13];actor.SetAfraid(false);actor.SetStrike(0);actor.SetMeleePose(.4f,0);
                civilianAssaultMotors[4].Patrol(actor,pupil.transform.position+new Vector3(2.3f,0,-1.2f),3.1f,.4f);
                actor.Face(pupil.transform.position,180);continue;
            }
            if(UpdatePoliceOpposition(attacker,actor))continue;
            actor.SetStrike(0);actor.SetMeleePose(0,0);
            WatchCampus(attacker,actor,concealment[attacker]);
        }
    }
    bool UpdateCivilianEncounter(int attacker,Chapter3SchoolCrowdActor actor,float elapsed)
    {
        int index=assaultVictim[attacker];var guest=crowd[index];
        if(!guest)return false;
        int encounter=System.Array.IndexOf(civilianAttackers,attacker);
        actor.HoldAtNeck(null,false);actor.SetMeleePose(.4f,0);actor.SetAfraid(false);
        if(guest.IsFallen)
        {
            actor.SetStrike(0);civilianEncounterDone[attacker]=true;return false;
        }
        if(duelBegan[encounter]>=0)SkirmishPairsReady++;
        return UpdateCivilianDuel(encounter,actor,guest,elapsed);
    }
    bool CivilianEncounterVisible(Chapter3SchoolCrowdActor guest)
    {
        var school=GetComponentInParent<Chapter3SchoolController>();
        // Let the player physically enter the campus before each encounter.
        // Once a swing begins, the existing contact checks own its completion.
        return school&&school.Battle&&school.Battle.Fighting&&school.player&&
            school.player.transform.position.z>=3&&
            Horizontal(school.player.transform.position,guest.transform.position)<=36f;
    }
    bool ClearCivilianContact(Chapter3SchoolCrowdActor actor,Chapter3SchoolCrowdActor guest)
    {
        Vector3 from=actor.NeckPoint,to=guest.NeckPoint,delta=to-from;
        int count=Physics.RaycastNonAlloc(from,delta.normalized,civilianContactHits,delta.magnitude,~0,QueryTriggerInteraction.Ignore);
        for(int i=0;i<count;i++)
        {
            var hit=civilianContactHits[i].collider;if(!hit)continue;
            var t=hit.transform;
            if(t==actor.transform||t.IsChildOf(actor.transform)||t==guest.transform||t.IsChildOf(guest.transform))continue;
            return false;
        }
        return true;
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
        if(IsJapanesePursuer(index)){UpdateJapaneseDefender(index);return true;}
        if(IsStagedCivilian(index)&&skirmishActive)
        {
            // The paired encounter owns this person's physical defence and
            // evasion. Do not overwrite it with the old waiting-at-refuge pose.
            return true;
        }
        UpdateCivilianPanic(index);return true;
    }
}
