using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Chapter1PerformanceController
{
    readonly Dictionary<Transform,float> guestStandingHeights = new Dictionary<Transform,float>();
    readonly List<Collider> guestBodyColliders = new List<Collider>();
    static readonly float[] RetreatAngles = { 0f, 30f, -30f, 60f, -60f, 85f, -85f };

    void PrepareGuestBodyColliders()
    {
        if (guestBodyColliders.Count != 0) return;
        foreach (var target in wineDeliveryTargets) AddGuestBodyCollider(target);
        foreach (var target in foodDeliveryTargets) AddGuestBodyCollider(target);
        AddGuestBodyCollider(groomActor);
        AddGuestBodyCollider(weddingBride);
    }

    void AddGuestBodyCollider(Transform target)
    {
        if (!target) return;
        Transform actor = GetDeliveryActorRoot(target);
        if (!actor || guestStandingHeights.ContainsKey(actor)) return;
        Animator animator = actor.GetComponentInChildren<Animator>();
        if (!animator) return;
        float height = GetVisibleRigHeight(actor);
        if (height < .5f) return;
        guestStandingHeights.Add(actor, height);
        // Start the cylinder at ankle level even on slopes. Parenting beneath the
        // Animator keeps the body out of the dancers' and guests' floor probes.
        var body = new GameObject("Wedding guest personal space");
        body.layer = 2; // Ignore Raycast
        body.transform.position = actor.position + Vector3.up * (.45f * height);
        body.transform.SetParent(animator.transform, true);
        var capsule = body.AddComponent<CapsuleCollider>();
        capsule.radius = .32f * height;
        capsule.height = .9f * height + 2f * capsule.radius;
        guestBodyColliders.Add(capsule);
    }

    void EndWeddingGuestMovement()
    {
        foreach (var body in guestBodyColliders) if (body) body.enabled = false;
    }

    IEnumerator HoldWeddingConversationPosition(Transform receiver)
    {
        var motor = characterController;
        Transform eye = GetPlayerViewTransform();
        Transform actor = GetDeliveryActorRoot(receiver);
        if (!motor || !motor.enabled || !eye || !actor) yield break;
        bool restoreInput = playerController && playerController.enabled;
        if (restoreInput) playerController.enabled = false;
        if (!guestStandingHeights.TryGetValue(actor, out float height)) height = GetVisibleRigHeight(actor);
        float separation = Mathf.Max(2f, .65f * height);
        Vector3 direction = Vector3.zero;
        Vector3 lastActor = actor.position;
        do
        {
            Vector3 offset = Vector3.ProjectOnPlane(eye.position - actor.position, Vector3.up);
            float distance = offset.magnitude;
            if (distance < separation - .03f)
            {
                if (direction == Vector3.zero || (actor.position - lastActor).sqrMagnitude > .1f)
                {
                    Vector3 outward = distance > .05f ? offset.normalized : -Vector3.ProjectOnPlane(eye.forward, Vector3.up).normalized;
                    direction = FindGuestRetreatDirection(eye, motor, actor, outward, separation, height);
                    lastActor = actor.position;
                }
                if (direction != Vector3.zero)
                {
                    Vector3 destination = actor.position + direction * separation;
                    Vector3 planar = Vector3.ProjectOnPlane(destination - eye.position, Vector3.up);
                    Vector3 step = Vector3.ClampMagnitude(planar, height * 1.6f * Mathf.Min(Time.deltaTime, .06f));
                    // Follow the actual walkable collision surface downhill. The motor
                    // prevents movement through scene props and the other guests.
                    motor.Move(step + Vector3.down * (step.magnitude + motor.skinWidth));
                }
            }
            yield return null;
        } while (physicalDeliveryAnimating || interactionAnimationRunning);
        if (restoreInput && !policeSequenceStarted && IsFreeExplorationActive()) playerController.enabled = true;
    }

    Vector3 FindGuestRetreatDirection(Transform eye, CharacterController motor, Transform actor, Vector3 outward, float separation, float height)
    {
        if (outward.sqrMagnitude < .01f) outward = Vector3.back;
        foreach (float angle in RetreatAngles)
        {
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * outward;
            Vector3 target = actor.position + direction * separation;
            Vector3 delta = Vector3.ProjectOnPlane(target - eye.position, Vector3.up);
            if (delta.sqrMagnitude < .001f || Vector3.Dot(delta, outward) < -.01f) continue;
            if (!TryGetIncidentSurfaceY(target, out float floor) ||
                Mathf.Abs(floor - motor.bounds.min.y) > motor.stepOffset + delta.magnitude * Mathf.Tan(motor.slopeLimit * Mathf.Deg2Rad)) continue;
            Vector3 lower = motor.bounds.center + Vector3.up * motor.stepOffset;
            Vector3 upper = eye.position;
            RaycastHit[] hits = Physics.CapsuleCastAll(lower, upper, Mathf.Max(.15f, motor.radius),
                delta.normalized, delta.magnitude, ~0, QueryTriggerInteraction.Ignore);
            bool blocked = false;
            foreach (var hit in hits)
            {
                if (!hit.collider || hit.collider is TerrainCollider || hit.transform.IsChildOf(actor)
                    || hit.transform.IsChildOf(motor.transform) || hit.normal.y > .7f) continue;
                blocked = true; break;
            }
            if (!blocked) return direction;
        }
        return Vector3.zero;
    }
}
