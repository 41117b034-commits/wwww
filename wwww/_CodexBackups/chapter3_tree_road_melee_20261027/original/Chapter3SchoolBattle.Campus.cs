using UnityEngine;

// Additional playable school support, only on the selected Tado assignment.
// Encounter positions are game staging rather than historical claims.
public sealed partial class Chapter3SchoolBattle
{
    public const int AssaultCombatStages=4;
    static readonly int[] campusAllies={6,9,7,8};
    static readonly Vector3[] campusPositions={P(-12,28),P(-12,37),P(11,31),P(14,39)};
    readonly int[] campusHealth={100,100,100,100};
    readonly bool[] campusWounded=new bool[4];
    readonly Vector3[] campusSavedPosition=new Vector3[4];
    public int CampusDefeatedCount
    {get{int count=0;for(int i=2;i<Enemies.Count;i++)if(Enemies[i]&&Enemies[i].Defeated)count++;return count;}}
    public bool CampusAllyUnderFire(Transform target)=>Selected==Mission.Tado&&ambient&&ambient.ReactToPoliceShot(target);
    void PrepareCampusOfficers()
    {
        for(int i=0;i<4;i++)
        {
            campusHealth[i]=100;campusWounded[i]=false;campusSavedPosition[i]=campusPositions[i];
            var enemy=CreateSchoolOfficer(i+2,campusPositions[i]);enemy.name="校內交戰警察 · "+(i+1);
            enemy.actor.Face(ambient.Ambusher(campusAllies[i]).position);
        }
        BindCampusAllies();
    }
    void BindCampusAllies()
    {
        for(int i=0;i<4;i++)if(Enemies.Count>i+2&&Enemies[i+2])ambient.AssignPoliceOpponent(campusAllies[i],Enemies[i+2]);
    }
    void SaveCampusCheckpoint()
    {
        if(Selected!=Mission.Tado)return;
        for(int i=0;i<4;i++)if(Enemies.Count>i+2&&Enemies[i+2])
        {var enemy=Enemies[i+2];campusHealth[i]=enemy.Health;campusWounded[i]=enemy.LegWounded;campusSavedPosition[i]=enemy.transform.position;}
    }
    void RestoreCampusOfficers()
    {
        for(int i=0;i<4;i++)
        {
            var enemy=CreateSchoolOfficer(i+2,campusSavedPosition[i]);enemy.name="校內交戰警察 · "+(i+1);
            enemy.RestoreEncounterState(campusHealth[i],campusWounded[i],campusSavedPosition[i]-Vector3.forward);
        }
        BindCampusAllies();
    }
    void UpdateCampusOfficers()
    {
        if(Selected!=Mission.Tado||CurrentStage==Stage.Complete)return;
        for(int i=2;i<Enemies.Count;i++)
        {
            var enemy=Enemies[i];if(!enemy)continue;
            enemy.AttackEnabled=Fighting&&!enemy.Defeated&&Step<AssaultCombatStages;
            if(!enemy.AttackEnabled){enemy.escortTarget=null;continue;}
            // Keep visible exchanges around the campus. Close player support
            // draws the officer's attention; distant combat remains with allies.
            var ally=ambient.Ambusher(campusAllies[i-2]);
            enemy.escortTarget=Flat(player.transform.position,enemy.transform.position)<8?null:ally;
            enemy.engagementRange=32;enemy.attackInterval=5.2f;
        }
    }
    bool AssaultStageCleared()
    {
        if(Step<2)return Step<Enemies.Count&&(Enemies[Step]==null||Enemies[Step].Defeated);
        int first=Step==2?2:4;
        return Enemies.Count>=first+2&&Enemies[first]&&Enemies[first].Defeated&&Enemies[first+1]&&Enemies[first+1].Defeated;
    }
}
