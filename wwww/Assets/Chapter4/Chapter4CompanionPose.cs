using UnityEngine;

// Chapter-specific poses are layered after the shared rig has restored its
// calibrated bones. Neither the donor models nor the shared rig are modified.
[DefaultExecutionOrder(4700)]
public sealed class Chapter4CompanionPose : MonoBehaviour
{
    public enum State { Normal, Coughing, SideFall, Helping, Supported, Supporting, Seated }
    public State CurrentState { get; private set; }
    public float Weight { get; private set; }
    public float Clock { get; private set; }
    public Transform LeftShoulder => leftArm;
    public Transform RightShoulder => rightArm;
    public bool Ready => actor && spine && leftArm && leftElbow && leftHand && rightArm && rightElbow && rightHand && leftThigh && leftCalf && leftFoot && rightThigh && rightCalf && rightFoot;
    Chapter4Companion actor;
    Transform spine, leftArm, leftElbow, leftHand, rightArm, rightElbow, rightHand;
    Transform leftThigh, leftCalf, leftFoot, rightThigh, rightCalf, rightFoot;
    Vector3 baseModelPosition;
    Quaternion baseModelRotation;
    bool modelPosed;

    public void Initialize(Chapter4Companion companion, Animator animator)
    {
        actor = companion;
        Transform B(HumanBodyBones bone, string a, string b) => Chapter1WeddingRigBones.Resolve(animator, bone, a, b);
        spine = Chapter1WeddingRigBones.Resolve(animator, HumanBodyBones.Spine, "Spine", "Spine1", "Spine01");
        leftArm = B(HumanBodyBones.LeftUpperArm, "L_Upperarm", "LeftArm");
        leftElbow = B(HumanBodyBones.LeftLowerArm, "L_Forearm", "LeftForeArm");
        leftHand = B(HumanBodyBones.LeftHand, "L_Hand", "LeftHand");
        rightArm = B(HumanBodyBones.RightUpperArm, "R_Upperarm", "RightArm");
        rightElbow = B(HumanBodyBones.RightLowerArm, "R_Forearm", "RightForeArm");
        rightHand = B(HumanBodyBones.RightHand, "R_Hand", "RightHand");
        leftThigh = B(HumanBodyBones.LeftUpperLeg, "L_Thigh", "LeftUpLeg");
        leftCalf = B(HumanBodyBones.LeftLowerLeg, "L_Calf", "LeftLeg");
        leftFoot = B(HumanBodyBones.LeftFoot, "L_Foot", "LeftFoot");
        rightThigh = B(HumanBodyBones.RightUpperLeg, "R_Thigh", "RightUpLeg");
        rightCalf = B(HumanBodyBones.RightLowerLeg, "R_Calf", "RightLeg");
        rightFoot = B(HumanBodyBones.RightFoot, "R_Foot", "RightFoot");
    }

    public void Set(State state, float weight = 1, float clock = 0)
    {
        CurrentState = state; Weight = Mathf.Clamp01(weight); Clock = clock;
    }

    // Some supplied generic avatars have separate pelvis and torso roots.
    // Translating only the pelvis stretches their clothes and leaves the head
    // upright. Move/roll the model as a whole, then solve the planted limbs.
    // Restore before the shared gait/grounder runs to prevent accumulation.
    public void RestoreModelBase()
    {
        if (!modelPosed || !actor || !actor.model) return;
        actor.model.localPosition = baseModelPosition;
        actor.model.localRotation = baseModelRotation;
        modelPosed = false;
    }

