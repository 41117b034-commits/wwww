using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Subtract the entry box from a COPY of this scene's static hut mesh. The
// imported model and every other instance keep their original mesh and UVs.
public static class Chapter1HutOpeningAuthoring
{
    const string OpeningAssetPath = "Assets/Models/Chapter1Doorway/IncidentHutOpening.asset";

    // This high-detail mesh exceeds GitHub's file limit in text format. Binary
    // serialization keeps the exact same mesh data and existing scene references.
    [MenuItem("Tools/Chapter 1/Store Hut Opening as Binary")]
    public static void StoreOpeningAsBinary()
    {
        Mesh opening = AssetDatabase.LoadAssetAtPath<Mesh>(OpeningAssetPath);
        if (opening == null)
            throw new System.InvalidOperationException("Build the incident hut opening before storing it.");

        // The main asset chooses the serialization format for the entire file.
        // Add a binary-preferring container without replacing the existing mesh,
        // so its GUID/local file ID and the scene's references stay intact.
        var storage = AssetDatabase.LoadAssetAtPath<Chapter1HutMeshAsset>(OpeningAssetPath);
        if (storage == null)
        {
            storage = ScriptableObject.CreateInstance<Chapter1HutMeshAsset>();
            storage.name = "Incident hut mesh storage";
            AssetDatabase.AddObjectToAsset(storage, opening);
        }
        storage.mesh = opening;
        AssetDatabase.SetMainObject(storage, OpeningAssetPath);
        EditorUtility.SetDirty(storage);
        AssetDatabase.SaveAssetIfDirty(storage);
        AssetDatabase.ImportAsset(OpeningAssetPath, ImportAssetOptions.ForceUpdate);
        long bytes = new FileInfo(OpeningAssetPath).Length;
        if (bytes >= 100000000)
            throw new System.InvalidOperationException("The generated hut mesh must be smaller than 100 MB before committing.");
        Debug.Log("[Doorway] Hut opening stored as binary: " + bytes + " bytes; mesh detail preserved.");
    }

    struct Vertex
    {
        public Vector3 position, normal;
        public Vector4 tangent;
        public Vector2 uv, uv2;
        public Color color;
        public static Vertex Lerp(Vertex a, Vertex b, float t) => new Vertex {
            position=Vector3.Lerp(a.position,b.position,t), normal=Vector3.Lerp(a.normal,b.normal,t),
            tangent=Vector4.Lerp(a.tangent,b.tangent,t), uv=Vector2.Lerp(a.uv,b.uv,t),
            uv2=Vector2.Lerp(a.uv2,b.uv2,t), color=Color.Lerp(a.color,b.color,t) };
    }

