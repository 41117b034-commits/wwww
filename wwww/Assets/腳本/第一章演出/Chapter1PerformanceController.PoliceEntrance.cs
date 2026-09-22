using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Chapter1PerformanceController
{
    private void CancelOpeningForPoliceIncident()
    {
        foreach (Chapter1EyeOpening opening in Object.FindObjectsByType<Chapter1EyeOpening>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (opening.chapterController == null || opening.chapterController == this)
                opening.FinishForPoliceIncident();
        }
    }

    // A single wide shot establishes the interruption. Its framing is measured
    // once from standing characters, never from animated feet or moving heads.
    private IEnumerator StableWeddingPoliceEntrance()
    {
        Transform first = primaryPoliceActor;
        Transform second = secondaryPoliceActor;
        if (first == null || second == null)
        {
            yield return AnimatePoliceEntranceFallback();
            yield break;
        }

        SetActiveIncludingParents(first);
        SetActiveIncludingParents(second);
        PreparePoliceProportions();
        Chapter1IncidentRig firstRig = first.GetComponent<Chapter1IncidentRig>();
        Chapter1IncidentRig secondRig = second.GetComponent<Chapter1IncidentRig>();
        PlayAnimatorStateIfAvailable(first, policeIdleStateName);
        PlayAnimatorStateIfAvailable(second, policeIdleStateName);
        Animator firstAnimator = first.GetComponentInChildren<Animator>(true);
        Animator secondAnimator = second.GetComponentInChildren<Animator>(true);
        if (firstAnimator != null) { firstAnimator.applyRootMotion = false; firstAnimator.Update(0f); }
        if (secondAnimator != null) { secondAnimator.applyRootMotion = false; secondAnimator.Update(0f); }

        // Let the shared dance-to-idle grounding pass finish before blocking actors.
        yield return new WaitForEndOfFrame();
        yield return null;
        Vector3 center = danceCenter != null ? danceCenter.position : GetFireCenterPosition();
        float height = Mathf.Max(GetActorStandingHeight(groomActor),
            Mathf.Max(GetActorStandingHeight(first), GetActorStandingHeight(second)));
        height = Mathf.Max(1.7f, height);
        if (TryGetIncidentSurfaceY(center, out float floor)) center.y = floor;

        Vector3 firstEnd = center + new Vector3(-1.5f, 0f, -1.65f) * height;
        Vector3 secondEnd = center + new Vector3(-2.65f, 0f, -0.15f) * height;
        Vector3 firstStart = firstEnd + new Vector3(-1.2f, 0f, -0.5f) * height;
        Vector3 secondStart = secondEnd + new Vector3(-1.3f, 0f, -0.5f) * height;
        StageStoppedWeddingCrowd(center, height, firstEnd);
        weddingDramaCenter = center;
        weddingDramaHeight = height;
        // The visible central leader is the same actor who answers and steps up.
        if (groomActor != null) shovedVillagerActor = groomActor;

        first.position = firstStart;
        second.position = secondStart;
        first.rotation = EntranceFacingRotation(first, firstEnd);
        second.rotation = EntranceFacingRotation(second, secondEnd);
        EnsureCinematicActorGrounding(first, true);
        EnsureCinematicActorGrounding(second, true);
        Chapter1NpcGrounding firstGrounding = first.GetComponent<Chapter1NpcGrounding>();
        Chapter1NpcGrounding secondGrounding = second.GetComponent<Chapter1NpcGrounding>();
        // Preserve the sole offset; grounding against whichever animated foot is
        // lower each frame otherwise lifts and drops the entire walking character.
        float firstOffset = EntranceSoleOffset(first);
        float secondOffset = EntranceSoleOffset(second);
        if (firstGrounding != null) firstGrounding.enabled = false;
        if (secondGrounding != null) secondGrounding.enabled = false;
        firstStart = first.position;
        secondStart = second.position;

        incidentCameraView = GetPlayerViewTransform();
        if (incidentCameraView != null)
        {
            incidentCameraTrackedPoseDrivers = DisableCameraTrackedPoseDrivers(incidentCameraView);
            incidentCameraPoseLock = BeginCinematicCameraPoseLock(incidentCameraView);
            incidentHiddenHandRenderers = HidePlayerHandRenderers(out incidentHiddenHandRendererStates);
            Camera camera = incidentCameraView.GetComponent<Camera>();
            if (camera != null)
            {
                witnessZoomCamera = camera;
                witnessOriginalFieldOfView = camera.fieldOfView;
                witnessZoomApplied = true;
                camera.fieldOfView = 50f;
            }
            Vector3 position = center + new Vector3(-0.55f, 1.1f, -4.15f) * height;
            if (TryGetIncidentSurfaceY(position, out float cameraGround))
                position.y = Mathf.Max(position.y, cameraGround + height * 0.95f);
            Vector3 focus = center + new Vector3(-0.28f, 0.5f, 0.05f) * height;
            SetCinematicCameraPose(incidentCameraView, incidentCameraPoseLock,
                position, Quaternion.LookRotation(focus - position, Vector3.up));
            CapturePoliceWitnessViewPose();
        }

        ShowLine("旁白", "鼓聲突然停了下來。兩名日本警察走入會場，族人停下舞步，轉身望向來人。", 6.4f);
        firstRig.walking = secondRig.walking = true;
        float elapsed = 0f;
        const float walkSeconds = 5.8f;
        while (elapsed < walkSeconds + 0.35f)
        {
            elapsed += Time.deltaTime;
            SetEntranceGroundPosition(first, Vector3.Lerp(firstStart, firstEnd,
                Mathf.Clamp01(elapsed / walkSeconds)), firstOffset);
            SetEntranceGroundPosition(second, Vector3.Lerp(secondStart, secondEnd,
                Mathf.Clamp01((elapsed - 0.35f) / walkSeconds)), secondOffset);
            if(TryGetIncidentSurfaceY(first.position,out float firstFloor))firstRig.Ground(firstFloor);
            if(TryGetIncidentSurfaceY(second.position,out float secondFloor))secondRig.Ground(secondFloor);
            yield return null;
        }
        firstRig.walking = secondRig.walking = false;
        ResetPoliceAnimatorSpeed(first);
        ResetPoliceAnimatorSpeed(second);
        PlayAnimatorStateIfAvailable(first, policeIdleStateName);
        PlayAnimatorStateIfAvailable(second, policeIdleStateName);
        if (firstAnimator != null) firstAnimator.Update(0f);
        if (secondAnimator != null) secondAnimator.Update(0f);
        EnsureCinematicActorGrounding(first, true);
        EnsureCinematicActorGrounding(second, true);

        Transform firstTarget = groomActor != null ? groomActor : (danceCenter != null ? danceCenter : transform);
        Transform secondTarget = femaleVillagerActor != null ? femaleVillagerActor : firstTarget;
        Quaternion firstFrom = first.rotation;
        Quaternion secondFrom = second.rotation;
        Quaternion firstTo = EntranceFacingRotation(first, firstTarget.position);
        Quaternion secondTo = EntranceFacingRotation(second, secondTarget.position);
        elapsed = 0f;
        while (elapsed < 0.55f)
        {
            elapsed += Time.deltaTime;
            float turn = Mathf.SmoothStep(0f, 1f, elapsed / 0.55f);
            first.rotation = Quaternion.Slerp(firstFrom, firstTo, turn);
            second.rotation = Quaternion.Slerp(secondFrom, secondTo, turn);
            yield return null;
        }

        firstRig.pointTarget=firstTarget; secondRig.pointTarget=secondTarget;
        firstRig.batonVisible=secondRig.batonVisible=true;
        ShowLine("旁白", "兩名警察抽出警棍，指向族人，喝令婚禮立刻停止。", 3.8f);
        elapsed = 0f;
        while (elapsed < 1.6f)
        {
            elapsed += Time.deltaTime;
            firstRig.pointProgress=Mathf.Clamp01(elapsed/1.3f);
            secondRig.pointProgress=Mathf.Clamp01((elapsed-0.2f)/1.4f);
            yield return null;
        }
        firstRig.pointProgress=secondRig.pointProgress=1f;
        yield return WeddingSpeakerShot(first, true);
        SetWeddingDramaBeat("police-order");
        ShowLine("日警", "停止！婚禮立刻停止！", 2.6f);
        yield return new WaitForSeconds(2.6f);
        SetWeddingDramaBeat("police-insult");
        yield return PlayPoliceVoicedLine("日警", "這種野蠻婚禮，竟然還敢辦得這麼熱鬧？",
            policeInsultVoice, 4f);
        firstRig.pointTarget=secondRig.pointTarget=null;
        Chapter1IncidentRig leaderRig = groomActor != null ? PrepareDoorwayRig(groomActor, false) : null;
        if (leaderRig != null) { leaderRig.conversationTarget=first; leaderRig.speakingWeight=0.8f; }
        yield return WeddingSpeakerShot(groomActor, false);
        SetWeddingDramaBeat("leader-reply");
        yield return PlayPoliceVoicedLine("族人", "我們只是辦婚禮，沒有冒犯。",
            groomReplyVoice, 3.2f, groomReplyVolumeScale);
        if (leaderRig != null) leaderRig.speakingWeight=0f;
        firstRig.pointTarget=secondRig.pointTarget=null;
        // Keep the same pose lock alive for the existing subsequent story shots.
    }

    private float EntranceSoleOffset(Transform actor)
    {
        return TryGetIncidentSurfaceY(actor.position, out float y) ? actor.position.y - y : 0f;
    }

    private Quaternion EntranceFacingRotation(Transform actor, Vector3 targetPosition)
    {
        Vector3 direction = Vector3.ProjectOnPlane(targetPosition - actor.position, Vector3.up);
        if (direction.sqrMagnitude < 0.001f) return actor.rotation;
        // The authored police meshes face local -X. Use their mesh axis rather
        // than the mirrored humanoid shoulders, which change during retargeting.
        if (actor == primaryPoliceActor || actor == secondaryPoliceActor)
            return Quaternion.LookRotation(direction, Vector3.up) * Quaternion.Euler(0f, 90f, 0f);
        Vector3 forward = actor.forward;
        Animator animator = actor.GetComponentInChildren<Animator>(true);
        if (animator != null && animator.isHuman)
        {
            Transform left = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            Transform right = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            if (left != null && right != null)
                forward = Vector3.Cross(right.position - left.position, Vector3.up);
        }
        forward = Vector3.ProjectOnPlane(forward, Vector3.up);
        if (forward.sqrMagnitude < 0.001f || direction.sqrMagnitude < 0.001f) return actor.rotation;
        return Quaternion.AngleAxis(Vector3.SignedAngle(forward, direction, Vector3.up),
            Vector3.up) * actor.rotation;
    }

    private void SetEntranceGroundPosition(Transform actor, Vector3 position, float soleOffset)
    {
        if (TryGetIncidentSurfaceY(position, out float y)) position.y = y + soleOffset;
        actor.position = position;
    }

    private void StageStoppedWeddingCrowd(Vector3 center, float height, Vector3 threat)
    {
        var actors = new List<Transform>();
        if (groomActor != null) actors.Add(groomActor);
        foreach (Chapter1CircleDancer dancer in weddingCrowdDancers)
            if (dancer != null && !actors.Contains(dancer.transform)) actors.Add(dancer.transform);
        if (femaleVillagerActor != null && !actors.Contains(femaleVillagerActor)) actors.Add(femaleVillagerActor);
        // Leave the near/left side of the fire open so the batons and villagers'
        // faces remain readable, with children in the front and adults behind.
        Vector2[] slots = {
            new Vector2(0.72f, -0.55f), new Vector2(-0.55f, 1.05f),
            new Vector2(0.02f, 1.25f), new Vector2(0.6f, 1.3f),
            new Vector2(1.15f, 1.12f), new Vector2(1.66f, 0.8f),
            new Vector2(1.95f, 0.22f), new Vector2(1.78f, -0.4f),
            new Vector2(1.3f, -0.9f), new Vector2(-0.28f, 1.85f),
            new Vector2(0.33f, 1.92f), new Vector2(0.95f, 1.87f),
            new Vector2(1.55f, 1.57f), new Vector2(2.32f, -0.55f)
        };
        for (int i = 0; i < actors.Count; i++)
        {
            Transform actor = actors[i];
            Chapter1CircleDancer dancer = actor.GetComponent<Chapter1CircleDancer>();
            if (dancer != null) { dancer.SetCanCircleDance(false); dancer.enabled = false; }
            foreach (Behaviour component in actor.GetComponentsInChildren<Behaviour>(true))
            {
                string type = component.GetType().Name;
                if (type == "Chapter1HandHoldIK" || type == "Chapter1FaceFire"
                    || type == "Chapter1WeddingFaceCenter" || type == "NPCNaturalLookAt")
                    component.enabled = false;
            }
            if (i < slots.Length)
            {
                Vector2 slot = slots[i];
                Vector3 position = center + new Vector3(slot.x, 0f, slot.y) * height;
                position.y = actor.position.y;
                actor.position = position;
            }
            Animator animator = actor.GetComponentInChildren<Animator>(true);
            if (animator != null)
            {
                animator.applyRootMotion = false;
                animator.speed = 1f;
                if (animator.HasState(0, Animator.StringToHash("Idle"))) animator.Play("Idle", 0, 0f);
                animator.Update(0f);
            }
            actor.rotation = EntranceFacingRotation(actor, threat);
            EnsureCinematicActorGrounding(actor, true);
        }
    }
}
