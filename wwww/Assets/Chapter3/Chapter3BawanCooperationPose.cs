using UnityEngine;

// School-only arm performance. No full-character mesh copies or mesh baking.
[DefaultExecutionOrder(4600)]
public sealed class Chapter3BawanCooperationPose : MonoBehaviour
{
    public float CoverWeight { get; private set; }
    public float PushWeight { get; private set; }
    public Transform HandWeapon => rifle;
    Chapter2Actor actor;
    Transform rifle, threat, leftContact, rightContact, spine;
    readonly Transform[] arms = new Transform[2], elbows = new Transform[2], hands = new Transform[2];
    float coverTarget, pushTarget, firedAt = -10;
    bool prepared;

    public void Configure(Chapter2Actor source, Transform weapon)
    { ResetPose(); actor = source; rifle = weapon; prepared = false; }
    public void SetCover(Transform target, float weight)
    { threat = target; coverTarget = Mathf.Clamp01(weight); }
    public void SetPush(Transform left, Transform right, float weight)
    { leftContact = left; rightContact = right; pushTarget = Mathf.Clamp01(weight); }
    public void FireCoverShot() { firedAt = Time.time; }
    public void ResetPose()
    {
        CoverWeight = PushWeight = coverTarget = pushTarget = 0;
        threat = leftContact = rightContact = null;
        if (rifle) rifle.gameObject.SetActive(false);
    }
    void LateUpdate()
    {
        if (!Prepare()) return;
        CoverWeight = Mathf.MoveTowards(CoverWeight, coverTarget, Time.deltaTime * 5);
        PushWeight = Mathf.MoveTowards(PushWeight, pushTarget, Time.deltaTime * 5);
        if (rifle) rifle.gameObject.SetActive(CoverWeight > .01f);
        if (CoverWeight <= 0 && PushWeight <= 0) return;
        var rig = actor.Rig;
        Vector3 forward = rig.Forward, right = Vector3.Cross(Vector3.up, forward);
        if (spine && PushWeight > 0)
            spine.rotation = Quaternion.AngleAxis((9 + Mathf.Sin(Time.time * 3) * 1.5f) * PushWeight, right) * spine.rotation;
        if (rifle && CoverWeight > 0)
        {
            Vector3 origin = arms[1].position + forward * .58f - Vector3.up * .09f - right * .05f;
            Vector3 destination = threat ? threat.position + Vector3.up * 1.38f : origin + forward * 10;
            Vector3 aim = (destination - origin).normalized;
            float recoil = Time.time - firedAt < .22f ? Mathf.Sin(Mathf.Clamp01((Time.time - firedAt) / .22f) * Mathf.PI) : 0;
            rifle.SetPositionAndRotation(origin - aim * recoil * .045f, Quaternion.LookRotation(aim, Vector3.up));
            PoseArm(1, rifle.TransformPoint(new Vector3(.015f, -.05f, -.28f)), right - Vector3.up, CoverWeight);
            PoseArm(0, rifle.TransformPoint(new Vector3(0, -.035f, .07f)), -right - Vector3.up, CoverWeight);
        }
        if (PushWeight > 0)
        {
            if (leftContact) PoseArm(0, leftContact.position, -right - Vector3.up * .4f, PushWeight);
            if (rightContact) PoseArm(1, rightContact.position, right - Vector3.up * .4f, PushWeight);
        }
    }
    void PoseArm(int index, Vector3 target, Vector3 bend, float weight)
    {
        Vector3 before = hands[index].position - elbows[index].position;
        Quaternion wrist = hands[index].rotation;
        Chapter1IncidentRig.Solve(arms[index], elbows[index], hands[index], Vector3.Lerp(hands[index].position, target, weight), bend);
        hands[index].rotation = Quaternion.FromToRotation(before, hands[index].position - elbows[index].position) * wrist;
    }
    bool Prepare()
    {
        if (prepared) return actor && actor.Rig;
        if (!actor || !actor.Rig || !actor.Rig.animator) return false;
        var animator = actor.Rig.animator;
        arms[0] = Chapter1WeddingRigBones.Resolve(animator, HumanBodyBones.LeftUpperArm, "L_Upperarm", "LeftArm");
        arms[1] = Chapter1WeddingRigBones.Resolve(animator, HumanBodyBones.RightUpperArm, "R_Upperarm", "RightArm");
        elbows[0] = Chapter1WeddingRigBones.Resolve(animator, HumanBodyBones.LeftLowerArm, "L_Forearm", "LeftForeArm");
        elbows[1] = Chapter1WeddingRigBones.Resolve(animator, HumanBodyBones.RightLowerArm, "R_Forearm", "RightForeArm");
        hands[0] = actor.Rig.LeftHand; hands[1] = actor.Rig.RightHand;
        spine = Chapter1WeddingRigBones.Resolve(animator, HumanBodyBones.Spine, "Spine", "Spine1", "Waist");
        prepared = arms[0] && arms[1] && elbows[0] && elbows[1] && hands[0] && hands[1];
        return prepared;
    }
    void OnDisable() { ResetPose(); }
}
