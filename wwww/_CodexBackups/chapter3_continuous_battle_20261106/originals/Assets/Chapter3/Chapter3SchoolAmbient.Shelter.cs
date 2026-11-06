using UnityEngine;

public sealed partial class Chapter3SchoolAmbient
{
    int[] shelterLeg;
    float[] shelterChanged;
    readonly int[] watchLeg=new int[AmbusherCount];
    readonly float[] watchChanged=new float[AmbusherCount];
    readonly Chapter3PathMotor[] watchMotors=new Chapter3PathMotor[AmbusherCount];
    static readonly Vector3[] shelterSteps={new Vector3(0,0,0),new Vector3(1.15f,0,.5f),new Vector3(-.7f,0,1.1f)};
    void MoveWithinShelter(int index,Vector3 shelter)
    {
        if(shelterLeg==null||shelterLeg.Length!=crowd.Length){shelterLeg=new int[crowd.Length];shelterChanged=new float[crowd.Length];}
        var guest=crowd[index];if(!guest||guest.IsFallen||Withdrawing)return;
        Vector3 target=shelter+shelterSteps[shelterLeg[index]%3];
        guest.SetAfraid(true);
        if(guest.MoveTowards(target,.75f+(index%3)*.1f)||Time.time-shelterChanged[index]>4)
        {shelterLeg[index]=(shelterLeg[index]+1)%3;shelterChanged[index]=Time.time;}
    }
    void WatchCampus(int index,Chapter3SchoolCrowdActor actor,Vector3 centre)
    {
        if(Withdrawing)return;
        Vector3 target=centre+shelterSteps[watchLeg[index]%3]*1.3f;
        if(watchMotors[index]==null)watchMotors[index]=new Chapter3PathMotor();
        if(Vector3.ProjectOnPlane(actor.transform.position-target,Vector3.up).sqrMagnitude<.25f*.25f||Time.time-watchChanged[index]>4)
        {watchLeg[index]=(watchLeg[index]+1)%3;watchChanged[index]=Time.time;watchMotors[index].Reset();target=centre+shelterSteps[watchLeg[index]%3]*1.3f;}
        watchMotors[index].Patrol(actor,target,.9f,.25f);
        actor.Face(new Vector3(0,0,29),70);
    }
}
