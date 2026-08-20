using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 賽德克風格／靈感的婚禮圍圈群舞：
/// 圍圈、時進時退、一步一步前進，並可用 Humanoid IK 讓相鄰 NPC 手牽手。
/// </summary>
[DefaultExecutionOrder(1100)]
public class Chapter1CircleDancer : MonoBehaviour
{
    private static readonly List<Chapter1CircleDancer> activeDancers =
        new List<Chapter1CircleDancer>();

    [Header("是否參加繞圈舞")]
    [Tooltip("任務 NPC 請取消勾選。")]
    public bool canCircleDance = true;

    public bool forceIdleWhenExcluded = true;

    [Header("Animator")]
    public Animator animator;
    public string danceStateName = "Dance";
    public string idleStateName = "Idle";
    public bool playDanceAnimatorState = true;
    public float animatorDanceSpeed = 1.50f;

    [Tooltip("跳舞期間持續確認 Animator 正在 Dance State，避免其他腳本把角色切回 Idle。")]
    public bool keepDanceAnimationPlaying = true;

    [Tooltip("多久確認一次 Dance State。")]
    public float danceStateCheckInterval = 0.35f;

    [Header("舞圈中心")]
    [Tooltip("建議拖入一個放在烤乳豬正中央的空物件。")]
    public Transform roastedPigCenter;

    [Tooltip("有指定 Roasted Pig Center 時，自動把它當成舞圈中心。")]
    public bool useRoastedPigAsCenter = true;

    public Transform center;
    public string fallbackCenterName = "DanceTrigger";
    public bool playOnAwake = true;

    [Header("舊版相容欄位（請不用調）")]
    [Tooltip("保留給 Chapter1PerformanceController 舊版程式使用；新版舞蹈不使用此值。")]
    public float orbitSpeedDegrees = -34f;

    [Tooltip("保留給 Chapter1PerformanceController 舊版程式使用；新版請調 Step Bob Height。")]
    public float stepHeight = 0.12f;

    [Tooltip("保留給 Chapter1PerformanceController 舊版程式使用；新版請調 Tempo Bpm。")]
    public float stepFrequency = 2.2f;

    [Tooltip("保留給 Chapter1PerformanceController 舊版程式使用；新版請調 In Out Distance。")]
    public float radialStepDistance = 0.12f;

    [Tooltip("保留給 Chapter1PerformanceController 舊版程式使用；新版已取消誇張左右搖。")]
    public float swayDegrees = 5f;

    [Header("自動排成完整圓圈")]
    [Tooltip("勾選後，所有使用同一個中心的舞者會自動平均排成一圈，不再各自在原本位置繞。")]
    public bool autoArrangeEvenlyAroundCenter = true;

    [Tooltip("角色移到自己的圓圈位置的速度。建議 3~6。")]
    public float circleArrangeSpeed = 9.5f;

    [Tooltip("整個舞圈的起始角度，只用來轉整圈方向。")]
    public float groupStartAngleDegrees = 0f;

    [Tooltip("所有 NPC 的繞圈腳步使用同一個節拍，避免有人越走越前面或落後。")]
    public bool synchronizeRootMovement = true;

    [Tooltip("Leave one empty circle slot for the player to join after the tasks.")]
    [Range(0, 2)]
    public int reservedPlayerSlots = 1;

    [Tooltip("直接使用固定舞圈半徑。這版預設開啟，避免大家縮成一坨。")]
    public bool useFixedCircleRadius = true;

    [Tooltip("NPC 距離烤乳豬中心的距離。建議 2.2~3.0。")]
    public float fixedCircleRadius = 22.00f;

    [Tooltip("固定半徑模式下，不再因手牽手功能自動縮小舞圈。")]
    public bool preventHandHoldAutoShrink = true;

    [Header("賽德克風格群舞節奏")]
    public float tempoBpm = 88f;
    public float degreesPerBeat = 1.65f;
    public float inOutDistance = 0.10f;
    public float stepBobHeight = 0f;
    public float kneePulse = 0f;

    [Header("Procedural stepping")]
    public bool enableProceduralStepping = true;
    [Range(0f, 30f)] public float upperLegLiftDegrees = 14f;
    [Range(0f, 45f)] public float kneeBendDegrees = 28f;
    [Range(0f, 0.04f)] public float pelvisBobHeightRatio = 0.012f;
    [Range(0f, 0.03f)] public float pelvisWeightShiftRatio = 0.007f;

    [Header("朝向")]
    public bool faceCenter = true;

    [Range(0f, 0.35f)]
    public float travelFacingBlend = 0.10f;

    public float turnSmooth = 7f;

    [Header("身體自然感")]
    public Transform visualRoot;
    public float bodyLeanDegrees = 1.3f;
    public float weightShiftDegrees = 0.8f;

    [Header("個體差異")]
    [Range(0f, 0.08f)]
    public float individualTempoVariation = 0.02f;

    [Range(0f, 0.12f)]
    public float individualPhaseVariation = 0.04f;

    public float minimumRadius = 1.2f;

    [Header("貼地修正")]
    [Tooltip("讓每個 NPC 自動貼著地面走，避免斜坡上有人埋進地板或飛起來。")]
    public bool followGround = true;

    [Tooltip("往角色上方多高開始往下偵測地面。")]
    public float groundProbeUp = 5f;

    [Tooltip("往下最多偵測多遠。")]
    public float groundProbeDown = 12f;

    [Tooltip("地面跟隨速度。越大越貼地。")]
    public float groundFollowSpeed = 28f;

    [Tooltip("每次地面偵測允許與上一個地面高度相差多少。用來避免突然打到屋頂、木架或其他道具。")]
    public float maxGroundHeightJump = 1.25f;

    [Tooltip("只接受朝上的表面，避免射線打到牆面。")]
    [Range(0f, 1f)]
    public float minimumGroundNormalY = 0.45f;

    [Tooltip("地面 Layer。預設 Everything；如果場景有 Ground/Terrain Layer，建議只勾地面。")]
    public LayerMask groundLayers = ~0;

    [Tooltip("保留角色一開始相對地面的高度差，適合不同模型 Pivot。")]
    public bool preserveInitialGroundOffset = true;

    [Tooltip("防止射線誤打到烤乳豬、桌子、木頭後把 NPC 瞬間抬高。")]
    public bool rejectSuspiciousHighGround = true;

    [Tooltip("偵測到的地面若比角色一開始的地面高超過此值，就視為道具而不是地面。")]
    public float maximumGroundRiseFromStart = 0.45f;

    [Tooltip("偵測到的地面若比角色一開始的地面低超過此值，就先不跟隨。")]
    public float maximumGroundDropFromStart = 0.70f;

    [Tooltip("Ground Layers 還沒設定好時，額外忽略烤乳豬中心物件及其子物件 Collider。")]
    public bool ignoreRoastedPigCollidersForGround = true;

    [Header("全員同一地面高度")]
    [Tooltip("勾選後，所有使用同一個烤乳豬中心的舞者會以烤乳豬附近的同一個地面高度為基準，不會有人一高一低。")]
    public bool lockAllDancersToSharedGround = false;

    [Tooltip("Humanoid 角色會用左右腳骨的位置校正 Root 高度，避免不同模型 Pivot 造成有人浮起或陷地。")]
    public bool useHumanoidFeetForGrounding = false;

    [Tooltip("腳底稍微離地的高度。0.01~0.03 通常最自然。")]
    public float footGroundClearance = 0.025f;

