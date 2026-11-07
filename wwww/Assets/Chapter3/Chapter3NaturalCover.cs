using System.Collections.Generic;
using UnityEngine;

// Reuses the project's walnut geometry/materials. Only its solid bark submesh
// blocks movement and bullets; transparent leaf cards never become cover.
public static class Chapter3NaturalCover
{
    static Mesh visibleMesh,barkMesh;
    static Material[] materials;
    public static Transform Tree(Transform parent,Vector3 position,int index)
    {
        if(!PrepareMesh())return null;
        var root=new GameObject("山路粗樹幹掩體 "+index,typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider)).transform;
        root.SetParent(parent,false);root.SetPositionAndRotation(position-Vector3.up*.025f,Quaternion.Euler(0,index*47,0));
        // Vary height without stretching the protective trunk sideways.
        root.localScale=new Vector3(1,.91f+(index%3)*.065f,1);
        root.GetComponent<MeshFilter>().sharedMesh=visibleMesh;
        root.GetComponent<MeshRenderer>().sharedMaterials=materials;
        var collider=root.GetComponent<MeshCollider>();collider.sharedMesh=barkMesh;collider.convex=false;
        return root;
    }
    static bool PrepareMesh()
    {
        if(visibleMesh&&barkMesh)return true;
        var model=Resources.Load<GameObject>("Chapter3/CombatCover/BlackWalnutCover");
        if(!model){Debug.LogError("Missing Chapter 3 walnut cover model.");return false;}
        MeshFilter filter=null;MeshRenderer renderer=null;int bark=-1;
        foreach(var candidate in model.GetComponentsInChildren<MeshFilter>(true))
        {
            var r=candidate.GetComponent<MeshRenderer>();if(!r||!candidate.sharedMesh)continue;
            var mats=r.sharedMaterials;
            for(int i=0;i<mats.Length&&i<candidate.sharedMesh.subMeshCount;i++)
                if(mats[i]&&mats[i].name.ToLowerInvariant().Contains("bark"))
                {filter=candidate;renderer=r;bark=i;break;}
            if(filter)break;
        }
        if(!filter||!filter.sharedMesh.isReadable)
        {Debug.LogError("Chapter 3 walnut cover needs its readable bark submesh.");return false;}
        var source=filter.sharedMesh;var vertices=source.vertices;var triangles=source.GetTriangles(bark);
        var matrix=filter.transform.localToWorldMatrix;
        float lowest=float.PositiveInfinity,highest=float.NegativeInfinity;
        for(int i=0;i<vertices.Length;i++){vertices[i]=matrix.MultiplyPoint3x4(vertices[i]);highest=Mathf.Max(highest,vertices[i].y);}
        foreach(int i in triangles)lowest=Mathf.Min(lowest,vertices[i].y);
        float scale=14f/Mathf.Max(.01f,highest-lowest);
        for(int i=0;i<vertices.Length;i++)vertices[i]=new Vector3(vertices[i].x*scale,(vertices[i].y-lowest)*scale,vertices[i].z*scale);

        // Measure the actual trunk at chest height, including long triangle
        // edges that have no vertex at that height. Preserve its bark/root shape.
        var section=new List<Vector3>();const float chest=1.3f;
        for(int i=0;i<triangles.Length;i+=3)
            for(int edge=0;edge<3;edge++)
            {
                Vector3 a=vertices[triangles[i+edge]],b=vertices[triangles[i+(edge+1)%3]];
                if((a.y-chest)*(b.y-chest)>0||Mathf.Abs(b.y-a.y)<.00001f)continue;
                section.Add(Vector3.Lerp(a,b,(chest-a.y)/(b.y-a.y)));
            }
        if(section.Count<3){Debug.LogError("Could not measure the walnut cover trunk.");return false;}
        Bounds cross=new Bounds(section[0],Vector3.zero);foreach(var point in section)cross.Encapsulate(point);
        Vector3 center=new Vector3(cross.center.x,0,cross.center.z);
        float widen=1.5f/Mathf.Max(.01f,Mathf.Min(cross.size.x,cross.size.z));
        for(int i=0;i<vertices.Length;i++)
        {
            var p=vertices[i]-center;
            // Thick lower bole blends into the original branches/crown above
            // six metres; no uniformly inflated or flattened tree crown.
            float factor=Mathf.Lerp(widen,1,Mathf.SmoothStep(0,1,Mathf.InverseLerp(2.8f,6,p.y)));
            p.x*=factor;p.z*=factor;
            if(p.y<.75f)
            {
                var radial=new Vector2(p.x,p.z);float limit=Mathf.Lerp(.98f,.86f,Mathf.Clamp01(p.y/.75f));
                if(radial.magnitude>limit){radial=radial.normalized*limit;p.x=radial.x;p.z=radial.y;}
            }
            vertices[i]=p;
        }
        visibleMesh=Object.Instantiate(source);visibleMesh.name="Chapter3 walnut cover visible";
        visibleMesh.vertices=vertices;visibleMesh.RecalculateNormals();visibleMesh.RecalculateBounds();
        // Keep unused leaf vertices out of the collider as well as its index
        // list. Otherwise its broadphase bounds still span the whole crown.
        var barkVertices=new List<Vector3>();var barkIndices=new int[triangles.Length];
        var remap=new Dictionary<int,int>();
        for(int i=0;i<triangles.Length;i++)
        {
            int original=triangles[i];
            if(!remap.TryGetValue(original,out int compact))
            {compact=barkVertices.Count;remap.Add(original,compact);barkVertices.Add(vertices[original]);}
            barkIndices[i]=compact;
        }
        barkMesh=new Mesh{name="Chapter3 walnut cover solid bark",indexFormat=source.indexFormat};
        barkMesh.SetVertices(barkVertices);barkMesh.triangles=barkIndices;barkMesh.RecalculateBounds();
        materials=renderer.sharedMaterials;return true;
    }
}
