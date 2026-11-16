using System;
using System.Collections.Generic;
using UnityEngine;

// These two local encounters are gameplay staging, not a claimed historical skirmish.
[DefaultExecutionOrder(10)]
public sealed class Chapter4RearGuard : MonoBehaviour
{
    public enum Phase { OpeningCover, ForcedWithdrawal, Moving, TurnBackCover, FinalRetreat }
    public Chapter4SceneController Chapter { get; private set; }
    public Chapter4Rifle Gun { get; private set; }
    public Phase CurrentPhase { get; private set; }
    public IReadOnlyList<Chapter4Pursuer> Enemies => enemies;
    public float Clock { get; private set; }
    public int SuppressingShots { get; private set; }
    public int Hits { get; private set; }
    public bool Frozen => !Chapter || Chapter.WorldFrozen || Chapter.gas.IsCollapsed || Chapter.evacuation.PartyGathered;
    public bool Holding => CurrentPhase == Phase.OpeningCover || CurrentPhase == Phase.TurnBackCover;
    public bool OpeningCleared => CurrentPhase != Phase.OpeningCover;
    public bool SecondCleared => CurrentPhase == Phase.FinalRetreat;
    public bool LaneSafe
    {
        get { foreach (var enemy in enemies) if (enemy.Group == ActiveGroup && enemy.Threatening) return false; return true; }
    }
    public int ActiveGroup => CurrentPhase == Phase.TurnBackCover || CurrentPhase == Phase.FinalRetreat ? 1 : 0;
    public float CrossingStart => ActiveGroup == 0 ? 12 : 105;
    public float CrossingEnd => ActiveGroup == 0 ? 42 : 124;
    public float PartyAdvance => CurrentPhase == Phase.OpeningCover || CurrentPhase == Phase.ForcedWithdrawal ? 50 : CurrentPhase == Phase.TurnBackCover ? 139 : 12;
    public float PlayerLimit => CurrentPhase == Phase.OpeningCover ? 43 : CurrentPhase == Phase.TurnBackCover ? 126 : Chapter.evacuation.RouteLength;
    public string Objective => CurrentPhase == Phase.OpeningCover ? "射擊壓制左右追兵・掩護傷患通過" :
        CurrentPhase == Phase.ForcedWithdrawal ? "砲火與濃煙逼近！離開掩護位置" :
        CurrentPhase == Phase.TurnBackCover ? "回身射擊・接應後方同伴通過" : "";
    public string Status => Holding ? (LaneSafe ? "追兵暫時受壓制・同伴正在穿越" : "同伴被火力壓住・朝追兵射擊") +
        "　" + ClearedCount + "／" + Chapter.evacuation.RequiredPartyCount + " 通過" :
        CurrentPhase == Phase.ForcedWithdrawal ? "傷患已過去・現在輪到你撤離" : "";
    public int ClearedCount
    {
        get { int count = 0; foreach (var a in Chapter.evacuation.Party) if (Chapter.evacuation.RequiredForArrival(a) && a.Progress >= CrossingEnd - 10) count++; return count; }
    }
    public string Hint
    {
        get
        {
            if (NearSupply) return (Chapter.player.IsVR ? "右 A" : "E") + " 補充備彈・" + (Chapter.player.IsVR ? "右 B" : "R") + " 單發裝填";
            if (Holding) return LaneSafe ? "抓住空檔！同伴會自己通過・留意追兵再度舉槍" :
                "從岩石邊緣探出射擊・命中或擦近追兵都能壓制，換彈時低身掩蔽";
            if (CurrentPhase == Phase.ForcedWithdrawal) return "飛機仍在上空・岩後也無法久留，往傷患撤走的方向跟上";
            return "";
        }
    }
    public Vector3 NavigationTarget => Holding ? Chapter.evacuation.PointOnRoute(ActiveGroup == 0 ? 5 : 111) : Chapter.evacuation.PointOnRoute(Chapter.evacuation.PlayerProgress + 8);
    public bool NearSupply
    {
        get { foreach (var box in supplies) if (Chapter4Evacuation.FlatDistance(Chapter.player.transform.position, box.position) <= 3) return true; return false; }
    }
    readonly List<Chapter4Pursuer> enemies = new List<Chapter4Pursuer>();
    readonly List<Transform> supplies = new List<Transform>();
    Transform world;
    LineRenderer tracer;
    Material tracerMaterial;
    float tracerClock;
    bool withdrawalImpact;

