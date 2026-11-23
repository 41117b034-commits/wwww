using System.Collections;
using UnityEngine;

public partial class Chapter1PerformanceController
{
    [Header("Doorway Incident")]
    public Vector3 incidentDoorPosition = new Vector3(3686f, -5826.4f, -63f);
    public Vector3 incidentDoorOutward = Vector3.back;
    [Min(0.1f)] public float incidentDoorDragSeconds = 5.5f;
    [Min(0.1f)] public float incidentKnockoutHoldSeconds = 3f;
    private const string DoorwayProtest = "放開我！";
    private const float DoorwayReactionSeconds = .75f;
    bool doorwayStaged;
    Chapter1IncidentRig doorwayOfficer, doorwayVictim;
    Vector3 doorwayPoliceRest, doorwayVictimRest, doorwayOut, doorwayRight;
    float doorwayHeight;
    Texture2D doorwayVignette;
    float doorwayVignetteAmount;
    Mesh doorwayHandMesh;
    GameObject doorwayFallenHand;

    void OnDestroy()
    {
        if (doorwayHandMesh != null) Destroy(doorwayHandMesh);
        if (doorwayFallenHand != null) Destroy(doorwayFallenHand);
        if (doorwayVignette != null) Destroy(doorwayVignette);
    }

    void CreateDoorwayFallenHand(Vector3 cameraPosition)
    {
        if (doorwayFallenHand != null || playerHandsRoot == null || incidentCameraView == null) return;
        SkinnedMeshRenderer source = null;
        foreach (var candidate in playerHandsRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            if (candidate.name.ToLowerInvariant().Contains("hand.l")) { source = candidate; break; }
        if (source == null) return;
        doorwayHandMesh = new Mesh { name = "Incident fallen left hand" }; source.BakeMesh(doorwayHandMesh);
        Bounds bounds = doorwayHandMesh.bounds; Vector3 size = bounds.size;
        Vector3 longAxis = size.x > size.y ? (size.x > size.z ? Vector3.right : Vector3.forward) : (size.y > size.z ? Vector3.up : Vector3.forward);
        Vector3 thinAxis = size.x < size.y ? (size.x < size.z ? Vector3.right : Vector3.forward) : (size.y < size.z ? Vector3.up : Vector3.forward);
        if (longAxis == thinAxis) return;
        float length = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
        foreach (var bone in source.bones)
        {
            if (bone == null) continue; string name = bone.name.ToLowerInvariant();
            if (name.Contains("wrist") || name == "hand.l")
            { if (Vector3.Dot(bounds.center - source.transform.InverseTransformPoint(bone.position), longAxis) < 0) longAxis = -longAxis; break; }
        }
        Vector3 right = Vector3.ProjectOnPlane(incidentCameraView.right, Vector3.up).normalized;
        Vector3 forward = Vector3.ProjectOnPlane(incidentCameraView.forward, Vector3.up).normalized;
        Quaternion rotation = Quaternion.LookRotation((right * 0.75f + forward).normalized, Vector3.up) * Quaternion.Inverse(Quaternion.LookRotation(-longAxis, thinAxis));
        float scale = doorwayHeight * 0.36f / Mathf.Max(0.001f, length);
        // Keep the cut end of the first-person forearm below the frame.
        Vector3 center = cameraPosition + forward * doorwayHeight * 0.22f - right * doorwayHeight * 0.18f;
        if (TryGetIncidentSurfaceY(center, out float floor)) center.y = floor + doorwayHeight * 0.025f;
        doorwayFallenHand = new GameObject("Player fallen left hand");
        doorwayFallenHand.AddComponent<MeshFilter>().sharedMesh = doorwayHandMesh;
        doorwayFallenHand.AddComponent<MeshRenderer>().sharedMaterials = source.sharedMaterials;
        doorwayFallenHand.transform.localScale = Vector3.one * scale;
        doorwayFallenHand.transform.SetPositionAndRotation(center - rotation * (bounds.center * scale), rotation);
    }

    Chapter1IncidentRig PrepareDoorwayRig(Transform actor, bool isPolice)
    {
        var rig = actor.GetComponent<Chapter1IncidentRig>();
        if (rig == null) rig = actor.gameObject.AddComponent<Chapter1IncidentRig>();
        rig.Initialize(actor.GetComponentInChildren<Animator>(true), isPolice);
        return rig;
    }
    void PreparePoliceProportions()
    {
        float height = Mathf.Max(GetVisibleRigHeight(femaleVillagerActor), GetVisibleRigHeight(groomActor)) * 1.04f;
        foreach (Transform actor in new[] { primaryPoliceActor, secondaryPoliceActor })
        {
            if (actor == null) continue;
            actor.rotation = Quaternion.Euler(0, actor.eulerAngles.y, 0);
            Vector3 scale = actor.lossyScale;
            actor.localScale = Vector3.Scale(actor.localScale, new Vector3(scale.y / scale.x, 1f, scale.y / scale.z));
            float current = GetVisibleRigHeight(actor);
            if (current > 0.1f) actor.localScale *= height / current;
            incidentPoliceScaleAdjusted.Add(actor);
            PrepareDoorwayRig(actor, true);
        }
    }
    void PlaceDoorwayActor(Chapter1IncidentRig rig, Vector3 position)
    {
        rig.transform.position = position;
        if (TryGetIncidentSurfaceY(position, out float ground)) rig.Ground(ground);
    }
    void SetDoorwayCamera(Vector3 position, Vector3 focus, float fov, float roll = 0)
    {
        if (incidentCameraView == null) return;
        Camera camera = incidentCameraView.GetComponent<Camera>();
        if (camera != null) { camera.fieldOfView = fov; camera.rect = new Rect(0, 0, 1, 1); }
        SetCinematicCameraPose(incidentCameraView, incidentCameraPoseLock, position,
            Quaternion.LookRotation(focus - position, Vector3.up) * Quaternion.Euler(0, 0, roll));
    }
    Vector3 DoorwayTrackingPosition(Vector3 subject)
    {
        // The second officer remains on the west edge of the courtyard.
        // Track from the open south lane to see the bride and the joined wrists.
        Vector3 eye = subject + (doorwayOut * 1.68f + doorwayRight * 0.24f) * doorwayHeight;
        if (TryGetIncidentSurfaceY(eye, out float ground)) eye.y = Mathf.Max(eye.y, ground);
        return eye + Vector3.up * doorwayHeight * 1.02f;
    }

    void TurnDoorwayActor(Chapter1IncidentRig rig, Vector3 direction, float degreesPerSecond)
    {
        Quaternion from = rig.transform.rotation;
        rig.Face(direction);
        rig.transform.rotation = Quaternion.RotateTowards(from, rig.transform.rotation,
            degreesPerSecond * Time.deltaTime);
    }

    IEnumerator DoorwayIncidentRoutine()
    {
        if (incidentHutDoor != null) incidentHutDoor.SetOpen(0f);
        Transform officer = primaryPoliceActor != null ? primaryPoliceActor : secondaryPoliceActor;
        if (officer == null || femaleVillagerActor == null) { Debug.LogError("[Doorway] Missing incident actors."); yield break; }
        StopIncidentActorMotions(true);
        RestoreIncidentShotOccluders();
        doorwayOfficer = PrepareDoorwayRig(officer, true);
        doorwayVictim = PrepareDoorwayRig(femaleVillagerActor, false);
        doorwayOfficer.ClearInteractionPose();
        doorwayVictim.ClearInteractionPose();
        doorwayOfficer.smoothLocomotion = doorwayVictim.smoothLocomotion = true;
        doorwayHeight = Mathf.Max(doorwayOfficer.Height, doorwayVictim.Height);
        doorwayOut = Vector3.ProjectOnPlane(incidentDoorOutward, Vector3.up).normalized;
        doorwayRight = Vector3.Cross(Vector3.up, -doorwayOut).normalized;
        Vector3 door = incidentDoorPosition;
        if (TryGetIncidentSurfaceY(door, out float floor)) door.y = floor;
        doorwayPoliceRest = door + doorwayOut * doorwayHeight * 0.16f;
        doorwayVictimRest = doorwayPoliceRest - doorwayRight * doorwayHeight * 0.43f + doorwayOut * doorwayHeight * 0.11f;
        doorwayOfficer.batonVisible = true;
        doorwayOfficer.batonInLeftHand = true;
        doorwayOfficer.gripWithLeft = doorwayVictim.gripWithLeft = false;
        doorwayOfficer.frozen = doorwayVictim.frozen = false;
        doorwayStaged = true;
        incidentCameraView = GetPlayerViewTransform();
        if (incidentCameraPoseLock == null) incidentCameraPoseLock = BeginCinematicCameraPoseLock(incidentCameraView);

        // Keep both roots where the shove and the crowd retreat actually ended.
        // The approach stops within reach of the bride before either wrist moves.
        Vector3 brideStart = WeddingActorFloor(femaleVillagerActor);
        Vector3 officerAtBride = brideStart + (doorwayPoliceRest - doorwayVictimRest);
        var path = BuildWeddingRetreatPath(officer.position, officerAtBride,
            WatchFireObstacleCenter(), doorwayHeight * 1.2f);
        float distance = WatchPathLength(path);
        float runSeconds = Mathf.Max(0.95f, distance / (doorwayHeight * 1.38f));
        Vector3 shotStart = incidentCameraView.position;
        Quaternion shotRotation = incidentCameraView.rotation;
        Camera camera = incidentCameraView.GetComponent<Camera>();
        float initialFov = camera != null ? camera.fieldOfView : 50f;
        SetWeddingDramaBeat("police-run-to-bride");
        Vector3 firstStep = path.Find(point =>
            Vector3.ProjectOnPlane(point - officer.position, Vector3.up).sqrMagnitude > 0.01f);
        Quaternion turnFrom = officer.rotation;
        Quaternion turnTo = EntranceFacingRotation(officer, firstStep);
        float turnSeconds = Quaternion.Angle(turnFrom, turnTo) / 220f;
        for (float elapsed = 0f; elapsed < turnSeconds; elapsed += Time.deltaTime)
        {
            officer.rotation = Quaternion.Slerp(turnFrom, turnTo, Mathf.SmoothStep(0f, 1f, elapsed / turnSeconds));
            yield return null;
        }
        officer.rotation = turnTo;
        doorwayOfficer.strideScale = 1.58f;
        doorwayOfficer.running = doorwayOfficer.walking = true;
        for (float elapsed = 0f; elapsed < runSeconds; elapsed += Time.deltaTime)
        {
            float t = WatchWalkProgress(elapsed / runSeconds);
            SampleWatchPath(doorwayOfficer, path, distance * t);
            Vector3 brideFacing = officer.position - femaleVillagerActor.position + doorwayOut * (doorwayHeight * 0.44f * t);
            TurnDoorwayActor(doorwayVictim, brideFacing, 120f);
            Vector3 subject = Vector3.Lerp(WeddingActorFloor(officer), brideStart, 0.5f * t);
            Vector3 position = DoorwayTrackingPosition(subject);
            float blend = Mathf.SmoothStep(0f, 1f, elapsed / Mathf.Min(1.25f, runSeconds));
            Vector3 eye = Vector3.Lerp(shotStart, position, blend);
            Quaternion look = Quaternion.LookRotation(subject + Vector3.up * doorwayHeight * 0.57f - eye);
            SetCinematicCameraPose(incidentCameraView, incidentCameraPoseLock, eye,
                Quaternion.Slerp(shotRotation, look, blend));
            if (camera != null) camera.fieldOfView = Mathf.Lerp(initialFov, 49f, blend);
            yield return null;
        }
        PlaceDoorwayActor(doorwayOfficer, officerAtBride);
        doorwayOfficer.running = doorwayOfficer.walking = false;
        doorwayOfficer.strideScale = 1f;
        doorwayOfficer.partnerGripWeight = doorwayVictim.partnerGripWeight = 0f;
        doorwayOfficer.gripPartner = femaleVillagerActor;
        doorwayVictim.gripPartner = officer;
        SetWeddingDramaBeat("police-takes-wrist");
        Vector3 pickup = (officerAtBride + brideStart) * 0.5f;
        if (TryGetIncidentSurfaceY(pickup, out float pickupFloor)) pickup.y = pickupFloor;
        SetDoorwayCamera(DoorwayTrackingPosition(pickup), pickup + Vector3.up * doorwayHeight * 0.57f, 49f);
        const float reachSeconds = 0.75f;
        for (float elapsed = 0f; elapsed < reachSeconds; elapsed += Time.deltaTime)
        {
            TurnDoorwayActor(doorwayOfficer, brideStart - officerAtBride, 170f);
            TurnDoorwayActor(doorwayVictim, officerAtBride - brideStart + doorwayOut * (doorwayHeight * 0.44f), 120f);
            doorwayOfficer.partnerGripWeight = doorwayVictim.partnerGripWeight =
                Mathf.SmoothStep(0f, 1f, elapsed / reachSeconds);
            yield return null;
        }
        doorwayOfficer.partnerGripWeight = doorwayVictim.partnerGripWeight = 1f;
        doorwayVictim.resisting = true;
        yield return new WaitForSeconds(0.25f);

        SetWeddingDramaBeat("doorway-drag");
        Vector3 route = Vector3.ProjectOnPlane(doorwayPoliceRest - officerAtBride, Vector3.up);
        Vector3 cameraEnd = door + doorwayOut * doorwayHeight * 2.1f - doorwayRight * doorwayHeight * 1.25f
            + Vector3.up * doorwayHeight * 0.87f;
        Vector3 finalFocus = door - doorwayRight * doorwayHeight * 0.66f + Vector3.up * doorwayHeight * 0.50f;
        float dragSeconds = Mathf.Max(incidentDoorDragSeconds, route.magnitude / (doorwayHeight * 0.62f));
        doorwayOfficer.walking = doorwayVictim.walking = true;
        doorwayVictim.strideScale = 0.78f;
        for (float elapsed = 0f; elapsed < dragSeconds; elapsed += Time.deltaTime)
        {
            float t = WatchWalkProgress(elapsed / dragSeconds);
            PlaceWatchActor(doorwayOfficer, Vector3.Lerp(officerAtBride, doorwayPoliceRest, t));
            PlaceWatchActor(doorwayVictim, Vector3.Lerp(brideStart, doorwayVictimRest, t));
            TurnDoorwayActor(doorwayOfficer, route, 140f);
            TurnDoorwayActor(doorwayVictim, route.normalized + doorwayRight * 0.45f, 100f);
            Vector3 subject = (WeddingActorFloor(officer) + WeddingActorFloor(femaleVillagerActor)) * 0.5f;
            float settle = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1f, t));
            SetDoorwayCamera(Vector3.Lerp(DoorwayTrackingPosition(subject), cameraEnd, settle),
                Vector3.Lerp(subject + Vector3.up * doorwayHeight * 0.57f, finalFocus, settle), 49f);
            yield return null;
        }
        PlaceWatchActor(doorwayOfficer, doorwayPoliceRest);
        PlaceWatchActor(doorwayVictim, doorwayVictimRest);
        SetDoorwayCamera(cameraEnd, finalFocus, 49f);
        doorwayOfficer.walking = doorwayVictim.walking = false;
        Quaternion policeTurnFrom = officer.rotation, victimTurnFrom = femaleVillagerActor.rotation;
        doorwayOfficer.Face(doorwayOut + doorwayRight * 0.15f);
        doorwayVictim.Face(doorwayRight * 0.90f + doorwayOut * 0.25f);
        Quaternion policeTurnTo = officer.rotation, victimTurnTo = femaleVillagerActor.rotation;
        for (float t = 0; t < 0.65f; t += Time.deltaTime)
        {
            float blend = Mathf.SmoothStep(0, 1, t / 0.65f);
            officer.rotation = Quaternion.Slerp(policeTurnFrom, policeTurnTo, blend);
            femaleVillagerActor.rotation = Quaternion.Slerp(victimTurnFrom, victimTurnTo, blend);
            yield return null;
        }
        officer.rotation = policeTurnTo; femaleVillagerActor.rotation = victimTurnTo;
        doorwayOfficer.frozen = true;
        doorwayVictim.frozen = false;
        doorwayVictim.strugglingInPlace = true;
        doorwayOfficer.AnchorPartnerGrip();
        yield return PlayPoliceVoicedLine("新娘", DoorwayProtest, femaleResistVoice, 2.2f,
            holdForCaption: true, speakerActor: doorwayVictim.transform);
        if (dialogueUI != null) dialogueUI.HideInstant();
        if (Chapter1StorySubtitleBridge.Instance) Chapter1StorySubtitleBridge.Instance.HideImmediate();
        fallbackLine = fallbackSpeaker = "";
        fallbackLineUntil = 0f;
        // Keep her resistance and the held wrist visible for a beat before accepting input.
        yield return new WaitForSeconds(DoorwayReactionSeconds);
        ShowConflictChoice();
        SetWeddingDramaBeat("doorway-choice");
        Debug.Log("[Doorway] Choice ready; both actors held at the entrance.");
    }
    IEnumerator ResolveDoorwayChoice(ConflictChoice choice)
    {
        if (choiceUI != null) choiceUI.Hide();
        if (dialogueUI != null) dialogueUI.HideInstant();
        if (choice == ConflictChoice.Watch)
        {
            doorwayVictim.strugglingInPlace = false;
            doorwayOfficer.StationaryGrip = doorwayVictim.StationaryGrip = null;
            yield return DoorwayWatchRoutine();
            yield break;
        }

        yield return ApproachAndHelpBride();
        yield return DoorwayOfficerInterrupts();
    }

    Vector3 DoorwayEyeAt(Vector3 position, float relativeHeight)
    {
        if (TryGetIncidentSurfaceY(position, out float floor)) position.y = floor;
        position.y += doorwayHeight * relativeHeight;
        return position;
    }

    IEnumerator ApproachAndHelpBride()
    {
        // Start at the choice viewpoint. Stay on the woman's open side so the
        // officer and the hut never pass between the player and her wrist.
        SetWeddingDramaBeat("intervene-approach");
        Coroutine call = StartCoroutine(PlayPoliceVoicedLine("你", "住手！放開她！", playerInterveneVoice, 1.8f));
        Vector3 start = incidentCameraView.position;
        Quaternion rotation = incidentCameraView.rotation;
        Vector3 near = DoorwayEyeAt(doorwayVictim.transform.position
            + (doorwayOut * .54f - doorwayRight * .16f) * doorwayHeight, .85f);
        Camera camera = incidentCameraView.GetComponent<Camera>();
        float startFov = camera ? camera.fieldOfView : 49f;
        Transform wrist = doorwayVictim.gripWithLeft ? doorwayVictim.RightHand : doorwayVictim.LeftHand;
        float seconds = Mathf.Clamp(GetFlatDistance(start, near) / (doorwayHeight * .85f), 1.65f, 3.4f);
        bool headset = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.Head).isValid;
        for (float time = 0; time < seconds; time += Time.deltaTime)
        {
            float t = Mathf.SmoothStep(0f, 1f, time / seconds);
            Vector3 eye = Vector3.Lerp(start, near, t);
            if (!headset) eye.y += Mathf.Sin(t * Mathf.PI * 6f) * Mathf.Sin(t * Mathf.PI) * doorwayHeight * .007f;
            Vector3 focus = Vector3.Lerp(wrist.position, doorwayVictim.Head.position, .64f);
            SetCinematicCameraPose(incidentCameraView, incidentCameraPoseLock, eye,
                Quaternion.Slerp(rotation, Quaternion.LookRotation(focus - eye), t));
            if (camera) camera.fieldOfView = Mathf.Lerp(startFov, 60f, t);
            yield return null;
        }
        SetDoorwayCamera(near, Vector3.Lerp(wrist.position, doorwayVictim.Head.position, .64f), 60f);
        yield return call;

        SetWeddingDramaBeat("intervene-reach");
        Vector3 towardPlayer = Vector3.ProjectOnPlane(near - doorwayVictim.transform.position, Vector3.up).normalized;
        Quaternion brideFrom = doorwayVictim.transform.rotation;
        doorwayVictim.Face(towardPlayer);
        Quaternion brideTo = doorwayVictim.transform.rotation;
        doorwayVictim.transform.rotation = brideFrom;
        Vector3 wristStart = wrist.position;
        Transform offeredWrist = new GameObject("Bride offered wrist").transform;
        offeredWrist.SetParent(transform, false);
        offeredWrist.position = wristStart;
        doorwayVictim.gripTarget = offeredWrist;
        doorwayVictim.gripTargetOnFreeHand = true;

        Chapter1HelpingHand hand = null;
        if (cupHandMesh && cupHandSkin && camera)
        {
            var obj = new GameObject("Player helping right hand", typeof(MeshFilter), typeof(MeshRenderer));
            obj.GetComponent<MeshFilter>().sharedMesh = cupHandMesh;
            obj.GetComponent<MeshRenderer>().sharedMaterial = cupHandSkin;
            obj.transform.SetParent(transform, true);
            obj.transform.localScale = Vector3.one * (doorwayHeight / 1.75f);
            hand = obj.AddComponent<Chapter1HelpingHand>();
            hand.Configure(camera, wrist, doorwayHeight);
        }

        const float reachSeconds = .95f;
        for (float time = 0; time < reachSeconds; time += Time.deltaTime)
        {
            float t = Mathf.SmoothStep(0f, 1f, time / reachSeconds);
            doorwayVictim.transform.rotation = Quaternion.Slerp(brideFrom, brideTo, .36f * t);
            offeredWrist.position = Vector3.Lerp(wristStart, doorwayVictim.OfferedWristPosition(towardPlayer), t);
            if (hand) hand.reach = t;
            SetDoorwayCamera(near, Vector3.Lerp(wrist.position, doorwayVictim.Head.position, Mathf.Lerp(.64f, .54f, t)), 60f);
            yield return null;
        }
        doorwayVictim.transform.rotation = Quaternion.Slerp(brideFrom, brideTo, .36f);
        offeredWrist.position = doorwayVictim.OfferedWristPosition(towardPlayer);
        if (hand) hand.reach = 1f;

        // Her other wrist remains caught until the player has made contact.
        // She then turns and takes two steps away from the door with the player.
        SetWeddingDramaBeat("intervene-pull");
        doorwayOfficer.StationaryGrip = doorwayVictim.StationaryGrip = null;
        doorwayOfficer.gripPartner = doorwayVictim.gripPartner = null;
        doorwayVictim.strugglingInPlace = doorwayVictim.resisting = false;
        doorwayVictim.walking = true;
        Vector3 brideStart = doorwayVictim.transform.position;
        Vector3 pull = (doorwayOut * .29f - doorwayRight * .28f) * doorwayHeight;
        brideFrom = doorwayVictim.transform.rotation;
        const float pullSeconds = 1.45f;
        for (float time = 0; ; time += Time.deltaTime)
        {
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / pullSeconds));
            doorwayVictim.transform.rotation = Quaternion.Slerp(brideFrom, brideTo, t);
            PlaceDoorwayActor(doorwayVictim, brideStart + pull * t);
            offeredWrist.position = doorwayVictim.OfferedWristPosition(towardPlayer);
            Vector3 eye = DoorwayEyeAt(near + pull * t, .85f);
            SetDoorwayCamera(eye, Vector3.Lerp(wrist.position, doorwayVictim.Head.position, .54f), 60f);
            if (t >= 1f) break;
            yield return null;
        }
        doorwayVictim.walking = false;
        for (float time = 0; time < .45f; time += Time.deltaTime)
        {
            float t = Mathf.SmoothStep(0f, 1f, time / .45f);
            if (hand) hand.reach = 1f - t;
            offeredWrist.position += Vector3.down * doorwayHeight * (.24f * Time.deltaTime);
            yield return null;
        }
        doorwayVictim.gripTarget = null;
        doorwayVictim.gripTargetOnFreeHand = false;
        Destroy(offeredWrist.gameObject);
        if (hand) { hand.gameObject.SetActive(false); Destroy(hand.gameObject); }
    }

    IEnumerator DoorwayOfficerInterrupts()
    {
        SetWeddingDramaBeat("intervene-officer-turn");
        Vector3 eye = incidentCameraView.position;
        Vector3 officerStart = doorwayOfficer.transform.position;
        Vector3 towardPlayer = Vector3.ProjectOnPlane(eye - officerStart, Vector3.up).normalized;
        Vector3 officerEnd = eye - towardPlayer * doorwayHeight * .86f;
        Quaternion from = doorwayOfficer.transform.rotation;
        doorwayOfficer.frozen = false;
        doorwayOfficer.Face(towardPlayer);
        Quaternion facing = doorwayOfficer.transform.rotation;
        doorwayOfficer.transform.rotation = from;
        Quaternion cameraFrom = incidentCameraView.rotation;
        Camera camera = incidentCameraView.GetComponent<Camera>();
        float seconds = Mathf.Max(1.2f, policeApproachPlayerSeconds);
        doorwayOfficer.walking = true;
        for (float time = 0; ; time += Time.deltaTime)
        {
            float t = Mathf.Clamp01(time / seconds);
            doorwayOfficer.transform.rotation = Quaternion.Slerp(from, facing, Mathf.SmoothStep(0, 1, t / .48f));
            PlaceDoorwayActor(doorwayOfficer, Vector3.Lerp(officerStart, officerEnd, Mathf.SmoothStep(0f, 1f, t)));
            Vector3 focus = doorwayOfficer.Head.position + Vector3.up * doorwayHeight * .07f;
            if (camera) camera.fieldOfView = Mathf.Lerp(60f, 66f, t);
            Quaternion look = Quaternion.LookRotation(focus - eye);
            SetCinematicCameraPose(incidentCameraView, incidentCameraPoseLock, eye,
                Quaternion.Slerp(cameraFrom, look, Mathf.SmoothStep(0f, 1f, t / .72f)));
            if (t >= 1f) break;
            yield return null;
        }
        doorwayOfficer.walking = false;
        SetWeddingDramaBeat("intervene-baton");
        doorwayOfficer.strike = true;
        doorwayOfficer.strikeProgress = 0;
        const float swingSeconds = 1.25f;
        for (float time = 0; time < swingSeconds * .72f; time += Time.deltaTime)
        {
            float progress = time / swingSeconds;
            doorwayOfficer.strikeProgress = progress;
            float step = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.42f, .72f, progress));
            PlaceDoorwayActor(doorwayOfficer, officerEnd + towardPlayer * doorwayHeight * (.26f * step));
            SetDoorwayCamera(eye, doorwayOfficer.Head.position + Vector3.up * doorwayHeight * Mathf.Lerp(.07f, -.04f, step), 66f);
            yield return null;
        }
        doorwayOfficer.strikeProgress = .72f;
        PlaceDoorwayActor(doorwayOfficer, officerEnd + towardPlayer * doorwayHeight * .26f);
        PlayPoliceEventClip(struggleClip);
        SetWeddingDramaBeat("intervene-impact");
        yield return null;

        Vector3 down = DoorwayEyeAt(eye + towardPlayer * doorwayHeight * .33f, .065f);
        Vector3 focusDown = Vector3.Lerp(doorwayOfficer.Head.position, doorwayVictim.Head.position, .45f)
            - Vector3.up * doorwayHeight * .26f;
        bool headset = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.Head).isValid;
        Quaternion upright = incidentCameraView.rotation;
        Quaternion downRotation = Quaternion.LookRotation(focusDown - down)
            * Quaternion.Euler(0f, 0f, headset ? 0f : 13f);
        const float fallSeconds = 1.05f;
        for (float time = 0; ; time += Time.deltaTime)
        {
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / fallSeconds));
            doorwayOfficer.strikeProgress = Mathf.Min(1f, .72f + time / swingSeconds);
            doorwayVignetteAmount = t * .92f;
            SetCinematicCameraPose(incidentCameraView, incidentCameraPoseLock,
                Vector3.Lerp(eye, down, t), Quaternion.Slerp(upright, downRotation, t));
            if (t >= 1f) break;
            yield return null;
        }
        doorwayOfficer.strike = false;
        CreateDoorwayFallenHand(down);
        knockoutCameraPosition = down;
        knockoutCameraRotation = downRotation;
        playerKnockedOut = true;
        knockoutDizzyVisible = false;
        SetWeddingDramaBeat("intervene-knockout");
        if (dialogueUI != null) dialogueUI.HideInstant();
        yield return new WaitForSecondsRealtime(incidentKnockoutHoldSeconds);
        chapterCompleted = true;
        cinematicStoryPlaying = false;
        SaveChapterResult();
        StopChapterPlayMode();
    }
    void DrawDoorwayHud()
    {
        // The staged doorway owns OnGUI through both ending branches. Keep
        // narration visible when this scene uses the fallback subtitle HUD.
        if (!waitingForChoice && showFallbackHud && dialogueUI == null && GameLanguageSettings.SubtitlesEnabled)
        {
            EnsureHudStyles();
            DrawFallbackDialogue();
        }
        if (doorwayVignetteAmount > 0)
        {
            if (doorwayVignette == null)
            {
                doorwayVignette = new Texture2D(128, 128, TextureFormat.RGBA32, false); doorwayVignette.wrapMode = TextureWrapMode.Clamp;
                for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++)
                    { float r = new Vector2((x - 63.5f) / 63.5f, (y - 63.5f) / 63.5f).magnitude; doorwayVignette.SetPixel(x, y, new Color(0, 0, 0, Mathf.SmoothStep(0, 0.94f, Mathf.InverseLerp(0.42f, 1.32f, r)))); }
                doorwayVignette.Apply();
            }
            Color previous = GUI.color; GUI.color = new Color(1, 1, 1, doorwayVignetteAmount); GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), doorwayVignette); GUI.color = previous;
        }
        if (!waitingForChoice || choiceUI) return;
        EnsureHudStyles();
        float scale = Mathf.Clamp(Screen.height / 900f, 0.6f, 1.5f);
        // Older scenes without a canvas choice panel use the same empty side of the shot.
        var titleStyle = new GUIStyle(hudTitleStyle)
        {
            fontSize = Mathf.RoundToInt(30 * scale),
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            wordWrap = false
        };
        var buttonStyle = new GUIStyle(hudButtonStyle)
        {
            fontSize = Mathf.RoundToInt(24 * scale),
            alignment = TextAnchor.MiddleCenter,
            wordWrap = false
        };

        float boxWidth = Mathf.Min(Screen.width * 0.33f, 330f * scale);
        float boxHeight = 238f * scale;
        Rect box = new Rect(
            Screen.width * 0.035f,
            (Screen.height - boxHeight) * 0.5f,
            boxWidth,
            boxHeight);

        GUI.Box(box, GUIContent.none, hudBoxStyle);

        GUI.Label(
            new Rect(
                box.x + 24 * scale,
                box.y + 18 * scale,
                box.width - 48 * scale,
                46 * scale),
            "你要怎麼做？",
            titleStyle);

        if (GUI.Button(
            new Rect(
                box.x + 26 * scale,
                box.y + 78 * scale,
                box.width - 52 * scale,
                58 * scale),
            "1 / A　上前阻止",
            buttonStyle))
        {
            PlayChapterUiClick();
            ChooseIntervene();
        }

        if (GUI.Button(
            new Rect(
                box.x + 26 * scale,
                box.y + 152 * scale,
                box.width - 52 * scale,
                58 * scale),
            "2 / B　沉默觀望",
            buttonStyle))
        {
            PlayChapterUiClick();
            ChooseWatch();
        }
    }
}

