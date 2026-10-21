using System.Collections.Generic;
using UnityEngine;

// Chapter 1-style arrows and companion positions share the authored escort polyline.
public sealed class Chapter3RouteGuide : MonoBehaviour
{
    readonly List<Vector3> points=new List<Vector3>();
    readonly List<float> distances=new List<float>();
    readonly List<GameObject> dashes=new List<GameObject>();
    readonly List<float> markDistances=new List<float>();
    Transform follower,leader;
    public float Length => distances.Count==0?0:distances[distances.Count-1];
    public int VisibleMarks {get;private set;}
    public void Show(Vector3 start,Vector3[] route,Transform player,Transform guide,System.Func<Vector3,float> ground=null)
    {
        Hide();follower=player;leader=guide;points.Clear();distances.Clear();start.y=0;points.Add(start);distances.Add(0);
        foreach(var waypoint in route)
        {
            var point=waypoint;point.y=0;
            if(Vector3.Distance(points[points.Count-1],point)>.01f)
            {distances.Add(Length+Vector3.Distance(points[points.Count-1],point));points.Add(point);}
        }
        for(float distance=Chapter3YellowArrow.StartDistance;distance<Length;distance+=Chapter3YellowArrow.Spacing)
        {
            var mark=Chapter3YellowArrow.Create(transform,"黃色地面帶路箭頭",dashes.Count);
            var point=Sample(distance,out var direction);
            if(ground!=null)
            {
                point.y=ground(point);
                var ahead=Sample(Mathf.Min(Length,distance+.35f),out _);ahead.y=ground(ahead);
                var behind=Sample(Mathf.Max(0,distance-.35f),out _);behind.y=ground(behind);
                direction=(ahead-behind).normalized;
            }
            mark.transform.position=point+Vector3.up*Chapter3YellowArrow.Height;
            mark.transform.rotation=Quaternion.LookRotation(direction,Vector3.up);
            dashes.Add(mark);markDistances.Add(distance);
        }
        UpdateVisible();
    }
    void LateUpdate(){if(dashes.Count>0)UpdateVisible();}
    void UpdateVisible()
    {
        if(!follower||!leader){Hide();return;}
        float start=Project(follower.position)+.3f,end=Project(leader.position)-.7f;
        VisibleMarks=0;
        for(int i=0;i<dashes.Count;i++)
        {
            // Include both wings and the largest pulse, not just the origin,
            // so the full arrow stays between player and guide at every frame.
            bool visible=markDistances[i]-Chapter3YellowArrow.RearExtent>=start&&
                markDistances[i]+Chapter3YellowArrow.ForwardExtent<=end;
            dashes[i].SetActive(visible);if(visible)VisibleMarks++;
        }
    }
    public Vector3 Sample(float distance,out Vector3 direction)
    {
        distance=Mathf.Clamp(distance,0,Length);
        for(int i=1;i<points.Count;i++)if(distance<=distances[i])
        {direction=(points[i]-points[i-1]).normalized;return Vector3.Lerp(points[i-1],points[i],Mathf.InverseLerp(distances[i-1],distances[i],distance));}
        direction=Vector3.forward;return points.Count>0?points[points.Count-1]:Vector3.zero;
    }
    public Vector3 SampleBeside(float distance,float side)
    {
        // Interpolate continuous offset vertices; a new segment must not make
        // the rear companion's target jump to the opposite side of a corner.
        for(int i=1;i<points.Count;i++)if(distance<=distances[i])
            return Vector3.Lerp(Beside(i-1,side),Beside(i,side),Mathf.InverseLerp(distances[i-1],distances[i],distance));
        return Beside(points.Count-1,side);
    }
    Vector3 Beside(int index,float side)
    {
        var before=(points[Mathf.Max(1,index)]-points[Mathf.Max(0,index-1)]).normalized;
        var after=(points[Mathf.Min(points.Count-1,index+1)]-points[Mathf.Min(points.Count-2,index)]).normalized;
        var normal=Vector3.Cross(Vector3.up,(before+after).normalized);
        float scale=Mathf.Min(1.3f,side/Mathf.Max(.4f,Vector3.Dot(normal,Vector3.Cross(Vector3.up,after))));
        return points[index]+normal*scale;
    }
    public float Project(Vector3 position,float maximum=float.MaxValue)
    {
        float best=float.MaxValue,result=0;position.y=0;
        for(int i=1;i<points.Count&&distances[i-1]<=maximum;i++)
        {
            var delta=points[i]-points[i-1];float t=Mathf.Clamp01(Vector3.Dot(position-points[i-1],delta)/delta.sqrMagnitude);
            float along=Mathf.Min(maximum,Mathf.Lerp(distances[i-1],distances[i],t));var projected=Sample(along,out _);
            float error=(position-projected).sqrMagnitude;if(error<best){best=error;result=along;}
        }
        return result;
    }
    public void Hide(){foreach(var mark in dashes)if(mark){mark.SetActive(false);Destroy(mark);}dashes.Clear();markDistances.Clear();VisibleMarks=0;follower=leader=null;}
}
