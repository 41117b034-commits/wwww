using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

public static class Chapter3GroundGateAuthoring
{
    const string MeshPath="Assets/Chapter3/Station/Continuous courtyard and road.asset";
    [MenuItem("Tools/Chapter 3/Fix Station Ground and Stone Collisions")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first.");
        var flow=Object.FindFirstObjectByType<Chapter3Flow>();
        if(!flow||flow.gameObject.scene.path!="Assets/Scenes/第三章.unity")throw new InvalidOperationException("Open the main Chapter 3 scene.");
        var root=flow.stationRegion.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="駐在所山坳與道路");
        var slabs=root.GetComponentsInChildren<BoxCollider>(true).Where(c=>c.name=="庭院可行走地面"||c.name.StartsWith("前往霧社的土路 ")).OrderBy(c=>c.name).ToArray();
        var polygons=new List<List<Vector2>>();var vertices=new List<Vector3>();var triangles=new List<int>();var uv=new List<Vector2>();
        foreach(var slab in slabs)
        {
            var corners=new List<Vector2>();
            foreach(var corner in new[]{new Vector3(-.5f,0,-.5f),new Vector3(.5f,0,-.5f),new Vector3(.5f,0,.5f),new Vector3(-.5f,0,.5f)})
            {var p=slab.transform.TransformPoint(slab.center+Vector3.Scale(slab.size,corner));corners.Add(new Vector2(p.x,p.z));}
            var pieces=new List<List<Vector2>>{corners};
            foreach(var previous in polygons)
            {
                var next=new List<List<Vector2>>();
                foreach(var piece in pieces)Subtract(piece,previous,next);
                pieces=next;
            }
            foreach(var piece in pieces)
            {
                int first=vertices.Count;
                foreach(var p in piece){vertices.Add(root.InverseTransformPoint(new Vector3(p.x,-.005f,p.y)));uv.Add(p/8);}
                for(int i=1;i<piece.Count-1;i++){triangles.Add(first);triangles.Add(first+i+1);triangles.Add(first+i);}
            }
            polygons.Add(corners);
            slab.GetComponent<Renderer>().enabled=false; // Keep authored bounds and physics.
        }
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
        if(!mesh){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,MeshPath);}else mesh.Clear();
        mesh.name="Single non-overlapping station surface";mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
        var surface=root.Find("駐在所連續路面");if(!surface){surface=new GameObject("駐在所連續路面",typeof(MeshFilter),typeof(MeshRenderer)).transform;surface.SetParent(root,false);}
        surface.GetComponent<MeshFilter>().sharedMesh=mesh;surface.GetComponent<MeshRenderer>().sharedMaterial=slabs[0].GetComponent<Renderer>().sharedMaterial;
        foreach(var stone in flow.stationRegion.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="庭院石緣"))
        {var collider=stone.GetComponent<BoxCollider>();if(!collider)collider=stone.gameObject.AddComponent<BoxCollider>();collider.enabled=true;collider.isTrigger=false;}
        // These decorative stones previously had no physics and intruded into
        // the leader's destination and Pihu's side of the same escort route.
        // Keep the solid stones on the verge rather than making actors pass through them.
        MoveStone(flow,new Vector3(13.868722f,.17f,3.2f),14.6f);
        MoveStone(flow,new Vector3(12.984826f,.17f,-.5f),12f);
        MoveStone(flow,new Vector3(13.741431f,.17f,-4.2f),12.2f);
        EditorSceneManager.MarkSceneDirty(flow.gameObject.scene);EditorSceneManager.SaveScene(flow.gameObject.scene);AssetDatabase.SaveAssets();
    }
    static void MoveStone(Chapter3Flow flow,Vector3 original,float x)
    {
        var moved=new Vector3(x,original.y,original.z);
        var stone=flow.stationRegion.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="庭院石緣"&&
            (Vector3.Distance(t.position,original)<.025f||Vector3.Distance(t.position,moved)<.025f));
        stone.position=moved;
    }
    // Subtract an already-covered convex rectangle into disjoint convex pieces.
    static void Subtract(List<Vector2> subject,List<Vector2> clip,List<List<Vector2>> output)
    {
        var remaining=subject;
        for(int i=0;i<clip.Count&&remaining.Count>=3;i++)
        {
            var outside=Half(remaining,clip[i],clip[(i+1)%clip.Count],false);
            if(Area(outside)>.00001f)output.Add(outside);
            remaining=Half(remaining,clip[i],clip[(i+1)%clip.Count],true);
        }
    }
    static List<Vector2> Half(List<Vector2> polygon,Vector2 a,Vector2 b,bool inside)
    {
        var result=new List<Vector2>();
        for(int i=0;i<polygon.Count;i++)
        {
            var p=polygon[i];var q=polygon[(i+1)%polygon.Count];float dp=Cross(b-a,p-a),dq=Cross(b-a,q-a);
            bool pin=inside?dp>=0:dp<=0,qin=inside?dq>=0:dq<=0;
            if(pin)result.Add(p);
            if(pin!=qin)result.Add(Vector2.Lerp(p,q,dp/(dp-dq)));
        }
        return result;
    }
    static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
    static float Area(List<Vector2> polygon){float area=0;for(int i=0;i<polygon.Count;i++)area+=Cross(polygon[i],polygon[(i+1)%polygon.Count]);return Mathf.Abs(area)*.5f;}
}