[DefaultExecutionOrder(4500)]
public sealed class Chapter1HelpingHand : MonoBehaviour
{
    public float reach;
    private Camera view;
    private Transform wrist;
    private Transform shoulder;
    private float height;

    public void Configure(Camera camera, Transform targetWrist, float actorHeight)
    {
        view = camera;
        wrist = targetWrist;
        height = actorHeight;
        shoulder = new GameObject("Helping arm shoulder").transform;
        shoulder.SetParent(view.transform, false);
        var arm = gameObject.AddComponent<Chapter3ViewArm>();
        arm.view = view;
        arm.ShoulderAnchor = shoulder;
    }

    // The bride solves her wrist at order 4000. Follow only its position,
    // then aim the player's forearm back toward the shoulder outside the frame.
    void LateUpdate()
    {
        if (!view || !wrist) return;
        Vector3 lowered = view.ViewportToWorldPoint(new Vector3(1.03f, -.24f, height * .26f));
        Vector3 palm = Vector3.Lerp(lowered, wrist.position, reach);
        float depth = Mathf.Max(view.nearClipPlane * 2f, view.WorldToViewportPoint(palm).z * .62f);
        shoulder.position = view.ViewportToWorldPoint(new Vector3(1.24f, -.30f, depth));
        transform.SetPositionAndRotation(palm, Quaternion.LookRotation(palm - shoulder.position, Vector3.up));
    }

    void OnDestroy()
    {
        if (shoulder) Destroy(shoulder.gameObject);
    }
}
