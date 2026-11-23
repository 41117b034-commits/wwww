using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Chapter1PerformanceController
{
    readonly Dictionary<Transform,float> guestStandingHeights = new Dictionary<Transform,float>();
    readonly List<Collider> guestBodyColliders = new List<Collider>();
    static readonly float[] RetreatAngles = { 0f, 30f, -30f, 60f, -60f, 90f, -90f };

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
        do
        {
            Vector3 offset = Vector3.ProjectOnPlane(eye.position - actor.position, Vector3.up);
            float distance = offset.magnitude;
            if (distance < separation - .03f)
            {
                Vector3 outward = distance > .05f ? offset.normalized : -Vector3.ProjectOnPlane(eye.forward, Vector3.up).normalized;
                float step = Mathf.Min(separation - distance, height * 1.6f * Mathf.Min(Time.deltaTime, .06f));
                RetreatFromWeddingGuest(eye, motor, outward, step);
            }
            yield return null;
        } while (physicalDeliveryAnimating || interactionAnimationRunning);
        if (restoreInput && !policeSequenceStarted && IsFreeExplorationActive()) playerController.enabled = true;
    }

    void RetreatFromWeddingGuest(Transform eye, CharacterController motor, Vector3 outward, float step)
    {
        if (step <= 0f) return;
        if (outward.sqrMagnitude < .01f) outward = Vector3.back;
        Vector3 start = motor.transform.position;
        foreach (float angle in RetreatAngles)
        {
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * outward;
            // Test the next footstep. A table at the far end of a retreat must
            // not prevent taking the first step or sliding sideways around it.
            Vector3 destination = eye.position + direction * step;
            if (!TryGetIncidentSurfaceY(destination, out float floor) ||
                floor < motor.bounds.min.y - motor.stepOffset - step * Mathf.Tan(motor.slopeLimit * Mathf.Deg2Rad)) continue;
            if (Physics.SphereCast(eye.position, .12f, direction, out RaycastHit headHit, step, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore) && !headHit.transform.IsChildOf(motor.transform)) continue;
            motor.Move(direction * step);
            Vector3 moved = Vector3.ProjectOnPlane(motor.transform.position - start, Vector3.up);
            if (Vector3.Dot(moved, direction) > step * .15f) break;
        }
        motor.Move(Vector3.down * (step + motor.skinWidth));
    }
}
