using System.Collections;
using UnityEngine;

public class EyeOpenEffect : MonoBehaviour
{
    [Header("眼皮物件")]
    public RectTransform topEyelid;
    public RectTransform bottomEyelid;

    [Header("一般睜眼")]
    [Tooltip("一般 OpenEyes() 完全睜開所需時間。")]
    public float openDuration = 1.4f;

    [Tooltip("眼皮移出畫面時額外多移動的距離。")]
    public float extraOpenDistance = 60f;

    [Tooltip("睜眼完成後自動關閉整個 EyeOpenCanvas。")]
    public bool hideCanvasAfterOpen = true;

    [Header("自然甦醒 / 眨眼")]
    [Range(0f, 1f)] public float firstPeekOpenness = 0.24f;
    public float firstPeekDuration = 0.52f;
    public float firstPeekHold = 0.12f;

    public float firstCloseDuration = 0.20f;
    public float closedHold = 0.14f;

    [Range(0f, 1f)] public float secondOpenOpenness = 0.70f;
    public float secondOpenDuration = 0.68f;
    public float secondOpenHold = 0.24f;

    [Range(0f, 1f)] public float blinkClosedOpenness = 0.08f;
    public float blinkCloseDuration = 0.14f;
    public float blinkOpenDuration = 0.22f;
    public float blinkHold = 0.10f;

    public float finalOpenDuration = 0.62f;

    private Vector2 topClosedPosition;
    private Vector2 bottomClosedPosition;
    private Vector2 topOpenPosition;
    private Vector2 bottomOpenPosition;

    private Coroutine openRoutine;
    private bool positionsSaved;
    private float currentOpenness;

    private void Awake()
    {
        SaveClosedPositions();
        CalculateOpenPositions();
        SetEyeOpennessImmediate(0f);
    }

    public void PrepareClosed()
    {
        if (!positionsSaved)
        {
            SaveClosedPositions();
        }

        CalculateOpenPositions();

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        if (openRoutine != null)
        {
            StopCoroutine(openRoutine);
            openRoutine = null;
        }

        SetEyeOpennessImmediate(0f);
    }

    public void OpenEyes()
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        if (!positionsSaved)
        {
            SaveClosedPositions();
        }

        CalculateOpenPositions();

        if (openRoutine != null)
        {
            StopCoroutine(openRoutine);
        }

        openRoutine = StartCoroutine(OpenEyesRoutine());
    }

    public void OpenEyesAfterDelay(float delaySeconds)
    {
        if (openRoutine != null)
        {
            StopCoroutine(openRoutine);
        }

        openRoutine = StartCoroutine(OpenEyesAfterDelayRoutine(delaySeconds));
    }

    public Coroutine PlayNaturalWakeUp()
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        if (!positionsSaved)
        {
            SaveClosedPositions();
        }

        CalculateOpenPositions();

        if (openRoutine != null)
        {
            StopCoroutine(openRoutine);
        }

        openRoutine = StartCoroutine(NaturalWakeUpRoutine());
        return openRoutine;
    }

    public IEnumerator NaturalWakeUpRoutine()
    {
        PrepareClosed();

        // 第一次只睜一小條縫：剛醒來眼睛還不適應。
        yield return AnimateEyeOpenness(firstPeekOpenness, firstPeekDuration);
        yield return WaitRealtime(firstPeekHold);

        // 又閉回去一下。
        yield return AnimateEyeOpenness(0f, firstCloseDuration);
        yield return WaitRealtime(closedHold);

        // 第二次比較完整地睜開。
        yield return AnimateEyeOpenness(secondOpenOpenness, secondOpenDuration);
        yield return WaitRealtime(secondOpenHold);

        // 很短的一次自然眨眼。
        yield return AnimateEyeOpenness(blinkClosedOpenness, blinkCloseDuration);
        yield return AnimateEyeOpenness(secondOpenOpenness, blinkOpenDuration);
        yield return WaitRealtime(blinkHold);

        // 最後完全睜開。
        yield return AnimateEyeOpenness(1f, finalOpenDuration);

        currentOpenness = 1f;
        openRoutine = null;

        if (hideCanvasAfterOpen)
        {
            gameObject.SetActive(false);
        }
    }

    public void SetEyeOpennessImmediate(float openness)
    {
        if (!positionsSaved)
        {
            SaveClosedPositions();
        }

        CalculateOpenPositions();

        currentOpenness = Mathf.Clamp01(openness);

        if (topEyelid != null)
        {
            topEyelid.anchoredPosition =
                Vector2.Lerp(topClosedPosition, topOpenPosition, currentOpenness);
        }

        if (bottomEyelid != null)
        {
            bottomEyelid.anchoredPosition =
                Vector2.Lerp(bottomClosedPosition, bottomOpenPosition, currentOpenness);
        }
    }

    private IEnumerator OpenEyesRoutine()
    {
        yield return AnimateEyeOpenness(1f, openDuration);

        openRoutine = null;

        if (hideCanvasAfterOpen)
        {
            gameObject.SetActive(false);
        }
    }

    private IEnumerator OpenEyesAfterDelayRoutine(float delaySeconds)
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        yield return WaitRealtime(Mathf.Max(0f, delaySeconds));
        yield return AnimateEyeOpenness(1f, openDuration);

        openRoutine = null;

        if (hideCanvasAfterOpen)
        {
            gameObject.SetActive(false);
        }
    }

    private IEnumerator AnimateEyeOpenness(float targetOpenness, float duration)
    {
        if (topEyelid == null || bottomEyelid == null)
        {
            Debug.LogWarning("[EyeOpenEffect] TopEyelid 或 BottomEyelid 尚未指定。", this);
            yield break;
        }

        float start = currentOpenness;
        float target = Mathf.Clamp01(targetOpenness);
        float safeDuration = Mathf.Max(0.02f, duration);

        float elapsed = 0f;

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);

            // smootherstep：比線性與一般 SmoothStep 更柔和，眼皮不會突然停。
            t = t * t * t * (t * (6f * t - 15f) + 10f);

            SetEyeOpennessImmediate(Mathf.Lerp(start, target, t));
            yield return null;
        }

        SetEyeOpennessImmediate(target);
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

    private void SaveClosedPositions()
    {
        if (topEyelid == null || bottomEyelid == null)
        {
            return;
        }

        topClosedPosition = topEyelid.anchoredPosition;
        bottomClosedPosition = bottomEyelid.anchoredPosition;
        positionsSaved = true;
    }

    private void CalculateOpenPositions()
    {
        if (!positionsSaved || topEyelid == null || bottomEyelid == null)
        {
            return;
        }

        float topDistance =
            Mathf.Max(1f, topEyelid.rect.height)
            + Mathf.Max(0f, extraOpenDistance);

        float bottomDistance =
            Mathf.Max(1f, bottomEyelid.rect.height)
            + Mathf.Max(0f, extraOpenDistance);

        topOpenPosition =
            topClosedPosition + Vector2.up * topDistance;

        bottomOpenPosition =
            bottomClosedPosition + Vector2.down * bottomDistance;
    }

#if UNITY_EDITOR
    [ContextMenu("測試：重設閉眼")]
    private void EditorPrepareClosed()
    {
        PrepareClosed();
    }

    [ContextMenu("測試：自然甦醒（Play Mode）")]
    private void EditorNaturalWakeUp()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("請先進入 Play Mode。", this);
            return;
        }

        PlayNaturalWakeUp();
    }
#endif
}
