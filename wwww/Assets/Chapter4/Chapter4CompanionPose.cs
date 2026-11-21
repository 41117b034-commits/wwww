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
    Vector3 leftAnkle, rightAnkle;
    Quaternion leftSole, rightSole;
    Quaternion leftWristRest, rightWristRest;
    Chapter3HandPose.Calibration palms;
    float leftClearance, rightClearance, seed, escapeClock, cycle, motionWeight;
    bool guardsLeft;
    public float EscapeCycle => cycle;
    public float MotionWeight => motionWeight;
    public float RecoveryWeight { get; private set; }
    public float LookBackWeight { get; private set; }
    public float Pace => CurrentState != State.Normal || !actor || actor.wounded ? 1 :
        Mathf.Clamp(.94f + .06f * Mathf.Sin(escapeClock * 1.7f + seed) - RecoveryWeight * .24f, .7f, 1);

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
        leftWristRest = Quaternion.Inverse(leftElbow.rotation) * leftHand.rotation;
        rightWristRest = Quaternion.Inverse(rightElbow.rotation) * rightHand.rotation;
        if (!Chapter3HandPose.TryGetCalibration(animator, out palms))
        {
            palms.left = actor.Rig.PalmFrame(true); palms.right = actor.Rig.PalmFrame(false);
            palms.leftLength = palms.rightLength = .1f;
        }
        leftThigh = B(HumanBodyBones.LeftUpperLeg, "L_Thigh", "LeftUpLeg");
        leftCalf = B(HumanBodyBones.LeftLowerLeg, "L_Calf", "LeftLeg");
        leftFoot = B(HumanBodyBones.LeftFoot, "L_Foot", "LeftFoot");
        rightThigh = B(HumanBodyBones.RightUpperLeg, "R_Thigh", "RightUpLeg");
        rightCalf = B(HumanBodyBones.RightLowerLeg, "R_Calf", "RightLeg");
        rightFoot = B(HumanBodyBones.RightFoot, "R_Foot", "RightFoot");
        leftAnkle = actor.model.InverseTransformPoint(leftFoot.position);
        rightAnkle = actor.model.InverseTransformPoint(rightFoot.position);
        leftSole = Quaternion.Inverse(actor.model.rotation) * leftFoot.rotation;
        rightSole = Quaternion.Inverse(actor.model.rotation) * rightFoot.rotation;
        leftClearance = leftFoot.position.y - actor.transform.position.y;
        rightClearance = rightFoot.position.y - actor.transform.position.y;
        int identity = 0;
        foreach (char letter in actor.actorId) identity = (identity * 31 + letter) % 997;
        seed = identity * .037f;
        guardsLeft = actor.actorId == "pihu";
        cycle = Mathf.Repeat(seed, 1);
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
        if (CurrentState == State.Normal || CurrentState == State.Supported || CurrentState == State.Supporting)
            EscapeGait();
        if (CurrentState == State.Normal)
        {
            return;
        }
        SaveModelBase();
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

    void SaveModelBase()
    {
        if (modelPosed) return;
        baseModelPosition = actor.model.localPosition;
        baseModelRotation = actor.model.localRotation;
        modelPosed = true;
    }

    // Distance-driven stance prevents skating: its ankle travels backwards by
    // the exact root displacement. The shortened support phase, heel recovery
    // and bent-arm counter-swing make this a run instead of a lowered walk.
    // Only the rendered model moves vertically/rolls; its motor stays grounded.
    void EscapeGait()
    {
        SaveModelBase();
        var rig = actor.Rig;
        var chapter = actor.RetreatChapter;
        bool supported = CurrentState == State.Supported || CurrentState == State.Supporting;
        bool hurt = actor.wounded || CurrentState == State.Supported;
        float height = rig.Height;
        float stride = height * (supported ? .62f : hurt ? .77f : .96f);
        float stance = supported ? .64f : .43f;
        if (!actor.IsPaused)
        {
            escapeClock += Time.deltaTime;
            motionWeight = Mathf.MoveTowards(motionWeight, actor.Walking ? 1 : 0, Time.deltaTime * 5);
            cycle = Mathf.Repeat(cycle + actor.StepDistance / Mathf.Max(.5f, stride), 1);
            RecoveryWeight = supported || hurt ? 0 : Beat(escapeClock + seed, 9.7f + seed % 3, .72f) * motionWeight;
            LookBackWeight = supported ? 0 : Beat(escapeClock + seed * 2, 5.9f + seed % 2, 1.15f) * (1 - RecoveryWeight) * (hurt ? .28f * motionWeight : 1);
        }
        float flinch = chapter && chapter.Retreat ? chapter.Retreat.ImpactPulse : 0;
        float swing = Mathf.Cos(cycle * Mathf.PI * 2);
        Vector3 forward = rig.Forward, right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 leftRest = actor.model.TransformPoint(leftAnkle), rightRest = actor.model.TransformPoint(rightAnkle);
        Quaternion leftRotation = actor.model.rotation * leftSole, rightRotation = actor.model.rotation * rightSole;
        float flight = !hurt && !supported ? Mathf.Pow(Mathf.Abs(Mathf.Sin(cycle * Mathf.PI * 2)), 4) : 0;
        float bob = motionWeight * height * (-.04f + .04f * flight - RecoveryWeight * .022f);
        float sway = .008f * swing;
        if (actor.wounded && !supported)
        {
            float contact = Mathf.Sin(cycle * Mathf.PI * 2 + .35f);
            bob = height * motionWeight * (-.028f - .018f * contact * contact);
            sway = -.017f * contact + (guardsLeft ? .006f : -.006f);
        }
        actor.model.position += Vector3.up * bob + right * (height * sway * motionWeight);
        actor.model.RotateAround(rig.Hips.position, right, (supported ? 2 : hurt ? 7 : 5) * motionWeight);
        float shoulderRoll = (hurt ? 2.2f : 2.5f) * swing * motionWeight + RecoveryWeight * 8;
        spine.rotation = Quaternion.AngleAxis(shoulderRoll, forward) *
            Quaternion.AngleAxis((supported ? 3 : hurt ? 5 : 9) * motionWeight + RecoveryWeight * 12 + flinch * 6, right) * spine.rotation;
        float glanceSide = seed % 2 < 1 ? -1 : 1;
        spine.rotation = Quaternion.AngleAxis(LookBackWeight * glanceSide * 12, Vector3.up) * spine.rotation;
        rig.Head.rotation = Quaternion.AngleAxis(LookBackWeight * glanceSide * 55, Vector3.up) *
            Quaternion.AngleAxis(-5 * motionWeight, right) * rig.Head.rotation;
        RunLeg(true, cycle, stride, stance, hurt, supported, leftRest, leftRotation, forward, right);
        RunLeg(false, Mathf.Repeat(cycle + .5f, 1), stride, stance, hurt, supported, rightRest, rightRotation, forward, right);
        bool holdingSide = actor.wounded && !supported;
        if (!holdingSide || !guardsLeft) RunArm(true, swing, supported, forward, right);
        if (!holdingSide || guardsLeft) RunArm(false, -swing, supported, forward, right);
        if (holdingSide)
        {
            var shoulder = guardsLeft ? leftArm : rightArm;
            var elbow = guardsLeft ? leftElbow : rightElbow;
            var hand = guardsLeft ? leftHand : rightHand;
            Vector3 outside = guardsLeft ? -right : right;
            float reach = Vector3.Distance(shoulder.position, elbow.position) + Vector3.Distance(elbow.position, hand.position);
            Vector3 hold = shoulder.position + reach * (forward * (.39f + .018f * swing * motionWeight)
                - outside * .12f - Vector3.up * .52f);
            Arm(guardsLeft, hold, Vector3.down + outside * .32f - forward * .28f);
            SetInjuredWrist(guardsLeft, -forward - outside * .5f, 16);
        }
        if (supported) return; // Authored paired hand contacts are applied next.
        // Stagger, recover and look back at different times for each person.
        // A warning raises a protecting forearm without sinking the whole body.
        if (RecoveryWeight > .001f)
        {
            Arm(false, Vector3.Lerp(rightHand.position, rightArm.position + right * height * .23f + forward * height * .12f - Vector3.up * height * .11f, RecoveryWeight), -forward);
        }
        float gas = chapter && chapter.gas ? chapter.gas.DensityAt(actor.transform.position) : 0;
        float cough = gas * Beat(escapeClock + seed, 4.6f, 1.1f);
        float guard = Mathf.Clamp01(Mathf.Max(flinch, actor.CoverCrouch * .85f));
        float protect = Mathf.Max(cough, guard);
        if (protect > .001f)
        {
            bool left = actor.wounded && !guardsLeft;
            Vector3 side = left ? -right : right;
            Vector3 target = rig.Head.position + forward * .16f + side * .065f - Vector3.up * Mathf.Lerp(.13f, .02f, guard);
            var shoulder = left ? leftArm : rightArm;
            var elbow = left ? leftElbow : rightElbow;
            var hand = left ? leftHand : rightHand;
            Vector3 bend = Vector3.ProjectOnPlane(elbow.position - shoulder.position, hand.position - shoulder.position).normalized;
            Arm(left, Vector3.Lerp(hand.position, target, protect), Vector3.Lerp(bend, side * .40f - forward * .45f + Vector3.down, protect));
            if (actor.wounded) SetInjuredWrist(left, Vector3.Slerp(-side, -forward, protect), Mathf.Lerp(10, 20, protect));
            else hand.rotation = Quaternion.Slerp(hand.rotation, Quaternion.LookRotation(forward - Vector3.up * .6f, -forward) *
                Quaternion.Inverse(rig.PalmFrame(left)), protect);
        }
    }

    void RunLeg(bool left, float phase, float stride, float stance, bool hurt, bool supported,
        Vector3 rest, Quaternion rotation, Vector3 forward, Vector3 right)
    {
        bool limping = hurt && !supported;
        bool sore = left == guardsLeft;
        if (limping) stance = sore ? .47f : .64f;
        float halfStride = stride * stance * .5f;
        float offset, lift = 0, toe = 0;
        if (phase < stance)
        {
            offset = Mathf.Lerp(halfStride, -halfStride, phase / stance);
            toe = 12 * Mathf.SmoothStep(0, 1, Mathf.InverseLerp(stance * .65f, stance, phase));
        }
        else
        {
            float t = (phase - stance) / (1 - stance);
            // Hermite tangents match the planted foot's velocity at both ends.
            float tangent = -stride * (1 - stance);
            offset = (2*t*t*t-3*t*t+1)*-halfStride + (t*t*t-2*t*t+t)*tangent +
                (-2*t*t*t+3*t*t)*halfStride + (t*t*t-t*t)*tangent;
            float recovery = Mathf.Sin(t * Mathf.PI);
            lift = actor.Rig.Height * (supported ? .055f : limping ? (sore ? .063f : .12f) : .17f) * recovery * recovery;
            toe = limping ? 12 * (1 - t) - (sore ? 12 : 19) * recovery : Mathf.Lerp(12, -12, t) * recovery;
        }
        var strideDirection = actor.EscortRifle && actor.EscortRifle.Turning && actor.Walking ? actor.StepDirection : forward;
        Vector3 target = rest + strideDirection * (offset * motionWeight);
        var terrain = actor.RetreatChapter.evacuation.terrain;
        if (terrain && terrain.Raycast(new Ray(target + Vector3.up * 5, Vector3.down), out var hit, 10))
            target.y = hit.point.y + (left ? leftClearance : rightClearance);
        target.y += lift * motionWeight;
        var thigh = left ? leftThigh : rightThigh;
        var calf = left ? leftCalf : rightCalf;
        var foot = left ? leftFoot : rightFoot;
        Plant(thigh, calf, foot, target, forward + right * (left ? -.08f : .08f), 1);
        foot.rotation = Quaternion.AngleAxis(toe * motionWeight, right) * rotation;
    }

    void RunArm(bool left, float swing, bool supported, Vector3 forward, Vector3 right)
    {
        var arm = left ? leftArm : rightArm;
        var elbow = left ? leftElbow : rightElbow;
        var hand = left ? leftHand : rightHand;
        Vector3 side = right * (left ? -1 : 1);
        float length = Vector3.Distance(arm.position, elbow.position) + Vector3.Distance(elbow.position, hand.position);
        Vector3 target = arm.position + Vector3.down * length * .61f + side * length * .10f +
            forward * length * (.10f - swing * (supported ? .21f : .50f));
        Vector3 waiting = arm.position + Vector3.down * length * .83f + side * length * .08f + forward * length * .14f;
        if (actor.wounded && !supported)
        {
            // The free elbow trails in the direction of travel. Keep it clear of
            // the wrap without pushing it out sideways in the player's rear view.
            target = arm.position + length * (Vector3.down * (.71f + .04f * swing) + side * .085f + forward * (.16f - swing * .40f));
            Arm(left, Vector3.Lerp(waiting, target, motionWeight), -forward * .76f + side * .20f + Vector3.down * .65f);
            SetInjuredWrist(left, -side, 10);
            return;
        }
        Arm(left, Vector3.Lerp(waiting, target, motionWeight), -forward + side * .4f + Vector3.down * .35f);
        OrientPalm(left, hand.position - elbow.position, -side);
    }

    static float Beat(float time, float period, float duration)
    {
        float t = Mathf.Repeat(time, period) / duration;
        return t < 1 ? Mathf.Sin(t * Mathf.PI) * Mathf.Sin(t * Mathf.PI) : 0;
    }

    public sealed class EscapeSnapshot
    {
        public float clock, cycle, weight, recovery, look;
    }
    public EscapeSnapshot CaptureEscape() => new EscapeSnapshot { clock = escapeClock, cycle = cycle,
        weight = motionWeight, recovery = RecoveryWeight, look = LookBackWeight };
    public void RestoreEscape(EscapeSnapshot saved)
    {
        escapeClock = saved != null ? saved.clock : 0;
        cycle = saved != null ? saved.cycle : Mathf.Repeat(seed, 1);
        motionWeight = saved != null ? saved.weight : 0;
        RecoveryWeight = saved != null ? saved.recovery : 0;
        LookBackWeight = saved != null ? saved.look : 0;
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

    void SetInjuredWrist(bool left, Vector3 palmNormal, float allowance)
    {
        var elbow = left ? leftElbow : rightElbow;
        var hand = left ? leftHand : rightHand;
        Quaternion neutral = elbow.rotation * (left ? leftWristRest : rightWristRest);
        Vector3 fingers = hand.position - elbow.position;
        Quaternion basis = left ? palms.left : palms.right;
        Quaternion straight = Quaternion.FromToRotation(neutral * (basis * Vector3.forward), fingers) * neutral;
        Quaternion desired = Quaternion.LookRotation(fingers, palmNormal) * Quaternion.Inverse(basis);
        hand.rotation = Quaternion.RotateTowards(straight, desired, allowance);
    }

    internal void HoldRifle(Transform weapon)
    {
        Vector3 forward = actor.Rig.Forward, side = Vector3.Cross(Vector3.up, forward);
        var rear = new Vector3(0, -.045f, .24f);
        var fore = new Vector3(0, -.028f, .40f);
        // The two actors have different shoulder widths and arm lengths.
        // Settle the rigid prop inside both arm spans before solving the palms.
        FitReach(leftArm, leftElbow, leftHand, weapon.TransformPoint(fore));
        FitReach(rightArm, rightElbow, rightHand, weapon.TransformPoint(rear));
        Chapter3HandPose.PlaceGrip(rightArm, rightElbow, rightHand, palms.right, palms.rightLength, forward, weapon,
            weapon.TransformPoint(rear), side * .55f + Vector3.down - forward * .25f, false);
        Chapter3HandPose.PlaceGrip(leftArm, leftElbow, leftHand, palms.left, palms.leftLength, forward, weapon,
            weapon.TransformPoint(fore), -side * .5f + Vector3.down, true);

        void FitReach(Transform shoulder, Transform elbow, Transform wrist, Vector3 contact)
        {
            float reach = .90f * (Vector3.Distance(shoulder.position, elbow.position) + Vector3.Distance(elbow.position, wrist.position));
            Vector3 span = contact - shoulder.position;
            weapon.position += Vector3.ClampMagnitude(span, reach) - span;
        }
    }

    static void Plant(Transform thigh, Transform calf, Transform foot, Vector3 target, Vector3 bend, float weight)
    {
        if (thigh && calf && foot) Chapter1IncidentRig.Solve(thigh, calf, foot, Vector3.Lerp(foot.position, target, weight), bend);
    }
}
