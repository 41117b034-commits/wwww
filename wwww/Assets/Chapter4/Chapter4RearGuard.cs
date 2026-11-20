using System;
using System.Collections.Generic;
using UnityEngine;

// Persistent rear pursuit is gameplay staging, not a claimed historical skirmish.
[DefaultExecutionOrder(10)]
public sealed class Chapter4RearGuard : MonoBehaviour
{
    public enum Phase { LeavingCamp, Pursued, FinalRetreat }
    public Chapter4SceneController Chapter { get; private set; }
    public Chapter4Rifle Gun { get; private set; }
    public Phase CurrentPhase { get; private set; }
    public IReadOnlyList<Chapter4Pursuer> Enemies => enemies;
    public float Clock { get; private set; }
    public int SuppressingShots { get; private set; }
    public int Hits { get; private set; }
    public bool Frozen => !Chapter || Chapter.WorldFrozen || Chapter.gas.IsCollapsed || Chapter.evacuation.PartyGathered;
    public float NearestDistance
    {
        get { float distance = 999; foreach (var enemy in enemies) if (enemy.Engaged && !enemy.Down) distance = Mathf.Min(distance, Chapter4Evacuation.FlatDistance(enemy.transform.position, Chapter.player.transform.position)); return distance; }
    }
    public bool Assisting => Chapter.gas.BlocksPartyAdvance || Chapter.contact.CurrentState == Chapter4Contact.State.Rising;
    public float PartyAdvance => Mathf.Max(16, Chapter.evacuation.PlayerProgress + 9);
    public float PlayerLimit => Chapter.evacuation.RouteLength;
    public string Objective => CurrentPhase == Phase.LeavingCamp ? "毒氣侵入營地・跟著隊伍撤離" : "";
    public string Status => NearestDistance < 24 ? "後方追兵逼近・約 " + Mathf.CeilToInt(NearestDistance) + " 公尺・利用山彎拉開距離" : "";
    public string Hint
    {
        get
        {
            if (NearSupply) return (Chapter.player.IsVR ? "右 A" : "E") + " 補充備彈・" + (Chapter.player.IsVR ? "右 B" : "R") + " 單發裝填";
            if (NearestDistance < 18) return "追兵就在後方！沿岩壁轉彎甩開；需要時可回身射擊爭取時間";
            return "";
        }
    }
    public bool NearSupply
    {
        get { foreach (var box in supplies) if (Chapter4Evacuation.FlatDistance(Chapter.player.transform.position, box.position) <= 3) return true; return false; }
    }
    readonly List<Chapter4Pursuer> enemies = new List<Chapter4Pursuer>();
    readonly List<Transform> supplies = new List<Transform>();
    readonly List<Chapter4CompanionRifle> escorts = new List<Chapter4CompanionRifle>();
    int nextEscort;
    float nextReport = 14;
    Transform world;
    LineRenderer tracer;
    Material tracerMaterial;
    float tracerClock;

