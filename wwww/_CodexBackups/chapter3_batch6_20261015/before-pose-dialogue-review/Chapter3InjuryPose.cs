using System.Collections.Generic;
using UnityEngine;

// A local performance layer for the injury and forest ending. The shared rig
// restores its normal pose each frame; this layer bends existing bones only.
[DefaultExecutionOrder(4600)]
public sealed class Chapter3InjuryPose : MonoBehaviour
{
    public float InjuryWeight { get; private set; }
    public float SupportWeight { get; private set; }
    public float DeathWeight { get; private set; }
    public bool BonesReady => prepared;
    public Vector3 SupportHandPosition => prepared ? hands[supportArm].position : transform.position;

    Chapter2Actor actor;
    Chapter1IncidentRig rig;
    Chapter2SpeechFace speechFace;
    MaterialPropertyBlock faceBlock;
    Transform bodyRoot, spine, supportPlayer;
    Camera supportView;
    readonly Transform[] thighs = new Transform[2], knees = new Transform[2], feet = new Transform[2];
    readonly Transform[] arms = new Transform[2], elbows = new Transform[2], hands = new Transform[2];
    readonly Vector3[] neutralAnkles = new Vector3[2], footTargets = new Vector3[2];
    readonly Quaternion[] soleRotations = new Quaternion[2];
    Transform[] posedBones;
    Vector3[] beforePositions;
    Quaternion[] beforeRotations;
    Transform[] deathBones;
    Vector3[] deathPositions, beforeDeathPositions;
    Quaternion[] deathRotations, beforeDeathRotations;
    Vector3 deathForward;
    Vector3 previousPosition;
    float limpPhase, motionWeight, rightSupportBlend;
    int supportArm;
    bool prepared, poseApplied, deathCaptured, deathApplied;

    public void Configure(Chapter2Actor source)
    {
        ResetPose();
        actor = source;
        rig = null;
        speechFace = source ? source.GetComponent<Chapter2SpeechFace>() : null;
        prepared = false;
        deathBones = null;
        deathPositions = beforeDeathPositions = null;
        deathRotations = beforeDeathRotations = null;
        previousPosition = source ? source.transform.position : transform.position;
    }

    public void SetInjured(float weight)
    {
        InjuryWeight = Mathf.Clamp01(weight);
        if (InjuryWeight <= 0f) ResetPose();
    }

    // Pass the player's controller root, rather than the camera. Positioning,
    // collision, movement restrictions and checkpoint choices belong to the flow.
    public void SetSupported(float weight, Transform player)
    {
        float next = Mathf.Clamp01(weight);
        bool chooseArm = SupportWeight <= 0f || supportPlayer != player;
        supportPlayer = player;
        var controller = player ? player.GetComponent<Chapter3Player>() : null;
        supportView = controller ? controller.view : null;
        if (chooseArm && player && actor && actor.Rig)
        {
            Vector3 right = Vector3.Cross(Vector3.up, actor.Rig.Forward);
            supportArm = Vector3.Dot(player.position - actor.transform.position, right) > 0f ? 1 : 0;
            rightSupportBlend = supportArm;
        }
        SupportWeight = next;
    }

    // The ending owns timing, camera, actor-root placement and collision. Do not
    // combine this with Actor.BeginFall: that rotates only Pelvis on this avatar
    // and bakes every body mesh, fighting the Hip/Waist injury performance.
    public void SetDying(float weight)
    {
        if (float.IsNaN(weight)) return;
        DeathWeight = Mathf.Clamp01(weight);
        if (DeathWeight <= 0f)
        {
            RestoreLastPose();
            deathCaptured = false;
        }
        else if (actor) actor.speaking = false;
    }

    public void ResetPose()
    {
        RestoreLastPose();
        InjuryWeight = SupportWeight = DeathWeight = 0f;
        deathCaptured = false;
        supportPlayer = null;
        supportView = null;
        limpPhase = motionWeight = rightSupportBlend = 0f;
        supportArm = 0;
        previousPosition = actor ? actor.transform.position : transform.position;
    }

