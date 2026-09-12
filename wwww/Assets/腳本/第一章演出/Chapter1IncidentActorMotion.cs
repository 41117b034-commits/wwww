using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(4100)]
public sealed class Chapter1VictimResistanceMotion : MonoBehaviour
{
    private sealed class BlendShapeTarget
    {
        public SkinnedMeshRenderer renderer;
        public int index;
        public float originalWeight;
    }

    public Animator animator;
    public Transform threat;
    [Range(0f, 1f)] public float intensity = 1f;
    public float cadence = 3.15f;

    private Transform hips;
    private Transform head;
    private Transform leftShoulder;
    private Transform rightShoulder;
    private Transform leftUpperArm;
    private Transform rightUpperArm;
    private Transform leftForearm;
    private Transform rightForearm;
    private Transform leftUpperLeg;
    private Transform rightUpperLeg;
    private readonly List<BlendShapeTarget> fearBlendShapes = new List<BlendShapeTarget>();
    private bool resisting;
    private float startedAt;

    public void BeginResistance(Animator sourceAnimator, Transform threatTarget, float strength)
    {
        animator = sourceAnimator;
        threat = threatTarget;
        intensity = Mathf.Clamp01(strength);
        CacheRig();
        CacheFearBlendShapes();
        startedAt = Time.time;
        resisting = true;
        enabled = true;
    }

    public void StopResistance()
    {
        resisting = false;
        RestoreFearBlendShapes();
        if (animator != null && animator.isActiveAndEnabled)
        {
            animator.Update(0f);
        }
        enabled = false;
    }

    private void LateUpdate()
    {
        if (!resisting || animator == null || hips == null || head == null)
        {
            return;
        }

        float phase = (Time.time - startedAt) * cadence * Mathf.PI * 2f;
        float fast = Mathf.Sin(phase * 1.37f);
        float opposite = Mathf.Sin(phase + Mathf.PI);
        float weight = Mathf.Clamp01(intensity);

        Vector3 bodyRight = GetBodyRight();
        Vector3 bodyUp = head.position - hips.position;
        Vector3 bodyForward = Vector3.Cross(bodyRight, bodyUp);
        if (bodyRight.sqrMagnitude < 0.001f || bodyForward.sqrMagnitude < 0.001f)
        {
            return;
        }

        bodyRight.Normalize();
        bodyForward.Normalize();

        // Uneven, restrained movements read as active resistance without pulling
        // joints beyond a normal humanoid range.
        RotateWorld(head, Vector3.up, fast * 11f * weight);
        RotateWorld(head, bodyRight, (-7f + Mathf.Abs(opposite) * 5f) * weight);
        RotateWorld(hips, Vector3.up, opposite * 5f * weight);

        RotateWorld(leftUpperArm, bodyForward, (-24f + fast * 18f) * weight);
        RotateWorld(rightUpperArm, bodyForward, (27f + opposite * 20f) * weight);
        RotateWorld(leftUpperArm, bodyRight, opposite * 13f * weight);
        RotateWorld(rightUpperArm, bodyRight, fast * 15f * weight);
        RotateWorld(leftForearm, bodyRight, (-18f + fast * 16f) * weight);
        RotateWorld(rightForearm, bodyRight, (-22f + opposite * 18f) * weight);

        RotateWorld(leftUpperLeg, bodyRight, fast * 9f * weight);
        RotateWorld(rightUpperLeg, bodyRight, opposite * 10f * weight);

        ApplyFearBlendShapes(65f + Mathf.Abs(fast) * 25f);
    }

