using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 獨立的警察劇情 VR 字幕系統（前三句共通＋選擇分支版）。
/// 不依賴 Chapter1DialogueUI，也不需要修改原本字幕腳本。
/// 掛到 Chapter1Controller 或任意常駐物件即可。
/// </summary>
public class Chapter1PoliceSubtitleOverlay : MonoBehaviour
{
    [System.Serializable]
    public class SubtitleEntry
    {
        public float delayFromPrevious = 0f;
        public string speaker = "";
        [TextArea(2, 4)] public string text = "";
        public float duration = 3f;
    }

    [Header("References")]
    [Tooltip("主控制器。留空會自動尋找。")]
    public Chapter1PerformanceController chapterController;

    [Tooltip("主攝影機。留空會自動使用 Camera.main。")]
    public Camera vrCamera;

    [Header("Auto Start")]
    [Tooltip("偵測到警察劇情開始後，自動播放字幕序列。")]
    public bool autoStartWithPoliceSequence = true;

    [Tooltip("每個章節只自動播放一次。")]
    public bool playOnlyOnce = true;

    [Header("VR Subtitle Position")]
    [Tooltip("字幕離玩家眼睛的距離。")]
    public float distanceFromCamera = 1.55f;

    [Tooltip("字幕在視線下方的位置。負值越大越低。")]
    public float verticalOffset = -0.43f;

    public float canvasScale = 0.00135f;

    [Header("Subtitle Style")]
    public float fontSize = 38f;
    public Color textColor = Color.white;
    public Color backgroundColor = new Color(0f, 0f, 0f, 0.72f);
    public TMP_FontAsset chineseFont;

    [Header("Police Subtitle Timeline - Common Lines")]
    [Tooltip("這裡只放所有分支都一定會發生的前三句。")]
    public List<SubtitleEntry> subtitles = new List<SubtitleEntry>()
    {
        new SubtitleEntry
        {
            delayFromPrevious = 0f,
            speaker = "日警",
            text = "這種野蠻婚禮，竟然還敢辦得這麼熱鬧？",
            duration = 4f
        },
        new SubtitleEntry
        {
            delayFromPrevious = 0.15f,
            speaker = "族人",
            text = "我們只是辦婚禮，沒有冒犯。",
            duration = 3.2f
        },
        new SubtitleEntry
        {
            delayFromPrevious = 0.15f,
            speaker = "女性族人",
            text = "放開我！",
            duration = 2.5f
        }
    };

    [Header("Choice Branch Subtitles")]
    [Tooltip("玩家選擇上前阻止時才顯示。")]
    [TextArea(2, 4)]
    public string interveneSubtitle = "夠了！不要再羞辱我們！";

    [Tooltip("玩家選擇沉默觀望時才顯示。")]
    [TextArea(2, 4)]
    public string watchSubtitle = "都給我安靜。你們最好記住自己的身分。";

    public float interveneSubtitleSeconds = 2.8f;
    public float watchSubtitleSeconds = 3.2f;


    private Canvas subtitleCanvas;
    private CanvasGroup canvasGroup;
    private TextMeshProUGUI subtitleText;
    private Coroutine sequenceRoutine;
    private bool hasPlayed;
    private bool previousPoliceState;
    private bool interveneBranchSubtitlePlayed;
    private bool watchBranchSubtitlePlayed;

    private void Awake()
    {
        if (chapterController == null)
        {
            chapterController = FindFirstObjectByType<Chapter1PerformanceController>();
        }

        if (vrCamera == null)
        {
            vrCamera = Camera.main;
        }

        EnsureUI();
        HideImmediate();
    }

    private void OnEnable()
    {
        Application.logMessageReceived += HandleChapterLog;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleChapterLog;
    }

    private void HandleChapterLog(
        string condition,
        string stackTrace,
        LogType type)
    {
        if (string.IsNullOrEmpty(condition))
        {
            return;
        }

        // 主劇情在 ResolveChoice 裡會先 SetMission，
        // SetMission 會印 [Chapter1 Mission]，用這個判斷玩家選到哪個分支。
        if (!interveneBranchSubtitlePlayed
            && condition.Contains("分支：你選擇上前阻止"))
        {
            interveneBranchSubtitlePlayed = true;
            ShowBranchSubtitle(
                "玩家",
                interveneSubtitle,
                interveneSubtitleSeconds);
            return;
        }

        if (!watchBranchSubtitlePlayed
            && condition.Contains("分支：你選擇沉默觀望"))
        {
            watchBranchSubtitlePlayed = true;
            ShowBranchSubtitle(
                "日警",
                watchSubtitle,
                watchSubtitleSeconds);
        }
    }

    private void ShowBranchSubtitle(
        string speaker,
        string line,
        float seconds)
    {
        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
            sequenceRoutine = null;
        }

