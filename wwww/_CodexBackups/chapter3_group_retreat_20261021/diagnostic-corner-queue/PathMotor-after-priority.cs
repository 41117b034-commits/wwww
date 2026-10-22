using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;

// Same flat authored-route grid and collision rules; search progresses only
// when its owner calls Move. No coroutine, worker, or global actor registry.
public sealed class Chapter3PathMotor
{
    static readonly ProfilerMarker MoveMarker=new ProfilerMarker("Chapter3.PathMotor.Move");
    static readonly ProfilerMarker PlanMarker=new ProfilerMarker("Chapter3.PathMotor.Plan");
    const float Cell=.5f, Radius=.36f;
    const int MaxSearchNodes=6000, NodesPerFrame=32, ShortcutChecksPerFrame=32;
    const float RetargetDistanceSquared=.64f;
    readonly List<Vector3> path=new List<Vector3>();
    readonly List<Vector2Int> open=new List<Vector2Int>();
    readonly HashSet<Vector2Int> closed=new HashSet<Vector2Int>();
    readonly Dictionary<Vector2Int,float> cost=new Dictionary<Vector2Int,float>();
    readonly Dictionary<Vector2Int,Vector2Int> parent=new Dictionary<Vector2Int,Vector2Int>();
    Vector3 goal,searchStart,searchTarget,origin;
    Vector2Int first,last;
    Chapter2Actor boundActor;
    int width,height,iterations;
    bool searching;
    float nextPlan;
    // Reset cancels navigation, but never grants another work quota in the
    // same frame. Multiple Move calls or repeated Reset cannot bypass the cap.
    int budgetFrame=-1,nodesThisFrame,shortcutChecksThisFrame;

    public void Reset()
    {
        path.Clear();CancelSearch();goal=Vector3.zero;nextPlan=0;boundActor=null;
    }

    void CancelSearch()
    {
        searching=false;open.Clear();closed.Clear();cost.Clear();parent.Clear();
        searchStart=searchTarget=origin=Vector3.zero;first=last=default;
        width=height=iterations=0;
    }

    void PrepareFrameBudget()
    {
        if(budgetFrame==Time.frameCount)return;
        budgetFrame=Time.frameCount;nodesThisFrame=shortcutChecksThisFrame=0;
    }

    public void Move(Chapter2Actor actor,Vector3 target,float speed,float stoppingDistance=.08f)
    {
        using var sample=MoveMarker.Auto();
        if(!actor||!actor.gameObject.activeInHierarchy){Reset();return;}
        if(boundActor!=actor){Reset();boundActor=actor;}
        PrepareFrameBudget();
        target.y=0;Vector3 start=actor.transform.position;float groundedY=start.y;start.y=0;
        // Moving formation slots need a smaller tolerance than fixed goals;
        // otherwise a rear companion alternates idle and walk every few frames.
        stoppingDistance=Mathf.Max(.001f,stoppingDistance);
        if((target-start).sqrMagnitude<stoppingDistance*stoppingDistance)
        {path.Clear();CancelSearch();goal=target;nextPlan=0;return;}

        // Explicit retargeting/repositioning cannot publish an old search.
        // Small moving-target adjustments retain the original search snapshot.
        if(searching&&((target-searchTarget).sqrMagnitude>RetargetDistanceSquared||
            (start-searchStart).sqrMagnitude>RetargetDistanceSquared))
        {CancelSearch();path.Clear();nextPlan=0;}
        if(!searching&&path.Count>0&&((target-goal).sqrMagnitude>RetargetDistanceSquared||
            (path.Count==1&&(path[0]-start).sqrMagnitude<.04f)))
        {path.Clear();nextPlan=0;}

        if(Clear(start,target))
        {
            // A door/crowd move can make the direct route clear while searching.
            CancelSearch();path.Clear();path.Add(target);goal=target;
        }
        else if(searching||(path.Count==0&&Time.time>=nextPlan))
        {
            bool found=Plan(start,target);
            if(searching||path.Count==0)return;
            if(found&&!RetargetPathEnd(start,target))
            {
                // A tiny target drift is accepted only if its new final edge is
                // clear now. Otherwise retry from the latest target next frame.
                path.Clear();nextPlan=0;return;
            }
        }

        while(path.Count>1&&(path[0]-start).sqrMagnitude<.04f)path.RemoveAt(0);
        if(path.Count==0)return;
        // A long reconstructed path must not move the original search spike
        // into an unbounded visibility scan. The retained path remains valid;
        // fewer shortcuts may be taken, but authored waypoints are unchanged.
        for(int i=path.Count-1;i>0&&shortcutChecksThisFrame<ShortcutChecksPerFrame;i--)
        {
            shortcutChecksThisFrame++;
            if(Clear(start,path[i])){path.RemoveRange(0,i);break;}
        }
        Vector3 step=Vector3.MoveTowards(start,path[0],speed*Time.deltaTime);
        // Search observations may span several frames. Always validate the
        // actual current-position step again; never trust stale search physics.
        if(!Clear(start,step)){path.Clear();return;}
        // Search stays on the flat grid, but an escort coroutine can run after
        // Actor.Update has grounded the rig. Keep that root-height correction.
        Vector3 direction=step-start;
        step.y=groundedY;actor.transform.position=step;actor.GroundHeight=0;
        if(direction.sqrMagnitude>.000001f)actor.Face(step+direction.normalized);
    }

