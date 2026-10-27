using UnityEngine;

public sealed partial class Chapter3Combat
{
    readonly Chapter3PathMotor[] coverMotors={new Chapter3PathMotor(),new Chapter3PathMotor()};
    readonly Chapter3SchoolCompanionPose[] coverPoses=new Chapter3SchoolCompanionPose[2];
    public bool CompanionsInCover {get;private set;}
    public Vector3 CompanionCoverPoint(int index)=>WorldPoint(index==0?new Vector3(-3.65f,0,-3.8f):new Vector3(-2.7f,0,-3.8f));

    // The old rectangular collider included empty space above the sloping
    // stone. Use the visible static rock mesh for bullets and physical cover.
    void FitRockColliders()
    {
        foreach(var root in GetComponentsInChildren<Transform>(true))
        {
            if(root.name!="山路岩石掩體")continue;
            bool fitted=false;
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if(!filter.sharedMesh)continue;
                var mesh=filter.GetComponent<MeshCollider>();
                if(!mesh)mesh=filter.gameObject.AddComponent<MeshCollider>();
                mesh.sharedMesh=filter.sharedMesh;mesh.convex=false;mesh.enabled=true;fitted=true;
            }
            if(fitted)foreach(var box in root.GetComponents<BoxCollider>())box.enabled=false;
        }
    }
    void UpdateCompanionCover()
    {
        if(SchoolEncounter)return;
        var actors=new[]{bawan,pihu};CompanionsInCover=true;
        for(int i=0;i<actors.Length;i++)
        {
            var actor=actors[i];if(!actor)continue;
            if(!coverPoses[i])coverPoses[i]=actor.GetComponent<Chapter3SchoolCompanionPose>()??actor.gameObject.AddComponent<Chapter3SchoolCompanionPose>();
            var destination=CompanionCoverPoint(i);
            bool arrived=Flat(actor.transform.position,destination)<.18f;
            if(!arrived){coverMotors[i].Move(actor,destination,2.7f);CompanionsInCover=false;}
            else actor.Face(WorldPoint(new Vector3(0,1,12)));
            coverPoses[i].SetUnderFire(arrived);
        }
    }
    void ClearCompanionCover()
    {
        CompanionsInCover=false;
        for(int i=0;i<coverMotors.Length;i++){coverMotors[i].Reset();if(coverPoses[i])coverPoses[i].Clear();}
    }
}
