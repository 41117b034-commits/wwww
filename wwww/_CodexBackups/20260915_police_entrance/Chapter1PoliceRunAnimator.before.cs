using System;
using UnityEngine;

[DefaultExecutionOrder(1100)]
public sealed class Chapter1PoliceRunAnimator : MonoBehaviour
{
    public Animator animator;
    public float cadence = 2.45f;
    [Range(0f, 1f)] public float legSwing = 0.62f;
    [Range(0f, 1f)] public float kneeBend = 0.55f;
    [Range(0f, 1f)] public float armSwing = 0.58f;
    [Range(0f, 1f)] public float elbowBend = 0.38f;
    public float torsoLeanDegrees = 9f;
    public float blendSeconds = 0.14f;

    private HumanPoseHandler poseHandler;
    private HumanPose pose;
    private float[] baseMuscles;
    private Transform spine;
    private bool running;
    private bool poseApplied;
    private float runStartedAt;
    private float phaseOffset;
    private float blend;

    private int leftUpperLeg = -1;
    private int rightUpperLeg = -1;
    private int leftLowerLeg = -1;
    private int rightLowerLeg = -1;
    private int leftArm = -1;
    private int rightArm = -1;
    private int leftForearm = -1;
    private int rightForearm = -1;
    private int chestTwist = -1;

    public bool Configure(
        Animator sourceAnimator,
        float cyclesPerSecond,
        float strideAmount,
        float kneeAmount,
        float armAmount,
        float forwardLeanDegrees)
    {
        animator = sourceAnimator;
        cadence = Mathf.Max(0.5f, cyclesPerSecond);
        legSwing = Mathf.Clamp01(strideAmount);
        kneeBend = Mathf.Clamp01(kneeAmount);
        armSwing = Mathf.Clamp01(armAmount);
        torsoLeanDegrees = forwardLeanDegrees;
        return EnsureInitialized();
    }

    public bool BeginRun(float startPhaseOffset)
    {
        if (!EnsureInitialized())
        {
            return false;
        }

        CaptureBasePose();
        phaseOffset = startPhaseOffset;
        runStartedAt = Time.time;
        running = true;
        poseApplied = true;
        enabled = true;
        return true;
    }

    public void EndRun()
    {
        running = false;
    }

    private void LateUpdate()
    {
        if (!EnsureInitialized() || baseMuscles == null)
        {
            return;
        }

        float blendDuration = Mathf.Max(0.02f, blendSeconds);
        blend = Mathf.MoveTowards(blend, running ? 1f : 0f, Time.deltaTime / blendDuration);

        if (!running && blend <= 0.001f)
        {
            RestoreBasePose();
            enabled = false;
            return;
        }

        poseHandler.GetHumanPose(ref pose);
        if (pose.muscles == null || pose.muscles.Length != baseMuscles.Length)
        {
            return;
        }

        float phase = (Time.time - runStartedAt) * cadence * Mathf.PI * 2f + phaseOffset;
        float stride = Mathf.Sin(phase);
        float leftKneeLift = Mathf.Max(0f, -stride);
        float rightKneeLift = Mathf.Max(0f, stride);

        SetMuscle(leftUpperLeg, stride * legSwing * blend);
        SetMuscle(rightUpperLeg, -stride * legSwing * blend);
        SetMuscle(leftLowerLeg, -leftKneeLift * kneeBend * blend);
        SetMuscle(rightLowerLeg, -rightKneeLift * kneeBend * blend);

        SetMuscle(leftArm, -stride * armSwing * blend);
        SetMuscle(rightArm, stride * armSwing * blend);
        SetMuscle(leftForearm, -(elbowBend + rightKneeLift * 0.12f) * blend);
        SetMuscle(rightForearm, -(elbowBend + leftKneeLift * 0.12f) * blend);
        SetMuscle(chestTwist, stride * 0.1f * blend);

        poseHandler.SetHumanPose(ref pose);

        if (spine != null && Mathf.Abs(torsoLeanDegrees) > 0.01f)
        {
            spine.rotation = Quaternion.AngleAxis(
                torsoLeanDegrees * blend,
                animator.transform.right) * spine.rotation;
        }
    }

    private bool EnsureInitialized()
    {
        if (poseHandler != null)
        {
            return true;
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (animator == null
            || animator.avatar == null
            || !animator.avatar.isValid
            || !animator.isHuman)
        {
            return false;
        }

        poseHandler = new HumanPoseHandler(animator.avatar, animator.transform);
        spine = animator.GetBoneTransform(HumanBodyBones.Spine);
        leftUpperLeg = FindMuscle("Left Upper Leg Front-Back");
        rightUpperLeg = FindMuscle("Right Upper Leg Front-Back");
        leftLowerLeg = FindMuscle("Left Lower Leg Stretch");
        rightLowerLeg = FindMuscle("Right Lower Leg Stretch");
        leftArm = FindMuscle("Left Arm Front-Back");
        rightArm = FindMuscle("Right Arm Front-Back");
        leftForearm = FindMuscle("Left Forearm Stretch");
        rightForearm = FindMuscle("Right Forearm Stretch");
        chestTwist = FindMuscle("Chest Twist Left-Right");
        return true;
    }

    private void CaptureBasePose()
    {
        poseHandler.GetHumanPose(ref pose);
        baseMuscles = pose.muscles != null ? (float[])pose.muscles.Clone() : null;
    }

    private void SetMuscle(int index, float offset)
    {
        if (index < 0 || index >= pose.muscles.Length)
        {
            return;
        }

        pose.muscles[index] = Mathf.Clamp(baseMuscles[index] + offset, -1f, 1f);
    }

    private void RestoreBasePose()
    {
        if (!poseApplied || poseHandler == null || baseMuscles == null)
        {
            return;
        }

        poseHandler.GetHumanPose(ref pose);
        if (pose.muscles != null && pose.muscles.Length == baseMuscles.Length)
        {
            Array.Copy(baseMuscles, pose.muscles, baseMuscles.Length);
            poseHandler.SetHumanPose(ref pose);
        }

        poseApplied = false;
    }

    private static int FindMuscle(string muscleName)
    {
        string[] names = HumanTrait.MuscleName;
        for (int i = 0; i < names.Length; i++)
        {
            if (names[i] == muscleName)
            {
                return i;
            }
        }

        return -1;
    }

    private void OnDisable()
    {
        if (!running)
        {
            RestoreBasePose();
        }
    }

    private void OnDestroy()
    {
        RestoreBasePose();
        poseHandler?.Dispose();
        poseHandler = null;
    }
}
