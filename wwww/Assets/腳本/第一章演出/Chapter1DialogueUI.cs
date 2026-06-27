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

        if (speakerText != null)
        {
            speakerText.text = speaker;
        }

        if (bodyText != null)
        {
            bodyText.text = line;
        }

        ShowInstant();
        hideRoutine = StartCoroutine(HideAfter(seconds));
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
