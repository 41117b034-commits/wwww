using UnityEngine;

// A scene-owned character, independent of the previous chapters' story scripts.
// Route progress is measured from actual CharacterController displacement.
[DefaultExecutionOrder(4500)]
[RequireComponent(typeof(CharacterController))]
public sealed class Chapter4Companion : MonoBehaviour
{
    public string actorId;
    public string displayName;
    public Transform model;
    public bool wounded;
    public float walkSpeed = 1.75f;
    public float Progress { get; private set; }
    public bool JoinedRoute { get; private set; }
    public bool Walking { get; private set; }
    public Vector3 StepDirection { get; private set; }
    public float StepDistance { get; private set; }
    public bool Fallen { get; private set; }
    public Vector3 Chest => transform.position + Vector3.up * (wounded ? 1.05f : 1.18f);
    float fallElapsed;
    public bool Speaking { get; set; }
    public float CoverCrouch { get; private set; }
    public Chapter1IncidentRig Rig { get; private set; }
    public CharacterController Motor { get; private set; }
    public Chapter4CompanionPose Pose { get; private set; }
    public bool MotionHeld { get; set; }
    public Chapter4CompanionRifle EscortRifle { get; internal set; }
    public bool OnForwardFlank => EscortRifle && actorId == "tado";
    public bool IndependentFollow { get; set; }
    public float RouteOffset { get; set; }
    public Chapter4Companion SupportPartner { get; set; }
    public Chapter4Companion SupportFollower { get; set; }
    public Vector3? AssistTarget { get; set; }
    public bool IsPaused => owner && owner.chapter.WorldFrozen;
    public Chapter4SceneController RetreatChapter => owner ? owner.chapter : null;
    // A formation slot is an arrival area, not an exact point that can lie on
    // the edge of a roadside rock. Both route progress and physical distance
    // must agree before the party controller counts this character as arrived.
    public bool AtDestination => !Fallen && !MotionHeld && !AssistTarget.HasValue && (SupportPartner
        ? SupportPartner.AtDestination && Chapter4Evacuation.FlatDistance(transform.position, SupportTarget()) < .3f
        : JoinedRoute && Progress >= destination - .55f && Chapter4Evacuation.FlatDistance(transform.position, RouteTarget(destination)) < .9f);
    public float Destination => SupportPartner ? SupportPartner.Destination : destination;

    Chapter4Evacuation owner;
    Chapter4Companion ahead;
    float destination, releaseAt, travelClock;
    Vector3 previousPosition;
    readonly Collider[] laneHits = new Collider[24];
    SkinnedMeshRenderer[] faces;
    static readonly float[] pairLanes = { 0, .35f, -.65f, .65f, -.9f };
    static readonly float[] singleLanes = { 0, .35f, -.35f, .65f, -.65f, .9f, -.9f, 1.2f, -1.2f };

    public void Initialize(Chapter4Evacuation flow, Chapter4Companion predecessor, int order)
    {
        owner = flow;
        walkSpeed = wounded ? 1.7f : 2.6f;
        ahead = predecessor;
        Motor = GetComponent<CharacterController>();
        var animator = model.GetComponentInChildren<Animator>();
        Rig = model.gameObject.AddComponent<Chapter1IncidentRig>();
        Rig.Initialize(animator, false);
        Rig.batonVisible = false;
        // The shared rig supplies calibrated neutral bones. Chapter 4 owns its
        // escape gait; layering a crouch over the donor's walk looked stealthy.
        Rig.smoothLocomotion = false;
        Rig.walking = false;
        Rig.Face(flow.PointOnRoute(6) - flow.PointOnRoute(0));
        Rig.Ground(transform.position.y);
        Pose = gameObject.AddComponent<Chapter4CompanionPose>();
        Pose.Initialize(this, animator);
        previousPosition = transform.position;
        faces = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        // Player and companions may share a narrow path without creating an
        // unwinnable body-block. Terrain, trees, rocks and props remain solid.
        Physics.IgnoreCollision(Motor, flow.chapter.player.Motor);
        foreach (var other in flow.cast)
            if (other && other != this) Physics.IgnoreCollision(Motor, other.GetComponent<CharacterController>());
    }

    public void FallWounded(bool hitByPolice)
    {
        if (Fallen) return;
        Fallen = true; fallElapsed = 0; MotionHeld = true; Speaking = false; AssistTarget = null;
        Pose.Set(Chapter4CompanionPose.State.SideFall, 0);
        owner.ReportFall(this, hitByPolice);
    }