    bool RetargetPathEnd(Vector3 start,Vector3 target)
    {
        if((target-goal).sqrMagnitude<.000001f)return true;
        if((target-goal).sqrMagnitude>RetargetDistanceSquared)return false;
        Vector3 from=path.Count>1?path[path.Count-2]:start;
        if(!Clear(from,target))return false;
        path[path.Count-1]=target;goal=target;return true;
    }

    static bool Obstacle(Collider collider)
    {
        if(collider is CharacterController||collider.GetComponentInParent<Chapter2Actor>())return false;
        // Preserve the explicit recalled-companion exception. Other crowd
        // members still participate in every real-time collision query.
        var crowd=collider.GetComponentInParent<Chapter3SchoolCrowdActor>();
        // The retreating crowd yields to leaders with its own collision checks.
        // Do not let a waiting follower trap the leader inside a capsule.
        if(crowd&&(crowd.IsRouteCompanion||crowd.FollowingWithdrawal))return false;
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

    Vector2Int Grid(Vector3 position)=>new Vector2Int(
        Mathf.RoundToInt((position.x-origin.x)/Cell),Mathf.RoundToInt((position.z-origin.z)/Cell));
    Vector3 World(Vector2Int position)=>origin+new Vector3(position.x*Cell,0,position.y*Cell);

    void BeginSearch(Vector3 start,Vector3 target)
    {
        path.Clear();CancelSearch();searchStart=start;searchTarget=target;
        origin=new Vector3(Mathf.Floor(Mathf.Min(start.x,target.x)-4),0,Mathf.Floor(Mathf.Min(start.z,target.z)-4));
        width=Mathf.CeilToInt((Mathf.Max(start.x,target.x)+4-origin.x)/Cell)+1;
        height=Mathf.CeilToInt((Mathf.Max(start.z,target.z)+4-origin.z)/Cell)+1;
        first=Grid(start);last=Grid(target);open.Add(first);cost[first]=0;searching=true;
    }

    // True means a complete candidate path was produced in this call. False
    // covers both pending and exhausted; only exhaustion starts the retry delay.
    bool Plan(Vector3 start,Vector3 target)
    {
        using var sample=PlanMarker.Auto();
        if(nodesThisFrame>=NodesPerFrame)return false;
        if(!searching)BeginSearch(start,target);
        while(open.Count>0&&iterations<MaxSearchNodes&&nodesThisFrame<NodesPerFrame)
        {
            iterations++;nodesThisFrame++;
            // Preserve original selection order and tie handling. A heap or
            // different heuristic is deliberately outside this change.
            int best=0;float score=float.MaxValue;
            for(int i=0;i<open.Count;i++)
            {float f=cost[open[i]]+Vector2Int.Distance(open[i],last);if(f<score){score=f;best=i;}}
            var node=open[best];open.RemoveAt(best);closed.Add(node);
            Vector3 here=node==first?searchStart:World(node);
            if(Vector3.Distance(here,searchTarget)<.8f&&Clear(here,searchTarget))
            {
                // Same point order as repeated Insert(0), with linear work.
                path.Add(searchTarget);
                while(node!=first){path.Add(World(node));node=parent[node];}
                path.Reverse();goal=searchTarget;CancelSearch();nextPlan=Time.time+.4f;
                return true;
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
        if(open.Count==0||iterations>=MaxSearchNodes)
        {CancelSearch();nextPlan=Time.time+.4f;}
        // A pending search keeps its frontier and total iteration count.
        // No safe route: wait, never teleport through an obstacle.
        return false;
    }
}
