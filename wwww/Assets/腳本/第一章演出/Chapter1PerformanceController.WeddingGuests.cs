using System.Collections;
using UnityEngine;
using UnityEngine.XR;

public partial class Chapter1PerformanceController
{
    [Header("第一杯喜酒")]
    [SerializeField] private Transform weddingElder;
    [SerializeField] private GameObject bambooCupModel;
    [SerializeField] private GameObject cupStandModel;
    [SerializeField] private Material cupStandMaterial;
    [SerializeField] private Mesh cupHandMesh;
    [SerializeField] private Material cupHandSkin;
    private GameObject playerCupHand;
    private const float PlayerCupHeight = .19f;
    private const float CupGripHeight = PlayerCupHeight * .46f;

    private const string ElderName = "火堆旁的長輩";
    private const string GroomRequest = "來，幫我把這杯酒送給火堆旁的長輩，好嗎？";
    private const string ElderInvitation = "謝謝你。這裡給你留個位子，等一下來跟我們跳。";
    private Chapter1WeddingGuestPose groomPose, elderPose;
    private Vector3 cupRestPoint;
    private float greetingUntil;
    private bool weddingPrimaryPressed, weddingReturnPressed;
    private bool FirstCupPending => weddingElder && !deliveredWineTargets.Contains(weddingElder.GetInstanceID());
    private bool WeddingActionPlaying => interactionAnimationRunning || physicalDeliveryAnimating || danceRoutineRunning;
    private bool WeddingGuidanceQuiet => Time.time < greetingUntil || WeddingActionPlaying || policeSequenceStarted;

    private enum WeddingAction { None, TakeWine, TakeFood, GiveWine, GiveFood, JoinDance }

    private bool IsWithinWeddingCircle() => GetFlatDistance(GetCurrentPlayerPosition(), GetFireCenterPosition())
        <= Mathf.Max(centerInteractionRange, weddingCircleRadius + physicalPickupRange);

    // Both the key press and the heading use the distance to the accessible surface of the prop.
    private WeddingAction NearbyWeddingAction()
    {
        if (!IsFreeExplorationActive() || physicalDeliveryAnimating || interactionAnimationRunning || danceRoutineRunning)
            return WeddingAction.None;
        Vector3 player = GetCurrentPlayerPosition();
        if (carriedWeddingItem != WeddingCarryItem.None)
        {
            bool wine = carriedWeddingItem == WeddingCarryItem.Wine;
            Transform receiver = GetDesignatedPhysicalDeliveryTarget(wine);
            if (receiver && GetFlatDistance(player, receiver.position) <= Mathf.Max(.5f, physicalDeliveryRange))
                return wine ? WeddingAction.GiveWine : WeddingAction.GiveFood;
            return WeddingAction.None;
        }
        if (AreWineAndFoodTasksComplete()) return CanUseDanceInteraction() ? WeddingAction.JoinDance : WeddingAction.None;
        if (!weddingPickupPropsReady) return WeddingAction.None;
        float wineDistance = deliveredWineCount < Mathf.Max(1, wineTargetCount)
            ? GetPhysicalPickupDistance(ResolvePhysicalPickupPoint(true), player) : float.PositiveInfinity;
        float foodDistance = sharedFoodCount < Mathf.Max(1, foodTargetCount)
            ? GetPhysicalPickupDistance(ResolvePhysicalPickupPoint(false), player) : float.PositiveInfinity;
        if (Mathf.Min(wineDistance, foodDistance) > Mathf.Max(.5f, physicalPickupRange)) return WeddingAction.None;
        return wineDistance <= foodDistance ? WeddingAction.TakeWine : WeddingAction.TakeFood;
    }

    private void StageWeddingHosts()
    {
        Transform view = GetPlayerViewTransform();
        if (!weddingElder || !groomActor || !view || groomPose) return;
        Animator groomAnimator = groomActor.GetComponentInChildren<Animator>();
        Animator elderAnimator = weddingElder.GetComponentInChildren<Animator>();
        if (!groomAnimator || !elderAnimator) return;
        if (!elderAnimator.runtimeAnimatorController) elderAnimator.runtimeAnimatorController = groomAnimator.runtimeAnimatorController;
        PlayAnimatorStateIfAvailable(groomActor, deliveryTargetIdleStateName);
        PlayAnimatorStateIfAvailable(weddingElder, deliveryTargetIdleStateName);
        groomAnimator.Update(0f);
        elderAnimator.Update(0f);
        groomPose = groomActor.gameObject.AddComponent<Chapter1WeddingGuestPose>();
        elderPose = weddingElder.gameObject.AddComponent<Chapter1WeddingGuestPose>();

        // Use the authored entrance direction even if the headset is turned during loading.
        Transform entrance = playerRoot ? playerRoot : view;
        Vector3 ahead = Vector3.ProjectOnPlane(entrance.forward, Vector3.up).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, ahead);
        Vector3 place = entrance.position + (ahead * 1.45f + right * .19f) * groomPose.Height;
        place.y = groomActor.position.y;
        groomActor.position = place;
        groomActor.rotation = GetReceiverRotationFacingPosition(groomActor, view.position);
        var look = groomActor.GetComponent<NPCNaturalLookAt>();
        if (look) look.enabled = false;
        EnsureDeliveryTargetGrounding(groomActor, new[] { groomAnimator });
        groomPose.Face(view.position - groomActor.position);

