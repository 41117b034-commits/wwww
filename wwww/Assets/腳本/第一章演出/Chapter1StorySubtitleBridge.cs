using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Chapter1StorySubtitleBridge : MonoBehaviour
{
    public static Chapter1StorySubtitleBridge Instance { get; private set; }

    [Header("VR Camera")]
    public Camera vrCamera;

    [Header("Subtitle Filter")]
    [Tooltip("勾選後顯示開場兩句旁白＋指定的五句警察劇情字幕。")]
    public bool onlyFivePoliceLines = true;

    [Header("VR Subtitle Position")]
    public float distanceFromCamera = 1.55f;
    public float verticalOffset = -0.43f;
    public float canvasScale = 0.00135f;

    [Header("Style")]
    public float fontSize = 38f;
    public Color textColor = Color.white;
    public Color backgroundColor = new Color(0f, 0f, 0f, 0.72f);
    public TMP_FontAsset chineseFont;

    private CanvasGroup canvasGroup;
    private TextMeshProUGUI subtitleText;
    private Coroutine hideRoutine;

    private void Awake()
    {
        Instance = this;
        EnsureUI();
        HideImmediate();
    }

    private void OnEnable()
    {
        Instance = this;
    }

    private void OnDisable()
    {
        if (Instance == this)
            Instance = null;
    }

    public void ShowFromStory(string speaker, string line, float seconds)
    {
        if (onlyFivePoliceLines
            && !IsOpeningNarrationLine(line)
            && !IsWantedPoliceLine(line))
        {
            return;
        }

        EnsureUI();

        if (subtitleText == null || canvasGroup == null)
            return;

        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
            hideRoutine = null;
        }

        string displaySpeaker = NormalizeSpeaker(speaker, line);

        subtitleText.text =
            string.IsNullOrWhiteSpace(displaySpeaker)
                ? line
                : "<b>" + displaySpeaker + "</b>：" + line;

        canvasGroup.alpha = 1f;
        hideRoutine = StartCoroutine(
            HideAfter(Mathf.Max(0.5f, seconds)));
    }

    private bool IsOpeningNarrationLine(string line)
    {
        return line == "1930.10.7，霧社。火光照亮婚禮，鼓聲和歌聲在山間回盪。"
            || line == "你睜開眼，看見族人圍著火堆歌舞。今晚本該只是祝福新人的夜晚。";
    }

    private bool IsWantedPoliceLine(string line)
    {
        return line == "這種野蠻婚禮，竟然還敢辦得這麼熱鬧？"
            || line == "我們只是辦婚禮，沒有冒犯。"
            || line == "放開我！"
            || line == "夠了！不要再羞辱我們！"
            || line == "都給我安靜。你們最好記住自己的身分。";
    }

    private string NormalizeSpeaker(string speaker, string line)
    {
        if (IsOpeningNarrationLine(line))
            return "";

        if (line == "這種野蠻婚禮，竟然還敢辦得這麼熱鬧？"
            || line == "都給我安靜。你們最好記住自己的身分。")
            return "日警";

        if (line == "我們只是辦婚禮，沒有冒犯。")
            return "族人";

        if (line == "放開我！")
            return "女性族人";

        if (line == "夠了！不要再羞辱我們！")
            return "玩家";

        return speaker;
    }

    [ContextMenu("Test Opening Subtitle")]
    public void TestOpeningSubtitle()
    {
        ShowFromStory(
            "字幕",
            "1930.10.7，霧社。火光照亮婚禮，鼓聲和歌聲在山間回盪。",
            4f);
    }

    [ContextMenu("Test Subtitle")]
    public void TestSubtitle()
    {
        ShowFromStory(
            "日警",
            "這種野蠻婚禮，竟然還敢辦得這麼熱鬧？",
            4f);
    }

    private IEnumerator HideAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        HideImmediate();
        hideRoutine = null;
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
            vrCamera = Camera.main;

        if (vrCamera == null)
        {
            Camera[] cameras = FindObjectsByType<Camera>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i] != null &&
                    cameras[i].gameObject.name == "Main Camera")
                {
                    vrCamera = cameras[i];
                    break;
                }
            }

            if (vrCamera == null && cameras.Length > 0)
                vrCamera = cameras[0];
        }

        if (vrCamera == null)
            return;

        Transform existing =
            vrCamera.transform.Find("StorySubtitleCanvas_Auto");

        GameObject canvasObject;

        if (existing != null)
        {
            canvasObject = existing.gameObject;
        }
        else
        {
            canvasObject = new GameObject(
                "StorySubtitleCanvas_Auto",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(CanvasGroup));

            canvasObject.transform.SetParent(vrCamera.transform, false);
        }

        RectTransform canvasRect =
            canvasObject.GetComponent<RectTransform>();

        canvasRect.localPosition =
            new Vector3(0f, verticalOffset, distanceFromCamera);
        canvasRect.localRotation = Quaternion.identity;
        canvasRect.localScale =
            Vector3.one * Mathf.Max(0.0001f, canvasScale);
        canvasRect.sizeDelta = new Vector2(1100f, 190f);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = vrCamera;
        canvas.sortingOrder = 5000;

        canvasGroup = canvasObject.GetComponent<CanvasGroup>();
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
        textRect.offsetMin = new Vector2(36f, 18f);
        textRect.offsetMax = new Vector2(-36f, -18f);

        subtitleText =
            textObject.GetComponent<TextMeshProUGUI>();

        subtitleText.alignment = TextAlignmentOptions.Center;
        subtitleText.fontSize = fontSize;
        subtitleText.color = textColor;
        subtitleText.enableWordWrapping = true;
        subtitleText.richText = true;

        if (chineseFont != null)
            subtitleText.font = chineseFont;
    }
}
