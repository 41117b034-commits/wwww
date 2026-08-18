using System.Collections;
using UnityEngine;

public class Chapter1EyeOpening : MonoBehaviour
{
    [Header("主要參考")]
    public Chapter1PerformanceController chapterController;
    public EyeOpenEffect eyeOpenEffect;

    [Tooltip("可留 None；自動找 Main Camera。")]
    public Transform viewTransform;

    [Header("開始時機")]
    [Tooltip("如果沒有 Intro Voice 1，就使用這個等待時間。")]
    public float fallbackDelay = 4.35f;

    [Tooltip("第一段配音結束後額外等待。")]
    public float extraDelay = 0.08f;

    [Header("自然睜眼")]
    public bool useNaturalWakeUp = true;

    [Tooltip("眼睛完全睜開後稍微停一下，再抬手。")]
    public float pauseAfterEyesOpen = 0.20f;

    [Header("玩家看手動畫")]
    public bool playHandCheckAnimation = true;

    [Tooltip("專門給開場用的手模型 Root。建議放在 Main Camera 底下。")]
    public GameObject introHandsRoot;

    public Transform leftHand;
    public Transform rightHand;

    [Tooltip("如果你另外有正式 VR 手部，這裡可拖 Gameplay Hands Root；開場結束後會打開。")]
    public GameObject gameplayHandsRoot;

    [Tooltip("開場手部動畫完成後，關閉 Intro Hands。若 Intro Hands 就是正式玩家手，取消勾選。")]
    public bool hideIntroHandsAfterSequence = true;

    [Header("手部動作節奏")]
    public float handRaiseDuration = 0.72f;
    public float handRaiseStagger = 0.10f;
    public float handInspectDuration = 1.45f;
    public float handSettleDuration = 0.45f;

    [Header("手從畫面下方出現")]
    public Vector3 leftHiddenLocalOffset =
        new Vector3(-0.06f, -0.42f, -0.16f);

    public Vector3 rightHiddenLocalOffset =
        new Vector3(0.06f, -0.42f, -0.16f);

    [Header("看手時手腕轉動")]
    public Vector3 leftInspectEulerOffset =
        new Vector3(-8f, -24f, 10f);

    public Vector3 rightInspectEulerOffset =
        new Vector3(-8f, 24f, -10f);

    [Tooltip("手在檢查時稍微靠近視線中心。")]
    public Vector3 leftInspectPositionOffset =
        new Vector3(0.045f, 0.035f, 0.035f);

    public Vector3 rightInspectPositionOffset =
        new Vector3(-0.045f, 0.035f, 0.035f);

    [Header("控制")]
    [Tooltip("開場動畫播放期間持續鎖定玩家移動。")]
    public bool keepPlayerLockedDuringIntro = true;

    private Vector3 leftShownPosition;
    private Vector3 rightShownPosition;
    private Quaternion leftShownRotation;
    private Quaternion rightShownRotation;

    private bool introRunning;
    private bool handPoseSaved;

    private IEnumerator Start()
    {
        ResolveReferences();
        PrepareHands();

        float delay = fallbackDelay;

        if (chapterController != null && chapterController.introVoice1 != null)
        {
            delay =
                chapterController.introVoice1.length
                + chapterController.introLineGap;
        }

        delay += Mathf.Max(0f, extraDelay);

        yield return WaitRealtime(Mathf.Max(0f, delay));

        introRunning = true;

        if (keepPlayerLockedDuringIntro && chapterController != null)
        {
            chapterController.SetPlayerControl(false);
        }

        // 1. 先做自然的睜眼、閉眼、再睜眼。
        if (eyeOpenEffect != null)
        {
            if (useNaturalWakeUp)
            {
                yield return eyeOpenEffect.NaturalWakeUpRoutine();
            }
            else
            {
                eyeOpenEffect.OpenEyes();
                yield return WaitRealtime(Mathf.Max(0.1f, eyeOpenEffect.openDuration));
            }
        }

        yield return WaitRealtime(pauseAfterEyesOpen);

        // 2. 雙手慢慢抬到眼前，像剛醒來確認自己的身體。
        if (playHandCheckAnimation)
        {
            yield return PlayHandsInspectionRoutine();
        }

        introRunning = false;

        // 若主劇情已經解鎖自由探索，就把控制權交還玩家。
        if (chapterController != null
            && chapterController.IsFreeExplorationActive())
        {
            chapterController.SetPlayerControl(true);
        }
    }

    private void Update()
    {
        // 主控制器可能在 Intro 還沒結束前先解鎖，
        // 這裡會暫時把控制權鎖住，直到看手動畫完成。
        if (introRunning
            && keepPlayerLockedDuringIntro
            && chapterController != null)
        {
            chapterController.SetPlayerControl(false);
        }
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
            eyeOpenEffect = GetComponent<EyeOpenEffect>();

            if (eyeOpenEffect == null)
            {
                eyeOpenEffect =
                    FindFirstObjectByType<EyeOpenEffect>(
                        FindObjectsInactive.Include);
            }
        }