    public void FollowTo(float distance, float delay)
    {
        destination = Mathf.Max(Progress, distance);
        releaseAt = travelClock + delay;
    }

    void Update()
    {
        if (!owner || !Rig) return;
        Pose.RestoreModelBase();
        if (EscortRifle) EscortRifle.Advance(Time.deltaTime);
        Walking = false;
        StepDistance = 0;
        if (Fallen)
        {
            if (!IsPaused) fallElapsed = Mathf.Min(1.15f, fallElapsed + Time.deltaTime);
            Pose.Set(Chapter4CompanionPose.State.SideFall, Mathf.SmoothStep(0, 1, fallElapsed / 1.15f));
            Rig.walking = false; Rig.speakingWeight = 0; Rig.frozen = true;
            return;
        }
        if (!IsPaused)
        {
            var pressure = owner.chapter.Retreat;
            bool nearBlast = pressure && pressure.IsTelegraphing && Chapter4Evacuation.FlatDistance(transform.position, pressure.ImpactPosition) < 12;
            CoverCrouch = Mathf.MoveTowards(CoverCrouch, nearBlast ? .65f : 0, Time.deltaTime * 4);
        }
        if (!IsPaused)
        {
            travelClock += Time.deltaTime;
            if (!MotionHeld)
            {
                if (AssistTarget.HasValue) StepToward(AssistTarget.Value, Time.deltaTime);
                else if ((owner.CurrentStage == Chapter4Evacuation.Stage.Following || owner.OpeningRush || IndependentFollow) && travelClock >= releaseAt)
                    Walk(Time.deltaTime);
            }
        }
        StepDirection = Vector3.ProjectOnPlane(transform.position - previousPosition, Vector3.up).normalized;
        StepDistance = IsPaused ? 0 : Chapter4Evacuation.FlatDistance(transform.position, previousPosition);
        Rig.walking = false;
        Rig.frozen = IsPaused;
        Rig.speakingWeight = Speaking && !IsPaused ? .6f : 0;
        Rig.Ground(transform.position.y);
        // Orders are called over the shoulder while retreating. Speech must not
        // turn an otherwise forward-facing body back toward the player.
        if (SupportPartner && !AssistTarget.HasValue && !MotionHeld) Rig.Face(SupportPartner.Rig.Forward);
        previousPosition = transform.position;
        // This companion left the camp already wounded. If protected on the way,
        // their earlier injury can still stop them on the last climb.
        if (actorId == "wounded" && Progress >= 123 && !IsPaused) FallWounded(false);
    }

    void AnimateSpeech()
    {
        if (!owner || faces == null) return;
        var voice = owner.chapter.Voice;
        float weight = !IsPaused && voice && voice.SpeakerRole == actorId ? voice.MouthWeight * 80 : 0;
        foreach (var face in faces)
        {
            if (!face || !face.sharedMesh) continue;
            int jaw = face.sharedMesh.GetBlendShapeIndex(Chapter2SpeechFace.Jaw);
            if (jaw >= 0) face.SetBlendShapeWeight(jaw, weight);
        }
    }

