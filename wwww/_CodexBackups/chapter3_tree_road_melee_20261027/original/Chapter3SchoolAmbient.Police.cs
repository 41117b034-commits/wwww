using UnityEngine;

// Four of the existing ten participants engage armed police on Tado's route.
// Their final withdrawal remains owned by the original all-ten system.
public sealed partial class Chapter3SchoolAmbient
{
    readonly Chapter3Enemy[] policeOpponents=new Chapter3Enemy[AmbusherCount];
    readonly float[] policeStrikeAt=new float[AmbusherCount],policeFlinchUntil=new float[AmbusherCount];
    readonly bool[] policeStrikeLanded=new bool[AmbusherCount],policeAssigned=new bool[AmbusherCount];
    public void AssignPoliceOpponent(int index,Chapter3Enemy enemy)
    {
        if(index<0||index>=AmbusherCount)return;
        policeOpponents[index]=enemy;policeAssigned[index]=true;policeStrikeAt[index]=-1;policeStrikeLanded[index]=false;
    }
    void ClearPoliceOpposition()
    {
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
        Vector3 stand=enemy.transform.position+direction*1.55f;
        actor.MoveTowards(stand,2.4f);
        actor.Face(enemy.transform.position,150);
        actor.SetStartled(Time.time<policeFlinchUntil[index]?.8f:0);
        float distance=Vector3.ProjectOnPlane(actor.transform.position-enemy.transform.position,Vector3.up).magnitude;
        if(distance>2.1f||Time.time<policeFlinchUntil[index]){actor.SetStrike(0);return true;}
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
        if(elapsed>3.8f)policeStrikeAt[index]=-1;
        return true;
    }
}
