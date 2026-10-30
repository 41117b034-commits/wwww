using UnityEngine;

// Contact-driven exchanges for the original seven fighters. Retreat still
// owns all ten original participants after the player's whistle.
public sealed partial class Chapter3SchoolAmbient
{
    readonly Chapter3Enemy[] policeOpponents=new Chapter3Enemy[AmbusherCount];
    readonly float[] policeStrikeAt=new float[AmbusherCount],policeFlinchUntil=new float[AmbusherCount],policeNextStrike=new float[AmbusherCount];
    readonly bool[] policeStrikeLanded=new bool[AmbusherCount],policeAssigned=new bool[AmbusherCount];
    readonly int[] policeGroups=new int[AmbusherCount],policeSlots=new int[AmbusherCount],policeExchanges=new int[AmbusherCount];
    static readonly int[][] fightingGroups={new[]{6},new[]{9},new[]{7,3},new[]{8,4,5}};
    Chapter3Enemy[] groupPolice;
    Vector3[] groupCentres;
    readonly Chapter3PathMotor[] policeMotors={new Chapter3PathMotor(),new Chapter3PathMotor(),new Chapter3PathMotor(),new Chapter3PathMotor()};
    public void ConfigurePoliceGroups(Chapter3Enemy[] enemies,Vector3[] centres)
    {
        ClearPoliceOpposition();groupPolice=enemies;groupCentres=centres;
        for(int g=0;g<fightingGroups.Length;g++)
        {
            policeMotors[g].Reset();
            for(int slot=0;slot<fightingGroups[g].Length;slot++)
            {int a=fightingGroups[g][slot];AssignPoliceOpponent(a,enemies[g]);policeGroups[a]=g;policeSlots[a]=slot;policeNextStrike[a]=Time.time+.7f+slot*.55f+g*.13f;}
        }
    }
    public Transform PoliceTarget(int group)
    {
        if(groupPolice==null||group>=groupPolice.Length)return null;
        var enemy=groupPolice[group];if(!enemy)return null;
        if(enemy.Melee&&enemy.Melee.Swinging&&enemy.escortTarget)return enemy.escortTarget;
        int selected=fightingGroups[group][0];float score=float.PositiveInfinity;
        foreach(int i in fightingGroups[group])
        {
            if(!assailants[i])continue;
            float distance=Vector3.Distance(enemy.transform.position,assailants[i].transform.position);
            // Turn toward an actual incoming attack, then keep that opponent
            // through the cut instead of snapping between nearby targets.
            if(policeStrikeAt[i]>=0)distance-=1.2f;
            if(enemy.escortTarget==assailants[i].transform)distance-=.25f;
            if(distance<score){selected=i;score=distance;}
        }
        return assailants[selected]?assailants[selected].transform:null;
    }
    void UpdatePoliceMovement()
    {
        if(groupPolice==null||Withdrawing)return;
        var battle=GetComponentInParent<Chapter3SchoolController>().Battle;
        if(!battle||!battle.Fighting)return;
        for(int g=0;g<groupPolice.Length;g++)
        {
            var enemy=groupPolice[g];if(!enemy||enemy.Defeated||!enemy.escortTarget)continue;
            Vector3 current=enemy.transform.position,ally=enemy.escortTarget.position;
            Vector3 away=Vector3.ProjectOnPlane(current-ally,Vector3.up).normalized;
            if(away.sqrMagnitude<.01f)away=Vector3.forward;
            var melee=enemy.Melee;float distance=Vector3.ProjectOnPlane(current-ally,Vector3.up).magnitude;
            float reach=melee&&melee.Swinging?(melee.Recovering?1.55f:1.12f):1.38f;
            Vector3 destination=ally+away*reach;
            if(melee&&!melee.Swinging&&distance<1.8f)
            {
                // Short lateral guard steps keep opponents facing each other;
                // each cut/recovery determines the footwork, never a patrol timer.
                float side=melee.AttackVariant==1?-1:1;
                destination+=Vector3.Cross(Vector3.up,away)*(.22f*side);
            }
            float speed=enemy.LegWounded?1.1f:distance>2.1f?2.5f:melee&&melee.Swinging?.65f:.85f;
            policeMotors[g].Move(enemy.actor,destination,speed,.10f);
            foreach(int i in fightingGroups[g])
            {
                Vector3 body=assailants[i].transform.position;
                if(Vector3.ProjectOnPlane(enemy.transform.position-body,Vector3.up).sqrMagnitude<.9f*.9f&&
                    Vector3.ProjectOnPlane(enemy.transform.position-body,Vector3.up).sqrMagnitude<Vector3.ProjectOnPlane(current-body,Vector3.up).sqrMagnitude)
                {enemy.transform.position=current;break;}
            }
        }
    }
    public void AssignPoliceOpponent(int index,Chapter3Enemy enemy)
    {
        if(index<0||index>=AmbusherCount)return;
        policeOpponents[index]=enemy;policeAssigned[index]=true;policeStrikeAt[index]=-1;policeStrikeLanded[index]=false;policeExchanges[index]=0;
    }
    void ClearPoliceOpposition()
    {
        groupPolice=null;groupCentres=null;
        for(int i=0;i<AmbusherCount;i++){policeOpponents[i]=null;policeAssigned[i]=false;policeStrikeAt[i]=-1;policeFlinchUntil[i]=0;if(assailants[i])assailants[i].SetMeleePose(0,0);}
    }
    public bool ReactToPoliceShot(Transform target)
    {
        for(int i=0;i<AmbusherCount;i++)if(policeAssigned[i]&&assailants[i]&&assailants[i].transform==target)
        {
            // Most guarded contacts are caught on the raised blade; an ally
            // caught mid-cut visibly recoils and loses that attack.
            if(policeStrikeAt[i]<0){assailants[i].SetMeleePose(1,policeExchanges[i]%3);policeNextStrike[i]=Time.time+.22f;}
            else {policeFlinchUntil[i]=Time.time+.45f;policeStrikeAt[i]=-1;policeNextStrike[i]=Time.time+.65f;}
            return true;
        }
        return false;
    }
    bool UpdatePoliceOpposition(int index,Chapter3SchoolCrowdActor actor)
    {
        if(!policeAssigned[index])return false;
        var enemy=policeOpponents[index];var battle=GetComponentInParent<Chapter3SchoolController>().Battle;
        if(!battle||!battle.Fighting||!enemy||enemy.Defeated)
        {actor.SetStrike(0);actor.SetMeleePose(0,0);actor.SetStartled(0);return true;}
        actor.HoldAtNeck(null,false);actor.SetAfraid(false);
        Vector3 current=actor.transform.position,direction=Vector3.ProjectOnPlane(current-enemy.transform.position,Vector3.up).normalized;
        if(direction.sqrMagnitude<.1f)direction=Vector3.left;
        int group=policeGroups[index],slot=policeSlots[index];var melee=enemy.Melee;
        bool threatened=melee&&melee.Swinging&&enemy.escortTarget==actor.transform;
        bool flinching=Time.time<policeFlinchUntil[index];
        float reach=flinching?1.8f:threatened?1.65f:policeStrikeAt[index]>=0?1.13f:1.36f;
        Vector3 stand=enemy.transform.position+direction*reach;
        if(slot>0&&policeStrikeAt[index]<0)
        {
            Vector3 lead=Vector3.ProjectOnPlane(assailants[fightingGroups[group][0]].transform.position-enemy.transform.position,Vector3.up).normalized;
            stand=enemy.transform.position+Quaternion.Euler(0,slot==1?105:-105,0)*lead*1.45f;
        }
        float distance=Vector3.ProjectOnPlane(current-enemy.transform.position,Vector3.up).magnitude;
        actor.MoveTowards(stand,distance>2.1f?2.6f:flinching?1.25f:.8f,false,.1f);
        actor.Face(enemy.transform.position,160);
        actor.SetStartled(flinching?.7f:0);actor.SetMeleePose(threatened?1:.5f,policeExchanges[index]%3);
        if(distance>1.9f||flinching){actor.SetStrike(0);policeStrikeAt[index]=-1;return true;}
        if(policeStrikeAt[index]<0)
        {
            actor.SetStrike(0);
            if(Time.time<policeNextStrike[index]||(threatened&&!melee.Recovering))return true;
            policeStrikeAt[index]=Time.time;policeStrikeLanded[index]=false;policeExchanges[index]++;
        }
        float elapsed=Time.time-policeStrikeAt[index];
        actor.SetStrike(Mathf.Clamp(elapsed/.85f,.01f,.99f));
        if(elapsed>=.48f&&!policeStrikeLanded[index])
        {
            policeStrikeLanded[index]=true;
            // A contact must be in range and unobstructed at impact. Parry,
            // counter and hit reactions continue even at the ally damage floor.
            if(Vector3.Distance(actor.transform.position,enemy.transform.position)<1.9f&&
                enemy.combat.ClearLine(enemy.AimPoint,actor.transform.position+Vector3.up*1.05f,enemy))
            {
                bool parried=melee&&melee.ParryAlly();
                if(!parried&&enemy.Health>20)enemy.Hit(Mathf.Min(12,enemy.Health-20),actor.transform.position);
            }
        }
        if(elapsed>=.85f)
        {actor.SetStrike(0);policeStrikeAt[index]=-1;policeNextStrike[index]=Time.time+.3f+(policeExchanges[index]%3)*.17f+slot*.15f;}
        return true;
    }
}