    private void CacheRig()
    {
        hips = Resolve(HumanBodyBones.Hips, "Hips", "Pelvis");
        head = Resolve(HumanBodyBones.Head, "Head");
        leftShoulder = Resolve(HumanBodyBones.LeftShoulder, "L_Shoulder", "LeftShoulder");
        rightShoulder = Resolve(HumanBodyBones.RightShoulder, "R_Shoulder", "RightShoulder");
        leftUpperArm = Resolve(HumanBodyBones.LeftUpperArm, "L_Upperarm", "LeftArm");
        rightUpperArm = Resolve(HumanBodyBones.RightUpperArm, "R_Upperarm", "RightArm");
        leftForearm = Resolve(HumanBodyBones.LeftLowerArm, "L_Forearm", "LeftForeArm");
        rightForearm = Resolve(HumanBodyBones.RightLowerArm, "R_Forearm", "RightForeArm");
        leftUpperLeg = Resolve(HumanBodyBones.LeftUpperLeg, "L_Thigh", "LeftUpLeg");
        rightUpperLeg = Resolve(HumanBodyBones.RightUpperLeg, "R_Thigh", "RightUpLeg");
    }

    private Transform Resolve(HumanBodyBones humanoidBone, params string[] aliases)
    {
        return Chapter1WeddingRigBones.Resolve(animator, humanoidBone, aliases);
    }

    private Vector3 GetBodyRight()
    {
        Transform left = leftShoulder != null ? leftShoulder : leftUpperArm;
        Transform right = rightShoulder != null ? rightShoulder : rightUpperArm;
        return left != null && right != null
            ? right.position - left.position
            : transform.right;
    }

    private static void RotateWorld(Transform bone, Vector3 axis, float degrees)
    {
        if (bone == null || axis.sqrMagnitude < 0.001f || Mathf.Abs(degrees) < 0.01f)
        {
            return;
        }

        bone.rotation = Quaternion.AngleAxis(degrees, axis.normalized) * bone.rotation;
    }

    private void CacheFearBlendShapes()
    {
        RestoreFearBlendShapes();
        fearBlendShapes.Clear();
        if (animator == null)
        {
            return;
        }

        string[] keywords = { "fear", "scared", "eyewide", "eye_wide", "jawopen", "jaw_open", "mouthopen", "mouth_open", "browup" };
        SkinnedMeshRenderer[] renderers = animator.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
        {
            SkinnedMeshRenderer renderer = renderers[rendererIndex];
            Mesh mesh = renderer != null ? renderer.sharedMesh : null;
            if (mesh == null)
            {
                continue;
            }

            for (int shapeIndex = 0; shapeIndex < mesh.blendShapeCount; shapeIndex++)
            {
                string normalized = mesh.GetBlendShapeName(shapeIndex).ToLowerInvariant().Replace(" ", string.Empty);
                bool matches = false;
                for (int keywordIndex = 0; keywordIndex < keywords.Length; keywordIndex++)
                {
                    if (normalized.Contains(keywords[keywordIndex]))
                    {
                        matches = true;
                        break;
                    }
                }

                if (matches)
                {
                    fearBlendShapes.Add(new BlendShapeTarget
                    {
                        renderer = renderer,
                        index = shapeIndex,
                        originalWeight = renderer.GetBlendShapeWeight(shapeIndex)
                    });
                }
            }
        }
    }

    private void ApplyFearBlendShapes(float weight)
    {
        for (int i = 0; i < fearBlendShapes.Count; i++)
        {
            BlendShapeTarget target = fearBlendShapes[i];
            if (target.renderer != null)
            {
                target.renderer.SetBlendShapeWeight(target.index, Mathf.Max(target.originalWeight, weight));
            }
        }
    }

    private void RestoreFearBlendShapes()
    {
        for (int i = 0; i < fearBlendShapes.Count; i++)
        {
            BlendShapeTarget target = fearBlendShapes[i];
            if (target.renderer != null)
            {
                target.renderer.SetBlendShapeWeight(target.index, target.originalWeight);
            }
        }
    }

    private void OnDisable()
    {
        RestoreFearBlendShapes();
    }
}

[DefaultExecutionOrder(4200)]
public sealed class Chapter1PoliceIncidentMotion : MonoBehaviour
{
    private enum MotionMode
    {
        None,
        Harass,
        BatonStrike
    }

    public Animator animator;
    public Transform target;

    private Transform rightUpperArm;
    private Transform rightForearm;
    private Transform rightHand;
    private Transform leftShoulder;
    private Transform rightShoulder;
    private Transform head;
    private Transform hips;
    private Transform targetGripBone;
    private Transform targetGripLowerBone;
    private GameObject baton;
    private MotionMode mode;
    private float strikeProgress;
    private float actorHeight = 1.7f;
    private Vector3 smoothedGripPoint;
    private bool hasSmoothedGripPoint;

