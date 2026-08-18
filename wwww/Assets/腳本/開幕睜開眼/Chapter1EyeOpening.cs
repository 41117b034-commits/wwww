using System.Collections;
using UnityEngine;

public class Chapter1EyeOpening : MonoBehaviour
{
    [Header("主要參考")]
    public Chapter1PerformanceController chapterController;
    public EyeOpenEffect eyeOpenEffect;

    [Tooltip("可留 None，會自動抓 Main Camera。")]
    public Camera playerCamera;

    [Tooltip("可留 None，會自動抓 Main Camera 的父物件 Camera Offset。")]
    public Transform cameraOffsetRoot;

    [Tooltip("拖入整個 Hands_Rigged。")]
    public GameObject introHandsRoot;

    [Header("開始時間")]
    public float fallbackDelay = 4.35f;
    public float extraDelay = 0.08f;

    [Header("自然睜眼")]
    public bool useNaturalWakeUp = true;
    public float pauseAfterEyesOpen = 0.25f;

    [Header("真正『看手』的鏡頭")]
    [Tooltip("鏡頭會真的往下看手，不只是把手移到畫面中。")]
    public bool cameraLooksAtHands = true;

    [Tooltip("VR 建議 12~18 度，不要太大。")]
    public float lookDownDegrees = 15f;

    public float cameraLookDownSeconds = 0.85f;
    public float cameraReturnSeconds = 0.70f;

    [Header("雙手位置")]
    [Tooltip("自動把雙手放到玩家前方，不用自己猜 Transform。")]
    public bool autoPlaceHandsInFront = true;

    [Tooltip("手距離眼睛多遠。")]
    public float handsForwardDistance = 0.58f;

    [Tooltip("手比眼睛低多少。負值=往下。")]
    public float handsVerticalOffset = -0.28f;

    [Tooltip("一開始把手藏到更下面。")]
    public float hiddenDownDistance = 0.58f;

    [Tooltip("一開始再往玩家方向縮一點。")]
    public float hiddenBackDistance = 0.12f;

    public float handRaiseSeconds = 0.95f;

    [Header("看手動作")]
    public float inspectHoldSeconds = 1.15f;
    public float inspectMoveSeconds = 0.75f;

    [Tooltip("看手時整雙手稍微靠近臉。")]
    public float inspectCloserDistance = 0.07f;

    [Tooltip("看手時整雙手稍微翻轉。")]
    public Vector3 inspectEulerOffset = new Vector3(-10f, 0f, 0f);

    [Header("結束")]
    public float settleSeconds = 0.45f;

    [Tooltip("測試時先不要勾，避免動畫結束後手立刻消失。")]
    public bool hideHandsAfterSequence = false;

    [Header("測試")]
    [Tooltip("Play 模式按 K 可立即重播看手段落，不用每次重跑前面。")]
    public bool debugReplayWithK = true;

    private bool sequenceRunning;

    private Quaternion cameraOffsetStartRotation;

    private Vector3 shownHandsWorldPosition;
    private Quaternion shownHandsWorldRotation;

    private Transform originalHandsParent;

