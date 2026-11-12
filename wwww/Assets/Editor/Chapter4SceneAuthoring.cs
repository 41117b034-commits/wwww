using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Creates the independent Chapter 4 environment. Existing scenes are never rebuilt.</summary>
public static class Chapter4SceneAuthoring
{
    public const string ScenePath = "Assets/Scenes/第四章.unity";
    const string Folder = "Assets/Chapter4/Environment";
    const string DonorPath = "Assets/Scenes/第二章.unity";
    static readonly Dictionary<Material, Material> vegetationMaterials = new Dictionary<Material, Material>();
    static readonly List<Vector3[]> paths = new List<Vector3[]>();
    static readonly Vector3[] clearings =
    {
        new Vector3(0, 0, -3), new Vector3(18, 3.7f, 63),
        new Vector3(7, 6.2f, 99), new Vector3(20, 7.2f, 139)
    };
    static readonly float[] clearingRadii = { 9f, 6.3f, 5.8f, 10f };
    static Material earth, road, rock, timber, boards, linen, metal, bark;
    static GameObject rockPrefab;
    static Font font;
    static Vector3[] mainPath;
    static int vegetationMaterialId;

    [MenuItem("Tools/Chapter 4/Create Forest Camp and Route")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before creating Chapter 4.");
        if (File.Exists(ScenePath))
            throw new InvalidOperationException("第四章 already exists. Edit the saved scene instead of rebuilding it.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Save the open scene changes before creating Chapter 4.");

        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();
        vegetationMaterials.Clear();
        vegetationMaterialId = 0;
        paths.Clear();
        var donor = EditorSceneManager.OpenPreviewScene(DonorPath);
        try
        {
            var donorTransforms = donor.GetRootGameObjects()
                .SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
            var treeDonors = donorTransforms.Where(t => t.name.StartsWith("Grove tree "))
                .Where(t => t.GetComponentInChildren<Renderer>(true)).Take(14).ToArray();
            var understory = donorTransforms.FirstOrDefault(t =>
                t.name == "Undergrowth · fern grass shrub flower pockets");
            var plantDonors = understory == null ? new Transform[0] : understory.Cast<Transform>()
                .Where(t => t.GetComponentInChildren<Renderer>(true)).Take(20).ToArray();
            if (treeDonors.Length == 0 || plantDonors.Length == 0)
                throw new InvalidOperationException("Chapter 2 forest donors were not found; no substitute forest was generated.");
            rockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Rock_pack/prefab/Rock_03.prefab");
            font = AssetDatabase.LoadAssetAtPath<Font>("Assets/中文字體/STKAITI.TTF");
            if (!rockPrefab || !font) throw new InvalidOperationException("The existing rock prefab or Chinese font is missing.");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "第四章";
            ConfigurePaths();
            Materials();
            Landscape();
            var props = Group("03 避難營地與沿途地標", null);
            Camp(props);
            RestClearings(props);
            BlockedBranches(props);
            Gathering(props);
            Forest(treeDonors, plantDonors);
            LightingAndAmbience();
            CreatePlayerAndPresentation();
            Chapter4Grounding.Apply(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            var buildScenes = EditorBuildSettings.scenes.ToList();
            if (!buildScenes.Any(s => s.path == ScenePath))
            {
                buildScenes.Add(new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = buildScenes.ToArray();
            }
            var view = SceneView.lastActiveSceneView;
            if (view) view.LookAt(new Vector3(0, 2, 6), Quaternion.Euler(24, 12, 0), 28);
            Chapter2GameViewScaleGuard.Restore();
            var allObjects = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
            Debug.Log("[Chapter4] Created " + ScenePath + "; ground=" +
                allObjects.Count(t => t.name == "Forest valley ground") + "; trees=" +
                allObjects.Count(t => t.name.StartsWith("山林樹木 ")) + "; rocks=" +
                allObjects.Count(t => t.name == "風化岩石") + "; route checkpoints=" +
                allObjects.Count(t => t.name.StartsWith("路線中心 ")) + ". Scene remains open for inspection.");
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(donor);
            vegetationMaterials.Clear();
        }
    }

    static void ConfigurePaths()
    {
        mainPath = Smooth(new[]
        {
            new Vector3(0, 0, -3), new Vector3(0, 0, 0), new Vector3(0, .1f, 9),
            new Vector3(3, .4f, 18), new Vector3(10, 1.1f, 28), new Vector3(18, 1.9f, 36),
            new Vector3(25, 2.7f, 43), new Vector3(26, 3.1f, 53), new Vector3(18, 3.7f, 63),
            new Vector3(8, 4.6f, 70), new Vector3(1, 5.2f, 78), new Vector3(0, 5.8f, 88),
            new Vector3(7, 6.2f, 99), new Vector3(17, 6.6f, 108), new Vector3(24, 7, 119),
            new Vector3(22, 7.2f, 131), new Vector3(20, 7.2f, 139)
        });
        paths.Add(mainPath);
        paths.Add(Smooth(new[]
        {
            new Vector3(10, 1.1f, 28), new Vector3(5, 1.2f, 31),
            new Vector3(-1, 1.45f, 35), new Vector3(-5, 1.65f, 40)
        }));
        paths.Add(Smooth(new[]
        {
            new Vector3(8, 4.6f, 70), new Vector3(1, 4.75f, 72),
            new Vector3(-8, 5, 77), new Vector3(-17, 5.2f, 82)
        }));
    }

    static Vector3[] Smooth(Vector3[] anchors)
    {
        var result = new List<Vector3>();
        for (int i = 0; i < anchors.Length - 1; i++)
        {
            Vector3 a = anchors[Mathf.Max(0, i - 1)], b = anchors[i];
            Vector3 c = anchors[i + 1], d = anchors[Mathf.Min(anchors.Length - 1, i + 2)];
            int steps = Mathf.Max(3, Mathf.CeilToInt(Vector3.Distance(b, c) / .9f));
            for (int n = 0; n < steps; n++)
            {
                float t = n / (float)steps, t2 = t * t, t3 = t2 * t;
                var p = .5f * ((2 * b) + (-a + c) * t +
                    (2 * a - 5 * b + 4 * c - d) * t2 + (-a + 3 * b - 3 * c + d) * t3);
                p.y = Mathf.Clamp(p.y, Mathf.Min(b.y, c.y), Mathf.Max(b.y, c.y));
                result.Add(p);
            }
        }
        result.Add(anchors[anchors.Length - 1]);
        return result.ToArray();
    }

    static void NearestRoute(Vector3 point, out float distance, out float height)
    {
        distance = float.MaxValue;
        height = 0;
        foreach (var route in paths)
            for (int i = 1; i < route.Length; i++)
            {
                var a = new Vector2(route[i - 1].x, route[i - 1].z);
                var b = new Vector2(route[i].x, route[i].z);
                var p = new Vector2(point.x, point.z);
                float t = Mathf.Clamp01(Vector2.Dot(p - a, b - a) / Mathf.Max(.0001f, (b - a).sqrMagnitude));
                float candidate = Vector2.Distance(p, Vector2.Lerp(a, b, t));
                if (candidate >= distance) continue;
                distance = candidate;
                height = Mathf.Lerp(route[i - 1].y, route[i].y, t);
            }
    }

    static float Clearance(Vector3 point)
    {
        NearestRoute(point, out float distance, out _);
        float clearance = distance - 3.9f;
        for (int i = 0; i < clearings.Length; i++)
            clearance = Mathf.Min(clearance, Flat(point, clearings[i]) - clearingRadii[i]);
        return clearance;
    }

    static float GroundHeight(float x, float z)
    {
        var p = new Vector3(x, 0, z);
        NearestRoute(p, out float distance, out float height);
        float clearance = distance - 3.9f;
        for (int i = 0; i < clearings.Length; i++)
        {
            float margin = Flat(p, clearings[i]) - clearingRadii[i];
            clearance = Mathf.Min(clearance, margin);
            float blend = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-1, 3, margin));
            height = Mathf.Lerp(height, clearings[i].y, blend);
        }
        float noise = Mathf.PerlinNoise(x * .027f + 8, z * .023f + 2);
        float rise = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(1, 13, clearance)) * (3.5f + noise * 5);
        rise += Mathf.SmoothStep(0, 1, Mathf.InverseLerp(12, 29, clearance)) * (5 + noise * 6);
        // The outer ridge is continuous solid terrain, not a visible square ground edge.
        rise += Mathf.SmoothStep(0, 1, Mathf.InverseLerp(28, 43, clearance)) * (26 + noise * 8);
        float texture = Mathf.PerlinNoise(x * .13f + 2, z * .12f + 9) - .5f;
        return height + rise + texture * .5f * Mathf.SmoothStep(0, 1, Mathf.InverseLerp(1, 7, clearance));
    }

    static float Flat(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

    // Match the saved terrain's actual triangles, including junction transitions.
    static float SurfaceHeight(float x, float z)
    {
        const float step = 1.5f;
        float gx = (x + 100) / step, gz = (z + 65) / step;
        int ix = Mathf.FloorToInt(gx), iz = Mathf.FloorToInt(gz);
        float u = gx - ix, v = gz - iz, xx = ix * step - 100, zz = iz * step - 65;
        float a = GroundHeight(xx, zz), b = GroundHeight(xx + step, zz);
        float c = GroundHeight(xx, zz + step), d = GroundHeight(xx + step, zz + step);
        return u + v <= 1 ? a + u * (b - a) + v * (c - a)
            : d + (1 - u) * (c - d) + (1 - v) * (b - d);
    }

    static void Materials()
    {
        earth = CloneMaterial("Forest earth", "Assets/Chapter3/Environment/Forest earth.mat");
        road = CloneMaterial("Walked forest earth", "Assets/Chapter3/Environment/Walked earth.mat");
        rock = CloneMaterial("Local weathered rock", "Assets/Chapter3/School/School rock.mat");
        timber = CloneMaterial("Dark shelter timber", "Assets/Chapter3/School/School structural timber.mat");
        boards = CloneMaterial("Weathered camp boards", "Assets/Chapter3/School/Weathered school cedar.mat");
        linen = CloneMaterial("Unbleached shelter cloth", "Assets/Chapter3/School/Unbleached cotton.mat");
        metal = CloneMaterial("Weathered barrel hoops", "Assets/Chapter3/School/Weathered iron.mat");
        bark = CloneMaterial("Fallen timber bark", "Assets/Chapter3/Environment/Weathered wood.mat");
        earth.SetColor("_BaseColor", new Color(.54f, .56f, .48f));
        road.SetColor("_BaseColor", new Color(.62f, .55f, .44f));
        rock.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture>("Assets/Rock_pack/Rock_03/rock_03_diffuse.jpg"));
        rock.SetColor("_BaseColor", new Color(.51f, .54f, .49f));
        rock.SetFloat("_Smoothness", .08f);
        linen.SetColor("_BaseColor", new Color(.53f, .50f, .39f));
        foreach (var m in new[] { earth, road, rock, timber, boards, linen, metal, bark }) EditorUtility.SetDirty(m);
    }

    static Material CloneMaterial(string name, string sourcePath)
    {
        var source = AssetDatabase.LoadAssetAtPath<Material>(sourcePath);
        if (!source) throw new InvalidOperationException("Missing material donor: " + sourcePath);
        string path = Folder + "/" + name + ".mat";
        var result = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!result)
        {
            result = new Material(source) { name = name };
            AssetDatabase.CreateAsset(result, path);
        }
        return result;
    }

    static void Landscape()
    {
        var root = Group("01 山谷地形與連續土路", null);
        const int nx = 141, nz = 181;
        const float step = 1.5f;
        var vertices = new Vector3[nx * nz];
        var uv = new Vector2[vertices.Length];
        var triangles = new int[(nx - 1) * (nz - 1) * 6];
        int index = 0;
        for (int z = 0; z < nz; z++)
            for (int x = 0; x < nx; x++)
            {
                int i = z * nx + x;
                float xx = x * step - 100, zz = z * step - 65;
                vertices[i] = new Vector3(xx, GroundHeight(xx, zz), zz);
                uv[i] = new Vector2(xx / 5, zz / 5);
                if (x == nx - 1 || z == nz - 1) continue;
                triangles[index++] = i; triangles[index++] = i + nx; triangles[index++] = i + 1;
                triangles[index++] = i + 1; triangles[index++] = i + nx; triangles[index++] = i + nx + 1;
            }
        MeshObject("Forest valley ground", root, vertices, uv, triangles, earth, true);
        for (int i = 0; i < paths.Count; i++) Ribbon(root, paths[i], i == 0 ? 2.45f : 2.15f, "Forest trail " + i);
        for (int i = 0; i < clearings.Length; i++) Clearing(root, clearings[i], clearingRadii[i] * .87f, i);
    }

    static void Ribbon(Transform parent, Vector3[] route, float halfWidth, string name)
    {
        // Cross strips follow the same height surface; no raised slabs or physical seams.
        const int across = 6;
        var vertices = new Vector3[route.Length * (across + 1)];
        var uv = new Vector2[vertices.Length];
        var triangles = new List<int>();
        float walked = 0;
        for (int i = 0; i < route.Length; i++)
        {
            var forward = route[Mathf.Min(route.Length - 1, i + 1)] - route[Mathf.Max(0, i - 1)];
            var side = Vector3.Cross(Vector3.up, forward).normalized;
            if (i > 0) walked += Vector3.Distance(route[i - 1], route[i]);
            float width = halfWidth + .10f * Mathf.Sin(i * .41f);
            for (int j = 0; j <= across; j++)
            {
                int id = i * (across + 1) + j;
                var p = route[i] + side * Mathf.Lerp(-width, width, j / (float)across);
                p.y = SurfaceHeight(p.x, p.z) + .04f;
                vertices[id] = p; uv[id] = new Vector2(j / (float)across, walked / 3);
                if (i == 0 || j == 0) continue;
                int a = id - across - 2;
                triangles.AddRange(new[] { a, a + across + 1, a + 1, a + 1, a + across + 1, id });
            }
        }
        MeshObject(name, parent, vertices, uv, triangles.ToArray(), road, false);
    }

    static void Clearing(Transform parent, Vector3 center, float radius, int id)
    {
        const int segments = 64;
        var vertices = new Vector3[segments + 2];
        var uv = new Vector2[vertices.Length];
        var triangles = new int[segments * 3];
        vertices[0] = new Vector3(center.x, SurfaceHeight(center.x, center.z) + .045f, center.z);
        uv[0] = new Vector2(center.x / 3, center.z / 3);
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2 / segments;
            float edge = radius * (1 + .07f * Mathf.Sin(angle * 3) + .04f * Mathf.Cos(angle * 7));
            var p = center + new Vector3(Mathf.Cos(angle) * edge, 0, Mathf.Sin(angle) * edge);
            p.y = SurfaceHeight(p.x, p.z) + .045f;
            vertices[i + 1] = p; uv[i + 1] = new Vector2(p.x / 3, p.z / 3);
            if (i < segments) { triangles[i * 3] = 0; triangles[i * 3 + 1] = i + 2; triangles[i * 3 + 2] = i + 1; }
        }
        MeshObject("Camp clearing " + id, parent, vertices, uv, triangles, road, false);
    }

    static void MeshObject(string name, Transform parent, Vector3[] vertices, Vector2[] uv, int[] triangles, Material material, bool solid)
    {
        var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
        mesh.vertices = vertices; mesh.uv = uv; mesh.triangles = triangles;
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        string path = Folder + "/" + name + ".asset";
        var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (saved) { EditorUtility.CopySerialized(mesh, saved); Object.DestroyImmediate(mesh); mesh = saved; }
        else AssetDatabase.CreateAsset(mesh, path);
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(parent, false);
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        go.GetComponent<MeshRenderer>().sharedMaterial = material;
        if (solid) go.AddComponent<MeshCollider>().sharedMesh = mesh;
    }

    static void Camp(Transform parent)
    {
        var camp = Group("起點 · 山林避難營地", parent);
        Rock(camp, new Vector3(-7, 0, -7), new Vector3(5.6f, 4.2f, 4.8f), 25);
        Rock(camp, new Vector3(-6.4f, 0, -11), new Vector3(6.8f, 5.5f, 4.4f), -23);
        Rock(camp, new Vector3(-9.8f, 0, -4.8f), new Vector3(4.5f, 3.2f, 5.6f), -15);
        Shelter(camp, new Vector3(5.8f, 0, -4.5f), -12, "斜布避難棚");
        Crate(camp, new Vector3(7.8f, 0, -6.8f), 18);
        Crate(camp, new Vector3(7.3f, 0, -5.6f), 5);
        Barrel(camp, new Vector3(8.5f, 0, -2.5f));
        Bench(camp, new Vector3(-4.3f, 0, -1.8f), -30);
        Bench(camp, new Vector3(4.5f, 0, -.4f), 20);
        Stretcher(camp, new Vector3(5.4f, 0, -4.3f), -12);
        var logs = Group("收存木料", camp);
        for (int i = 0; i < 4; i++)
            Beam("木料", logs, new Vector3(-4.6f + i * .20f, .17f, -7.4f), new Vector3(-4.7f + i * .20f, .22f, -5.7f), .17f, bark);
    }

    static void RestClearings(Transform parent)
    {
        var first = Group("途中 · 可回望的休息空地", parent);
        Rock(first, new Vector3(23.8f, 0, 64.5f), new Vector3(3.5f, 2.7f, 4.4f), 35);
        Rock(first, new Vector3(20.5f, 0, 68.5f), new Vector3(2.5f, 1.5f, 2.8f), -5);
        var second = Group("途中 · 岩肩空地", parent);
        Rock(second, new Vector3(2, 0, 101.8f), new Vector3(4.3f, 3.2f, 4.1f), -25);
        Rock(second, new Vector3(9.7f, 0, 104.2f), new Vector3(3.2f, 2, 3.4f), 38);
    }

    static void BlockedBranches(Transform parent)
    {
        var rockfall = Group("短支路 · 落石止路", parent);
        Rock(rockfall, new Vector3(-6.8f, 0, 40.5f), new Vector3(3.9f, 3, 3.2f), 40);
        Rock(rockfall, new Vector3(-4.3f, 0, 42.2f), new Vector3(4, 3.5f, 3.6f), -22);
        Rock(rockfall, new Vector3(-3.2f, 0, 39.8f), new Vector3(2, 1.8f, 2.4f), 18);
        var fallen = Group("短支路 · 倒木止路", parent);
        float y = SurfaceHeight(-17, 82);
        Beam("倒木主幹", fallen, new Vector3(-19.1f, y + .75f, 78.8f), new Vector3(-14.2f, y + .8f, 85.2f), 1.02f, bark);
        Beam("倒木分枝", fallen, new Vector3(-16.2f, y + .8f, 82), new Vector3(-18.6f, y + 1.65f, 84.3f), .23f, bark);
        Rock(fallen, new Vector3(-20.4f, 0, 81), new Vector3(4.2f, 3.4f, 4.7f), 12);
        Rock(fallen, new Vector3(-15.5f, 0, 85.9f), new Vector3(4.3f, 3.1f, 4.8f), -15);
    }

    static void Gathering(Transform parent)
    {
        var clearing = Group("終點 · 林間集合地", parent);
        Shelter(clearing, new Vector3(27.2f, 7.2f, 142), 14, "集合處遮棚");
        Bench(clearing, new Vector3(14.2f, 7.2f, 141), -25);
        Bench(clearing, new Vector3(19.2f, 7.2f, 145), 80);
        Crate(clearing, new Vector3(28, 7.2f, 143.2f), 10);
        Barrel(clearing, new Vector3(28.9f, 7.2f, 140.3f));
        Rock(clearing, new Vector3(13.3f, 0, 145.8f), new Vector3(4, 3.5f, 4.6f), -20);
        Rock(clearing, new Vector3(22.4f, 0, 149.6f), new Vector3(5.2f, 4.2f, 4.3f), 22);
        var sign = Group("集合處木牌", clearing);
        sign.position = new Vector3(17.3f, SurfaceHeight(17.3f, 136), 136);
        sign.localRotation = Quaternion.Euler(0, 17, 0);
        Box("木牌柱", sign, new Vector3(0, .76f, 0), new Vector3(.12f, 1.52f, .13f), timber);
        Box("集合處牌面", sign, new Vector3(0, 1.28f, -.03f), new Vector3(1.32f, .43f, .11f), boards);
        var label = new GameObject("集合處", typeof(TextMesh)).GetComponent<TextMesh>();
        label.transform.SetParent(sign, false); label.transform.localPosition = new Vector3(0, 1.28f, -.095f);
        label.font = font; label.fontSize = 56; label.characterSize = .125f;
        label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center;
        label.text = "集合處"; label.color = new Color(.13f, .12f, .08f);
        var textMaterial = new Material(Shader.Find("Chapter3/School World Text") ?? font.material.shader);
        textMaterial.name = "Gathering sign text"; textMaterial.mainTexture = font.material.mainTexture;
        string textPath = Folder + "/Gathering sign text.mat";
        var prior = AssetDatabase.LoadAssetAtPath<Material>(textPath);
        if (prior) { Object.DestroyImmediate(textMaterial); textMaterial = prior; }
        else AssetDatabase.CreateAsset(textMaterial, textPath);
        label.GetComponent<Renderer>().sharedMaterial = textMaterial;
    }

    static void Shelter(Transform parent, Vector3 position, float yaw, string name)
    {
        var root = Group(name, parent); root.position = position; root.rotation = Quaternion.Euler(0, yaw, 0);
        foreach (float x in new[] { -2.7f, 2.7f })
            foreach (float z in new[] { -1.8f, 1.8f })
                Cylinder("棚木柱", root, new Vector3(x, 1.45f, z), .07f, 2.9f, timber);
        Beam("棚前橫梁", root, new Vector3(-2.95f, 2.85f, -1.8f), new Vector3(2.95f, 2.85f, -1.8f), .15f, timber);
        Beam("棚後橫梁", root, new Vector3(-2.95f, 2.55f, 1.8f), new Vector3(2.95f, 2.55f, 1.8f), .15f, timber);
        var cloth = Box("斜掛舊布", root, new Vector3(0, 2.72f, 0), new Vector3(6, .055f, 4.2f), linen);
        cloth.localRotation = Quaternion.Euler(5, 0, 0);
    }

    static void Stretcher(Transform parent, Vector3 position, float yaw)
    {
        var root = Group("空擔架", parent); root.position = position; root.rotation = Quaternion.Euler(0, yaw, 0);
        foreach (float x in new[] { -.42f, .42f })
            Beam("擔架木杆", root, new Vector3(x, .42f, -1.5f), new Vector3(x, .42f, 1.5f), .075f, boards);
        Box("擔架布面", root, new Vector3(0, .42f, 0), new Vector3(.80f, .055f, 2.10f), linen);
        foreach (float x in new[] { -.40f, .40f })
            foreach (float z in new[] { -.86f, .86f })
                Box("擔架腳", root, new Vector3(x, .2f, z), new Vector3(.07f, .4f, .07f), timber);
    }

    static void Crate(Transform parent, Vector3 position, float yaw)
    {
        var root = Group("物資木箱", parent); root.position = position; root.rotation = Quaternion.Euler(0, yaw, 0);
        Box("木箱", root, new Vector3(0, .42f, 0), new Vector3(1.05f, .84f, .72f), boards);
        foreach (float x in new[] { -.38f, .38f })
            Box("木箱束條", root, new Vector3(x, .43f, 0), new Vector3(.085f, .87f, .75f), timber);
    }

    static void Barrel(Transform parent, Vector3 position)
    {
        var root = Group("木桶", parent); root.position = position;
        Cylinder("桶身", root, new Vector3(0, .48f, 0), .34f, .96f, boards);
        foreach (float y in new[] { .16f, .77f }) Cylinder("桶箍", root, new Vector3(0, y, 0), .35f, .045f, metal);
    }

    static void Bench(Transform parent, Vector3 position, float yaw)
    {
        var root = Group("木凳", parent); root.position = position; root.rotation = Quaternion.Euler(0, yaw, 0);
        Box("凳面", root, new Vector3(0, .43f, 0), new Vector3(1.65f, .12f, .42f), boards);
        foreach (float x in new[] { -.59f, .59f }) Box("凳腳", root, new Vector3(x, .2f, 0), new Vector3(.13f, .4f, .35f), timber);
    }

    static void Forest(Transform[] treeDonors, Transform[] plantDonors)
    {
        var trees = Group("02 山林 · 實體樹幹", null);
        var plants = Group("林下蕨草 · 無碰撞", trees);
        var stones = Group("山壁與路肩岩石 · 網格碰撞", trees);
        var random = new System.Random(41112);
        var placed = new List<Vector3>();
        for (int i = 0; i < 165; i++)
        {
            Vector3 p;
            if (i < 90)
            {
                int at = random.Next(5, mainPath.Length - 4);
                var direction = (mainPath[at + 1] - mainPath[at - 1]).normalized;
                var side = Vector3.Cross(Vector3.up, direction).normalized * (i % 2 == 0 ? -1 : 1);
                p = mainPath[at] + side * Range(random, 5.3f, 17);
            }
            else p = new Vector3(Range(random, -53, 68), 0, Range(random, -27, 165));
            if (Clearance(p) < 1.6f || placed.Any(q => Flat(p, q) < 4.2f)) continue;
            p.y = SurfaceHeight(p.x, p.z);
            var wrapper = Group("山林樹木 " + i.ToString("D3"), trees); wrapper.position = p;
            var model = Object.Instantiate(treeDonors[i % treeDonors.Length].gameObject, wrapper).transform;
            model.gameObject.SetActive(true); StripBehavioursAndColliders(model);
            model.Rotate(0, Range(random, 0, 360), 0, Space.World);
            var bounds = Bounds(model);
            model.localScale *= Range(random, 11, 17) / Mathf.Max(.1f, bounds.size.y);
            GroundObject(model, p);
            CopyVegetationMaterials(model);
            var trunk = wrapper.gameObject.AddComponent<CapsuleCollider>();
            trunk.height = 7; trunk.radius = Range(random, .24f, .36f); trunk.center = Vector3.up * 3.5f;
            placed.Add(p);
        }
        for (int i = 0; i < 210; i++)
        {
            int at = random.Next(mainPath.Length);
            var p = mainPath[at] + new Vector3(Range(random, -13, 13), 0, Range(random, -7, 7));
            if (Clearance(p) < -.7f || Clearance(p) > 11) continue;
            p.y = SurfaceHeight(p.x, p.z);
            var model = Object.Instantiate(plantDonors[i % plantDonors.Length].gameObject, plants).transform;
            model.name = "路肩蕨草 " + i.ToString("D3"); model.gameObject.SetActive(true);
            StripBehavioursAndColliders(model); model.Rotate(0, Range(random, 0, 360), 0, Space.World);
            model.localScale *= Range(random, .65f, 1.05f);
            GroundObject(model, p); CopyVegetationMaterials(model);
        }
        for (int i = 0; i < 34; i++)
        {
            int at = random.Next(5, mainPath.Length - 4);
            var direction = (mainPath[at + 1] - mainPath[at - 1]).normalized;
            var side = Vector3.Cross(Vector3.up, direction).normalized * (i % 2 == 0 ? -1 : 1);
            var p = mainPath[at] + side * Range(random, 8, 20);
            if (Clearance(p) < 3) continue;
            float size = Range(random, 2.6f, 5.1f);
            Rock(stones, p, new Vector3(size, size * Range(random, .6f, 1), size * Range(random, .8f, 1.25f)), Range(random, 0, 360));
        }
    }

    static void CopyVegetationMaterials(Transform model)
    {
        foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
        {
            var sourceMaterials = renderer.sharedMaterials;
            for (int i = 0; i < sourceMaterials.Length; i++)
            {
                var source = sourceMaterials[i]; if (!source) continue;
                if (!vegetationMaterials.TryGetValue(source, out var copy))
                {
                    string name = "Vegetation " + (vegetationMaterialId++).ToString("D3");
                    string path = Folder + "/" + name + ".mat";
                    copy = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (!copy) { copy = new Material(source) { name = name }; AssetDatabase.CreateAsset(copy, path); }
                    vegetationMaterials.Add(source, copy);
                }
                sourceMaterials[i] = copy;
            }
            renderer.sharedMaterials = sourceMaterials;
        }
    }

    static void StripBehavioursAndColliders(Transform model)
    {
        foreach (var script in model.GetComponentsInChildren<MonoBehaviour>(true)) if (script) Object.DestroyImmediate(script);
        foreach (var collider in model.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
    }

    static Transform Rock(Transform parent, Vector3 point, Vector3 size, float yaw)
    {
        var model = Object.Instantiate(rockPrefab, parent).transform;
        model.name = "風化岩石"; model.gameObject.SetActive(true); StripBehavioursAndColliders(model);
        var before = Bounds(model);
        model.localScale = Vector3.Scale(model.localScale, new Vector3(
            size.x / Mathf.Max(.01f, before.size.x), size.y / Mathf.Max(.01f, before.size.y), size.z / Mathf.Max(.01f, before.size.z)));
        model.Rotate(0, yaw, 0, Space.World);
        point.y = SurfaceHeight(point.x, point.z) - .11f;
        GroundObject(model, point);
        foreach (var renderer in model.GetComponentsInChildren<Renderer>(true)) renderer.sharedMaterial = rock;
        foreach (var filter in model.GetComponentsInChildren<MeshFilter>(true))
            if (filter.sharedMesh) filter.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
        return model;
    }

    static Bounds Bounds(Transform t)
    {
        var renderers = t.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) throw new InvalidOperationException("Missing visible mesh on " + t.name);
        var result = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) result.Encapsulate(renderers[i].bounds);
        return result;
    }

    static void GroundObject(Transform model, Vector3 point)
    {
        var bounds = Bounds(model);
        model.position += point - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
    }

    static void LightingAndAmbience()
    {
        var sun = new GameObject("陰天漫射天光").AddComponent<Light>();
        sun.type = LightType.Directional; sun.color = new Color(.89f, .94f, 1);
        sun.intensity = .88f; sun.shadows = LightShadows.Soft; sun.shadowStrength = .52f;
        sun.transform.rotation = Quaternion.Euler(38, -38, 0);
        RenderSettings.sun = sun; RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.55f, .60f, .63f);
        RenderSettings.ambientEquatorColor = new Color(.36f, .41f, .38f);
        RenderSettings.ambientGroundColor = new Color(.20f, .23f, .19f);
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(.49f, .56f, .56f); RenderSettings.fogDensity = .0085f;
        var sky = new Material(Shader.Find("Skybox/Procedural")) { name = "Overcast mountain sky" };
        sky.SetColor("_SkyTint", new Color(.47f, .50f, .52f));
        sky.SetColor("_GroundColor", new Color(.27f, .30f, .27f));
        sky.SetFloat("_AtmosphereThickness", 1.6f); sky.SetFloat("_Exposure", .85f); sky.SetFloat("_SunSize", .01f);
        string path = Folder + "/Overcast mountain sky.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing) { Object.DestroyImmediate(sky); sky = existing; } else AssetDatabase.CreateAsset(sky, path);
        RenderSettings.skybox = sky;
        var ambient = new GameObject("遠處林間環境聲").AddComponent<AudioSource>();
        ambient.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Chapter2/Audio/forest_morning.wav");
        ambient.loop = true; ambient.playOnAwake = true; ambient.spatialBlend = 0; ambient.volume = .13f;
    }

    static void CreatePlayerAndPresentation()
    {
        var root = new GameObject("第四章 · 場景入口");
        var controller = root.AddComponent<Chapter4SceneController>();
        var routeRoot = Group("路線資料 · 不可見", root.transform);
        controller.spawnPoint = Point(routeRoot, "起點", new Vector3(0, .10f, -3));
        controller.gatheringPoint = Point(routeRoot, "集合地中心", new Vector3(20, 7.30f, 139));
        var checkpoints = new List<Transform>();
        for (int i = 0; i < mainPath.Length; i += 2)
        {
            var p = mainPath[i]; p.y = SurfaceHeight(p.x, p.z) + .08f;
            checkpoints.Add(Point(routeRoot, "路線中心 " + checkpoints.Count.ToString("D3"), p));
        }
        checkpoints.Add(Point(routeRoot, "路線終點", new Vector3(20, 7.28f, 139)));
        controller.routeCheckpoints = checkpoints.ToArray();
        Physics.SyncTransforms();
        BranchReferences(root.transform, paths[1], "支路一參考點", 3.2f);
        BranchReferences(root.transform, paths[2], "支路二參考點", 2.0f);
        var playerObject = new GameObject("玩家 · 第四章", typeof(CharacterController));
        playerObject.transform.position = controller.spawnPoint.position;
        var character = playerObject.GetComponent<CharacterController>();
        character.height = 1.72f; character.center = new Vector3(0, .88f, 0);
        character.radius = .24f; character.stepOffset = .28f; character.slopeLimit = 45; character.skinWidth = .035f;
        var player = playerObject.AddComponent<Chapter4Player>();
        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera"; cameraObject.transform.SetParent(playerObject.transform, false);
        cameraObject.transform.localPosition = new Vector3(0, 1.65f, 0);
        player.view = cameraObject.GetComponent<Camera>();
        player.view.fieldOfView = 64; player.view.nearClipPlane = .06f; player.view.farClipPlane = 225;
        player.view.allowHDR = true; player.view.backgroundColor = RenderSettings.fogColor;
        controller.player = player;
        controller.presentation = Chapter4Presentation.Build(player.view, font);
        Selection.activeGameObject = root;
    }

    static void BranchReferences(Transform parent, Vector3[] route, string name, float endTrim)
    {
        var group = Group(name, parent);
        float length = 0;
        for (int i = 1; i < route.Length; i++) length += Vector3.Distance(route[i - 1], route[i]);
        float traveled = 0, safeLength = Mathf.Max(0, length - endTrim);
        int count = 0;
        for (int i = 0; i < route.Length; i++)
        {
            if (i > 0) traveled += Vector3.Distance(route[i - 1], route[i]);
            if (traveled > safeLength) break;
            var p = route[i]; p.y = SurfaceHeight(p.x, p.z) + .08f;
            // These are data-only points; the branch obstruction remains a physical visible prop.
            Point(group, "支路中心 " + (count++).ToString("D3"), p);
        }
        if (count < 3) throw new InvalidOperationException("Insufficient safe branch route points: " + name);
    }

    static float Range(System.Random random, float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());
    static Transform Group(string name, Transform parent)
    {
        var t = new GameObject(name).transform;
        if (parent) t.SetParent(parent, false);
        return t;
    }
    static Transform Point(Transform parent, string name, Vector3 p)
    {
        var t = Group(name, parent); t.position = p; return t;
    }
    static Transform Box(string name, Transform parent, Vector3 position, Vector3 size, Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
        go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = material; return go.transform;
    }
    static Transform Cylinder(string name, Transform parent, Vector3 position, float radius, float height, Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder); go.name = name;
        go.transform.SetParent(parent, false); go.transform.localPosition = position;
        go.transform.localScale = new Vector3(radius * 2, height * .5f, radius * 2);
        go.GetComponent<Renderer>().sharedMaterial = material; return go.transform;
    }
    static void Beam(string name, Transform parent, Vector3 a, Vector3 b, float thickness, Material material)
    {
        var t = Cylinder(name, parent, (a + b) * .5f, thickness * .5f, (b - a).magnitude, material);
        t.localRotation = Quaternion.FromToRotation(Vector3.up, b - a);
    }
}