    public static void EnsureOpening(Chapter1HutDoor door)
    {
        GameObject hut = GameObject.Find("傳統建築");
        MeshFilter filter = hut != null ? hut.GetComponent<MeshFilter>() : null;
        // The generated asset may be missing after an interrupted authoring
        // session. The saved imported mesh is enough to rebuild it safely.
        if (filter == null || (filter.sharedMesh == null && door.originalFacadeMesh == null))
            throw new System.InvalidOperationException("The incident hut mesh was not found.");
        const string path = OpeningAssetPath;
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (door.originalFacadeMesh == null)
        {
            if (filter.sharedMesh == existing && existing != null) return;
            door.originalFacadeMesh = filter.sharedMesh;
        }
        Mesh source = door.originalFacadeMesh;
        Vector3[] positions = source.vertices, normals = source.normals;
        Vector4[] tangents = source.tangents;
        Vector2[] uv = source.uv, uv2 = source.uv2;
        Color[] colors = source.colors;
        var vertices = new Vertex[positions.Length];
        for (int i = 0; i < vertices.Length; i++) vertices[i] = new Vertex {
            position=positions[i], normal=normals.Length>i?normals[i]:Vector3.up,
            tangent=tangents.Length>i?tangents[i]:new Vector4(1,0,0,1),
            uv=uv.Length>i?uv[i]:Vector2.zero, uv2=uv2.Length>i?uv2[i]:Vector2.zero,
            color=colors.Length>i?colors[i]:Color.white };
        Matrix4x4 toEntry = door.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
        Bounds opening = new Bounds(new Vector3(0f,10f,5f),new Vector3(10.2f,19.3f,11f));
        // Keep the imported indexing for all untouched triangles. Duplicating
        // the entire high-detail log house would needlessly multiply GPU memory.
        var output = new List<Vertex>(vertices);
        var triangles = new List<int>[source.subMeshCount];
        for (int sub = 0; sub < source.subMeshCount; sub++)
        {
            triangles[sub] = new List<int>();
            int[] indices = source.GetTriangles(sub);
            for (int i = 0; i < indices.Length; i += 3)
            {
                var polygon = new List<Vertex> { vertices[indices[i]], vertices[indices[i+1]], vertices[indices[i+2]] };
                Bounds bounds = new Bounds(toEntry.MultiplyPoint3x4(polygon[0].position),Vector3.zero);
                bounds.Encapsulate(toEntry.MultiplyPoint3x4(polygon[1].position));
                bounds.Encapsulate(toEntry.MultiplyPoint3x4(polygon[2].position));
                if (!bounds.Intersects(opening))
                {
                    triangles[sub].Add(indices[i]); triangles[sub].Add(indices[i+1]); triangles[sub].Add(indices[i+2]);
                    continue;
                }
                // Partition into the pieces outside each half-space. The final
                // remaining polygon lies inside the opening and is discarded.
                for (int plane = 0; plane < 6 && polygon.Count > 0; plane++)
                {
                    int axis = plane / 2;
                    float sign = plane % 2 == 0 ? 1f : -1f;
                    float boundary = plane % 2 == 0 ? opening.min[axis] : opening.max[axis];
                    AddPolygon(Clip(polygon,toEntry,axis,boundary,sign,false),output,triangles[sub]);
                    polygon = Clip(polygon,toEntry,axis,boundary,sign,true);
                }
            }
        }
        var mesh = new Mesh { name="Incident hut with doorway", indexFormat=IndexFormat.UInt32 };
        var outPositions=new List<Vector3>(); var outNormals=new List<Vector3>(); var outTangents=new List<Vector4>();
        var outUv=new List<Vector2>(); var outUv2=new List<Vector2>(); var outColors=new List<Color>();
        foreach(var vertex in output)
        {
            outPositions.Add(vertex.position); outNormals.Add(vertex.normal.normalized); outTangents.Add(vertex.tangent);
            outUv.Add(vertex.uv); outUv2.Add(vertex.uv2); outColors.Add(vertex.color);
        }
        mesh.SetVertices(outPositions); mesh.SetNormals(outNormals); mesh.SetTangents(outTangents);
        mesh.SetUVs(0,outUv); if(uv2.Length>0)mesh.SetUVs(1,outUv2); if(colors.Length>0)mesh.SetColors(outColors);
        mesh.subMeshCount=triangles.Length;
        for(int sub=0;sub<triangles.Length;sub++)mesh.SetTriangles(triangles[sub],sub);
        mesh.RecalculateBounds();
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        if(existing==null){AssetDatabase.CreateAsset(mesh,path);existing=mesh;}
        else {EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(existing);}
        Undo.RecordObject(filter,"Open incident hut facade"); filter.sharedMesh=existing;
        PrefabUtility.RecordPrefabInstancePropertyModifications(filter);
        var collider=hut.GetComponent<MeshCollider>();
        if(collider!=null)
        {
            Undo.RecordObject(collider,"Open incident hut collider"); collider.sharedMesh=existing;
            PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
        }
        OpenBoxColliders(hut, door, opening);
        EditorUtility.SetDirty(filter);EditorUtility.SetDirty(door);AssetDatabase.SaveAssets();
        StoreOpeningAsBinary();
        Debug.Log("[Doorway] Local hut mesh opening created; imported source retained.");
    }

