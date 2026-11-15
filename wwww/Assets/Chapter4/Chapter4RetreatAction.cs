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
    public bool ControlsShells => Crossing != CrossingState.Approach && Crossing != CrossingState.Clear || Rockfall && flow.PlayerProgress < ForkEnd + 3;
    public bool CanSignal => Crossing == CrossingState.Gap && Clock < 24 &&
        flow.chapter.player.IsGrounded && !flow.chapter.gas.IsCollapsed &&
        flow.PlayerProgress >= CrossingEntry - 3.5f && flow.PlayerProgress <= CrossingExit + 2 &&
        flow.RearProgress >= 23 && !flow.chapter.gas.BlocksPartyAdvance;
    public float PartyLimit => Crossing != CrossingState.Clear && Crossing != CrossingState.Crossing
        ? CrossingEntry - 1 : Choice == RouteChoice.Undecided ? ForkStart - 1 : flow.RouteLength;
    public float MinimumPartyAdvance => Crossing == CrossingState.Crossing ? CrossingExit + 12 : 12;
    public float PlayerLimit => Crossing != CrossingState.Clear && Crossing != CrossingState.Crossing
        ? CrossingEntry + 2 : Crossing == CrossingState.Crossing ? CrossingExit + 6 :
        Choice == RouteChoice.Undecided ? ForkStart + 10 : flow.RouteLength;
    public string Objective => Crossing == CrossingState.UnderFire ? "先到岩後避砲，觀察前方空檔" :
        Crossing == CrossingState.Gap ? "砲聲暫歇・示意隊伍穿過空地" :
        Crossing == CrossingState.Crossing ? "穿過空地，到對面岩石接應隊伍" :
        Rockfall && Choice == RouteChoice.Undecided ? "前路被落石截斷・從岩石兩側繞行" :
        Choice != RouteChoice.Undecided && flow.PlayerProgress < ForkEnd ?
            (Choice == RouteChoice.Sheltered ? "沿左側岩後小路撤離" : "穿過右側暴露小路・留意落彈") :
        flow.PlayerProgress < 16 ? "砲擊逼近！先隨隊離開營地" : "帶著同伴往上坡撤離";
    public string Hint
    {
        get
        {
            string key = flow.chapter.player.IsVR ? "右扳機" : "E";
            if (Crossing == CrossingState.UnderFire) return "前方空地正在落彈・留在路旁岩後，等砲聲停下";
            if (Crossing == CrossingState.Gap)
                return CanSignal ? "空檔已到！按 " + key + " 帶隊穿過，前往對面岩石" :
                    flow.RearProgress < 23 || flow.chapter.gas.BlocksPartyAdvance ? "傷患正在靠攏・先在路旁岩後接應" :
                    Clock >= 24 ? "這次空檔已不足全隊通過・等下一輪砲聲停下" : "到前方岩石附近，確認空檔後帶隊通過";
            if (Crossing == CrossingState.Crossing) return "隊伍已跟上・穿過空地後在對面岩石接應，勿停在路中央";
            if (Rockfall && Choice == RouteChoice.Undecided) return "左側貼岩：多繞幾步、有掩護　／　右側短路：較暴露・直接走入你選的路";
            if (flow.chapter.contact && flow.chapter.contact.Responded && !flow.chapter.contact.Reunited) return "歐冰已回應・留在附近接應，她正沿剛才的路追上";
            return "隊伍會持續跟隨・留意前方落彈與身後腳步";
        }
    }

    readonly Chapter4Evacuation flow;
    readonly Vector3[][] branches = { new Vector3[26], new Vector3[26] };
    readonly List<Mesh> meshes = new List<Mesh>();
    readonly List<Material> materials = new List<Material>();
    Transform geometry, rubble;
    Vector3 rubbleRest;
    float fallClock, shellClock;
    bool volley;

    public Chapter4RetreatAction(Chapter4Evacuation owner)
    {
        flow = owner;
        for (int side = 0; side < 2; side++)
            for (int i = 0; i < 26; i++)
            {
                float d = ForkStart + i, t = i / 25f;
                float bend = Mathf.Min(1, Mathf.Sin(t * Mathf.PI) * 1.6f);
                branches[side][i] = Ground(flow.BasePoint(d) + Right(d) * (side == 0 ? -4.8f : 3.1f) * bend);
            }
        BuildLandmarks();
    }

    public void Tick(float dt)
    {
        float player = flow.PlayerProgress;
        if (Crossing == CrossingState.Approach && player >= 29)
        { Crossing = CrossingState.UnderFire; Clock = 0; volley = false; }
        if (Crossing != CrossingState.Approach && Crossing != CrossingState.Clear)
        {
            Clock += dt;
            if (Crossing == CrossingState.UnderFire)
            {
                if (!volley && flow.chapter.Retreat && flow.chapter.Retreat.ScheduleImpact(flow.BasePoint(45), false)) volley = true;
                if (Clock >= 8 && !flow.chapter.Retreat.IsTelegraphing)
                { Crossing = CrossingState.Gap; Clock = 0; }
            }
            else if (Crossing == CrossingState.Gap && Clock >= 32)
            { Crossing = CrossingState.UnderFire; Clock = 0; volley = false; }
            else if (Crossing == CrossingState.Crossing)
            {
                // Once released, the wave remains safe until the slowest required person clears it.
                // This is a committed crossing, not a hidden timer that kills the wounded.
                if (flow.RearProgress >= CrossingExit - 1)
                { Crossing = CrossingState.Clear; Clock = 0; }
            }
        }
        if (!Rockfall && player >= 58)
        {
            Rockfall = true; fallClock = 0; rubble.gameObject.SetActive(true);
            flow.chapter.gas.atmosphere.BlastAt(flow.BasePoint(77), false);
        }
        if (Rockfall)
        {
            fallClock = Mathf.Min(1.3f, fallClock + dt);
            rubble.position = rubbleRest + Vector3.up * (2.5f * (1 - Mathf.SmoothStep(0, 1, fallClock / 1.3f)));
        }
        if (Rockfall && Choice == RouteChoice.Undecided && player >= ForkStart + 2)
        {
            float side = Vector3.Dot(flow.chapter.player.transform.position - flow.BasePoint(player), Right(player));
            if (Mathf.Abs(side) > 1.1f) Choice = side < 0 ? RouteChoice.Sheltered : RouteChoice.Exposed;
        }
        if (Rockfall && player >= ForkStart - 3 && player < ForkEnd + 3)
        {
            shellClock -= dt;
            if (shellClock <= 0 && flow.chapter.Retreat.ScheduleImpact(BranchPoint(78, RouteChoice.Exposed), false)) shellClock = 15;
        }
    }

    public bool Signal()
    {
        if (!CanSignal) return false;
        Crossing = CrossingState.Crossing; Clock = 0; CrossingsSignalled++; return true;
    }

    public Vector3 NavigationTarget => Crossing == CrossingState.UnderFire || Crossing == CrossingState.Gap
        ? Ground(flow.BasePoint(CrossingEntry - 2) - Right(CrossingEntry - 2) * 1.4f) :
        Rockfall && Choice == RouteChoice.Undecided ? flow.BasePoint(ForkStart + 1) :
        flow.PointOnRoute(Mathf.Min(flow.AdvanceLimit, flow.PlayerProgress + 6));

    public Vector3 MapPoint(float distance, Vector3 original) => Choice == RouteChoice.Undecided || distance <= ForkStart || distance >= ForkEnd
        ? original : BranchPoint(distance, Choice);
    public Vector3 BranchPoint(float d, RouteChoice choice)
    {
        float p = Mathf.Clamp(d - ForkStart, 0, 25); int i = Mathf.Min(24, Mathf.FloorToInt(p));
        var route = branches[choice == RouteChoice.Sheltered ? 0 : 1]; return Vector3.Lerp(route[i], route[i + 1], p - i);
    }
    public float BranchDistance(Vector3 position)
    {
        float closest = float.MaxValue;
        foreach (var branch in branches) for (int i = 1; i < branch.Length; i++)
        {
            Vector3 a = branch[i - 1], v = Vector3.ProjectOnPlane(branch[i] - a, Vector3.up);
            float t = Mathf.Clamp01(Vector3.Dot(Vector3.ProjectOnPlane(position - a, Vector3.up), v) / Mathf.Max(.001f, v.sqrMagnitude));
            closest = Mathf.Min(closest, Chapter4Evacuation.FlatDistance(position, a + v * t));
        }
        return closest;
    }
    Vector3 Right(float d) => Vector3.Cross(Vector3.up, flow.BasePoint(d + .5f) - flow.BasePoint(d - .5f)).normalized;
    Vector3 Ground(Vector3 p)
    {
        if (flow.terrain.Raycast(new Ray(p + Vector3.up * 30, Vector3.down), out var hit, 60)) p.y = hit.point.y + .08f;
        return p;
    }
    void BuildLandmarks()
    {
        geometry = new GameObject("撤離掩護與落石繞路").transform; geometry.SetParent(flow.transform, false);
        MeshFilter donor = null;
        foreach (var mesh in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            if (mesh.name == "風化岩石" && mesh.sharedMesh && mesh.GetComponent<MeshRenderer>()) { donor = mesh; break; }
        if (!donor) throw new InvalidOperationException("Chapter 4 weathered-rock donor missing.");
        Rock(donor, geometry, 11, -2.8f, new Vector3(2.2f, 2, 3.3f));
        Rock(donor, geometry, 36, -2.8f, new Vector3(2.2f, 2.2f, 4));
        Rock(donor, geometry, 54, -3.1f, new Vector3(2.3f, 2.4f, 4));
        Rock(donor, geometry, 78, -7, new Vector3(2.3f, 2.6f, 6));
        rubble = new GameObject("砲擊後滑落的岩塊").transform; rubble.SetParent(geometry, false);
        Rock(donor, rubble, 74, 0, new Vector3(2.9f, 1.8f, 4.6f));
        Rock(donor, rubble, 78, 0, new Vector3(3.6f, 2.5f, 5.8f));
        Rock(donor, rubble, 82, 0, new Vector3(2.7f, 1.9f, 4.2f));
        rubbleRest = rubble.position; rubble.gameObject.SetActive(false);
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
                vertices[v] = Ground(point + right * Mathf.Lerp(-.9f, .9f, j / 6f));
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
        public CrossingState crossing; public RouteChoice choice; public float clock, fall, shell; public bool rockfall; public int signals;
    }
    public Snapshot Capture() => new Snapshot { crossing = Crossing, choice = Choice, clock = Clock, fall = fallClock, shell = shellClock, rockfall = Rockfall, signals = CrossingsSignalled };
    public void Restore(Snapshot s)
    {
        if (s == null) return;
        Crossing = s.crossing; Choice = s.choice; Clock = s.clock; fallClock = s.fall; shellClock = s.shell;
        Rockfall = s.rockfall; volley = false; CrossingsSignalled = s.signals; rubble.gameObject.SetActive(Rockfall);
        rubble.position = rubbleRest + Vector3.up * (Rockfall ? 2.5f * (1 - Mathf.SmoothStep(0, 1, fallClock / 1.3f)) : 0);
    }
    public void Dispose()
    {
        foreach (var mesh in meshes) UnityEngine.Object.Destroy(mesh);
        foreach (var mat in materials) UnityEngine.Object.Destroy(mat);
    }
}
