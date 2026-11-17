using System;
using System.Collections.Generic;
using UnityEngine;

// A physical, persistent opponent. Suppression, aiming and falling use the chapter's paused clock.
[DefaultExecutionOrder(4800)]
public sealed class Chapter4Pursuer : MonoBehaviour
{
    public Chapter2Actor actor;
    public Chapter2Rifle rifle;
    public int Group { get; private set; }
    public int Index { get; private set; }
    public bool Down { get; private set; }
    public bool Engaged { get; private set; }
    public bool Suppressed => suppression > 0 && !Down;
    public bool Telegraphing { get; private set; }
    public int Shots { get; private set; }
    public Vector3 AimPoint => transform.position + Vector3.up * (1.18f - crouch * .38f);
    public float SuppressionRemaining => suppression;
    public float RouteProgress { get; private set; }
    public float Travelled { get; private set; }
    public bool Threatening => Engaged && !Down && !Suppressed && owner.ClearTerrain(AimPoint, owner.Chapter.player.transform.position + Vector3.up);
    Chapter4RearGuard owner;
    CharacterController motor;
    AudioSource audioSource;
    Transform aimTarget;
    Vector3 visualPosition, shotTarget;
    Quaternion visualRotation;
    float suppression, attackClock, aimClock, crouch, fallClock;
    bool posed, audioPaused;
    bool fallSettled;
    float settledLift;
    Transform leftFoot, rightFoot, leftThigh, rightThigh, leftCalf, rightCalf;
    Mesh baked;
    readonly List<Vector3> vertices = new List<Vector3>();
    SkinnedMeshRenderer[] skins;
    float lane;
    readonly Collider[] obstacles = new Collider[24];
    readonly List<Vector3> trail = new List<Vector3>();
    float replanClock;

