using UnityEngine;

// Four of the existing ten participants engage armed police on Tado's route.
// Their final withdrawal remains owned by the original all-ten system.
public sealed partial class Chapter3SchoolAmbient
{
    readonly Chapter3Enemy[] policeOpponents=new Chapter3Enemy[AmbusherCount];
    readonly float[] policeStrikeAt=new float[AmbusherCount],policeFlinchUntil=new float[AmbusherCount];
    readonly bool[] policeStrikeLanded=new bool[AmbusherCount],policeAssigned=new bool[AmbusherCount];
    readonly int[] policeGroups=new int[AmbusherCount],policeSlots=new int[AmbusherCount];
    Chapter3Enemy[] groupPolice;
    Vector3[] groupCentres;
    readonly Chapter3PathMotor[] policeMotors={new Chapter3PathMotor(),new Chapter3PathMotor(),new Chapter3PathMotor(),new Chapter3PathMotor()};
    float groupsBegan;
    // Distinct blocking: a moving duel, pursuit, two-person encirclement,
    // and three allies pushing an officer back. All participants are the
    // existing ten villagers, so withdrawal continues with the same people.
    public void ConfigurePoliceGroups(Chapter3Enemy[] enemies,Vector3[] centres)
    {
        ClearPoliceOpposition();groupPolice=enemies;groupCentres=centres;groupsBegan=Time.time;
        int[][] groups={new[]{6},new[]{9},new[]{7,3},new[]{8,4,5}};
        for(int g=0;g<groups.Length;g++)
        {
            policeMotors[g].Reset();
            for(int slot=0;slot<groups[g].Length;slot++)
            {int a=groups[g][slot];AssignPoliceOpponent(a,enemies[g]);policeGroups[a]=g;policeSlots[a]=slot;}
        }
    }
    float GroupPhase(int g)=>(Time.time-groupsBegan+g*2.1f)%(g==1?12:9);
    bool GroupMoving(int g)=>GroupPhase(g)<(g==1?7:4.5f);
    Vector3 GroupTravel(int g)
    {
        int leg=Mathf.FloorToInt((Time.time-groupsBegan+g*2.1f)/(g==1?6:4.5f))%4;
        Vector3[] corners=g==1?new[]{new Vector3(-2,0,-3),new Vector3(2,0,-3),new Vector3(2,0,3),new Vector3(-2,0,3)}:
            new[]{new Vector3(-1.7f,0,-1.4f),new Vector3(1.7f,0,-1.4f),new Vector3(1.7f,0,1.4f),new Vector3(-1.7f,0,1.4f)};
        return groupCentres[g]+corners[leg];
    }
    void UpdatePoliceMovement()
    {
        if(groupPolice==null||Withdrawing)return;
        var battle=GetComponentInParent<Chapter3SchoolController>().Battle;
        if(!battle||!battle.Fighting)return;
        for(int g=0;g<groupPolice.Length;g++)
        {
            var enemy=groupPolice[g];if(!enemy||enemy.Defeated||!enemy.escortTarget)continue;
            Vector3 ally=enemy.escortTarget.position;
            Vector3 away=Vector3.ProjectOnPlane(enemy.transform.position-ally,Vector3.up).normalized;
            if(away.sqrMagnitude<.01f)away=Vector3.forward;
            Vector3 destination=GroupMoving(g)&&g!=1?GroupTravel(g):ally+away*1.25f;
            if(enemy.Melee&&enemy.Melee.Swinging)continue;
            Vector3 before=enemy.transform.position;
            policeMotors[g].Move(enemy.actor,destination,enemy.LegWounded?1.25f:g==1?3.15f:2.6f);
            // The shared leader planner ignores actors. Combat must still
            // respect the bodies of its opponents at every actual step.
            if(Vector3.ProjectOnPlane(enemy.transform.position-ally,Vector3.up).sqrMagnitude<.85f*.85f&&
                Vector3.ProjectOnPlane(before-ally,Vector3.up).sqrMagnitude>.85f*.85f)
                enemy.transform.position=before;
        }
    }
    public void AssignPoliceOpponent(int index,Chapter3Enemy enemy)
    {
        if(index<0||index>=AmbusherCount)return;
        policeOpponents[index]=enemy;policeAssigned[index]=true;policeStrikeAt[index]=-1;policeStrikeLanded[index]=false;
    }
    void ClearPoliceOpposition()
    {
        groupPolice=null;groupCentres=null;
        for(int i=0;i<AmbusherCount;i++){policeOpponents[i]=null;policeAssigned[i]=false;policeStrikeAt[i]=-1;policeFlinchUntil[i]=0;}
    }
    public bool ReactToPoliceShot(Transform target)
    {
        for(int i=0;i<AmbusherCount;i++)if(policeAssigned[i]&&assailants[i]&&assailants[i].transform==target)
        {policeFlinchUntil[i]=Time.time+.65f;return true;}
        return false;
    }
    bool UpdatePoliceOpposition(int index,Chapter3SchoolCrowdActor actor)
    {
        if(!policeAssigned[index])return false;
        var enemy=policeOpponents[index];
        var battle=GetComponentInParent<Chapter3SchoolController>().Battle;
        if(!battle||battle.CurrentStage==Chapter3SchoolBattle.Stage.Down)
        {actor.SetStrike(0);return true;}
        if(!enemy||enemy.Defeated)
        {actor.SetStrike(0);actor.SetStartled(0);actor.SetAfraid(false);return true;}
        actor.HoldAtNeck(null,false);actor.SetAfraid(false);
        Vector3 direction=Vector3.ProjectOnPlane(actor.transform.position-enemy.transform.position,Vector3.up).normalized;
        if(direction.sqrMagnitude<.1f)direction=Vector3.left;
        int group=policeGroups[index],slot=policeSlots[index];
        Vector3 stand=enemy.transform.position+direction*1.3f;
        if(GroupMoving(group))
        {
            if(group==1)stand=GroupTravel(group);
            else if(slot>0)
                stand=enemy.transform.position+Quaternion.Euler(0,slot==1?125:-125,0)*
                    Vector3.ProjectOnPlane(assailants[group==2?7:8].transform.position-enemy.transform.position,Vector3.up).normalized*1.45f;
        }
        actor.MoveTowards(stand,group==1?2.85f:3.2f);
        if(group==1&&GroupMoving(group))
        {
            // Run toward the escape point during pursuit; do not turn the
            // runner back toward the officer and make him slide backwards.
            actor.SetStrike(0);policeStrikeAt[index]=-1;
            actor.SetStartled(Time.time<policeFlinchUntil[index]?.8f:0);return true;
        }
        actor.Face(enemy.transform.position,150);
        // Keep the knife up between exchanges; the shared arm pose provides
        // a guard, then yields to the actual attack swing when it starts.
        actor.SetStartled(Time.time<policeFlinchUntil[index]?.8f:enemy.Melee?(enemy.Melee.Swinging?.8f:.5f):0);
        float distance=Vector3.ProjectOnPlane(actor.transform.position-enemy.transform.position,Vector3.up).magnitude;
        if(distance>1.9f||Time.time<policeFlinchUntil[index]){actor.SetStrike(0);policeStrikeAt[index]=-1;return true;}
        if(policeStrikeAt[index]<0){policeStrikeAt[index]=Time.time;policeStrikeLanded[index]=false;}
        float elapsed=Time.time-policeStrikeAt[index];
        actor.SetStrike(elapsed<1.1f?Mathf.Clamp(elapsed/1.1f,.01f,.99f):0);
        // Allies contribute damage, but leave each officer for the player to
        // defeat. No hidden timed victories can unlock the retreat signal.
        if(elapsed>=.72f&&!policeStrikeLanded[index])
        {
            policeStrikeLanded[index]=true;
            if(enemy.Health>20)enemy.Hit(Mathf.Min(20,enemy.Health-20),actor.transform.position);
        }
        if(elapsed>(enemy.Melee?2.6f:3.8f))policeStrikeAt[index]=-1;
        return true;
    }
}