    private IEnumerator Start()
    {
        ResolveReferences();

        if (playerCamera == null)
        {
            Debug.LogError("[Chapter1EyeOpening] 找不到 Main Camera。", this);
            yield break;
        }

        if (cameraOffsetRoot == null)
        {
            Debug.LogError("[Chapter1EyeOpening] 找不到 Camera Offset。", this);
            yield break;
        }

        if (introHandsRoot == null)
        {
            Debug.LogError("[Chapter1EyeOpening] Intro Hands Root 沒有拖 Hands_Rigged。", this);
            yield break;
        }

        PrepareHandsHierarchy();
        CacheShownHandPose();

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

        Debug.Log("[Intro] 開始自然睜眼。", this);

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

        Debug.Log("[Intro] 開始真正的看手動畫。", this);

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
            StartCoroutine(DebugReplayHands());
        }
    }

    private IEnumerator DebugReplayHands()
    {
        sequenceRunning = true;

        ResolveReferences();
        PrepareHandsHierarchy();
        CacheShownHandPose();

        Debug.Log("[Intro] K 鍵：立即重播看手動畫。", this);

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

        if (cameraOffsetRoot == null
            && playerCamera != null)
        {
            // 你的結構：
            // XR Origin (VR)
            // └─ Camera Offset
            //    └─ Main Camera
            cameraOffsetRoot =
                playerCamera.transform.parent;
        }
    }

    private void PrepareHandsHierarchy()
    {
        if (introHandsRoot == null
            || playerCamera == null
            || cameraOffsetRoot == null)
        {
            return;
        }

        Transform hands = introHandsRoot.transform;

        originalHandsParent = hands.parent;

        // 關鍵修正：
        // Hands_Rigged 如果放在 Main Camera 底下，
        // 鏡頭低頭時手也會一起跟鏡頭轉，看起來就像「沒有看手」。
        // 把它移到 XR Origin 底下，讓鏡頭和手可以相對移動。
        Transform xrOrigin =
            cameraOffsetRoot.parent;

        if (xrOrigin != null
            && hands.IsChildOf(playerCamera.transform))
        {
            hands.SetParent(xrOrigin, true);

            Debug.Log(
                "[Intro] Hands_Rigged 已自動移出 Main Camera，改放到 XR Origin 底下。",
                this);
        }

        introHandsRoot.SetActive(true);
    }

    private void CacheShownHandPose()
    {
        if (introHandsRoot == null || playerCamera == null)
        {
            return;
        }

        Transform cam = playerCamera.transform;
        Transform hands = introHandsRoot.transform;

        if (autoPlaceHandsInFront)
        {
            shownHandsWorldPosition =
                cam.position
                + cam.forward * handsForwardDistance
                + cam.up * handsVerticalOffset;
        }
        else
        {
            shownHandsWorldPosition =
                hands.position;
        }

        // 保留你 FBX 原本正確的朝向，
        // 不強制把模型旋轉成 Camera.rotation。
        shownHandsWorldRotation =
            hands.rotation;

        cameraOffsetStartRotation =
            cameraOffsetRoot.localRotation;
    }

    private IEnumerator PlayLookAtHands()
    {
        if (introHandsRoot == null
            || playerCamera == null
            || cameraOffsetRoot == null)
        {
            yield break;
        }

        Transform cam = playerCamera.transform;
        Transform hands = introHandsRoot.transform;

        introHandsRoot.SetActive(true);

        // 每次播放都以目前頭部方向重新計算一次位置。
        if (autoPlaceHandsInFront)
        {
            shownHandsWorldPosition =
                cam.position
                + cam.forward * handsForwardDistance
                + cam.up * handsVerticalOffset;
        }

        cameraOffsetStartRotation =
            cameraOffsetRoot.localRotation;

        Vector3 hiddenPosition =
            shownHandsWorldPosition
            - cam.up * hiddenDownDistance
            - cam.forward * hiddenBackDistance;

        hands.position = hiddenPosition;
        hands.rotation =
            shownHandsWorldRotation
            * Quaternion.Euler(12f, 0f, 0f);

        Quaternion lookDownRotation =
            cameraOffsetStartRotation
            * Quaternion.Euler(lookDownDegrees, 0f, 0f);

        // 1) 手抬起，同時鏡頭慢慢低頭。
        float total =
            Mathf.Max(handRaiseSeconds, cameraLookDownSeconds);

        float elapsed = 0f;

        Vector3 handStart = hands.position;
        Quaternion handStartRot = hands.rotation;

        while (elapsed < total)
        {
            elapsed += Time.unscaledDeltaTime;

            float handT =
                Smooth(
                    elapsed / Mathf.Max(0.05f, handRaiseSeconds));

            float cameraT =
                Smooth(
                    elapsed / Mathf.Max(0.05f, cameraLookDownSeconds));

            hands.position =
                Vector3.Lerp(
                    handStart,
                    shownHandsWorldPosition,
                    handT);

            hands.rotation =
                Quaternion.Slerp(
                    handStartRot,
                    shownHandsWorldRotation,
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

        hands.position = shownHandsWorldPosition;
        hands.rotation = shownHandsWorldRotation;

        if (cameraLooksAtHands)
        {
            cameraOffsetRoot.localRotation =
                lookDownRotation;
        }

        // 2) 停一下，讓玩家真的「看到自己的手」。
        yield return WaitRealtime(inspectHoldSeconds);

        // 3) 手靠近一點、翻一下。
        Vector3 inspectPosition =
            shownHandsWorldPosition
            + cam.forward * inspectCloserDistance;

        Quaternion inspectRotation =
            shownHandsWorldRotation
            * Quaternion.Euler(inspectEulerOffset);

        elapsed = 0f;
        float inspectDuration =
            Mathf.Max(0.1f, inspectMoveSeconds);

        while (elapsed < inspectDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float n =
                Mathf.Clamp01(
                    elapsed / inspectDuration);

            float amount =
                Mathf.Sin(n * Mathf.PI);

            hands.position =
                Vector3.Lerp(
                    shownHandsWorldPosition,
                    inspectPosition,
                    amount);

            hands.rotation =
                Quaternion.Slerp(
                    shownHandsWorldRotation,
                    inspectRotation,
                    amount);

            yield return null;
        }

        hands.position = shownHandsWorldPosition;
        hands.rotation = shownHandsWorldRotation;

        yield return WaitRealtime(0.20f);

        // 4) 鏡頭抬回原本視角。
        Quaternion returnStart =
            cameraOffsetRoot.localRotation;

        elapsed = 0f;
        float returnDuration =
            Mathf.Max(0.05f, cameraReturnSeconds);

        while (elapsed < returnDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t =
                Smooth(
                    elapsed / returnDuration);

            if (cameraLooksAtHands)
            {
                cameraOffsetRoot.localRotation =
                    Quaternion.Slerp(
                        returnStart,
                        cameraOffsetStartRotation,
                        t);
            }

            yield return null;
        }

        cameraOffsetRoot.localRotation =
            cameraOffsetStartRotation;

        // 5) 手稍微沉回自然位置。
        Vector3 settleTarget =
            shownHandsWorldPosition
            - cam.up * 0.10f;

        Vector3 settleStart =
            hands.position;

        elapsed = 0f;
        float settleDuration =
            Mathf.Max(0.05f, settleSeconds);

        while (elapsed < settleDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t =
                Smooth(
                    elapsed / settleDuration);

            hands.position =
                Vector3.Lerp(
                    settleStart,
                    settleTarget,
                    t);

            yield return null;
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
