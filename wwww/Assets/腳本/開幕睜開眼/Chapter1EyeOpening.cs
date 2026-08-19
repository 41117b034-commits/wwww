using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Chapter1EyeOpening : MonoBehaviour
{
    [Header("主要參考")]
    public Chapter1PerformanceController chapterController;
    public EyeOpenEffect eyeOpenEffect;

    [Tooltip("可留 None，自動抓 Main Camera。")]
    public Camera playerCamera;

    [Tooltip("可留 None，自動抓 Main Camera 的父物件 Camera Offset。")]
    public Transform cameraOffsetRoot;

    [Tooltip("拖整個 Hands_Rigged。")]
    public GameObject introHandsRoot;

    [Header("開始時間")]
    public float fallbackDelay = 4.35f;
    public float extraDelay = 0.08f;

    [Header("自然睜眼")]
    public bool useNaturalWakeUp = true;
    public float pauseAfterEyesOpen = 0.25f;

    [Header("開場隱藏雙手")]
    [Tooltip("勾選後，遊戲一開始完全看不到手；睜眼完成後才啟用 Hands_Rigged。")]
    public bool hideHandsUntilEyesOpen = true;

    [Header("看手鏡頭")]
    public bool cameraLooksAtHands = true;
    [Range(5f, 25f)] public float lookDownDegrees = 15f;
    public float cameraLookDownSeconds = 0.85f;
    public float cameraReturnSeconds = 0.70f;

    [Header("手在玩家眼前的位置")]
    [Tooltip("這裡是『手模型可見外觀的中心』到眼睛的距離，不再使用 FBX Root/Pivot。")]
    public float handsForwardDistance = 0.42f;

    [Tooltip("負值代表手在眼睛下方。")]
    public float handsVerticalOffset = -0.14f;

    [Tooltip("手一開始藏在畫面下方多少。")]
    public float hiddenDownDistance = 0.55f;

    [Header("手模型自動縮放 - 建議開啟")]
    [Tooltip("你的舊 FBX 比例很怪，開啟後會依 Renderer 實際大小自動縮放到適合第一人稱的尺寸。")]
    public bool autoFitHandsSize = true;

    [Tooltip("整雙手可見 Bounds 的最大尺寸，建議 0.55~0.75 公尺。")]
    public float targetHandsMaxSize = 0.65f;

    [Tooltip("防止錯誤模型被縮放到極端數值。")]
    public float minAutoScaleMultiplier = 0.001f;

    [Tooltip("防止錯誤模型被放大到極端數值。")]
    public float maxAutoScaleMultiplier = 1000f;

    public float handRaiseSeconds = 0.95f;

    [Header("看手動作")]
    public float inspectHoldSeconds = 0.55f;
    public float inspectMoveSeconds = 1.30f;

    [Tooltip("檢查手時，整組手往臉靠近多少。")]
    public float inspectCloserDistance = 0f;

    [Tooltip("檢查手時整組手的額外旋轉。")]
    public Vector3 inspectEulerOffset = Vector3.zero;

    [Header("觀察自己：快握拳再鬆開")]
    [Tooltip("看手時會做一次接近握拳、再放鬆的動作。")]
    public bool playFingerFlexWhileInspecting = true;

    [Tooltip("可留空，會自動找 Hands_Rigged/Hand_Left。")]
    public Transform leftFingerBoneRoot;

    [Tooltip("可留空，會自動找 Hands_Rigged/Hand_Right。")]
    public Transform rightFingerBoneRoot;

    [Range(0f, 1f)]
    [Tooltip("0=完全不握，1=完整握拳。建議 0.68~0.78，像在觀察自己的手而不是用力握拳。")]
    public float almostFistAmount = 0.18f;

    [Tooltip("手指自然彎曲時間。不要太快，像是在確認自己的手。")]
    public float fingerCloseSeconds = 0.85f;

    [Tooltip("接近握拳後短暫停一下。")]
    public float fingerHoldSeconds = 0.08f;

    [Tooltip("放鬆張開時間，略慢於握起來會比較自然。")]
    public float fingerOpenSeconds = 1.05f;

    [Tooltip("左右手不要完全同步；右手會稍微慢一點。")]
    public float rightHandFingerDelay = 0.06f;

    [Range(0f, 0.3f)]
    [Tooltip("不同手指的彎曲量稍微不同，避免像機器手。")]
    public float fingerCurlVariation = 0.01f;

    [Tooltip("完全放鬆後停一下，再進下一段。")]
    public float relaxedPauseSeconds = 0.20f;

    [Tooltip("握放期間整雙手只有很輕微的手腕動作。")]
    public Vector3 fingerGestureWristEuler = Vector3.zero;

    public enum FingerCurlAxis
    {
        X,
        Y,
        Z
    }

    [Tooltip("這個 Blender Rig 預設先用 X；如果手指往側邊歪，改 Z 試。")]
    public FingerCurlAxis fingerCurlAxis = FingerCurlAxis.X;

    [Tooltip("每節左手指骨最大彎曲角度。")]
    public float leftFingerCurlDegrees = 8f;

    [Tooltip("每節右手指骨最大彎曲角度。若右手反方向，改成 +48。")]
    public float rightFingerCurlDegrees = 8f;

    [Tooltip("跳過 Hand_Left / Hand_Right 底下第一層掌骨，只彎更深層的手指骨。")]
    [Range(1, 4)]
    public int minimumFingerBoneDepth = 2;

    [Tooltip("Play 模式按 L 只測試握拳/鬆開，不重播整個開場。")]
    public bool debugFingerFlexWithL = true;

    public float settleSeconds = 0.35f;

    [Header("看完後收手")]
    [Tooltip("看完手後，雙手會往畫面下方收回。")]
    public bool retractHandsAfterInspect = true;

    [Tooltip("收手動畫時間。")]
    public float retractHandsSeconds = 0.75f;

    [Tooltip("收手時往下移動多少。")]
    public float retractDownDistance = 0.75f;

    [Tooltip("收手時稍微往玩家身體方向退回多少。")]
    public float retractBackDistance = 0.12f;

    [Header("顯示修正")]
    [Tooltip("自動強制開啟 Hand.L / Hand.R 的 Renderer。")]
    public bool forceHandRenderersVisible = true;

    [Tooltip("SkinnedMeshRenderer 離開畫面時仍更新，避免重新進畫面後不刷新。")]
    public bool updateSkinnedMeshWhenOffscreen = true;

    [Header("結束")]
    public bool hideHandsAfterSequence = true;

    [Header("測試")]
    [Tooltip("Play 模式按 K 立即重播看手動畫。")]
    public bool debugReplayWithK = true;

    private bool sequenceRunning;
    private Quaternion cameraOffsetStartRotation;

    // 注意：這是「Hands_Rigged Root 的目標世界位置」，
    // 已經扣除 FBX 奇怪 Pivot 與 Mesh Bounds 偏移。
    private Vector3 shownRootWorldPosition;
    private Quaternion shownRootWorldRotation;

    private readonly List<Transform> leftFingerBones = new List<Transform>();
    private readonly List<Quaternion> leftFingerOpenRotations = new List<Quaternion>();
    private readonly List<Transform> rightFingerBones = new List<Transform>();
    private readonly List<Quaternion> rightFingerOpenRotations = new List<Quaternion>();
    private bool fingerPoseCached;
    private bool fingerFlexRunning;

    private IEnumerator Start()
    {
        ResolveReferences();

        if (!ValidateReferences())
        {
            yield break;
        }

        if (hideHandsUntilEyesOpen && introHandsRoot != null)
        {
            // Chapter1EyeOpening 掛在 IntroSequence，不是在 Hands_Rigged 上，
            // 所以可以安全把整雙手關掉，不會中斷本 Coroutine。
            introHandsRoot.SetActive(false);
        }
        else
        {
            PrepareHands();
            yield return null;
            AutoFitHandsToCamera();
        }

        float delay = fallbackDelay;

        if (chapterController != null && chapterController.introVoice1 != null)
        {
            delay =
                chapterController.introVoice1.length
                + chapterController.introLineGap;
        }

        delay += Mathf.Max(0f, extraDelay);

        yield return WaitRealtime(delay);

        sequenceRunning = true;

        if (eyeOpenEffect != null)
        {
            if (useNaturalWakeUp)
            {
                yield return eyeOpenEffect.NaturalWakeUpRoutine();
            }
            else
            {
                eyeOpenEffect.OpenEyes();
                yield return WaitRealtime(
                    Mathf.Max(0.1f, eyeOpenEffect.openDuration));
            }
        }

        yield return WaitRealtime(pauseAfterEyesOpen);

        if (hideHandsUntilEyesOpen && introHandsRoot != null)
        {
            // 在看手段落開始前才把手打開。
            introHandsRoot.SetActive(true);

            // 先立刻移到鏡頭下方的安全位置，避免啟用時閃現在畫面中央。
            if (playerCamera != null)
            {
                Transform handsRoot = introHandsRoot.transform;
                Vector3 safeHiddenPosition =
                    playerCamera.transform.position
                    + playerCamera.transform.forward * 0.45f
                    - playerCamera.transform.up * 1.2f;

                handsRoot.position = safeHiddenPosition;
            }

            PrepareHands();

            // Skinned Mesh 開啟後等一幀，讓 Bounds 更新，再自動縮放與定位。
            yield return null;
            AutoFitHandsToCamera();
        }

        Debug.Log("[Intro] 開始看手動畫。", this);
        yield return PlayLookAtHands();
        Debug.Log("[Intro] 看手動畫完成。", this);

        sequenceRunning = false;

        if (hideHandsAfterSequence && introHandsRoot != null)
        {
            introHandsRoot.SetActive(false);
        }
    }

    private void Update()
    {
        if (debugReplayWithK
            && Input.GetKeyDown(KeyCode.K)
            && !sequenceRunning)
        {
            StartCoroutine(DebugReplay());
        }

        if (debugFingerFlexWithL
            && Input.GetKeyDown(KeyCode.L)
            && !fingerFlexRunning
            && introHandsRoot != null
            && introHandsRoot.activeInHierarchy)
        {
            StartCoroutine(PlayFingerObserveGesture());
        }
    }

    private IEnumerator DebugReplay()
    {
        ResolveReferences();

        if (!ValidateReferences())
        {
            yield break;
        }

        sequenceRunning = true;

        if (introHandsRoot != null)
        {
            introHandsRoot.SetActive(true);
        }

        PrepareHands();

        yield return null;
        AutoFitHandsToCamera();

        Debug.Log("[Intro] K：立即重播看手。", this);

        yield return PlayLookAtHands();

        sequenceRunning = false;
    }

    private void ResolveReferences()
    {
        if (chapterController == null)
        {
            chapterController =
                FindFirstObjectByType<Chapter1PerformanceController>();
        }

        if (eyeOpenEffect == null)
        {
            eyeOpenEffect =
                FindFirstObjectByType<EyeOpenEffect>(
                    FindObjectsInactive.Include);
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (cameraOffsetRoot == null && playerCamera != null)
        {
            cameraOffsetRoot = playerCamera.transform.parent;
        }
    }

    private bool ValidateReferences()
    {
        if (playerCamera == null)
        {
            Debug.LogError("[Intro] 找不到 Main Camera。", this);
            return false;
        }

        if (cameraOffsetRoot == null)
        {
            Debug.LogError("[Intro] 找不到 Camera Offset。", this);
            return false;
        }

        if (introHandsRoot == null)
        {
            Debug.LogError("[Intro] Intro Hands Root 沒有拖 Hands_Rigged。", this);
            return false;
        }

        return true;
    }

    private void PrepareHands()
    {
        introHandsRoot.SetActive(true);

        // 手如果還在 Main Camera 底下，移到 XR Origin 底下。
        // 這樣鏡頭低頭時，手不會跟著鏡頭一起低頭。
        Transform handsRoot = introHandsRoot.transform;

        if (handsRoot.IsChildOf(playerCamera.transform))
        {
            Transform xrOrigin =
                cameraOffsetRoot.parent != null
                    ? cameraOffsetRoot.parent
                    : cameraOffsetRoot;

            handsRoot.SetParent(xrOrigin, true);
        }

        MakeAllHandRenderersVisible();
        ResolveAndCacheFingerBones();

        // 等同於保留 Blender 匯入時的模型朝向。
        shownRootWorldRotation = handsRoot.rotation;

        cameraOffsetStartRotation =
            cameraOffsetRoot.localRotation;
    }

    private void AutoFitHandsToCamera()
    {
        if (introHandsRoot == null || playerCamera == null)
        {
            return;
        }

        Transform handsRoot = introHandsRoot.transform;

        MakeAllHandRenderersVisible();

        if (!TryGetCombinedRendererBounds(out Bounds beforeBounds))
        {
            Debug.LogError(
                "[Intro] 找不到雙手 Renderer Bounds，無法自動縮放。",
                this);
            return;
        }

        float currentMaxSize =
            Mathf.Max(
                beforeBounds.size.x,
                Mathf.Max(
                    beforeBounds.size.y,
                    beforeBounds.size.z));

        if (autoFitHandsSize && currentMaxSize > 0.00001f)
        {
            float multiplier =
                Mathf.Clamp(
                    Mathf.Max(0.05f, targetHandsMaxSize) / currentMaxSize,
                    Mathf.Max(0.000001f, minAutoScaleMultiplier),
                    Mathf.Max(minAutoScaleMultiplier, maxAutoScaleMultiplier));

            handsRoot.localScale *= multiplier;

            // 強制讓 SkinnedMeshRenderer 重新計算目前姿勢的 Bounds。
            SkinnedMeshRenderer[] skinned =
                introHandsRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            for (int i = 0; i < skinned.Length; i++)
            {
                if (skinned[i] == null)
                {
                    continue;
                }

                skinned[i].updateWhenOffscreen = true;
                skinned[i].forceMatrixRecalculationPerRender = true;
            }

            Debug.Log(
                "[Intro] 雙手自動縮放：原最大尺寸="
                + currentMaxSize.ToString("0.###")
                + "m，倍率="
                + multiplier.ToString("0.####")
                + "，目標="
                + targetHandsMaxSize.ToString("0.###")
                + "m",
                this);
        }

        // 重新取得縮放後真正可見的中心，移到鏡頭前。
        if (TryGetCombinedRendererBounds(out Bounds fittedBounds))
        {
            Vector3 desiredCenter =
                GetDesiredVisibleHandsCenter();

            handsRoot.position +=
                desiredCenter - fittedBounds.center;

            shownRootWorldPosition =
                handsRoot.position;

            shownRootWorldRotation =
                handsRoot.rotation;

            // 額外印出相機視口座標。
            Vector3 viewport =
                playerCamera.WorldToViewportPoint(desiredCenter);

            Debug.Log(
                "[Intro] 雙手已放進畫面。Viewport=("
                + viewport.x.ToString("0.00") + ", "
                + viewport.y.ToString("0.00") + "), 深度="
                + viewport.z.ToString("0.00")
                + "m，Bounds Size="
                + fittedBounds.size,
                this);
        }
    }

    private void MakeAllHandRenderersVisible()
    {
        if (!forceHandRenderersVisible || introHandsRoot == null)
        {
            return;
        }

        Renderer[] renderers =
            introHandsRoot.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null)
            {
                continue;
            }

            renderer.enabled = true;
            renderer.forceRenderingOff = false;
            renderer.shadowCastingMode = ShadowCastingMode.On;

            if (renderer is SkinnedMeshRenderer skinned)
            {
                skinned.updateWhenOffscreen =
                    updateSkinnedMeshWhenOffscreen;
            }
        }
    }

    private Vector3 GetDesiredVisibleHandsCenter()
    {
        Transform cam = playerCamera.transform;

        return cam.position
            + cam.forward * handsForwardDistance
            + cam.up * handsVerticalOffset;
    }

    private void AlignVisibleBoundsCenterTo(Vector3 desiredCenter)
    {
        if (!TryGetCombinedRendererBounds(out Bounds bounds))
        {
            Debug.LogError(
                "[Intro] Hands_Rigged 找不到 Renderer，無法定位雙手。",
                this);
            return;
        }

        Vector3 delta =
            desiredCenter - bounds.center;

        introHandsRoot.transform.position += delta;

        Debug.Log(
            "[Intro] 雙手可見中心已校正。原本 Bounds Center="
            + bounds.center
            + "，目標="
            + desiredCenter
            + "，位移="
            + delta,
            this);
    }

    private bool TryGetCombinedRendererBounds(out Bounds combined)
    {
        combined = new Bounds();

        if (introHandsRoot == null)
        {
            return false;
        }

        Renderer[] renderers =
            introHandsRoot.GetComponentsInChildren<Renderer>(true);

        bool found = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null
                || !renderer.enabled
                || renderer.forceRenderingOff)
            {
                continue;
            }

            if (!found)
            {
                combined = renderer.bounds;
                found = true;
            }
            else
            {
                combined.Encapsulate(renderer.bounds);
            }
        }

        return found;
    }

    private IEnumerator PlayLookAtHands()
    {
        Transform handsRoot =
            introHandsRoot.transform;

        // 每次播放前再對準一次，避免玩家頭部位置改變。
        Vector3 desiredCenter =
            GetDesiredVisibleHandsCenter();

        // 先暫時回到顯示位置，再重新量 Bounds。
        handsRoot.position =
            shownRootWorldPosition;

        AlignVisibleBoundsCenterTo(desiredCenter);

        shownRootWorldPosition =
            handsRoot.position;

        shownRootWorldRotation =
            handsRoot.rotation;

        Vector3 hiddenRootPosition =
            shownRootWorldPosition
            - playerCamera.transform.up * hiddenDownDistance;

        handsRoot.position =
            hiddenRootPosition;

        cameraOffsetStartRotation =
            cameraOffsetRoot.localRotation;

        Quaternion lookDownRotation =
            cameraOffsetStartRotation
            * Quaternion.Euler(
                lookDownDegrees,
                0f,
                0f);

        // 1. 雙手抬起 + 鏡頭低頭
        float total =
            Mathf.Max(
                handRaiseSeconds,
                cameraLookDownSeconds);

        float elapsed = 0f;

        while (elapsed < total)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float handT =
                Smooth(
                    elapsed
                    / Mathf.Max(
                        0.05f,
                        handRaiseSeconds));

            float cameraT =
                Smooth(
                    elapsed
                    / Mathf.Max(
                        0.05f,
                        cameraLookDownSeconds));

            handsRoot.position =
                Vector3.Lerp(
                    hiddenRootPosition,
                    shownRootWorldPosition,
                    handT);

            if (cameraLooksAtHands)
            {
                cameraOffsetRoot.localRotation =
                    Quaternion.Slerp(
                        cameraOffsetStartRotation,
                        lookDownRotation,
                        cameraT);
            }

            yield return null;
        }

        handsRoot.position =
            shownRootWorldPosition;

        if (cameraLooksAtHands)
        {
            cameraOffsetRoot.localRotation =
                lookDownRotation;
        }

        yield return WaitRealtime(
            inspectHoldSeconds);

        // 2. 玩家像是在確認自己的手：快速接近握拳，再自然鬆開。
        if (playFingerFlexWhileInspecting)
        {
            yield return PlayFingerObserveGesture();
        }

        // 3. 手指放鬆後，不再讓整雙手靠近、翻轉或往外移。
        // 保持手掌完全停在原本觀察位置。
        handsRoot.position =
            shownRootWorldPosition;

        handsRoot.rotation =
            shownRootWorldRotation;

        yield return WaitRealtime(0.18f);

        // 4. 先收手，再讓鏡頭回正。
        // 這樣玩家看到的是「手直接往下收」，
        // 不會因鏡頭先轉回去而產生手往外飄的視覺。
        if (retractHandsAfterInspect)
        {
            Transform cam =
                playerCamera.transform;

            Vector3 retractStart =
                handsRoot.position;

            // 只往畫面下方與身體方向收，不加任何左右位移。
            Vector3 retractTarget =
                retractStart
                - cam.up * Mathf.Max(0.05f, retractDownDistance)
                - cam.forward * Mathf.Max(0f, retractBackDistance);

            Quaternion fixedRotation =
                shownRootWorldRotation;

            elapsed = 0f;

            float retractDuration =
                Mathf.Max(0.05f, retractHandsSeconds);

            while (elapsed < retractDuration)
            {
                elapsed += Time.unscaledDeltaTime;

                float t =
                    Smooth(
                        elapsed / retractDuration);

                handsRoot.position =
                    Vector3.Lerp(
                        retractStart,
                        retractTarget,
                        t);

                // 整個收手過程鎖死原本方向，不再往外翻。
                handsRoot.rotation =
                    fixedRotation;

                // 鏡頭保持低頭，不在這時候回正。
                if (cameraLooksAtHands)
                {
                    cameraOffsetRoot.localRotation =
                        lookDownRotation;
                }

                yield return null;
            }

            handsRoot.position =
                retractTarget;

            handsRoot.rotation =
                fixedRotation;
        }

        // 手已經離開視野後直接隱藏，避免鏡頭回正時又看到手漂出去。
        if (hideHandsAfterSequence && introHandsRoot != null)
        {
            introHandsRoot.SetActive(false);
        }

        // 5. 最後才讓鏡頭慢慢回正，此時手已經不在畫面裡。
        Quaternion cameraReturnStart =
            cameraOffsetRoot.localRotation;

        elapsed = 0f;

        while (elapsed < cameraReturnSeconds)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float t =
                Smooth(
                    elapsed
                    / Mathf.Max(
                        0.05f,
                        cameraReturnSeconds));

            if (cameraLooksAtHands)
            {
                cameraOffsetRoot.localRotation =
                    Quaternion.Slerp(
                        cameraReturnStart,
                        cameraOffsetStartRotation,
                        t);
            }

            yield return null;
        }

        cameraOffsetRoot.localRotation =
            cameraOffsetStartRotation;
    }

    private void ResolveAndCacheFingerBones()
    {
        if (introHandsRoot == null)
        {
            return;
        }

        if (leftFingerBoneRoot == null)
        {
            leftFingerBoneRoot =
                FindChildRecursive(
                    introHandsRoot.transform,
                    "Hand_Left");
        }

        if (rightFingerBoneRoot == null)
        {
            rightFingerBoneRoot =
                FindChildRecursive(
                    introHandsRoot.transform,
                    "Hand_Right");
        }

        leftFingerBones.Clear();
        leftFingerOpenRotations.Clear();
        rightFingerBones.Clear();
        rightFingerOpenRotations.Clear();

        CacheFingerBones(
            leftFingerBoneRoot,
            0,
            leftFingerBones,
            leftFingerOpenRotations);

        CacheFingerBones(
            rightFingerBoneRoot,
            0,
            rightFingerBones,
            rightFingerOpenRotations);

        fingerPoseCached =
            leftFingerBones.Count > 0
            || rightFingerBones.Count > 0;

        Debug.Log(
            "[Intro] 手指骨快取完成：左 "
            + leftFingerBones.Count
            + "，右 "
            + rightFingerBones.Count,
            this);
    }

    private Transform FindChildRecursive(
        Transform root,
        string targetName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == targetName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found =
                FindChildRecursive(
                    root.GetChild(i),
                    targetName);

            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private void CacheFingerBones(
        Transform current,
        int depth,
        List<Transform> bones,
        List<Quaternion> rotations)
    {
        if (current == null)
        {
            return;
        }

        if (depth >= minimumFingerBoneDepth)
        {
            bones.Add(current);
            rotations.Add(current.localRotation);
        }

        for (int i = 0; i < current.childCount; i++)
        {
            CacheFingerBones(
                current.GetChild(i),
                depth + 1,
                bones,
                rotations);
        }
    }

    private IEnumerator PlayFingerObserveGesture()
    {
        if (fingerFlexRunning)
        {
            yield break;
        }

        if (!fingerPoseCached)
        {
            ResolveAndCacheFingerBones();
        }

        if (!fingerPoseCached)
        {
            Debug.LogWarning(
                "[Intro] 找不到 Hand_Left / Hand_Right 的手指骨，略過手指觀察動畫。",
                this);
            yield break;
        }

        fingerFlexRunning = true;

        Transform handsRoot =
            introHandsRoot != null
                ? introHandsRoot.transform
                : null;

        Quaternion wristStartRotation =
            handsRoot != null
                ? handsRoot.rotation
                : Quaternion.identity;

        Quaternion wristGestureRotation =
            wristStartRotation
            * Quaternion.Euler(fingerGestureWristEuler);

        // 第一段：手指慢慢彎起來。
        // 不是完整握拳，只到約 60%，而且右手慢一點開始。
        float duration =
            Mathf.Max(
                0.08f,
                fingerCloseSeconds + rightHandFingerDelay);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float leftT =
                Smooth(
                    elapsed
                    / Mathf.Max(0.05f, fingerCloseSeconds));

            float rightT =
                Smooth(
                    (elapsed - rightHandFingerDelay)
                    / Mathf.Max(0.05f, fingerCloseSeconds));

            ApplyNaturalFingerCurl(
                leftT * almostFistAmount,
                rightT * almostFistAmount);

            // 握拳時不要轉整隻手腕，避免一握完手掌往外翻。
            if (handsRoot != null)
            {
                handsRoot.rotation = wristStartRotation;
            }

            yield return null;
        }

        ApplyNaturalFingerCurl(
            almostFistAmount,
            almostFistAmount);

        yield return WaitRealtime(
            Mathf.Max(0f, fingerHoldSeconds));

        // 第二段：鬆開比握起來稍微慢，
        // 左右手依然保留很小的時間差。
        duration =
            Mathf.Max(
                0.08f,
                fingerOpenSeconds + rightHandFingerDelay);

        elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float leftT =
                Smooth(
                    elapsed
                    / Mathf.Max(0.05f, fingerOpenSeconds));

            float rightT =
                Smooth(
                    (elapsed - rightHandFingerDelay)
                    / Mathf.Max(0.05f, fingerOpenSeconds));

            ApplyNaturalFingerCurl(
                Mathf.Lerp(almostFistAmount, 0f, leftT),
                Mathf.Lerp(almostFistAmount, 0f, rightT));

            // 放鬆時保持原本手掌方向，不做往外翻的回彈。
            if (handsRoot != null)
            {
                handsRoot.rotation = wristStartRotation;
            }

            yield return null;
        }

        ApplyNaturalFingerCurl(0f, 0f);

        if (handsRoot != null)
        {
            handsRoot.rotation =
                wristStartRotation;
        }

        // 完全放鬆後停一點點，
        // 看起來像玩家真的在感受自己的手，而不是播完一個機械動作。
        yield return WaitRealtime(
            Mathf.Max(0f, relaxedPauseSeconds));

        fingerFlexRunning = false;
    }

    private void ApplyNaturalFingerCurl(
        float leftAmount,
        float rightAmount)
    {
        Vector3 axis;

        switch (fingerCurlAxis)
        {
            case FingerCurlAxis.Y:
                axis = Vector3.up;
                break;

            case FingerCurlAxis.Z:
                axis = Vector3.forward;
                break;

            default:
                axis = Vector3.right;
                break;
        }

        ApplyNaturalCurlToHand(
            leftFingerBones,
            leftFingerOpenRotations,
            axis,
            leftFingerCurlDegrees,
            leftAmount,
            0);

        ApplyNaturalCurlToHand(
            rightFingerBones,
            rightFingerOpenRotations,
            axis,
            rightFingerCurlDegrees,
            rightAmount,
            1);
    }

    private void ApplyNaturalCurlToHand(
        List<Transform> bones,
        List<Quaternion> openRotations,
        Vector3 axis,
        float curlDegrees,
        float amount,
        int handSeed)
    {
        int count =
            Mathf.Min(
                bones.Count,
                openRotations.Count);

        for (int i = 0; i < count; i++)
        {
            Transform bone = bones[i];

            if (bone == null)
            {
                continue;
            }

            // 每根骨頭有固定但很小的差異。
            // 不用 Random，避免每幀跳動。
            float variationWave =
                Mathf.Sin(
                    (i + 1) * 1.73f
                    + handSeed * 2.11f);

            float variation =
                1f
                + variationWave
                * fingerCurlVariation;

            // 同一根手指越末端，稍微多彎一點。
            float jointWeight =
                0.42f
                + (i % 3) * 0.03f;

            float finalAmount =
                Mathf.Clamp01(
                    amount * variation);

            bone.localRotation =
                openRotations[i]
                * Quaternion.AngleAxis(
                    curlDegrees
                    * finalAmount
                    * jointWeight,
                    axis);
        }
    }

    private IEnumerator AnimateFingerCurl(
        float from,
        float to,
        float seconds)
    {
        float duration =
            Mathf.Max(0.05f, seconds);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t =
                Smooth(
                    elapsed / duration);

            float amount =
                Mathf.Lerp(
                    from,
                    to,
                    t);

            ApplyFingerCurl(amount);

            yield return null;
        }

        ApplyFingerCurl(to);
    }

    private void ApplyFingerCurl(float amount)
    {
        Vector3 axis;

        switch (fingerCurlAxis)
        {
            case FingerCurlAxis.Y:
                axis = Vector3.up;
                break;

            case FingerCurlAxis.Z:
                axis = Vector3.forward;
                break;

            default:
                axis = Vector3.right;
                break;
        }

        ApplyFingerCurlToHand(
            leftFingerBones,
            leftFingerOpenRotations,
            axis,
            leftFingerCurlDegrees,
            amount);

        ApplyFingerCurlToHand(
            rightFingerBones,
            rightFingerOpenRotations,
            axis,
            rightFingerCurlDegrees,
            amount);
    }

    private void ApplyFingerCurlToHand(
        List<Transform> bones,
        List<Quaternion> openRotations,
        Vector3 axis,
        float curlDegrees,
        float amount)
    {
        int count =
            Mathf.Min(
                bones.Count,
                openRotations.Count);

        for (int i = 0; i < count; i++)
        {
            Transform bone =
                bones[i];

            if (bone == null)
            {
                continue;
            }

            // 越靠近指尖稍微多彎一點，
            // 會比所有關節完全相同更像自然握拳。
            float depthBoost =
                0.88f
                + (i % 3) * 0.08f;

            bone.localRotation =
                openRotations[i]
                * Quaternion.AngleAxis(
                    curlDegrees
                    * amount
                    * depthBoost,
                    axis);
        }
    }

    private float Smooth(float value)
    {
        float t =
            Mathf.Clamp01(value);

        return t * t * (3f - 2f * t);
    }

    private IEnumerator WaitRealtime(float seconds)
    {
        float remaining =
            Mathf.Max(0f, seconds);

        while (remaining > 0f)
        {
            remaining -=
                Time.unscaledDeltaTime;

            yield return null;
        }
    }
}
