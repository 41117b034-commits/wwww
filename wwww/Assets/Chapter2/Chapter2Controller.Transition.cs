using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed partial class Chapter2Controller
{
    public const string Chapter2ResultKey = "WusheEvent.Chapter2.Result";
    public const string FollowingChapterScene = "第三章";
    // A runtime-only validation switch: it cannot leave a saved scene in preview mode.
    [NonSerialized] public bool HoldAfterEndingForValidation;
    public bool ResultSaved { get; private set; }
    public bool TransitionInProgress { get; private set; }
    public string TransitionError { get; private set; }

    void FinishChapterAndContinue()
    {
        player.canMove = player.canLook = false;
        ui.continueButton.gameObject.SetActive(false);
        // RunMeeting reaches here only after both branches' final fade and hold.
        if (!TrySaveChapter2Result()) return;
        if (!HoldAfterEndingForValidation) ContinueToChapter3();
    }

    bool TrySaveChapter2Result()
    {
        return TrySaveChapter2Result(PlayerPrefs.Save);
    }

    internal bool TrySaveChapter2Result(Action persist)
    {
        // P previews have no completed daytime route. Keep the previous real result.
        if (ResultSaved || !saveResult || meetingShortcutUsed) return true;
        if (TreeDecision < 0 || TreeDecision > 1 || MeetingDecision < 0 || MeetingDecision > 1)
        {
            ShowTransitionError("第二章的選擇尚未完成，結果未保存。");
            return false;
        }
        if (persist == null)
        {
            ShowTransitionError("第二章結果尚未保存，請按下方按鈕重試。");
            return false;
        }
        var result = new Chapter2Result
        {
            treeChoice = TreeDecision == 0 ? "protect" : "fell",
            meetingChoice = MeetingDecision == 0 ? "support" : "refuse",
            woodIntegrity = Integrity, validCuts = ValidCuts, failedCuts = FailedCuts,
            casualties = TreeDecision == 0 ? 1 : 0, completed = true
        };
        bool hadPrevious = PlayerPrefs.HasKey(Chapter2ResultKey);
        string previous = PlayerPrefs.GetString(Chapter2ResultKey, "");
        try
        {
            string json = JsonUtility.ToJson(result);
            PlayerPrefs.SetString(Chapter2ResultKey, json);
            persist();
            ResultSaved = PlayerPrefs.GetString(Chapter2ResultKey, "") == json;
        }
        catch (PlayerPrefsException) { ResultSaved = false; }
        if (!ResultSaved)
        {
            // Restore the pending preferences too: a later unrelated Save must
            // not flush the chapter result whose own persistence just failed.
            if (hadPrevious) PlayerPrefs.SetString(Chapter2ResultKey, previous);
            else PlayerPrefs.DeleteKey(Chapter2ResultKey);
            ShowTransitionError("第二章結果尚未保存，請按下方按鈕重試。");
        }
        return ResultSaved;
    }

    public void ContinueToChapter3()
    {
        if (CurrentStage != Stage.Complete || TransitionInProgress) return;
        if (!TrySaveChapter2Result()) return;
        if (!Application.CanStreamedLevelBeLoaded(FollowingChapterScene))
        {
            ShowTransitionError("第三章尚未載入，請確認章節已加入建置後重試。");
            return;
        }
        TransitionInProgress = true; TransitionError = null;
        ui.continueButton.interactable = false;
        ui.continueButton.gameObject.SetActive(false);
        if (exploration) exploration.CancelExploration();
        if (chapterAudio) chapterAudio.StopAllAudio();
        if (ambience) ambience.Stop();
        if (fireAudio) fireAudio.Stop();
        if (effects) effects.Stop();
        player.canMove = player.canLook = false;
        // Do not carry a paused or accelerated second-chapter clock into a new scene.
        Time.timeScale = 1;
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        StartCoroutine(LoadFollowingChapter());
    }

    IEnumerator LoadFollowingChapter()
    {
        AsyncOperation operation = null;
        try { operation = SceneManager.LoadSceneAsync(FollowingChapterScene, LoadSceneMode.Single); }
        catch (Exception) { ShowTransitionError("第三章載入失敗，請按下方按鈕重試。"); }
        if (operation == null)
        {
            if (TransitionError == null) ShowTransitionError("第三章載入失敗，請按下方按鈕重試。");
            yield break;
        }
        yield return operation;
    }

    void ShowTransitionError(string message)
    {
        TransitionInProgress = false; TransitionError = message;
        ui.endText.text = message;
        var label = ui.continueButton.GetComponentInChildren<Text>();
        if (label) label.text = "重試並前往第三章";
        ui.endingPanel.SetActive(true);
        ui.continueButton.interactable = true;
        ui.continueButton.gameObject.SetActive(true);
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
    }
}
