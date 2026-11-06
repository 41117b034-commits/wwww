using UnityEngine;

public sealed partial class Chapter3SchoolAmbient
{
    readonly bool[] sheltered=new bool[3];
    readonly Chapter3PathMotor[] watchMotors=new Chapter3PathMotor[AmbusherCount];
    static readonly Vector3[] childShelters={new Vector3(-20.5f,0,17),new Vector3(-20.5f,0,18.2f),new Vector3(-20.5f,0,19.4f)};
    void ShelterNativeChild(int index,Chapter3SchoolCrowdActor actor)
    {
        actor.SetAfraid(true);
        if(!sheltered[index])sheltered[index]=actor.MoveTowards(childShelters[index],Chapter3Player.WalkSpeed,true,.32f);
        actor.SetCowering(sheltered[index]);
        if(sheltered[index])actor.Face(new Vector3(-7,0,25),65);
    }
    void MoveWithinShelter(int index,Vector3 shelter)
    {
        var guest=crowd[index];if(!guest||guest.IsFallen||Withdrawing)return;
        guest.SetAfraid(true);
        // One refuge, not an endlessly cycling triangle around the refuge.
        bool arrived=Horizontal(guest.transform.position,shelter)<.65f;
        if(!arrived)arrived=guest.MoveTowards(shelter,1.8f,true,.5f);
        guest.SetCowering(arrived);
    }
    void WatchCampus(int index,Chapter3SchoolCrowdActor actor,Vector3 centre)
    {
        if(Withdrawing)return;
        // A defeated opponent releases this fighter toward the exit aisle.
        Vector3 target=new Vector3(15.2f+(index%2)*1.3f,0,31+(index/2)*1.3f);
        if(watchMotors[index]==null)watchMotors[index]=new Chapter3PathMotor();
        if(Horizontal(actor.transform.position,target)>.35f)watchMotors[index].Move(actor,target,Chapter3Player.WalkSpeed,.3f);
    }
}