    [Tooltip("在斜坡上額外檢查左右腳底；任何一隻腳穿地，就把整個角色往上補。")]
    public bool preventFeetSinkingOnSlopes = false;

    [Tooltip("腳底穿地校正最多一次抬高多少，避免撞到奇怪 Collider 時瞬間飛起。")]
    public float maxFootPenetrationCorrection = 0.22f;

    [Tooltip("從烤乳豬中心上方往下找真正地面時的額外高度。")]
    public float sharedGroundProbeUp = 8f;

    [Tooltip("尋找共同地面的最大向下距離。")]
    public float sharedGroundProbeDown = 20f;

    [Header("手牽手 - Humanoid IK")]
    [Tooltip("勾選後，相鄰舞者會自動左右手牽手。")]
    public bool enableHandHolding = true;

    [Range(0f, 1f)]
    [Tooltip("手牽手 IK 強度。0.8~0.92 比較自然。")]
    public float handHoldIKWeight = 0.88f;

    [Range(0f, 1f)]
    [Tooltip("手掌旋轉跟隨強度。建議低一點，避免手腕扭曲。")]
    public float handHoldRotationWeight = 0.12f;

    [Tooltip("牽手位置上下微調。")]
    public float handHoldHeightOffset = -0.025f;

    [Tooltip("兩人的手相距超過這個值時不強拉，避免手臂被扯長。")]
    public float maximumHandPairDistance = 1.65f;

    [Tooltip("自動把舞圈縮到相鄰 NPC 可以舒服牽手的大小。")]
    public bool autoFitCircleForHandHolding = true;

    [Tooltip("相鄰角色 Root 理想間距。1.0~1.25 通常適合成人角色。")]
    public float desiredNeighborSpacing = 1.12f;

    [Tooltip("舞圈半徑調整速度。")]
    public float handHoldRadiusAdjustSpeed = 0.8f;

    [Tooltip("如果 Animator 是 Humanoid，會自動在 Animator 物件上加入 IK Driver。")]
    public bool autoCreateHandHoldIKDriver = true;

    [Tooltip("Animator IK 沒有觸發時，仍在 LateUpdate 用 Humanoid 手臂骨架完成牽手。")]
    public bool enableProceduralHandHolding = true;

    [Range(0f, 1f)]
    public float proceduralHandHoldWeight = 0.94f;

    [Range(0.05f, 0.5f)]
    public float handHoldShoulderDropRatio = 0.26f;

    [Tooltip("0 = shoulder height, 1 = hip height. The reference pose keeps joined hands just above the waist.")]
    [Range(0.25f, 0.8f)]
    public float handHoldShoulderToHipRatio = 0.52f;

    [Header("手牽手除錯")]
    public bool logHandHoldSetup = false;

    private bool dancing;
    private float radius;
    private float baseHeight;
    private float startAngle;
    private float initialOrderAngle;
    private bool initialOrderAngleCaptured;

    private float tempoScale = 1f;
    private float phaseOffset;

    private Quaternion visualBaseLocalRotation;
    private bool visualBaseSaved;

    private float lastAnimatorSpeed = 1f;
    private float nextDanceStateCheckTime;

    private bool groundOffsetCalibrated;
    private float groundRootOffset;
    private float lastGroundY;
    private bool hasLastGroundY;
    private float initialGroundY;
    private bool hasInitialGroundY;

    private readonly RaycastHit[] groundHits = new RaycastHit[24];

    private Transform leftHandBone;
    private Transform rightHandBone;
    private Transform leftFootBone;
    private Transform rightFootBone;
    private Transform hipsBone;
    private Transform headBone;
    private Transform leftUpperArmBone;
    private Transform leftLowerArmBone;
    private Transform rightUpperArmBone;
    private Transform rightLowerArmBone;
    private Transform leftUpperLegBone;
    private Transform leftLowerLegBone;
    private Transform rightUpperLegBone;
    private Transform rightLowerLegBone;

    private float sharedGroundY;
    private bool sharedGroundReady;

    public bool IsActivelyDancing =>
        enabled
        && gameObject.activeInHierarchy
        && canCircleDance
        && dancing
        && center != null;

    private void OnEnable()
    {
        if (!activeDancers.Contains(this))
        {
            activeDancers.Add(this);
        }
    }