    void Walk(float deltaTime)
    {
        if (SupportPartner)
        {
            StepToward(SupportTarget(), deltaTime);
            Progress = Mathf.Max(Progress, owner.ProjectProgress(transform.position, Progress, Mathf.Min(Destination, Progress + 1.5f)));
            return;
        }
        if (SupportFollower && Chapter4Evacuation.FlatDistance(transform.position, SupportFollower.transform.position) > 1.12f) return;
        // At the first shelling everyone leaves their own camp position.
        // Do not make the whole cast wait for the wounded leader to join a queue.
        bool rushing = owner.OpeningRush;
        var leader = ahead;
        while (leader && leader.Fallen) leader = leader.ahead;
        if (!rushing && !OnForwardFlank && leader && (!leader.JoinedRoute || leader.Progress < .9f)) return;
        float limit = leader && !rushing && !OnForwardFlank ? Mathf.Min(destination, leader.Progress - 1.35f) : destination;
        if (JoinedRoute && Progress >= limit - .06f)
        {
            // Longitudinal progress can reach the endpoint before the leader's
            // lateral arrival slot. Finish that short physical step instead of
            // leaving the entire party waiting under pursuit fire forever.
            if (destination >= owner.RouteLength - 12 && limit >= destination - .06f &&
                Chapter4Evacuation.FlatDistance(transform.position, RouteTarget(destination)) > .25f)
                StepToward(RouteTarget(destination), deltaTime);
            return;
        }
        if (SupportFollower && JoinedRoute) SelectPairLane(limit);
        else if (JoinedRoute) SelectSingleLane(limit);
        float entry = rushing ? owner.ProjectProgress(transform.position, 0, destination) : 0;
        Vector3 target = JoinedRoute ? RouteTarget(Mathf.Min(limit, Progress + .65f)) : owner.PointOnRoute(rushing ? Mathf.Min(destination, entry + 3) : entry);
        StepToward(target, deltaTime);
        if (!JoinedRoute)
        {
            float joinedAt = rushing ? owner.ProjectProgress(transform.position, 0, destination) : entry;
            if (Vector3.ProjectOnPlane(transform.position - owner.PointOnRoute(joinedAt), Vector3.up).magnitude < .20f)
            { JoinedRoute = true; Progress = joinedAt; }
        }
        else if (Walking) Progress = Mathf.Max(Progress, ProjectWalkedLane(Mathf.Min(destination, Progress + 1.5f)));
    }

    float ProjectWalkedLane(float end)
    {
        if (!EscortRifle || Mathf.Abs(RouteOffset) < .01f) return owner.ProjectProgress(transform.position, Progress, end);
        // At a bend the projection onto the middle of the trail can remain behind
        // someone already walking around its outside edge. Measure that person's lane.
        float best = float.PositiveInfinity, result = Progress, start = Progress;
        Vector3 a = RouteTarget(start);
        while (start < end)
        {
            float next = Mathf.Min(end, start + .25f);
            Vector3 b = RouteTarget(next), segment = Vector3.ProjectOnPlane(b - a, Vector3.up);
            float t = Mathf.Clamp01(Vector3.Dot(transform.position - a, segment) / Mathf.Max(.00001f, segment.sqrMagnitude));
            float error = Chapter4Evacuation.FlatDistance(transform.position, a + t * segment);
            if (error < best) { best = error; result = Mathf.Lerp(start,next,t); }
            a = b; start = next;
        }
        return result;
    }

