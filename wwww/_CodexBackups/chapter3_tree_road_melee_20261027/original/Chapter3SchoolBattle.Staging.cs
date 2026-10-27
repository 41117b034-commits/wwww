using UnityEngine;

// Two existing officers attend the gathering before the attack. Their response
// paths and timing are game staging, not a reconstruction of recorded tactics.
public sealed partial class Chapter3SchoolBattle
{
    public enum OfficerPhase { Absent, Calm, Noticing, Moving, Ready, Wounded, Defeated, Paused }
    readonly OfficerPhase[] officerPhases={OfficerPhase.Absent,OfficerPhase.Absent};
    readonly Chapter3PathMotor[] officerMotors={new Chapter3PathMotor(),new Chapter3PathMotor()};
    readonly int[] officerWaypoint=new int[2];
    readonly float[] officerDistance=new float[2];
    readonly int[] checkpointOfficerHealth={100,100};
    readonly bool[] checkpointOfficerWounded=new bool[2];
    readonly Vector3[] checkpointOfficerPosition=new Vector3[2],checkpointOfficerForward=new Vector3[2];
    Vector3[][] officerRoutes;
    float officerResponseAt=-1;
    public bool OfficersResponding=>officerResponseAt>=0;
    public float OfficerResponseElapsed=>OfficersResponding?Mathf.Max(0,Time.time-officerResponseAt):0;
    public int VisibleOfficers
    {
        get{int count=0;foreach(var officer in Enemies)if(officer&&officer.gameObject.activeInHierarchy)count++;return count;}
    }
    public OfficerPhase OfficerState(int index)=>index>=0&&index<2?officerPhases[index]:OfficerPhase.Absent;
    public float OfficerWalkedDistance(int index)=>index>=0&&index<2?officerDistance[index]:0;
    public Vector3 OfficerDestination(int index)=>threatPositions!=null&&index>=0&&index<threatPositions.Length?threatPositions[index]:Vector3.zero;
    public Vector3[] OfficerResponseRoute(int index)=>officerRoutes!=null&&index>=0&&index<officerRoutes.Length?(Vector3[])officerRoutes[index].Clone():new Vector3[0];
    public bool OfficerSettled(int index)=>OfficerState(index)==OfficerPhase.Ready||OfficerState(index)==OfficerPhase.Wounded||OfficerState(index)==OfficerPhase.Defeated;
    public int CheckpointOfficerHealth(int index)=>index>=0&&index<2?checkpointOfficerHealth[index]:0;
    public bool CheckpointOfficerLegWounded(int index)=>index>=0&&index<2&&checkpointOfficerWounded[index];
    public bool CheckpointOfficerDefeated(int index)=>index>=0&&index<2&&checkpointOfficerHealth[index]<=0;