    public void BeginHarassment(Animator sourceAnimator, Transform harassmentTarget)
    {
        animator = sourceAnimator;
        target = harassmentTarget;
        CacheRig();
        CacheTargetGripBone();
        hasSmoothedGripPoint = false;
        mode = MotionMode.Harass;
        enabled = true;
    }

    public void BeginBatonStrike(Animator sourceAnimator, Transform strikeTarget)
    {
        animator = sourceAnimator;
        target = strikeTarget;
        CacheRig();
        EnsureBaton();
        SetBatonVisible(true);
        strikeProgress = 0f;
        mode = MotionMode.BatonStrike;
        enabled = true;
    }

    public void SetStrikeProgress(float normalizedProgress)
    {
        strikeProgress = Mathf.Clamp01(normalizedProgress);
    }

    public void StopMotion(bool hideBaton)
    {
        mode = MotionMode.None;
        targetGripBone = null;
        targetGripLowerBone = null;
        hasSmoothedGripPoint = false;
        if (hideBaton)
        {
            SetBatonVisible(false);
        }
        if (animator != null && animator.isActiveAndEnabled)
        {
            animator.Update(0f);
        }
        enabled = false;
    }

    private void LateUpdate()
    {
        if (mode == MotionMode.None || animator == null || rightUpperArm == null)
        {
            return;
        }

        Vector3 aimPoint = GetAimPoint();
        if (mode == MotionMode.Harass)
        {
            float smoothing = 1f - Mathf.Exp(-18f * Time.deltaTime);
            smoothedGripPoint = hasSmoothedGripPoint
                ? Vector3.Lerp(smoothedGripPoint, aimPoint, smoothing)
                : aimPoint;
            hasSmoothedGripPoint = true;
            AimArmWithTwoBoneIk(smoothedGripPoint);
            PinHandToGrip(smoothedGripPoint);
        }
        else
        {
            AimBoneAt(rightUpperArm, rightForearm, aimPoint, 0.9f);
            AimBoneAt(rightForearm, rightHand, aimPoint, 0.92f);
        }
        UpdateBatonPose();
    }

    private void PinHandToGrip(Vector3 gripPoint)
    {
        if (rightHand == null)
        {
            return;
        }

        // The actor roots are staged within arm's reach. Pinning in LateUpdate
        // keeps the rendered hand on the victim after both Animators evaluate.
        rightHand.position = gripPoint;
    }

    private Vector3 GetAimPoint()
    {
        if (mode == MotionMode.Harass)
        {
            return targetGripBone != null
                ? (targetGripLowerBone != null
                    ? Vector3.Lerp(
                        targetGripBone.position,
                        targetGripLowerBone.position,
                        0.38f)
                    : targetGripBone.position)
                : (target != null
                    ? target.position + Vector3.up * (actorHeight * 0.58f)
                    : transform.position + transform.forward * actorHeight);
        }

        Vector3 shoulder = rightShoulder != null
            ? rightShoulder.position
            : rightUpperArm.position;
        Vector3 strikeTarget = target != null
            ? target.position
            : transform.position + transform.forward * actorHeight;
        Vector3 raised = shoulder
            + Vector3.up * (actorHeight * 0.38f)
            - transform.forward * (actorHeight * 0.12f)
            + transform.right * (actorHeight * 0.16f);

        if (strikeProgress < 0.42f)
        {
            return Vector3.Lerp(
                shoulder + transform.forward * actorHeight * 0.28f,
                raised,
                Mathf.SmoothStep(0f, 1f, strikeProgress / 0.42f));
        }

        if (strikeProgress < 0.78f)
        {
            return Vector3.Lerp(
                raised,
                strikeTarget,
                Mathf.SmoothStep(0f, 1f, (strikeProgress - 0.42f) / 0.36f));
        }

        return Vector3.Lerp(
            strikeTarget,
            shoulder + transform.forward * actorHeight * 0.24f,
            Mathf.SmoothStep(0f, 1f, (strikeProgress - 0.78f) / 0.22f));
    }