    float LaneOffset(float distance, float offset)
    {
        if (!EscortRifle) return offset;
        // The two trails peel sharply away from the main path. Approach and leave
        // the narrow junction in single file, then reopen the flank gradually.
        float before = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(57, 64, distance));
        float after = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(92, 100, distance));
        return offset * Mathf.Max(before, after);
    }

    void StepToward(Vector3 target, float deltaTime)
    {
        Vector3 delta = Vector3.ProjectOnPlane(target - transform.position, Vector3.up);
        if (delta.magnitude > .025f)
        {
            float step = Mathf.Min(delta.magnitude, walkSpeed * Pose.Pace * deltaTime);
            Motor.Move(delta.normalized * step + Vector3.down * (2f * deltaTime));
            Vector3 moved = Vector3.ProjectOnPlane(transform.position - previousPosition, Vector3.up);
            Walking = moved.magnitude > .0005f;
            if (Walking && !(EscortRifle && EscortRifle.Turning)) Rig.Face(Vector3.RotateTowards(Rig.Forward, moved.normalized, 220 * Mathf.Deg2Rad * deltaTime, 0));
        }
    }

    public Vector3 RouteRight(float distance)
    {
        Vector3 forward = owner.PointOnRoute(distance + .5f) - owner.PointOnRoute(distance - .5f);
        return Vector3.Cross(Vector3.up, forward).normalized;
    }

    Vector3 SupportTarget() => SupportPartner.transform.position + RouteRight(SupportPartner.Progress) * .72f;

    void SelectPairLane(float limit)
    {
        // The authored centerline was walkable for a single person; a coupled
        // pair also needs space for the helper. Check both bodies ahead and
        // choose a nearby clear lane, then reach it with ordinary bounded Move.
        // Keep the current lane until obstructed to avoid left/right jitter.
        float near = Mathf.Min(limit, Progress + .35f), far = Mathf.Min(limit, Progress + 1.25f);
        if (PairFits(near, RouteOffset) && PairFits(far, RouteOffset)) return;
        foreach (float offset in pairLanes)
            if (PairFits(near, offset) && PairFits(far, offset)) { RouteOffset = offset; return; }
    }

    bool PairFits(float distance, float offset)
    {
        Vector3 point = owner.PointOnRoute(distance), right = RouteRight(distance);
        return BodyFits(point + right * offset) && BodyFits(point + right * (offset + .72f));
    }

    void SelectSingleLane(float limit)
    {
        float near = Mathf.Min(limit, Progress + .35f), far = Mathf.Min(limit, Progress + 1.25f);
        bool Fits(float distance, float offset) => BodyFits(owner.PointOnRoute(distance) + RouteRight(distance) * LaneOffset(distance, offset));
        if (Fits(near, RouteOffset) && Fits(far, RouteOffset)) return;
        foreach (float offset in singleLanes)
            if (Fits(near, offset) && Fits(far, offset)) { RouteOffset = offset; return; }
    }

    bool BodyFits(Vector3 feet)
    {
        float radius = Motor.radius + .05f;
        Vector3 center = feet + Motor.center + Vector3.up * .04f;
        Vector3 half = Vector3.up * (Motor.height * .5f - radius);
        int count = Physics.OverlapCapsuleNonAlloc(center - half, center + half, radius, laneHits, ~0, QueryTriggerInteraction.Ignore);
        if (count == laneHits.Length) return false;
        for (int i = 0; i < count; i++)
        {
            var hit = laneHits[i];
            if (hit == owner.terrain || hit == owner.chapter.player.Motor || hit.GetComponent<Chapter4Companion>()) continue;
            return false;
        }
        return true;
    }

    Vector3 RouteTarget(float distance)
    {
        Vector3 point = owner.PointOnRoute(distance);
        point += RouteRight(distance) * LaneOffset(distance, RouteOffset);
        // The first person approaches beside the signal point, leaving room
        // for a player who remains there to watch the party arrive.
        if (!ahead && destination >= owner.RouteLength - .05f)
        {
            Vector3 forward = owner.PointOnRoute(destination) - owner.PointOnRoute(destination - .7f);
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            point -= right * (.95f * Mathf.SmoothStep(0, 1, Mathf.InverseLerp(destination - 3, destination, distance)));
        }
        return point;
    }

    public sealed class Snapshot
    {
        public Vector3 position, forward;
        public float progress, destination, speed, offset, fallElapsed;
        public bool joined, held, independent, fallen;
        public Chapter4Companion partner, follower;
        public Chapter4CompanionPose.State pose;
        public float poseWeight, poseClock;
        public Chapter4CompanionPose.EscapeSnapshot escape;
    }

    public Snapshot Capture() => new Snapshot { position = transform.position, forward = Rig.Forward,
        progress = Progress, destination = destination, speed = walkSpeed, offset = RouteOffset,
        fallen = Fallen, fallElapsed = fallElapsed, joined = JoinedRoute, held = MotionHeld, independent = IndependentFollow, partner = SupportPartner, follower = SupportFollower,
        pose = Pose.CurrentState, poseWeight = Pose.Weight, poseClock = Pose.Clock, escape = Pose.CaptureEscape() };

    public void Restore(Snapshot saved)
    {
        Pose.RestoreModelBase();
        Motor.enabled = false; transform.position = saved.position; Motor.enabled = true;
        Progress = saved.progress; destination = saved.destination; walkSpeed = saved.speed; RouteOffset = saved.offset;
        Fallen = saved.fallen; fallElapsed = saved.fallElapsed; Rig.frozen = Fallen;
        JoinedRoute = saved.joined; MotionHeld = saved.held; SupportPartner = saved.partner; SupportFollower = saved.follower;
        IndependentFollow = saved.independent;
        AssistTarget = null; Walking = Speaking = false; StepDistance = CoverCrouch = 0; releaseAt = travelClock = 0;
        Rig.Face(saved.forward); Rig.Ground(transform.position.y); Rig.walking = false;
        Pose.Set(saved.pose, saved.poseWeight, saved.poseClock);
        Pose.RestoreEscape(saved.escape);
        previousPosition = transform.position;
    }

    void LateUpdate()
    {
        AnimateSpeech();
        // All limb posing belongs to Chapter4CompanionPose. A second injured
        // arm solve here folded the sleeve into the torso before the escape pose.
    }
}
