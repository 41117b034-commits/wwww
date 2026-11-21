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

    // From the inner chamber, through the rear opening, then left into the trees.
    static readonly Vector2[] trailStations = {
        new(21.15f,140.25f), new(22.7f,143.4f), new(24.35f,146.65f), new(25.2f,149.55f),
        new(25.35f,151.8f), new(24.85f,153.55f), new(23.65f,155.15f), new(21.05f,157.05f),
        new(18.15f,158.65f), new(15.45f,160.35f), new(13.65f,162.6f), new(12.6f,165.05f),
        new(12.25f,168.4f),
        // The visible trail continues around another bend beyond the party's stopping point.
        new(12.45f,171.4f), new(13.6f,174.2f), new(16.3f,177.4f), new(20.5f,180.2f),
        new(26.2f,183.15f), new(33.1f,185.4f), new(39.9f,188.4f)
    };
    const int ForestStart = 4;
    const int RefugeStation = 12;

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


    // The forest path opens into a shallow ravine on the far side of the ridge.
    // This is a ceiling on ground elevation, so an existing lower surface and
    // repeat installs keep their height.
    public static float RearValleyHeight(float x, float z, float ground)
    {
        if (z < 145) return ground;
        float axis = 25.0f + (z - 149) * .33f + 1.3f * Mathf.Sin((z - 149) * .09f);
        float width = Mathf.Lerp(2, 7, Mathf.InverseLerp(145, 185, z));
        float bank = Mathf.Max(0, Mathf.Abs(x-axis) - width);
        float entry = 1 - Mathf.InverseLerp(145,153,z);
        float path = 7.1f - .055f*(z-148) + .067f*bank*bank + 18*entry*entry;
        float valley = Mathf.Min(ground,path);
        if (z < 150 || z > 196 || x > 49 || x < 1) return valley;
        var point = new Vector2(x,z);
        for (int i = ForestStart; i < trailStations.Length - 1; i++)
        {
            Vector2 a = trailStations[i], direction = trailStations[i+1] - a;
            float t = Mathf.Clamp01(Vector2.Dot(point-a,direction) / direction.sqrMagnitude);
            Vector2 center = a + direction*t;
            float shoulder = Mathf.Lerp(2.2f, 3.3f, Mathf.InverseLerp(158,168,center.y));
            float bankDistance = Mathf.Max(0, Vector2.Distance(point,center) - shoulder);
            float trailHeight = Mathf.Lerp(7.05f,8.05f,Mathf.InverseLerp(152,168,center.y));
            valley = Mathf.Min(valley, trailHeight + .16f*bankDistance*bankDistance);
        }
        return valley;
    }

    static void FitRearGround(MeshCollider ground)
    {
        Mesh mesh=ground.sharedMesh;
        var points=mesh.vertices;
        bool changed=false;
        for(int i=0;i<points.Length;i++)
        {
            float y=RearValleyHeight(points[i].x,points[i].z,points[i].y);
            if(y >= points[i].y) continue;
            points[i].y=y; changed=true;
        }
        if(!changed) return;
        var vegetation=GameObject.Find("02 山林 · 實體樹幹");
        var plantings=new List<(Transform root,float oldFloor)>();
        float GroundAt(Vector3 p) => ground.Raycast(new Ray(new Vector3(p.x,80,p.z),Vector3.down),out var hit,140)
            ? hit.point.y : p.y;
        void Remember(Transform item)
        {
            if(item.position.z>=144) plantings.Add((item,GroundAt(item.position)));
        }
        if(vegetation) foreach(Transform child in vegetation.transform)
        {
            if(child.name.StartsWith("山林樹木",StringComparison.Ordinal)) Remember(child);
            else foreach(Transform item in child) Remember(item);
        }
        ground.sharedMesh=null;
        mesh.vertices=points; mesh.RecalculateNormals(); mesh.RecalculateBounds(); EditorUtility.SetDirty(mesh);
        ground.sharedMesh=mesh; Physics.SyncTransforms();
        foreach(var planting in plantings)
        {
            float shift=GroundAt(planting.root.position)-planting.oldFloor;
            if(Mathf.Abs(shift)>.004f) planting.root.position+=Vector3.up*shift;
        }
    }

    static void PlantRearOpening(Transform root, MeshCollider ground, Func<float,float,float> floor)
    {
        var forest=GameObject.Find("02 山林 · 實體樹幹").transform;
        var plants=forest.Find("林下蕨草 · 無碰撞");
        Transform tree=null;
        foreach(Transform child in forest)
            if(child.name.StartsWith("山林樹木",StringComparison.Ordinal)) { tree=child; break; }
        if(!tree || !plants || plants.childCount==0) throw new InvalidOperationException("Missing Chapter 4 forest models.");
        var grove=new GameObject("後山林間的小徑").transform;
        grove.SetParent(root,false);
        void Plant(Transform source,string name,float x,float z,float height,float yaw)
        {
            var model=UnityEngine.Object.Instantiate(source,grove);
            model.name=name;model.rotation=Quaternion.Euler(0,yaw,0)*source.rotation;
            Bounds BoundsOf()
            {
                var renderers=model.GetComponentsInChildren<Renderer>();
                var b=renderers[0].bounds;
                for(int i=1;i<renderers.Length;i++) b.Encapsulate(renderers[i].bounds);
                return b;
            }
            var b=BoundsOf();
            float scale=height/Mathf.Max(.1f,b.size.y);
            if(source==tree)
            {
                model.position=new Vector3(x,floor(x,z),z);
                model.GetChild(0).localScale*=scale;
                model.GetComponent<CapsuleCollider>().height*=scale;
                Chapter4Grounding.SeatTree(model,ground);
                return;
            }
            model.localScale*=scale;
            b=BoundsOf();
            model.position+=new Vector3(x-b.center.x,floor(x,z)-b.min.y-.035f,z-b.center.z);
        }
        foreach(var t in new[] {
            new Vector4(20.95f,154.2f,8.4f,14),new Vector4(30.0f,154.7f,7.2f,178),
            new Vector4(20.6f,164,10.0f,96),new Vector4(33.6f,164.3f,8.6f,232),
            new Vector4(28.0f,173,8.8f,46),new Vector4(42.2f,181,11.6f,123),
            new Vector4(7.1f,166.8f,9.2f,193),new Vector4(18.8f,170.9f,8.8f,72),
            new Vector4(8.25f,175.6f,8.6f,258),new Vector4(22.5f,175.5f,9.5f,135),
            new Vector4(16.2f,183.4f,8.7f,314),new Vector4(34.5f,179.7f,10.4f,52) })
            Plant(tree,"後山林木",t.x,t.y,t.z,t.w);
        int index=0;
        foreach(var t in new[] {
            new Vector4(21.9f,147.6f,.62f,30),new Vector4(27.6f,148.65f,.75f,132),
            new Vector4(22.7f,151.0f,1.0f,74),new Vector4(28.35f,151.5f,.78f,195),
            new Vector4(21.9f,152.8f,.6f,108),new Vector4(29f,153.7f,1.1f,241),
            new Vector4(23f,156.6f,.85f,61),new Vector4(29.5f,159,.95f,154),
            new Vector4(24.6f,162.1f,.72f,12),new Vector4(10.8f,162,.85f,213),
            new Vector4(17f,165.9f,.68f,77),new Vector4(9.25f,169.9f,.82f,128),
            new Vector4(15.9f,172.2f,.76f,286),new Vector4(10.8f,174.3f,.95f,171),
            new Vector4(17.6f,174.8f,.8f,51),new Vector4(16.9f,180.1f,1.02f,195) })
            Plant(plants.GetChild(index++%plants.childCount),"路邊蕨叢",t.x,t.y,t.z,t.w);
    }

    public static void Build(Transform parent)
    {
        var root = new GameObject(CaveName).transform;
        root.SetParent(parent, false);
        var ground = GameObject.Find("Forest valley ground").GetComponent<MeshCollider>();
        FitRearGround(ground);
        Physics.SyncTransforms();
        float Floor(float x, float z) => ground.Raycast(new Ray(new Vector3(x, 75, z), Vector3.down), out var hit, 120)
            ? hit.point.y : 7.15f;
        var vertices = new List<Vector3>();
        var uv = new List<Vector2>();
        var faces = new[] { new List<int>(), new List<int>(), new List<int>(), new List<int>() };

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
        var ridge = Ring(137, 20.4f, 15.9f, 11.8f, true);
        var hillside = Ring(144.2f, 21.8f, 11.2f, 8.45f, true);
        var rearLip = Ring(150.4f, 25.3f, 4.85f, 5.85f, true);
        Strip(crest, ridge, 0, true); Strip(ridge, hillside, 0, true); Strip(hillside, rearLip, 0, true);
        // The two openings share the same bedrock shell. The party crosses the
        // broad middle chamber before the narrower trail bends out through the far slope.
        var previous = mouth;
        foreach (var station in new[] { new Vector4(123,24.8f,3.9f,3.85f),new Vector4(125.6f,24.7f,3.65f,3.5f),
            new Vector4(128,23.35f,4.15f,3.85f),new Vector4(130.5f,21.95f,4.4f,4.4f),
            new Vector4(133.7f,20.6f,4.95f,4.75f),new Vector4(136.4f,19.9f,5.0f,4.5f),
            new Vector4(139.3f,20.6f,4.7f,4.55f),new Vector4(141.7f,21.85f,3.95f,4.45f),
            new Vector4(144,23.15f,3.25f,4.05f),new Vector4(146.8f,24.5f,3.05f,4.30f),
            new Vector4(149.5f,25.25f,3.3f,4.60f) })
        {
            var next = Ring(station.x,station.y,station.z,station.w);
            Strip(previous, next, 1, false); previous = next;
        }
        Strip(previous, rearLip, 0, false);

        // A sloping rock spur grows out of the left wall. Its steep inner face
        // shelters the resting area and its taper points toward the right-hand trail.
        Vector3[] Spur(float z, float tip, float width, float height)
        {
            float h = Floor(tip,z);
            return new[] {
                new Vector3(tip-width, h-.55f, z), new Vector3(tip-width, h+height*.88f,z-.18f),
                new Vector3(tip-width*.44f,h+height,z+.10f), new Vector3(tip-.26f,h+height*.83f,z),
                new Vector3(tip+.10f,h+height*.39f,z+.17f), new Vector3(tip+.22f,h-.20f,z+.25f)
            };
        }
        var shoulder = Spur(134.1f,15.35f,2.8f,.12f);
        foreach (var profile in new[] {
            new Vector4(135.5f,16.65f,3.7f,1.35f), new Vector4(138,17.7f,4.1f,2.1f),
            new Vector4(139.85f,18.8f,4.8f,2.22f), new Vector4(141.65f,19.3f,4.4f,1.72f),
            new Vector4(144,19.7f,3.65f,.67f), new Vector4(145.6f,19.6f,2.4f,.10f) })
        {
            var next = Spur(profile.x,profile.y,profile.z,profile.w);
            Strip(shoulder,next,1,true); shoulder=next;
        }

        float TrailX(float z) => z <= 137.5f
            ? Mathf.Lerp(24.55f,20.05f,Mathf.InverseLerp(124,137.5f,z))
            : Mathf.Lerp(20.05f,25.45f,Mathf.InverseLerp(137.5f,149.5f,z));

        // Dress the existing continuous, collision-tested ground through the mouth.
        Vector3[] FloorRow(float z)
        {
            float x = TrailX(z);
            var row = new Vector3[13];
            for (int i = 0; i < row.Length; i++)
            {
                float px = x + (i-6) * Mathf.Lerp(.88f,.33f,Mathf.InverseLerp(136,150,z));
                float pz = z + .46f * Mathf.Sin(i * 1.4f + .6f);
                float leafCover = Mathf.SmoothStep(0,1,Mathf.InverseLerp(141.5f,149,pz));
                float edge = Mathf.Pow(Mathf.Abs(i-6)/6f,4);
                float lift = .095f - leafCover*(.21f + .18f*edge);
                row[i] = new Vector3(px, Floor(px,pz) + lift, pz);
            }
            return row;
        }
        var prevFloor = FloorRow(121.7f);
        for (float z = 122.7f; z < 150; z += 1)
        {
            var next = FloorRow(z); Strip(prevFloor,next,2,true); prevFloor = next;
        }
        // Weathered flakes break up the floor near the walls. Their low edges sit
        // below the ground and leave the path through the middle open.
        var random = new System.Random(1930);
        for (int k = 0; k < 29; k++)
        {
            float z = 120.4f + (float)random.NextDouble() * 23.5f;
            float axis = TrailX(z);
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
        // A worn strip continues from the leaf-covered rear lip through the left turn.
        Vector3[] PathRow(Vector2 center, Vector2 across, float at)
        {
            float width = 1.46f + .18f*Mathf.Sin(at*1.3f);
            var row = new Vector3[7];
            for (int i = 0; i < row.Length; i++)
            {
                float u = (i-3)/3f;
                Vector2 p = center + across * (u*width + .12f*Mathf.Sin(at*.8f+u));
                float lift = .048f - .065f * Mathf.Pow(Mathf.Abs(u),5);
                row[i] = new Vector3(p.x,Floor(p.x,p.y)+lift,p.y);
            }
            return row;
        }
        Vector3[] lastRow=null;
        float walked=0;
        for (int k=ForestStart-1; k<trailStations.Length-1; k++)
        {
            Vector2 a=trailStations[k], b=trailStations[k+1], direction=(b-a).normalized;
            Vector2 right=new Vector2(direction.y,-direction.x);
            float length=Vector2.Distance(a,b);
            int sections=Mathf.CeilToInt(length/.6f);
            for (int s=k==ForestStart-1?0:1; s<=sections; s++)
            {
                float t=(float)s/sections;
                var row=PathRow(Vector2.Lerp(a,b,t),right,walked+length*t);
                if(lastRow!=null) Strip(lastRow,row,3,true);
                lastRow=row;
            }
            walked+=length;
        }
        var mesh = new Mesh {name = "Mehebu cave shell"};
        mesh.SetVertices(vertices); mesh.SetUVs(0,uv); mesh.subMeshCount = faces.Length;
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
        shell.GetComponent<MeshRenderer>().sharedMaterials = new[] {cliff,inner,soil,AssetDatabase.LoadAssetAtPath<Material>(Folder+"Walked forest earth.mat")};
        var interior = new GameObject("穿過窟口後的內室", typeof(BoxCollider), typeof(Chapter4RockShelter));
        interior.transform.SetParent(root,false);
        interior.transform.SetPositionAndRotation(new Vector3(20.75f,8.95f,135.8f),Quaternion.Euler(0,-13,0));
        var volume = interior.GetComponent<BoxCollider>();
        volume.isTrigger = true; volume.size = new Vector3(8.3f,4.8f,17.6f);

        BoxCollider Passage(string name, Vector2 start, Vector2 end, float width)
        {
            var box = new GameObject(name,typeof(BoxCollider)).GetComponent<BoxCollider>();
            box.transform.SetParent(root,false);
            Vector2 mid = (start+end)*.5f, heading=end-start;
            box.transform.SetPositionAndRotation(new Vector3(mid.x,Floor(mid.x,mid.y)+1.9f,mid.y),
                Quaternion.LookRotation(new Vector3(heading.x,0,heading.y)));
            box.isTrigger=true; box.size=new Vector3(width,5.8f,heading.magnitude);
            return box;
        }
        var refuge = interior.GetComponent<Chapter4RockShelter>();
        refuge.entrance = Passage("洞口覆岩",new Vector2(24.15f,120.9f),new Vector2(24.15f,127.8f),8.8f);
        refuge.bend = Passage("前室覆岩",new Vector2(24.6f,125.4f),new Vector2(20.5f,134),8.2f);
        refuge.rearTrail = Passage("岩肩後方的山徑",new Vector2(20.1f,137.2f),new Vector2(25.8f,152.9f),7.2f);
        refuge.farHollow = Passage("後山林下的凹地",new Vector2(25.35f,150.5f),new Vector2(29.7f,165),10.4f);
        refuge.forestBend = Passage("後洞口左彎林徑", new Vector2(25.6f,150.95f), new Vector2(16.6f,160.4f),7.45f);
        refuge.rally = Passage("左側林徑會合處",new Vector2(19.8f,156.65f),new Vector2(11.1f,170.6f),8.1f);
        refuge.escapeTrail = new Transform[RefugeStation + 1];
        for (int i=0; i<refuge.escapeTrail.Length; i++)
        {
            Vector2 p=trailStations[i];
            var marker=new GameObject("後山路線 "+(i+1)).transform;
            marker.SetParent(root,false);
            marker.position=new Vector3(p.x,Floor(p.x,p.y)+.08f,p.y);
            refuge.escapeTrail[i]=marker;
        }
        var skylight = new GameObject("後山出口的天光",typeof(Light)).GetComponent<Light>();
        skylight.transform.SetParent(root,false); skylight.transform.position=new Vector3(24.25f,Floor(24.25f,146.25f)+2.65f,146.25f);
        skylight.type=LightType.Point; skylight.color=new Color(.83f,.92f,.96f); skylight.range=14; skylight.intensity=3.25f;
        PlantRearOpening(root,ground,Floor);
        var bounce = new GameObject("洞口反射的天光",typeof(Light)).GetComponent<Light>();
        bounce.transform.SetParent(root,false); bounce.transform.position = new Vector3(23.6f,9.85f,125.6f);
        bounce.type = LightType.Point; bounce.color = new Color(.81f,.86f,.89f); bounce.range = 15; bounce.intensity = 2.2f;
        var reflected = new GameObject("洞內散光",typeof(Light)).GetComponent<Light>();
        reflected.transform.SetParent(root,false); reflected.transform.position = new Vector3(19.6f,10.2f,137);
        reflected.type = LightType.Point; reflected.color = new Color(.84f,.79f,.69f); reflected.range=10; reflected.intensity=.95f;
    }
}
