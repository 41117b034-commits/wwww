using System.Collections.Generic;
using UnityEngine;

// Flat, authored Chapter 3 routes. Sweeps protect each step; a small local grid
// routes around rails and cover instead of cutting through them at corners.
public sealed class Chapter3PathMotor
{
    const float Cell=.5f, Radius=.36f;
    readonly List<Vector3> path=new List<Vector3>();
    Vector3 goal;
    float nextPlan;
    public void Reset(){path.Clear();nextPlan=0;}
    public void Move(Chapter2Actor actor,Vector3 target,float speed)
    {
        target.y=0;Vector3 start=actor.transform.position;start.y=0;
        if((target-start).sqrMagnitude<.0064f)return;
        if(Clear(start,target)){path.Clear();path.Add(target);goal=target;}
        else if(Time.time>=nextPlan&&(path.Count==0||(target-goal).sqrMagnitude>.64f))
        {Plan(start,target);goal=target;nextPlan=Time.time+.4f;}
        while(path.Count>1&&(path[0]-start).sqrMagnitude<.04f)path.RemoveAt(0);
        if(path.Count==0)return;
        for(int i=path.Count-1;i>0;i--)if(Clear(start,path[i])){path.RemoveRange(0,i);break;}
        Vector3 step=Vector3.MoveTowards(start,path[0],speed*Time.deltaTime);
        if(!Clear(start,step)){path.Clear();return;}
        actor.transform.position=step;actor.GroundHeight=0;
        if((step-start).sqrMagnitude>.000001f)actor.Face(step+(step-start).normalized);
    }
    static bool Obstacle(Collider collider)
    {
        if(collider is CharacterController||collider.GetComponentInParent<Chapter2Actor>())return false;
        // The recalled ally follows behind Tado with an explicit separation
        // rule. Treating that moving companion as a wall traps both at the
        // first shared waypoint. Other crowd members remain path obstacles.
        var crowd=collider.GetComponentInParent<Chapter3SchoolCrowdActor>();
        if(crowd&&crowd.IsRouteCompanion)return false;
        return collider.bounds.max.y>.28f;
    }
    public static bool Clear(Vector3 from,Vector3 to)
    {
        from.y=to.y=0;Vector3 delta=to-from;
        foreach(var c in Physics.OverlapCapsule(to+Vector3.up*.42f,to+Vector3.up*1.25f,Radius,~0,QueryTriggerInteraction.Ignore))
            if(Obstacle(c))return false;
        if(delta.sqrMagnitude<.000001f)return true;
        foreach(var h in Physics.CapsuleCastAll(from+Vector3.up*.42f,from+Vector3.up*1.25f,Radius,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore))
            if(Obstacle(h.collider))return false;
        return true;
    }
    void Plan(Vector3 start,Vector3 target)
    {
        path.Clear();
        Vector3 origin=new Vector3(Mathf.Floor(Mathf.Min(start.x,target.x)-4),0,Mathf.Floor(Mathf.Min(start.z,target.z)-4));
        int width=Mathf.CeilToInt((Mathf.Max(start.x,target.x)+4-origin.x)/Cell)+1;
        int height=Mathf.CeilToInt((Mathf.Max(start.z,target.z)+4-origin.z)/Cell)+1;
        Vector2Int Grid(Vector3 p)=>new Vector2Int(Mathf.RoundToInt((p.x-origin.x)/Cell),Mathf.RoundToInt((p.z-origin.z)/Cell));
        Vector3 World(Vector2Int p)=>origin+new Vector3(p.x*Cell,0,p.y*Cell);
        var first=Grid(start);var last=Grid(target);
        var open=new List<Vector2Int>{first};var closed=new HashSet<Vector2Int>();
        var cost=new Dictionary<Vector2Int,float>{{first,0}};var parent=new Dictionary<Vector2Int,Vector2Int>();
        for(int iteration=0;open.Count>0&&iteration<6000;iteration++)
        {
            int best=0;float score=float.MaxValue;
            for(int i=0;i<open.Count;i++){float f=cost[open[i]]+Vector2Int.Distance(open[i],last);if(f<score){score=f;best=i;}}
            var node=open[best];open.RemoveAt(best);closed.Add(node);
            Vector3 here=node==first?start:World(node);
            if(Vector3.Distance(here,target)<.8f&&Clear(here,target))
            {
                path.Add(target);
                while(node!=first){path.Insert(0,World(node));node=parent[node];}
                return;
            }
            for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)
            {
                if(x==0&&z==0)continue;
                var n=node+new Vector2Int(x,z);
                if(n.x<0||n.y<0||n.x>=width||n.y>=height||closed.Contains(n))continue;
                float value=cost[node]+(x==0||z==0?1:1.414214f);
                if(cost.TryGetValue(n,out float old)&&value>=old)continue;
                if(!Clear(here,World(n)))continue;
                cost[n]=value;parent[n]=node;if(!open.Contains(n))open.Add(n);
            }
        }
        // No safe route: wait, never teleport through an obstacle.
    }
}
