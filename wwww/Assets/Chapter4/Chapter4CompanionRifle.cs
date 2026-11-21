using UnityEngine;

// Two escorts can return fire while their own locomotion continues.
[DefaultExecutionOrder(4800)]
public sealed class Chapter4CompanionRifle : MonoBehaviour
{
    public int Shots { get; private set; }
    public bool Turning => target != null;
    public Transform Weapon { get; private set; }
    Chapter4Companion actor;
    Chapter4RearGuard combat;
    Chapter4Pursuer target;
    AudioSource report;
    Transform muzzle;
    GameObject flash;
    LineRenderer shotTrail;
    Material trailMaterial;
    Vector3 trailFacing, shotStart, shotEnd;
    float beat, raise, sinceShot = 1;
    bool reported, returning, audioPaused;

    internal void Initialize(Chapter4Companion companion, Chapter4RearGuard owner, Chapter2Rifle source)
    {
        actor = companion; combat = owner;
        bool forwardEscort = actor.actorId == "tado";
        actor.RouteOffset = forwardEscort ? 1.35f : -1.05f;
        if (forwardEscort) actor.walkSpeed = 3.5f;
        Weapon = Instantiate(source.weapon, actor.transform, false);
        Weapon.name = "撤離時攜帶的步槍"; Weapon.localScale = Vector3.one;
        // On the donor prop the sling arches over the stock and the barrel
        // sits below the pivot. Register its visible barrel with the FX socket.
        var body = Weapon.GetComponentInChildren<MeshFilter>().transform;
        Quaternion roll = Quaternion.AngleAxis(180, Vector3.forward);
        body.localRotation = roll * body.localRotation;
        body.localPosition = roll * body.localPosition + Vector3.down * .099f;
        muzzle = Weapon.Find(source.muzzle.name);
        flash = muzzle.Find(source.muzzleFlash.name).gameObject;
        flash.transform.localPosition = Vector3.forward * .027f;
        flash.transform.localScale = new Vector3(.052f, .052f, .14f);
        flash.SetActive(false);
        shotTrail = new GameObject("族人回身射出的彈光").AddComponent<LineRenderer>();
        shotTrail.transform.SetParent(transform, false); shotTrail.useWorldSpace = true; shotTrail.positionCount = 2;
        trailMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        trailMaterial.SetColor("_BaseColor", new Color(1, .77f, .42f)); shotTrail.sharedMaterial = trailMaterial;
        shotTrail.startWidth = .020f; shotTrail.endWidth = .006f; shotTrail.enabled = false;
        report = muzzle.gameObject.AddComponent<AudioSource>();
        report.playOnAwake = false; report.spatialBlend = 1; report.minDistance = 5; report.maxDistance = 52; report.rolloffMode = AudioRolloffMode.Linear;
    }

    internal bool TryTurn()
    {
        if (Turning || actor.Fallen || actor.MotionHeld || !actor.JoinedRoute || actor.Pose.CurrentState != Chapter4CompanionPose.State.Normal ||
            combat.Chapter.evacuation.Inside(actor)) return false;
        var flow = combat.Chapter.evacuation;
        Vector3 trail = Vector3.ProjectOnPlane(flow.PointOnRoute(actor.Progress + .8f) - flow.PointOnRoute(actor.Progress - .8f), Vector3.up).normalized;
        foreach (var pursuer in combat.Enemies)
        {
            Vector3 behind = pursuer.transform.position - actor.transform.position;
            if (!pursuer.Engaged || pursuer.Down || pursuer.Suppressed || behind.sqrMagnitude > 46 * 46 ||
                Vector3.Dot(behind, trail) >= -2 || !combat.ClearEscortShot(actor, actor.Pose.RightShoulder.position, pursuer.AimPoint)) continue;
            target = pursuer; trailFacing = trail; beat = raise = 0; reported = returning = false;
            return true;
        }
        return false;
    }