        if (viewTransform == null && Camera.main != null)
        {
            viewTransform = Camera.main.transform;
        }
    }

    private void PrepareHands()
    {
        if (!playHandCheckAnimation)
        {
            return;
        }

        if (introHandsRoot != null)
        {
            introHandsRoot.SetActive(true);
        }

        if (gameplayHandsRoot != null)
        {
            gameplayHandsRoot.SetActive(false);
        }

        if (leftHand == null || rightHand == null)
        {
            Debug.LogWarning(
                "[Chapter1EyeOpening] 尚未指定 Left Hand / Right Hand。"
                + "睜眼會正常播放，但不會播放看手動畫。",
                this);
            return;
        }

        leftShownPosition = leftHand.localPosition;
        rightShownPosition = rightHand.localPosition;
        leftShownRotation = leftHand.localRotation;
        rightShownRotation = rightHand.localRotation;

        handPoseSaved = true;

        // 一開始雙手藏到畫面下方。
        leftHand.localPosition =
            leftShownPosition + leftHiddenLocalOffset;

        rightHand.localPosition =
            rightShownPosition + rightHiddenLocalOffset;

        leftHand.localRotation =
            leftShownRotation * Quaternion.Euler(18f, 5f, -10f);

        rightHand.localRotation =
            rightShownRotation * Quaternion.Euler(18f, -5f, 10f);
    }

    private IEnumerator PlayHandsInspectionRoutine()
    {
        if (!handPoseSaved || leftHand == null || rightHand == null)
        {
            yield break;
        }

        Vector3 leftStartPosition = leftHand.localPosition;
        Vector3 rightStartPosition = rightHand.localPosition;
        Quaternion leftStartRotation = leftHand.localRotation;
        Quaternion rightStartRotation = rightHand.localRotation;

        float raiseDuration = Mathf.Max(0.05f, handRaiseDuration);
        float elapsed = 0f;

        // 左手先一點、右手慢半拍，避免兩隻手像機器同步升起。
        while (elapsed < raiseDuration + handRaiseStagger)
        {
            elapsed += Time.unscaledDeltaTime;

            float leftT =
                Smooth01(elapsed / raiseDuration);

            float rightT =
                Smooth01(
                    (elapsed - handRaiseStagger)
                    / raiseDuration);

            leftHand.localPosition =
                Vector3.Lerp(
                    leftStartPosition,
                    leftShownPosition,
                    leftT);

            rightHand.localPosition =
                Vector3.Lerp(
                    rightStartPosition,
                    rightShownPosition,
                    rightT);

            leftHand.localRotation =
                Quaternion.Slerp(
                    leftStartRotation,
                    leftShownRotation,
                    leftT);

            rightHand.localRotation =
                Quaternion.Slerp(
                    rightStartRotation,
                    rightShownRotation,
                    rightT);

            yield return null;
        }

        leftHand.SetLocalPositionAndRotation(
            leftShownPosition,
            leftShownRotation);

        rightHand.SetLocalPositionAndRotation(
            rightShownPosition,
            rightShownRotation);

        // 慢慢翻一下手腕，像在確認「這是我的手」。
        float inspectDuration = Mathf.Max(0.1f, handInspectDuration);
        elapsed = 0f;

        while (elapsed < inspectDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized =
                Mathf.Clamp01(elapsed / inspectDuration);

            // 0 -> 1 -> 0，最後會自然回到原本姿勢。
            float inspectAmount =
                Mathf.Sin(normalized * Mathf.PI);

            float microFloat =
                Mathf.Sin(normalized * Mathf.PI * 2f)
                * 0.008f;

            leftHand.localPosition =
                leftShownPosition
                + leftInspectPositionOffset * inspectAmount
                + Vector3.up * microFloat;

            rightHand.localPosition =
                rightShownPosition
                + rightInspectPositionOffset * inspectAmount
                + Vector3.up * microFloat;

            leftHand.localRotation =
                Quaternion.Slerp(
                    leftShownRotation,
                    leftShownRotation
                        * Quaternion.Euler(leftInspectEulerOffset),
                    inspectAmount);

            rightHand.localRotation =
                Quaternion.Slerp(
                    rightShownRotation,
                    rightShownRotation
                        * Quaternion.Euler(rightInspectEulerOffset),
                    inspectAmount);

            yield return null;
        }

        // 最後回到正常手的位置，不要突然跳回去。
        yield return SettleHandsToNormal();

        if (hideIntroHandsAfterSequence)
        {
            if (introHandsRoot != null)
            {
                introHandsRoot.SetActive(false);
            }

            if (gameplayHandsRoot != null)
            {
                gameplayHandsRoot.SetActive(true);
            }
        }
    }

    private IEnumerator SettleHandsToNormal()
    {
        Vector3 leftStartPosition = leftHand.localPosition;
        Vector3 rightStartPosition = rightHand.localPosition;
        Quaternion leftStartRotation = leftHand.localRotation;
        Quaternion rightStartRotation = rightHand.localRotation;

        float duration = Mathf.Max(0.05f, handSettleDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Smooth01(elapsed / duration);

            leftHand.localPosition =
                Vector3.Lerp(
                    leftStartPosition,
                    leftShownPosition,
                    t);

            rightHand.localPosition =
                Vector3.Lerp(
                    rightStartPosition,
                    rightShownPosition,
                    t);

            leftHand.localRotation =
                Quaternion.Slerp(
                    leftStartRotation,
                    leftShownRotation,
                    t);

            rightHand.localRotation =
                Quaternion.Slerp(
                    rightStartRotation,
                    rightShownRotation,
                    t);

            yield return null;
        }

        leftHand.SetLocalPositionAndRotation(
            leftShownPosition,
            leftShownRotation);

        rightHand.SetLocalPositionAndRotation(
            rightShownPosition,
            rightShownRotation);
    }

    private float Smooth01(float value)
    {
        float t = Mathf.Clamp01(value);

        // smootherstep
        return t * t * t * (t * (6f * t - 15f) + 10f);
    }

    private IEnumerator WaitRealtime(float seconds)
    {
        float remaining = Mathf.Max(0f, seconds);

        while (remaining > 0f)
        {
            remaining -= Time.unscaledDeltaTime;
            yield return null;
        }
    }
}