        weddingElder.rotation = GetReceiverRotationFacingPosition(weddingElder, view.position);
        elderPose.Face(view.position - weddingElder.position);
        RememberElderPlace();
        if (!cupStandModel) return;
        float height = elderPose.Height;
        GameObject stand = CreateWeddingProp(cupStandModel, cupStandMaterial, "WeddingCupTable", height * .49f);
        Vector3 outward = Vector3.ProjectOnPlane(weddingElder.position - GetFireCenterPosition(), Vector3.up).normalized;
        Vector3 basePoint = weddingElder.position + outward * (.36f * height);
        if (TryGetIncidentSurfaceY(basePoint, out float floor)) basePoint.y = floor;
        stand.transform.SetPositionAndRotation(basePoint, Quaternion.LookRotation(outward));
        // The middle board has a flat top. Dancers and ground probes should only encounter the floor.
        cupRestPoint = basePoint + Vector3.up * (height * .49f);
    }

    private GameObject CreatePlayerCup()
    {
        Renderer reference = ceremonyCup ? ceremonyCup.GetComponentInChildren<Renderer>() : null;
        GameObject cup = CreateWeddingProp(bambooCupModel, reference ? reference.sharedMaterial : null, "WeddingBambooCup", PlayerCupHeight);
        if (cupHandMesh && cupHandSkin)
        {
            playerCupHand = new GameObject("握竹杯的右手", typeof(MeshFilter), typeof(MeshRenderer));
            var hand = playerCupHand.transform;
            hand.SetParent(cup.transform, false);
            hand.localPosition = Vector3.up * CupGripHeight;
            hand.localRotation = Quaternion.Euler(0f, -32f, 0f);
            playerCupHand.GetComponent<MeshFilter>().sharedMesh = cupHandMesh;
            playerCupHand.GetComponent<MeshRenderer>().sharedMaterial = cupHandSkin;
        }
        return cup;
    }

    private void PositionPlayerCup(Transform view)
    {
        Vector3 grip = view.position + view.forward * .76f + view.right * .345f - view.up * .28f;
        Quaternion rotation = Quaternion.Euler(0f, view.eulerAngles.y, 0f);
        var head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        var right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (view.parent && right.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 handPosition)
            && right.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion handRotation)
            && head.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 headPosition))
        {
            grip = view.position + view.parent.TransformVector(handPosition - headPosition);
            rotation = view.parent.rotation * handRotation;
        }
        physicalCarriedProp.transform.SetPositionAndRotation(grip - rotation * (Vector3.up * CupGripHeight), rotation);
    }

    private GameObject CreateWeddingProp(GameObject model, Material finish, string propName, float height)
    {
        GameObject root = new GameObject(propName);
        Transform visual = Instantiate(model, root.transform, false).transform;
        Renderer[] parts = visual.GetComponentsInChildren<Renderer>();
        Bounds bounds = parts[0].bounds;
        foreach (Renderer part in parts)
        {
            bounds.Encapsulate(part.bounds);
            if (finish) part.sharedMaterial = finish;
        }
        float scale = height / Mathf.Max(.001f, bounds.size.y);
        Vector3 bottom = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        visual.localPosition = (visual.position - bottom) * scale;
        visual.localScale *= scale;
        return root;
    }

    private void InvitePlayerToHelp()
    {
        ShowLine("新郎", GroomRequest, 4.4f);
        greetingUntil = Time.time + (chineseSpeech && chineseSpeech.HasAudio ? chineseSpeech.Remaining : 4.4f);
        if (groomPose) groomPose.Beckon();
    }

    private IEnumerator MakeRoomForPlayer()
    {
        yield return new WaitForSeconds(receiverDialoguePanSeconds + .2f);
        elderPose.Beckon();
        yield return StepBesideCupStand();
    }

    private IEnumerator StepBesideCupStand()
    {
        Vector3 remaining = Vector3.ProjectOnPlane(cupRestPoint - weddingElder.position, Vector3.up);
        float distance = remaining.magnitude - elderPose.Height * .22f;
        if (!cupStandModel || distance < elderPose.Height * .008f) yield break;
        yield return elderPose.StepAside(remaining.normalized * distance);
        RememberElderPlace();
    }

    private void RememberElderPlace()
    {
        if (deliveryNpcAuthoredTransforms.TryGetValue(weddingElder.GetInstanceID(), out var place))
        {
            place.worldPosition = weddingElder.position;
            place.worldRotation = weddingElder.rotation;
        }
    }

    private IEnumerator ElderSetsDownCup()
    {
        if (!elderPose || !elderPose.Cup || !cupStandModel) yield break;
        SetWeddingDramaBeat("elder-sets-down-cup");
        float height = elderPose.Height;
        Vector3 towardStand = Vector3.ProjectOnPlane(cupRestPoint - weddingElder.position, Vector3.up).normalized;
        Vector3 facing = Quaternion.AngleAxis(-35f, Vector3.up) * towardStand;
        Vector3 focus = Vector3.Lerp(elderPose.Head.position, cupRestPoint, .47f);
        Vector3 camera = focus + (facing * 1.30f + Vector3.Cross(Vector3.up, facing) * .38f + Vector3.up * .12f) * height;
        SetDoorwayCamera(camera, focus, 43f);
        Quaternion from = weddingElder.rotation;
        Quaternion to = GetReceiverRotationFacingPosition(weddingElder, weddingElder.position + facing);
        for (float t = 0f; t < .42f; t += Time.deltaTime)
        {
            weddingElder.rotation = Quaternion.Slerp(from, to, Mathf.SmoothStep(0f, 1f, t / .42f));
            yield return null;
        }
        weddingElder.rotation = to;
        elderPose.Face(facing);
        yield return elderPose.RestCupOn(cupRestPoint);
        yield return new WaitForSeconds(.28f);
    }
}
