// Copy into the temporary Editor probe only while validating. The runner must
// survive scene loads (Editor update runner or DontDestroyOnLoad MonoBehaviour).
// This observes the real council coroutine; it does not set its stage or result.
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Chapter2EntryVerification
{
    public static IEnumerator CouncilToThirdChapter(Chapter2Controller chapter, int choice,
        bool isPreview, Action<string, bool> check)
    {
        string key = Chapter2Controller.Chapter2ResultKey;
        bool previousExists = PlayerPrefs.HasKey(key);
        string previous = PlayerPrefs.GetString(key, "");
        bool chapter3Exists = PlayerPrefs.HasKey(Chapter3ResultStore.SaveKey);
        string chapter3Previous = PlayerPrefs.GetString(Chapter3ResultStore.SaveKey, "");
        int treeChoice = chapter.TreeDecision;
        bool completeSeen = false, fadeComplete = false, prematureSave = false;
        bool selected = false; int thirdChapterLoads = 0;
        Action<Chapter2Controller.Stage> stageChanged = stage =>
        {
            if (stage != Chapter2Controller.Stage.Complete) return;
            completeSeen = true;
            fadeComplete = chapter.ui.fade.color.a >= .999f;
        };
        UnityEngine.Events.UnityAction<Scene, LoadSceneMode> sceneLoaded = (scene, mode) =>
        { if (scene.name == Chapter2Controller.FollowingChapterScene) thirdChapterLoads++; };
        chapter.StageChanged += stageChanged;
        SceneManager.sceneLoaded += sceneLoaded;
        try
        {
            check("uses-default-automatic-transition", !chapter.HoldAfterEndingForValidation);
            chapter.ContinueToChapter3();
            check("cannot-enter-third-chapter-before-ending", !chapter.TransitionInProgress);
            float deadline = Time.realtimeSinceStartup + 240;
            while (thirdChapterLoads == 0 && Time.realtimeSinceStartup < deadline)
            {
                if (chapter)
                {
                    if (!completeSeen && (PlayerPrefs.HasKey(key) != previousExists ||
                        PlayerPrefs.GetString(key, "") != previous)) prematureSave = true;
                    if (!selected && chapter.CurrentStage == Chapter2Controller.Stage.Vote)
                    {
                        if (choice == 0) chapter.ui.buttonA.onClick.Invoke();
                        else chapter.ui.buttonB.onClick.Invoke();
                        selected = chapter.MeetingDecision == choice;
                    }
                }
                yield return null;
            }
            check("selected-requested-council-branch", selected);
            check("original-council-reaches-complete", completeSeen);
            check("fade-is-black-before-completion", fadeComplete);
            check("no-result-overwrite-before-completion", !prematureSave);
            check("automatic-scene-load-exactly-once", thirdChapterLoads == 1);
            if (thirdChapterLoads == 0) yield break;
            yield return null; yield return null;
            check("old-chapter-unloaded", !chapter);
            check("clock-reset-for-third-chapter", Mathf.Approximately(Time.timeScale, 1));
            var flow = UnityEngine.Object.FindFirstObjectByType<Chapter3Flow>();
            check("third-chapter-starts-at-station", flow && flow.Region == "Station");
            check("fresh-third-chapter-has-no-equipment", flow && !flow.CarriedWeapon.HasValue);
            if (isPreview)
                check("preview-preserves-second-chapter-result", PlayerPrefs.HasKey(key) == previousExists &&
                    PlayerPrefs.GetString(key, "") == previous);
            else
            {
                Chapter2Controller.Chapter2Result saved = null;
                try { saved = JsonUtility.FromJson<Chapter2Controller.Chapter2Result>(PlayerPrefs.GetString(key, "")); }
                catch (ArgumentException) { }
                check("formal-result-is-completed", saved != null && saved.completed);
                check("formal-result-keeps-council-choice", saved != null && saved.meetingChoice == (choice == 0 ? "support" : "refuse"));
                check("formal-result-keeps-daytime-choice", saved != null && saved.treeChoice == (treeChoice == 0 ? "protect" : "fell"));
            }
            check("entry-preserves-previous-third-chapter-result",
                PlayerPrefs.HasKey(Chapter3ResultStore.SaveKey) == chapter3Exists &&
                PlayerPrefs.GetString(Chapter3ResultStore.SaveKey, "") == chapter3Previous);
        }
        finally
        {
            if (chapter) chapter.StageChanged -= stageChanged;
            SceneManager.sceneLoaded -= sceneLoaded;
        }
    }
}
