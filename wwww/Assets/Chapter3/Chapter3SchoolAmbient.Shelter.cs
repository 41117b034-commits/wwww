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
    Vector3[] civilianCorridorEntries;
    int[] civilianCorridorLeg;
    bool[] civilianPassedCorridor;
    float[] civilianDestinationChecks;
    Collider[] corridorFloors;
    public bool CivilianHidesInCorridor(int index)=>false;
    public Vector3 CivilianRefuge(int index)=>civilianCorridorEntries[index];
    public int CiviliansPassedCorridorCount
    {get{int count=0;if(civilianPassedCorridor!=null)foreach(bool passed in civilianPassedCorridor)if(passed)count++;return count;}}
    public int CiviliansAtRefuge
    {get{int count=0;if(civilianPassedCorridor!=null)for(int i=0;i<crowd.Length;i++)if(civilianPassedCorridor[i]&&crowd[i]&&Horizontal(crowd[i].transform.position,civilianCorridorEntries[i])<2)count++;return count;}}
    void ResetCivilianShelters()
    {
        civilianPanicMotors=new Chapter3PathMotor[crowd.Length];
        civilianPanicLeg=new int[crowd.Length];civilianPanicRoutes=new Vector3[crowd.Length][];
        civilianCorridorEntries=new Vector3[crowd.Length];civilianCorridorLeg=new int[crowd.Length];civilianPassedCorridor=new bool[crowd.Length];
        civilianDestinationChecks=new float[crowd.Length];
        for(int i=0;i<crowd.Length;i++)
        {
            civilianPanicMotors[i]=new Chapter3PathMotor();
            // Enter the veranda on scattered approaches, then keep running
            // through the east opening to safety. The corridor is a passage,
            // never a row of permanently crouching or waiting civilians.
            float x=Mathf.Clamp(homes[i].x*.75f+(i%3-1)*1.1f,-15,14);
            Vector3 refuge=civilianCorridorEntries[i]=new Vector3(x,0,46.3f);
            float side=homes[i].x<0?-1:1;
            var route=new System.Collections.Generic.List<Vector3>();
            if(i%3==1)
            {route.Add(new Vector3(side*19.2f,0,Mathf.Clamp(homes[i].z+9,20,37)));route.Add(new Vector3(side*19.2f,0,41.5f));}
            route.Add(new Vector3(x,0,43.4f));civilianCorridorLeg[i]=route.Count;route.Add(refuge);
            route.Add(new Vector3(17.3f,0,46.3f));route.Add(new Vector3(18,0,44.8f));
            route.Add(new Vector3(24.2f,0,45.4f));route.Add(new Vector3(24.2f,0,61));
            // A scattered safe area east of the school is separate from the
            // Indigenous party's forest approach and roll-call positions.
            route.Add(CivilianSafeDestination(i));
            civilianPanicRoutes[i]=route.ToArray();
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
        if(leg<route.Length)
        {
            Vector3 current=actor.transform.position;
            // A waiting participant can occupy the precise approach point,
            // while a veranda post blocks the straight line to the next one.
            // Once close to an intermediate corner, let the next actual path
            // search go around both. Every walking step still checks collision;
            // corridor entry and the final refuge retain their tight arrival.
            bool canTurn=leg<route.Length-1&&leg!=civilianCorridorLeg[index]&&Horizontal(current,route[leg])<1.1f;
            if(Horizontal(current,route[leg])<.4f||canTurn)
            {
                if(leg==civilianCorridorLeg[index])civilianPassedCorridor[index]=true;
                civilianPanicLeg[index]=++leg;civilianPanicMotors[index].Reset();
            }
        }
        if(leg<route.Length)
        {
            crowdPhases[index]=CrowdPhase.Fleeing;
            if(leg==route.Length-1&&Time.time>=civilianDestinationChecks[index])
            {
                civilianDestinationChecks[index]=Time.time+1;
                // The scattered destination is not guaranteed to be outside
                // authored rocks or already settled people. Resolve an open
                // standing place, then walk there normally before counting it.
                Vector3 target=OpenCivilianDestination(index,actor,route[leg]);
                if((target-route[leg]).sqrMagnitude>.001f)
                {route[leg]=target;civilianPanicMotors[index].Reset();}
            }
            civilianPanicMotors[index].Patrol(actor,route[leg],3.0f+(index%3)*.18f,.25f);
            return;
        }
        crowdPhases[index]=CrowdPhase.Escaped;
        actor.SetCowering(false);actor.SetWaiting(true);
        actor.Face(PanicOrigin,65);
    }
    static Vector3 CivilianSafeDestination(int index)
    {
        float angle=(index*137.5f)*Mathf.Deg2Rad,radius=2.1f+(index%4)*1.05f;
        return new Vector3(32+Mathf.Cos(angle)*radius,0,64+Mathf.Sin(angle)*radius);
    }
    static Vector3 OpenCivilianDestination(int index,Chapter3SchoolCrowdActor actor,Vector3 target)
    {
        if(Chapter3PathMotor.ClearCrowdPoint(target,actor))return target;
        // Keep alternatives anchored to the intended refuge, rather than
        // repeatedly pushing a blocked destination farther into the scenery.
        Vector3 anchor=CivilianSafeDestination(index);
        Vector3 approach=Vector3.ProjectOnPlane(actor.transform.position-anchor,Vector3.up).normalized;
        if(approach.sqrMagnitude<.1f)approach=Vector3.back;
        for(int ring=1;ring<=4;ring++)for(int turn=0;turn<8;turn++)
        {
            Vector3 candidate=anchor+Quaternion.Euler(0,turn*45,0)*approach*(ring*.8f);
            if(Chapter3PathMotor.ClearCrowdPoint(candidate,actor))return candidate;
        }
        return target;
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
        actor.SetCowering(false);
        actor.SetWaiting(sheltered[index]);
        if(sheltered[index])actor.Face(new Vector3(-7,0,25),65);
    }
    void MoveWithinShelter(int index,Vector3 shelter)
    {
        var guest=crowd[index];if(!guest||guest.IsFallen||Withdrawing)return;
        guest.SetAfraid(true);
        // One refuge, not an endlessly cycling triangle around the refuge.
        bool arrived=Horizontal(guest.transform.position,shelter)<.65f;
        if(!arrived)arrived=guest.MoveTowards(shelter,1.8f,true,.5f);
        guest.SetCowering(false);guest.SetWaiting(arrived);
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
