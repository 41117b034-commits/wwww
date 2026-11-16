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
    public bool Threatening => Engaged && !Down && !Suppressed && owner.LaneVisible(this);
    Chapter4RearGuard owner;
    CharacterController motor;
    AudioSource audioSource;
    Transform aimTarget;
    Vector3 destination, visualPosition;
    Quaternion visualRotation;
    float suppression, attackClock, aimClock, crouch, fallClock;
    bool posed, audioPaused;
    bool fallSettled;
    float settledLift;
    Transform leftFoot, rightFoot, leftThigh, rightThigh, leftCalf, rightCalf;
    Mesh baked;
    readonly List<Vector3> vertices = new List<Vector3>();
    SkinnedMeshRenderer[] skins;

    public void Initialize(Chapter4RearGuard combat, int index, int group, Vector3 start, Vector3 end)
    {
        owner = combat; Index = index; Group = group; destination = end;
        transform.position = start;
        motor = gameObject.AddComponent<CharacterController>();
        motor.height = 1.75f; motor.radius = .3f; motor.center = Vector3.up * .88f; motor.stepOffset = .32f; motor.skinWidth = .03f;
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
        if (Engaged && !Suppressed)
        {
            Vector3 delta = Vector3.ProjectOnPlane(destination - transform.position, Vector3.up);
            if (delta.magnitude > .12f) motor.Move(Vector3.ClampMagnitude(delta, .7f * dt) + Vector3.down * (3 * dt));
        }
        actor.GroundHeight = transform.position.y;
        var player = owner.Chapter.player;
        aimTarget.position = player.transform.position + Vector3.up * (player.IsBracing ? .95f : 1.22f);
        actor.Face(aimTarget.position);
        bool visible = Engaged && !Suppressed && Vector3.Distance(AimPoint, aimTarget.position) < 52 && owner.ClearTerrain(AimPoint, aimTarget.position);
        if (!visible) { Telegraphing = false; aimClock = 0; rifle.aim = 0; rifle.lowered = .65f; return; }
        attackClock -= dt;
        if (attackClock <= 0) { Telegraphing = true; aimClock += dt; }
        rifle.lowered = 0; rifle.aim = Telegraphing ? Mathf.Clamp01(aimClock * 1.5f) : .2f;
        if (Telegraphing && aimClock >= 1.7f)
        {
            Shots++; rifle.Fire(); audioSource.PlayOneShot(owner.Gun.shotClip, .5f);
            owner.EnemyReport(this, aimTarget.position);
            if (owner.ClearTerrain(AimPoint, aimTarget.position)) owner.Chapter.gas.ReceiveGunfire(player.IsBracing ? .07f : .16f);
            Telegraphing = false; aimClock = 0; attackClock = 4.8f + Index * .2f;
        }
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
        public Vector3 position; public bool down, engaged, telegraph; public float suppression, attack, aim, crouch, fall; public int shots;
    }
    public Snapshot Capture() => new Snapshot { position = transform.position, down = Down, engaged = Engaged, telegraph = Telegraphing, suppression = suppression, attack = attackClock, aim = aimClock, crouch = crouch, fall = fallClock, shots = Shots };
    public void Restore(Snapshot s)
    {
        motor.enabled = false; transform.position = s.position; Down = s.down; Engaged = s.engaged; Telegraphing = s.telegraph;
        suppression = s.suppression; attackClock = s.attack; aimClock = s.aim; crouch = s.crouch; fallClock = s.fall; Shots = s.shots;
        motor.enabled = !Down; audioSource.Stop();
        fallSettled = false;
    }
    void OnDestroy() { if (baked) Destroy(baked); }
}
