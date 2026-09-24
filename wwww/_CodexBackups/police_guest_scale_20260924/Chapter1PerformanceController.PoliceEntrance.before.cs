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

    // Hold the dancers' actual positions for the reaction, then show the police
    // entering before revealing the crowd walking into the confrontation layout.
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
        List<WeddingRetreatActor> crowd = PrepareWeddingCrowdRetreat(center, height);
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
        Vector3 widePosition = center + new Vector3(-0.55f, 1.1f, -4.15f) * height;
        if (TryGetIncidentSurfaceY(widePosition, out float wideGround))
            widePosition.y = Mathf.Max(widePosition.y, wideGround + height * 0.95f);
        Vector3 wideFocus = center + new Vector3(-0.28f, 0.5f, 0.05f) * height;
        Vector3 entranceFocus = (firstStart + secondStart) * 0.5f + Vector3.up * height * 0.63f;
        Vector3 entrancePosition = entranceFocus + new Vector3(0.65f, 0.26f, -3.8f) * height;
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
                camera.rect = new Rect(0f, 0f, 1f, 1f);
            }
        }

        SetWeddingDramaBeat("crowd-reaction");
        ShowLine("旁白", "鼓聲突然停了下來。族人鬆開雙手，轉頭望向腳步聲傳來的方向。", 2.2f);
        for (float reaction = 0f; reaction < 0.9f; reaction += Time.deltaTime)
        {
            foreach (WeddingRetreatActor member in crowd)
            {
                float turn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((reaction - member.delay * 0.5f) / 0.65f));
                member.actor.rotation = Quaternion.Slerp(member.rotation,
                    EntranceFacingRotation(member.actor, firstStart), turn);
            }
            yield return null;
        }
        // A motivated cut to the arriving officers, on the same side of the
        // action axis. Crowd roots keep moving continuously throughout this shot.
        SetDoorwayCamera(entrancePosition, entranceFocus, 48f);
        SetWeddingDramaBeat("police-entrance-short");
        ShowLine("旁白", "兩名日本警察走入會場，族人紛紛退開，讓出空間。", 7.2f);
        firstRig.walking = secondRig.walking = true;
        firstRig.smoothLocomotion = secondRig.smoothLocomotion = true;
        float elapsed = 0f;
        const float walkSeconds = 5.8f;
        float retreatSeconds = walkSeconds + 0.35f;
        foreach (WeddingRetreatActor member in crowd)
            retreatSeconds = Mathf.Max(retreatSeconds, member.delay + member.duration + 0.5f);
        bool wideShot = false;
        while (elapsed < retreatSeconds)
        {
            elapsed += Time.deltaTime;
            SetEntranceGroundPosition(first, Vector3.Lerp(firstStart, firstEnd,
                Mathf.Clamp01(elapsed / walkSeconds)), firstOffset);
            SetEntranceGroundPosition(second, Vector3.Lerp(secondStart, secondEnd,
                Mathf.Clamp01((elapsed - 0.35f) / walkSeconds)), secondOffset);
            if(TryGetIncidentSurfaceY(first.position,out float firstFloor))firstRig.Ground(firstFloor);
            if(TryGetIncidentSurfaceY(second.position,out float secondFloor))secondRig.Ground(secondFloor);
            firstRig.walking = elapsed < walkSeconds;
            secondRig.walking = elapsed < walkSeconds + 0.35f;
            UpdateWeddingCrowdRetreat(crowd, elapsed, firstEnd);
            if (elapsed >= 1.8f)
            {
                if (!wideShot) { wideShot = true; SetWeddingDramaBeat("crowd-retreat-wide"); }
                // Cut back to a steady wide angle; no fly-through of the dancers.
                SetDoorwayCamera(widePosition, wideFocus, 50f);
            }
            yield return null;
        }
        foreach (WeddingRetreatActor member in crowd) member.rig.walking = false;
        CapturePoliceWitnessViewPose();
        SetWeddingDramaBeat("crowd-retreat-complete");
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
        Chapter1IncidentRig rig = actor.GetComponent<Chapter1IncidentRig>();
        if (rig != null && rig.Height > 0f)
            forward = rig.Forward;
        Animator animator = actor.GetComponentInChildren<Animator>(true);
        if ((rig == null || rig.Height <= 0f) && animator != null && animator.isHuman)
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

    private sealed class WeddingRetreatActor
    {
        public Transform actor;
        public Chapter1IncidentRig rig;
        public Quaternion rotation;
        public List<Vector3> path;
        public float length, delay, duration, soleOffset;
    }

    private List<WeddingRetreatActor> PrepareWeddingCrowdRetreat(Vector3 center, float height)
    {
        var actors = new List<Transform>();
        AddWeddingRetreatActor(actors, groomActor);
        // Delivery guests deliberately stay outside the dance circle during
        // quests. They still belong to the wedding and must react to the police.
        // Give these outer guests nearby slots before filling the inner crowd.
        AddWeddingDeliveryGuests(actors, wineDeliveryTargets);
        AddWeddingDeliveryGuests(actors, foodDeliveryTargets);
        foreach (Chapter1CircleDancer dancer in weddingCrowdDancers)
            if (dancer != null) AddWeddingRetreatActor(actors, dancer.transform);
        AddWeddingRetreatActor(actors, femaleVillagerActor);
        // Leave the near/left side of the fire open so the batons and villagers'
        // faces remain readable, with children in the front and adults behind.
        Vector2[] slots = {
            new Vector2(0.72f, -0.55f), new Vector2(-0.55f, 1.05f),
            new Vector2(0.02f, 1.25f), new Vector2(0.6f, 1.3f),
            new Vector2(1.15f, 1.12f), new Vector2(1.66f, 0.8f),
            new Vector2(1.95f, 0.22f), new Vector2(1.78f, -0.4f),
            new Vector2(1.3f, -0.9f), new Vector2(-0.28f, 1.85f),
            new Vector2(0.33f, 1.92f), new Vector2(0.95f, 1.87f),
            new Vector2(1.55f, 1.57f), new Vector2(2.32f, -0.55f),
            new Vector2(0.50f, -1.15f), new Vector2(1.05f, -1.22f),
            new Vector2(1.65f, -1.08f), new Vector2(2.26f, 1.25f)
        };
        var available = new List<Vector3>();
        foreach (Vector2 slot in slots) available.Add(center + new Vector3(slot.x, 0f, slot.y) * height);
        var frontSlots = new HashSet<Vector3> { available[14], available[15], available[16] };
        // Future added guests also need a destination, rather than silently
        // remaining behind the officers when the authored slots run out.
        while (available.Count < actors.Count)
        {
            int extra = available.Count - slots.Length;
            available.Add(center + new Vector3(-0.35f + (extra % 5) * 0.6f,
                0f, 2.55f + (extra / 5) * 0.6f) * height);
        }
        var crowd = new List<WeddingRetreatActor>();
        for (int i = 0; i < actors.Count; i++)
        {
            Transform actor = actors[i];
            Chapter1CircleDancer dancer = actor.GetComponent<Chapter1CircleDancer>();
            if (dancer != null) { dancer.SetCanCircleDance(false); dancer.enabled = false; }
            foreach (Behaviour component in actor.GetComponentsInChildren<Behaviour>(true))
            {
                string type = component.GetType().Name;
                if (type == "Chapter1HandHoldIK" || type == "Chapter1FaceFire"
                    || type == "Chapter1WeddingFaceCenter" || type == "Chapter1WeddingLimbDance"
                    || type == "NPCNaturalLookAt")
                    component.enabled = false;
            }
            DetachActorFromDancePivot(actor);
            Vector3 start = actor.position;
            // Keep the speaking leader's established blocking; every other
            // villager takes the nearest free slot to avoid crossing the crowd.
            int nearest = -1;
            float best = float.MaxValue;
            bool shortGuest = IsDeliveryTaskNPC(actor) && GetActorStandingHeight(actor) < height * 0.8f;
            for (int slot = 0; slot < available.Count; slot++)
            {
                float distance = Vector3.ProjectOnPlane(available[slot] - start, Vector3.up).sqrMagnitude;
                // The smaller stationary guests disappear behind the roast if
                // assigned the back row. Reserve visible front-row places.
                if (frontSlots.Contains(available[slot]) != shortGuest) distance += height * height * 100f;
                if (distance < best) { best = distance; nearest = slot; }
            }
            if (actor == groomActor && available.Count > 0) nearest = 0;
            Vector3 destination = nearest >= 0 ? available[nearest] : start;
            if (nearest >= 0) available.RemoveAt(nearest);
            Animator animator = actor.GetComponentInChildren<Animator>(true);
            if (animator != null)
            {
                animator.applyRootMotion = false;
                animator.speed = 1f;
            }
            Chapter1IncidentRig rig = actor.GetComponent<Chapter1IncidentRig>();
            if (rig == null) rig = actor.gameObject.AddComponent<Chapter1IncidentRig>();
            rig.Initialize(animator, false, 0.6f);
            rig.smoothLocomotion = true;
            rig.strideScale = 0.8f;
            var grounder = actor.GetComponent<Chapter1NpcGrounding>();
            if (grounder != null) grounder.enabled = false;
            List<Vector3> path = BuildWeddingRetreatPath(start, destination, center, height * 0.72f);
            float length = 0f;
            for (int point = 1; point < path.Count; point++) length += Vector3.Distance(path[point - 1], path[point]);
            crowd.Add(new WeddingRetreatActor {
                actor = actor, rig = rig, rotation = actor.rotation, path = path, length = length,
                delay = 0.15f + (i % 5) * 0.12f,
                duration = Mathf.Max(2.5f, length / (height * 0.90f)), soleOffset = EntranceSoleOffset(actor)
            });
        }
        return crowd;
    }

    private void AddWeddingDeliveryGuests(List<Transform> actors, Transform[] targets)
    {
        if (targets == null) return;
        foreach (Transform target in targets)
            AddWeddingRetreatActor(actors, GetDeliveryActorRoot(target));
    }

    private void AddWeddingRetreatActor(List<Transform> actors, Transform actor)
    {
        if (actor == null || !actor.gameObject.activeInHierarchy
            || actor == primaryPoliceActor || actor == secondaryPoliceActor) return;
        foreach (Transform existing in actors)
            if (actor == existing || actor.IsChildOf(existing) || existing.IsChildOf(actor)) return;
        actors.Add(actor);
    }

    private static List<Vector3> BuildWeddingRetreatPath(Vector3 start, Vector3 end, Vector3 center, float clearance)
    {
        end.y = start.y;
        center.y = start.y;
        var path = new List<Vector3> { start };
        Vector3 segment = end - start;
        float along = segment.sqrMagnitude > 0.001f
            ? Mathf.Clamp01(Vector3.Dot(center - start, segment) / segment.sqrMagnitude) : 0f;
        if (Vector3.Distance(start + segment * along, center) < clearance)
        {
            Vector3 from = start - center, to = end - center;
            float angle = Vector3.SignedAngle(from, to, Vector3.up);
            float radius = Mathf.Max(clearance * 1.08f, Mathf.Min(from.magnitude, to.magnitude));
            int steps = Mathf.Max(2, Mathf.CeilToInt(Mathf.Abs(angle) / 12f));
            for (int step = 0; step <= steps; step++)
                path.Add(center + Quaternion.AngleAxis(angle * step / steps, Vector3.up) * from.normalized * radius);
        }
        path.Add(end);
        return path;
    }

    private void UpdateWeddingCrowdRetreat(List<WeddingRetreatActor> crowd, float elapsed, Vector3 threat)
    {
        foreach (WeddingRetreatActor member in crowd)
        {
            float progress = Mathf.Clamp01((elapsed - member.delay) / member.duration);
            float distance = Mathf.SmoothStep(0f, 1f, progress) * member.length;
            Vector3 position = member.path[member.path.Count - 1];
            Vector3 direction = Vector3.zero;
            for (int point = 1; point < member.path.Count; point++)
            {
                Vector3 segment = member.path[point] - member.path[point - 1];
                float length = segment.magnitude;
                if (distance <= length && length > 0.001f)
                {
                    position = member.path[point - 1] + segment * (distance / length);
                    direction = segment;
                    break;
                }
                distance -= length;
            }
            member.rig.walking = progress > 0f && progress < 1f && member.length > 0.05f;
            if (progress > 0f)
            {
                Vector3 look = progress < 0.94f && direction.sqrMagnitude > 0.001f
                    ? member.actor.position + direction : threat;
                member.actor.rotation = Quaternion.RotateTowards(member.actor.rotation,
                    EntranceFacingRotation(member.actor, look), 160f * Time.deltaTime);
                SetEntranceGroundPosition(member.actor, position, member.soleOffset);
            }
            if (TryGetIncidentSurfaceY(member.actor.position, out float floor)) member.rig.Ground(floor);
        }
    }
}