    public void Initialize(Chapter4SceneController chapter)
    {
        Chapter = chapter;
        world = new GameObject("沿後方山路追趕的軍警").transform; world.SetParent(transform, false);
        var weapon = Resources.Load<Chapter4Rifle>("Chapter4Combat/PlayerRifle");
        var pursuer = Resources.Load<GameObject>("Chapter4Combat/Pursuer");
        var supply = Resources.Load<GameObject>("Chapter4Combat/Supply");
        if (!weapon || !pursuer || !supply) throw new InvalidOperationException("Chapter4 combat prefabs are missing.");
        Gun = Instantiate(weapon); Gun.Initialize(this);
        foreach (string id in new[] { "bawan", "tado" })
        {
            var companion = Array.Find(chapter.evacuation.cast, member => member.actorId == id);
            if (!companion || !companion.isActiveAndEnabled || companion.wounded) continue;
            var escort = companion.gameObject.AddComponent<Chapter4CompanionRifle>();
            escort.Initialize(companion, this, pursuer.GetComponent<Chapter2Rifle>());
            companion.EscortRifle = escort;
            escorts.Add(escort);
        }
        // All opponents start behind the camp and traverse the same physical world.
        for (int i = 0; i < 6; i++)
        {
            float start = -8 - i * 2.4f;
            var root = new GameObject("後方追兵 " + (i + 1)); root.transform.SetParent(world, false);
            var visual = Instantiate(pursuer, root.transform); visual.name = "日方軍警";
            visual.transform.localPosition = Vector3.zero; visual.transform.localRotation = Quaternion.identity; visual.SetActive(true);
            var enemy = root.AddComponent<Chapter4Pursuer>(); enemy.actor = visual.GetComponent<Chapter2Actor>(); enemy.rifle = visual.GetComponent<Chapter2Rifle>();
            enemy.Initialize(this, i, 1, PursuitPoint(start, 4.6f + i % 3 * .65f), start);
            foreach (var other in enemies) Physics.IgnoreCollision(enemy.GetComponent<CharacterController>(), other.GetComponent<CharacterController>());
            enemies.Add(enemy);
        }
        foreach (float distance in new[] { 3f, 110f })
        {
            var box = Instantiate(supply, world).transform; box.name = "備彈木箱・E 補充";
            box.position = Ground(At(distance, 1.8f)); box.rotation = Quaternion.identity; box.gameObject.SetActive(true); supplies.Add(box);
        }
        tracerMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit")); tracerMaterial.color = new Color(.67f, .65f, .53f);
        tracer = new GameObject("短促彈道").AddComponent<LineRenderer>(); tracer.transform.SetParent(world, false);
        tracer.sharedMaterial = tracerMaterial; tracer.positionCount = 2; tracer.startWidth = .012f; tracer.endWidth = .005f; tracer.enabled = false;
        chapter.presentation.EnsureCombatUI();
    }
    Vector3 At(float d, float side)
    {
        var flow = Chapter.evacuation; var forward = flow.BasePoint(d + .5f) - flow.BasePoint(d - .5f);
        return flow.BasePoint(d) + Vector3.Cross(Vector3.up, forward).normalized * side;
    }
    public Vector3 PursuitPoint(float d, float side)
    {
        var flow = Chapter.evacuation;
        Vector3 forward = d < 0 ? flow.BasePoint(1) - flow.BasePoint(0) : flow.BasePoint(d + .5f) - flow.BasePoint(d - .5f);
        forward = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
        Vector3 point = d < 0 ? flow.BasePoint(0) + forward * d : flow.BasePoint(d);
        return Ground(point + Vector3.Cross(Vector3.up, forward) * side);
    }
    Vector3 Ground(Vector3 point)
    {
        if (Chapter.evacuation.terrain.Raycast(new Ray(point + Vector3.up * 40, Vector3.down), out var hit, 80)) point.y = hit.point.y + .02f;
        return point;
    }
    void Update()
    {
        if (!Chapter) return;
        float dt = Frozen ? 0 : Time.deltaTime;
        tracerClock = Mathf.Max(0, tracerClock - dt);
        Gun.Tick(dt);
        if (!Frozen) Clock += dt;
        foreach (var enemy in enemies)
        {
            enemy.Tick(dt, Clock >= 6 + enemy.Index * 2);
        }
        if (!Frozen)
        {
            var flow = Chapter.evacuation;
            if (!Assisting && Clock >= nextReport && escorts.Count > 0)
            {
                bool started = escorts[nextEscort].TryTurn();
                nextEscort = (nextEscort + 1) % escorts.Count;
                nextReport = Clock + (started ? 19 : 1.5f);
            }
            if (CurrentPhase == Phase.LeavingCamp && flow.PlayerProgress >= 28)
            {
                CurrentPhase = Phase.Pursued;
                Gun.Notice("後方有追兵・持續撤離，步槍可用來爭取距離", 5);
            }
            if (CurrentPhase == Phase.Pursued && flow.PlayerProgress >= 125)
            {
                CurrentPhase = Phase.FinalRetreat;
                Gun.Notice("隊伍已通過・沿山路撤離，別再逗留", 5);
            }
            if (Chapter.player.InteractPressed && NearSupply) Gun.Refill();
        }
        tracer.enabled = tracerClock > 0 && !Frozen;
        Chapter.presentation.SetCombatStatus(Gun, !Frozen, Status);
    }
    public bool ClearTerrain(Vector3 from, Vector3 to)
    {
        Vector3 delta = to - from;
        foreach (var hit in Physics.RaycastAll(from, delta.normalized, delta.magnitude, ~0, QueryTriggerInteraction.Ignore))
            if (!hit.collider.GetComponentInParent<Chapter4Pursuer>() && !hit.collider.GetComponentInParent<Chapter4Companion>() &&
                !hit.collider.GetComponentInParent<Chapter4Player>()) return false;
        return true;
    }
    public string ResolveShot(Ray ray)
    {
        Physics.SyncTransforms();
        RaycastHit[] hits = Physics.RaycastAll(ray, 80, ~0, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        float length = 80; Chapter4Pursuer victim = null; bool friend = false;
        foreach (var hit in hits)
        {
            if (hit.collider.GetComponentInParent<Chapter4Player>()) continue;
            length = hit.distance; victim = hit.collider.GetComponentInParent<Chapter4Pursuer>();
            friend = hit.collider.GetComponentInParent<Chapter4Companion>(); break;
        }
        DrawTracer(Gun.muzzle.position, ray.GetPoint(length));
        if (friend) return "同伴擋住射線・移開準星再射擊";
        if (victim && !victim.Down) { victim.Hit(); Hits++; }
        bool suppressed = false;
        foreach (var enemy in enemies)
        {
            if (enemy.Down || !enemy.Engaged) continue;
            float along = Vector3.Dot(enemy.AimPoint - ray.origin, ray.direction);
            // A near miss must actually reach the person. No suppression through rock or trees.
            if (along > 0 && along <= length + .45f && Vector3.Distance(enemy.AimPoint, ray.GetPoint(along)) <= 1.6f && ClearTerrain(ray.origin, enemy.AimPoint))
            { enemy.Suppress(8); suppressed = true; }
        }
        if (suppressed) SuppressingShots++;
        if (victim) return "命中追兵・趁現在繼續撤離";
        return suppressed ? "追兵暫停追趕！趁空檔拉開距離" : length < 80 ? "子彈落在掩體・繼續找路撤離" : "繼續撤離・需要時按 R 裝填";
    }
    public void EnemyReport(Chapter4Pursuer enemy, Vector3 target)
    {
        DrawTracer(enemy.rifle.muzzle ? enemy.rifle.muzzle.position : enemy.AimPoint, target);
    }
    void DrawTracer(Vector3 start, Vector3 end)
    {
        tracer.SetPosition(0, start); tracer.SetPosition(1, end); tracerClock = .07f;
    }
    [Serializable] public sealed class Snapshot
    {
        public Phase phase; public float clock, nextReport; public int suppressingShots, hits, nextEscort;
        public Chapter4Rifle.Snapshot gun; public Chapter4Pursuer.Snapshot[] enemies;
        public Chapter4CompanionRifle.Snapshot[] escorts;
    }
    public Snapshot Capture()
    {
        var s = new Snapshot { phase = CurrentPhase, clock = Clock, suppressingShots = SuppressingShots, hits = Hits, gun = Gun.Capture(), enemies = new Chapter4Pursuer.Snapshot[enemies.Count] };
        for (int i = 0; i < enemies.Count; i++) s.enemies[i] = enemies[i].Capture();
        s.nextEscort = nextEscort; s.nextReport = nextReport;
        s.escorts = escorts.ConvertAll(escort => escort.Capture()).ToArray();
        return s;
    }
    public void Restore(Snapshot s)
    {
        if (s == null) return;
        CurrentPhase = s.phase; Clock = s.clock; SuppressingShots = s.suppressingShots; Hits = s.hits;
        Gun.Restore(s.gun); for (int i = 0; i < enemies.Count; i++) enemies[i].Restore(s.enemies[i]); tracerClock = 0;
        nextEscort = s.nextEscort; nextReport = s.nextReport;
        for (int i = 0; i < escorts.Count; i++) escorts[i].Restore(s.escorts != null && i < s.escorts.Length ? s.escorts[i] : null);
    }
    void OnDestroy() { if (Gun) Destroy(Gun.gameObject); if (tracerMaterial) Destroy(tracerMaterial); }
}
