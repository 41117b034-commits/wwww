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
    public bool Speaking { get; set; }
    public Chapter1IncidentRig Rig { get; private set; }
    public CharacterController Motor { get; private set; }
    public Chapter4CompanionPose Pose { get; private set; }
    public bool MotionHeld { get; set; }
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
    public bool AtDestination => !MotionHeld && !AssistTarget.HasValue && (SupportPartner
        ? SupportPartner.AtDestination && Chapter4Evacuation.FlatDistance(transform.position, SupportTarget()) < .3f
        : JoinedRoute && Progress >= destination - .55f && Chapter4Evacuation.FlatDistance(transform.position, RouteTarget(destination)) < .9f);
    public float Destination => SupportPartner ? SupportPartner.Destination : destination;

    Chapter4Evacuation owner;
    Chapter4Companion ahead;
    float destination, releaseAt, travelClock;
    Vector3 previousPosition;
    Transform upperArm, elbow, hand, spine;
    readonly Collider[] laneHits = new Collider[24];
    SkinnedMeshRenderer[] faces;
    static readonly float[] pairLanes = { 0, .35f, -.65f, .65f, -.9f };

    public void Initialize(Chapter4Evacuation flow, Chapter4Companion predecessor, int order)
    {
        owner = flow;
        walkSpeed = wounded ? 1.7f : 2.15f;
        ahead = predecessor;
        Motor = GetComponent<CharacterController>();
        var animator = model.GetComponentInChildren<Animator>();
        Rig = model.gameObject.AddComponent<Chapter1IncidentRig>();
        Rig.Initialize(animator, false);
        Rig.batonVisible = false;
        Rig.smoothLocomotion = true;
        Rig.BeginDepartureWalk(order * .72f);
        Rig.walking = false;
        Rig.Face(flow.PointOnRoute(6) - flow.PointOnRoute(0));
        Rig.Ground(transform.position.y);
        Pose = gameObject.AddComponent<Chapter4CompanionPose>();
        Pose.Initialize(this, animator);
        upperArm = Chapter1WeddingRigBones.Resolve(animator, HumanBodyBones.LeftUpperArm, "L_Upperarm", "LeftArm");
        elbow = Chapter1WeddingRigBones.Resolve(animator, HumanBodyBones.LeftLowerArm, "L_Forearm", "LeftForeArm");
        hand = Chapter1WeddingRigBones.Resolve(animator, HumanBodyBones.LeftHand, "L_Hand", "LeftHand");
        spine = Chapter1WeddingRigBones.Resolve(animator, HumanBodyBones.Spine, "Spine", "Spine1", "Spine01");
        previousPosition = transform.position;
        faces = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        // Player and companions may share a narrow path without creating an
        // unwinnable body-block. Terrain, trees, rocks and props remain solid.
        Physics.IgnoreCollision(Motor, flow.chapter.player.Motor);
        foreach (var other in flow.cast)
            if (other && other != this) Physics.IgnoreCollision(Motor, other.GetComponent<CharacterController>());
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
        Walking = false;
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
        Rig.walking = Walking;
        Rig.frozen = IsPaused;
        Rig.speakingWeight = Speaking && !IsPaused ? .6f : 0;
        Rig.Ground(transform.position.y);
        // Orders are called over the shoulder while retreating. Speech must not
        // turn an otherwise forward-facing body back toward the player.
        if (SupportPartner && !AssistTarget.HasValue && !MotionHeld) Rig.Face(SupportPartner.Rig.Forward);
        previousPosition = transform.position;
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
        if (!rushing && ahead && (!ahead.JoinedRoute || ahead.Progress < .9f)) return;
        float limit = ahead && !rushing ? Mathf.Min(destination, ahead.Progress - 1.35f) : destination;
        if (JoinedRoute && Progress >= limit - .06f) return;
        if (SupportFollower && JoinedRoute) SelectPairLane(limit);
        float entry = rushing ? owner.ProjectProgress(transform.position, 0, destination) : 0;
        Vector3 target = JoinedRoute ? RouteTarget(Mathf.Min(limit, Progress + .65f)) : owner.PointOnRoute(rushing ? Mathf.Min(destination, entry + 3) : entry);
        StepToward(target, deltaTime);
        if (!JoinedRoute)
        {
            float joinedAt = rushing ? owner.ProjectProgress(transform.position, 0, destination) : entry;
            if (Vector3.ProjectOnPlane(transform.position - owner.PointOnRoute(joinedAt), Vector3.up).magnitude < .20f)
            { JoinedRoute = true; Progress = joinedAt; }
        }
        else Progress = Mathf.Max(Progress, owner.ProjectProgress(transform.position, Progress, Mathf.Min(destination, Progress + 1.5f)));
    }

    void StepToward(Vector3 target, float deltaTime)
    {
        Vector3 delta = Vector3.ProjectOnPlane(target - transform.position, Vector3.up);
        if (delta.magnitude > .025f)
        {
            float step = Mathf.Min(delta.magnitude, walkSpeed * deltaTime);
            Motor.Move(delta.normalized * step + Vector3.down * (2f * deltaTime));
            Vector3 moved = Vector3.ProjectOnPlane(transform.position - previousPosition, Vector3.up);
            Walking = moved.magnitude > .0005f;
            if (Walking) Rig.Face(Vector3.RotateTowards(Rig.Forward, moved.normalized, 220 * Mathf.Deg2Rad * deltaTime, 0));
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
        point += RouteRight(distance) * RouteOffset;
        // The first person approaches beside the signal point, leaving room
        // for a player who remains there to watch the party arrive.
        if (!ahead)
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
        public float progress, destination, speed, offset;
        public bool joined, held, independent;
        public Chapter4Companion partner, follower;
        public Chapter4CompanionPose.State pose;
        public float poseWeight, poseClock;
    }

    public Snapshot Capture() => new Snapshot { position = transform.position, forward = Rig.Forward,
        progress = Progress, destination = destination, speed = walkSpeed, offset = RouteOffset,
        joined = JoinedRoute, held = MotionHeld, independent = IndependentFollow, partner = SupportPartner, follower = SupportFollower,
        pose = Pose.CurrentState, poseWeight = Pose.Weight, poseClock = Pose.Clock };

    public void Restore(Snapshot saved)
    {
        Pose.RestoreModelBase();
        Motor.enabled = false; transform.position = saved.position; Motor.enabled = true;
        Progress = saved.progress; destination = saved.destination; walkSpeed = saved.speed; RouteOffset = saved.offset;
        JoinedRoute = saved.joined; MotionHeld = saved.held; SupportPartner = saved.partner; SupportFollower = saved.follower;
        IndependentFollow = saved.independent;
        AssistTarget = null; Walking = Speaking = false; releaseAt = travelClock = 0;
        Rig.Face(saved.forward); Rig.Ground(transform.position.y); Rig.walking = false;
        Pose.Set(saved.pose, saved.poseWeight, saved.poseClock);
        previousPosition = transform.position;
    }

    void LateUpdate()
    {
        AnimateSpeech();
        // An injured arm stays supported while the existing gait moves the legs.
        if (!wounded || !Rig || !upperArm || !elbow || !hand) return;
        Vector3 forward = Rig.Forward;
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        Vector3 target = upperArm.position + right * .22f + forward * .13f + Vector3.down * .26f;
        Chapter1IncidentRig.Solve(upperArm, elbow, hand, target, -forward + Vector3.down);
        if (spine) spine.rotation = Quaternion.AngleAxis(5, right) * spine.rotation;
    }
}
