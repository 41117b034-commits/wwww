using System;
using System.Collections.Generic;
using UnityEngine;

// Authored gameplay beats, not a reconstruction of a specific historical attack.
// Distances remain in the original route coordinate system so story/save data stay compatible.
public sealed class Chapter4RetreatAction
{
    public enum CrossingState { Approach, UnderFire, Gap, Crossing, Clear }
    public enum RouteChoice { Undecided, Sheltered, Exposed }
    public const float CrossingEntry = 38, CrossingExit = 51, ForkStart = 66, ForkEnd = 91;
    public CrossingState Crossing { get; private set; }
    public RouteChoice Choice { get; private set; }
    public float Clock { get; private set; }
    public bool Rockfall { get; private set; }
    public int CrossingsSignalled { get; private set; }
    Chapter4RearGuard Combat => flow.chapter.RearGuard;
    public bool ControlsShells => Rockfall && flow.PlayerProgress < ForkEnd + 3;
    public bool CanSignal => false;
    public float PartyLimit => Choice == RouteChoice.Undecided ? ForkStart - 1 : flow.RouteLength;
    public float MinimumPartyAdvance => Combat ? Combat.PartyAdvance : 12;
    public float PlayerLimit => Combat ? Combat.PlayerLimit : flow.RouteLength;
    public string Objective => Combat && !string.IsNullOrEmpty(Combat.Objective) ? Combat.Objective :
        Rockfall && Choice == RouteChoice.Undecided ? "山道中央有巨石・從兩側繞行" :
        Choice != RouteChoice.Undecided && flow.PlayerProgress < ForkEnd ?
            (Choice == RouteChoice.Sheltered ? "沿左側岩後小路撤離" : "穿過右側暴露小路・留意落彈") :
        flow.PlayerProgress >= flow.ApproachLength - 30 ? "帶著同伴走進馬赫坡岩窟" : "接應同伴，往上坡撤離";
    public string Hint => Combat && !string.IsNullOrEmpty(Combat.Hint) ? Combat.Hint :
        Rockfall && Choice == RouteChoice.Undecided ? "左側貼岩：多繞幾步、有掩護　／　右側短路：較暴露・直接走入你選的路" :
        flow.chapter.contact && flow.chapter.contact.Responded && !flow.chapter.contact.Reunited ? "歐冰已回應・留在附近接應，她正沿剛才的路追上" :
        "留意前方落彈與身後腳步・步槍仍可使用";
    readonly Chapter4Evacuation flow;
    readonly Vector3[][] branches = { new Vector3[26], new Vector3[26] };
    readonly Vector3[] mainTrail = new Vector3[26];
    // Forest trail 0 is 2.45 m wide on each side, with an uneven painted edge.
    const float MainTrailHalfWidthWithShoulder = 2.75f;
    readonly List<Mesh> meshes = new List<Mesh>();
    readonly List<Material> materials = new List<Material>();
    Transform geometry;
    float shellClock;


    public Chapter4RetreatAction(Chapter4Evacuation owner, MeshFilter rockModel)
    {
        flow = owner;
        for (int i = 0; i < mainTrail.Length; i++)
        {
            float d = ForkStart + i;
            mainTrail[i] = flow.BasePoint(d);
            float bend = Mathf.Min(1, Mathf.Sin(i * Mathf.PI / (mainTrail.Length - 1)) * 1.6f);
            Vector3 offset = Right(d) * bend;
            branches[0][i] = Ground(mainTrail[i] - offset * 5.1f);
            branches[1][i] = Ground(mainTrail[i] + offset * 4.25f);
        }
        BuildLandmarks(rockModel);
    }

    public void Tick(float dt)
    {
        float player = flow.PlayerProgress;
        if (Combat)
        {
            Clock = Combat.Clock;
            Crossing = CrossingState.Clear;
        }
        if (player >= 58) Rockfall = true;
        if (Rockfall && Choice == RouteChoice.Undecided && player >= 71.5f)
        {
            Vector3 feet = flow.chapter.player.transform.position;
            float side = Vector3.Dot(feet - flow.BasePoint(player), Right(player));
            if (Mathf.Abs(side) >= .8f && ForkEdgeDistance(feet, player) <= 0)
                Choice = side < 0 ? RouteChoice.Sheltered : RouteChoice.Exposed;
        }
        if (Rockfall && player >= ForkStart - 3 && player < ForkEnd + 3)
        {
            shellClock -= dt;
            if (shellClock <= 0 && flow.chapter.Retreat.ScheduleImpact(BranchPoint(78, RouteChoice.Exposed), false)) shellClock = 15;
        }
    }