    private static void AimBoneAt(Transform bone, Transform child, Vector3 targetPoint, float weight)
    {
        if (bone == null || child == null)
        {
            return;
        }

        Vector3 currentDirection = child.position - bone.position;
        Vector3 desiredDirection = targetPoint - bone.position;
        if (currentDirection.sqrMagnitude < 0.0001f || desiredDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion aimed = Quaternion.FromToRotation(currentDirection, desiredDirection) * bone.rotation;
        bone.rotation = Quaternion.Slerp(bone.rotation, aimed, Mathf.Clamp01(weight));
    }

    private void AimArmWithTwoBoneIk(Vector3 targetPoint)
    {
        if (rightUpperArm == null || rightForearm == null || rightHand == null)
        {
            return;
        }

        Vector3 shoulder = rightUpperArm.position;
        float upperLength = Vector3.Distance(shoulder, rightForearm.position);
        float lowerLength = Vector3.Distance(rightForearm.position, rightHand.position);
        if (upperLength < 0.01f || lowerLength < 0.01f)
        {
            return;
        }

        Vector3 toTarget = targetPoint - shoulder;
        float rawDistance = toTarget.magnitude;
        if (rawDistance < 0.01f)
        {
            return;
        }

        Vector3 direction = toTarget / rawDistance;
        float minimumReach = Mathf.Abs(upperLength - lowerLength) + 0.002f;
        float maximumReach = upperLength + lowerLength - 0.006f;
        float distance = Mathf.Clamp(rawDistance, minimumReach, maximumReach);
        float along = (upperLength * upperLength + distance * distance - lowerLength * lowerLength)
            / (2f * distance);
        float bendAmount = Mathf.Sqrt(Mathf.Max(0f, upperLength * upperLength - along * along));

        Vector3 currentElbowOffset = rightForearm.position - shoulder;
        Vector3 bendDirection = currentElbowOffset
            - direction * Vector3.Dot(currentElbowOffset, direction);
        if (bendDirection.sqrMagnitude < 0.0001f)
        {
            bendDirection = Vector3.Cross(direction, transform.up);
        }
        if (bendDirection.sqrMagnitude < 0.0001f)
        {
            bendDirection = transform.right;
        }
        bendDirection.Normalize();

        Vector3 desiredElbow = shoulder + direction * along + bendDirection * bendAmount;
        AimBoneAt(rightUpperArm, rightForearm, desiredElbow, 0.94f);
        AimBoneAt(rightForearm, rightHand, targetPoint, 0.98f);

        if (targetGripBone != null)
        {
            rightHand.rotation = Quaternion.Slerp(
                rightHand.rotation,
                targetGripBone.rotation,
                0.28f);
        }
    }

    private void CacheRig()
    {
        rightUpperArm = Resolve(HumanBodyBones.RightUpperArm, "R_Upperarm", "RightArm");
        rightForearm = Resolve(HumanBodyBones.RightLowerArm, "R_Forearm", "RightForeArm");
        rightHand = Resolve(HumanBodyBones.RightHand, "R_Hand", "RightHand");
        leftShoulder = Resolve(HumanBodyBones.LeftShoulder, "L_Shoulder", "LeftShoulder");
        rightShoulder = Resolve(HumanBodyBones.RightShoulder, "R_Shoulder", "RightShoulder");
        head = Resolve(HumanBodyBones.Head, "Head");
        hips = Resolve(HumanBodyBones.Hips, "Hips", "Pelvis");
        actorHeight = GetActorHeight();
    }

    private void CacheTargetGripBone()
    {
        targetGripBone = null;
        targetGripLowerBone = null;
        if (target == null)
        {
            return;
        }

        Animator targetAnimator = target.GetComponentInChildren<Animator>(true);
        if (targetAnimator != null && targetAnimator.isHuman)
        {
            Transform leftArm = targetAnimator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            Transform rightArm = targetAnimator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            Transform leftLowerArm = targetAnimator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            Transform rightLowerArm = targetAnimator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            Transform left = leftArm != null
                ? leftArm
                : targetAnimator.GetBoneTransform(HumanBodyBones.LeftShoulder);
            Transform right = rightArm != null
                ? rightArm
                : targetAnimator.GetBoneTransform(HumanBodyBones.RightShoulder);
            Vector3 handPosition = rightHand != null ? rightHand.position : transform.position;
            if (left != null && right != null)
            {
                targetGripBone = Vector3.SqrMagnitude(left.position - handPosition)
                    <= Vector3.SqrMagnitude(right.position - handPosition)
                        ? left
                        : right;
            }
            else
            {
                targetGripBone = left != null ? left : right;
            }

            targetGripLowerBone = targetGripBone == left
                ? leftLowerArm
                : (targetGripBone == right ? rightLowerArm : null);
        }

        if (targetGripBone == null)
        {
            Transform left = Chapter1WeddingRigBones.Resolve(
                targetAnimator,
                HumanBodyBones.LeftUpperArm,
                "L_Upperarm",
                "LeftArm",
                "LeftUpperArm");
            Transform right = Chapter1WeddingRigBones.Resolve(
                targetAnimator,
                HumanBodyBones.RightUpperArm,
                "R_Upperarm",
                "RightArm",
                "RightUpperArm");
            Vector3 handPosition = rightHand != null ? rightHand.position : transform.position;
            if (left != null && right != null)
            {
                targetGripBone = Vector3.SqrMagnitude(left.position - handPosition)
                    <= Vector3.SqrMagnitude(right.position - handPosition)
                        ? left
                        : right;
            }
            else
            {
                targetGripBone = left != null ? left : right;
            }
        }
    }

    private Transform Resolve(HumanBodyBones humanoidBone, params string[] aliases)
    {
        return Chapter1WeddingRigBones.Resolve(animator, humanoidBone, aliases);
    }

    private float GetActorHeight()
    {
        if (head != null && hips != null)
        {
            return Mathf.Clamp(Vector3.Distance(head.position, hips.position) * 2.2f, 1f, 30f);
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        Bounds bounds = new Bounds(transform.position, Vector3.zero);
        bool found = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || renderer.bounds.size.sqrMagnitude < 0.0001f)
            {
                continue;
            }
            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }
        return found ? Mathf.Clamp(bounds.size.y, 1f, 30f) : 1.7f;
    }