    void Update()
    {
        // This runs after Actor.Update and before the speech-face LateUpdate.
        // Keep lips and talking gestures quiet without disabling shared scripts
        // or changing fallen/seated state needed by a later F8 or checkpoint.
        if (DeathWeight <= 0f || !actor) return;
        actor.speaking = false;
        if (actor.Rig) actor.Rig.speakingWeight = 0f;
    }

    void LateUpdate()
    {
        if (!Prepare()) return;
        Vector3 movement = Vector3.ProjectOnPlane(actor.transform.position - previousPosition, Vector3.up);
        previousPosition = actor.transform.position;
        if (InjuryWeight <= 0f && DeathWeight <= 0f) { poseApplied = false; return; }
        if (DeathWeight > 0f)
        {
            CaptureDeathRestore();
            if (deathCaptured)
            {
                CapturePose();
                ApplyDeathPose();
                return;
            }
        }

        float height = rig.Height;
        float injury = DeathWeight > 0f ? 1f : InjuryWeight;
        float supported = DeathWeight > 0f ? 0f : SupportWeight;
        float seated = 1f - supported;
        Vector3 forward = rig.Forward;
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        if (supported > 0f && supportPlayer)
        {
            // Path movement can turn the actor after lifting. Re-evaluate the
            // near arm in that final facing, but keep it through small turns or
            // player movement directly in front/behind the body.
            float side = Vector3.Dot(supportPlayer.position - actor.transform.position, right);
            float switchMargin = Mathf.Max(.10f, height * .075f);
            if (supportArm == 0 && side > switchMargin) supportArm = 1;
            else if (supportArm == 1 && side < -switchMargin) supportArm = 0;
            rightSupportBlend = Mathf.MoveTowards(rightSupportBlend, supportArm, Time.deltaTime * 4f);
        }
        float supportSide = Mathf.Lerp(-1f, 1f, rightSupportBlend);
        motionWeight = Mathf.MoveTowards(motionWeight,
            movement.sqrMagnitude > .000001f && supported > .5f ? 1f : 0f, Time.deltaTime * 7f);
        // Distance, not elapsed time, advances the limp: waiting never slides feet.
        limpPhase += Mathf.Min(movement.magnitude, height * .15f) / Mathf.Max(.1f, height * .44f) * Mathf.PI * 2f;

        CapturePose();
        for (int i = 0; i < 2; i++)
        {
            Vector3 neutral = actor.transform.TransformPoint(neutralAnkles[i]);
            // Preserve the shared gait timing, but shorten the injured right leg's step.
            Vector3 standing = Vector3.Lerp(neutral, feet[i].position, i == 0 ? .66f : .25f);
            Vector3 side = right * (i == 0 ? -1f : 1f);
            Vector3 sitting = neutral + forward * height * (i == 0 ? .13f : .29f) + side * height * .035f;
            footTargets[i] = Vector3.Lerp(feet[i].position, Vector3.Lerp(sitting, standing, supported), injury);
            soleRotations[i] = feet[i].rotation;
        }

        float restingHipY = actor.GroundHeight + height * .20f;
        float standingHipY = rig.Hips.position.y - height * (.045f + .012f * motionWeight * Mathf.Max(0f, Mathf.Sin(limpPhase)));
        float wantedHipY = Mathf.Lerp(restingHipY, standingHipY, supported);
        // Pelvis and Waist are siblings on the source avatar. Moving their common
        // Hip bone lowers the torso and legs together, without moving the actor root.
        bodyRoot.position += Vector3.up * ((wantedHipY - rig.Hips.position.y) * injury)
            - forward * height * .035f * seated * injury;
        if (spine)
        {
            float breath = Mathf.Sin(Time.time * 2.1f) * .65f;
            spine.rotation = Quaternion.AngleAxis((Mathf.Lerp(27f, 13f, supported) + breath) * injury, right)
                * Quaternion.AngleAxis(Mathf.Lerp(7f, -supportSide * 10f, supported) * injury, forward)
                * spine.rotation;
        }
        for (int i = 0; i < 2; i++)
        {
            Vector3 side = right * (i == 0 ? -1f : 1f);
            Chapter1IncidentRig.Solve(thighs[i], knees[i], feet[i], footTargets[i], forward + side * .2f * seated);
            feet[i].rotation = soleRotations[i];
        }

        for (int i = 0; i < 2; i++)
        {
            Vector3 side = right * (i == 0 ? -1f : 1f);
            Vector3 originalForearm = hands[i].position - elbows[i].position;
            Quaternion originalWrist = hands[i].rotation;
            // One palm braces beside the body, the other holds the injured thigh.
            Vector3 resting = i == 0
                ? rig.Hips.position + side * height * .16f + forward * height * .07f
                : knees[i].position - forward * height * .045f + Vector3.up * height * .045f;
            if (i == 0) resting.y = actor.GroundHeight + height * .045f;
            Vector3 standing = rig.Hips.position + side * height * .10f + forward * height * .12f + Vector3.up * height * .10f;
            if (supportPlayer)
            {
                Vector3 playerRight = Vector3.ProjectOnPlane(supportPlayer.right, Vector3.up).normalized;
                float shoulderSide = Vector3.Dot(actor.transform.position - supportPlayer.position, playerRight) >= 0f ? 1f : -1f;
                Vector3 shoulder = supportView
                    ? supportView.transform.position - Vector3.up * .27f
                    : supportPlayer.position + Vector3.up * height * .78f;
                shoulder += playerRight * (shoulderSide * .16f);
                // Transfer the reach over a quarter-second rather than snapping
                // both arms when a large turn makes the other arm nearer.
                standing = Vector3.Lerp(standing, shoulder, i == 0 ? 1f - rightSupportBlend : rightSupportBlend);
            }
            Vector3 target = Vector3.Lerp(resting, standing, supported);
            float reach = (Vector3.Distance(arms[i].position, elbows[i].position)
                + Vector3.Distance(elbows[i].position, hands[i].position)) * .96f;
            target = arms[i].position + Vector3.ClampMagnitude(target - arms[i].position, reach);
            Chapter1IncidentRig.Solve(arms[i], elbows[i], hands[i], Vector3.Lerp(hands[i].position, target, injury), side - forward * .22f);
            // Preserve the avatar's authored wrist basis; its local axes differ
            // from Unity's axes and there is no need to duplicate the hand mesh.
            hands[i].rotation = Quaternion.FromToRotation(originalForearm, hands[i].position - elbows[i].position) * originalWrist;
        }
        if (rig.Head)
            rig.Head.rotation = Quaternion.AngleAxis(Mathf.Lerp(14f, 8f, supported) * injury, right) * rig.Head.rotation;
        if (DeathWeight > 0f)
        {
            // Capture a wounded seated pose once, then make the entire collapse
            // depend only on DeathWeight. Breath, gait and late gestures cannot
            // make the body move again after the ending reaches weight one.
            for (int i = 0; i < deathBones.Length; i++)
            {
                deathPositions[i] = deathBones[i].localPosition;
                deathRotations[i] = deathBones[i].localRotation;
            }
            deathForward = actor.transform.InverseTransformDirection(forward);
            deathCaptured = true;
            ApplyDeathPose();
        }
    }

