using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Editor-only placement. Read the real terrain and model vertices without
// changing imported meshes, shared materials or the other chapter scenes.
public static class Chapter4Grounding
{
    [Serializable] public sealed class Placement
    {
        public string name, kind;
        public Vector3 before, after;
        public float baseOffsetBefore, maxGapBefore, maxGapAfter, tiltDegrees, terrainSlope;
        public int baseSamples;
    }
    [Serializable] public sealed class Report
    {
        public int trees, rocks;
        public List<Placement> objects = new List<Placement>();
    }
    static readonly Dictionary<Mesh, Vector3[]> vertices = new Dictionary<Mesh, Vector3[]>();

    [MenuItem("Tools/Chapter 4/Seat Trees and Tilt Rocks")]
    public static void RepairOpenScene()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != Chapter4SceneAuthoring.ScenePath)
            throw new InvalidOperationException("Open the saved Chapter 4 scene outside Play Mode first.");
        var report = Apply(scene);
        Debug.Log("[Chapter4] Seated " + report.trees + " trees and " + report.rocks + " rocks. Review and save the scene.");
    }

    public static Report Apply(Scene scene)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.name != "第四章")
            throw new InvalidOperationException("Grounding only applies to Chapter 4 outside Play Mode.");
        var transforms = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
        var ground = transforms.First(t => t.name == "Forest valley ground").GetComponent<MeshCollider>();
        if (!ground || !ground.sharedMesh) throw new InvalidOperationException("Chapter 4 terrain collider is missing.");
        Physics.SyncTransforms();
        var report = new Report();
        try
        {
            foreach (var tree in transforms.Where(t => t.name.StartsWith("山林樹木 ")))
            {
                report.objects.Add(SeatTree(tree, ground)); report.trees++;
            }
            foreach (var rock in transforms.Where(t => t.name == "風化岩石"))
            {
                report.objects.Add(SeatRock(rock, ground)); report.rocks++;
            }
        }
        finally { vertices.Clear(); }
        Physics.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(scene);
        return report;
    }

    static Placement SeatTree(Transform wrapper, MeshCollider ground)
    {
        if (wrapper.childCount != 1) throw new InvalidOperationException("Unexpected tree hierarchy: " + wrapper.name);
        var model = wrapper.GetChild(0);
        var points = BasePoints(model, Vector3.up, .006f, .055f);
        Vector3 center = Center(points);
        var result = new Placement { name = wrapper.name, kind = "tree", before = model.position,
            baseSamples = points.Count, maxGapBefore = MaxGap(points, ground),
            baseOffsetBefore = Vector2.Distance(new Vector2(center.x, center.z), new Vector2(wrapper.position.x, wrapper.position.z)) };
        Undo.RecordObject(model, "Seat Chapter 4 tree root");
        // Preserve the authored trunk site; the asymmetrical crown is not a ground anchor.
        model.position += new Vector3(wrapper.position.x - center.x, 0, wrapper.position.z - center.z);
        points = BasePoints(model, Vector3.up, .006f, .055f);
        model.position += Vector3.up * SeatShift(points, ground, .065f);
        points = BasePoints(model, Vector3.up, .006f, .055f);
        center = Center(points);
        var trunk = wrapper.GetComponent<CapsuleCollider>();
        if (trunk)
        {
            Undo.RecordObject(trunk, "Align Chapter 4 trunk collision");
            // The old capsule was left at the crown's centre while its trunk was offset.
            float radius = points.Max(p => Vector2.Distance(new Vector2(p.x, p.z), new Vector2(center.x, center.z)));
            trunk.radius = Mathf.Clamp(radius * .86f, .18f, .9f);
            Vector3 foot = new Vector3(center.x, points.Min(p => p.y), center.z);
            trunk.center = wrapper.InverseTransformPoint(foot + Vector3.up * (trunk.height * .5f));
        }
        result.after = model.position; result.maxGapAfter = MaxGap(points, ground);
        result.tiltDegrees = Vector3.Angle(model.up, Vector3.up);
        return result;
    }

    static Placement SeatRock(Transform model, MeshCollider ground)
    {
        var points = BasePoints(model, model.up, .12f, .12f);
        var bounds = VisibleBounds(model);
        Vector3 anchor = new Vector3(bounds.center.x, 0, bounds.center.z);
        float width = points.Max(p => Vector3.Dot(p, model.right)) - points.Min(p => Vector3.Dot(p, model.right));
        float depth = points.Max(p => Vector3.Dot(p, model.forward)) - points.Min(p => Vector3.Dot(p, model.forward));
        float sampleRadius = Mathf.Clamp(Mathf.Min(width, depth) * .3f, .45f, 1.6f);
        Vector3 normal = SlopeNormal(ground, anchor, sampleRadius);
        var result = new Placement { name = HierarchyPath(model), kind = "rock", before = model.position,
            maxGapBefore = MaxGap(points, ground), terrainSlope = Vector3.Angle(Vector3.up, normal) };
        Undo.RecordObject(model, "Tilt and seat Chapter 4 rock");
        // Remove only the previous ground tilt, retaining the original yaw on repeated repairs.
        Quaternion upright = Quaternion.FromToRotation(model.up, Vector3.up) * model.rotation;
        Vector3 up = Vector3.RotateTowards(Vector3.up, normal, 38 * Mathf.Deg2Rad, 0);
        model.rotation = Quaternion.FromToRotation(Vector3.up, up) * upright;
        bounds = VisibleBounds(model);
        model.position += new Vector3(anchor.x - bounds.center.x, 0, anchor.z - bounds.center.z);
        points = BasePoints(model, model.up, .12f, .12f);
        // Seat a band of real lower vertices, not just one bounding-box corner.
        // The downhill rim is buried as well, so it cannot expose a floating underside.
        model.position += Vector3.up * SeatShift(points, ground, .10f);
        points = BasePoints(model, model.up, .12f, .12f);
        result.after = model.position; result.maxGapAfter = MaxGap(points, ground);
        result.tiltDegrees = Vector3.Angle(Vector3.up, model.up); result.baseSamples = points.Count;
        return result;
    }

    public static List<Vector3> BasePoints(Transform model, Vector3 axis, float fraction, float minimumBand)
    {
        var all = new List<Vector3>();
        foreach (var filter in model.GetComponentsInChildren<MeshFilter>(true))
        {
            if (!filter.sharedMesh) continue;
            if (!vertices.TryGetValue(filter.sharedMesh, out var data))
            {
                using (var read = MeshUtility.AcquireReadOnlyMeshData(filter.sharedMesh))
                using (var buffer = new NativeArray<Vector3>(read[0].vertexCount, Allocator.Temp))
                { read[0].GetVertices(buffer); data = buffer.ToArray(); }
                vertices.Add(filter.sharedMesh, data);
            }
            foreach (var vertex in data) all.Add(filter.transform.TransformPoint(vertex));
        }
        if (all.Count == 0) throw new InvalidOperationException("No readable model vertices: " + model.name);
        float low = all.Min(p => Vector3.Dot(p, axis)), high = all.Max(p => Vector3.Dot(p, axis));
        float ceiling = low + Mathf.Max(minimumBand, (high - low) * fraction);
        return all.Where(p => Vector3.Dot(p, axis) <= ceiling).ToList();
    }

    static Vector3 Center(List<Vector3> points)
    {
        var bounds = new Bounds(points[0], Vector3.zero);
        foreach (var point in points) bounds.Encapsulate(point);
        return bounds.center;
    }
    static Bounds VisibleBounds(Transform model)
    {
        var renderers = model.GetComponentsInChildren<Renderer>(true);
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        return bounds;
    }
    public static float GroundHeight(MeshCollider ground, Vector3 point)
    {
        var ray = new Ray(new Vector3(point.x, ground.bounds.max.y + 100, point.z), Vector3.down);
        if (!ground.Raycast(ray, out RaycastHit hit, ground.bounds.size.y + 200))
            throw new InvalidOperationException("Model base lies outside Chapter 4 terrain: " + point);
        return hit.point.y;
    }
    static Vector3 SlopeNormal(MeshCollider ground, Vector3 center, float radius)
    {
        float dx = (GroundHeight(ground, center + Vector3.right * radius) - GroundHeight(ground, center - Vector3.right * radius)) / (2 * radius);
        float dz = (GroundHeight(ground, center + Vector3.forward * radius) - GroundHeight(ground, center - Vector3.forward * radius)) / (2 * radius);
        return new Vector3(-dx, 1, -dz).normalized;
    }
    static float SeatShift(List<Vector3> points, MeshCollider ground, float embed) => points.Min(p => GroundHeight(ground, p) - p.y) - embed;
    static float MaxGap(List<Vector3> points, MeshCollider ground) => points.Max(p => p.y - GroundHeight(ground, p));
    static string HierarchyPath(Transform transform) => transform.parent ? HierarchyPath(transform.parent) + "/" + transform.name : transform.name;
}
