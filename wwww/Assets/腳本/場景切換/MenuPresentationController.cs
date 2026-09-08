using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class MenuPresentationController : MonoBehaviour
{
    private static MenuPresentationController instance;

    private readonly HashSet<int> boundLanguageButtons = new HashSet<int>();
    private readonly HashSet<int> boundVolumeButtons = new HashSet<int>();
    private TextMeshProUGUI volumeLabel;
    private bool returningToSettings;

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

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        boundLanguageButtons.Clear();
        boundVolumeButtons.Clear();
        volumeLabel = null;
        returningToSettings = false;
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

        if (scene.name == "點選介面")
        {
            CenterMainMenuCanvas();
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

        Canvas canvas = title.GetComponentInParent<Canvas>();
        CenterCanvasVertically(canvas);
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
        volumeLabel = FindTextStartingWith("音量");

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
                button.onClick.AddListener(IncreaseVolume);
            }
            else
            {
                button.onClick.AddListener(DecreaseVolume);
            }
        }

        UpdateVolumeLabel();
    }

    private void IncreaseVolume()
    {
        GameAudioSettings.Increase();
        UpdateVolumeLabel();
    }

    private void DecreaseVolume()
    {
        GameAudioSettings.Decrease();
        UpdateVolumeLabel();
    }

    private void UpdateVolumeLabel()
    {
        if (volumeLabel != null)
        {
            volumeLabel.text = "音量　" + GameAudioSettings.Level + " / 10";
        }
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
