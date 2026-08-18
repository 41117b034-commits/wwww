using System.Collections;
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
    public float inspectCloserDistance = 0.08f;

    [Tooltip("檢查手時整組手的額外旋轉。")]
    public Vector3 inspectEulerOffset = new Vector3(-9f, 0f, 0f);

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

        // 2. 稍微靠近、翻手
        Vector3 inspectPosition =
            shownRootWorldPosition
            + playerCamera.transform.forward
            * inspectCloserDistance;

        Quaternion inspectRotation =
            shownRootWorldRotation
            * Quaternion.Euler(
                inspectEulerOffset);

        elapsed = 0f;

        while (elapsed < inspectMoveSeconds)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float n =
                Mathf.Clamp01(
                    elapsed
                    / Mathf.Max(
                        0.1f,
                        inspectMoveSeconds));

            float amount =
                Mathf.Sin(
                    n * Mathf.PI);

            handsRoot.position =
                Vector3.Lerp(
                    shownRootWorldPosition,
                    inspectPosition,
                    amount);

            handsRoot.rotation =
                Quaternion.Slerp(
                    shownRootWorldRotation,
                    inspectRotation,
                    amount);

            yield return null;
        }

        handsRoot.position =
            shownRootWorldPosition;

        handsRoot.rotation =
            shownRootWorldRotation;

        // 3. 鏡頭回正
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

        // 4. 看完後先稍微沉一下，避免直接突然收掉。
        Vector3 settleTarget =
            shownRootWorldPosition
            - playerCamera.transform.up * 0.08f;

        Vector3 settleStart =
            handsRoot.position;

        elapsed = 0f;

        while (elapsed < settleSeconds)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float t =
                Smooth(
                    elapsed
                    / Mathf.Max(
                        0.05f,
                        settleSeconds));

            handsRoot.position =
                Vector3.Lerp(
                    settleStart,
                    settleTarget,
                    t);

            yield return null;
        }

        handsRoot.position = settleTarget;

        // 5. 雙手自然往下、往身體方向收回。
        if (retractHandsAfterInspect)
        {
            Transform cam =
                playerCamera.transform;

            Vector3 retractStart =
                handsRoot.position;

            Vector3 retractTarget =
                retractStart
                - cam.up * Mathf.Max(0.05f, retractDownDistance)
                - cam.forward * Mathf.Max(0f, retractBackDistance);

            Quaternion retractStartRotation =
                handsRoot.rotation;

            Quaternion retractTargetRotation =
                retractStartRotation
                * Quaternion.Euler(12f, 0f, 0f);

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

                handsRoot.rotation =
                    Quaternion.Slerp(
                        retractStartRotation,
                        retractTargetRotation,
                        t);

                yield return null;
            }

            handsRoot.position =
                retractTarget;

            handsRoot.rotation =
                retractTargetRotation;
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