    void PrepareCalmOfficers()
    {
        ResetPassageCheckpoint();
        officerResponseAt=-1;officerRoutes=null;
        var positions=new[]{P(-7,12),P(6,31)};
        for(int i=0;i<2;i++)
        {
            officerMotors[i].Reset();officerWaypoint[i]=0;officerDistance[i]=0;
            var officer=CreateSchoolOfficer(i,positions[i]);
            officer.actor.Face(P(0,23));
            officerPhases[i]=OfficerPhase.Calm;
            checkpointOfficerHealth[i]=100;checkpointOfficerWounded[i]=false;
            checkpointOfficerPosition[i]=positions[i];checkpointOfficerForward[i]=Vector3.back;
        }
    }
    Chapter3Enemy CreateSchoolOfficer(int index,Vector3 position)
    {
        var go=Instantiate(combat.enemyTemplates[index%combat.enemyTemplates.Length],enemiesRoot);
        go.name=index==0?"會場警察 · 前場":"會場警察 · 校舍附近";
        go.transform.position=position;
        var enemy=go.GetComponent<Chapter3Enemy>();
        enemy.combat=combat;enemy.advances=false;enemy.AttackEnabled=false;
        enemy.escortTarget=null;enemy.attackInterval=4.5f;
        enemy.engagementRange=school.flow.CarriedWeapon==Chapter3Weapons.Weapon.Knife?12:Selected==Mission.Tado?28:32;
        Enemies.Add(enemy);go.SetActive(true);
        return enemy;
    }
    Vector3[] MissionOfficerPositions()
    {
        bool knife=school.flow.CarriedWeapon==Chapter3Weapons.Weapon.Knife;
        if(Selected==Mission.Tado)return new[]{P(-1,knife?18.5f:21),P(5,knife?33:36)};
        if(Selected==Mission.Bawan)return new[]{P(-25.7f,knife?14.4f:22),P(-28,knife?44.4f:53)};
        return new[]{P(29,knife?27.8f:33),P(34.5f,knife?52:59)};
    }
    void BeginOfficerResponse()
    {
        threatPositions=MissionOfficerPositions();
        if(Selected==Mission.Tado)
            officerRoutes=new[]{new[]{P(-7,16),threatPositions[0]},new[]{P(7,33),threatPositions[1]}};
        else if(Selected==Mission.Bawan)
        {
            bool knife=school.flow.CarriedWeapon==Chapter3Weapons.Weapon.Knife;
            // Side fences have their actual opening at z=21..28. The first
            // officer stays on the appropriate side of the ordinary front gate.
            var first=knife
                ?new[]{P(-2,8),P(-2,2),P(-2,-4.5f),P(-24,-4.5f),P(-27,8),threatPositions[0]}
                :new[]{P(-16,18),P(-19,19),P(-19,24),P(-24,24),threatPositions[0]};
            // The rear officer goes round the gate's east end, through the
            // open lane west of the main building, never through a closed leaf.
            var second=knife
                ?new[]{P(-12,33),P(-19,31),P(-19,24),P(-24,24),P(-27,28),P(-27,38),threatPositions[1]}
                :new[]{P(-12,36),P(-19.8f,43),P(-20,44.6f),P(-22.7f,44.6f),P(-22.7f,49),P(-26,51),threatPositions[1]};
            officerRoutes=new[]{first,second};
        }
        else
            officerRoutes=new[]{
                new[]{P(0,18),P(17,24),P(24,24),P(27.5f,27),threatPositions[0]},
                new[]{P(16,35),P(19,35),P(20.8f,38),P(20.8f,44.6f),P(24.2f,44.6f),P(24.2f,49),P(29,53),P(32.5f,51.5f),threatPositions[1]}};
        officerResponseAt=Time.time;
        for(int i=0;i<2;i++)
        {
            officerMotors[i].Reset();officerWaypoint[i]=0;officerDistance[i]=0;
            officerPhases[i]=OfficerPhase.Calm;
            if(i<Enemies.Count&&Enemies[i])
            {
                Enemies[i].AttackEnabled=false;
                Enemies[i].engagementRange=school.flow.CarriedWeapon==Chapter3Weapons.Weapon.Knife?12:Selected==Mission.Tado?28:32;
            }
        }
    }
    void ClearOfficerStaging()
    {
        officerResponseAt=-1;officerRoutes=null;threatPositions=null;
        for(int i=0;i<2;i++)
        {officerPhases[i]=OfficerPhase.Absent;officerMotors[i].Reset();officerWaypoint[i]=0;officerDistance[i]=0;}
    }
    void ResetOfficersForCheckpoint()
    {
        ClearEnemies();threatPositions=MissionOfficerPositions();officerResponseAt=Time.time;
        officerRoutes=null;
        for(int i=0;i<2;i++)
        {
            officerMotors[i].Reset();officerWaypoint[i]=0;officerDistance[i]=0;
            if(i<CheckpointStep){Enemies.Add(null);officerPhases[i]=OfficerPhase.Defeated;continue;}
            bool defeated=checkpointOfficerHealth[i]<=0,wounded=checkpointOfficerWounded[i];
            var position=defeated||wounded?checkpointOfficerPosition[i]:threatPositions[i];
            var enemy=CreateSchoolOfficer(i,position);
            enemy.actor.Face(position+checkpointOfficerForward[i]);
            enemy.RestoreEncounterState(checkpointOfficerHealth[i],wounded,position-checkpointOfficerForward[i]);
            officerPhases[i]=defeated?OfficerPhase.Defeated:wounded?OfficerPhase.Wounded:OfficerPhase.Ready;
        }
    }
    void SaveOfficerCheckpoint()
    {
        SaveCampusCheckpoint();
        SavePassageCheckpoint();
        // A future officer can already be wounded or defeated while the player
        // completes the current passage. Retrying must keep that saved outcome.
        for(int i=0;i<2;i++)
        {
            var enemy=i<Enemies.Count?Enemies[i]:null;
            if(!enemy){if(i<CheckpointStep)checkpointOfficerHealth[i]=0;continue;}
            checkpointOfficerHealth[i]=enemy.Health;checkpointOfficerWounded[i]=enemy.LegWounded;
            checkpointOfficerPosition[i]=enemy.transform.position;
            checkpointOfficerForward[i]=enemy.actor&&enemy.actor.Rig?enemy.actor.Rig.Forward:enemy.transform.forward;
        }
    }
    void UpdateOfficerResponse()
    {
        // Rescue reuses Enemies for its forest pursuer after the school mission.
        // Its own encounter must retain control of that officer's attacks.
        if(CurrentStage==Stage.Complete)return;
        for(int i=0;i<Enemies.Count&&i<2;i++)
        {
            var enemy=Enemies[i];if(!enemy)continue;
            if(enemy.Defeated){officerPhases[i]=OfficerPhase.Defeated;enemy.AttackEnabled=false;continue;}
            if(CurrentStage==Stage.Down||CurrentStage==Stage.Inactive||CurrentStage==Stage.Regroup)
            {enemy.AttackEnabled=false;officerPhases[i]=OfficerPhase.Paused;continue;}
            if(enemy.LegWounded)officerPhases[i]=OfficerPhase.Wounded;
            else if(OfficersResponding&&officerRoutes!=null)
            {
                float noticeAt=.65f+i*.6f,walkAt=noticeAt+1.1f;
                if(OfficerResponseElapsed<noticeAt)officerPhases[i]=OfficerPhase.Calm;
                else if(OfficerResponseElapsed<walkAt)
                {
                    officerPhases[i]=OfficerPhase.Noticing;
                    enemy.actor.Face(P(0,4));
                }
                else if(officerWaypoint[i]<officerRoutes[i].Length)
                {
                    officerPhases[i]=OfficerPhase.Moving;
                    Vector3 before=enemy.transform.position,target=officerRoutes[i][officerWaypoint[i]];
                    officerMotors[i].Move(enemy.actor,target,2.05f);
                    officerDistance[i]+=Flat(before,enemy.transform.position);
                    if(Flat(enemy.transform.position,target)<.20f)officerWaypoint[i]++;
                    if(officerWaypoint[i]>=officerRoutes[i].Length)officerPhases[i]=OfficerPhase.Ready;
                }
            }
            // Movement owns facing while the officer is relocating. The
            // existing Enemy script only aims after it has visibly stopped.
            bool ready=officerPhases[i]==OfficerPhase.Ready||officerPhases[i]==OfficerPhase.Wounded;
            enemy.AttackEnabled=Fighting&&i==Step&&ready&&
                (Selected!=Mission.Pihu||(AtCrossing&&!ThreatRevealing));
        }
    }
}
