using System.Collections;
using TMPro;
using UnityEngine;

public class Chapter1DialogueUI : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI bodyText;

    private Coroutine hideRoutine;

    private void Awake()
    {
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
