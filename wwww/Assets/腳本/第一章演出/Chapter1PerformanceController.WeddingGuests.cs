using System.Collections;
using UnityEngine;

public partial class Chapter1PerformanceController
{
    [Header("第一杯喜酒")]
    [SerializeField] private Transform weddingElder;
    [SerializeField] private GameObject bambooCupModel;
    [SerializeField] private GameObject cupStandModel;
    [SerializeField] private Material cupStandMaterial;

    private const string ElderName = "火堆旁的長輩";
    private const string GroomRequest = "來，幫我把這杯酒送給火堆旁的長輩，好嗎？";
    private const string ElderInvitation = "謝謝你。這裡給你留個位子，等一下來跟我們跳。";
    private Chapter1WeddingGuestPose groomPose, elderPose;
    private Vector3 cupRestPoint;
    private float greetingUntil;
    private bool weddingPrimaryPressed, weddingReturnPressed;
    private bool FirstCupPending => weddingElder && !deliveredWineTargets.Contains(weddingElder.GetInstanceID());
    private bool WeddingGuidanceQuiet => Time.time < greetingUntil || interactionAnimationRunning
        || physicalDeliveryAnimating || danceRoutineRunning || policeSequenceStarted;

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
        GameObject stand = CreateWeddingProp(cupStandModel, cupStandMaterial, "WeddingCupStand", height * .51f);
        Vector3 basePoint = weddingElder.position + (elderPose.Right * .31f + elderPose.Forward * .14f) * height;
        if (TryGetIncidentSurfaceY(basePoint, out float floor)) basePoint.y = floor;
        stand.transform.position = basePoint;
        cupRestPoint = basePoint + Vector3.up * (height * .51f);
        // The top of the scanned stump is uneven. Rest the cup on the wood under its centre.
        foreach (MeshFilter mesh in stand.GetComponentsInChildren<MeshFilter>())
        {
            if (!mesh.sharedMesh) continue;
            var surface = mesh.gameObject.AddComponent<MeshCollider>();
            surface.sharedMesh = mesh.sharedMesh;
            Physics.SyncTransforms();
            if (surface.Raycast(new Ray(cupRestPoint + Vector3.up * height, Vector3.down), out RaycastHit hit, height * 2f))
            {
                cupRestPoint = hit.point;
                break;
            }
        }
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
