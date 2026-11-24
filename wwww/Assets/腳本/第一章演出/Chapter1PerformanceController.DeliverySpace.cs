using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

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
        Transform head = Chapter1WeddingRigBones.Resolve(actor.GetComponentInChildren<Animator>(), HumanBodyBones.Head, "Head");
        var desktop = InputDevices.GetDeviceAtXRNode(XRNode.Head).isValid ? null : playerController as PCPlayerController;
        Vector3 offset = Vector3.ProjectOnPlane(eye.position - actor.position, Vector3.up);
        Vector3 outward = offset.sqrMagnitude > .01f ? offset.normalized : -GetReceiverVisualForward(actor);
        float distance = Mathf.Max(offset.magnitude, 2f, .76f * height);
        // Stand on the approach side when it is open. Beside a second guest,
        // use the nearest clear angle from which the speaker's face is visible.
        foreach (float angle in RetreatAngles)
        {
            Vector3 bearing = Quaternion.AngleAxis(angle, Vector3.up) * outward;
            Vector3 spot = actor.position + bearing * distance;
            if (!WeddingGuestBlocksView(spot, actor) && TryGetIncidentSurfaceY(spot, out _))
            {
                outward = bearing;
                break;
            }
        }
        Vector3 standingPoint = actor.position + outward * distance;
        do
        {
            Vector3 toSpot = Vector3.ProjectOnPlane(standingPoint - eye.position, Vector3.up);
            if (toSpot.sqrMagnitude > .035f * .035f)
                RetreatFromWeddingGuest(eye, motor, toSpot.normalized, Mathf.Min(toSpot.magnitude, height * 1.6f * Mathf.Min(Time.deltaTime, .06f)));
            if (desktop)
            {
                Vector3 focus = head ? head.position - Vector3.up * (.09f * height) : actor.position + Vector3.up * (.78f * height);
                Vector3 look = Vector3.RotateTowards(eye.forward, (focus - eye.position).normalized, 155f * Mathf.Deg2Rad * Time.deltaTime, 0f);
                float yaw = Vector3.SignedAngle(Vector3.ProjectOnPlane(eye.forward, Vector3.up), Vector3.ProjectOnPlane(look, Vector3.up), Vector3.up);
                desktop.transform.Rotate(Vector3.up, yaw, Space.World);
                // Set the movement controller's own pitch so its first free frame keeps this view.
                desktop.SetInitialLookAt(eye.position + look);
            }
            yield return null;
        } while (physicalDeliveryAnimating || interactionAnimationRunning);
        if (restoreInput && !policeSequenceStarted && IsFreeExplorationActive()) playerController.enabled = true;
    }

    bool WeddingGuestBlocksView(Vector3 spot, Transform speaker)
    {
        Vector3 sight = Vector3.ProjectOnPlane(speaker.position - spot, Vector3.up);
        float length = sight.magnitude;
        if (length < .01f) return false;
        Vector3 direction = sight / length;
        foreach (var guest in guestStandingHeights)
        {
            if (guest.Key == speaker || !guest.Key || !guest.Key.gameObject.activeInHierarchy) continue;
            Vector3 delta = Vector3.ProjectOnPlane(guest.Key.position - spot, Vector3.up);
            // A shoulder beside the eye can fill the edge of the picture even
            // when the ray to the speaker's face passes the other guest.
            float nearClearance = guest.Value * .51f + characterController.radius;
            if (delta.sqrMagnitude < nearClearance * nearClearance) return true;
            float along = Vector3.Dot(delta, direction);
            if (along > length) continue;
            float clearance = guest.Value * .25f + characterController.radius;
            if ((delta - direction * Mathf.Max(0f, along)).sqrMagnitude < clearance * clearance) return true;
        }
        return false;
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
