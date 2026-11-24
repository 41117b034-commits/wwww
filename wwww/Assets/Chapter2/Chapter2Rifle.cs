using UnityEngine;

// Chapter 2 only: shoulder stock, two hand grips, barrel aim and a single report.
[DefaultExecutionOrder(4300)]
public sealed class Chapter2Rifle : MonoBehaviour
{
    public Transform weapon, muzzle;
    public GameObject muzzleFlash;
    public int modelVersion;
    public Chapter2Actor target;
    [Range(0, 1)] public float aim;
    [Range(0, 1)] public float lowered;
    public Transform aimTarget;
    [Header("自然持槍姿勢")]
    public bool naturalCarry = true;
    [Tooltip("相對骨盆的槍托位置（公尺）：X 右側、Y 高度、Z 前方。")]
    public Vector3 carryStockOffset = new Vector3(.14f, .16f, -.14f);
    [Tooltip("待機槍口方向：X 右側、Y 上方、Z 前方。")]
    public Vector3 carryDirection = new Vector3(-.12f, .48f, .85f);
    [Tooltip("右手握點，相對槍模型。不同槍模型可微調。")]
    public Vector3 rightGripOffset = new Vector3(0, -.045f, .24f);
    public Vector3 leftGripOffset = new Vector3(0, -.028f, .50f);
    [Tooltip("選用：可用槍模型上的空物件指定實際握點。未指定沿用 Offset。")]
    public Transform rightGripTarget, leftGripTarget;
    [Range(0, .015f)] public float breathingAmount = .003f;
    public int Shots { get; private set; }
    public float RightGripError { get; private set; }
    public float LeftGripError { get; private set; }
    public float AimError { get; private set; }
    Chapter2Actor actor;
    Transform la, le, ra, re;
    float firedAt = -100;
    void Start()
    {
        actor = GetComponent<Chapter2Actor>();
        var a = GetComponentInChildren<Animator>();
        la = Chapter1WeddingRigBones.Resolve(a, HumanBodyBones.LeftUpperArm, "L_Upperarm", "LeftArm");
        le = Chapter1WeddingRigBones.Resolve(a, HumanBodyBones.LeftLowerArm, "L_Forearm", "LeftForeArm");
        ra = Chapter1WeddingRigBones.Resolve(a, HumanBodyBones.RightUpperArm, "R_Upperarm", "RightArm");
        re = Chapter1WeddingRigBones.Resolve(a, HumanBodyBones.RightLowerArm, "R_Forearm", "RightForeArm");
    }
    public void Fire() { Shots++; firedAt = Time.time; }
    void LateUpdate()
    {
        if (!actor || !actor.Rig || !ra || !re || !la || !le || !weapon) return;
        var rig = actor.Rig;
        if (!rig.Hips || !rig.LeftHand || !rig.RightHand) return;
        if (actor.fallen)
        {
            // Do not overwrite the existing collapse arm pose.
            if (muzzleFlash) muzzleFlash.SetActive(false);
            return;
        }
        Vector3 forward = rig.Forward, right = Vector3.Cross(Vector3.up, forward);
        float since = Time.time - firedAt;
        float recoil = since < .28f ? Mathf.Sin(Mathf.Clamp01(since / .28f) * Mathf.PI) : 0;
        float aimWeight = Mathf.Clamp01(aim), lowerWeight = Mathf.Clamp01(lowered);
        float relaxed = naturalCarry ? (1 - aimWeight) * (1 - lowerWeight) : 0;
        Vector3 originalStock = rig.Hips.position + right * .18f + Vector3.up * .02f;
        Vector3 carryStock = rig.Hips.position + right * carryStockOffset.x + Vector3.up * carryStockOffset.y + forward * carryStockOffset.z;
        Vector3 idleStock = Vector3.Lerp(originalStock, carryStock, relaxed);
        // Gentle motion in the target, not added cumulatively to skeleton transforms.
        idleStock += Vector3.up * (Mathf.Sin(Time.time * 1.45f) * breathingAmount * relaxed);
        Vector3 stock = Vector3.Lerp(idleStock, ra.position - right * .08f - forward * .10f - Vector3.up * .08f, aimWeight);
        Vector3 destination = aimTarget ? aimTarget.position : target ? target.transform.position + Vector3.up * 1.17f : stock + forward * 4;
        Vector3 restDirection = Vector3.Slerp((forward * .5f + Vector3.up * .85f).normalized, (forward * .7f - Vector3.up * .65f).normalized, lowered);
        Vector3 naturalDirection = right * carryDirection.x + Vector3.up * carryDirection.y + forward * carryDirection.z;
        if (naturalDirection.sqrMagnitude > .001f) restDirection = Vector3.Slerp(restDirection, naturalDirection.normalized, relaxed);
        Vector3 direction = Vector3.Slerp(restDirection, (destination - stock).normalized, aimWeight);
        stock -= direction * (recoil * .055f);
        weapon.SetPositionAndRotation(stock, Quaternion.LookRotation(direction, Vector3.up) * Quaternion.Euler(-recoil * 3, 0, 0));
        // Keep elbows below their shoulders instead of flaring sideways.
        Vector3 rightBend = Vector3.Slerp(right - Vector3.up * .7f, right * .4f - Vector3.up - forward * .12f, relaxed);
        Vector3 leftBend = Vector3.Slerp(-right - Vector3.up, -right * .3f - Vector3.up - forward * .12f, relaxed);
        Vector3 rightPoint = rightGripTarget ? rightGripTarget.position : weapon.TransformPoint(rightGripOffset);
        Vector3 leftPoint = leftGripTarget ? leftGripTarget.position : weapon.TransformPoint(leftGripOffset);
        rig.ApplyPropGrip(false, rightPoint, -weapon.up, -weapon.right, rightBend);
        rig.ApplyPropGrip(true, leftPoint, weapon.right, weapon.up, leftBend);
        RightGripError = Vector3.Distance(rig.PropPalmPosition(false), rightPoint);
        LeftGripError = Vector3.Distance(rig.PropPalmPosition(true), leftPoint);
        AimError = Vector3.Angle(weapon.forward, destination - (muzzle ? muzzle.position : weapon.position));
        if (muzzleFlash) muzzleFlash.SetActive(since >= 0 && since < .12f);
    }
    void OnDrawGizmosSelected()
    {
        if (!weapon) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(rightGripTarget ? rightGripTarget.position : weapon.TransformPoint(rightGripOffset), .025f);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(leftGripTarget ? leftGripTarget.position : weapon.TransformPoint(leftGripOffset), .025f);
    }
}
