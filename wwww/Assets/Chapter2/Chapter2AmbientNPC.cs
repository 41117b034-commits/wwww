using UnityEngine;

// Background cast stays in authored pockets outside the escort and story blocking.
[DefaultExecutionOrder(4100)]
[RequireComponent(typeof(Chapter2Actor))]
public sealed class Chapter2AmbientNPC : MonoBehaviour
{
    public Chapter2Controller chapter;
    public Vector3 patrolOffset = new Vector3(1.2f, 0, .5f);
    public Vector3 watchDirection = Vector3.forward;
    public float pauseSeconds = 5;
    public float phaseOffset;
    public float speed = .42f;
    public bool patrolEnabled = true;
    public float MaxDistanceFromHome { get; private set; }
    public int TripsCompleted { get; private set; }
    public Vector3 Home { get; private set; }
    public Transform ConversationPartner { get; set; }
    Chapter2Actor actor;
    float began;
    bool configured, wasMoving;
    Vector3 facingFrom;

    void Start()
    {
        actor = GetComponent<Chapter2Actor>();
        Home = transform.position;
        began = Time.time - phaseOffset;
    }

    public void RestartPatrol()
    {
        Home=transform.position;began=Time.time;wasMoving=false;
        patrolEnabled=true;ConversationPartner=null;
    }

    void Update()
    {
        if (!actor || !actor.Rig) return;
        if (ConversationPartner)
        {
            // Freeze the patrol clock too, so resuming never snaps to a later waypoint.
            began += Time.deltaTime;
            actor.Face(ConversationPartner.position);
            return;
        }
        if (!patrolEnabled) return;
        if (!configured)
        {
            actor.Rig.BeginDepartureWalk(phaseOffset * .13f);
            configured = true;
        }
        // At the confrontation, background people stop and watch; they never
        // continue a casual patrol through the gunshot. The day parent hides them at night.
        if (chapter && chapter.CurrentStage >= Chapter2Controller.Stage.TreeChoice)
        {
            actor.Face(chapter.sacredTree.position);
            return;
        }
        float length = Vector3.ProjectOnPlane(patrolOffset, Vector3.up).magnitude;
        float travel = Mathf.Max(2, length / Mathf.Max(.15f, speed));
        float leg = pauseSeconds + 1.2f + travel;
        float elapsed = Mathf.Max(0, Time.time - began);
        int trip = Mathf.FloorToInt(elapsed / leg);
        float t = elapsed % leg;
        bool returning = trip % 2 != 0;
        Vector3 start = Home + (returning ? patrolOffset : Vector3.zero);
        Vector3 end = Home + (returning ? Vector3.zero : patrolOffset);
        Vector3 direction = (end - start).normalized;
        bool moving = t > pauseSeconds + 1.2f;
        if (t < pauseSeconds)
        {
            Vector3 look = Quaternion.AngleAxis(Mathf.Sin(elapsed * .42f + phaseOffset) * 14, Vector3.up) * watchDirection;
            actor.Face(transform.position + look);
            facingFrom = actor.Rig.Forward;
        }
        else if (!moving)
        {
            actor.Face(transform.position + Vector3.Slerp(facingFrom, direction,
                Mathf.SmoothStep(0, 1, (t - pauseSeconds) / 1.2f)));
        }
        else
        {
            float u = Mathf.Clamp01((t - pauseSeconds - 1.2f) / travel);
            // Ease the first and last step while preserving continuous travel.
            transform.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0, 1, u));
            actor.Face(transform.position + direction);
        }
        if (wasMoving && !moving) transform.position = start;
        wasMoving = moving;
        TripsCompleted = trip;
        MaxDistanceFromHome = Mathf.Max(MaxDistanceFromHome, Vector3.Distance(Home, transform.position));
    }
}