    public bool Signal() => false;
    public bool AtOpenFork => Rockfall && Choice == RouteChoice.Undecided;
    public Vector3 NavigationTarget => flow.PointOnRoute(Mathf.Min(flow.AdvanceLimit, flow.PlayerProgress + 6));
    public Vector3 MapPoint(float distance, Vector3 original) => Choice == RouteChoice.Undecided || distance <= ForkStart || distance >= ForkEnd
        ? original : BranchPoint(distance, Choice);
    public Vector3 BranchPoint(float d, RouteChoice choice)
    {
        float p = Mathf.Clamp(d - ForkStart, 0, 25); int i = Mathf.Min(24, Mathf.FloorToInt(p));
        var route = branches[choice == RouteChoice.Sheltered ? 0 : 1]; return Vector3.Lerp(route[i], route[i + 1], p - i);
    }
    public static float CorridorWidth(float progress)
    {
        float interior = Mathf.Min(Mathf.InverseLerp(ForkStart, 74, progress), Mathf.InverseLerp(ForkEnd, 83, progress));
        return Mathf.Lerp(4.6f, 2.55f, Mathf.SmoothStep(0, 1, interior));
    }
    public float BranchDistance(Vector3 position) =>
        Mathf.Min(DistanceToTrail(position, branches[0]), DistanceToTrail(position, branches[1]));
    public float ForkEdgeDistance(Vector3 position, float progress) =>
        Mathf.Min(BranchDistance(position) - CorridorWidth(progress),
            DistanceToTrail(position, mainTrail) - MainTrailHalfWidthWithShoulder);
    static float DistanceToTrail(Vector3 position, Vector3[] trail)
    {
        float closest = float.MaxValue;
        for (int i = 1; i < trail.Length; i++)
        {
            Vector3 v = trail[i] - trail[i - 1], relative = position - trail[i - 1];
            v.y = relative.y = 0;
            float t = Mathf.Clamp01(Vector3.Dot(relative, v) / Mathf.Max(.001f, v.sqrMagnitude));
            closest = Mathf.Min(closest, (relative - v * t).sqrMagnitude);
        }
        return Mathf.Sqrt(closest);
    }
    Vector3 Right(float d) => Vector3.Cross(Vector3.up, flow.BasePoint(d + .5f) - flow.BasePoint(d - .5f)).normalized;
    Vector3 Ground(Vector3 p)
    {
        if (flow.terrain.Raycast(new Ray(p + Vector3.up * 30, Vector3.down), out var hit, 60)) p.y = hit.point.y + .08f;
        return p;
    }
    void BuildLandmarks(MeshFilter donor)
    {
        geometry = new GameObject("撤離掩護與落石繞路").transform; geometry.SetParent(flow.transform, false);
        Rock(donor, geometry, 3, -4.4f, new Vector3(2, 1.55f, 2.6f));
        Rock(donor, geometry, 111, -2.8f, new Vector3(2.2f, 1.8f, 3.2f));
        Rock(donor, geometry, 11, -2.8f, new Vector3(2.2f, 2, 3.3f));
        Rock(donor, geometry, 36, -2.8f, new Vector3(2.2f, 2.2f, 4));
        Rock(donor, geometry, 54, -3.1f, new Vector3(2.3f, 2.4f, 4));
        Rock(donor, geometry, 78, -7, new Vector3(2.3f, 2.6f, 6));
        var rubble = new GameObject("山道中央既有的岩塊").transform; rubble.SetParent(geometry, false);
        Rock(donor, rubble, 74, 0, new Vector3(2.9f, 1.8f, 4.6f));
        Rock(donor, rubble, 78, 0, new Vector3(3.6f, 2.5f, 5.8f));
        Rock(donor, rubble, 82, 0, new Vector3(2.7f, 1.9f, 4.2f));
        for (int i = 0; i < 2; i++) Trail(branches[i], i == 0 ? "貼岩繞行小路" : "外側短路");
    }
    void Rock(MeshFilter donor, Transform parent, float d, float side, Vector3 size)
    {
        var wrapper = new GameObject("撤離岩石 " + d + " " + side).transform;
        wrapper.SetParent(parent, false);
        var model = new GameObject("風化岩面", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
        model.transform.SetParent(wrapper, false);
        model.GetComponent<MeshFilter>().sharedMesh = donor.sharedMesh;
        model.GetComponent<MeshRenderer>().sharedMaterials = donor.GetComponent<MeshRenderer>().sharedMaterials;
        model.GetComponent<MeshCollider>().sharedMesh = donor.sharedMesh;
        // Use the imported prefab's orientation, independent of scene instance order/slope.
        model.transform.localRotation = Quaternion.Euler(-90, 0, -114.323006f);
        var bounds = model.GetComponent<MeshRenderer>().bounds;
        model.transform.position -= bounds.center;
        wrapper.localScale = new Vector3(size.x / bounds.size.x, size.y / bounds.size.y, size.z / bounds.size.z);
        wrapper.rotation = Quaternion.LookRotation(Vector3.Cross(Right(d), Vector3.up));
        wrapper.position = Ground(flow.BasePoint(d) + Right(d) * side) + Vector3.up * (size.y * .5f - .24f);
        // Seat the visible lower surface against the real slope, not just its centre.
        // Collider rays work with the imported non-readable rock mesh in a player build.
        Physics.SyncTransforms();
        var collider = model.GetComponent<MeshCollider>(); bounds = collider.bounds;
        float gap = float.NegativeInfinity;
        for (int x = 0; x <= 10; x++) for (int z = 0; z <= 10; z++)
        {
            var origin = new Vector3(Mathf.Lerp(bounds.min.x, bounds.max.x, x / 10f), bounds.min.y - 2,
                Mathf.Lerp(bounds.min.z, bounds.max.z, z / 10f));
            if (collider.Raycast(new Ray(origin, Vector3.up), out var bottom, bounds.size.y + 4) &&
                bottom.point.y <= bounds.min.y + Mathf.Max(.12f, bounds.size.y * .12f))
                gap = Mathf.Max(gap, bottom.point.y - (Ground(bottom.point).y - .08f));
        }
        if (!float.IsNegativeInfinity(gap)) wrapper.position -= Vector3.up * Mathf.Max(0, gap + .1f);
    }
    void Trail(Vector3[] points, string name)
    {
        const int rows = 101, columns = 7;
        var vertices = new Vector3[rows * columns]; var uv = new Vector2[vertices.Length];
        var triangles = new List<int>();
        for (int i = 0; i < rows; i++)
        {
            float p = i * .25f; int n = Mathf.Min(24, Mathf.FloorToInt(p));
            Vector3 point = Vector3.Lerp(points[n], points[n + 1], p - n);
            var right = Vector3.Cross(Vector3.up, points[n + 1] - points[n]).normalized;
            for (int j = 0; j < columns; j++)
            {
                int v = i * columns + j;
                vertices[v] = Ground(point + right * Mathf.Lerp(-1.55f, 1.55f, j / 6f));
                uv[v] = new Vector2(j / 6f, p / 3);
                if (i == 0 || j == 0) continue;
                triangles.Add(v - columns - 1); triangles.Add(v - 1); triangles.Add(v - columns);
                triangles.Add(v - columns); triangles.Add(v - 1); triangles.Add(v);
            }
        }
        var mesh = new Mesh { name = name, vertices = vertices, uv = uv, triangles = triangles.ToArray() }; mesh.RecalculateNormals(); meshes.Add(mesh);
        var road = GameObject.Find("Forest trail 0").GetComponent<MeshRenderer>().sharedMaterial;
        var mat = new Material(road); materials.Add(mat);
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(geometry, false);
        go.GetComponent<MeshFilter>().sharedMesh = mesh; go.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }
    [Serializable] public sealed class Snapshot
    {
        public CrossingState crossing; public RouteChoice choice; public float clock, shell; public bool rockfall; public int signals; public Chapter4RearGuard.Snapshot combat;
    }
    public Snapshot Capture() => new Snapshot { crossing = Crossing, choice = Choice, clock = Clock, shell = shellClock, rockfall = Rockfall, signals = CrossingsSignalled, combat = Combat ? Combat.Capture() : null };
    public void Restore(Snapshot s)
    {
        if (s == null) return;
        if (Combat) Combat.Restore(s.combat);
        Crossing = s.crossing; Choice = s.choice; Clock = s.clock; shellClock = s.shell;
        Rockfall = s.rockfall; CrossingsSignalled = s.signals;
    }
    public void Dispose()
    {
        foreach (var mesh in meshes) UnityEngine.Object.Destroy(mesh);
        foreach (var mat in materials) UnityEngine.Object.Destroy(mat);
    }
}