    private void EnsureBaton()
    {
        if (baton != null)
        {
            return;
        }

        baton = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baton.name = "Chapter1_RuntimePoliceBaton";
        Collider batonCollider = baton.GetComponent<Collider>();
        if (batonCollider != null)
        {
            batonCollider.enabled = false;
            Destroy(batonCollider);
        }

        float length = actorHeight * 0.34f;
        float thickness = actorHeight * 0.018f;
        baton.transform.localScale = new Vector3(thickness, length * 0.5f, thickness);

        Renderer batonRenderer = baton.GetComponent<Renderer>();
        if (batonRenderer != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }
            if (shader != null)
            {
                Material material = new Material(shader);
                Color darkWood = new Color(0.12f, 0.055f, 0.025f, 1f);
                material.color = darkWood;
                if (material.HasProperty("_BaseColor"))
                {
                    material.SetColor("_BaseColor", darkWood);
                }
                batonRenderer.material = material;
            }
        }

        SetBatonVisible(false);
    }

    private void UpdateBatonPose()
    {
        if (baton == null || !baton.activeSelf || rightHand == null)
        {
            return;
        }

        Vector3 direction = rightForearm != null
            ? rightHand.position - rightForearm.position
            : transform.forward;
        if (direction.sqrMagnitude < 0.001f)
        {
            direction = transform.forward;
        }
        direction.Normalize();

        float length = actorHeight * 0.34f;
        baton.transform.position = rightHand.position + direction * (length * 0.42f);
        baton.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);
    }

    private void SetBatonVisible(bool visible)
    {
        if (baton != null)
        {
            baton.SetActive(visible);
        }
    }

    private void OnDestroy()
    {
        if (baton != null)
        {
            Destroy(baton);
        }
    }
}