    static void OpenBoxColliders(GameObject hut, Chapter1HutDoor door, Bounds opening)
    {
        // This scene used one solid BoxCollider for the entire log house. Split
        // that volume around the portal instead of baking millions of triangles
        // into a new physics mesh or removing the rest of the house's collision.
        Transform previous = hut.transform.Find("Incident doorway collision");
        if(previous != null) Object.DestroyImmediate(previous.gameObject);
        var parent = new GameObject("Incident doorway collision");
        parent.transform.SetParent(hut.transform,false);
        Matrix4x4 local = hut.transform.worldToLocalMatrix * door.transform.localToWorldMatrix;
        Bounds cut = new Bounds(local.MultiplyPoint3x4(opening.min),Vector3.zero);
        for(int corner=0;corner<8;corner++)cut.Encapsulate(local.MultiplyPoint3x4(new Vector3(
            (corner&1)==0?opening.min.x:opening.max.x,
            (corner&2)==0?opening.min.y:opening.max.y,
            (corner&4)==0?opening.min.z:opening.max.z)));
        foreach(BoxCollider original in hut.GetComponents<BoxCollider>())
        {
            Bounds full = new Bounds(original.center,original.size);
            if(!full.Intersects(cut))continue;
            Undo.RecordObject(original,"Open incident hut collision"); original.enabled=false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(original);
            Vector3 low=Vector3.Max(full.min,cut.min), high=Vector3.Min(full.max,cut.max);
            AddColliderSlab(parent.transform,full.min,new Vector3(low.x,full.max.y,full.max.z),original);
            AddColliderSlab(parent.transform,new Vector3(high.x,full.min.y,full.min.z),full.max,original);
            AddColliderSlab(parent.transform,new Vector3(low.x,full.min.y,full.min.z),new Vector3(high.x,low.y,full.max.z),original);
            AddColliderSlab(parent.transform,new Vector3(low.x,high.y,full.min.z),new Vector3(high.x,full.max.y,full.max.z),original);
            AddColliderSlab(parent.transform,new Vector3(low.x,low.y,full.min.z),new Vector3(high.x,high.y,low.z),original);
            AddColliderSlab(parent.transform,new Vector3(low.x,low.y,high.z),new Vector3(high.x,high.y,full.max.z),original);
        }
    }

    static void AddColliderSlab(Transform parent,Vector3 min,Vector3 max,BoxCollider source)
    {
        Vector3 size=max-min;
        if(size.x<0.001f||size.y<0.001f||size.z<0.001f)return;
        var part=new GameObject("Hut collision segment"); part.layer=source.gameObject.layer;
        part.transform.SetParent(parent,false); part.transform.localPosition=(min+max)*0.5f;
        var collider=part.AddComponent<BoxCollider>(); collider.size=size;
        collider.sharedMaterial=source.sharedMaterial; collider.isTrigger=source.isTrigger;
    }

    static List<Vertex> Clip(List<Vertex> polygon, Matrix4x4 matrix, int axis, float boundary, float sign, bool inside)
    {
        var result=new List<Vertex>();
        if(polygon.Count==0)return result;
        Vertex previous=polygon[polygon.Count-1];
        float a=(matrix.MultiplyPoint3x4(previous.position)[axis]-boundary)*sign;
        foreach(Vertex current in polygon)
        {
            float b=(matrix.MultiplyPoint3x4(current.position)[axis]-boundary)*sign;
            bool keepPrevious=inside?a>=0f:a<0f, keepCurrent=inside?b>=0f:b<0f;
            if(keepPrevious!=keepCurrent)result.Add(Vertex.Lerp(previous,current,a/(a-b)));
            if(keepCurrent)result.Add(current);
            previous=current;a=b;
        }
        return result;
    }

    static void AddPolygon(List<Vertex> polygon,List<Vertex> output,List<int> triangles)
    {
        for(int i=1;i+1<polygon.Count;i++)
        {
            if(Vector3.Cross(polygon[i].position-polygon[0].position,polygon[i+1].position-polygon[0].position).sqrMagnitude<1e-12f)continue;
            triangles.Add(output.Count);output.Add(polygon[0]);
            triangles.Add(output.Count);output.Add(polygon[i]);
            triangles.Add(output.Count);output.Add(polygon[i+1]);
        }
    }
}
