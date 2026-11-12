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
    // A formation slot is an arrival area, not an exact point that can lie on
    // the edge of a roadside rock. Both route progress and physical distance
    // must agree before the party controller counts this character as arrived.
    public bool AtDestination => JoinedRoute && Progress >= destination - .55f &&
        Chapter4Evacuation.FlatDistance(transform.position, RouteTarget(destination)) < .9f;
    public float Destination => destination;

    Chapter4Evacuation owner;
    Chapter4Companion ahead;
    float destination, releaseAt, travelClock;
    Vector3 previousPosition;
    Transform upperArm, elbow, hand, spine;

    public void Initialize(Chapter4Evacuation flow, Chapter4Companion predecessor, int order)
    {
        owner = flow;
        ahead = predecessor;
        Motor = GetComponent<CharacterController>();
        var animator = model.GetComponentInChildren<Animator>();
        Rig = model.gameObject.AddComponent<Chapter1IncidentRig>();
        Rig.Initialize(animator, false);
        Rig.batonVisible = false;
        Rig.smoothLocomotion = true;
        Rig.BeginDepartureWalk(order * .72f);
        Rig.walking = false;
        Rig.Face(flow.chapter.player.transform.position - transform.position);
        Rig.Ground(transform.position.y);
        upperArm = Chapter1WeddingRigBones.Resolve(animator, HumanBodyBones.LeftUpperArm, "L_Upperarm", "LeftArm");
        elbow = Chapter1WeddingRigBones.Resolve(animator, HumanBodyBones.LeftLowerArm, "L_Forearm", "LeftForeArm");
        hand = Chapter1WeddingRigBones.Resolve(animator, HumanBodyBones.LeftHand, "L_Hand", "LeftHand");
        spine = Chapter1WeddingRigBones.Resolve(animator, HumanBodyBones.Spine, "Spine", "Spine");
        previousPosition = transform.position;
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
        Walking = false;
        if (!owner.chapter.IsPaused)
        {
            travelClock += Time.deltaTime;
            if (owner.CurrentStage == Chapter4Evacuation.Stage.Following && travelClock >= releaseAt)
                Walk(Time.deltaTime);
        }
        Rig.walking = Walking;
        Rig.frozen = owner.chapter.IsPaused;
        Rig.speakingWeight = Speaking && !owner.chapter.IsPaused ? .6f : 0;
        Rig.Ground(transform.position.y);
        if (!Walking && Speaking) Rig.Face(owner.chapter.player.transform.position - transform.position);
        previousPosition = transform.position;
    }

    void Walk(float deltaTime)
    {
        if (ahead && (!ahead.JoinedRoute || ahead.Progress < .9f)) return;
        float limit = ahead ? Mathf.Min(destination, ahead.Progress - 1.35f) : destination;
        if (JoinedRoute && Progress >= limit - .06f) return;
        Vector3 target = JoinedRoute ? RouteTarget(Mathf.Min(limit, Progress + .65f)) : owner.PointOnRoute(0);
        Vector3 delta = Vector3.ProjectOnPlane(target - transform.position, Vector3.up);
        if (delta.magnitude > .025f)
        {
            float step = Mathf.Min(delta.magnitude, walkSpeed * deltaTime);
            Motor.Move(delta.normalized * step + Vector3.down * (2f * deltaTime));
            Vector3 moved = Vector3.ProjectOnPlane(transform.position - previousPosition, Vector3.up);
            Walking = moved.magnitude > .0005f;
            if (Walking) Rig.Face(moved);
        }
        if (!JoinedRoute)
        {
            if (Vector3.ProjectOnPlane(transform.position - owner.PointOnRoute(0), Vector3.up).magnitude < .18f)
                JoinedRoute = true;
        }
        else Progress = Mathf.Max(Progress, owner.ProjectProgress(transform.position, Progress, Mathf.Min(destination, Progress + 1.5f)));
    }

    Vector3 RouteTarget(float distance)
    {
        Vector3 point = owner.PointOnRoute(distance);
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

    void LateUpdate()
    {
        // An injured arm stays supported while the existing gait moves the legs.
        if (!wounded || !Rig || !upperArm || !elbow || !hand) return;
        Vector3 forward = Rig.Forward;
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        Vector3 target = upperArm.position + right * .22f + forward * .13f + Vector3.down * .26f;
        Chapter1IncidentRig.Solve(upperArm, elbow, hand, target, -forward + Vector3.down);
        if (spine) spine.rotation = Quaternion.AngleAxis(5, right) * spine.rotation;
    }
}
