using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// One guide retrieves the player wherever free exploration ended. The other two
// original workers remain witnesses, so the authored consequence can reuse them.
public sealed class Chapter2ForestEscort : MonoBehaviour
{
    public Chapter2Controller chapter;
    public float walkingSpeed=1.65f;
    public float waitDistance=5.5f;
    public bool Approaching { get; private set; }
    public bool Leading { get; private set; }
    public bool Waiting { get; private set; }
    public Vector3 MeetingPoint { get; private set; }
    public bool NavigationReady => data;
    NavMeshData data;
    NavMeshDataInstance instance;
    Collider ground;
    NavMeshPath query;

    public void Initialize(Chapter2Controller owner)
    {
        chapter=owner;
        var floor=GameObject.Find("Forest ground");
        ground=floor?floor.GetComponent<Collider>():null;
        for(int i=1;i<chapter.workers.Length;i++)
        {
            var actor=chapter.workers[i];
            var patrol=actor.GetComponent<Chapter2AmbientNPC>();
            if(!patrol)
            {
                Vector3 home=WitnessHome(i);home.y=actor.transform.position.y;
                actor.transform.position=home;
                patrol=actor.gameObject.AddComponent<Chapter2AmbientNPC>();
            }
            actor.name=i==1?"巨木右側族人":"巨木左側族人";
            patrol.chapter=chapter;
            patrol.patrolOffset=new Vector3(i==1?.8f:-.8f,0,-.4f);
            patrol.watchDirection=Vector3.back;
            patrol.pauseSeconds=5;patrol.phaseOffset=i*2;patrol.speed=.42f;
        }
    }
    public Vector3 WitnessHome(int index)=>chapter.sacredTree.position+new Vector3(index==1?3.8f:-3.8f,0,index==1?-2.2f:-1.7f);
    public void StopWitnessPatrols()
    {
        for(int i=1;i<chapter.workers.Length;i++)
        {
            var patrol=chapter.workers[i].GetComponent<Chapter2AmbientNPC>();
            if(patrol){patrol.ConversationPartner=null;patrol.enabled=false;}
            chapter.workers[i].Face(chapter.sacredTree.position);
        }
    }
    void PrepareNavigation()
    {
        if(data)return;
        Physics.SyncTransforms();
        var sources=new List<NavMeshBuildSource>();
        NavMeshBuilder.CollectSources((Transform)null,~0,NavMeshCollectGeometry.PhysicsColliders,0,new List<NavMeshBuildMarkup>(),sources);
        sources.RemoveAll(s=>s.component && (s.component.gameObject.scene!=gameObject.scene ||
            s.component.transform.IsChildOf(chapter.player.transform) ||
            s.component.transform.IsChildOf(chapter.nightGroup.transform) ||
            s.component.GetComponentInParent<Chapter2Actor>()));
        var settings=NavMesh.GetSettingsByIndex(0);
        settings.agentRadius=.32f;settings.agentHeight=1.8f;settings.agentClimb=.4f;settings.agentSlope=50;
        settings.overrideVoxelSize=true;settings.voxelSize=.12f;
        var bounds=ground?ground.bounds:new Bounds(Vector3.zero,new Vector3(160,40,160));bounds.Expand(4);
        data=NavMeshBuilder.BuildNavMeshData(settings,sources,bounds,Vector3.zero,Quaternion.identity);
        if(data)instance=NavMesh.AddNavMeshData(data);
    }
    float GroundAt(Vector3 position)
    {
        if(ground && ground.Raycast(new Ray(new Vector3(position.x,200,position.z),Vector3.down),out var hit,400))return hit.point.y;
        return 0;
    }
    bool Sample(Vector3 position,out Vector3 point,float radius=2)
    {
        position.y=GroundAt(position);
        bool found=NavMesh.SamplePosition(position,out var hit,radius,NavMesh.AllAreas);
        point=hit.position;return found;
    }
    public bool TryPath(Vector3 from,Vector3 to,out Vector3[] corners)
    {
        PrepareNavigation();corners=null;
        if(query==null)query=new NavMeshPath();
        if(!data || !Sample(from,out var start) || !Sample(to,out var end))return false;
        if(!NavMesh.CalculatePath(start,end,NavMesh.AllAreas,query) || query.status!=NavMeshPathStatus.PathComplete)return false;
        corners=query.corners;return corners.Length>0;
    }
    Vector3 InFrontOfPlayer()
    {
        var view=chapter.player.view.transform;
        Vector3 origin=view.position;origin.y=GroundAt(origin);
        Vector3 forward=Vector3.ProjectOnPlane(view.forward,Vector3.up).normalized;
        if(forward.sqrMagnitude<.01f)forward=chapter.player.transform.forward;
        Vector3 best=origin;float score=float.MaxValue;
        foreach(float angle in new[]{0f,-15,15,-30,30,-45,45})
            foreach(float distance in new[]{2.4f,1.8f,3.2f,1.2f})
            {
                var desired=origin+Quaternion.AngleAxis(angle,Vector3.up)*forward*distance;
                if(!Sample(desired,out var candidate,.6f))continue;
                Vector3 relative=Vector3.ProjectOnPlane(candidate-origin,Vector3.up);
                if(relative.magnitude<1 || Vector3.Angle(forward,relative)>48)continue;
                // A trunk must not separate the guide and the player's viewing position.
                if(Sample(origin,out var at) && NavMesh.Raycast(at,candidate,out _,NavMesh.AllAreas))continue;
                if(!TryPath(chapter.workers[0].transform.position,candidate,out _))continue;
                float value=Mathf.Abs(angle)*.07f+Mathf.Abs(distance-2.4f);
                if(value<score){score=value;best=candidate;}
            }
        // At a map edge the nearest reachable patch may be to the side. The
        // final view correction below still makes the guide visible, without a player warp.
        if(score==float.MaxValue && Sample(origin+forward*1.2f,out var fallback,4))best=fallback;
        return best;
    }
    public IEnumerator ComeToPlayer()
    {
        PrepareNavigation();Approaching=true;
        chapter.ui.objective.text="族人正來帶你前往巨木";
        var guide=chapter.workers[0];guide.Rig.BeginDepartureWalk(0);
        MeetingPoint=InFrontOfPlayer();
        yield return WalkTo(MeetingPoint,false);
        // Head tracking remains active in VR. Recheck if the viewer turned during approach.
        if(chapter.player.IsVR)
        {
            var updated=InFrontOfPlayer();
            if(Vector3.Distance(updated,MeetingPoint)>.8f){MeetingPoint=updated;yield return WalkTo(updated,false);}
        }
        guide.Face(chapter.player.view.transform.position);
        var head=guide.Rig.Head?guide.Rig.Head.position:guide.transform.position+Vector3.up*1.55f;
        var screen=chapter.player.view.WorldToViewportPoint(head);
        if(screen.z<=0 || screen.x<.12f || screen.x>.88f || screen.y<.18f || screen.y>.9f)
            chapter.player.FocusOn(head);
        Approaching=false;
    }
    public IEnumerator LeadToTree()
    {
        Leading=true;
        yield return WalkTo(chapter.WorkerDestination(0),true);
        Leading=Waiting=false;
    }
    IEnumerator WalkTo(Vector3 destination,bool waitForPlayer)
    {
        var guide=chapter.workers[0];
        Vector3[] path;
        while(!TryPath(guide.transform.position,destination,out path))yield return new WaitForSeconds(.5f);
        // Include the sampled first point for a smooth step onto the navigation surface.
        foreach(var point in path)
        {
            while(Vector3.ProjectOnPlane(guide.transform.position-point,Vector3.up).magnitude>.055f)
            {
                Waiting=waitForPlayer && Vector3.ProjectOnPlane(chapter.player.transform.position-guide.transform.position,Vector3.up).magnitude>waitDistance;
                if(waitForPlayer)chapter.ui.objective.text=Waiting?"族人正在等你，沿黃色箭頭跟上":"跟著族人，前往巨木";
                if(Waiting)guide.Face(chapter.player.transform.position);
                else
                {
                    var next=Vector3.MoveTowards(new Vector3(guide.transform.position.x,point.y,guide.transform.position.z),point,walkingSpeed*Time.deltaTime);
                    guide.GroundHeight=GroundAt(next);
                    guide.transform.position=new Vector3(next.x,guide.transform.position.y,next.z);
                    guide.Face(point);
                }
                yield return null;
            }
        }
        guide.GroundHeight=GroundAt(destination);Waiting=false;
    }
    void OnDestroy(){if(instance.valid)instance.Remove();if(data)Destroy(data);}
}
