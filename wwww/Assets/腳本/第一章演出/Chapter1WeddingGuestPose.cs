using System.Collections;
using UnityEngine;

// Layer the invitation and the cup hand over the wedding idle and the later crowd reaction.
[DefaultExecutionOrder(5100)]
public sealed class Chapter1WeddingGuestPose : MonoBehaviour
{
    public Transform Head { get; private set; }
    public float Height { get; private set; }
    public Transform Cup { get; private set; }
    public bool CanHoldCup => Head && rightArm && rightElbow && rightHand;
    public Vector3 Forward => transform.TransformDirection(localForward);
    public Vector3 Right => Vector3.Cross(Vector3.up, Forward);
    public Vector3 CupGrip => rightArm.position + (Forward * .15f + Right * .035f - Vector3.up * .17f) * Height;
    private Transform leftArm, leftElbow, leftHand, rightArm, rightElbow, rightHand;
    private Vector3 localForward = Vector3.forward;
    private float waveStart = float.NegativeInfinity;
    private float lower;
    private Vector3 restingPoint;
    private Vector3 cupBeforeLowering;
    private Transform hips;
    private FootStep plantedLeft, plantedRight;
    private Chapter1IncidentRig reactionRig;
    private FootStep leadingFoot, trailingFoot;
    private Vector3 stepDelta;
    private float stepPhase;

    private void Awake()
    {
        Animator animator = GetComponentInChildren<Animator>();
        Head = Chapter1WeddingRigBones.Resolve(animator, HumanBodyBones.Head, "Head");
        hips = Chapter1WeddingRigBones.Resolve(animator, HumanBodyBones.Hips, "Hips", "Pelvis");
        Transform foot = Chapter1WeddingRigBones.Resolve(animator, HumanBodyBones.LeftFoot, "L_Foot", "LeftFoot");
        leftArm = Chapter1WeddingRigBones.Resolve(animator, HumanBodyBones.LeftUpperArm, "L_Upperarm", "LeftArm");
        leftElbow = Chapter1WeddingRigBones.Resolve(animator, HumanBodyBones.LeftLowerArm, "L_Forearm", "LeftForeArm");
        leftHand = Chapter1WeddingRigBones.Resolve(animator, HumanBodyBones.LeftHand, "L_Hand", "LeftHand");
        rightArm = Chapter1WeddingRigBones.Resolve(animator, HumanBodyBones.RightUpperArm, "R_Upperarm", "RightArm");
        rightElbow = Chapter1WeddingRigBones.Resolve(animator, HumanBodyBones.RightLowerArm, "R_Forearm", "RightForeArm");
        rightHand = Chapter1WeddingRigBones.Resolve(animator, HumanBodyBones.RightHand, "R_Hand", "RightHand");
        Height = Head && foot ? Mathf.Max(.5f, (Head.position.y - foot.position.y) * 1.09f) : 1.7f;
    }

    public void Face(Vector3 direction)
    {
        direction = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
        if (direction.sqrMagnitude > .5f) localForward = transform.InverseTransformDirection(direction);
    }

    public void Beckon(float secondsIntoWave = 0f) => waveStart = Time.time - secondsIntoWave;
    public void AcceptCup(Transform cup) => Cup = cup;
    public void FinishInvitation()
    {
        waveStart = float.NegativeInfinity;
        leadingFoot = trailingFoot = null;
    }

    public IEnumerator StepAside(Vector3 delta)
    {
        var animator = GetComponentInChildren<Animator>();
        bool rightLeads = Vector3.Dot(delta, Right) > 0f;
        leadingFoot = new FootStep(animator, rightLeads);
        trailingFoot = new FootStep(animator, !rightLeads);
        stepDelta = delta;
        Vector3 start = transform.position;
        for (float elapsed = 0f; elapsed < .95f; elapsed += Time.deltaTime)
        {
            stepPhase = elapsed / .95f;
            Vector3 position = start + delta * Mathf.SmoothStep(0f, 1f, stepPhase);
            position.y = transform.position.y;
            transform.position = position;
            yield return null;
        }
        transform.position = new Vector3(start.x + delta.x, transform.position.y, start.z + delta.z);
        leadingFoot = trailingFoot = null;
    }

    private sealed class FootStep
    {
        private readonly Transform thigh, shin, foot;
        private readonly Vector3 start;
        private readonly Quaternion footRotation;

        public FootStep(Animator animator, bool right)
        {
            thigh = Chapter1WeddingRigBones.Resolve(animator, right ? HumanBodyBones.RightUpperLeg : HumanBodyBones.LeftUpperLeg,
                right ? "R_Thigh" : "L_Thigh");
            shin = Chapter1WeddingRigBones.Resolve(animator, right ? HumanBodyBones.RightLowerLeg : HumanBodyBones.LeftLowerLeg,
                right ? "R_Calf" : "L_Calf");
            foot = Chapter1WeddingRigBones.Resolve(animator, right ? HumanBodyBones.RightFoot : HumanBodyBones.LeftFoot,
                right ? "R_Foot" : "L_Foot");
            if (foot) { start = foot.position; footRotation = foot.rotation; }
        }

