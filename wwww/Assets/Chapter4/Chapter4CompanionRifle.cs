using UnityEngine;

// A short performance for two members of the fleeing party. Their report
// interrupts one pursuer's animation and never consumes the player's round.
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
    Vector3 trailFacing;
    float beat, raise, sinceShot = 1;
    bool reported, returning, audioPaused;

    internal void Initialize(Chapter4Companion companion, Chapter4RearGuard owner, Chapter2Rifle source)
    {
        actor = companion; combat = owner;
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
        flash.transform.localScale = new Vector3(.032f, .032f, .085f);
        flash.SetActive(false);
        report = muzzle.gameObject.AddComponent<AudioSource>();
        report.playOnAwake = false; report.spatialBlend = 1; report.minDistance = 3; report.maxDistance = 48;
    }

    internal bool TryTurn()
    {
        if (Turning || actor.MotionHeld || !actor.JoinedRoute || actor.Pose.CurrentState != Chapter4CompanionPose.State.Normal ||
            actor.Progress >= combat.Chapter.evacuation.RouteLength - 16) return false;
        var flow = combat.Chapter.evacuation;
        Vector3 trail = Vector3.ProjectOnPlane(flow.PointOnRoute(actor.Progress + .8f) - flow.PointOnRoute(actor.Progress - .8f), Vector3.up).normalized;
        foreach (var pursuer in combat.Enemies)
        {
            Vector3 behind = pursuer.transform.position - actor.transform.position;
            if (!pursuer.Engaged || pursuer.Down || pursuer.Suppressed || behind.sqrMagnitude > 32 * 32 ||
                Vector3.Dot(behind, trail) >= -2 || !combat.ClearTerrain(actor.Pose.RightShoulder.position, pursuer.AimPoint)) continue;
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
            !target.Suppressed && raise >= .99f && Vector3.Dot(forward, direction) > .94f && combat.ClearTerrain(muzzle.position, target.AimPoint))
        {
            reported = true; sinceShot = 0; Shots++;
            target.Suppress(1.65f);
            report.PlayOneShot(combat.Gun.shotClip, .44f);
        }
        flash.SetActive(sinceShot < .10f && !combat.Frozen);
    }

    [System.Serializable] public sealed class Snapshot
    {
        public int shots, enemy;
        public float beat, raise, sinceShot;
        public bool reported, returning;
        public Vector3 trail;
    }
    internal Snapshot Capture() => new Snapshot { shots = Shots, enemy = Turning ? target.Index : -1, beat = beat,
        raise = raise, sinceShot = sinceShot, reported = reported, returning = returning, trail = trailFacing };
    internal void Restore(Snapshot s)
    {
        target = s != null && s.enemy >= 0 && s.enemy < combat.Enemies.Count ? combat.Enemies[s.enemy] : null;
        Shots = s?.shots ?? 0; beat = s?.beat ?? 0; raise = s?.raise ?? 0; sinceShot = s?.sinceShot ?? 1;
        reported = s?.reported ?? false; returning = s?.returning ?? false; trailFacing = s?.trail ?? Vector3.zero;
        report.Stop(); flash.SetActive(false);
    }
}
