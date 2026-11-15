using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// A quiet, terrain-following boundary. Its outer edge shares the damage radius;
// gaps and a dark keyline preserve readability without colouring the ground.
public sealed class Chapter4ImpactMarker : MonoBehaviour
{
    public bool Visible => gameObject.activeSelf;
    public Vector3 Center { get; private set; }
    public float Radius { get; private set; }
    readonly List<Mesh> meshes = new List<Mesh>();
    Material outline, shadow;
    Collider terrain;
    public void Initialize(Collider ground)
    {
        terrain = ground;
        var shader = Resources.Load<Shader>("Chapter4ThreatMarker");
        outline = Material(shader, new Color(.83f, .82f, .74f, .74f));
        shadow = Material(shader, new Color(.065f, .075f, .06f, .56f));
        shadow.renderQueue = 3049;
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
        Boundary("警戒線暗邊", radius - .14f, radius, shadow);
        Boundary("灰白分段警戒線", radius - .065f, radius, outline);
        // A small hollow diamond marks the centre, with no upright light beam.
        Diamond("落點菱形暗邊", .46f, .13f, shadow);
        Diamond("落點空心菱形", .46f, .055f, outline);
        gameObject.SetActive(true);
    }
    public void Hide() { gameObject.SetActive(false); }
    public void Pulse(float seconds)
    {
        float urgency = 1 - Mathf.Clamp01(seconds / Chapter4RetreatPressure.WarningDuration);
        float breathe = .04f * Mathf.Sin(seconds * (4 + urgency * 2));
        outline.SetColor("_BaseColor", new Color(.83f, .82f, .74f, .67f + urgency * .12f + breathe));
    }
    Vector3 Ground(float x, float z)
    {
        var point = Center + new Vector3(x, 0, z);
        if (terrain.Raycast(new Ray(point + Vector3.up * 30, Vector3.down), out var hit, 60)) point.y = hit.point.y;
        return point - Center + Vector3.up * .075f;
    }
    void Boundary(string label, float inner, float outer, Material material)
    {
        const int dashes = 28, subdivisions = 3;
        var vertices = new Vector3[dashes * (subdivisions + 1) * 2];
        var triangles = new int[dashes * subdivisions * 6];
        for (int dash = 0; dash < dashes; dash++) for (int step = 0; step <= subdivisions; step++)
        {
            float a = (dash + step / (float)subdivisions * .62f) * Mathf.PI * 2 / dashes;
            int v = (dash * (subdivisions + 1) + step) * 2;
            vertices[v] = Ground(Mathf.Cos(a) * inner, Mathf.Sin(a) * inner);
            vertices[v + 1] = Ground(Mathf.Cos(a) * outer, Mathf.Sin(a) * outer);
            if (step == subdivisions) continue;
            int j = (dash * subdivisions + step) * 6;
            triangles[j] = v; triangles[j + 1] = v + 2; triangles[j + 2] = v + 1;
            triangles[j + 3] = v + 1; triangles[j + 4] = v + 2; triangles[j + 5] = v + 3;
        }
        var mesh = new Mesh { name = label, vertices = vertices, triangles = triangles }; mesh.RecalculateNormals(); mesh.RecalculateBounds(); meshes.Add(mesh);
        var go = new GameObject(label, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(transform, false);
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material; renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
    }
    void Diamond(string label, float radius, float width, Material material)
    {
        var go = new GameObject(label); go.transform.SetParent(transform, false);
        var line = go.AddComponent<LineRenderer>(); line.useWorldSpace = false; line.positionCount = 4; line.loop = true;
        for (int i = 0; i < 4; i++)
        {
            float angle = i * Mathf.PI * .5f;
            line.SetPosition(i, Ground(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius));
        }
        line.startWidth = line.endWidth = width;
        line.sharedMaterial = material; line.numCornerVertices = 2; line.shadowCastingMode = ShadowCastingMode.Off;
    }
    void OnDestroy()
    {
        foreach (var mesh in meshes) if (mesh) Destroy(mesh);
        foreach (var material in new[] { outline, shadow }) if (material) Destroy(material);
    }
}
