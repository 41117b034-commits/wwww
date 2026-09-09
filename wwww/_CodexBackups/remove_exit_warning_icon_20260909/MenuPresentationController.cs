using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class MenuPresentationController : MonoBehaviour
{
    private static MenuPresentationController instance;

    private readonly HashSet<int> boundLanguageButtons = new HashSet<int>();
    private readonly HashSet<int> boundVolumeButtons = new HashSet<int>();
    private readonly HashSet<int> boundExitButtons = new HashSet<int>();
    private TextMeshProUGUI volumeLabel;
    private AudioSource volumePreviewSource;
    private AudioClip volumePreviewClip;
    private bool returningToSettings;
    private GameObject exitConfirmationOverlay;
    private bool quitPending;

    private static readonly string[] ChapterPrefixes =
    {
        "第一章",
        "第二章",
        "第三章",
        "第四章",
        "第五章"
    };

    private static readonly string[] ChapterTitles =
    {
        "第一章\n婚禮與導火線",
        "第二章\n新規定與秘密會議",
        "第三章\n武裝準備與突襲計畫",
        "第四章\n霧社突襲與戰鬥",
        "第五章\n日軍鎮壓與毒氣攻擊"
    };

    private static readonly Vector2[] ChapterPositions =
    {
        new Vector2(-44f, 6f),
        new Vector2(0f, 6f),
        new Vector2(44f, 6f),
        new Vector2(-22f, -25f),
        new Vector2(22f, -25f)
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateRuntimeController()
    {
        if (instance != null)
        {
            return;
        }

        GameObject controllerObject = new GameObject("Menu Presentation Controller");
        instance = controllerObject.AddComponent<MenuPresentationController>();
        DontDestroyOnLoad(controllerObject);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        GameSubtitleLanguage unused = GameLanguageSettings.CurrentLanguage;
        GameAudioSettings.Apply();
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void Start()
    {
        StartCoroutine(ApplyScenePresentationAfterLayout(SceneManager.GetActiveScene()));
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            instance = null;
        }
    }

    private void Update()
    {
        if (exitConfirmationOverlay != null
            && exitConfirmationOverlay.activeSelf
            && !quitPending
            && Input.GetKeyDown(KeyCode.Escape))
        {
            HideExitConfirmation();
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        boundLanguageButtons.Clear();
        boundVolumeButtons.Clear();
        boundExitButtons.Clear();
        volumeLabel = null;
        returningToSettings = false;
        exitConfirmationOverlay = null;
        quitPending = false;
        GameAudioSettings.Apply();
        StartCoroutine(ApplyScenePresentationAfterLayout(scene));
    }

    private IEnumerator ApplyScenePresentationAfterLayout(Scene scene)
    {
        yield return null;
        // Some older scene debug components set AudioListener.volume in Start().
        // Reapply the saved master volume after all scene Start methods have run.
        GameAudioSettings.Apply();
        Canvas.ForceUpdateCanvases();
        EnlargeBrownMenuPanels();

        if (scene.name == "點選介面")
        {
            CenterMainMenuCanvas();
            BindMainMenuExitButton();
        }
        else if (scene.name == "字幕選單")
        {
            BindLanguageButtons();
        }
        else if (scene.name == "設定內容")
        {
            BindVolumeControls();
        }
        else if (scene.name.Contains("選擇章節"))
        {
            ArrangeChapterSelection();
        }
    }

    private void CenterMainMenuCanvas()
    {
        TextMeshProUGUI title = FindTextStartingWith("霧社事件");
        if (title == null)
        {
            return;
        }

        ConfigureMainMenuTitle(title);
        Canvas canvas = title.GetComponentInParent<Canvas>();
        CenterCanvasVertically(canvas);
    }

    private static void EnlargeBrownMenuPanels()
    {
        Image[] images = FindObjectsByType<Image>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (image == null
                || image.gameObject.name != "Panel"
                || !IsBrownMenuPanel(image.color))
            {
                continue;
            }

            RectTransform panelRect = image.rectTransform;
            bool stretchesHorizontally = Mathf.Approximately(
                panelRect.anchorMax.x - panelRect.anchorMin.x,
                1f);
            bool stretchesVertically = Mathf.Approximately(
                panelRect.anchorMax.y - panelRect.anchorMin.y,
                1f);

            if (stretchesHorizontally && stretchesVertically)
            {
                panelRect.anchoredPosition = Vector2.zero;
                panelRect.offsetMin = new Vector2(-8f, -6f);
                panelRect.offsetMax = new Vector2(8f, 6f);
            }
            else
            {
                panelRect.sizeDelta = new Vector2(
                    panelRect.sizeDelta.x + 16f,
                    panelRect.sizeDelta.y + 12f);
            }
        }
    }

    private static bool IsBrownMenuPanel(Color color)
    {
        return color.r >= 0.60f
            && color.r <= 0.76f
            && color.g >= 0.45f
            && color.g <= 0.62f
            && color.b <= 0.18f;
    }

    private static void ConfigureMainMenuTitle(TextMeshProUGUI title)
    {
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0f, 35f);
        titleRect.sizeDelta = new Vector2(108f, 24f);
        titleRect.localScale = Vector3.one;

        title.alignment = TextAlignmentOptions.Center;
        title.enableAutoSizing = true;
        title.fontSizeMin = 15f;
        title.fontSizeMax = 24f;
        title.textWrappingMode = TextWrappingModes.NoWrap;
        title.margin = new Vector4(3f, 1f, 3f, 1f);
        title.raycastTarget = false;
    }

    private void BindMainMenuExitButton()
    {
        Button[] buttons = FindObjectsByType<Button>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            TextMeshProUGUI label = button != null
                ? button.GetComponentInChildren<TextMeshProUGUI>(true)
                : null;
            if (label == null || Compact(label.text) != "退出")
            {
                continue;
            }

            Canvas canvas = button.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                continue;
            }

            EnsureExitConfirmationDialog(
                canvas,
                label,
                button.targetGraphic as Image);

            if (boundExitButtons.Add(button.GetInstanceID()))
            {
                button.onClick.AddListener(ShowExitConfirmation);
            }
        }
    }

    private void EnsureExitConfirmationDialog(
        Canvas canvas,
        TextMeshProUGUI fontTemplate,
        Image buttonImageTemplate)
    {
        if (exitConfirmationOverlay != null || canvas == null || fontTemplate == null)
        {
            return;
        }

        exitConfirmationOverlay = new GameObject(
            "Exit Confirmation Overlay",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(CanvasGroup),
            typeof(Image));
        exitConfirmationOverlay.layer = canvas.gameObject.layer;

        RectTransform overlayRect = exitConfirmationOverlay.GetComponent<RectTransform>();
        overlayRect.SetParent(canvas.transform, false);
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.pivot = new Vector2(0.5f, 0.5f);
        overlayRect.anchoredPosition = Vector2.zero;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        overlayRect.localScale = Vector3.one;

        Image overlayImage = exitConfirmationOverlay.GetComponent<Image>();
        overlayImage.color = new Color(0.035f, 0.025f, 0.018f, 0.76f);
        overlayImage.raycastTarget = true;

        CanvasGroup overlayGroup = exitConfirmationOverlay.GetComponent<CanvasGroup>();
        overlayGroup.interactable = true;
        overlayGroup.blocksRaycasts = true;

        GameObject panelObject = new GameObject(
            "Exit Confirmation Panel",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Outline));
        panelObject.layer = canvas.gameObject.layer;
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.SetParent(overlayRect, false);
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(88f, 32f);
        panelRect.localScale = Vector3.one;

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0.25f, 0.17f, 0.105f, 0.98f);
        panelImage.raycastTarget = true;

        Outline panelOutline = panelObject.GetComponent<Outline>();
        panelOutline.effectColor = new Color(0.78f, 0.60f, 0.24f, 0.92f);
        panelOutline.effectDistance = new Vector2(0.55f, -0.55f);
        panelOutline.useGraphicAlpha = true;

        Image accent = CreateDialogImage(
            "Top Accent",
            panelRect,
            new Color(0.58f, 0.22f, 0.14f, 1f));
        RectTransform accentRect = accent.rectTransform;
        accentRect.anchorMin = new Vector2(0f, 1f);
        accentRect.anchorMax = new Vector2(1f, 1f);
        accentRect.pivot = new Vector2(0.5f, 1f);
        accentRect.anchoredPosition = Vector2.zero;
        accentRect.sizeDelta = new Vector2(-1.2f, 1.25f);

        CreateDialogText(
            "Warning Icon",
            panelRect,
            fontTemplate,
            "!",
            new Vector2(-37f, 5.5f),
            new Vector2(8f, 10f),
            7.5f,
            new Color(0.96f, 0.76f, 0.30f, 1f));

        CreateDialogText(
            "Question",
            panelRect,
            fontTemplate,
            "確定要退出遊戲嗎？",
            new Vector2(2.5f, 5.5f),
            new Vector2(70f, 9f),
            5.4f,
            new Color(0.98f, 0.93f, 0.79f, 1f));

        Image divider = CreateDialogImage(
            "Divider",
            panelRect,
            new Color(0.78f, 0.60f, 0.24f, 0.52f));
        RectTransform dividerRect = divider.rectTransform;
        dividerRect.anchorMin = new Vector2(0.5f, 0.5f);
        dividerRect.anchorMax = new Vector2(0.5f, 0.5f);
        dividerRect.pivot = new Vector2(0.5f, 0.5f);
        dividerRect.anchoredPosition = new Vector2(0f, 0.2f);
        dividerRect.sizeDelta = new Vector2(74f, 0.45f);

        CreateExitDialogButton(
            "Confirm Exit",
            panelRect,
            fontTemplate,
            buttonImageTemplate,
            "確定",
            new Vector2(-18f, -8.2f),
            new Color(0.48f, 0.18f, 0.13f, 1f),
            ConfirmExit);

        CreateExitDialogButton(
            "Cancel Exit",
            panelRect,
            fontTemplate,
            buttonImageTemplate,
            "取消",
            new Vector2(18f, -8.2f),
            new Color(0.32f, 0.25f, 0.16f, 1f),
            HideExitConfirmation);

        exitConfirmationOverlay.transform.SetAsLastSibling();
        exitConfirmationOverlay.SetActive(false);
    }

    private static Image CreateDialogImage(
        string objectName,
        Transform parent,
        Color color)
    {
        GameObject imageObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        imageObject.layer = parent.gameObject.layer;
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;

        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TextMeshProUGUI CreateDialogText(
        string objectName,
        Transform parent,
        TextMeshProUGUI fontTemplate,
        string value,
        Vector2 position,
        Vector2 size,
        float maximumFontSize,
        Color color)
    {
        GameObject textObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.layer = parent.gameObject.layer;
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = fontTemplate.font;
        text.text = value;
        text.color = color;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Max(2f, maximumFontSize * 0.72f);
        text.fontSizeMax = maximumFontSize;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        return text;
    }

    private static Button CreateExitDialogButton(
        string objectName,
        Transform parent,
        TextMeshProUGUI fontTemplate,
        Image imageTemplate,
        string label,
        Vector2 position,
        Color normalColor,
        UnityAction action)
    {
        GameObject buttonObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button),
            typeof(Outline));
        buttonObject.layer = parent.gameObject.layer;
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(30f, 7.8f);
        rect.localScale = Vector3.one;

        Image image = buttonObject.GetComponent<Image>();
        image.color = normalColor;
        image.raycastTarget = true;
        if (imageTemplate != null && imageTemplate.sprite != null)
        {
            image.sprite = imageTemplate.sprite;
            image.type = Image.Type.Sliced;
        }

        Outline outline = buttonObject.GetComponent<Outline>();
        outline.effectColor = new Color(0.82f, 0.66f, 0.35f, 0.85f);
        outline.effectDistance = new Vector2(0.35f, -0.35f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = Color.Lerp(normalColor, Color.white, 0.16f);
        colors.pressedColor = Color.Lerp(normalColor, Color.black, 0.28f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(normalColor.r, normalColor.g, normalColor.b, 0.45f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        CreateDialogText(
            "Label",
            rect,
            fontTemplate,
            label,
            Vector2.zero,
            new Vector2(27f, 6.4f),
            4.8f,
            new Color(1f, 0.95f, 0.82f, 1f));

        UIButtonSound.RegisterButton(button);
        button.onClick.AddListener(action);
        return button;
    }

    private void ShowExitConfirmation()
    {
        if (exitConfirmationOverlay == null || quitPending)
        {
            return;
        }

        exitConfirmationOverlay.SetActive(true);
        exitConfirmationOverlay.transform.SetAsLastSibling();
    }

    private void HideExitConfirmation()
    {
        if (exitConfirmationOverlay != null && !quitPending)
        {
            exitConfirmationOverlay.SetActive(false);
        }
    }

    private void ConfirmExit()
    {
        if (!quitPending)
        {
            quitPending = true;
            StartCoroutine(QuitAfterClickSound());
        }
    }

    private IEnumerator QuitAfterClickSound()
    {
        yield return new WaitForSecondsRealtime(0.16f);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void BindLanguageButtons()
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label == null || !TryGetLanguage(label.text, out GameSubtitleLanguage language))
            {
                continue;
            }

            int instanceId = button.GetInstanceID();
            if (!boundLanguageButtons.Add(instanceId))
            {
                continue;
            }

            GameSubtitleLanguage capturedLanguage = language;
            button.onClick.AddListener(() => SelectLanguage(capturedLanguage));
        }
    }

    private static bool TryGetLanguage(string label, out GameSubtitleLanguage language)
    {
        string compact = Compact(label);
        if (compact == "中文")
        {
            language = GameSubtitleLanguage.Chinese;
            return true;
        }

        if (compact == "日文")
        {
            language = GameSubtitleLanguage.Japanese;
            return true;
        }

        if (compact == "賽德克語")
        {
            language = GameSubtitleLanguage.Seediq;
            return true;
        }

        if (compact == "無字幕")
        {
            language = GameSubtitleLanguage.None;
            return true;
        }

        language = GameSubtitleLanguage.Chinese;
        return false;
    }

    private void BindVolumeControls()
    {
        ConfigureSettingsOptionFrames();
        volumeLabel = FindTextStartingWith("音量");
        ConfigureVolumeLayout();
        EnsureVolumePreviewAudio();

        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label == null)
            {
                continue;
            }

            string compact = Compact(label.text);
            if (compact != "+" && compact != "-")
            {
                continue;
            }

            int instanceId = button.GetInstanceID();
            if (!boundVolumeButtons.Add(instanceId))
            {
                continue;
            }

            if (compact == "+")
            {
                ConfigureVolumeStepButton(button, 1f);
                button.onClick.AddListener(IncreaseVolume);
            }
            else
            {
                ConfigureVolumeStepButton(button, -1f);
                button.onClick.AddListener(DecreaseVolume);
            }
        }

        UpdateVolumeLabel();
    }

    private static void ConfigureSettingsOptionFrames()
    {
        Button[] buttons = FindObjectsByType<Button>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label == null)
            {
                continue;
            }

            string compact = Compact(label.text);
            bool isMainSettingsOption = compact == "語言"
                || compact == "返回"
                || compact.StartsWith("音量", System.StringComparison.Ordinal);
            if (!isMainSettingsOption)
            {
                continue;
            }

            RectTransform buttonRect = button.transform as RectTransform;
            if (buttonRect != null)
            {
                buttonRect.sizeDelta = new Vector2(
                    Mathf.Max(200f, buttonRect.sizeDelta.x),
                    Mathf.Max(38f, buttonRect.sizeDelta.y));
            }

            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.offsetMin = new Vector2(12f, 4f);
            labelRect.offsetMax = new Vector2(-12f, -4f);

            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = 12f;
            label.fontSizeMax = 21f;
            label.textWrappingMode = TextWrappingModes.NoWrap;
        }
    }

    private void IncreaseVolume()
    {
        GameAudioSettings.Increase();
        UpdateVolumeLabel();
        PlayVolumePreview();
    }

    private void DecreaseVolume()
    {
        GameAudioSettings.Decrease();
        UpdateVolumeLabel();
        PlayVolumePreview();
    }

    private void UpdateVolumeLabel()
    {
        if (volumeLabel != null)
        {
            volumeLabel.text = "音量 " + GameAudioSettings.Level + " / 10";
        }
    }

    private void ConfigureVolumeLayout()
    {
        if (volumeLabel == null)
        {
            return;
        }

        RectTransform rowRect = volumeLabel.GetComponentInParent<Button>()?.transform as RectTransform;
        if (rowRect != null)
        {
            rowRect.sizeDelta = new Vector2(
                Mathf.Max(200f, rowRect.sizeDelta.x),
                Mathf.Max(38f, rowRect.sizeDelta.y));
        }

        RectTransform labelRect = volumeLabel.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = Vector2.zero;
        labelRect.offsetMin = new Vector2(27f, 1f);
        labelRect.offsetMax = new Vector2(-27f, -1f);

        volumeLabel.alignment = TextAlignmentOptions.Center;
        volumeLabel.enableAutoSizing = true;
        volumeLabel.fontSizeMin = 13f;
        volumeLabel.fontSizeMax = 21f;
        volumeLabel.textWrappingMode = TextWrappingModes.NoWrap;
        volumeLabel.raycastTarget = false;
    }

    private static void ConfigureVolumeStepButton(Button button, float direction)
    {
        RectTransform buttonRect = button.transform as RectTransform;
        if (buttonRect == null)
        {
            return;
        }

        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(78f * direction, 0f);
        buttonRect.sizeDelta = new Vector2(14f, 14f);

        TextMeshProUGUI symbol = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (symbol != null)
        {
            RectTransform symbolRect = symbol.rectTransform;
            symbolRect.anchorMin = Vector2.zero;
            symbolRect.anchorMax = Vector2.one;
            symbolRect.pivot = new Vector2(0.5f, 0.5f);
            symbolRect.anchoredPosition = Vector2.zero;
            symbolRect.offsetMin = Vector2.zero;
            symbolRect.offsetMax = Vector2.zero;

            symbol.alignment = TextAlignmentOptions.Center;
            symbol.enableAutoSizing = true;
            symbol.fontSizeMin = 8f;
            symbol.fontSizeMax = 14f;
            symbol.textWrappingMode = TextWrappingModes.NoWrap;
            symbol.raycastTarget = false;
        }
    }

    private void EnsureVolumePreviewAudio()
    {
        if (volumePreviewClip == null)
        {
            volumePreviewClip = Resources.Load<AudioClip>("VolumePreview");
        }

        if (volumePreviewSource == null)
        {
            volumePreviewSource = gameObject.GetComponent<AudioSource>();
            if (volumePreviewSource == null)
            {
                volumePreviewSource = gameObject.AddComponent<AudioSource>();
            }

            volumePreviewSource.playOnAwake = false;
            volumePreviewSource.loop = false;
            volumePreviewSource.spatialBlend = 0f;
            volumePreviewSource.ignoreListenerPause = true;
        }
    }

    private void PlayVolumePreview()
    {
        EnsureVolumePreviewAudio();
        if (volumePreviewSource == null || volumePreviewClip == null)
        {
            return;
        }

        volumePreviewSource.Stop();
        volumePreviewSource.clip = volumePreviewClip;
        volumePreviewSource.volume = 1f;
        volumePreviewSource.Play();
    }

    private void SelectLanguage(GameSubtitleLanguage language)
    {
        if (returningToSettings)
        {
            return;
        }

        GameLanguageSettings.SetLanguage(language);
        returningToSettings = true;
        StartCoroutine(ReturnToSettingsAfterClickSound());
    }

    private IEnumerator ReturnToSettingsAfterClickSound()
    {
        yield return new WaitForSecondsRealtime(0.18f);

        if (Application.CanStreamedLevelBeLoaded("設定內容"))
        {
            SceneManager.LoadScene("設定內容");
        }
        else
        {
            returningToSettings = false;
        }
    }

    private void ArrangeChapterSelection()
    {
        TextMeshProUGUI[] allTexts =
            FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        TextMeshProUGUI[] chapterLabels = new TextMeshProUGUI[ChapterPrefixes.Length];

        for (int i = 0; i < allTexts.Length; i++)
        {
            string compact = Compact(allTexts[i].text);
            for (int chapterIndex = 0; chapterIndex < ChapterPrefixes.Length; chapterIndex++)
            {
                if (compact.StartsWith(ChapterPrefixes[chapterIndex]))
                {
                    chapterLabels[chapterIndex] = allTexts[i];
                    break;
                }
            }
        }

        if (chapterLabels[0] == null)
        {
            return;
        }

        Canvas canvas = chapterLabels[0].GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return;
        }

        RectTransform canvasRect = canvas.transform as RectTransform;
        if (canvasRect == null)
        {
            return;
        }

        canvasRect.sizeDelta = new Vector2(
            Mathf.Max(132f, canvasRect.sizeDelta.x),
            Mathf.Max(100f, canvasRect.sizeDelta.y));

        Button[] allButtons = canvas.GetComponentsInChildren<Button>(true);
        Button[] chapterButtons = new Button[ChapterPrefixes.Length];
        HashSet<Button> usedButtons = new HashSet<Button>();

        for (int i = 0; i < chapterLabels.Length; i++)
        {
            if (chapterLabels[i] == null)
            {
                continue;
            }

            Button parentButton = chapterLabels[i].GetComponentInParent<Button>();
            if (parentButton != null && parentButton.GetComponentInParent<Canvas>() == canvas)
            {
                chapterButtons[i] = parentButton;
                usedButtons.Add(parentButton);
            }
        }

        for (int i = 0; i < chapterLabels.Length; i++)
        {
            if (chapterLabels[i] == null || chapterButtons[i] != null)
            {
                continue;
            }

            float closestDistance = float.MaxValue;
            Button closestButton = null;
            for (int buttonIndex = 0; buttonIndex < allButtons.Length; buttonIndex++)
            {
                Button candidate = allButtons[buttonIndex];
                if (usedButtons.Contains(candidate))
                {
                    continue;
                }

                float distance = (candidate.transform.position - chapterLabels[i].transform.position).sqrMagnitude;
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestButton = candidate;
                }
            }

            if (closestButton != null)
            {
                chapterButtons[i] = closestButton;
                usedButtons.Add(closestButton);
            }
        }

        for (int i = 0; i < chapterButtons.Length; i++)
        {
            Button button = chapterButtons[i];
            TextMeshProUGUI label = chapterLabels[i];
            if (button == null || label == null)
            {
                continue;
            }

            RectTransform buttonRect = button.transform as RectTransform;
            RectTransform labelRect = label.rectTransform;

            buttonRect.SetParent(canvasRect, false);
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.localScale = Vector3.one;
            buttonRect.sizeDelta = new Vector2(42f, 27.5f);
            buttonRect.anchoredPosition = ChapterPositions[i];
            buttonRect.SetSiblingIndex(canvasRect.childCount - 1);

            labelRect.SetParent(buttonRect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.localScale = Vector3.one;
            labelRect.offsetMin = new Vector2(2.2f, 2.2f);
            labelRect.offsetMax = new Vector2(-2.2f, -2.2f);

            label.text = ChapterTitles[i];
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = 3.2f;
            label.fontSizeMax = 6f;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
        }

        CenterCanvasVertically(canvas);
        Canvas.ForceUpdateCanvases();
    }

    private static void CenterCanvasVertically(Canvas canvas)
    {
        if (canvas == null)
        {
            return;
        }

        RectTransform canvasRect = canvas.transform as RectTransform;
        if (canvasRect == null)
        {
            return;
        }

        if (canvas.renderMode == RenderMode.WorldSpace)
        {
            Camera camera = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
            if (camera == null)
            {
                return;
            }

            Vector3 screenPosition = camera.WorldToScreenPoint(canvasRect.position);
            if (screenPosition.z <= 0f)
            {
                return;
            }

            screenPosition.y = Screen.height * 0.5f;
            canvasRect.position = camera.ScreenToWorldPoint(screenPosition);
        }
        else
        {
            Vector2 anchoredPosition = canvasRect.anchoredPosition;
            anchoredPosition.y = 0f;
            canvasRect.anchoredPosition = anchoredPosition;
        }
    }

    private static TextMeshProUGUI FindTextStartingWith(string prefix)
    {
        TextMeshProUGUI[] texts =
            FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < texts.Length; i++)
        {
            if (Compact(texts[i].text).StartsWith(prefix))
            {
                return texts[i];
            }
        }

        return null;
    }

    private static string Compact(string value)
    {
        return string.IsNullOrEmpty(value)
            ? string.Empty
            : value.Replace(" ", string.Empty).Replace("\r", string.Empty).Replace("\n", string.Empty).Trim();
    }
}
