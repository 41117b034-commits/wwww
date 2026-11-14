using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Original in-project model inspired by the period Otsu two-seat biplane.
// No claim of an exact serial-number or dimensional restoration.
public sealed class Chapter4JapaneseAircraft : MonoBehaviour
{
    public Transform Propeller { get; private set; }
    public float PropellerAngle { get; private set; }
    readonly List<Material> materials = new List<Material>();
    readonly List<Mesh> ownedMeshes = new List<Mesh>();
    Material fabric, metal, dark, red, rim, wood, glass;

    public void Build()
    {
        fabric = Mat("灰綠蒙布", new Color(.60f, .63f, .53f), .08f);
        metal = Mat("金屬整流罩", new Color(.46f, .49f, .47f), .6f);
        dark = Mat("引擎與輪胎", new Color(.055f, .06f, .052f), .12f);
        red = Mat("日之丸識別", new Color(.64f, .025f, .025f), .02f);
        rim = Mat("座艙皮革", new Color(.28f, .18f, .10f), .05f);
        wood = Mat("木製螺旋槳", new Color(.33f, .19f, .075f), .05f);
        glass = Mat("擋風玻璃", new Color(.30f, .45f, .48f), .65f);
        Loft("圓弧機身", new[] { -4.25f, -3.6f, -2.5f, -1.25f, 0f, 1.25f, 2.3f, 2.85f },
            new[] { .07f, .19f, .32f, .46f, .55f, .59f, .58f, .53f },
            new[] { .12f, .28f, .39f, .48f, .54f, .58f, .58f, .52f }, fabric);
        Wing("上層主翼", 5.9f, 1.12f, .1f, 1.55f, fabric);
        Wing("下層主翼", 5.65f, -.48f, .28f, 1.5f, fabric);
        Primitive("水平尾翼", PrimitiveType.Sphere, new Vector3(0, .08f, -3.55f), new Vector3(3.5f, .105f, 1.35f), Quaternion.identity, fabric);
        Primitive("垂直尾舵", PrimitiveType.Sphere, new Vector3(0, .65f, -3.72f), new Vector3(.10f, 1.65f, 1.32f), Quaternion.Euler(0, 0, -5), fabric);
        foreach (float side in new[] { -1f, 1f })
        {
            foreach (float span in new[] { 2.1f, 4.65f })
            {
                Rod("翼間前支柱", new Vector3(side * span, -.46f, .7f), new Vector3(side * span, 1.1f, .62f), .045f, wood);
                Rod("翼間後支柱", new Vector3(side * span, -.46f, -.5f), new Vector3(side * span, 1.1f, -.52f), .045f, wood);
            }
            Rod("斜向翼間張線", new Vector3(side * 2.1f, -.46f, .7f), new Vector3(side * 4.65f, 1.1f, .62f), .014f, metal);
            Rod("反向翼間張線", new Vector3(side * 2.1f, 1.1f, .62f), new Vector3(side * 4.65f, -.46f, .7f), .014f, metal);
            Rod("座艙前支柱", new Vector3(side * .38f, .43f, 1.15f), new Vector3(side * .95f, 1.1f, .68f), .04f, metal);
            Rod("座艙後支柱", new Vector3(side * .43f, .4f, -.6f), new Vector3(side * .95f, 1.1f, -.52f), .04f, metal);
            Rod("起落架前支撐", new Vector3(side * .42f, -.4f, 1.7f), new Vector3(side * .84f, -1.23f, .95f), .055f, metal);
            Rod("起落架後支撐", new Vector3(side * .40f, -.4f, .1f), new Vector3(side * .84f, -1.23f, .95f), .055f, metal);
            Primitive("固定起落輪", PrimitiveType.Cylinder, new Vector3(side * .9f, -1.24f, .95f), new Vector3(.77f, .105f, .77f), Quaternion.Euler(0, 0, 90), dark);
            Primitive("輪轂", PrimitiveType.Cylinder, new Vector3(side * 1.015f, -1.24f, .95f), new Vector3(.35f, .008f, .35f), Quaternion.Euler(0, 0, 90), metal);
            // Recognition marks on both upper and lower surfaces remain visible from the valley.
            foreach (float height in new[] { 1.205f, -.565f })
                Primitive("機翼日之丸", PrimitiveType.Cylinder, new Vector3(side * 3.95f, height, .12f), new Vector3(1.22f, .006f, 1.22f), Quaternion.identity, red);
            Primitive("機身日之丸", PrimitiveType.Cylinder, new Vector3(side * .38f, .02f, -2.1f), new Vector3(.65f, .009f, .65f), Quaternion.Euler(0, 0, 90), red);
        }
        Rod("起落架輪軸", new Vector3(-.95f, -1.24f, .95f), new Vector3(.95f, -1.24f, .95f), .065f, metal);
        Rod("尾橇", new Vector3(0, -.15f, -3.7f), new Vector3(0, -.75f, -4.15f), .06f, wood);
        foreach (float z in new[] { .65f, -.95f })
        {
            Primitive("雙座艙皮革緣", PrimitiveType.Sphere, new Vector3(0, .54f, z), new Vector3(.78f, .13f, .94f), Quaternion.identity, rim);
            Primitive("開放座艙", PrimitiveType.Sphere, new Vector3(0, .592f, z), new Vector3(.61f, .055f, .75f), Quaternion.identity, dark);
            Primitive("飛行員皮帽", PrimitiveType.Sphere, new Vector3(0, .84f, z - .12f), new Vector3(.24f, .29f, .26f), Quaternion.identity, rim);
            Primitive("擋風板", PrimitiveType.Cube, new Vector3(0, .73f, z + .31f), new Vector3(.46f, .28f, .035f), Quaternion.Euler(18, 0, 0), glass);
        }
        Primitive("引擎整流罩", PrimitiveType.Cylinder, new Vector3(0, 0, 2.88f), new Vector3(1.22f, .19f, 1.22f), Quaternion.Euler(90, 0, 0), metal);
        Primitive("星形引擎面", PrimitiveType.Cylinder, new Vector3(0, 0, 3.09f), new Vector3(1.06f, .025f, 1.06f), Quaternion.Euler(90, 0, 0), dark);
        for (int i = 0; i < 9; i++)
        {
            float a = i * Mathf.PI * 2 / 9;
            var v = new Vector3(Mathf.Sin(a), Mathf.Cos(a), 0);
            Rod("星形引擎汽缸", v * .22f + Vector3.forward * 3.16f, v * .50f + Vector3.forward * 3.16f, .095f, metal);
        }
        Propeller = new GameObject("旋轉螺旋槳").transform; Propeller.SetParent(transform, false); Propeller.localPosition = new Vector3(0, 0, 3.29f);
        var blade = Primitive("木製雙葉槳", PrimitiveType.Sphere, Vector3.zero, new Vector3(.19f, 2.65f, .09f), Quaternion.Euler(0, 0, 15), wood);
        blade.SetParent(Propeller, false);
        var hub = Primitive("槳轂", PrimitiveType.Sphere, Vector3.zero, new Vector3(.27f, .27f, .29f), Quaternion.identity, metal); hub.SetParent(Propeller, false);
        CombineStaticParts();
    }
    public void Tick(float dt)
    {
        PropellerAngle = Mathf.Repeat(PropellerAngle + dt * 2350, 360);
        Propeller.localRotation = Quaternion.Euler(0, 0, PropellerAngle);
    }
    Material Mat(string label, Color color, float metallic)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = label, color = color };
        m.SetFloat("_Metallic", metallic); m.SetFloat("_Smoothness", .22f); materials.Add(m); return m;
    }
    Transform Primitive(string label, PrimitiveType type, Vector3 p, Vector3 size, Quaternion rotation, Material material)
    {
        var part = GameObject.CreatePrimitive(type); part.name = label; Destroy(part.GetComponent<Collider>());
        part.transform.SetParent(transform, false); part.transform.localPosition = p; part.transform.localScale = size; part.transform.localRotation = rotation;
        part.GetComponent<Renderer>().sharedMaterial = material; return part.transform;
    }
    void Rod(string label, Vector3 a, Vector3 b, float radius, Material material)
    {
        Primitive(label, PrimitiveType.Cylinder, (a + b) * .5f, new Vector3(radius * 2, (b - a).magnitude * .5f, radius * 2), Quaternion.FromToRotation(Vector3.up, b - a), material);
    }
    void AddMesh(string label, Vector3[] vertices, int[] triangles, Material material)
    {
        var mesh = new Mesh { name = label, vertices = vertices, triangles = triangles }; mesh.RecalculateNormals(); mesh.RecalculateBounds(); ownedMeshes.Add(mesh);
        var go = new GameObject(label, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(transform, false);
        go.GetComponent<MeshFilter>().sharedMesh = mesh; go.GetComponent<MeshRenderer>().sharedMaterial = material;
    }
    void Loft(string label, float[] z, float[] width, float[] height, Material material)
    {
        const int sides = 24; var vertices = new Vector3[z.Length * sides]; var triangles = new int[(z.Length - 1) * sides * 6];
        for (int i = 0; i < z.Length; i++) for (int j = 0; j < sides; j++)
        {
            float a = j * Mathf.PI * 2 / sides; vertices[i * sides + j] = new Vector3(Mathf.Cos(a) * width[i], Mathf.Sin(a) * height[i], z[i]);
            if (i == z.Length - 1) continue;
            int v = i * sides + j, next = i * sides + (j + 1) % sides, t = (i * sides + j) * 6;
            triangles[t] = v; triangles[t + 1] = next; triangles[t + 2] = v + sides;
            triangles[t + 3] = next; triangles[t + 4] = next + sides; triangles[t + 5] = v + sides;
        }
        AddMesh(label, vertices, triangles, material);
    }
    void Wing(string label, float halfSpan, float y, float z, float chord, Material material)
    {
        const int spans = 24, sides = 12; var vertices = new Vector3[(spans + 1) * sides]; var triangles = new int[spans * sides * 6];
        for (int i = 0; i <= spans; i++) for (int j = 0; j < sides; j++)
        {
            float x = (i / (float)spans * 2 - 1) * halfSpan, a = j * Mathf.PI * 2 / sides;
            float tip = Mathf.Lerp(1, .45f, Mathf.InverseLerp(halfSpan * .90f, halfSpan, Mathf.Abs(x)));
            vertices[i * sides + j] = new Vector3(x, y + Mathf.Sin(a) * .065f * tip, z + Mathf.Cos(a) * chord * .5f * tip);
            if (i == spans) continue;
            int v = i * sides + j, next = i * sides + (j + 1) % sides, t = (i * sides + j) * 6;
            triangles[t] = v; triangles[t + 1] = v + sides; triangles[t + 2] = next;
            triangles[t + 3] = next; triangles[t + 4] = v + sides; triangles[t + 5] = next + sides;
        }
        AddMesh(label, vertices, triangles, material);
    }
    void CombineStaticParts()
    {
        var originals = GetComponentsInChildren<MeshFilter>();
        foreach (var material in materials)
        {
            var combine = new List<CombineInstance>(); var remove = new List<GameObject>();
            foreach (var part in originals)
            {
                if (part.transform.IsChildOf(Propeller) || part.GetComponent<Renderer>().sharedMaterial != material) continue;
                combine.Add(new CombineInstance { mesh = part.sharedMesh, transform = transform.worldToLocalMatrix * part.transform.localToWorldMatrix }); remove.Add(part.gameObject);
            }
            if (combine.Count == 0) continue;
            var mesh = new Mesh { name = "日軍複葉機_" + material.name, indexFormat = IndexFormat.UInt32 }; mesh.CombineMeshes(combine.ToArray()); ownedMeshes.Add(mesh);
            var go = new GameObject(mesh.name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(transform, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh; go.GetComponent<MeshRenderer>().sharedMaterial = material;
            foreach (var part in remove) Destroy(part);
        }
    }
    void OnDestroy()
    {
        foreach (var mesh in ownedMeshes) if (mesh) Destroy(mesh);
        foreach (var material in materials) if (material) Destroy(material);
    }
}
