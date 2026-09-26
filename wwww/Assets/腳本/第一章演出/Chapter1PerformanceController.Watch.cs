using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Chapter1PerformanceController
{
    [Header("Silent Watch Continuation")]
    public Chapter1HutDoor incidentHutDoor;
    [Min(0.1f)] public float incidentDoorSwingSeconds = 0.85f;
    [Min(1f)] public float incidentEnterHutSeconds = 3.6f;
    [Min(1f)] public float incidentDepartureHoldSeconds = 6f;
    AudioSource hutInteriorAudio;
    readonly Dictionary<Renderer, bool> hutHiddenRenderers = new Dictionary<Renderer, bool>();
    bool doorwayWatchActive;
    float watchTensionVolume;
    Coroutine watchCrowdAdvance;

    IEnumerator DoorwayWatchRoutine()
    {
        if (incidentHutDoor == null)
            incidentHutDoor = FindFirstObjectByType<Chapter1HutDoor>();
        if (incidentHutDoor == null || incidentHutDoor.hinge == null)
        {
            Debug.LogError("[Doorway Watch] Missing authored hut door. Run Upgrade Watch Branch Door.");
            yield break;
        }
        doorwayWatchActive = true;
        cinematicStoryPlaying = true;
        SetPlayerControl(false);
        SetMission("你選擇沉默觀望。");
        if (tensionAmbience != null)
        {
            watchTensionVolume = tensionAmbience.volume;
            tensionAmbience.volume *= 0.3f;
        }
        PrepareHutInteriorAudio();
        watchCrowdAdvance = StartCoroutine(AdvanceWatchWitnesses());
        doorwayOfficer.frozen = doorwayVictim.frozen = false;
        doorwayOfficer.smoothLocomotion = doorwayVictim.smoothLocomotion = true;
        doorwayOfficer.pointTarget = doorwayVictim.pointTarget = null;
        doorwayOfficer.gripPartner = doorwayVictim.transform;
        doorwayVictim.gripPartner = doorwayOfficer.transform;
        doorwayOfficer.gripWithLeft = true;
        doorwayVictim.gripWithLeft = false;
        doorwayOfficer.batonInLeftHand = false;
        doorwayVictim.resisting = true;
        Vector3 threshold = incidentHutDoor.Threshold;
        Vector3 insideOfficer = threshold - doorwayOut * doorwayHeight * 0.50f + doorwayRight * doorwayHeight * 0.18f;
        Vector3 insideVictim = threshold - doorwayOut * doorwayHeight * 0.35f - doorwayRight * doorwayHeight * 0.18f;

        SetWeddingDramaBeat("watch-door-opening");
        yield return SwingWatchDoor(1f);
        SetWeddingDramaBeat("watch-entering-hut");
        yield return MoveWatchPair(insideOfficer, insideVictim, incidentEnterHutSeconds, -doorwayOut);
        SetWeddingDramaBeat("watch-door-closing");
        yield return SwingWatchDoor(0f);
        // Hide only behind the fully closed, opaque door. No interior action is
        // staged, rendered, or shown from another camera.
        SetHutPairVisible(false);
        doorwayOfficer.batonVisible = false;
        doorwayOfficer.frozen = doorwayVictim.frozen = true;
        SetWeddingDramaBeat("watch-door-closed");
        float closedAt = Time.time;
        float hold = Mathf.Max(0.1f, hutInteriorHoldSeconds);
        float screamDelay = Mathf.Clamp(hutScreamDelaySeconds, 0f, Mathf.Max(0f, hold - 0.1f));
        bool screamed = false;
        while (Time.time - closedAt < hold)
        {
            if (!screamed && Time.time - closedAt >= screamDelay)
            {
                screamed = true;
                if (hutInteriorAudio.clip != null) hutInteriorAudio.Play();
                ShowLine("", "（屋內傳來女性的慘叫聲）", Mathf.Min(3.2f, hold - screamDelay));
                SetWeddingDramaBeat("watch-hut-scream");
            }
            yield return null;
        }
        hutInteriorAudio.Stop();
        if (dialogueUI != null) dialogueUI.HideInstant();
        Debug.Log($"[Doorway Watch] Closed-door hold: {Time.time - closedAt:F3} seconds.");
        // Restore the same positions behind the still-closed door before opening.
        SetHutPairVisible(true);
        doorwayOfficer.batonVisible = true;
        doorwayOfficer.frozen = doorwayVictim.frozen = false;
        doorwayOfficer.Face(doorwayOut);
        doorwayVictim.Face(doorwayOut);
        doorwayOfficer.gripWithLeft = false;
        doorwayVictim.gripWithLeft = true;
        doorwayOfficer.batonInLeftHand = true;
        SetWeddingDramaBeat("watch-door-reopening");
        yield return SwingWatchDoor(1f);
        SetWeddingDramaBeat("watch-dragging-out");
        Vector3 outsideOfficer = threshold + doorwayOut * doorwayHeight * 1.0f + doorwayRight * doorwayHeight * 0.19f;
        Vector3 outsideVictim = threshold + doorwayOut * doorwayHeight * 0.80f - doorwayRight * doorwayHeight * 0.19f;
        yield return MoveWatchPair(outsideOfficer, outsideVictim, Mathf.Max(3.2f, dragVictimBackOutSeconds), doorwayOut);
        doorwayOfficer.gripPartner = doorwayVictim.gripPartner = null;
        doorwayVictim.resisting = false;
        doorwayVictim.walking = false;
        SetWeddingDramaBeat("watch-woman-released");
        yield return new WaitForSeconds(0.9f);
        yield return WatchPoliceDeparture();
        morale -= 1;
        peopleInjured += 1;
        SaveChapterResult();
        chapterCompleted = true;
        cinematicStoryPlaying = false;
        SetMission("族人默默望著警察離去的背影。");
        SetWeddingDramaBeat("watch-complete");
        CleanupDoorwayWatch();
        // Keep the final composition intact until the configured chapter exit.
        // With automatic exit disabled it remains available for the next chapter.
        if (stopPlayModeAfterChapterEnding) StopChapterPlayMode();
    }

    void PrepareHutInteriorAudio()
    {
        if (hutInteriorAudio == null)
        {
            var sound = new GameObject("Hut interior scream");
            sound.transform.SetParent(transform, false);
            hutInteriorAudio = sound.AddComponent<AudioSource>();
            var filter = sound.AddComponent<AudioLowPassFilter>();
            filter.cutoffFrequency = 1700f;
        }
        hutInteriorAudio.transform.position = incidentHutDoor.Threshold - doorwayOut * doorwayHeight * 0.4f + Vector3.up * doorwayHeight * 0.65f;
        hutInteriorAudio.playOnAwake = false;
        hutInteriorAudio.loop = false;
        hutInteriorAudio.spatialBlend = 0.8f;
        hutInteriorAudio.rolloffMode = AudioRolloffMode.Linear;
        hutInteriorAudio.minDistance = doorwayHeight * 3f;
        hutInteriorAudio.maxDistance = doorwayHeight * 15f;
        hutInteriorAudio.volume = 0.9f;
        hutInteriorAudio.clip = GetHutCryClip();
        if (hutInteriorAudio.clip != null) hutInteriorAudio.clip.LoadAudioData();
    }

    IEnumerator SwingWatchDoor(float target)
    {
        float from = incidentHutDoor.OpenAmount;
        float seconds = Mathf.Max(0.1f, incidentDoorSwingSeconds);
        for (float elapsed = 0f; elapsed < seconds; elapsed += Time.deltaTime)
        {
            incidentHutDoor.SetOpen(Mathf.Lerp(from, target, Mathf.SmoothStep(0f, 1f, elapsed / seconds)));
            yield return null;
        }
        incidentHutDoor.SetOpen(target);
    }

    IEnumerator MoveWatchPair(Vector3 officerEnd, Vector3 victimEnd, float seconds, Vector3 facing)
    {
        Vector3 officerStart = doorwayOfficer.transform.position, victimStart = doorwayVictim.transform.position;
        Quaternion officerFrom = doorwayOfficer.transform.rotation, victimFrom = doorwayVictim.transform.rotation;
        doorwayOfficer.Face(facing); doorwayVictim.Face(facing);
        Quaternion officerTo = doorwayOfficer.transform.rotation, victimTo = doorwayVictim.transform.rotation;
        for (float elapsed = 0f; elapsed < 0.55f; elapsed += Time.deltaTime)
        {
            float blend = Mathf.SmoothStep(0f, 1f, elapsed / 0.55f);
            doorwayOfficer.transform.rotation = Quaternion.Slerp(officerFrom, officerTo, blend);
            doorwayVictim.transform.rotation = Quaternion.Slerp(victimFrom, victimTo, blend);
            yield return null;
        }
        doorwayOfficer.transform.rotation = officerTo; doorwayVictim.transform.rotation = victimTo;
        doorwayOfficer.walking = doorwayVictim.walking = true;
        for (float elapsed = 0f; elapsed < seconds; elapsed += Time.deltaTime)
        {
            float t = Mathf.Clamp01(elapsed / seconds);
            PlaceWatchActor(doorwayOfficer, Vector3.Lerp(officerStart, officerEnd, t));
            PlaceWatchActor(doorwayVictim, Vector3.Lerp(victimStart, victimEnd, t));
            yield return null;
        }
        PlaceWatchActor(doorwayOfficer, officerEnd); PlaceWatchActor(doorwayVictim, victimEnd);
        doorwayOfficer.walking = doorwayVictim.walking = false;
    }

    void PlaceWatchActor(Chapter1IncidentRig rig, Vector3 position)
    {
        PlaceDoorwayActor(rig, position);
        if (incidentHutDoor == null) return;
        Vector3 local = incidentHutDoor.transform.InverseTransformPoint(position);
        if (Mathf.Abs(local.x) < 5.1f && local.z > -2.8f && local.z < 9.8f)
        {
            float floor = incidentHutDoor.Threshold.y;
            if (TryGetIncidentSurfaceY(position, out float terrain)) floor = Mathf.Max(floor, terrain);
            rig.Ground(floor);
        }
    }

    void SetHutPairVisible(bool visible)
    {
        if (visible)
        {
            foreach (var saved in hutHiddenRenderers) if (saved.Key != null) saved.Key.enabled = saved.Value;
            hutHiddenRenderers.Clear();
            return;
        }
        foreach (Chapter1IncidentRig rig in new[] { doorwayOfficer, doorwayVictim })
            foreach (Renderer renderer in rig.GetComponentsInChildren<Renderer>(true))
                if (!hutHiddenRenderers.ContainsKey(renderer))
                {
                    hutHiddenRenderers.Add(renderer, renderer.enabled);
                    renderer.enabled = false;
                }
    }

    List<Chapter1IncidentRig> WatchWitnesses()
    {
        var actors = new List<Transform>();
        AddWeddingRetreatActor(actors, groomActor);
        AddWeddingDeliveryGuests(actors, wineDeliveryTargets);
        AddWeddingDeliveryGuests(actors, foodDeliveryTargets);
        foreach (var dancer in weddingCrowdDancers)
            if (dancer != null) AddWeddingRetreatActor(actors, dancer.transform);
        AddWeddingRetreatActor(actors, femaleVillagerActor);
        var result = new List<Chapter1IncidentRig>();
        foreach (Transform actor in actors)
        {
            var rig = PrepareDoorwayRig(actor, false);
            rig.walking = false; rig.frozen = false;
            rig.speakingWeight = rig.stumbleWeight = rig.pushWeight = 0f;
            rig.conversationTarget = null;
            result.Add(rig);
        }
        return result;
    }

    void TurnWatchWitnesses(List<Chapter1IncidentRig> crowd, Vector3 target)
    {
        foreach (var rig in crowd)
            rig.transform.rotation = Quaternion.RotateTowards(rig.transform.rotation,
                EntranceFacingRotation(rig.transform, target), 75f * Time.deltaTime);
    }

    Vector3 WatchFinalCameraGround()
    {
        return weddingDramaCenter + new Vector3(-1.75f, 0f, 1.6f) * doorwayHeight;
    }

    Vector3 WatchFireObstacleCenter()
    {
        Transform roast = FindTransformByName("烤豬");
        Renderer renderer = roast != null ? roast.GetComponent<Renderer>() : null;
        return renderer != null ? renderer.bounds.center : GetFireCenterPosition();
    }

    IEnumerator AdvanceWatchWitnesses()
    {
        // Four adults take a few steps out of the wedding group while the
        // camera watches the hut. Their complete movement runs in world space.
        // Everyone else keeps their established spot and turns to watch.
        var available = WatchWitnesses();
        available.Remove(doorwayVictim);
        available.RemoveAll(rig => rig.Height < doorwayHeight * 0.8f);
        Vector3 origin = WatchFinalCameraGround();
        Vector3[] slots = {
            origin + new Vector3(-0.85f, 0f, -0.58f) * doorwayHeight,
            origin + new Vector3(-0.85f, 0f, 0.58f) * doorwayHeight,
            origin + new Vector3(-1.35f, 0f, -0.90f) * doorwayHeight,
            origin + new Vector3(-1.35f, 0f, 0.90f) * doorwayHeight
        };
        var moving = new List<WeddingRetreatActor>();
        Vector3 fire = WatchFireObstacleCenter();
        float duration = 0f;
        foreach (Vector3 slot in slots)
        {
            Chapter1IncidentRig closest = null;
            float nearest = float.MaxValue;
            foreach (var rig in available)
            {
                float distance = Vector3.ProjectOnPlane(rig.transform.position - slot, Vector3.up).sqrMagnitude;
                if (distance < nearest) { nearest = distance; closest = rig; }
            }
            if (closest == null) break;
            available.Remove(closest);
            var path = BuildWeddingRetreatPath(closest.transform.position, slot, fire, doorwayHeight * 1.3f);
            float seconds = Mathf.Max(3f, WatchPathLength(path) / (doorwayHeight * 0.60f));
            moving.Add(new WeddingRetreatActor { actor = closest.transform, rig = closest,
                rotation = closest.transform.rotation, path = path, length = WatchPathLength(path),
                duration = seconds, delay = moving.Count * 0.2f, soleOffset = EntranceSoleOffset(closest.transform) });
            duration = Mathf.Max(duration, seconds + moving.Count * 0.2f);
        }
        for (float elapsed = 0f; elapsed < duration + 0.5f; elapsed += Time.deltaTime)
        {
            UpdateWeddingCrowdRetreat(moving, elapsed, incidentDoorPosition);
            yield return null;
        }
        foreach (var member in moving) member.rig.walking = false;
    }

    IEnumerator WatchPoliceDeparture()
    {
        if (watchCrowdAdvance != null) { yield return watchCrowdAdvance; watchCrowdAdvance = null; }
        var first = PrepareDoorwayRig(primaryPoliceActor, true);
        var second = PrepareDoorwayRig(secondaryPoliceActor, true);
        foreach (var rig in new[] { first, second })
        {
            rig.gripPartner = rig.gripTarget = rig.pointTarget = rig.conversationTarget = null;
            rig.speakingWeight = rig.pushWeight = 0f; rig.strike = false; rig.frozen = false;
        }
        first.BeginDepartureWalk(0.12f);
        second.BeginDepartureWalk(0.48f);
        Vector3 center = weddingDramaCenter;
        // The old PoliceExitPoint is inside a raised hut. Finish the visible
        // departure on the open approach to the lane, before its drying rack.
        Vector3 exit = center + new Vector3(-5.80f, 0f, 2.04f) * doorwayHeight;
        Vector3 cameraGround = WatchFinalCameraGround();
        Vector3 direction = Vector3.left;
        Vector3 right = Vector3.Cross(Vector3.up, direction);
        Vector3 formation = cameraGround + direction * doorwayHeight * 1.75f;
        var crowd = WatchWitnesses();
        var firstPath = BuildWeddingRetreatPath(first.transform.position,
            formation - right * doorwayHeight * 0.29f, WatchFireObstacleCenter(), doorwayHeight * 1.3f);
        var secondPath = BuildWeddingRetreatPath(second.transform.position,
            formation + right * doorwayHeight * 0.29f - direction * doorwayHeight * 0.35f, WatchFireObstacleCenter(), doorwayHeight * 1.3f);
        SetWeddingDramaBeat("watch-police-regroup");
        // The released woman stays outside the hut. The officers walk back to
        // the open courtyard; none of the actors are teleported to the ending.
        yield return WalkWatchPaths(first, firstPath, second, secondPath, crowd, 0.65f);
        Vector3 cameraPosition = cameraGround;
        if (TryGetIncidentSurfaceY(cameraPosition, out float floor)) cameraPosition.y = floor;
        cameraPosition += Vector3.up * doorwayHeight * 0.94f;
        Vector3 focus = exit + Vector3.up * doorwayHeight * 0.65f;
        if (TryGetIncidentSurfaceY(exit, out float exitFloor)) focus.y = exitFloor + doorwayHeight * 0.65f;
        SetDoorwayCamera(cameraPosition, focus, 49f);
        if (dialogueUI != null) dialogueUI.HideInstant();
        SetWeddingDramaBeat("watch-police-departure");
        var leaveFirst = new List<Vector3> { first.transform.position, exit - right * doorwayHeight * 0.29f };
        var leaveSecond = new List<Vector3> { second.transform.position, exit + right * doorwayHeight * 0.29f - direction * doorwayHeight * 0.35f };
        ShowLine("", "警察們揚長而去，族人望著他們下山的背影。",
            WatchWalkDuration(WatchPathLength(leaveFirst), WatchPathLength(leaveSecond), 0.42f) + 2f);
        yield return WalkWatchPaths(first, leaveFirst, second, leaveSecond, crowd, 0.42f);
        SetWeddingDramaBeat("watch-departure-tableau");
        ShowLine("", "族人望著日警下山的背影。憤怒留在每個人的眼神裡，卻沒有人知道下一步該怎麼辦。",
            incidentDepartureHoldSeconds + 0.5f);
        yield return new WaitForSeconds(incidentDepartureHoldSeconds);
    }

    IEnumerator WalkWatchPaths(Chapter1IncidentRig first, List<Vector3> firstPath,
        Chapter1IncidentRig second, List<Vector3> secondPath, List<Chapter1IncidentRig> crowd, float speedInHeights)
    {
        float a = WatchPathLength(firstPath), b = WatchPathLength(secondPath);
        // Finish the turn before travelling so the first steps do not slide
        // sideways while the officers are still facing the courtyard.
        Quaternion firstFrom = first.transform.rotation, secondFrom = second.transform.rotation;
        Quaternion firstFacing = EntranceFacingRotation(first.transform, firstPath[1]);
        Quaternion secondFacing = EntranceFacingRotation(second.transform, secondPath[1]);
        float turnSeconds = Mathf.Max(Quaternion.Angle(firstFrom, firstFacing),
            Quaternion.Angle(secondFrom, secondFacing)) / 130f;
        first.walking = second.walking = false;
        for (float elapsed = 0f; elapsed < turnSeconds; elapsed += Time.deltaTime)
        {
            float turn = Mathf.SmoothStep(0f, 1f, elapsed / turnSeconds);
            first.transform.rotation = Quaternion.Slerp(firstFrom, firstFacing, turn);
            second.transform.rotation = Quaternion.Slerp(secondFrom, secondFacing, turn);
            TurnWatchWitnesses(crowd, (first.transform.position + second.transform.position) * 0.5f);
            yield return null;
        }
        first.transform.rotation = firstFacing; second.transform.rotation = secondFacing;
        float duration = WatchWalkDuration(a, b, speedInHeights);
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            float progress = WatchWalkProgress(Mathf.Clamp01(elapsed / duration));
            SampleWatchPath(first, firstPath, a * progress);
            SampleWatchPath(second, secondPath, b * progress);
            TurnWatchWitnesses(crowd, (first.transform.position + second.transform.position) * 0.5f);
            yield return null;
        }
        SampleWatchPath(first, firstPath, a); SampleWatchPath(second, secondPath, b);
        first.walking = second.walking = false;
    }

    float WatchWalkDuration(float firstLength, float secondLength, float speedInHeights)
    {
        float minimum = speedInHeights < 0.5f ? Mathf.Max(8f, minimumPoliceExitSeconds) : 1f;
        return Mathf.Max(minimum, Mathf.Max(firstLength, secondLength) / (doorwayHeight * speedInHeights));
    }

    static float WatchWalkProgress(float t)
    {
        // Short acceleration/deceleration, constant speed through the shot.
        const float ramp = 0.08f;
        if (t < ramp) return t * t / (2f * ramp * (1f - ramp));
        if (t > 1f - ramp) return 1f - (1f - t) * (1f - t) / (2f * ramp * (1f - ramp));
        return (t - ramp * 0.5f) / (1f - ramp);
    }

    static float WatchPathLength(List<Vector3> path)
    {
        float length = 0f;
        for (int i = 1; i < path.Count; i++) length += Vector3.ProjectOnPlane(path[i] - path[i - 1], Vector3.up).magnitude;
        return length;
    }

    void SampleWatchPath(Chapter1IncidentRig rig, List<Vector3> path, float distance)
    {
        for (int i = 1; i < path.Count; i++)
        {
            Vector3 segment = Vector3.ProjectOnPlane(path[i] - path[i - 1], Vector3.up);
            float length = segment.magnitude;
            if (distance <= length || i == path.Count - 1)
            {
                Vector3 position = Vector3.Lerp(path[i - 1], path[i], length > 0.001f ? Mathf.Clamp01(distance / length) : 1f);
                rig.transform.rotation = Quaternion.RotateTowards(rig.transform.rotation,
                    EntranceFacingRotation(rig.transform, rig.transform.position + segment), 130f * Time.deltaTime);
                rig.walking = true;
                PlaceWatchActor(rig, position);
                return;
            }
            distance -= length;
        }
    }

    void CleanupDoorwayWatch()
    {
        if (doorwayVictim != null) doorwayVictim.strugglingInPlace = false;
        if (doorwayVictim != null) doorwayVictim.StationaryGrip = null;
        if (doorwayOfficer != null) doorwayOfficer.StationaryGrip = null;
        if (hutInteriorAudio != null) hutInteriorAudio.Stop();
        if (watchCrowdAdvance != null) { StopCoroutine(watchCrowdAdvance); watchCrowdAdvance = null; }
        SetHutPairVisible(true);
        if (!doorwayWatchActive) return;
        foreach (var rig in new[] { doorwayOfficer, doorwayVictim })
            if (rig != null) { rig.walking = rig.frozen = false; rig.gripPartner = null; }
        if (doorwayOfficer != null) doorwayOfficer.batonVisible = true;
        if (tensionAmbience != null) tensionAmbience.volume = watchTensionVolume;
        doorwayWatchActive = false;
    }
}
