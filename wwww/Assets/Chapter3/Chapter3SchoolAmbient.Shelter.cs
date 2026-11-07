using UnityEngine;

public sealed partial class Chapter3SchoolAmbient
{
    readonly bool[] sheltered=new bool[3];
    readonly Chapter3PathMotor[] watchMotors=new Chapter3PathMotor[AmbusherCount];
    readonly Chapter3PathMotor[] childShelterMotors={new Chapter3PathMotor(),new Chapter3PathMotor(),new Chapter3PathMotor()};
    readonly int[] childShelterLeg=new int[3];
    static readonly Vector3[] childShelters={new Vector3(-19.5f,0,46.3f),new Vector3(-18.2f,0,46.3f),new Vector3(-16.9f,0,46.3f)};
    static readonly Vector3[][] childShelterRoutes={
        new[]{new Vector3(-20,0,41.5f),new Vector3(-20,0,45.9f),childShelters[0]},
        new[]{new Vector3(-20,0,41.5f),new Vector3(-20,0,45.9f),childShelters[1]},
        new[]{new Vector3(-19,0,23),new Vector3(-19.5f,0,36),new Vector3(-20,0,45.9f),childShelters[2]}};
    Chapter3PathMotor[] civilianPanicMotors;
    int[] civilianPanicLeg;
    Vector3[][] civilianPanicRoutes;
    Collider[] corridorFloors;
    public bool CivilianHidesInCorridor(int index)=>index>=0&&index<spectators.Length&&index%2==0;
    public Vector3 CivilianRefuge(int index)=>civilianPanicRoutes[index][civilianPanicRoutes[index].Length-1];
    public int CiviliansAtRefuge
    {get{int count=0;if(civilianPanicLeg!=null)for(int i=0;i<civilianPanicLeg.Length;i++)if(civilianPanicLeg[i]>=civilianPanicRoutes[i].Length)count++;return count;}}
    void ResetCivilianShelters()
    {
        civilianPanicMotors=new Chapter3PathMotor[crowd.Length];
        civilianPanicLeg=new int[crowd.Length];civilianPanicRoutes=new Vector3[crowd.Length][];
        for(int i=0;i<crowd.Length;i++)
        {
            civilianPanicMotors[i]=new Chapter3PathMotor();
            // Ten nearby refuges and ten longer escape paths lead to separate
            // spaces under the classroom veranda, not the middle of the track.
            float x=-15+(i/2)*3+(i%2)*1.2f;
            Vector3 refuge=new Vector3(x,0,46.3f);
            float side=homes[i].x<0?-1:1;
            civilianPanicRoutes[i]=CivilianHidesInCorridor(i)
                ?new[]{new Vector3(x,0,42.2f),refuge}
                :new[]{new Vector3(side*19.2f,0,Mathf.Clamp(homes[i].z+9,20,37)),
                    new Vector3(side*19.2f,0,41.5f),new Vector3(x,0,44),refuge};
        }
        for(int i=0;i<childShelterLeg.Length;i++){childShelterLeg[i]=0;childShelterMotors[i].Reset();}
    }
    internal float CorridorGroundHeight(Vector3 point)
    {
        if(corridorFloors==null)
        {
            var floors=new System.Collections.Generic.List<Collider>();
            var school=GetComponentInParent<Chapter3SchoolController>();
            if(school)foreach(var floor in school.GetComponentsInChildren<Collider>(true))
                if(floor.name=="外廊木地板")floors.Add(floor);
            corridorFloors=floors.ToArray();
        }
        float height=0;
        foreach(var floor in corridorFloors)if(floor&&floor.enabled&&floor.gameObject.activeInHierarchy)
        {
            var bounds=floor.bounds;
            if(point.x>=bounds.min.x&&point.x<=bounds.max.x&&point.z>=bounds.min.z&&point.z<=bounds.max.z)
                height=Mathf.Max(height,bounds.max.y);
        }
        return height;
    }
    void UpdateCivilianPanic(int index)
    {
        var actor=crowd[index];var route=civilianPanicRoutes[index];
        actor.SetAfraid(true);actor.SetStartled(0);actor.SetCowering(false);actor.SetWaiting(false);
        int leg=civilianPanicLeg[index];
        if(leg<route.Length&&Horizontal(actor.transform.position,route[leg])<.38f)
        {civilianPanicLeg[index]=++leg;civilianPanicMotors[index].Reset();}
        if(leg<route.Length)
        {
            crowdPhases[index]=CrowdPhase.Fleeing;
            civilianPanicMotors[index].Patrol(actor,route[leg],CivilianHidesInCorridor(index)?2.7f:3.2f,.30f);
            return;
        }
        crowdPhases[index]=CrowdPhase.Sheltered;
        actor.SetCowering(CivilianHidesInCorridor(index));
        actor.SetWaiting(!CivilianHidesInCorridor(index));
        actor.Face(PanicOrigin,65);
    }
    void ShelterNativeChild(int index,Chapter3SchoolCrowdActor actor)
    {
        actor.SetAfraid(true);
        // Two children head directly to cover; the third panics along the
        // side aisle first. None is placed at the destination or loops in place.
        Vector3[] route=childShelterRoutes[index];
        int leg=childShelterLeg[index];
        if(leg<route.Length&&Horizontal(actor.transform.position,route[leg])<.38f)
        {childShelterLeg[index]=++leg;childShelterMotors[index].Reset();}
        sheltered[index]=leg>=route.Length;
        if(!sheltered[index])childShelterMotors[index].Patrol(actor,route[leg],index<2?2.7f:3.2f,.3f);
        actor.SetCowering(sheltered[index]&&index<2);
        actor.SetWaiting(sheltered[index]&&index==2);
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
        if(Horizontal(actor.transform.position,target)>.35f)watchMotors[index].Patrol(actor,target,Chapter3Player.WalkSpeed,.3f);
    }
}
