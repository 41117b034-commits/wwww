using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Chapter1ChoiceUI : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public TextMeshProUGUI questionText;
    public TextMeshProUGUI optionAText;
    public TextMeshProUGUI optionBText;
    public Button optionAButton;
    public Button optionBButton;

    private Chapter1PerformanceController controller;
    private bool ownsPointer;
    private CursorLockMode previousLock;
    private bool previousVisibility;

    private void Awake()
    {
        GameDialogueOverlay.Ensure(GetComponentInParent<Canvas>());
        HideInstant();
    }

    public void Bind(Chapter1PerformanceController target)
    {
        controller = target;

        if (optionAButton != null)
        {
            optionAButton.onClick.RemoveListener(OnOptionA);
            optionAButton.onClick.AddListener(OnOptionA);
        }

        if (optionBButton != null)
        {
            optionBButton.onClick.RemoveListener(OnOptionB);
            optionBButton.onClick.AddListener(OnOptionB);
        }
    }

    public void Show(string question, string optionA, string optionB, bool releasePointer = false)
    {
        if (!releasePointer) RestorePointer();
        if (releasePointer && !ownsPointer)
        {
            previousLock = Cursor.lockState;
            previousVisibility = Cursor.visible;
            ownsPointer = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        if (questionText != null)
        {
            questionText.text = question;
        }

        if (optionAText != null)
        {
            optionAText.text = optionA;
        }

        if (optionBText != null)
        {
            optionBText.text = optionB;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }
    }

    public void Hide()
    {
        HideInstant();
    }

    public void HideInstant()
    {
        RestorePointer();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }

    private void RestorePointer()
    {
        if (!ownsPointer) return;
        Cursor.lockState = previousLock;
        Cursor.visible = previousVisibility;
        ownsPointer = false;
    }

    private void OnDisable() => HideInstant();

    private void OnOptionA()
    {
        if (controller != null)
        {
            controller.ChooseIntervene();
        }
    }

    private void OnOptionB()
    {
        if (controller != null)
        {
            controller.ChooseWatch();
        }
    }
}
