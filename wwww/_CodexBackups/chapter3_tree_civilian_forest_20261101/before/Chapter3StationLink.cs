using System.Collections.Generic;
using UnityEngine;

// Join the existing regions once, before either is shown. The player never
// moves as part of the station-to-combat handoff. Distances are game staging.
public sealed class Chapter3StationLink : MonoBehaviour
{
    public static readonly Vector3 Origin=new Vector3(-22,0,-101);
    readonly List<Mesh> meshes=new List<Mesh>();
    Chapter3Flow flow;
    public void Build(Chapter3Flow owner)
    {
        flow=owner;var station=flow.station;var root=flow.stationRegion.transform;
        root.position=Origin;
        for(int i=0;i<station.approachPath.Length;i++)station.approachPath[i]+=Origin;
        var local=new[]{new Vector3(0,0,-1.5f),new Vector3(5.5f,0,-3),new Vector3(8.5f,0,-1),
            new Vector3(8.5f,0,3),new Vector3(13,0,3),new Vector3(18,0,6),new Vector3(22,0,10)};
        station.departurePath=new Vector3[local.Length];
        for(int i=0;i<local.Length;i++)station.departurePath[i]=Origin+local[i];
        Material road=null;
        foreach(var t in root.GetComponentsInChildren<Transform>(true))
        {
            if(t.name.StartsWith("前往霧社的土路 "))
            {road=t.GetComponent<Renderer>().sharedMaterial;t.gameObject.SetActive(false);}
            // Remove the old path's far scenery, and stones inside the new
            // physical connector. Keep the courtyard and building intact.
            if(t.name.StartsWith("駐在所林木 ")&&t.position.z>-93)t.gameObject.SetActive(false);
            if(t.name=="庭院石緣"&&t.position.x>-10&&t.position.z>-101)t.gameObject.SetActive(false);
            if(t.name=="Station mountain ground")Trim(t.GetComponent<MeshFilter>(),-93,false);
        }
        for(int i=1;i<local.Length;i++)Road(root,station.departurePath[i-1],station.departurePath[i],road,i);
        Road(root,station.departurePath[local.Length-1],flow.combat.WorldPoint(new Vector3(0,0,-8)),road,local.Length);
        foreach(var t in flow.combatRegion.GetComponentsInChildren<Transform>(true))
        {
            if(t.name=="交戰山谷地形"||t.name=="山路地面")Trim(t.GetComponent<MeshFilter>(),-93,true);
            if(t.name.StartsWith("交戰道路林木 ")&&t.position.z<-92)t.gameObject.SetActive(false);
        }
    }
    void Road(Transform parent,Vector3 a,Vector3 b,Material material,int index)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name="前往霧社的土路 連接 "+index;
        go.transform.SetParent(parent,true);go.transform.position=(a+b)*.5f+Vector3.down*.08f;
        go.transform.rotation=Quaternion.LookRotation(b-a);go.transform.localScale=new Vector3(4.2f,.15f,Vector3.Distance(a,b)+.12f);
        go.GetComponent<Renderer>().sharedMaterial=material;
    }
    void Trim(MeshFilter filter,float seam,bool keepNorth)
    {
        if(!filter||!filter.sharedMesh||!filter.sharedMesh.isReadable)return;
        var original=filter.sharedMesh;var source=original.vertices;var uv=original.uv;
        var verts=new List<Vector3>();var tex=new List<Vector2>();var triangles=new List<int>();
        var indices=original.triangles;
        for(int i=0;i<indices.Length;i+=3)
        {
            var polygon=new List<(Vector3 p,Vector2 uv)>();
            for(int j=0;j<3;j++){int n=indices[i+j];polygon.Add((filter.transform.TransformPoint(source[n]),uv.Length==source.Length?uv[n]:Vector2.zero));}
            var clipped=new List<(Vector3 p,Vector2 uv)>();
            for(int j=0;j<polygon.Count;j++)
            {
                var a=polygon[j];var b=polygon[(j+1)%polygon.Count];
                bool insideA=keepNorth?a.p.z>=seam:a.p.z<=seam,insideB=keepNorth?b.p.z>=seam:b.p.z<=seam;
                if(insideA)clipped.Add(a);
                if(insideA!=insideB){float t=(seam-a.p.z)/(b.p.z-a.p.z);clipped.Add((Vector3.Lerp(a.p,b.p,t),Vector2.Lerp(a.uv,b.uv,t)));}
            }
            int start=verts.Count;
            foreach(var v in clipped){verts.Add(filter.transform.InverseTransformPoint(v.p));tex.Add(v.uv);}
            for(int n=1;n+1<clipped.Count;n++)triangles.AddRange(new[]{start,start+n,start+n+1});
        }
        var mesh=new Mesh{name=original.name+" continuous station join",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};
        mesh.SetVertices(verts);mesh.SetUVs(0,tex);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);
        filter.sharedMesh=mesh;var collider=filter.GetComponent<MeshCollider>();if(collider)collider.sharedMesh=mesh;
    }
    public void Preview()
    {
        flow.combatRegion.SetActive(true);flow.combat.Suspend();
        flow.combat.bawan.gameObject.SetActive(false);flow.combat.pihu.gameObject.SetActive(false);
        flow.Approach.Prepare();
        foreach(var light in flow.combatRegion.GetComponentsInChildren<Light>(true))light.enabled=false;
    }
    void OnDestroy(){foreach(var mesh in meshes)if(mesh)Destroy(mesh);}
}
