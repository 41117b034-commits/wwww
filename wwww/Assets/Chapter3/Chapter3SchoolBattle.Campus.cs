using UnityEngine;

// Additional playable school support, only on the selected Tado assignment.
// Encounter positions are game staging rather than historical claims.
public sealed partial class Chapter3SchoolBattle
{
    public const int AssaultCombatStages=2;
    static readonly int[] campusAllies={6,9};
    static readonly Vector3[] campusPositions={P(-10,15),P(-12,28)};
    readonly int[] campusHealth={100,100};
    readonly bool[] campusWounded=new bool[AssaultCombatStages];
    readonly Vector3[] campusSavedPosition=new Vector3[AssaultCombatStages];
    public int CampusDefeatedCount
    {get{int count=0;foreach(var enemy in Enemies)if(enemy&&enemy.Defeated)count++;return count;}}
    public bool CampusAllyUnderFire(Transform target)=>Selected==Mission.Tado&&ambient&&ambient.ReactToPoliceShot(target);
    void PrepareCampusOfficers()
    {
        // The two stationary corridor guards are not part of this assignment.
        ClearEnemies();
        for(int i=0;i<AssaultCombatStages;i++)
        {
            campusHealth[i]=100;campusWounded[i]=false;campusSavedPosition[i]=campusPositions[i];
            var enemy=CreateSchoolOfficer(i,campusPositions[i]);enemy.name="校內交戰警察 · "+(i+1);
            enemy.AlliesOnly=true;
            enemy.EquipSabre();
            enemy.actor.Face(ambient.Ambusher(campusAllies[i]).position);
        }
        BindCampusAllies();
    }
    void BindCampusAllies()
    {
        ambient.ConfigurePoliceGroups(Enemies.ToArray(),campusPositions);
    }
    void SaveCampusCheckpoint()
    {
        if(Selected!=Mission.Tado)return;
        for(int i=0;i<AssaultCombatStages;i++)if(Enemies.Count>i&&Enemies[i])
        {var enemy=Enemies[i];campusHealth[i]=enemy.Health;campusWounded[i]=enemy.LegWounded;campusSavedPosition[i]=enemy.transform.position;}
    }
    void RestoreCampusOfficers()
    {
        ClearEnemies();
        for(int i=0;i<AssaultCombatStages;i++)
        {
            var enemy=CreateSchoolOfficer(i,campusSavedPosition[i]);enemy.name="校內交戰警察 · "+(i+1);
            enemy.AlliesOnly=true;
            enemy.EquipSabre();
            enemy.RestoreEncounterState(campusHealth[i],campusWounded[i],campusSavedPosition[i]-Vector3.forward);
        }
        BindCampusAllies();
    }
    void UpdateCampusOfficers()
    {
        if(Selected!=Mission.Tado||CurrentStage==Stage.Complete)return;
        for(int i=0;i<Enemies.Count;i++)
        {
            var enemy=Enemies[i];if(!enemy)continue;
            enemy.AttackEnabled=Fighting&&!enemy.Defeated&&Step<AssaultCombatStages;
            if(!enemy.AttackEnabled){enemy.escortTarget=null;continue;}
            var ally=ambient.PoliceTarget(i);
            enemy.escortTarget=ally;
            enemy.engagementRange=1.9f;enemy.attackInterval=2.8f;
        }
    }
    bool AssaultStageCleared()
    {
        return Step<Enemies.Count&&Enemies[Step]&&Enemies[Step].Defeated;
    }
}
