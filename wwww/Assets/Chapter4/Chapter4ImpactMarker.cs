using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// The outer white ring and gameplay damage use the same radius. This is an
// accessibility marker, deliberately legible through foliage and smoke.
public sealed class Chapter4ImpactMarker : MonoBehaviour
{
    public bool Visible => gameObject.activeSelf;
    public Vector3 Center { get; private set; }
    public float Radius { get; private set; }
    readonly List<Mesh> meshes = new List<Mesh>();
    Material fill, orange, white;
    Collider terrain;
    public void Initialize(Collider ground)
    {
        terrain = ground;
        var shader = Resources.Load<Shader>("Chapter4ThreatMarker");
        fill = Material(shader, new Color(1, .16f, .035f, .14f));
        orange = Material(shader, new Color(1, .32f, .035f, .96f));
        white = Material(shader, new Color(1, .97f, .83f, .98f));
        gameObject.SetActive(false);
    }
    static Material Material(Shader shader, Color color)
    {
        var material = new Material(shader); material.SetColor("_BaseColor", color); return material;
    }
    public void Show(Vector3 center, float radius)
    {
        foreach (Transform child in transform) Destroy(child.gameObject);
        foreach (var mesh in meshes) Destroy(mesh); meshes.Clear();
        Center = center; Radius = radius; transform.position = center;
        Ring("爆炸範圍底色", 0, radius - .25f, fill);
        Ring("傷害邊界白圈", radius - .25f, radius, white);
        Ring("落點橘色內圈", radius - .65f, radius - .42f, orange);
        Line("中心十字橫線", new Vector3(-1.15f, .13f, 0), new Vector3(1.15f, .13f, 0), .16f, white);
        Line("中心十字直線", new Vector3(0, .13f, -1.15f), new Vector3(0, .13f, 1.15f), .16f, white);
        Line("落點直立標柱", Vector3.up * .15f, Vector3.up * 3.6f, .1f, orange);
        gameObject.SetActive(true);
    }
    public void Hide() { gameObject.SetActive(false); }
    public void Pulse(float seconds)
    {
        orange.SetColor("_BaseColor", new Color(1, .28f + .10f * Mathf.Sin(seconds * 7), .03f, .96f));
    }
    Vector3 Ground(float x, float z)
    {
        var point = Center + new Vector3(x, 0, z);
        if (terrain.Raycast(new Ray(point + Vector3.up * 30, Vector3.down), out var hit, 60)) point.y = hit.point.y;
        return point - Center + Vector3.up * .075f;
    }
    void Ring(string label, float inner, float outer, Material material)
    {
        const int count = 80;
        var vertices = new Vector3[(count + 1) * 2]; var triangles = new int[count * 6];
        for (int i = 0; i <= count; i++)
        {
            float a = i * Mathf.PI * 2 / count;
            vertices[i * 2] = Ground(Mathf.Cos(a) * inner, Mathf.Sin(a) * inner);
            vertices[i * 2 + 1] = Ground(Mathf.Cos(a) * outer, Mathf.Sin(a) * outer);
            if (i == count) break;
            int j = i * 6, v = i * 2;
            triangles[j] = v; triangles[j + 1] = v + 2; triangles[j + 2] = v + 1;
            triangles[j + 3] = v + 1; triangles[j + 4] = v + 2; triangles[j + 5] = v + 3;
        }
        var mesh = new Mesh { name = label, vertices = vertices, triangles = triangles }; mesh.RecalculateNormals(); mesh.RecalculateBounds(); meshes.Add(mesh);
        var go = new GameObject(label, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(transform, false);
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material; renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
    }
    void Line(string label, Vector3 from, Vector3 to, float width, Material material)
    {
        var go = new GameObject(label); go.transform.SetParent(transform, false);
        var line = go.AddComponent<LineRenderer>(); line.useWorldSpace = false; line.positionCount = 2;
        line.SetPosition(0, from); line.SetPosition(1, to); line.startWidth = line.endWidth = width;
        line.sharedMaterial = material; line.numCapVertices = 4; line.shadowCastingMode = ShadowCastingMode.Off;
    }
    void OnDestroy()
    {
        foreach (var mesh in meshes) if (mesh) Destroy(mesh);
        foreach (var material in new[] { fill, orange, white }) if (material) Destroy(material);
    }
}