    public void Initialize(Chapter4SceneController chapter)
    {
        Chapter = chapter;
        world = new GameObject("兩次掩護與實體追兵").transform; world.SetParent(transform, false);
        var weapon = Resources.Load<Chapter4Rifle>("Chapter4Combat/PlayerRifle");
        var pursuer = Resources.Load<GameObject>("Chapter4Combat/Pursuer");
        var supply = Resources.Load<GameObject>("Chapter4Combat/Supply");
        if (!weapon || !pursuer || !supply) throw new InvalidOperationException("Chapter4 combat prefabs are missing.");
        Gun = Instantiate(weapon); Gun.Initialize(this);
        // Every person exists from scene entry, including the later rear pursuit.
        // Reinforcements walk toward their firing position; no wave teleports.
        for (int i = 0; i < 6; i++)
        {
            int group = i < 4 ? 0 : 1, lane = i % 2 == 0 ? -1 : 1;
            float start = i < 2 ? 23 : i < 4 ? 35 : 94;
            float finish = i < 2 ? 17 : i < 4 ? 27 : 100;
            var root = new GameObject((group == 0 ? "前方林側追兵 " : "後方追兵 ") + (i + 1)); root.transform.SetParent(world, false);
            var visual = Instantiate(pursuer, root.transform); visual.name = "日方軍警";
            visual.transform.localPosition = Vector3.zero; visual.transform.localRotation = Quaternion.identity; visual.SetActive(true);
            var enemy = root.AddComponent<Chapter4Pursuer>(); enemy.actor = visual.GetComponent<Chapter2Actor>(); enemy.rifle = visual.GetComponent<Chapter2Rifle>();
            enemy.Initialize(this, i, group, Ground(At(start, lane * 4.1f)), Ground(At(finish, lane * 3.8f)));
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
            float delay = enemy.Index == 0 ? 3 : enemy.Index == 1 ? 8 : enemy.Index == 2 ? 20 : enemy.Index == 3 ? 27 : enemy.Index == 4 ? 1 : 5;
            bool engaged = enemy.Group == 0 ? (CurrentPhase == Phase.OpeningCover ? Clock >= delay : CurrentPhase == Phase.ForcedWithdrawal) :
                (CurrentPhase == Phase.TurnBackCover || CurrentPhase == Phase.FinalRetreat) && Clock >= delay;
            enemy.Tick(dt, engaged);
        }
        if (!Frozen)
        {
            var flow = Chapter.evacuation;
            if (CurrentPhase == Phase.OpeningCover && Clock >= 32 && flow.RearProgress >= 32 && !Chapter.gas.BlocksPartyAdvance)
            {
                CurrentPhase = Phase.ForcedWithdrawal; Clock = 0;
                Chapter.gas.CaptureCheckpoint(flow.PlayerProgress);
                Gun.Notice("同伴已通過！砲擊逼近，立刻撤離", 6);
            }
            if (CurrentPhase == Phase.ForcedWithdrawal)
            {
                if (!withdrawalImpact && Chapter.Retreat.ScheduleImpact(Ground(At(Mathf.Max(1, flow.PlayerProgress - 3), -1)), false)) withdrawalImpact = true;
                if (flow.PlayerProgress >= 54 && flow.RearProgress >= 42) { CurrentPhase = Phase.Moving; Clock = 0; }
            }
            if (CurrentPhase == Phase.Moving && flow.PlayerProgress >= 100 && flow.RearProgress >= 78)
            {
                CurrentPhase = Phase.TurnBackCover; Clock = 0;
                Chapter.gas.CaptureCheckpoint(flow.PlayerProgress);
                Gun.Notice("後方追兵接近！回身掩護，讓同伴過彎", 6);
            }
            if (CurrentPhase == Phase.TurnBackCover && Clock >= 10 && flow.RearProgress >= 121 && !Chapter.gas.BlocksPartyAdvance)
            {
                CurrentPhase = Phase.FinalRetreat; Clock = 0;
                Chapter.gas.CaptureCheckpoint(flow.PlayerProgress);
                Gun.Notice("隊伍已通過・沿山路撤離，別再逗留", 5);
            }
            if (Chapter.player.InteractPressed && NearSupply) Gun.Refill();
        }
        tracer.enabled = tracerClock > 0 && !Frozen;
        Chapter.presentation.SetCombatStatus(Gun, !Frozen, Holding ? Status : "");
        if (Holding && !Frozen) Chapter.presentation.navigation.text = ActiveGroup == 0 ? "注意左右林側追兵" : "回身掩護後方追兵";
    }
    public bool ShouldPin(Chapter4Companion companion) => Holding && !LaneSafe && companion.JoinedRoute &&
        companion.Progress >= CrossingStart && companion.Progress < CrossingEnd;
    public bool LaneVisible(Chapter4Pursuer enemy)
    {
        float distance = enemy.Group == 0 ? 20 : 113;
        return ClearTerrain(enemy.AimPoint, Chapter.evacuation.PointOnRoute(distance) + Vector3.up * 1.1f);
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
        if (victim) return "命中追兵・留意另一側，按 R 裝填";
        return suppressed ? "追兵縮回掩蔽！同伴正找空檔通過" : length < 80 ? "子彈落在掩體・調整角度，按 R 裝填" : "未壓制追兵・朝林側舉槍處瞄準";
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
        public Phase phase; public float clock; public int suppressingShots, hits; public bool impact;
        public Chapter4Rifle.Snapshot gun; public Chapter4Pursuer.Snapshot[] enemies;
    }
    public Snapshot Capture()
    {
        var s = new Snapshot { phase = CurrentPhase, clock = Clock, suppressingShots = SuppressingShots, hits = Hits, impact = withdrawalImpact, gun = Gun.Capture(), enemies = new Chapter4Pursuer.Snapshot[enemies.Count] };
        for (int i = 0; i < enemies.Count; i++) s.enemies[i] = enemies[i].Capture(); return s;
    }
    public void Restore(Snapshot s)
    {
        if (s == null) return;
        CurrentPhase = s.phase; Clock = s.clock; SuppressingShots = s.suppressingShots; Hits = s.hits;
        withdrawalImpact = CurrentPhase == Phase.ForcedWithdrawal ? false : s.impact;
        Gun.Restore(s.gun); for (int i = 0; i < enemies.Count; i++) enemies[i].Restore(s.enemies[i]); tracerClock = 0;
    }
    void OnDestroy() { if (Gun) Destroy(Gun.gameObject); if (tracerMaterial) Destroy(tracerMaterial); }
}
