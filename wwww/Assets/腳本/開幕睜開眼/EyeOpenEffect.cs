using System.Collections;
using UnityEngine;

public class EyeOpenEffect : MonoBehaviour
{
    [Header("眼皮物件")]
    public RectTransform topEyelid;
    public RectTransform bottomEyelid;

    [Header("睜眼設定")]
    [Tooltip("眼睛完全打開所需時間。")]
    public float openDuration = 2.5f;

    [Tooltip("眼皮移出畫面時額外多移動的距離。")]
    public float extraOpenDistance = 60f;

    [Tooltip("睜眼完成後自動關閉整個 EyeOpenCanvas。")]
    public bool hideCanvasAfterOpen = true;

    private Vector2 topClosedPosition;
    private Vector2 bottomClosedPosition;
    private Coroutine openRoutine;
    private bool positionsSaved;

    private void Awake()
    {
        SaveClosedPositions();
        SetClosedPositions();
    }

    /// <summary>
    /// 把畫面重設成閉眼狀態。第一章開始時由控制器自動呼叫。
    /// </summary>
    public void PrepareClosed()
    {
        if (!positionsSaved)
        {
            SaveClosedPositions();
        }

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        if (openRoutine != null)
        {
            StopCoroutine(openRoutine);
            openRoutine = null;
        }

        SetClosedPositions();
    }

    /// <summary>
    /// 立即開始慢慢睜眼。
    /// </summary>
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

        if (openRoutine != null)
        {
            StopCoroutine(openRoutine);
        }

        openRoutine = StartCoroutine(OpenEyesRoutine(0f));
    }

    /// <summary>
    /// 延遲指定秒數後開始睜眼。
    /// </summary>
    public void OpenEyesAfterDelay(float delaySeconds)
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        if (!positionsSaved)
        {
            SaveClosedPositions();
        }

        if (openRoutine != null)
        {
            StopCoroutine(openRoutine);
        }

        openRoutine = StartCoroutine(OpenEyesRoutine(Mathf.Max(0f, delaySeconds)));
    }

    private IEnumerator OpenEyesRoutine(float delaySeconds)
    {
        if (topEyelid == null || bottomEyelid == null)
        {
            Debug.LogWarning("[EyeOpenEffect] TopEyelid 或 BottomEyelid 尚未指定。", this);
            openRoutine = null;
            yield break;
        }

        if (delaySeconds > 0f)
        {
            yield return new WaitForSecondsRealtime(delaySeconds);
        }

        float topDistance = Mathf.Max(1f, topEyelid.rect.height) + Mathf.Max(0f, extraOpenDistance);
        float bottomDistance = Mathf.Max(1f, bottomEyelid.rect.height) + Mathf.Max(0f, extraOpenDistance);

        Vector2 topOpenPosition = topClosedPosition + Vector2.up * topDistance;
        Vector2 bottomOpenPosition = bottomClosedPosition + Vector2.down * bottomDistance;

        float duration = Mathf.Max(0.05f, openDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // SmoothStep：剛開始與快結束時比較柔和，比線性移動更像睜眼。
            t = t * t * (3f - 2f * t);

            topEyelid.anchoredPosition = Vector2.Lerp(topClosedPosition, topOpenPosition, t);
            bottomEyelid.anchoredPosition = Vector2.Lerp(bottomClosedPosition, bottomOpenPosition, t);

            yield return null;
        }

        topEyelid.anchoredPosition = topOpenPosition;
        bottomEyelid.anchoredPosition = bottomOpenPosition;

        openRoutine = null;

        if (hideCanvasAfterOpen)
        {
            gameObject.SetActive(false);
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

    private void SetClosedPositions()
    {
        if (topEyelid != null)
        {
            topEyelid.anchoredPosition = topClosedPosition;
        }

        if (bottomEyelid != null)
        {
            bottomEyelid.anchoredPosition = bottomClosedPosition;
        }
    }

#if UNITY_EDITOR
    [ContextMenu("測試：重設為閉眼")]
    private void EditorPrepareClosed()
    {
        PrepareClosed();
    }

    [ContextMenu("測試：睜眼（Play Mode）")]
    private void EditorOpenEyes()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("請先進入 Play Mode 再測試睜眼效果。", this);
            return;
        }

        OpenEyes();
    }
#endif
}