    void CaptureDeathRestore()
    {
        if (deathBones == null)
        {
            // Only lightweight transform data; no cloned or baked meshes.
            deathBones = bodyRoot.GetComponentsInChildren<Transform>(true);
            deathPositions = new Vector3[deathBones.Length];
            deathRotations = new Quaternion[deathBones.Length];
            beforeDeathPositions = new Vector3[deathBones.Length];
            beforeDeathRotations = new Quaternion[deathBones.Length];
        }
        for (int i = 0; i < deathBones.Length; i++)
        {
            beforeDeathPositions[i] = deathBones[i].localPosition;
            beforeDeathRotations[i] = deathBones[i].localRotation;
        }
        deathApplied = true;
    }

    void ApplyDeathPose()
    {
        for (int i = 0; i < deathBones.Length; i++)
            deathBones[i].SetLocalPositionAndRotation(deathPositions[i], deathRotations[i]);

        float weight = Mathf.SmoothStep(0f, 1f, DeathWeight);
        float height = rig.Height;
        Vector3 forward = actor.transform.TransformDirection(deathForward).normalized;
        Vector3 left = -Vector3.Cross(Vector3.up, forward);
        Vector3 pivot = rig.Hips.position;
        // Roll over the bracing left hand into a curled side-resting position.
        // Rotating the shared Hip ancestor carries both Pelvis and Waist.
        Quaternion roll = Quaternion.AngleAxis(96f * weight, forward);
        bodyRoot.position = pivot + roll * (bodyRoot.position - pivot)
            + left * (height * .08f * weight) - Vector3.up * (height * .11f * weight);
        bodyRoot.rotation = roll * bodyRoot.rotation;

        // Conservative joint radii keep the head, shoulder and limbs above the
        // floor without querying or baking high-poly skinned meshes.
        float lowest = Mathf.Min(rig.Hips.position.y - height * .075f,
            rig.Head.position.y - height * .075f);
        for (int i = 0; i < 2; i++)
        {
            lowest = Mathf.Min(lowest, arms[i].position.y - height * .055f);
            lowest = Mathf.Min(lowest, elbows[i].position.y - height * .035f);
            lowest = Mathf.Min(lowest, hands[i].position.y - height * .025f);
            lowest = Mathf.Min(lowest, knees[i].position.y - height * .04f);
            lowest = Mathf.Min(lowest, feet[i].position.y - height * .03f);
        }
        float lift = Mathf.Max(0f, actor.GroundHeight + .015f - lowest);
        float groundBlend = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(DeathWeight / .18f));
        bodyRoot.position += Vector3.up * (lift * groundBlend);
        RefreshFaceCut();
    }

    void RefreshFaceCut()
    {
        if (!speechFace || speechFace.surfaces == null) return;
        if (faceBlock == null) faceBlock = new MaterialPropertyBlock();
        // SpeechFace runs before this bone layer. Move its existing face mask
        // with the final side-lying head instead of leaving a standing-head cut.
        foreach (var surface in speechFace.surfaces)
        {
            if (surface == null || !surface.head || !surface.body) continue;
            surface.body.GetPropertyBlock(faceBlock);
            faceBlock.SetVector("_StoryCutOrigin", surface.head.TransformPoint(surface.origin));
            faceBlock.SetVector("_StoryCutRight", InverseAxis(surface.head.TransformVector(surface.right)));
            faceBlock.SetVector("_StoryCutUp", InverseAxis(surface.head.TransformVector(surface.up)));
            faceBlock.SetVector("_StoryCutForward", InverseAxis(surface.head.TransformVector(surface.forward)));
            faceBlock.SetFloat("_StoryCutEnabled", 1f);
            surface.body.SetPropertyBlock(faceBlock);
        }
    }

    static Vector3 InverseAxis(Vector3 axis) => axis / Mathf.Max(.00001f, axis.sqrMagnitude);

    bool Prepare()
    {
        if (prepared) return actor && rig;
        if (!actor) actor = GetComponent<Chapter2Actor>();
        if (!actor || !actor.Rig || !actor.Rig.animator || !actor.Rig.Hips || !actor.Rig.Head) return false;
        rig = actor.Rig;
        speechFace = actor.GetComponent<Chapter2SpeechFace>();
        var animator = rig.animator;
        spine = Chapter1WeddingRigBones.Resolve(animator, HumanBodyBones.Spine, "Spine", "Spine1", "Waist");
        thighs[0] = Chapter1WeddingRigBones.Resolve(animator, HumanBodyBones.LeftUpperLeg, "L_Thigh", "LeftUpLeg");
        thighs[1] = Chapter1WeddingRigBones.Resolve(animator, HumanBodyBones.RightUpperLeg, "R_Thigh", "RightUpLeg");
        knees[0] = Chapter1WeddingRigBones.Resolve(animator, HumanBodyBones.LeftLowerLeg, "L_Calf", "LeftLeg");
        knees[1] = Chapter1WeddingRigBones.Resolve(animator, HumanBodyBones.RightLowerLeg, "R_Calf", "RightLeg");
        feet[0] = Chapter1WeddingRigBones.Resolve(animator, HumanBodyBones.LeftFoot, "L_Foot", "LeftFoot");
        feet[1] = Chapter1WeddingRigBones.Resolve(animator, HumanBodyBones.RightFoot, "R_Foot", "RightFoot");
        arms[0] = Chapter1WeddingRigBones.Resolve(animator, HumanBodyBones.LeftUpperArm, "L_Upperarm", "LeftArm");
        arms[1] = Chapter1WeddingRigBones.Resolve(animator, HumanBodyBones.RightUpperArm, "R_Upperarm", "RightArm");
        elbows[0] = Chapter1WeddingRigBones.Resolve(animator, HumanBodyBones.LeftLowerArm, "L_Forearm", "LeftForeArm");
        elbows[1] = Chapter1WeddingRigBones.Resolve(animator, HumanBodyBones.RightLowerArm, "R_Forearm", "RightForeArm");
        hands[0] = rig.LeftHand;
        hands[1] = rig.RightHand;
        for (int i = 0; i < 2; i++)
            if (!thighs[i] || !knees[i] || !feet[i] || !arms[i] || !elbows[i] || !hands[i]) return false;
        bodyRoot = rig.Hips;
        while (bodyRoot && !rig.Head.IsChildOf(bodyRoot) && bodyRoot.parent
            && bodyRoot.parent != actor.transform && bodyRoot.parent != animator.transform)
            bodyRoot = bodyRoot.parent;
        // Never translate a scene/model root as a fallback for an unsupported rig.
        if (!bodyRoot || !rig.Head.IsChildOf(bodyRoot)) return false;

        var bones = new List<Transform> { bodyRoot };
        if (spine && !bones.Contains(spine)) bones.Add(spine);
        if (!bones.Contains(rig.Head)) bones.Add(rig.Head);
        for (int i = 0; i < 2; i++)
        {
            foreach (Transform bone in new[] { thighs[i], knees[i], feet[i], arms[i], elbows[i], hands[i] })
                if (!bones.Contains(bone)) bones.Add(bone);
            neutralAnkles[i] = actor.transform.InverseTransformPoint(feet[i].position);
        }
        posedBones = bones.ToArray();
        beforePositions = new Vector3[posedBones.Length];
        beforeRotations = new Quaternion[posedBones.Length];
        previousPosition = actor.transform.position;
        prepared = true;
        return true;
    }

    void CapturePose()
    {
        for (int i = 0; i < posedBones.Length; i++)
        {
            beforePositions[i] = posedBones[i].localPosition;
            beforeRotations[i] = posedBones[i].localRotation;
        }
        poseApplied = true;
    }

    void RestoreLastPose()
    {
        bool restoreFace = deathApplied;
        if (deathApplied && deathBones != null)
        {
            for (int i = 0; i < deathBones.Length; i++)
                if (deathBones[i]) deathBones[i].SetLocalPositionAndRotation(beforeDeathPositions[i], beforeDeathRotations[i]);
            deathApplied = false;
        }
        if (poseApplied && posedBones != null)
            for (int i = 0; i < posedBones.Length; i++)
                if (posedBones[i]) posedBones[i].SetLocalPositionAndRotation(beforePositions[i], beforeRotations[i]);
        poseApplied = false;
        if (restoreFace) RefreshFaceCut();
    }

    void OnDisable() { ResetPose(); }
}