    void LateUpdate()
    {
        if (!actor || !actor.Rig || !spine || !leftThigh || !rightThigh) return;
        if (!actor.IsPaused) Clock += Time.deltaTime;
        if (CurrentState == State.Normal) return;
        baseModelPosition = actor.model.localPosition;
        baseModelRotation = actor.model.localRotation;
        modelPosed = true;
        var rig = actor.Rig;
        Vector3 forward = rig.Forward, right = Vector3.Cross(Vector3.up, forward);
        Vector3 origin = actor.transform.position;
        float height = rig.Height, w = Weight;
        if (CurrentState == State.SideFall)
        {
            Vector3 pivot = rig.Hips.position;
            actor.model.RotateAround(pivot, forward, -78f * w);
            actor.model.position -= Vector3.up * ((pivot.y - origin.y - .28f) * w);
            Plant(leftThigh, leftCalf, leftFoot, origin + forward * .48f - right * .27f + Vector3.up * .11f, forward + Vector3.up, w);
            Plant(rightThigh, rightCalf, rightFoot, origin + forward * .16f - right * .52f + Vector3.up * .12f, forward + Vector3.up, w);
            Arm(false, Vector3.Lerp(rightHand.position, origin + right * .63f + forward * .16f + Vector3.up * .13f, w), -forward);
            Arm(true, Vector3.Lerp(leftHand.position, rig.Head.position - forward * .08f + Vector3.up * .04f, w), forward);
        }
        else if (CurrentState == State.Seated)
        {
            actor.model.position -= Vector3.up * ((rig.Hips.position.y - origin.y - .29f) * w);
            spine.rotation = Quaternion.AngleAxis(16f * w, right) * spine.rotation;
            Plant(leftThigh, leftCalf, leftFoot, origin + forward * .52f - right * .19f + Vector3.up * .10f, forward + Vector3.up, w);
            Plant(rightThigh, rightCalf, rightFoot, origin + forward * .44f + right * .20f + Vector3.up * .10f, forward + Vector3.up, w);
            Arm(true, Vector3.Lerp(leftHand.position, leftCalf.position + forward * .09f, w), -right);
            Cough(forward, right, w * .55f);
        }
        else if (CurrentState == State.Helping)
        {
            Vector3 left = leftFoot.position, rightFootTarget = rightFoot.position;
            actor.model.position -= Vector3.up * height * .22f * w;
            spine.rotation = Quaternion.AngleAxis(30f * w, right) * spine.rotation;
            Plant(leftThigh, leftCalf, leftFoot, left, forward, 1);
            Plant(rightThigh, rightCalf, rightFoot, rightFootTarget, forward, 1);
            var partner = actor.SupportPartner;
            if (partner && partner.Pose)
            {
                Arm(true, Vector3.Lerp(leftHand.position, partner.Pose.RightShoulder.position, w), Vector3.down);
                Arm(false, Vector3.Lerp(rightHand.position, partner.Rig.Hips.position + Vector3.up * .18f, w), Vector3.down);
            }
        }
        else if (CurrentState == State.Supported)
        {
            spine.rotation = Quaternion.AngleAxis(-10, forward) * Quaternion.AngleAxis(9, right) * spine.rotation;
            var helper = actor.SupportFollower;
            if (helper && helper.Pose)
            {
                Arm(false, helper.Pose.LeftShoulder.position + Vector3.up * .05f, -forward);
                OrientPalm(false, helper.Rig.Forward - Vector3.up * .55f, Vector3.down);
            }
            Arm(true, spine.position + forward * .17f - Vector3.up * .04f, Vector3.down);
        }
        else if (CurrentState == State.Supporting)
        {
            var partner = actor.SupportPartner;
            if (partner)
            {
                Arm(true, partner.Rig.Hips.position + Vector3.up * .19f + forward * .03f, -forward);
                OrientPalm(true, forward - Vector3.up * .45f, -right);
            }
        }
        else Cough(forward, right, w);
    }

    void Cough(Vector3 forward, Vector3 right, float weight)
    {
        float pulse = Mathf.Max(0, Mathf.Sin(Clock * 7.5f));
        spine.rotation = Quaternion.AngleAxis((14 + pulse * 6) * weight, right) * spine.rotation;
        actor.Rig.Head.rotation = Quaternion.AngleAxis(9f * weight, right) * actor.Rig.Head.rotation;
        Arm(false, Vector3.Lerp(rightHand.position, actor.Rig.Head.position + forward * .12f - Vector3.up * .13f, weight), -forward + Vector3.down);
    }

    void Arm(bool left, Vector3 target, Vector3 bend)
    {
        var a = left ? leftArm : rightArm; var b = left ? leftElbow : rightElbow; var c = left ? leftHand : rightHand;
        if (a && b && c) Chapter1IncidentRig.Solve(a, b, c, target, bend);
    }

    void OrientPalm(bool left, Vector3 fingers, Vector3 normal)
    {
        var hand = left ? leftHand : rightHand;
        if (hand) hand.rotation = Quaternion.LookRotation(fingers, normal) * Quaternion.Inverse(actor.Rig.PalmFrame(left));
    }

    static void Plant(Transform thigh, Transform calf, Transform foot, Vector3 target, Vector3 bend, float weight)
    {
        if (thigh && calf && foot) Chapter1IncidentRig.Solve(thigh, calf, foot, Vector3.Lerp(foot.position, target, weight), bend);
    }
}
