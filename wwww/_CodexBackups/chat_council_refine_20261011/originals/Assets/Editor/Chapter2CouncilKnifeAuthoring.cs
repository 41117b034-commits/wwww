using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// A game-scale interpretation of the documented Seediq iron knife and open wooden
// scabbard (Shung Ye Museum M137701-02), not a replica attributed to a named person.
// Keep the grip origin used by CouncilDrama; only replace the prop's geometry.
public static class Chapter2CouncilKnifeAuthoring
{
    const string Folder = "Assets/Chapter2/Props";
    public static readonly Vector3 Tip = new Vector3(-.013f, .50f, 0);
    static readonly float[] Y = { .061f, .12f, .24f, .36f, .44f, .48f, .50f };
    static readonly float[] Back = { -.018f, -.019f, -.021f, -.023f, -.024f, -.021f, -.013f };
    static readonly float[] Edge = { .019f, .027f, .036f, .044f, .040f, .021f, -.013f };
    sealed class Parts
    {
        public Mesh blade, handle, sheath, collar;
        public Material iron, edge, wood, sheathWood, tie;
    }

    [MenuItem("Tools/Chapter 2/Update Council Seediq Knives")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        var chapter = Object.FindFirstObjectByType<Chapter2Controller>();
        if (!chapter || chapter.gameObject.scene.path != "Assets/Scenes/第二章.unity")
            throw new InvalidOperationException("Open Chapter 2 first.");
        var parts = BuildAssets();
        int count = 0;
        foreach (var drama in chapter.nightGroup.GetComponentsInChildren<Chapter2CouncilDrama>(true))
        {
            if (!drama.hasKnife || !drama.knife) continue;
            Rebuild(drama.knife, parts);
            drama.knifeTip = drama.knife.Find("刀尖");
            EditorUtility.SetDirty(drama);
            count++;
        }
        if (count == 0) throw new InvalidOperationException("No council knives were found.");
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(chapter.gameObject.scene);
        EditorSceneManager.SaveScene(chapter.gameObject.scene);
        Debug.Log("[Council knives] Updated " + count + " Seediq knife props.");
    }

    public static Transform CreateKnife(Transform parent)
    {
        var root = new GameObject("賽德克佩刀").transform;
        root.SetParent(parent, false);
        root.position = parent.position + Vector3.up * .8f;
        RefreshKnife(root);
        return root;
    }

    public static void RefreshKnife(Transform root) { Rebuild(root, BuildAssets()); }

    static void Rebuild(Transform root, Parts p)
    {
        // These children belong exclusively to this generated prop. Preserve the
        // root reference, placement and the actor's existing authored hand pose.
        for (int i = root.childCount - 1; i >= 0; i--) Object.DestroyImmediate(root.GetChild(i).gameObject);
        root.name = "賽德克佩刀";
        MeshPart(root, "刀刃", p.blade, p.iron, p.edge);
        MeshPart(root, "木製刀柄", p.handle, p.wood);
        MeshPart(root, "刀柄鐵箍", p.collar, p.iron);
        var tip = new GameObject("刀尖").transform;
        tip.SetParent(root, false); tip.localPosition = Tip;
        var sheath = new GameObject("刀鞘").transform;
        sheath.SetParent(root, false);
        MeshPart(sheath, "單面木鞘", p.sheath, p.sheathWood);
        MeshPart(sheath, "鞘內刀身", p.blade, p.iron, p.edge);
        // Eleven retaining wires cross the open face; not decorative sword guards.
        for (int i = 0; i < 11; i++)
        {
            float y = Mathf.Lerp(.086f, .467f, i / 10f);
            float left = Sample(Back, y) - .009f, right = Sample(Edge, y) + .009f;
            var wire = new GameObject("固定鐵條 " + (i + 1).ToString("00")).transform;
            wire.SetParent(sheath, false);
            Bar(wire, "橫條", new Vector3((left + right) * .5f, y, .009f), new Vector3(right - left, .003f, .003f), p.iron);
            Bar(wire, "左扣", new Vector3(left, y, -.003f), new Vector3(.003f, .003f, .027f), p.iron);
            Bar(wire, "右扣", new Vector3(right, y, -.003f), new Vector3(.003f, .003f, .027f), p.iron);
        }
        // Two simple dark-fibre bindings, without unrelated tribal motifs.
        foreach (float y in new[] { .15f, .32f })
        {
            float left = Sample(Back, y) - .012f, right = Sample(Edge, y) + .012f;
            Bar(sheath, "背面繫帶", new Vector3((left + right) * .5f, y, -.037f), new Vector3(right - left, .012f, .005f), p.tie);
        }
    }

