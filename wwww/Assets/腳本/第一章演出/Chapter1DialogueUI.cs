using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Chapter1DialogueUI : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI bodyText;

    private Coroutine hideRoutine;
    private Text chapterSpeaker, chapterBody;

    public static Chapter1DialogueUI Create(Canvas canvas, Font font)
    {
        var panel = Chapter3Presentation.CreateSubtitlePanel(canvas.transform, font, out Text speaker, out Text body);
        panel.GetComponent<Image>().raycastTarget = false;
        panel.gameObject.AddComponent<CanvasGroup>();
        var ui = panel.gameObject.AddComponent<Chapter1DialogueUI>();
        ui.chapterSpeaker = speaker;
        ui.chapterBody = body;
        return ui;
    }

    private void Awake()
    {
        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
        GameDialogueOverlay.Ensure(GetComponentInParent<Canvas>());
        HideInstant();
    }

    public void ShowLine(string speaker, string line, float seconds)
    {
        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
        }

        if (!GameLanguageSettings.SubtitlesEnabled)
        {
            HideInstant();
            return;
        }

        bool showSpeaker = !ShouldHideNarrationLabel(speaker);
        if (chapterBody)
        {
            chapterBody.text = GameLanguageSettings.LocalizeSubtitle(line);
            chapterSpeaker.text = showSpeaker ? GameLanguageSettings.LocalizeSpeaker(speaker) : "";
            chapterBody.rectTransform.anchorMax = new Vector2(.965f, showSpeaker ? .67f : .92f);
            // A narration line has no name row. Keep its lower edge and give it just the space it needs.
            var panel = (RectTransform)transform;
            panel.anchorMax = new Vector2(.86f, showSpeaker ? .29f : .21f);
            panel.offsetMax = Vector2.zero;
            float bodyFraction = chapterBody.rectTransform.anchorMax.y - .08f;
            float requiredHeight = (chapterBody.preferredHeight + 2f) / bodyFraction;
            panel.offsetMax = Vector2.up * Mathf.Max(0f, requiredHeight - panel.rect.height);
        }
        if (speakerText != null)
        {
            speakerText.gameObject.SetActive(showSpeaker);
            speakerText.text = showSpeaker
                ? GameLanguageSettings.LocalizeSpeaker(speaker)
                : string.Empty;
        }

        if (bodyText != null)
        {
            bodyText.text = GameLanguageSettings.LocalizeSubtitle(line);
        }

        ShowInstant();
        hideRoutine = StartCoroutine(HideAfter(seconds));
    }

    private static bool ShouldHideNarrationLabel(string speaker)
    {
        if (string.IsNullOrWhiteSpace(speaker))
        {
            return true;
        }

        string label = speaker.Trim();
        return label == "字幕"
            || label == "旁白"
            || label.Equals("Subtitle", System.StringComparison.OrdinalIgnoreCase)
            || label.Equals("Narration", System.StringComparison.OrdinalIgnoreCase)
            || label.Equals("Narrator", System.StringComparison.OrdinalIgnoreCase);
    }

    public void ShowInstant()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }

    public void HideInstant()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }

    private IEnumerator HideAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        HideInstant();
    }
}