        sequenceRoutine =
            StartCoroutine(
                ShowBranchSubtitleRoutine(
                    speaker,
                    line,
                    seconds));
    }

    private IEnumerator ShowBranchSubtitleRoutine(
        string speaker,
        string line,
        float seconds)
    {
        yield return ShowSingleSubtitle(
            speaker,
            line,
            seconds);

        sequenceRoutine = null;
    }

    private void Update()
    {
        if (!autoStartWithPoliceSequence || chapterController == null)
        {
            return;
        }

        bool policeStarted = chapterController.IsPoliceSequenceStarted;

        if (policeStarted && !previousPoliceState)
        {
            interveneBranchSubtitlePlayed = false;
            watchBranchSubtitlePlayed = false;

            if (!playOnlyOnce || !hasPlayed)
            {
                PlayPoliceSubtitles();
            }
        }

        previousPoliceState = policeStarted;
    }

    [ContextMenu("Test Police Subtitles")]
    public void PlayPoliceSubtitles()
    {
        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
        }

        sequenceRoutine = StartCoroutine(PlaySequence());
    }

    public void ShowSubtitle(string speaker, string text, float seconds)
    {
        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
        }

        sequenceRoutine = StartCoroutine(
            ShowSingleSubtitle(speaker, text, seconds));
    }

    public void HideSubtitle()
    {
        HideImmediate();
    }

    private IEnumerator PlaySequence()
    {
        hasPlayed = true;

        for (int i = 0; i < subtitles.Count; i++)
        {
            SubtitleEntry entry = subtitles[i];

            if (entry == null)
            {
                continue;
            }

            if (entry.delayFromPrevious > 0f)
            {
                yield return new WaitForSeconds(entry.delayFromPrevious);
            }

            yield return ShowSingleSubtitle(
                entry.speaker,
                entry.text,
                entry.duration);
        }

        sequenceRoutine = null;
    }

    private IEnumerator ShowSingleSubtitle(
        string speaker,
        string text,
        float seconds)
    {
        EnsureUI();

        if (subtitleText == null || canvasGroup == null)
        {
            yield break;
        }

        string speakerPart = string.IsNullOrWhiteSpace(speaker)
            ? ""
            : "<b>" + speaker + "</b>：";

        subtitleText.text = speakerPart + text;

        canvasGroup.alpha = 1f;

        yield return new WaitForSeconds(
            Mathf.Max(0.1f, seconds));

        canvasGroup.alpha = 0f;
    }

    private void HideImmediate()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    private void EnsureUI()
    {
        if (vrCamera == null)
        {
            vrCamera = Camera.main;
        }

        if (vrCamera == null)
        {
            Debug.LogWarning(
                "[Chapter1PoliceSubtitleOverlay] 找不到 Main Camera。");
            return;
        }

        Transform existing =
            vrCamera.transform.Find("PoliceSubtitleCanvas_Auto");

        GameObject canvasObject;

        if (existing != null)
        {
            canvasObject = existing.gameObject;
        }
        else
        {
            canvasObject = new GameObject(
                "PoliceSubtitleCanvas_Auto",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(CanvasGroup));

            canvasObject.transform.SetParent(
                vrCamera.transform,
                false);
        }

        RectTransform canvasRect =
            canvasObject.GetComponent<RectTransform>();

        canvasRect.localPosition =
            new Vector3(
                0f,
                verticalOffset,
                distanceFromCamera);

        canvasRect.localRotation = Quaternion.identity;
        canvasRect.localScale =
            Vector3.one * canvasScale;

        canvasRect.sizeDelta =
            new Vector2(1050f, 190f);

        subtitleCanvas =
            canvasObject.GetComponent<Canvas>();

        subtitleCanvas.renderMode =
            RenderMode.WorldSpace;

        subtitleCanvas.worldCamera = vrCamera;
        subtitleCanvas.sortingOrder = 1000;

        canvasGroup =
            canvasObject.GetComponent<CanvasGroup>();

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        Transform background =
            canvasObject.transform.Find("Background");

        GameObject backgroundObject;

        if (background == null)
        {
            backgroundObject = new GameObject(
                "Background",
                typeof(RectTransform),
                typeof(Image));

            backgroundObject.transform.SetParent(
                canvasObject.transform,
                false);
        }
        else
        {
            backgroundObject = background.gameObject;
        }

        RectTransform backgroundRect =
            backgroundObject.GetComponent<RectTransform>();

        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        backgroundObject.GetComponent<Image>().color =
            backgroundColor;

        Transform textTransform =
            backgroundObject.transform.Find("SubtitleText");

        GameObject textObject;

        if (textTransform == null)
        {
            textObject = new GameObject(
                "SubtitleText",
                typeof(RectTransform),
                typeof(TextMeshProUGUI));

            textObject.transform.SetParent(
                backgroundObject.transform,
                false);
        }
        else
        {
            textObject = textTransform.gameObject;
        }

        RectTransform textRect =
            textObject.GetComponent<RectTransform>();

        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;

        textRect.offsetMin =
            new Vector2(32f, 18f);

        textRect.offsetMax =
            new Vector2(-32f, -18f);

        subtitleText =
            textObject.GetComponent<TextMeshProUGUI>();

        subtitleText.alignment =
            TextAlignmentOptions.Center;

        subtitleText.fontSize = fontSize;
        subtitleText.color = textColor;
        subtitleText.enableWordWrapping = true;
        subtitleText.richText = true;

        TMP_FontAsset resolvedFont =
            ResolveChineseFont();

        if (resolvedFont != null)
        {
            subtitleText.font = resolvedFont;
        }
    }

    private TMP_FontAsset ResolveChineseFont()
    {
        if (chineseFont != null)
        {
            return chineseFont;
        }

        TextMeshProUGUI[] sceneTexts =
            FindObjectsByType<TextMeshProUGUI>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        for (int i = 0; i < sceneTexts.Length; i++)
        {
            TextMeshProUGUI item = sceneTexts[i];

            if (item != null
                && item != subtitleText
                && item.font != null)
            {
                chineseFont = item.font;
                return chineseFont;
            }
        }

        return TMP_Settings.defaultFontAsset;
    }
}