    static Parts BuildAssets()
    {
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Chapter2", "Props");
        var p = new Parts();
        p.iron = Material("CouncilBlade", new Color(.24f, .255f, .25f), .68f, .25f);
        p.edge = Material("CouncilKnifeEdge", new Color(.52f, .53f, .48f), .75f, .39f);
        p.wood = Material("CouncilHilt", new Color(.32f, .18f, .085f), 0, .2f);
        p.sheathWood = Material("CouncilScabbardWood", new Color(.38f, .24f, .12f), 0, .16f);
        p.tie = Material("CouncilKnifeBinding", new Color(.12f, .095f, .052f), 0, .1f);
        var grain = WoodGrain();
        p.wood.SetTexture("_BaseMap", grain); p.sheathWood.SetTexture("_BaseMap", grain);
        EditorUtility.SetDirty(p.wood); EditorUtility.SetDirty(p.sheathWood);

        var blade = new Builder(2);
        for (int i = 0; i < Y.Length - 1; i++)
        {
            // Broad forged face, narrow bevel on the cutting edge only.
            float b0 = Mathf.Lerp(Back[i], Edge[i], .77f), b1 = Mathf.Lerp(Back[i + 1], Edge[i + 1], .77f);
            float t0 = .003f, t1 = i == Y.Length - 2 ? .00025f : .003f;
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 a = new Vector3(Back[i], Y[i], t0 * side), b = new Vector3(b0, Y[i], t0 * side);
                Vector3 c = new Vector3(b1, Y[i + 1], t1 * side), d = new Vector3(Back[i + 1], Y[i + 1], t1 * side);
                blade.Quad(a, b, c, d, 0, side < 0);
                blade.Quad(b, new Vector3(Edge[i], Y[i], .0003f * side), new Vector3(Edge[i + 1], Y[i + 1], .00025f * side), c, 1, side < 0);
            }
            blade.Quad(new Vector3(Back[i], Y[i], -.003f), new Vector3(Back[i], Y[i], .003f),
                new Vector3(Back[i + 1], Y[i + 1], t1), new Vector3(Back[i + 1], Y[i + 1], -t1), 0);
            blade.Quad(new Vector3(Edge[i], Y[i], .0003f), new Vector3(Edge[i], Y[i], -.0003f),
                new Vector3(Edge[i + 1], Y[i + 1], -.00025f), new Vector3(Edge[i + 1], Y[i + 1], .00025f), 1);
        }
        blade.Quad(new Vector3(Back[0], Y[0], -.003f), new Vector3(Edge[0], Y[0], -.0003f),
            new Vector3(Edge[0], Y[0], .0003f), new Vector3(Back[0], Y[0], .003f), 0);
        p.blade = Save("CouncilKnifeBlade", blade.Finish());
        p.handle = Save("CouncilKnifeHandle", Oval(new[] { -.115f, -.10f, -.055f, .016f, .048f, .062f },
            new[] { .027f, .025f, .0215f, .019f, .0175f, .017f }, new[] { -.009f, -.006f, -.002f, 0f, 0f, 0f }, .80f));
        p.collar = Save("CouncilKnifeCollar", Oval(new[] { .043f, .0615f }, new[] { .019f, .0185f }, new[] { 0f, 0f }, .83f));
        var outline = new List<Vector2>();
        outline.Add(new Vector2(Back[0] - .012f, .059f));
        outline.Add(new Vector2(Edge[0] + .012f, .059f));
        for (int i = 1; i < Y.Length; i++) outline.Add(new Vector2(Edge[i] + .012f, Y[i]));
        outline.Add(new Vector2(Tip.x, .514f));
        for (int i = Y.Length - 1; i >= 1; i--) outline.Add(new Vector2(Back[i] - .012f, Y[i]));
        p.sheath = Save("CouncilKnifeScabbard", Slab(outline, -.033f, -.006f));
        return p;
    }

    static float Sample(float[] values, float y)
    {
        for (int i = 1; i < Y.Length; i++) if (y <= Y[i]) return Mathf.Lerp(values[i - 1], values[i], Mathf.InverseLerp(Y[i - 1], Y[i], y));
        return values[values.Length - 1];
    }
    static Material Material(string name, Color color, float metallic, float smooth)
    {
        string path = Folder + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!material) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
        material.SetColor("_BaseColor", color); material.SetFloat("_Metallic", metallic); material.SetFloat("_Smoothness", smooth);
        EditorUtility.SetDirty(material); return material;
    }
    static Texture2D WoodGrain()
    {
        string path = Folder + "/CouncilKnifeWoodGrain.png";
        if (!File.Exists(path))
        {
            var t = new Texture2D(128, 256, TextureFormat.RGB24, false);
            for (int y = 0; y < t.height; y++) for (int x = 0; x < t.width; x++)
            {
                float noise = Mathf.PerlinNoise(x * .22f, y * .007f);
                float grain = .72f + .24f * noise + .035f * Mathf.Sin(x * 1.8f + noise * 4);
                t.SetPixel(x, y, new Color(grain, grain, grain));
            }
            t.Apply(); File.WriteAllBytes(path, t.EncodeToPNG()); Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static Mesh Save(string name, Mesh generated)
    {
        string path = Folder + "/" + name + ".asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path); generated.name = name;
        if (!mesh) { AssetDatabase.CreateAsset(generated, path); mesh = generated; }
        else
        {
            // Updating through Mesh setters also refreshes the Editor's native GPU
            // buffers; CopySerialized alone can leave the old blade on screen.
            mesh.Clear(); mesh.name = name; mesh.vertices = generated.vertices;
            mesh.normals = generated.normals; mesh.uv = generated.uv;
            mesh.subMeshCount = generated.subMeshCount;
            for (int i = 0; i < generated.subMeshCount; i++) mesh.SetTriangles(generated.GetTriangles(i), i);
            mesh.RecalculateBounds(); mesh.UploadMeshData(false);
            Object.DestroyImmediate(generated); EditorUtility.SetDirty(mesh);
        }
        return mesh;
    }
    static Transform MeshPart(Transform parent, string name, Mesh mesh, params Material[] materials)
    {
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(parent, false);
        go.GetComponent<MeshFilter>().sharedMesh = mesh; go.GetComponent<MeshRenderer>().sharedMaterials = materials;
        return go.transform;
    }
    static void Bar(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>()); go.transform.SetParent(parent, false);
        go.transform.localPosition = position; go.transform.localScale = scale; go.GetComponent<Renderer>().sharedMaterial = material;
    }
    static Mesh Oval(float[] heights, float[] radius, float[] shift, float depth)
    {
        var b = new Builder(1); const int segments = 12;
        for (int ring = 0; ring < heights.Length - 1; ring++) for (int i = 0; i < segments; i++)
        {
            float a = i * Mathf.PI * 2 / segments, n = (i + 1) * Mathf.PI * 2 / segments;
            b.Quad(OvalPoint(ring, a), OvalPoint(ring, n), OvalPoint(ring + 1, n), OvalPoint(ring + 1, a), 0, true);
        }
        for (int i = 0; i < segments; i++)
        {
            float a = i * Mathf.PI * 2 / segments, n = (i + 1) * Mathf.PI * 2 / segments;
            b.Triangle(new Vector3(shift[0], heights[0], 0), OvalPoint(0, a), OvalPoint(0, n), 0);
            int last = heights.Length - 1;
            b.Triangle(new Vector3(shift[last], heights[last], 0), OvalPoint(last, n), OvalPoint(last, a), 0);
        }
        return b.Finish();
        Vector3 OvalPoint(int ring, float angle) { return new Vector3(shift[ring] + Mathf.Cos(angle) * radius[ring], heights[ring], Mathf.Sin(angle) * radius[ring] * depth); }
    }
    static Mesh Slab(List<Vector2> outline, float back, float front)
    {
        var b = new Builder(1); Vector2 center = new Vector2(.003f, .27f);
        for (int i = 0; i < outline.Count; i++)
        {
            Vector2 a = outline[i], n = outline[(i + 1) % outline.Count];
            b.Triangle(new Vector3(center.x, center.y, front), new Vector3(a.x, a.y, front), new Vector3(n.x, n.y, front), 0);
            b.Triangle(new Vector3(center.x, center.y, back), new Vector3(n.x, n.y, back), new Vector3(a.x, a.y, back), 0);
            b.Quad(new Vector3(a.x, a.y, back), new Vector3(n.x, n.y, back), new Vector3(n.x, n.y, front), new Vector3(a.x, a.y, front), 0);
        }
        return b.Finish();
    }
    sealed class Builder
    {
        readonly List<Vector3> v = new List<Vector3>();
        readonly List<Vector2> uv = new List<Vector2>();
        readonly List<int>[] indices;
        public Builder(int submeshes) { indices = new List<int>[submeshes]; for (int i = 0; i < submeshes; i++) indices[i] = new List<int>(); }
        public void Triangle(Vector3 a, Vector3 b, Vector3 c, int material)
        {
            if (Vector3.Cross(b - a, c - a).sqrMagnitude < 1e-14f) return;
            int start = v.Count; v.Add(a); v.Add(b); v.Add(c);
            foreach (var p in new[] { a, b, c }) uv.Add(new Vector2((p.x + p.z) * 12, p.y * 2));
            indices[material].AddRange(new[] { start, start + 1, start + 2 });
        }
        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, int m, bool reverse = false)
        { if (reverse) { Triangle(a, c, b, m); Triangle(a, d, c, m); } else { Triangle(a, b, c, m); Triangle(a, c, d, m); } }
        public Mesh Finish()
        {
            var mesh = new Mesh(); mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.subMeshCount = indices.Length;
            for (int i = 0; i < indices.Length; i++) mesh.SetTriangles(indices[i], i);
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
    }
}
