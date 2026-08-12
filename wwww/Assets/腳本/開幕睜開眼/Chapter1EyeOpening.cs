using System.Collections;
using UnityEngine;

public class Chapter1EyeOpening : MonoBehaviour
{
    [Header("參考")]
    public Chapter1PerformanceController chapterController;
    public EyeOpenEffect eyeOpenEffect;

    [Header("睜眼時機")]
    [Tooltip("如果第一段配音沒有指定，就使用這個秒數等待。")]
    public float fallbackDelay = 4.35f;

    [Tooltip("第一段配音結束後，再多等幾秒才睜眼。")]
    public float extraDelay = 0f;

    private IEnumerator Start()
    {
        // 讓 EyeOpenEffect 在 Awake 時先把眼皮固定在閉眼位置。
        if (eyeOpenEffect == null)
        {
            eyeOpenEffect = GetComponent<EyeOpenEffect>();
        }

        if (chapterController == null)
        {
            chapterController = FindFirstObjectByType<Chapter1PerformanceController>();
        }

        float delay = fallbackDelay;

        if (chapterController != null && chapterController.introVoice1 != null)
        {
            delay = chapterController.introVoice1.length + chapterController.introLineGap;
        }

        delay += Mathf.Max(0f, extraDelay);
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, delay));

        if (eyeOpenEffect != null)
        {
            eyeOpenEffect.OpenEyes();
        }
        else
        {
            Debug.LogWarning("[Chapter1EyeOpening] 找不到 EyeOpenEffect。", this);
        }
    }
}