    private void OnDisable()
    {
        activeDancers.Remove(this);
    }

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }

        if (visualRoot != null)
        {
            visualBaseLocalRotation = visualRoot.localRotation;
            visualBaseSaved = true;
        }

        CacheHumanoidHands();
        EnsureHandHoldIKDriver();
    }

    private void Start()
    {
        // 穩定貼地版：強制使用「起始 Root 到地面的偏移」。
        // 不再用 Humanoid 腳骨高度推算 Root，避免不同角色骨架比例造成整群飄高。
        followGround = true;
        lockAllDancersToSharedGround = false;
        useHumanoidFeetForGrounding = false;
        preventFeetSinkingOnSlopes = false;

        if (useRoastedPigAsCenter)
        {
            if (roastedPigCenter == null)
            {
                GameObject sharedPigCenter =
                    GameObject.Find("烤乳豬舞圈中心");

                if (sharedPigCenter != null)
                {
                    roastedPigCenter =
                        sharedPigCenter.transform;
                }
            }

            if (roastedPigCenter != null)
            {
                center = roastedPigCenter;
            }
        }

        if (!canCircleDance)
        {
            dancing = false;
            ForceIdle();
            return;
        }

        ResolveCenter();

        if (center == null)
        {
            dancing = false;
            enabled = false;
            return;
        }

        RecalculateCirclePlacement();
        CaptureInitialOrderAngle();
        CalibrateGroundOffset();
        RefreshSharedGroundHeight();

        tempoScale =
            1f + Random.Range(
                -individualTempoVariation,
                individualTempoVariation);

        phaseOffset =
            Random.Range(
                -individualPhaseVariation,
                individualPhaseVariation);

        dancing = playOnAwake && canCircleDance;

        if (dancing)
        {
            PlayDanceAnimation();
        }
    }

    private void Update()
    {
        if (!canCircleDance)
        {
            dancing = false;
            ForceIdle();
            return;
        }

        if (!dancing || center == null)
        {
            return;
        }

        KeepDanceAnimatorAlive();

        if (enableHandHolding
            && autoFitCircleForHandHolding
            && !(useFixedCircleRadius && preventHandHoldAutoShrink))
        {
            UpdateHandHoldingCircleRadius();
        }

        UpdateNaturalCircleDance();
    }

    private void LateUpdate()
    {
        ApplyProceduralStepping();
        ApplyProceduralHandHolding();
    }

    private void ApplyProceduralHandHolding()
    {
        if (!enableHandHolding
            || !enableProceduralHandHolding
            || !IsActivelyDancing
            || animator == null)
        {
            return;
        }

        if (leftUpperArmBone == null
            || leftLowerArmBone == null
            || leftHandBone == null
            || rightUpperArmBone == null
            || rightLowerArmBone == null
            || rightHandBone == null)
        {
            CacheHumanoidHands();
        }

        Chapter1CircleDancer previous = FindNeighbor(clockwise: false);
        Chapter1CircleDancer next = FindNeighbor(clockwise: true);

        ApplyProceduralArmIK(
            leftUpperArmBone,
            leftLowerArmBone,
            leftHandBone,
            previous,
            false);
        ApplyProceduralArmIK(
            rightUpperArmBone,
            rightLowerArmBone,
            rightHandBone,
            next,
            true);
    }

    private void ApplyProceduralArmIK(
        Transform upperArm,
        Transform lowerArm,
        Transform hand,
        Chapter1CircleDancer partner,
        bool usePartnerLeftArm)
    {
        if (upperArm == null || lowerArm == null || hand == null || partner == null)
        {
            return;
        }

        Transform partnerUpperArm = usePartnerLeftArm
            ? partner.leftUpperArmBone
            : partner.rightUpperArmBone;
        Transform partnerLowerArm = usePartnerLeftArm
            ? partner.leftLowerArmBone
            : partner.rightLowerArmBone;
        Transform partnerHand = usePartnerLeftArm
            ? partner.GetLeftHandBone()
            : partner.GetRightHandBone();

        if (partnerUpperArm == null || partnerLowerArm == null || partnerHand == null)
        {
            partner.CacheHumanoidHands();
            partnerUpperArm = usePartnerLeftArm
                ? partner.leftUpperArmBone
                : partner.rightUpperArmBone;
            partnerLowerArm = usePartnerLeftArm
                ? partner.leftLowerArmBone
                : partner.rightLowerArmBone;
            partnerHand = usePartnerLeftArm
                ? partner.leftHandBone
                : partner.rightHandBone;
        }

        if (partnerUpperArm == null || partnerLowerArm == null || partnerHand == null)
        {
            return;
        }

        float ownArmLength = GetArmChainLength(upperArm, lowerArm, hand);
        float partnerArmLength = GetArmChainLength(
            partnerUpperArm,
            partnerLowerArm,
            partnerHand);
        float shoulderDistance = Vector3.Distance(
            upperArm.position,
            partnerUpperArm.position);

        // The two dancers beside the reserved player slot are intentionally not joined.
        if (ownArmLength <= 0.01f
            || partnerArmLength <= 0.01f
            || shoulderDistance > (ownArmLength + partnerArmLength) * 1.08f)
        {
            return;
        }

        Vector3 target = Vector3.Lerp(
            upperArm.position,
            partnerUpperArm.position,
            0.5f);

        if (hipsBone != null && partner.hipsBone != null)
        {
            float shoulderY = (upperArm.position.y + partnerUpperArm.position.y) * 0.5f;
            float hipY = (hipsBone.position.y + partner.hipsBone.position.y) * 0.5f;
            target.y = Mathf.Lerp(
                shoulderY,
                hipY,
                Mathf.Clamp01(handHoldShoulderToHipRatio));
        }
        else
        {
            float drop = Mathf.Min(ownArmLength, partnerArmLength)
                * handHoldShoulderDropRatio;
            target += Vector3.down * drop;
        }

        target += Vector3.up * handHoldHeightOffset;

        Vector3 towardCenter = center != null
            ? center.position - Vector3.Lerp(transform.position, partner.transform.position, 0.5f)
            : transform.forward;
        towardCenter.y = 0f;
        if (towardCenter.sqrMagnitude < 0.001f)
        {
            towardCenter = transform.forward;
        }
        towardCenter.Normalize();
        Vector3 elbowPole = Vector3.down + towardCenter * 0.12f;

        SolveTwoBoneArm(
            upperArm,
            lowerArm,
            hand,
            target,
            elbowPole,
            Mathf.Clamp01(proceduralHandHoldWeight));
    }

    private void SolveTwoBoneArm(
        Transform upperArm,
        Transform lowerArm,
        Transform hand,
        Vector3 requestedTarget,
        Vector3 requestedPole,
        float weight)
    {
        if (weight <= 0f)
        {
            return;
        }

        Vector3 shoulder = upperArm.position;
        float upperLength = Vector3.Distance(shoulder, lowerArm.position);
        float lowerLength = Vector3.Distance(lowerArm.position, hand.position);
        if (upperLength <= 0.001f || lowerLength <= 0.001f)
        {
            return;
        }

        Vector3 targetDirection = requestedTarget - shoulder;
        float targetDistance = targetDirection.magnitude;
        if (targetDistance <= 0.001f)
        {
            return;
        }

        targetDirection /= targetDistance;
        float minimumReach = Mathf.Abs(upperLength - lowerLength) + 0.002f;
        float maximumReach = upperLength + lowerLength - 0.002f;
        targetDistance = Mathf.Clamp(targetDistance, minimumReach, maximumReach);
        Vector3 target = shoulder + targetDirection * targetDistance;

        Vector3 pole = Vector3.ProjectOnPlane(requestedPole, targetDirection);
        if (pole.sqrMagnitude < 0.0001f)
        {
            pole = Vector3.ProjectOnPlane(
                lowerArm.position - shoulder,
                targetDirection);
        }
        pole.Normalize();

        float along = (
            upperLength * upperLength
            + targetDistance * targetDistance
            - lowerLength * lowerLength)
            / (2f * targetDistance);
        float perpendicular = Mathf.Sqrt(Mathf.Max(
            0f,
            upperLength * upperLength - along * along));
        Vector3 elbowTarget = shoulder
            + targetDirection * along
            + pole * perpendicular;

        Quaternion originalUpperRotation = upperArm.rotation;
        Quaternion desiredUpperRotation = Quaternion.FromToRotation(
            lowerArm.position - shoulder,
            elbowTarget - shoulder) * upperArm.rotation;
        upperArm.rotation = Quaternion.Slerp(
            originalUpperRotation,
            desiredUpperRotation,
            weight);

        Quaternion originalLowerRotation = lowerArm.rotation;
        Vector3 currentForearm = hand.position - lowerArm.position;
        Vector3 desiredForearm = target - lowerArm.position;
        if (currentForearm.sqrMagnitude > 0.0001f
            && desiredForearm.sqrMagnitude > 0.0001f)
        {
            Quaternion desiredLowerRotation = Quaternion.FromToRotation(
                currentForearm,
                desiredForearm) * lowerArm.rotation;
            lowerArm.rotation = Quaternion.Slerp(
                originalLowerRotation,
                desiredLowerRotation,
                weight);
        }
    }

    private float GetArmChainLength(
        Transform upperArm,
        Transform lowerArm,
        Transform hand)
    {
        return upperArm != null && lowerArm != null && hand != null
            ? Vector3.Distance(upperArm.position, lowerArm.position)
                + Vector3.Distance(lowerArm.position, hand.position)
            : 0f;
    }

    private void ApplyProceduralStepping()
    {
        if (!enableProceduralStepping
            || !IsActivelyDancing
            || animator == null
            || animator.runtimeAnimatorController == null)
        {
            return;
        }

        if (hipsBone == null
            || leftUpperLegBone == null
            || leftLowerLegBone == null
            || rightUpperLegBone == null
            || rightLowerLegBone == null)
        {
            CacheHumanoidHands();
        }

        if (leftUpperLegBone == null
            || leftLowerLegBone == null
            || rightUpperLegBone == null
            || rightLowerLegBone == null)
        {
            return;
        }

        float beatsPerSecond = Mathf.Max(72f, tempoBpm) / 60f;
        float alternatingStep = Mathf.Sin(Time.time * beatsPerSecond * Mathf.PI);
        float leftLift = Mathf.SmoothStep(0f, 1f, Mathf.Max(0f, alternatingStep));
        float rightLift = Mathf.SmoothStep(0f, 1f, Mathf.Max(0f, -alternatingStep));
        Vector3 bendAxis = transform.right.normalized;

        ApplyLegStep(leftUpperLegBone, leftLowerLegBone, bendAxis, leftLift);
        ApplyLegStep(rightUpperLegBone, rightLowerLegBone, bendAxis, rightLift);

        if (hipsBone != null)
        {
            float bodyHeight = GetApproximateBodyHeight();
            float strongestLift = Mathf.Max(leftLift, rightLift);
            float sideShift = leftLift - rightLift;
            hipsBone.position += Vector3.up
                * bodyHeight
                * pelvisBobHeightRatio
                * strongestLift;
            hipsBone.position += transform.right
                * bodyHeight
                * pelvisWeightShiftRatio
                * sideShift;
        }
    }

    private void ApplyLegStep(
        Transform upperLeg,
        Transform lowerLeg,
        Vector3 bendAxis,
        float lift)
    {
        if (lift <= 0.0001f)
        {
            return;
        }

        upperLeg.rotation = Quaternion.AngleAxis(
            -upperLegLiftDegrees * lift,
            bendAxis) * upperLeg.rotation;
        lowerLeg.rotation = Quaternion.AngleAxis(
            kneeBendDegrees * lift,
            bendAxis) * lowerLeg.rotation;
    }

    private float GetApproximateBodyHeight()
    {
        if (headBone != null && (leftFootBone != null || rightFootBone != null))
        {
            float lowestFootY = float.PositiveInfinity;
            if (leftFootBone != null)
            {
                lowestFootY = Mathf.Min(lowestFootY, leftFootBone.position.y);
            }

            if (rightFootBone != null)
            {
                lowestFootY = Mathf.Min(lowestFootY, rightFootBone.position.y);
            }

            if (!float.IsPositiveInfinity(lowestFootY))
            {
                return Mathf.Max(0.25f, headBone.position.y - lowestFootY);
            }
        }

        return Mathf.Max(0.25f, transform.lossyScale.y);
    }

    private void UpdateNaturalCircleDance()
    {
        // 現有場景的 Inspector 可能仍保留舊速度，
        // 這版直接保證至少使用較快的婚禮舞節奏。
        float bpm = Mathf.Max(72f, tempoBpm);
        float beatsPerSecond = bpm / 60f;

        float beatTime;

        if (synchronizeRootMovement)
        {
            // 所有人使用同一個世界時間節拍，
            // 不讓每個 NPC 的 tempoScale / phaseOffset 把圓圈越走越散。
            beatTime =
                Time.time
                * beatsPerSecond;
        }
        else
        {
            beatTime =
                Time.time
                * beatsPerSecond
                * tempoScale
                + phaseOffset;
        }

        float beatIndex = Mathf.Floor(beatTime);
        float beatFraction = Mathf.Repeat(beatTime, 1f);
        float stepEase = SmootherStep(beatFraction);
        float steppedBeat = beatIndex + stepEase;

        float movingAngle =
            steppedBeat
            * Mathf.Max(0.20f, degreesPerBeat)
            * Mathf.Deg2Rad;

        float angle =
            autoArrangeEvenlyAroundCenter
                ? GetEvenlySpacedAngle(movingAngle)
                : startAngle + movingAngle;

        float eightBeatPhase =
            Mathf.Repeat(
                beatTime / 8f,
                1f);

        float inOutWave =
            Mathf.Sin(
                eightBeatPhase
                * Mathf.PI
                * 2f);

        float baseCircleRadius =
            autoArrangeEvenlyAroundCenter
                ? GetSharedCircleRadius()
                : radius;

        float currentRadius =
            Mathf.Max(
                minimumRadius,
                baseCircleRadius
                - inOutWave
                * inOutDistance);

        Vector3 radial =
            new Vector3(
                Mathf.Sin(angle),
                0f,
                Mathf.Cos(angle));

        Vector3 targetPosition =
            center.position
            + radial
            * currentRadius;

        if (autoArrangeEvenlyAroundCenter)
        {
            float arrangeT =
                1f
                - Mathf.Exp(
                    -Mathf.Max(0.1f, circleArrangeSpeed)
                    * Time.deltaTime);

            targetPosition.x =
                Mathf.Lerp(
                    transform.position.x,
                    targetPosition.x,
                    arrangeT);

            targetPosition.z =
                Mathf.Lerp(
                    transform.position.z,
                    targetPosition.z,
                    arrangeT);
        }

        float stepArc =
            Mathf.Sin(
                beatFraction
                * Mathf.PI);

        float bob =
            stepArc
            * stepBobHeight;

        float knee =
            Mathf.Sin(
                beatFraction
                * Mathf.PI
                * 2f)
            * kneePulse;

        float desiredY =
            baseHeight
            + bob
            + knee;

        if (followGround)
        {
            float targetGroundY = 0f;
            bool gotGround = false;

            // 你的場地不是平的，所以每個 NPC 都要依「目前 X/Z 位置」
            // 自己抓腳下的真實地面高度，不能再共用同一個 Y。
            if (TryGetGroundHeight(
                    targetPosition,
                    out float detectedGroundY))
            {
                targetGroundY = detectedGroundY;
                gotGround = true;

                lastGroundY = detectedGroundY;
                hasLastGroundY = true;
            }

            if (!gotGround && hasLastGroundY)
            {
                targetGroundY = lastGroundY;
                gotGround = true;
            }

            if (gotGround)
            {
                if (!groundOffsetCalibrated)
                {
                    groundRootOffset =
                        preserveInitialGroundOffset
                            ? transform.position.y - targetGroundY
                            : 0f;

                    groundOffsetCalibrated = true;
                }

                // Root 只跟目前位置的地面高度變化走。
                // 不再加入腳骨高度修正，避免整個角色被抬到空中。
                desiredY =
                    targetGroundY
                    + groundRootOffset;

                // 程式不再額外上下彈，腳步感交給 Animator。
                // 這樣在斜坡上最穩定。
            }
        }

        // X/Z 照舞圈走；Y 單獨平滑跟地面，避免一幀突然飛高或插進地下。
        float yLerp =
            1f
            - Mathf.Exp(
                -Mathf.Max(0.1f, groundFollowSpeed)
                * Time.deltaTime);

        targetPosition.y =
            followGround
                ? Mathf.Lerp(
                    transform.position.y,
                    desiredY,
                    yLerp)
                : desiredY;

        transform.position =
            targetPosition;

        UpdateFacing(radial, beatTime, inOutWave);
        UpdateBodyWeight(beatTime, inOutWave);
    }

    private void UpdateFacing(
        Vector3 radialOutward,
        float beatTime,
        float inOutWave)
    {
        if (!faceCenter)
        {
            return;
        }

        Vector3 towardCenter =
            center.position
            - transform.position;

        towardCenter.y = 0f;

        if (towardCenter.sqrMagnitude < 0.001f)
        {
            return;
        }

        towardCenter.Normalize();

        Vector3 travelDirection =
            Vector3.Cross(
                Vector3.up,
                radialOutward);

        travelDirection.Normalize();

        Vector3 blendedDirection =
            Vector3.Slerp(
                towardCenter,
                travelDirection,
                Mathf.Clamp01(travelFacingBlend));

        float microYaw =
            Mathf.Sin(
                beatTime
                * Mathf.PI)
            * 0.8f;

        Quaternion targetRotation =
            Quaternion.LookRotation(
                blendedDirection,
                Vector3.up)
            * Quaternion.Euler(
                0f,
                microYaw,
                0f);

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                1f
                - Mathf.Exp(
                    -turnSmooth
                    * Time.deltaTime));
    }

    private void UpdateBodyWeight(
        float beatTime,
        float inOutWave)
    {
        if (visualRoot == null)
        {
            return;
        }

        if (!visualBaseSaved)
        {
            visualBaseLocalRotation =
                visualRoot.localRotation;

            visualBaseSaved = true;
        }

        float lean =
            -inOutWave
            * bodyLeanDegrees;

        float weightShift =
            Mathf.Sin(
                beatTime
                * Mathf.PI)
            * weightShiftDegrees;

        Quaternion danceOffset =
            Quaternion.Euler(
                lean,
                0f,
                weightShift);

        visualRoot.localRotation =
            Quaternion.Slerp(
                visualRoot.localRotation,
                visualBaseLocalRotation
                * danceOffset,
                1f
                - Mathf.Exp(
                    -8f
                    * Time.deltaTime));
    }

    private void CaptureInitialOrderAngle()
    {
        if (center == null)
        {
            return;
        }

        Vector3 offset =
            transform.position
            - center.position;

        offset.y = 0f;

        if (offset.sqrMagnitude < 0.0001f)
        {
            initialOrderAngle = 0f;
        }
        else
        {
            initialOrderAngle =
                Mathf.Atan2(
                    offset.x,
                    offset.z);
        }

        initialOrderAngleCaptured = true;
    }

    private float GetEvenlySpacedAngle(
        float movingAngle)
    {
        int participantCount =
            GetParticipantCountForThisCircle();

        if (participantCount <= 1)
        {
            return startAngle + movingAngle;
        }

        int slotIndex =
            GetMyCircleSlotIndex()
            + GetReservedPlayerSlotsForThisCircle();

        float slotAngle =
            Mathf.PI
            * 2f
            * slotIndex
            / participantCount;

        return groupStartAngleDegrees
            * Mathf.Deg2Rad
            + slotAngle
            + movingAngle;
    }

    private int GetMyCircleSlotIndex()
    {
        List<Chapter1CircleDancer> sameCircle =
            new List<Chapter1CircleDancer>();

        for (int i = 0; i < activeDancers.Count; i++)
        {
            Chapter1CircleDancer dancer =
                activeDancers[i];

            if (dancer == null
                || !dancer.IsActivelyDancing
                || dancer.center != center)
            {
                continue;
            }

            sameCircle.Add(dancer);
        }

        sameCircle.Sort(
            (a, b) =>
                a.GetInstanceID()
                .CompareTo(b.GetInstanceID()));

        int myIndex =
            sameCircle.IndexOf(this);

        return Mathf.Max(0, myIndex);
    }

    private float GetSharedCircleRadius()
    {
        if (useFixedCircleRadius)
        {
            // 既有場景中的 Component 會保留 Inspector 舊數值，
            // 所以這版直接保證舞圈至少 22 公尺半徑。
            return Mathf.Max(
                22.00f,
                minimumRadius,
                fixedCircleRadius);
        }

        int participantCount =
            GetParticipantCountForThisCircle();

        if (participantCount < 2)
        {
            return Mathf.Max(
                minimumRadius,
                radius);
        }

        float spacing =
            Mathf.Max(
                0.65f,
                desiredNeighborSpacing);

        float denominator =
            2f
            * Mathf.Sin(
                Mathf.PI
                / participantCount);

        if (denominator <= 0.001f)
        {
            return Mathf.Max(
                minimumRadius,
                radius);
        }

        return Mathf.Max(
            minimumRadius,
            spacing / denominator);
    }

    private void UpdateHandHoldingCircleRadius()
    {
        int participantCount =
            GetParticipantCountForThisCircle();

        if (participantCount < 3)
        {
            return;
        }

        float spacing =
            Mathf.Max(
                0.65f,
                desiredNeighborSpacing);

        float denominator =
            2f
            * Mathf.Sin(
                Mathf.PI
                / participantCount);

        if (denominator <= 0.001f)
        {
            return;
        }

        float idealRadius =
            Mathf.Max(
                minimumRadius,
                spacing / denominator);

        radius =
            Mathf.MoveTowards(
                radius,
                idealRadius,
                Mathf.Max(
                    0.01f,
                    handHoldRadiusAdjustSpeed)
                * Time.deltaTime);
    }

    private int GetParticipantCountForThisCircle()
    {
        int count = 0;

        for (int i = 0; i < activeDancers.Count; i++)
        {
            Chapter1CircleDancer dancer =
                activeDancers[i];

            if (IsSameActiveCircle(dancer))
            {
                count++;
            }
        }

        return count + GetReservedPlayerSlotsForThisCircle();
    }

    private int GetReservedPlayerSlotsForThisCircle()
    {
        int reserved = Mathf.Max(0, reservedPlayerSlots);

        for (int i = 0; i < activeDancers.Count; i++)
        {
            Chapter1CircleDancer dancer = activeDancers[i];
            if (dancer != null
                && dancer.IsActivelyDancing
                && dancer.center == center)
            {
                reserved = Mathf.Max(reserved, dancer.reservedPlayerSlots);
            }
        }

        return reserved;
    }

    private bool IsSameActiveCircle(
        Chapter1CircleDancer other)
    {
        return other != null
            && other != this
                ? other.IsActivelyDancing
                    && other.center == center
                : other == this
                    && IsActivelyDancing;
    }

    private void CacheHumanoidHands()
    {
        leftHandBone = null;
        rightHandBone = null;
        leftFootBone = null;
        rightFootBone = null;
        hipsBone = null;
        headBone = null;
        leftUpperArmBone = null;
        leftLowerArmBone = null;
        rightUpperArmBone = null;
        rightLowerArmBone = null;
        leftUpperLegBone = null;
        leftLowerLegBone = null;
        rightUpperLegBone = null;
        rightLowerLegBone = null;

        if (animator == null)
        {
            return;
        }

        if (animator.avatar != null
            && animator.avatar.isValid
            && animator.isHuman)
        {
            leftHandBone = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            rightHandBone = animator.GetBoneTransform(HumanBodyBones.RightHand);
            leftFootBone = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            rightFootBone = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            hipsBone = animator.GetBoneTransform(HumanBodyBones.Hips);
            headBone = animator.GetBoneTransform(HumanBodyBones.Head);
            leftUpperArmBone = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            leftLowerArmBone = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            rightUpperArmBone = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            rightLowerArmBone = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            leftUpperLegBone = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            leftLowerLegBone = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            rightUpperLegBone = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            rightLowerLegBone = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
        }

        // Several of the added wedding characters are imported as Generic rigs.
        // Their Tripo skeletons still expose consistent named bones, so resolve
        // those directly instead of excluding the character from hand-holding.
        Transform[] rigTransforms = animator.GetComponentsInChildren<Transform>(true);
        leftUpperArmBone = leftUpperArmBone != null
            ? leftUpperArmBone
            : FindRigBone(rigTransforms, "L_Upperarm", "LeftUpperArm", "LeftArm", "upper_arm.L", "upperarm_l", "J_Bip_L_UpperArm");
        leftLowerArmBone = leftLowerArmBone != null
            ? leftLowerArmBone
            : FindRigBone(rigTransforms, "L_Forearm", "LeftForeArm", "LeftLowerArm", "forearm.L", "forearm_l", "J_Bip_L_ForeArm");
        leftHandBone = leftHandBone != null
            ? leftHandBone
            : FindRigBone(rigTransforms, "L_Hand", "LeftHand", "Hand.L", "hand_l", "LeftWrist", "J_Bip_L_Hand");
        rightUpperArmBone = rightUpperArmBone != null
            ? rightUpperArmBone
            : FindRigBone(rigTransforms, "R_Upperarm", "RightUpperArm", "RightArm", "upper_arm.R", "upperarm_r", "J_Bip_R_UpperArm");
        rightLowerArmBone = rightLowerArmBone != null
            ? rightLowerArmBone
            : FindRigBone(rigTransforms, "R_Forearm", "RightForeArm", "RightLowerArm", "forearm.R", "forearm_r", "J_Bip_R_ForeArm");
        rightHandBone = rightHandBone != null
            ? rightHandBone
            : FindRigBone(rigTransforms, "R_Hand", "RightHand", "Hand.R", "hand_r", "RightWrist", "J_Bip_R_Hand");
        hipsBone = hipsBone != null
            ? hipsBone
            : FindRigBone(rigTransforms, "Pelvis", "Hips", "Hip", "J_Bip_C_Hips");
        headBone = headBone != null
            ? headBone
            : FindRigBone(rigTransforms, "Head", "J_Bip_C_Head");
        leftUpperLegBone = leftUpperLegBone != null
            ? leftUpperLegBone
            : FindRigBone(rigTransforms, "L_Thigh", "LeftUpLeg", "LeftUpperLeg", "thigh.L", "thigh_l", "J_Bip_L_UpperLeg");
        leftLowerLegBone = leftLowerLegBone != null
            ? leftLowerLegBone
            : FindRigBone(rigTransforms, "L_Calf", "LeftLeg", "LeftLowerLeg", "shin.L", "calf_l", "J_Bip_L_LowerLeg");
        rightUpperLegBone = rightUpperLegBone != null
            ? rightUpperLegBone
            : FindRigBone(rigTransforms, "R_Thigh", "RightUpLeg", "RightUpperLeg", "thigh.R", "thigh_r", "J_Bip_R_UpperLeg");
        rightLowerLegBone = rightLowerLegBone != null
            ? rightLowerLegBone
            : FindRigBone(rigTransforms, "R_Calf", "RightLeg", "RightLowerLeg", "shin.R", "calf_r", "J_Bip_R_LowerLeg");
        leftFootBone = leftFootBone != null
            ? leftFootBone
            : FindRigBone(rigTransforms, "L_Foot", "LeftFoot", "Foot.L", "foot_l", "J_Bip_L_Foot");
        rightFootBone = rightFootBone != null
            ? rightFootBone
            : FindRigBone(rigTransforms, "R_Foot", "RightFoot", "Foot.R", "foot_r", "J_Bip_R_Foot");
    }

    private static Transform FindRigBone(
        Transform[] rigTransforms,
        params string[] aliases)
    {
        if (rigTransforms == null || aliases == null)
        {
            return null;
        }

        string[] normalizedAliases = new string[aliases.Length];
        for (int i = 0; i < aliases.Length; i++)
        {
            normalizedAliases[i] = NormalizeBoneName(aliases[i]);
        }

        for (int pass = 0; pass < 2; pass++)
        {
            for (int i = 0; i < rigTransforms.Length; i++)
            {
                Transform candidate = rigTransforms[i];
                if (candidate == null)
                {
                    continue;
                }

                string candidateName = NormalizeBoneName(candidate.name);
                for (int j = 0; j < normalizedAliases.Length; j++)
                {
                    string alias = normalizedAliases[j];
                    if (alias.Length == 0)
                    {
                        continue;
                    }

                    bool matches = pass == 0
                        ? candidateName == alias
                        : candidateName.EndsWith(alias, System.StringComparison.Ordinal);
                    if (matches)
                    {
                        return candidate;
                    }
                }
            }
        }

        return null;
    }

    private static string NormalizeBoneName(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        char[] normalized = new char[value.Length];
        int count = 0;
        for (int i = 0; i < value.Length; i++)
        {
            char character = value[i];
            if (char.IsLetterOrDigit(character))
            {
                normalized[count++] = char.ToLowerInvariant(character);
            }
        }

        return new string(normalized, 0, count);
    }

    private void EnsureHandHoldIKDriver()
    {
        if (!autoCreateHandHoldIKDriver
            || animator == null)
        {
            return;
        }

        Chapter1HandHoldIK driver =
            animator.GetComponent<Chapter1HandHoldIK>();

        if (driver == null)
        {
            driver =
                animator.gameObject.AddComponent<
                    Chapter1HandHoldIK>();
        }

        driver.owner = this;

        if (logHandHoldSetup)
        {
            Debug.Log(
                "[Chapter1CircleDancer] "
                + name
                + " 已建立 HandHold IK Driver。",
                this);
        }
    }

    public void ApplyHandHoldingIK(
        Animator ikAnimator)
    {
        if (ikAnimator == null)
        {
            return;
        }

        if (!enableHandHolding
            || !IsActivelyDancing
            || !ikAnimator.isHuman)
        {
            ClearHandIK(ikAnimator);
            return;
        }

        if (leftHandBone == null
            || rightHandBone == null)
        {
            CacheHumanoidHands();
        }

        if (leftHandBone == null
            || rightHandBone == null)
        {
            ClearHandIK(ikAnimator);
            return;
        }

        Chapter1CircleDancer previous =
            FindNeighbor(clockwise: false);

        Chapter1CircleDancer next =
            FindNeighbor(clockwise: true);

        ApplySingleHandIK(
            ikAnimator,
            AvatarIKGoal.LeftHand,
            leftHandBone,
            previous,
            false);

        ApplySingleHandIK(
            ikAnimator,
            AvatarIKGoal.RightHand,
            rightHandBone,
            next,
            true);
    }

    private void ApplySingleHandIK(
        Animator ikAnimator,
        AvatarIKGoal goal,
        Transform ownHand,
        Chapter1CircleDancer partner,
        bool usePartnerLeftHand)
    {
        Transform partnerHand = partner != null
            ? (usePartnerLeftHand
                ? partner.GetLeftHandBone()
                : partner.GetRightHandBone())
            : null;

        if (ownHand == null
            || partnerHand == null)
        {
            ikAnimator.SetIKPositionWeight(
                goal,
                0f);

            ikAnimator.SetIKRotationWeight(
                goal,
                0f);

            return;
        }

        float pairDistance =
            Vector3.Distance(
                ownHand.position,
                partnerHand.position);

        float combinedArmReach = GetArmReach(ownHand)
            + (partner != null ? partner.GetArmReach(partnerHand) : 0f);
        float allowedPairDistance = Mathf.Max(
            0.25f,
            maximumHandPairDistance,
            combinedArmReach * 1.2f);

        if (pairDistance > allowedPairDistance)
        {
            ikAnimator.SetIKPositionWeight(
                goal,
                0f);

            ikAnimator.SetIKRotationWeight(
                goal,
                0f);

            return;
        }

        Vector3 targetPosition =
            Vector3.Lerp(
                ownHand.position,
                partnerHand.position,
                0.5f);

        targetPosition +=
            Vector3.up
            * handHoldHeightOffset;

        ikAnimator.SetIKPositionWeight(
            goal,
            Mathf.Clamp01(
                handHoldIKWeight));

        ikAnimator.SetIKPosition(
            goal,
            targetPosition);

        float rotationWeight =
            Mathf.Clamp01(
                handHoldRotationWeight);

        ikAnimator.SetIKRotationWeight(
            goal,
            rotationWeight);

        if (rotationWeight > 0f)
        {
            Quaternion targetRotation =
                Quaternion.Slerp(
                    ownHand.rotation,
                    partnerHand.rotation,
                    0.5f);

            ikAnimator.SetIKRotation(
                goal,
                targetRotation);
        }
    }

    private float GetArmReach(Transform hand)
    {
        if (hand == null)
        {
            return 0f;
        }

        Transform upperArm = hand == leftHandBone
            ? leftUpperArmBone
            : rightUpperArmBone;
        Transform lowerArm = hand == leftHandBone
            ? leftLowerArmBone
            : rightLowerArmBone;

        return GetArmChainLength(upperArm, lowerArm, hand);
    }

    public float GetAverageArmReach()
    {
        if (leftUpperArmBone == null
            || leftLowerArmBone == null
            || leftHandBone == null
            || rightUpperArmBone == null
            || rightLowerArmBone == null
            || rightHandBone == null)
        {
            CacheHumanoidHands();
        }

        float total = 0f;
        int count = 0;
        float leftReach = GetArmChainLength(
            leftUpperArmBone,
            leftLowerArmBone,
            leftHandBone);
        float rightReach = GetArmChainLength(
            rightUpperArmBone,
            rightLowerArmBone,
            rightHandBone);

        if (leftReach > 0.01f)
        {
            total += leftReach;
            count++;
        }

        if (rightReach > 0.01f)
        {
            total += rightReach;
            count++;
        }

        return count > 0 ? total / count : 0f;
    }

    private void ClearHandIK(
        Animator ikAnimator)
    {
        ikAnimator.SetIKPositionWeight(
            AvatarIKGoal.LeftHand,
            0f);

        ikAnimator.SetIKPositionWeight(
            AvatarIKGoal.RightHand,
            0f);

        ikAnimator.SetIKRotationWeight(
            AvatarIKGoal.LeftHand,
            0f);

        ikAnimator.SetIKRotationWeight(
            AvatarIKGoal.RightHand,
            0f);
    }

    private Chapter1CircleDancer FindNeighbor(
        bool clockwise)
    {
        if (center == null)
        {
            return null;
        }

        float ownAngle =
            GetCircleAngle(transform.position);

        Chapter1CircleDancer best = null;
        float bestDelta = 999f;

        for (int i = 0; i < activeDancers.Count; i++)
        {
            Chapter1CircleDancer candidate =
                activeDancers[i];

            if (candidate == null
                || candidate == this
                || !candidate.IsActivelyDancing
                || candidate.center != center)
            {
                continue;
            }

            float candidateAngle =
                GetCircleAngle(
                    candidate.transform.position);

            float delta;

            if (clockwise)
            {
                delta =
                    Mathf.Repeat(
                        candidateAngle
                        - ownAngle,
                        Mathf.PI * 2f);
            }
            else
            {
                delta =
                    Mathf.Repeat(
                        ownAngle
                        - candidateAngle,
                        Mathf.PI * 2f);
            }

            if (delta > 0.0001f
                && delta < bestDelta)
            {
                bestDelta = delta;
                best = candidate;
            }
        }

        return best;
    }

    private float GetCircleAngle(
        Vector3 worldPosition)
    {
        Vector3 offset =
            worldPosition
            - center.position;

        return Mathf.Atan2(
            offset.x,
            offset.z);
    }

    public Transform GetLeftHandBone()
    {
        if (leftHandBone == null)
        {
            CacheHumanoidHands();
        }

        return leftHandBone;
    }

    public Transform GetRightHandBone()
    {
        if (rightHandBone == null)
        {
            CacheHumanoidHands();
        }

        return rightHandBone;
    }

    private float GetCurrentRootToLowestFootOffset()
    {
        if (!useHumanoidFeetForGrounding
            || animator == null
            || !animator.isHuman)
        {
            return -1f;
        }

        if (leftFootBone == null
            || rightFootBone == null)
        {
            CacheHumanoidHands();
        }

        if (leftFootBone == null
            && rightFootBone == null)
        {
            return -1f;
        }

        float lowestFootY =
            float.PositiveInfinity;

        if (leftFootBone != null)
        {
            lowestFootY =
                Mathf.Min(
                    lowestFootY,
                    leftFootBone.position.y);
        }

        if (rightFootBone != null)
        {
            lowestFootY =
                Mathf.Min(
                    lowestFootY,
                    rightFootBone.position.y);
        }

        if (float.IsPositiveInfinity(lowestFootY))
        {
            return -1f;
        }

        return transform.position.y - lowestFootY;
    }

    private float GetFeetPenetrationCorrection()
    {
        if (animator == null
            || !animator.isHuman)
        {
            return 0f;
        }

        if (leftFootBone == null
            || rightFootBone == null)
        {
            CacheHumanoidHands();
        }

        float correction = 0f;

        if (leftFootBone != null
            && TryGetGroundHeight(
                leftFootBone.position,
                out float leftGroundY))
        {
            float leftPenetration =
                leftGroundY
                + footGroundClearance
                - leftFootBone.position.y;

            if (leftPenetration > 0f)
            {
                correction =
                    Mathf.Max(
                        correction,
                        leftPenetration);
            }
        }

        if (rightFootBone != null
            && TryGetGroundHeight(
                rightFootBone.position,
                out float rightGroundY))
        {
            float rightPenetration =
                rightGroundY
                + footGroundClearance
                - rightFootBone.position.y;

            if (rightPenetration > 0f)
            {
                correction =
                    Mathf.Max(
                        correction,
                        rightPenetration);
            }
        }

        return Mathf.Clamp(
            correction,
            0f,
            Mathf.Max(
                0f,
                maxFootPenetrationCorrection));
    }

    private void RefreshSharedGroundHeight()
    {
        sharedGroundReady = false;

        if (!lockAllDancersToSharedGround
            || center == null)
        {
            return;
        }

        Vector3 origin =
            center.position
            + Vector3.up
            * Mathf.Max(
                1f,
                sharedGroundProbeUp);

        float distance =
            Mathf.Max(
                2f,
                sharedGroundProbeUp
                + sharedGroundProbeDown);

        int hitCount =
            Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                groundHits,
                distance,
                groundLayers,
                QueryTriggerInteraction.Ignore);

        if (hitCount <= 0)
        {
            return;
        }

        // 烤乳豬、木架、火堆通常都在真正地面上方。
        // 所以這裡選「最低的有效朝上表面」作為整個舞圈共同地面。
        float lowestValidY =
            float.PositiveInfinity;

        bool found = false;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit =
                groundHits[i];

            if (hit.collider == null
                || hit.normal.y < minimumGroundNormalY)
            {
                continue;
            }

            Transform hitTransform =
                hit.collider.transform;

            if (BelongsToAnyDancer(hitTransform))
            {
                continue;
            }

            if (ignoreRoastedPigCollidersForGround
                && roastedPigCenter != null
                && (hitTransform == roastedPigCenter
                    || hitTransform.IsChildOf(roastedPigCenter)))
            {
                continue;
            }

            if (hit.point.y < lowestValidY)
            {
                lowestValidY =
                    hit.point.y;

                found = true;
            }
        }

        if (found)
        {
            sharedGroundY =
                lowestValidY;

            sharedGroundReady =
                true;

            lastGroundY =
                sharedGroundY;

            hasLastGroundY =
                true;
        }
    }

    private void CalibrateGroundOffset()
    {
        if (!followGround)
        {
            return;
        }

        if (TryGetGroundHeight(
            transform.position,
            out float groundY))
        {
            // 角色一開始通常已經正確站在場景地面上。
            // 直接保存 Root 到地面的高度差，比不同模型的腳骨比例更可靠。
            groundRootOffset =
                preserveInitialGroundOffset
                    ? transform.position.y - groundY
                    : 0f;

            groundOffsetCalibrated = true;
            lastGroundY = groundY;
            hasLastGroundY = true;
            initialGroundY = groundY;
            hasInitialGroundY = true;
        }
    }

    private bool TryGetGroundHeight(
        Vector3 aroundPosition,
        out float groundY)
    {
        groundY = 0f;

        Vector3 origin =
            new Vector3(
                aroundPosition.x,
                Mathf.Max(
                    transform.position.y,
                    aroundPosition.y)
                + Mathf.Max(0.2f, groundProbeUp),
                aroundPosition.z);

        float distance =
            Mathf.Max(
                0.5f,
                groundProbeUp + groundProbeDown);

        int hitCount =
            Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                groundHits,
                distance,
                groundLayers,
                QueryTriggerInteraction.Ignore);

        if (hitCount <= 0)
        {
            return false;
        }

        bool found = false;
        float bestScore = float.PositiveInfinity;
        float expectedGroundY =
            hasLastGroundY
                ? lastGroundY
                : transform.position.y - groundRootOffset;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit =
                groundHits[i];

            if (hit.collider == null)
            {
                continue;
            }

            if (hit.normal.y < minimumGroundNormalY)
            {
                continue;
            }

            Transform hitTransform =
                hit.collider.transform;

            if (BelongsToAnyDancer(hitTransform))
            {
                continue;
            }

            if (ignoreRoastedPigCollidersForGround
                && roastedPigCenter != null
                && (hitTransform == roastedPigCenter
                    || hitTransform.IsChildOf(roastedPigCenter)))
            {
                continue;
            }

            float delta =
                Mathf.Abs(
                    hit.point.y
                    - expectedGroundY);

            // 走斜坡時地面高度會慢慢變，
            // 但屋頂、木架、烤架等通常會突然差很多。
            if (hasLastGroundY
                && delta > Mathf.Max(0.1f, maxGroundHeightJump))
            {
                continue;
            }

            if (delta < bestScore)
            {
                bestScore = delta;
                groundY = hit.point.y;
                found = true;
            }
        }

        return found;
    }

    private bool BelongsToAnyDancer(
        Transform hitTransform)
    {
        if (hitTransform == null)
        {
            return false;
        }

        for (int i = 0; i < activeDancers.Count; i++)
        {
            Chapter1CircleDancer dancer =
                activeDancers[i];

            if (dancer == null)
            {
                continue;
            }

            Transform dancerTransform =
                dancer.transform;

            if (hitTransform == dancerTransform
                || hitTransform.IsChildOf(dancerTransform))
            {
                return true;
            }
        }

        return false;
    }

    private void ResolveCenter()
    {
        if (center != null)
        {
            return;
        }

        GameObject fallbackCenter =
            GameObject.Find(
                fallbackCenterName);

        if (fallbackCenter != null)
        {
            center =
                fallbackCenter.transform;
        }
    }

    public void SetDancing(bool shouldDance)
    {
        if (!canCircleDance)
        {
            dancing = false;
            ForceIdle();
            return;
        }

        bool wasDancing = dancing;
        dancing = shouldDance;

        if (dancing && !wasDancing)
        {
            RecalculateCirclePlacement();
            PlayDanceAnimation();
        }
        else if (!dancing && wasDancing)
        {
            RestoreVisualRoot();
        }
    }

    public void StopDancing()
    {
        dancing = false;
        RestoreVisualRoot();

        if (!canCircleDance)
        {
            ForceIdle();
        }
    }

    public void SetCanCircleDance(bool allowed)
    {
        canCircleDance = allowed;

        if (!allowed)
        {
            dancing = false;
            RestoreVisualRoot();
            ForceIdle();
        }
    }

    private void RecalculateCirclePlacement()
    {
        ResolveCenter();

        if (center == null)
        {
            return;
        }

        Vector3 offset =
            transform.position
            - center.position;

        offset.y = 0f;

        if (offset.sqrMagnitude < 0.25f)
        {
            offset =
                transform.forward
                * Mathf.Max(
                    2f,
                    minimumRadius);
        }

        radius =
            Mathf.Max(
                minimumRadius,
                offset.magnitude);

        baseHeight =
            transform.position.y;

        groundOffsetCalibrated = false;
        hasLastGroundY = false;
        hasInitialGroundY = false;
        sharedGroundReady = false;

        startAngle =
            Mathf.Atan2(
                offset.x,
                offset.z);

        initialOrderAngleCaptured = false;
    }

    private void KeepDanceAnimatorAlive()
    {
        if (!keepDanceAnimationPlaying
            || animator == null
            || animator.runtimeAnimatorController == null
            || string.IsNullOrWhiteSpace(danceStateName))
        {
            return;
        }

        if (Time.time < nextDanceStateCheckTime)
        {
            return;
        }

        nextDanceStateCheckTime =
            Time.time
            + Mathf.Max(
                0.10f,
                danceStateCheckInterval);

        int danceHash =
            Animator.StringToHash(
                danceStateName.Trim());

        if (!animator.HasState(0, danceHash))
        {
            return;
        }

        AnimatorStateInfo current =
            animator.GetCurrentAnimatorStateInfo(0);

        // 只要不是 Dance，就柔和切回 Dance。
        if (current.shortNameHash != danceHash)
        {
            animator.CrossFade(
                danceHash,
                0.12f,
                0);
        }

        // 保證真正的舞蹈 Clip 仍是快版速度。
        animator.speed =
            Mathf.Max(
                1.50f,
                animatorDanceSpeed
                * tempoScale);
    }

    private void PlayDanceAnimation()
    {
        if (animator == null)
        {
            return;
        }

        lastAnimatorSpeed =
            animator.speed;

        animator.applyRootMotion = false;

        animator.speed =
            Mathf.Max(
                1.50f,
                animatorDanceSpeed
                * tempoScale);

        if (!playDanceAnimatorState
            || animator.runtimeAnimatorController == null
            || string.IsNullOrWhiteSpace(danceStateName))
        {
            return;
        }

        int danceHash =
            Animator.StringToHash(
                danceStateName.Trim());

        if (animator.HasState(0, danceHash))
        {
            animator.CrossFade(
                danceHash,
                0.20f,
                0);
        }
    }

    private void ForceIdle()
    {
        if (!forceIdleWhenExcluded)
        {
            return;
        }

        if (animator == null)
        {
            animator =
                GetComponentInChildren<Animator>(
                    true);
        }

        if (animator == null
            || animator.runtimeAnimatorController == null)
        {
            return;
        }

        animator.applyRootMotion = false;
        animator.speed = 1f;

        if (string.IsNullOrWhiteSpace(idleStateName))
        {
            return;
        }

        int idleHash =
            Animator.StringToHash(
                idleStateName.Trim());

        if (animator.HasState(0, idleHash))
        {
            animator.CrossFade(
                idleHash,
                0.15f,
                0);
        }
    }

    private void RestoreVisualRoot()
    {
        if (visualRoot != null
            && visualBaseSaved)
        {
            visualRoot.localRotation =
                visualBaseLocalRotation;
        }

        if (animator != null)
        {
            animator.speed =
                Mathf.Max(
                    0.05f,
                    lastAnimatorSpeed);
        }
    }

    private float SmootherStep(float t)
    {
        t = Mathf.Clamp01(t);

        return t
            * t
            * t
            * (t
                * (6f * t - 15f)
                + 10f);
    }
}