    public void Initialize(Chapter4RearGuard combat, int index, int group, Vector3 start, float progress)
    {
        owner = combat; Index = index; Group = group; RouteProgress = progress; lane = 4.6f + index % 3 * .65f;
        transform.position = start;
        motor = gameObject.AddComponent<CharacterController>();
        motor.height = 1.75f; motor.radius = .3f; motor.center = Vector3.up * .88f; motor.stepOffset = .45f; motor.skinWidth = .03f; motor.slopeLimit = 60;
        foreach (var col in actor.GetComponentsInChildren<Collider>(true)) Destroy(col);
        actor.GroundHeight = start.y; actor.facing = combat.Chapter.player.transform.position - start;
        aimTarget = new GameObject("追兵瞄準位置").transform; aimTarget.SetParent(transform, false);
        rifle.target = null; rifle.aimTarget = aimTarget; rifle.aim = 0; rifle.lowered = .8f;
        audioSource = gameObject.AddComponent<AudioSource>(); audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1; audioSource.minDistance = 5; audioSource.maxDistance = 70; audioSource.rolloffMode = AudioRolloffMode.Linear;
        skins = actor.GetComponentsInChildren<SkinnedMeshRenderer>();
        var animator = actor.GetComponentInChildren<Animator>();
        Transform B(HumanBodyBones bone, string a, string b) => Chapter1WeddingRigBones.Resolve(animator, bone, a, b);
        leftFoot = B(HumanBodyBones.LeftFoot,"L_Foot","LeftFoot"); rightFoot = B(HumanBodyBones.RightFoot,"R_Foot","RightFoot");
        leftThigh = B(HumanBodyBones.LeftUpperLeg,"L_Thigh","LeftUpLeg"); rightThigh = B(HumanBodyBones.RightUpperLeg,"R_Thigh","RightUpLeg");
        leftCalf = B(HumanBodyBones.LeftLowerLeg,"L_Calf","LeftLeg"); rightCalf = B(HumanBodyBones.RightLowerLeg,"R_Calf","RightLeg");
        attackClock = 2 + index * .5f;
        Physics.IgnoreCollision(motor, combat.Chapter.player.Motor);
        foreach (var companion in combat.Chapter.evacuation.Party) Physics.IgnoreCollision(motor, companion.Motor);
    }
    public void Tick(float dt, bool engaged)
    {
        if (posed) { actor.transform.localPosition = visualPosition; actor.transform.localRotation = visualRotation; posed = false; }
        bool frozen = owner.Frozen;
        if (audioPaused != frozen) { audioPaused = frozen; if (frozen) audioSource.Pause(); else audioSource.UnPause(); }
        if (actor.Rig) actor.Rig.frozen = frozen || Down;
        if (frozen) return;
        Engaged = engaged;
        if (Down) { fallClock = Mathf.Min(1.6f, fallClock + dt); rifle.aim = 0; rifle.lowered = 1; return; }
        suppression = Mathf.Max(0, suppression - dt);
        crouch = Mathf.MoveTowards(crouch, Suppressed ? 1 : 0, dt * 4);
        motor.height = 1.75f - crouch * .38f; motor.center = Vector3.up * (motor.height * .5f);
        if (Engaged && !Suppressed && !Telegraphing && !owner.Assisting) Pursue(dt);
        actor.GroundHeight = transform.position.y;
        var player = owner.Chapter.player;
        aimTarget.position = player.transform.position + Vector3.up * (player.IsBracing ? .95f : 1.22f);
        actor.Face(aimTarget.position);
        bool visible = Engaged && !Suppressed && !owner.Assisting && Vector3.Distance(AimPoint, aimTarget.position) < 38 && owner.ClearTerrain(AimPoint, aimTarget.position);
        if (!visible) { Telegraphing = false; aimClock = 0; rifle.aim = 0; rifle.lowered = .65f; return; }
        attackClock -= dt;
        if (attackClock <= 0)
        {
            if (!Telegraphing) shotTarget = aimTarget.position;
            Telegraphing = true; aimClock += dt;
            aimTarget.position = shotTarget;
        }
        rifle.lowered = 0; rifle.aim = Telegraphing ? Mathf.Clamp01(aimClock * 1.5f) : .2f;
        if (Telegraphing && aimClock >= 1.9f)
        {
            Shots++; rifle.Fire(); audioSource.PlayOneShot(owner.Gun.shotClip, .5f);
            owner.EnemyReport(this, shotTarget);
            // The aim point is committed before firing. Moving away or reaching rock cover works without returning fire.
            if (Chapter4Evacuation.FlatDistance(player.transform.position, shotTarget) < .85f && owner.ClearTerrain(AimPoint, player.transform.position + Vector3.up))
                owner.Chapter.gas.ReceiveGunfire(player.IsBracing ? .018f : .055f);
            else owner.Chapter.gas.atmosphere.BulletAt(shotTarget);
            Telegraphing = false; aimClock = 0; attackClock = 6.5f + Index * .7f;
        }
    }
    void Pursue(float dt)
    {
        var flow = owner.Chapter.evacuation;
        float limit = Mathf.Min(flow.PlayerProgress - 5, flow.RearProgress - 4);
        if (RouteProgress >= limit) return;
        replanClock -= dt;
        if (trail.Count == 0 && replanClock <= 0)
        {
            BuildTrail(owner.PursuitPoint(Mathf.Min(limit, RouteProgress + 12), lane));
            replanClock = 1.5f + Index * .15f;
        }
        if (trail.Count == 0) return;
        while (trail.Count > 1 && ClearStep(transform.position, trail[1])) trail.RemoveAt(0);
        Vector3 target = trail[0];
        Vector3 from = transform.position, delta = Vector3.ProjectOnPlane(target - from, Vector3.up);
        float gap = Chapter4Evacuation.FlatDistance(from, owner.Chapter.player.transform.position);
        float speed = gap > 24 ? 2.25f : 1.55f;
        motor.Move(Vector3.ClampMagnitude(delta, speed * dt) + Vector3.down * (3 * dt));
        Travelled += Chapter4Evacuation.FlatDistance(from, transform.position);
        float best = float.PositiveInfinity, progress = RouteProgress;
        for (float d = RouteProgress - 5; d <= Mathf.Min(flow.RouteLength - 2, RouteProgress + 8); d += .25f)
        {
            float distance = Chapter4Evacuation.FlatDistance(transform.position, owner.PursuitPoint(d, 0));
            if (distance < best) { best = distance; progress = d; }
        }
        RouteProgress = progress;
        if (Chapter4Evacuation.FlatDistance(transform.position, target) < .18f) trail.RemoveAt(0);
        if (Chapter4Evacuation.FlatDistance(from, transform.position) < speed * dt * .1f && replanClock <= 0) trail.Clear();
    }
    // A small local terrain graph routes around solid camp rocks and the fork rubble.
    // Only CharacterController.Move changes the person; pathfinding never snaps them.
    void BuildTrail(Vector3 goal)
    {
        trail.Clear();
        const float cell = 1.5f;
        Vector3 origin = transform.position;
        var start = Vector2Int.zero;
        var open = new List<Vector2Int> { start };
        var positions = new Dictionary<Vector2Int, Vector3> { [start] = origin };
        var costs = new Dictionary<Vector2Int, float> { [start] = 0 };
        var parents = new Dictionary<Vector2Int, Vector2Int>();
        var closed = new HashSet<Vector2Int>();
        Vector2Int[] neighbors = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        Vector2Int best = start;
        float bestDistance = Chapter4Evacuation.FlatDistance(origin, goal);
        for (int expanded = 0; open.Count > 0 && expanded < 420; expanded++)
        {
            int index = 0; float score = float.PositiveInfinity;
            for (int i = 0; i < open.Count; i++)
            {
                float value = costs[open[i]] + Chapter4Evacuation.FlatDistance(positions[open[i]], goal);
                if (value < score) { score = value; index = i; }
            }
            var node = open[index]; open.RemoveAt(index); closed.Add(node);
            var point = positions[node]; float distance = Chapter4Evacuation.FlatDistance(point, goal);
            if (distance < bestDistance) { best = node; bestDistance = distance; }
            if (distance < 1.6f) break;
            foreach (var offset in neighbors)
            {
                var next = node + offset;
                if (closed.Contains(next) || Mathf.Abs(next.x) > 18 || Mathf.Abs(next.y) > 18) continue;
                if (!positions.TryGetValue(next, out var target))
                {
                    target = origin + new Vector3(next.x * cell, 0, next.y * cell);
                    if (!owner.Chapter.evacuation.terrain.Raycast(new Ray(target + Vector3.up * 35, Vector3.down), out var hit, 70)) { closed.Add(next); continue; }
                    target.y = hit.point.y + .02f;
                    if (!Fits(target)) { closed.Add(next); continue; }
                    positions[next] = target;
                }
                if (Mathf.Abs(target.y - point.y) > cell * .85f || !ClearStep(point, target)) continue;
                float cost = costs[node] + Vector3.Distance(point, target);
                if (costs.TryGetValue(next, out float old) && old <= cost) continue;
                costs[next] = cost; parents[next] = node;
                if (!open.Contains(next)) open.Add(next);
            }
        }
        while (best != start) { trail.Insert(0, positions[best]); best = parents[best]; }
    }
    bool ClearStep(Vector3 from, Vector3 to)
    {
        Vector3 delta = to - from;
        foreach (var hit in Physics.CapsuleCastAll(from + Vector3.up * .4f, from + Vector3.up * 1.35f, .31f, delta.normalized, delta.magnitude, ~0, QueryTriggerInteraction.Ignore))
            if (Obstacle(hit.collider)) return false;
        return true;
    }
    bool Obstacle(Collider hit) => hit != owner.Chapter.evacuation.terrain && !hit.GetComponentInParent<Chapter4Pursuer>() && !hit.GetComponentInParent<Chapter4Companion>() && !hit.GetComponentInParent<Chapter4Player>();
    bool Fits(Vector3 feet)
    {
        int count = Physics.OverlapCapsuleNonAlloc(feet + Vector3.up * .4f, feet + Vector3.up * 1.35f, .34f, obstacles, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
            if (Obstacle(obstacles[i])) return false;
        return count < obstacles.Length;
    }
    public void Suppress(float seconds)
    {
        if (Down || owner.Frozen) return;
        suppression = Mathf.Max(suppression, seconds); Telegraphing = false; aimClock = 0; attackClock = 1.4f;
    }
    public void Hit()
    {
        if (Down || owner.Frozen) return;
        Down = true; Telegraphing = false; fallClock = 0; motor.enabled = false;
        rifle.aim = 0; rifle.lowered = 1;
    }
    void LateUpdate()
    {
        if (!owner || !actor.Rig) return;
        visualPosition = actor.transform.localPosition; visualRotation = actor.transform.localRotation; posed = true;
        var rig = actor.Rig;
        if (Down)
        {
            float fall = Mathf.SmoothStep(0, 1, fallClock / 1.6f);
            Vector3 pivot = rig.Hips.position;
            actor.transform.RotateAround(pivot, rig.Forward, 83 * fall);
            actor.transform.position -= Vector3.up * (.72f * fall);
            // Seat the visible body on the local ground; do not leave the old standing mesh floating.
            if (fall > .7f)
            {
                if (fallSettled) { actor.transform.position += Vector3.up * settledLift; return; }
                if (!baked) baked = new Mesh();
                float bottom = float.PositiveInfinity;
                foreach (var skin in skins)
                {
                    skin.BakeMesh(baked); baked.GetVertices(vertices);
                    foreach (var vertex in vertices) bottom = Mathf.Min(bottom, skin.transform.TransformPoint(vertex).y);
                }
                if (!float.IsPositiveInfinity(bottom))
                {
                    settledLift = (transform.position.y + .02f - bottom) * Mathf.InverseLerp(.7f, 1, fall);
                    actor.transform.position += Vector3.up * settledLift;
                    if (fall >= 1) fallSettled = true;
                }
            }
        }
        else if (crouch > .001f)
        {
            var lf = leftFoot; var rf = rightFoot;
            Vector3 left = lf.position, right = rf.position; Quaternion lr = lf.rotation, rr = rf.rotation;
            actor.transform.position -= Vector3.up * (.38f * crouch);
            Chapter1IncidentRig.Solve(leftThigh,leftCalf,lf,left,rig.Forward);
            Chapter1IncidentRig.Solve(rightThigh,rightCalf,rf,right,rig.Forward);
            lf.rotation = lr; rf.rotation = rr;
        }
    }
    [Serializable] public sealed class Snapshot
    {
        public Vector3 position, target; public bool down, engaged, telegraph; public float suppression, attack, aim, crouch, fall, progress, travelled, lane; public int shots;
    }
    public Snapshot Capture() => new Snapshot { position = transform.position, target = shotTarget, down = Down, engaged = Engaged, telegraph = Telegraphing, suppression = suppression, attack = attackClock, aim = aimClock, crouch = crouch, fall = fallClock, shots = Shots, progress = RouteProgress, travelled = Travelled, lane = lane };
    public void Restore(Snapshot s)
    {
        motor.enabled = false; transform.position = s.position; Down = s.down; Engaged = s.engaged; Telegraphing = s.telegraph;
        suppression = s.suppression; attackClock = s.attack; aimClock = s.aim; crouch = s.crouch; fallClock = s.fall; Shots = s.shots;
        RouteProgress = s.progress; Travelled = s.travelled; lane = s.lane; shotTarget = s.target;
        trail.Clear(); replanClock = 0;
        motor.enabled = !Down; audioSource.Stop();
        fallSettled = false;
    }
    void OnDestroy() { if (baked) Destroy(baked); }
}
