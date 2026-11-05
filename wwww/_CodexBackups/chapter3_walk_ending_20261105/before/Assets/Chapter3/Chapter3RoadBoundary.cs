using UnityEngine;

// Uses the saved road's actual outline, including its varying width and bends.
public sealed class Chapter3RoadBoundary
{
    readonly Vector2[] outline;
    public Chapter3RoadBoundary(Transform region)
    {
        foreach(var filter in region.GetComponentsInChildren<MeshFilter>(true))
        {
            if(filter.name!="往駐在所的土路"||!filter.sharedMesh)continue;
            var vertices=filter.sharedMesh.vertices;
            outline=new Vector2[vertices.Length];int n=vertices.Length/2;
            for(int i=0;i<n;i++)
            {
                Vector3 left=filter.transform.TransformPoint(vertices[i*2]);
                Vector3 right=filter.transform.TransformPoint(vertices[i*2+1]);
                outline[i]=new Vector2(left.x,left.z);outline[vertices.Length-1-i]=new Vector2(right.x,right.z);
            }
            return;
        }
        Debug.LogError("[Chapter3] Saved village road outline is missing.");
    }
    public bool Contains(Vector3 point,float margin)
    {
        if(outline==null)return false;
        Vector2 p=new Vector2(point.x,point.z);bool inside=false;float distance=float.MaxValue;
        for(int i=0,j=outline.Length-1;i<outline.Length;j=i++)
        {
            Vector2 a=outline[j],b=outline[i],d=b-a;
            if((a.y>p.y)!=(b.y>p.y)&&p.x<(b.x-a.x)*(p.y-a.y)/(b.y-a.y)+a.x)inside=!inside;
            float t=d.sqrMagnitude>0?Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude):0;
            distance=Mathf.Min(distance,(p-a-d*t).sqrMagnitude);
        }
        return inside&&distance>=margin*margin;
    }
}