        public void Place(Vector3 delta, float phase, Vector3 forward)
        {
            if (!foot) return;
            float t = Mathf.Clamp01(phase);
            Vector3 point = start + delta * Mathf.SmoothStep(0f, 1f, t) + Vector3.up * (Mathf.Sin(Mathf.PI * t) * delta.magnitude * .27f);
            Chapter1IncidentRig.Solve(thigh, shin, foot, point, forward);
            foot.rotation = footRotation;
        }
    }

    public IEnumerator RestCupOn(Vector3 surface)
    {
        var animator = GetComponentInChildren<Animator>();
        plantedLeft = new FootStep(animator, false);
        plantedRight = new FootStep(animator, true);
        reactionRig = GetComponent<Chapter1IncidentRig>();
        cupBeforeLowering = Cup.position;
        restingPoint = surface;
        for (float t = 0f; t < 1.05f; t += Time.deltaTime)
        {
            lower = Mathf.SmoothStep(0f, 1f, t / 1.05f);
            yield return null;
        }
        lower = 1f;
        yield return null;
        if (Cup) Cup.position = surface;
        Cup = null;
        for (float t = 0f; t < .48f; t += Time.deltaTime)
        {
            lower = 1f - Mathf.SmoothStep(0f, 1f, t / .48f);
            yield return null;
        }
        lower = 0f;
        plantedLeft = plantedRight = null;
        reactionRig = null;
    }

    private void LateUpdate()
    {
        if (leadingFoot != null)
        {
            leadingFoot.Place(stepDelta, stepPhase / .55f, Forward);
            trailingFoot.Place(stepDelta, (stepPhase - .45f) / .55f, Forward);
        }
        Vector3 side = Right;
        float phase = (Time.time - waveStart) / 2.1f;
        if (leftHand && phase >= 0f && phase < 1f)
        {
            float weight = Mathf.SmoothStep(0f, 1f, Mathf.Min(phase * 5f, (1f - phase) * 4f));
            float sweep = Mathf.Sin(phase * Mathf.PI * 5f);
            Vector3 palm = leftArm.position + (Forward * (.105f - .035f * sweep) - side * (.085f + .035f * sweep)
                + Vector3.up * .12f) * Height;
            Chapter1IncidentRig.Solve(leftArm, leftElbow, leftHand,
                Vector3.Lerp(leftHand.position, palm, weight), -side - Vector3.up * .3f);
        }
        bool settingDown = plantedLeft != null;
        if ((!Cup && !settingDown) || !rightHand) return;
        if (settingDown && hips)
        {
            // Bend through the knees to reach the wood, keeping both soles planted.
            hips.position -= Vector3.up * (.082f * Height * lower);
            plantedLeft.Place(Vector3.zero, 0f, Forward);
            plantedRight.Place(Vector3.zero, 0f, Forward);
        }
        // Cup origin is its base. Keep the open rim upright, with the palm beside it.
        Vector3 bottom = settingDown ? Vector3.Lerp(cupBeforeLowering, restingPoint, lower) : CupGrip;
        Vector3 palmOffset = (-Forward * .026f + side * .024f + Vector3.up * .040f) * Height;
        Vector3 wristTarget = bottom + palmOffset;
        Quaternion wristRotation = rightHand.rotation;
        if (reactionRig && reactionRig.Height > 0f)
        {
            Vector3 fingers = (Forward - side * .28f - Vector3.up * .14f).normalized;
            Quaternion grip = Quaternion.LookRotation(fingers, Vector3.ProjectOnPlane(-side, fingers))
                * Quaternion.Inverse(reactionRig.PalmFrame(false));
            float weight = Mathf.SmoothStep(0f, 1f, lower * 3f);
            wristRotation = Quaternion.Slerp(rightHand.rotation, grip, weight);
            Vector3 wristToPalm = wristRotation * Quaternion.Inverse(rightHand.rotation)
                * rightHand.TransformVector(reactionRig.PalmCenterLocal(false));
            Vector3 contact = bottom + (Vector3.up * .046f + side * .022f) * Height;
            wristTarget = Vector3.Lerp(wristTarget, contact - wristToPalm, weight);
        }
        Chapter1IncidentRig.Solve(rightArm, rightElbow, rightHand, wristTarget, side - Vector3.up);
        if (reactionRig) rightHand.rotation = wristRotation;
        if (Cup) Cup.SetPositionAndRotation(settingDown ? bottom : rightHand.position - palmOffset,
            Quaternion.LookRotation(Forward, Vector3.up));
    }
}
