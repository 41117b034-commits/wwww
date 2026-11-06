using UnityEngine;

public sealed partial class Chapter3Combat
{
    readonly Chapter3PathMotor[] coverMotors={new Chapter3PathMotor(),new Chapter3PathMotor()};
    readonly Chapter3SchoolCompanionPose[] coverPoses=new Chapter3SchoolCompanionPose[2];
    public bool CompanionsInCover {get;private set;}
    Vector3 pihuTreeCover;
    Transform pihuCoverTree;
    public Transform PihuCoverTree=>pihuCoverTree;
    public Vector3 CompanionCoverPoint(int index)=>index==0?WorldPoint(new Vector3(-3.65f,0,-3.8f)):pihuTreeCover;

    // The old rectangular collider included empty space above the sloping
    // stone. Use the visible static rock mesh for bullets and physical cover.
    void FitRockColliders()
    {
        PrepareRightTreeCover();
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
    void PrepareRightTreeCover()
    {
        // Pihu starts on the right. Select a nearby right-side trunk and stay
        // on that side of the road instead of crossing the officers' lane.
        Vector3 start=WorldPoint(new Vector3(5.2f,0,-6));float best=float.MaxValue;
        foreach(var tree in GetComponentsInChildren<Transform>(true))
        {
            if(!tree.name.StartsWith("交戰道路林木 ")||tree.position.x<start.x)continue;
            float distance=Flat(start,tree.position);
            if(distance>=best||tree.position.z<start.z-1||tree.position.z>start.z+6)continue;
            best=distance;pihuCoverTree=tree;
        }
        if(!pihuCoverTree){pihuTreeCover=start;return;}
        var trunk=pihuCoverTree.Find("樹幹掩體碰撞");
        if(!trunk)
        {
            trunk=new GameObject("樹幹掩體碰撞").transform;trunk.SetParent(pihuCoverTree,false);
            // A world-size capsule follows the existing visible trunk. Do not
            // add collision to transparent foliage or enlarge the tree mesh.
            trunk.position=new Vector3(pihuCoverTree.position.x,0,pihuCoverTree.position.z);
            trunk.rotation=Quaternion.identity;
            var scale=trunk.lossyScale;trunk.localScale=Vector3.Scale(trunk.localScale,new Vector3(1/scale.x,1/scale.y,1/scale.z));
            var capsule=trunk.gameObject.AddComponent<CapsuleCollider>();capsule.radius=.42f;capsule.height=4;capsule.center=Vector3.up*2;
        }
        Vector3 threat=WorldPoint(new Vector3(0,0,12));
        Vector3 away=Vector3.ProjectOnPlane(trunk.position-threat,Vector3.up).normalized;
        pihuTreeCover=trunk.position+away*.92f;pihuTreeCover.y=0;
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