    // Called before the actor takes a step, after restoring the preceding pose.
    internal void Advance(float dt)
    {
        bool frozen = combat.Frozen;
        if (frozen != audioPaused)
        {
            audioPaused = frozen;
            if (frozen) report.Pause(); else report.UnPause();
        }
        if (frozen) return;
        sinceShot += dt;
        if (!Turning) return;
        beat += dt;
        if (combat.Assisting || target.Down || actor.MotionHeld || (reported && sinceShot > .24f) || beat > 1.85f)
            returning = true;
        Vector3 facing = returning ? trailFacing : Vector3.ProjectOnPlane(target.AimPoint - actor.transform.position, Vector3.up).normalized;
        var rig = actor.Rig;
        rig.Face(Vector3.RotateTowards(rig.Forward, facing, dt * 230 * Mathf.Deg2Rad, 0));
        bool lift = !returning && Vector3.Dot(rig.Forward, facing) > .45f;
        raise = Mathf.MoveTowards(raise, lift ? 1 : 0, dt * 3.2f);
        if (returning && raise == 0 && Vector3.Dot(rig.Forward, facing) > .98f) target = null;
    }

    void LateUpdate()
    {
        if (!actor || !Weapon) return;
        shotTrail.enabled = false;
        if (actor.Fallen)
        {
            target = null; flash.SetActive(false);
            Vector3 side = Vector3.Cross(Vector3.up, actor.Rig.Forward);
            Vector3 dropped = actor.transform.position + side * .49f + Vector3.up * .13f;
            Weapon.position = Vector3.Lerp(Weapon.position, dropped, 1 - Mathf.Exp(-9 * Time.deltaTime));
            Weapon.rotation = Quaternion.LookRotation(actor.Rig.Forward, side);
            return;
        }
        Vector3 forward = actor.Rig.Forward, right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 shoulder = actor.Pose.RightShoulder.position;
        Vector3 stock = shoulder + Vector3.Lerp(Vector3.down * .32f + forward * .16f + right * .035f,
            Vector3.down * .105f - forward * .105f - right * .10f, raise);
        Vector3 carry = (forward * .65f - right * .66f - Vector3.up * .38f).normalized;
        Vector3 direction = Vector3.Slerp(carry, Turning ? (target.AimPoint - stock).normalized : carry, raise);
        float kick = sinceShot < .20f ? Mathf.Sin(sinceShot / .20f * Mathf.PI) : 0;
        Weapon.SetPositionAndRotation(stock - direction * (.036f * kick), Quaternion.LookRotation(direction, Vector3.up));
        actor.Pose.HoldRifle(Weapon);
        if (Turning && !combat.Frozen && !combat.Assisting && !reported && !returning && !target.Down &&
            !target.Suppressed && raise >= .99f && Vector3.Dot(forward, direction) > .94f && combat.ClearEscortShot(actor, muzzle.position, target.AimPoint))
        {
            reported = true; sinceShot = 0; Shots++;
            shotStart = muzzle.position; shotEnd = target.AimPoint;
            combat.ResolveEscortReport(target);
            report.PlayOneShot(combat.Gun.shotClip, .72f);
        }
        flash.SetActive(sinceShot < .085f && !combat.Frozen);
        if (sinceShot < .13f && !combat.Frozen)
        {
            float head = Mathf.Lerp(.09f, 1, sinceShot / .13f);
            float segment = Mathf.Min(.19f, 1.5f / Mathf.Max(1, Vector3.Distance(shotStart, shotEnd)));
            shotTrail.SetPosition(0, Vector3.Lerp(shotStart, shotEnd, Mathf.Max(0, head - segment)));
            shotTrail.SetPosition(1, Vector3.Lerp(shotStart, shotEnd, head));
            shotTrail.enabled = true;
        }
    }

    void OnDestroy() { if (trailMaterial) Destroy(trailMaterial); }

    [System.Serializable] public sealed class Snapshot
    {
        public int shots, enemy;
        public float beat, raise, sinceShot;
        public bool reported, returning;
        public Vector3 trail, start, end;
    }
    internal Snapshot Capture() => new Snapshot { shots = Shots, enemy = Turning ? target.Index : -1, beat = beat,
        raise = raise, sinceShot = sinceShot, reported = reported, returning = returning, trail = trailFacing, start = shotStart, end = shotEnd };
    internal void Restore(Snapshot s)
    {
        target = s != null && s.enemy >= 0 && s.enemy < combat.Enemies.Count ? combat.Enemies[s.enemy] : null;
        Shots = s?.shots ?? 0; beat = s?.beat ?? 0; raise = s?.raise ?? 0; sinceShot = s?.sinceShot ?? 1;
        reported = s?.reported ?? false; returning = s?.returning ?? false; trailFacing = s?.trail ?? Vector3.zero;
        shotStart = s?.start ?? Vector3.zero; shotEnd = s?.end ?? Vector3.zero;
        report.Stop(); flash.SetActive(false); shotTrail.enabled = false;
    }
}
