using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// The recorded site is a rock shelter. Its shape and the playable chamber are scene design.
public static class Chapter4CaveAuthoring
{
    const string Folder = "Assets/Chapter4/Environment/";
    const string OldName = "終點 · 馬赫坡岩窟入口";
    const string CaveName = "終點 · 馬赫坡岩窟";
    // The cleft flares on the left and the roof descends toward the inner chamber.
    static readonly Vector2[] section = {
        new(-1.08f,-.35f), new(-1.02f,.10f), new(-.96f,.39f), new(-.89f,.48f),
        new(-.91f,.65f), new(-.73f,.83f), new(-.52f,.95f), new(-.24f,1.02f),
        new(.04f,.94f), new(.33f,.98f), new(.57f,.84f), new(.70f,.80f),
        new(.82f,.57f), new(.96f,.41f), new(1,.15f), new(1.06f,-.35f)
    };

    public static void Install()
    {
        var scene = EditorSceneManager.OpenScene(Chapter4SceneAuthoring.ScenePath);
        var current = GameObject.Find(OldName) ?? GameObject.Find(CaveName);
        if (!current) throw new InvalidOperationException("Chapter 4 shelter landmark missing.");
        Transform parent = current.transform.parent;
        UnityEngine.Object.DestroyImmediate(current);
        Build(parent);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    public static void Build(Transform parent)
    {
        var root = new GameObject(CaveName).transform;
        root.SetParent(parent, false);
        var ground = GameObject.Find("Forest valley ground").GetComponent<Collider>();
        Physics.SyncTransforms();
        float Floor(float x, float z) => ground.Raycast(new Ray(new Vector3(x, 75, z), Vector3.down), out var hit, 120)
            ? hit.point.y : 7.15f;
        var vertices = new List<Vector3>();
        var uv = new List<Vector2>();
        var faces = new[] { new List<int>(), new List<int>(), new List<int>() };

        // Longitudinal variation changes both the silhouette and the seams. All
        // rings meet edge to edge; the entrance, sidewalls and roof are one shell.
        Vector3[] Ring(float z, float x, float halfWidth, float height, bool outside = false)
        {
            var ring = new Vector3[section.Length];
            for (int i = 0; i < section.Length; i++)
            {
                Vector2 s = section[i];
                float noise = Mathf.PerlinNoise(i * .63f + 2, z * .17f) - .5f;
                float px = x + s.x * halfWidth + noise * (outside ? 1.9f : .85f);
                float pz = z + 1.05f * s.x + noise * (outside ? 2.4f : 1.2f);
                float y = Floor(x, z) + s.y * height + noise * (outside ? 1.65f : .78f);
                if (i == 0 || i == section.Length - 1) y = Mathf.Min(y, Floor(px, pz) - .80f);
                ring[i] = new Vector3(px, y, pz);
            }
            return ring;
        }

        int Vertex(Vector3 point, Vector2 tex)
        {
            int i = vertices.Count; vertices.Add(point); uv.Add(tex); return i;
        }
        void Triangle(int a, int b, int c, int material, bool reverse)
        {
            var f = faces[material]; f.Add(a); f.Add(reverse ? c : b); f.Add(reverse ? b : c);
        }
        void Strip(Vector3[] first, Vector3[] second, int material, bool reverse)
        {
            for (int i = 1; i < first.Length; i++)
            {
                Vector3 a = first[i-1], b = first[i], c = second[i-1], d = second[i];
                // Dominant-axis projection keeps stone bedding continuous across adjacent faces.
                var n = Vector3.Cross(b-a, c-a).normalized;
                Vector2 UV(Vector3 p) => (Mathf.Abs(n.y) > .62f ? new Vector2(p.x, p.z) :
                    Mathf.Abs(n.z) > Mathf.Abs(n.x) ? new Vector2(p.x, p.y) : new Vector2(p.z, p.y)) * .18f;
                int ia = Vertex(a, UV(a)), ib = Vertex(b, UV(b)), ic = Vertex(c, UV(c)), id = Vertex(d, UV(d));
                Triangle(ia, ib, ic, material, reverse); Triangle(ib, id, ic, material, reverse);
            }
        }
        var mouth = Ring(120.8f, 24.1f, 4.35f, 4.25f);
        var lip = Ring(119.9f, 24.2f, 5.65f, 5.5f, true);
        var face = Ring(122.8f, 22.8f, 9.3f, 8.45f, true);
        var crest = Ring(127.6f, 21.6f, 15.8f, 12.2f, true);
        Strip(mouth, lip, 0, true); Strip(lip, face, 0, true); Strip(face, crest, 0, true);
        var ridge = Ring(139, 19, 16.3f, 12.8f, true);
        var hillside = Ring(152.5f, 17.8f, 15.1f, 8.4f, true);
        Strip(crest, ridge, 0, true); Strip(ridge, hillside, 0, true);
        var previous = mouth;
        foreach (var station in new[] { new Vector4(123,24.8f,3.9f,3.85f),new Vector4(125.6f,24.7f,3.65f,3.5f),
            new Vector4(128,23.35f,4.15f,3.85f),new Vector4(130.5f,21.95f,4.4f,4.4f),
            new Vector4(133.7f,20.6f,4.95f,4.75f),new Vector4(136.4f,19.65f,5.05f,4.5f),
            new Vector4(139.8f,18.9f,4.6f,4.05f),new Vector4(142.1f,18.55f,4.05f,3.75f),
            new Vector4(144.6f,18.35f,3.25f,3.10f),new Vector4(146,18.25f,2.45f,2.50f) })
        {
            var next = Ring(station.x,station.y,station.z,station.w);
            Strip(previous, next, 1, false); previous = next;
        }
        var back = new Vector3[previous.Length];
        for (int i = 0; i < back.Length; i++) back[i] = new Vector3(18.1f,Floor(18.1f,147.3f)-.32f,147.3f);
        Strip(previous, back, 1, false);

        // Dress the existing continuous, collision-tested ground through the mouth.
        Vector3[] FloorRow(float z)
        {
            float x = Mathf.Lerp(24.2f,18.6f,Mathf.InverseLerp(123,140,z));
            var row = new Vector3[13];
            for (int i = 0; i < row.Length; i++)
            {
                float px = x + (i-6) * .92f;
                float pz = z + .46f * Mathf.Sin(i * 1.4f + .6f);
                row[i] = new Vector3(px, Floor(px,pz) + .095f, pz);
            }
            return row;
        }
        var prevFloor = FloorRow(121.7f);
        for (float z = 122.7f; z < 148; z += 1)
        {
            var next = FloorRow(z); Strip(prevFloor,next,2,true); prevFloor = next;
        }
        // Weathered flakes break up the floor near the walls. Their low edges sit
        // below the ground and leave the path through the middle open.
        var random = new System.Random(1930);
        for (int k = 0; k < 29; k++)
        {
            float z = 120.4f + (float)random.NextDouble() * 23.5f;
            float axis = Mathf.Lerp(24.2f,18.6f,Mathf.InverseLerp(123,140,z));
            float x = axis + (k % 2 == 0 ? 1 : -1) * (2.55f + (float)random.NextDouble() * 1.55f);
            float baseY = Floor(x,z) + .065f;
            float size = .13f + (float)random.NextDouble() * .42f;
            float yaw = (float)random.NextDouble() * 6.28f;
            var center = new Vector3(x,baseY + size*.36f,z);
            for (int i = 0; i < 6; i++)
            {
                Vector3 Edge(int n) => new Vector3(x + size*Mathf.Cos(yaw+n*Mathf.PI/3),baseY-.065f,
                    z + .65f*size*Mathf.Sin(yaw+n*Mathf.PI/3));
                var a = Edge(i); var b = Edge(i+1);
                Vector2 Tex(Vector3 v) => new Vector2(v.x, v.z)*.37f;
                Triangle(Vertex(center,Tex(center)),Vertex(b,Tex(b)),Vertex(a,Tex(a)),1,false);
            }
        }
        var mesh = new Mesh {name = "Mehebu cave shell"};
        mesh.SetVertices(vertices); mesh.SetUVs(0,uv); mesh.subMeshCount = 3;
        for (int i = 0; i < faces.Length; i++) mesh.SetTriangles(faces[i],i);
        mesh.RecalculateNormals();
        // Welding normals across UV seams keeps the curved vault continuous while
        // retaining sharp edges where two layers of bedrock meet.
        Vector3[] normals = mesh.normals;
        var linked = new Dictionary<Vector3,List<int>>();
        for (int i = 0; i < vertices.Count; i++)
        {
            if (!linked.TryGetValue(vertices[i], out var matching)) linked[vertices[i]] = matching = new List<int>();
            matching.Add(i);
        }
        var smooth = new Vector3[normals.Length];
        for (int i = 0; i < normals.Length; i++)
        {
            Vector3 sum = normals[i];
            foreach (int j in linked[vertices[i]]) if (j != i && Vector3.Dot(normals[i],normals[j]) > .52f) sum += normals[j];
            smooth[i] = sum.normalized;
        }
        mesh.normals = smooth; mesh.RecalculateTangents(); mesh.RecalculateBounds();
        var stored = AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"Mehebu cave shell.asset");
        if (stored) { EditorUtility.CopySerialized(mesh, stored); UnityEngine.Object.DestroyImmediate(mesh); }
        else { stored=mesh; AssetDatabase.CreateAsset(stored,Folder+"Mehebu cave shell.asset"); }
        var shell = new GameObject("山壁、窟口與洞室", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
        shell.transform.SetParent(root,false);
        shell.GetComponent<MeshFilter>().sharedMesh = stored;
        shell.GetComponent<MeshCollider>().sharedMesh = stored;
        var cliff = AssetDatabase.LoadAssetAtPath<Material>(Folder+"Mehebu rock face.mat");
        Material Shade(string name, Material source, Color tint)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(Folder+name+".mat");
            if (!mat) { mat = new Material(source); AssetDatabase.CreateAsset(mat,Folder+name+".mat"); }
            mat.SetColor("_BaseColor",tint); EditorUtility.SetDirty(mat); return mat;
        }
        const string textures = "Assets/Realistic Terrain Collection/Other/Textures/";
        void Bedrock(Material mat, string source, string surface)
        {
            var color = AssetDatabase.LoadAssetAtPath<Texture>(textures+source+"/"+surface+".jpg");
            var normal = AssetDatabase.LoadAssetAtPath<Texture>(textures+source+"/"+surface+"_normal.png");
            if (!color || !normal) throw new InvalidOperationException("Missing tiling bedrock texture.");
            mat.SetTexture("_BaseMap",color); mat.SetTexture("_MainTex",color);
            mat.SetTexture("_BumpMap",normal); mat.EnableKeyword("_NORMALMAP"); mat.SetFloat("_BumpScale",.64f);
            EditorUtility.SetDirty(mat);
        }
        Bedrock(cliff, "SolidRock2", "solid_rock_2");
        var inner = Shade("Mehebu inner stone", cliff, new Color(.49f,.49f,.47f));
        Bedrock(inner, "SolidRock", "solid_rock");
        var soil = Shade("Mehebu cave earth", AssetDatabase.LoadAssetAtPath<Material>(Folder+"Walked forest earth.mat"),new Color(.48f,.41f,.34f));
        shell.GetComponent<MeshRenderer>().sharedMaterials = new[] {cliff,inner,soil};
        var interior = new GameObject("穿過窟口後的內室", typeof(BoxCollider), typeof(Chapter4RockShelter));
        interior.transform.SetParent(root,false);
        interior.transform.SetPositionAndRotation(new Vector3(20.75f,8.95f,135.8f),Quaternion.Euler(0,-13,0));
        var volume = interior.GetComponent<BoxCollider>();
        volume.isTrigger = true; volume.size = new Vector3(8.3f,4.8f,17.6f);
        var bounce = new GameObject("洞口反射的天光",typeof(Light)).GetComponent<Light>();
        bounce.transform.SetParent(root,false); bounce.transform.position = new Vector3(23.6f,9.85f,125.6f);
        bounce.type = LightType.Point; bounce.color = new Color(.81f,.86f,.89f); bounce.range = 15; bounce.intensity = 2.2f;
        var reflected = new GameObject("洞內散光",typeof(Light)).GetComponent<Light>();
        reflected.transform.SetParent(root,false); reflected.transform.position = new Vector3(19.6f,10.2f,137);
        reflected.type = LightType.Point; reflected.color = new Color(.84f,.79f,.69f); reflected.range=10; reflected.intensity=.95f;
    }
}
